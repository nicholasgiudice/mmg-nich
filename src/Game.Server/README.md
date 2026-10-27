# Game.Server — servidor de zona (T-105)

Servidor autoritativo: roda o mesmo `Game.Core` do cliente a 20 Hz, uma sala com um encontro do protótipo, login de desenvolvimento (só um nome).

```bash
dotnet run --project src/Game.Server
dotnet run --project src/Game.Server -- --encounter encounter.four_rats --port 7777 --bots false
dotnet run --project src/Game.Server -- --help
```

- **Opções:** `--data`, `--port` (UDP, padrão 7777), `--encounter` (padrão Matriarca), `--seed`, `--bots` (vagas sem jogador jogam como bots; padrão `true`), `--disconnect-grace` (s até remover quem caiu; padrão 30), `--max-catch-up`, `--max-players`, `--restart-after` (s mostrando o fim do encontro antes de recomeçar; padrão 10, 0 = não recomeça), `--interest-cell` (16 tiles), `--interest-radius` (2 células), `--max-delta-age` (40 ticks), `--log-level` (`verbose` · `debug` · `info` · `warning` · `error` · `fatal`; padrão `info`). Variáveis de ambiente: `MMO_DATA_DIR`, `MMO_PORT`, `MMO_LOG_LEVEL`.
- **Console:** `restart` (reinicia o encontro; quem está conectado recebe um `Welcome` novo), `status`, `quit`. Ctrl+C e SIGTERM (`docker compose stop`, systemd) também param: as conexões são avisadas e o log mostra "Servidor parado".
- **Entrada:** o cliente manda `Hello` com a versão do protocolo e o fingerprint de `data/`; diferente = `Reject`. Cada jogador ocupa uma vaga de jogador do encontro com a classe pedida (vaga da mesma classe; senão a primeira livre, que troca de classe). Sem vaga = `Reject` (`Full`).
- **Contas (T-212):** com `MMO_DB_CONNECTION`, o `Hello` pode levar o token de jogo da API (`Hello.Ticket`). O servidor valida assinatura, servidor (`MMO_GAME_SERVER_ID`), expiração e uso único com `MMO_GAME_TICKET_KEY`, sem chamar a API. Depois carrega o personagem fora do loop e o põe numa vaga com nível, pontos, NH, equipamento e inventário salvos. Recusas: `Unauthorized` (token), `AlreadyConnected` (personagem já está dentro) e `Unavailable` (banco fora, personagem apagado ou corrompido). Sem token, só entra com `--dev-login` (ou `MMO_DEV_LOGIN=true`): é o login só com nome, de antes, usado pelo cliente atual, pelos testes e pelos bots.
- **Gravação (T-212):** `Persistence/CharacterPersistence` grava fora do loop, com um trabalhador e uma fila que guarda só o estado mais novo de cada personagem. Grava ao sair, a cada `--save-interval` (padrão 120 s), ao subir de nível, ao pegar item raro ou épico, no reinício da sala e na parada (SIGTERM espera até 20 s; o Compose dá 30 s). Falha do banco é registrada e repetida a cada 5 s. Formato do documento `progression`: `Persistence/CharacterState.cs` (`CharacterDocument`). Zona e posição ainda não são gravadas: a sala é um encontro, não um mapa (T-214).
- **A cada tick (T-106):** para cada jogador, `SnapshotDelta` só com as entidades da área de interesse dele (grade de células; o grupo e os chefes sempre), como diferença em relação ao último tick que ele confirmou com `SnapshotAck` (canal 1, não confiável). Sem confirmação recente, vai completo. `Movement` só vai para o dono. Eventos só das entidades que ele vê, mais `IntentAck` (canal 0, confiável e ordenado). O cliente monta o estado com `Game.Protocol.SnapshotReconstructor`.
- **Ciclo da luta:** sala sem jogadores fica parada no começo (os bots não lutam sozinhos). Quem entra numa sala vazia começa a luta do zero. No fim (vitória, derrota ou tempo esgotado) a sala mostra o resultado por `--restart-after` segundos e recomeça para quem está nela, com um `Welcome` novo.
- **Quem cai:** a entidade fica parada (ou vira bot) e é removida depois do prazo; voltando com o mesmo nome antes disso, retoma a mesma entidade. Sem jogadores, a sala reinicia.
- **Log (Serilog):** conexões, recusas, entradas e saídas, e o tempo de tick a cada 10 s.

Estrutura: `ZoneRoom` (sala e regras), `TickLoop` (relógio de 20 Hz sem deriva, recupera até N ticks e pula o resto com aviso), `Interest/` (`InterestGrid`, `ClientView` com o delta por cliente, `EventEntities`), `Transport/` (`LiteNetTransport` em produção, `InMemoryTransport` nos testes, com perda simulada).
