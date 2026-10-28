# T-214 — Servidor: zonas abertas, Cidade e Campos, troca pelo portão

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-212
- **Libera:** T-215, T-223
- **Áreas que pode alterar:** `src/Game.Server/`, `src/Game.Core/` (só o necessário para zona aberta), `src/Game.Protocol/`, `data/arenas.json` ou mapas, testes correspondentes

## Contexto

Objetivo da fase 2: **Cidade + Campos online** (`design/first-map-v0.1.md` §1–§2). Hoje o servidor tem **uma sala com um encontro que termina**. Um mundo aberto precisa de zonas que não acabam: monstros que renascem, jogadores entrando e saindo a qualquer hora, troca de zona pelo portão. Os mapas desenhados no Tiled vêm da trilha de arte (T-051/T-055); se ainda não existirem, usar mapas provisórios nos dados com o mesmo tamanho aproximado.

## Requisitos

1. **Várias zonas num processo** (Cidade, Campos da Vigília), cada uma com seu `World`, tick próprio e interesse por grade (T-106). A sala de encontro atual continua existindo como tipo de zona (instância de teste/Matriarca).
2. **Cidade segura**: sem monstros, sem dano entre jogadores (PvP fora de escopo), regenera vida e Mana mais rápido; é onde o personagem nasce e renasce.
3. **Campos**: pontos de surgimento de monstros com respawn (tempo por ponto), níveis 1–4; a zona nunca "termina".
4. **Portões**: área no mapa que leva à outra zona (portão leste da Cidade ↔ entrada dos Campos); o cliente recebe um `Welcome`/troca de zona com arena e época novas. Posição e zona são gravadas (T-212).
5. **Morte no mundo aberto**: o personagem cai, mostra a opção de renascer e volta na Cidade sem perder equipamento (visão do MVP).
6. Protocolo v+1 se a troca de zona pedir mensagem nova.

## Critérios de aceite

- testes: troca de zona leva a entidade e mantém o estado; respawn de monstros; morte e renascimento na Cidade; Cidade sem dano;
- bots de rede (T-109) andando entre zonas sem quedas;
- `./scripts/check.sh` completo passa.

## Resultado

**Feito** (VPS, 2026-10-28). `./scripts/check.sh` completo passa com o PostgreSQL de teste; os 62 testes de design não mudaram.

**Dados** (sem mapas do Tiled: provisórios, em `data/arenas.json`)
- `arena.city` (40×30): praça no centro, prédios em volta, estrada até o portão leste.
- `arena.vigil_fields` (96×64): portão oeste, Estrada Leste, Fazenda (cerca e casa), Clareira dos Caçadores e Acampamento dos Salteadores (paliçada).
- `data/zones.json` (seção nova `zones`, `ZoneDefinition` no Core, validação no `Game.Data`, schema):
  - `zone.city`: segura, descanso de 5%/s;
  - `zone.vigil_fields`: 6 pontos de surgimento (Ratos nível 1, Lobos 2 e 3, Salteadores 3 e 4), renascimento de 30 a 90 s, corpo por 5 s.
- Portões: leste da Cidade ↔ oeste dos Campos.
- Validação: arena, nascimento, chegadas e surgimentos no chão; portão dentro do mapa e apontando para zona que existe; chegada fora dos portões do destino; zona segura sem monstros; descanso só em zona segura.
- Densidade e equilíbrio dos Campos ficam para a T-215.

**Core** (só o necessário)
- `ZoneDefinition` e afins, `ContentCatalog.Zones` (propriedade `init`: quem monta o catálogo sem zonas continua compilando).
- `Resting` + `RestSystem` (ordem 4100): fração da vida e da Mana máximas por segundo, só vivo, sem evento.

