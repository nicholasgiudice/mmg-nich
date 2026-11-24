# Game.Server — servidor de zona (T-105)

Servidor autoritativo: roda o mesmo `Game.Core` do cliente a 20 Hz, uma sala com um encontro do protótipo, login de desenvolvimento (só um nome).

```bash
dotnet run --project src/Game.Server
dotnet run --project src/Game.Server -- --encounter encounter.four_rats --port 7777 --bots false
dotnet run --project src/Game.Server -- --help
```

- **Opções:** `--data`, `--port` (UDP, padrão 7777), `--dev-login`, `--dev-tools` (ferramenta de teste, seção abaixo), `--encounter` (padrão Matriarca), `--seed`, `--bots` (vagas sem jogador jogam como bots; padrão `true`), `--disconnect-grace` (s até remover quem caiu; padrão 30), `--max-catch-up`, `--max-players`, `--restart-after` (s mostrando o fim do encontro antes de recomeçar; padrão 10, 0 = não recomeça), `--interest-cell` (16 tiles), `--interest-radius` (2 células), `--max-delta-age` (40 ticks), `--log-level` (`verbose` · `debug` · `info` · `warning` · `error` · `fatal`; padrão `info`). Variáveis de ambiente: `MMO_DATA_DIR`, `MMO_PORT`, `MMO_LOG_LEVEL`, `MMO_DEV_LOGIN`, `MMO_DEV_TOOLS`.
- **Console:** `restart` (reinicia o encontro; quem está conectado recebe um `Welcome` novo), `status`, `quit`. Ctrl+C e SIGTERM (`docker compose stop`, systemd) também param: as conexões são avisadas e o log mostra "Servidor parado".
- **Entrada:** o cliente manda `Hello` com a versão do protocolo e o fingerprint de `data/`; diferente = `Reject`. Cada jogador ocupa uma vaga de jogador do encontro com a classe pedida (vaga da mesma classe; senão a primeira livre, que troca de classe). Sem vaga = `Reject` (`Full`).
- **Contas (T-212):** com `MMO_DB_CONNECTION`, o `Hello` pode levar o token de jogo da API (`Hello.Ticket`). O servidor valida assinatura, servidor (`MMO_GAME_SERVER_ID`), expiração e uso único com `MMO_GAME_TICKET_KEY`, sem chamar a API. Depois carrega o personagem fora do loop e o põe numa vaga com nível, pontos, NH, equipamento e inventário salvos. Recusas: `Unauthorized` (token), `AlreadyConnected` (personagem já está dentro) e `Unavailable` (banco fora, personagem apagado ou corrompido). Sem token, só entra com `--dev-login` (ou `MMO_DEV_LOGIN=true`): é o login só com nome, de antes, usado pelo cliente atual, pelos testes e pelos bots.
- **Gravação (T-212):** `Persistence/CharacterPersistence` grava fora do loop, com um trabalhador e uma fila que guarda só o estado mais novo de cada personagem. Grava ao sair, a cada `--save-interval` (padrão 120 s), ao subir de nível, ao pegar item raro ou épico, no reinício da sala e na parada (SIGTERM espera até 20 s; o Compose dá 30 s). Falha do banco é registrada e repetida a cada 5 s. Formato do documento `progression`: `Persistence/CharacterState.cs` (`CharacterDocument`). Zona e posição ainda não são gravadas: a sala é um encontro, não um mapa (T-214).
- **A cada tick (T-106):** para cada jogador, `SnapshotDelta` só com as entidades da área de interesse dele (grade de células; o grupo e os chefes sempre), como diferença em relação ao último tick que ele confirmou com `SnapshotAck` (canal 1, não confiável). Sem confirmação recente, vai completo. `Movement` só vai para o dono. Eventos só das entidades que ele vê, mais `IntentAck` (canal 0, confiável e ordenado). O cliente monta o estado com `Game.Protocol.SnapshotReconstructor`.
- **Ciclo da luta:** sala sem jogadores fica parada no começo (os bots não lutam sozinhos). Quem entra numa sala vazia começa a luta do zero. No fim (vitória, derrota ou tempo esgotado) a sala mostra o resultado por `--restart-after` segundos e recomeça para quem está nela, com um `Welcome` novo.
- **Quem cai:** a entidade fica parada (ou vira bot) e é removida depois do prazo; voltando com o mesmo nome antes disso, retoma a mesma entidade. Sem jogadores, a sala reinicia.
- **Log (Serilog):** conexões, recusas, entradas e saídas, e o tempo de tick a cada 10 s.

Estrutura: `ZoneRoom` (sala e regras), `TickLoop` (relógio de 20 Hz sem deriva, recupera até N ticks e pula o resto com aviso), `Interest/` (`InterestGrid`, `ClientView` com o delta por cliente, `EventEntities`), `Transport/` (`LiteNetTransport` em produção, `InMemoryTransport` nos testes, com perda simulada).

## Ferramenta de teste (T-405)

`--dev-tools` (ou `MMO_DEV_TOOLS=true`; no Compose, `GAME_DEV_TOOLS=true` em `deploy/.env`) liga a ferramenta do designer para testar as segundas classes sem as áreas do 16 ao 40 (`design/segunda-classe-v0.1.md` §4.2). **Desligada por padrão; nunca num servidor aberto a jogadores.** Ligada, o servidor avisa no log ao subir:

```
[WRN] Ferramenta de teste ativa: qualquer jogador pode subir de nível e desfazer a segunda classe (--dev-tools). Nunca num servidor aberto a jogadores
```