**Servidor** (`src/Game.Server/Zones/`)
- `--world` (ou `MMO_WORLD=true`) liga o mundo aberto. Sem ele, a sala de encontro da T-105/T-212 continua igual. O `Program` usa os dois pela interface `IGameHost`.
- `OpenZone`: `World` próprio (mapa, tick, RNG com seed), grade de interesse e pontos de surgimento. Monstro morto vira corpo, some depois de `corpse` e renasce em volta do ponto (RNG da simulação) depois de `respawn`.
- `WorldServer`:
  - entrada por token (zona e posição gravadas) ou `--dev-login` (nível 1 com progressão, na zona inicial);
  - intenções da entidade da conexão;
  - só zonas com jogador avançam;
  - portão → troca de zona com o mesmo build, itens, vida e Mana, e `Welcome` novo com a arena e época nova;
  - morte → fica caído até `RespawnRequest`, renasce cheio no nascimento da Cidade;
  - envio com interesse e delta por zona;
  - gravação como na T-212, também ao trocar de zona.
- Zona e posição gravadas: `CharacterState.Location` → colunas `zone_id`/`position_x/y`. Morto grava na Cidade. Posição (0, 0) (personagem novo da API) = nascimento da zona.

**Protocolo v7:** mensagem `RespawnRequest`. Não precisou de mensagem nova para a troca de zona: o `Welcome` já tem arena e época, e no mundo aberto `EncounterId` é o id da zona. O cliente atual já faz `TryGetValue` nos encontros e mostra o id se não achar.

**Testes**
- `tests/Game.Server.Tests/WorldTests.cs`:
  - nasce na Cidade com progressão e descanso;
  - anda até o portão leste com intenção de verdade e chega nos Campos com nível, item e vida iguais; volta pelo portão oeste;
  - monstro morto deixa corpo, some e renasce;
  - morte nos Campos espera o pedido e renasce na Cidade com tudo;
  - Cidade sem monstros, ataque em outro jogador não causa dano, descanso regenera;
  - zona vazia parada;
  - personagem com conta gravado com zona e posição e volta lá;
  - morto gravado na Cidade;
  - nome sem `--dev-login` recusado.
- `tests/Game.Data.Tests/ZoneLoaderTests.cs` (carga, segundos→ticks, 9 erros de validação, dados reais) e `tests/Game.Core.Tests/RestTests.cs`.

**Bots de rede:** `--roam` (anda do nascimento ao portão de cada zona); no mundo aberto, morto pede para renascer. Rodada local: `--world --dev-login`, 8 bots por 60 s → 132 trocas de zona, 0 quedas, 0% de perda, tick médio 0,7–0,9 ms (p99 até 10 ms).

**Decisões**
- **Sala de encontro e mundo aberto são modos do processo, não zonas de um mesmo mundo.** A Matriarca como instância acessível pelo mundo fica para a fase 3 (Cripta instanciada).
- **Zona vazia fica parada**, sem gastar CPU: monstros e renascimentos esperam alguém entrar.
- **Ao trocar de zona perdem-se status, recargas e ameaça** (a entidade é recriada); vida e Mana continuam.
- **Monstros não voltam ao ponto** quando perdem o alvo (sem "leash"): ficam onde pararam até morrer.
- **Login de desenvolvimento no mundo aberto** cria personagem nível 1 com progressão, para os portões e a morte funcionarem igual; nada é gravado.
- O Compose continua na sala de encontro (`GAME_WORLD=false`) até o cliente tratar zonas (T-223).

**Fora das áreas da tarefa:** `src/Game.Data/` (seção `zones`), `data/zones.json`, `data/schemas/game-data.schema.json`, `tests/Game.Data.Tests/`, `tests/Game.Core.Tests/RestTests.cs`, `tests/Game.NetBots/` (roam/renascer), `deploy/compose.yml` e `deploy/.env.example` (`GAME_WORLD`).

**Pendências**
- **T-215:** densidade, níveis e tempos dos Campos e teste de design por subárea.
- **T-223:** cliente com `Zones = db.Zones` no `ContentLoader`, transição de zona no `Welcome`, nome da zona e portões na tela, botão "Renascer na Cidade" (`RespawnRequest`). Depois, `GAME_WORLD=true` na VPS.
- **T-055:** trocar os mapas provisórios pelos do Tiled (os ids de arena podem ficar).
- Monstros voltarem ao ponto quando perdem o alvo (leash), se o playtest pedir.
- O primeiro snapshot de uma zona cheia é fragmentado (completo); normal, mas dá para medir na T-230.