- O `Welcome` leva `DevTools = true`: só então o cliente mostra o botão (menu do Esc, T-411).
- O cliente manda `DevToolRequest(Action)`: `LevelUp1` (+1 nível), `LevelUp5` (+5), `ToSecondClassLevel` (nível 20, `progression.secondClassLevel`), `ToMaxLevel` (nível 40, `progression.maxLevel`), `UndoSecondClass` (tira a segunda classe, devolve os NH dela e os +5 de atributo; se os +5 já foram distribuídos, devolve todos os pontos de atributo) e `FreeReset` (a redistribuição normal, sem custo).
- Subir de nível dá a XP que falta por `Leveling.GrantExperience`, o mesmo caminho da XP de monstro: mesmos pontos, mesma vida, um `LevelUp` por nível. Nada passa do nível máximo.
- Resposta sempre: `DevToolResult(Action, Outcome, Level)`. `Outcome`: `Done`, `Disabled` (servidor sem `--dev-tools`; o log registra quem pediu), `InCombat` (em combate ou conjurando, como a redistribuição), `Dead`, `NoProgression` (personagem de encontro da sala), `AlreadyAtLevel`, `NoSecondClass`.
- Aceito, o personagem com conta é gravado na hora. Com `--dev-login`, o personagem não tem banco: o pedido só muda o personagem em memória (some ao sair).
- Funciona no mundo aberto e na sala de encontro (lá, só personagens com conta têm progressão).

Playtest local (Windows ou VPS, sem banco): suba o servidor e, no cliente, entre sem conta (`MenuScreen`) em `127.0.0.1:7777`.

```bash
dotnet run --project src/Game.Server -c Release -- --world --dev-login --dev-tools --port 7777
```

## Mundo aberto (T-214)

`--world` (ou `MMO_WORLD=true`) troca a sala de encontro pelas zonas de `data/zones.json`: hoje a Cidade-Fortaleza (`zone.city`) e os Campos da Vigília (`zone.vigil_fields`), com mapas provisórios em `data/arenas.json` (`arena.city`, `arena.vigil_fields`) até a T-055.

- `Zones/WorldServer`: conexões, entrada, intenções, portões, renascimento, gravação e envio por zona. `Zones/OpenZone`: um `World` por zona, com grade de interesse e pontos de surgimento. Só zonas com jogador avançam.
- Entrada: com token, na zona e posição gravadas (posição (0, 0) ou zona desconhecida = nascimento da zona inicial, `--start-zone`, padrão `zone.city`); com `--dev-login`, na zona inicial, nível 1 com progressão (não gravado).
- Cidade (`kind: safe`): sem monstros e com descanso (`restPerSecond`: fração da vida e da Mana por segundo, componente `Resting` do Core). Não há dano entre jogadores: as habilidades só acertam a facção inimiga.
- Campos (`kind: field`): cada ponto de surgimento tem monstro, quantidade, raio, nível e `respawn`. O corpo fica `corpse` segundos e o monstro renasce em volta do ponto.
- Portão: personagem vivo dentro da `area` vai para a zona `to`, na posição `arrival`, com o mesmo build, itens, vida e Mana. O cliente recebe um `Welcome` novo (`EncounterId` = id da zona, arena da zona, época nova).
- Morte: o personagem fica caído até o cliente mandar `RespawnRequest`; renasce cheio, sem perder nada, no nascimento da zona segura do campo `respawn` da zona em que morreu (T-501), ou da zona inicial se a zona não tem o campo. Morto que sai é gravado nessa mesma zona, no nascimento.
- Bots de rede: `--roam` anda do nascimento ao portão de cada zona sem parar; em qualquer modo, morto pede para renascer.
- Instância (`kind: instance`, T-350): não roda de saída. O portão que leva a ela põe o personagem na cópia do grupo dele (ou dele, sem grupo), criada na hora (`Zones/DungeonInstance`, `World` próprio com seed derivada do id da cópia). Limites: `maxPlayers` por cópia (padrão 4) e `--max-instances` no processo (padrão 20); a recusa chega como `InstanceRejected` e não se repete até sair do portão. Os monstros nascem uma vez e não renascem. O primeiro portão do mapa é a saída; a gravação usa a zona e a chegada dessa saída, nunca a instância. Morte lá dentro renasce como em qualquer zona (T-385, T-501): no `respawn` da instância ou, sem ele, na zona inicial; a cópia continua para o resto do grupo, e o portão leva de volta à mesma cópia, com os monstros como estavam, enquanto ela existir. Quem desconecta volta para dentro se reconectar enquanto a cópia existir; a cópia vazia some depois de `emptyTimeout` (padrão 300 s). Quem não é do grupo dono recebe `InstanceNotice` e sai depois de `outsiderTimeout` (padrão 30 s); se ninguém do grupo dono está dentro (ex.: grupo de 2 desfeito), a cópia passa para quem entrou primeiro. O `Welcome` leva o id da cópia (`InstanceId`, 0 = zona aberta).
- Posição do grupo (T-391): a cada 10 ticks (2 por segundo) o servidor manda `PartyPositions` (confiável) a cada membro de grupo com pelo menos um outro membro na mesma zona — e na mesma cópia, dentro de instância. Cada linha tem o `MemberId` (o do `PartyState`) e a posição em tiles (`X`, `Y`), tirada do mundo do servidor, mesmo fora da área de interesse. Membro morto continua na posição em que caiu até renascer. Sem grupo, ou sozinho na zona, não vai nada; quando o último membro sai da zona (ou quem recebe troca de zona, ou o grupo acaba), vai uma vez a lista vazia. Só no mundo aberto; a sala de encontro não tem grupo.

```bash
dotnet run --project src/Game.Server -c Release -- --world --dev-login --port 7790
dotnet run --project tests/Game.NetBots -c Release -- --server 127.0.0.1:7790 --bots 8 --duration 60 --roam
```

