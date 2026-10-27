# T-212 — Servidor: entra com token, carrega e salva o personagem

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-203, T-204, T-210, T-211
- **Libera:** T-214, T-221, T-222, T-230
- **Áreas que pode alterar:** `src/Game.Server/`, `src/Game.Protocol/`, `src/Game.Persistence/`, `tests/Game.Server.Tests/`, `tests/Game.NetBots/` (entrar com token de teste)

## Contexto

Hoje o servidor aceita um nome (`Hello.DevName`) e cria um personagem nível 1 a cada entrada. Com contas e progressão, ele passa a carregar o personagem salvo e gravar o que mudou (XP, nível, pontos, NH, inventário, equipamento, posição).

## Requisitos

1. `Hello` leva o **token de jogo** (T-211) em vez do nome; o servidor valida assinatura, expiração e uso único. Modo de desenvolvimento por opção (`--dev-login`) mantém o login só com nome para testes locais e bots.
2. Ao entrar: carrega o personagem do banco e cria a entidade com nível, atributos, NH, equipamento e inventário. Um personagem só pode estar conectado uma vez.
3. **Gravação assíncrona e transacional fora do loop de 20 Hz**: ao sair, a cada N minutos e em eventos importantes (subir de nível, item raro). O loop nunca espera o banco; falha de gravação é registrada e repetida.
4. Ao parar o servidor (SIGTERM, T-207), grava todos os conectados antes de sair.
5. Protocolo v+1; bots de rede da T-109 continuam funcionando com `--dev-login`.

## Critérios de aceite

- teste: entra, ganha XP e item, sai, entra de novo e está tudo lá;
- teste: o tick não espera o banco (gravação lenta simulada não aumenta o tempo de tick);
- `./scripts/check.sh` completo passa.

## Resultado

**Feito** (VPS, 2026-10-27). `./scripts/check.sh` completo passa com o PostgreSQL de teste (33 testes do servidor, 1 deles contra o banco real; sem `MMO_TEST_DB` esse é pulado).

**Protocolo v6:** `Hello` ganhou `Ticket` (último campo, opcional: `new Hello(...)` do cliente continua compilando) e `RejectReason` ganhou `Unauthorized`, `AlreadyConnected` e `Unavailable`.

**Entrada com token** (`ZoneRoom`):
- O token é validado na hora, na thread do loop (HMAC, sem rede), pelo `GameTicketReplayGuard` da T-211: assinatura, servidor, expiração e uso único.
- O personagem é carregado em segundo plano e entra no primeiro tick depois de chegar. Nasce na vaga com o build validado (`Leveling.Validate`), o equipamento e o inventário salvos, com vida e Mana cheias.
- Progressão `{}` (personagem novo da API) = `Leveling.SpawnCharacter` sem build, com o equipamento inicial da classe.
- Um personagem só entra uma vez: conectado ou carregando → `AlreadyConnected`. Saiu: a entidade some na hora (sem vaga reservada) e voltar exige token novo.
- `--dev-login` (ou `MMO_DEV_LOGIN=true`) mantém a entrada só com nome. Os dois modos convivem na mesma sala. Sem banco e sem `--dev-login`, o servidor não sobe.

**Gravação** (`src/Game.Server/Persistence/`):
- `CharacterPersistence`: um trabalhador e uma fila que guarda só o estado mais novo de cada personagem. O loop só captura o estado (imutável) e enfileira.
- Cada gravação é um UPDATE com checagem de versão (`GameRepository.SaveCharacterAsync`, uma transação).
- Exceção do banco: registra e repete a cada 5 s, sem perder estado mais novo. Conflito ou personagem apagado: registra erro e descarta.
- Enquanto há estado não gravado, carregar o personagem devolve esse estado, e não o do banco.
- Gatilhos: saída, a cada `--save-interval` (120 s), `LevelUp`, `ItemPickedUp` raro ou épico, reinício da sala (o personagem renasce com o capturado) e parada do servidor.
- Parada (SIGTERM/Ctrl+C/`quit`): `SaveAll` e espera a fila esvaziar por até 20 s. O Compose ganhou `stop_grace_period: 30s`.
- Formato do documento (`CharacterDocument`, versão 1): `allocated`, `skillRanks`, `equipment` (por espaço) e `inventory` (lista esparsa `{slot, item}`). Nível e XP ficam nas colunas. Na carga, item que o conteúdo não aceita mais é descartado com aviso no log.

**Testes** (`tests/Game.Server.Tests/AccountTests.cs`, banco em memória que pode atrasar ou falhar):
- entra, ganha XP (sobe de nível), gasta ponto, pega anel raro com afixos, sai; outro processo carrega do "banco" e está tudo lá;
- banco travado: 40 ticks com gravação pendente, o mais lento abaixo de 200 ms; carregamento travado também não segura o loop (o jogador só entra quando chega);
- falhas repetidas e gravadas com o estado mais novo; nível e item raro gravam na hora; gravação periódica;
- reinício da sala mantém o personagem;
- token inválido, de outro servidor ou reusado, personagem já conectado, de outra conta ou corrompido, nome sem `--dev-login`; voltar com token novo depois de sair;
- dev-login e contas lado a lado; documento ida e volta, `{}` = novo;
- `PostgresAccountTests`: o mesmo fluxo com `GameRepository` contra o PostgreSQL real.

**Verificado na VPS:**
- `docker compose up -d --build game-server` subiu com contas e `--dev-login`.
- Conta e personagem criados pela API; o bot de rede entrou com o token (`--ticket`, opção nova do `Game.NetBots`), jogou 25 s, saiu, e a linha ficou com `progression_version` 1 e o documento.
- Outro personagem conectado durante `docker compose stop game-server`: o log mostrou "Personagens gravados antes de parar (1)" e o `updated_at` mudou.
- 4 bots sem token (dev-login) jogaram sem quedas.
- Contas de teste apagadas; servidor religado.

**Decisões**
- **Zona e posição não são gravadas**: a sala ainda é o encontro da Matriarca, com vagas fixas, e a posição dela não vale na `zone.city`. Fica para a T-214.
- **Vaga do encontro**: o personagem com conta usa a mesma regra de vaga (da classe dele; senão a primeira livre, que troca de classe). Sala cheia → `Full`.
- Morte no encontro não perde nada (só vida, que não é gravada).
- **Compose:** `game-server` recebe banco, chave e id de servidor e roda com `MMO_DEV_LOGIN=true` (`GAME_DEV_LOGIN` no `.env`), porque o cliente ainda entra só com nome até a T-220.

**Fora das áreas da tarefa:** `deploy/compose.yml` e `deploy/.env.example` (variáveis do servidor, `depends_on: migrate`, prazo de parada); `tests/Game.NetBots/load.sh` e `README.md` (passam `--dev-login`).

**Pendências**
- **T-214:** gravar zona e posição (`Character.ZoneId`/`PositionX/Y`; o `SaveCharacterAsync` já grava se a entidade tiver os valores) e tirar os personagens com conta das vagas do encontro.
- **T-220/T-223:** mandar `Hello.Ticket` e mostrar as recusas novas; depois, `GAME_DEV_LOGIN=false` na VPS.
- Uso único vale por processo. Com mais de um processo de servidor, ou depois de um reinício dentro dos 60 s do token, o mesmo token poderia entrar de novo. Hoje não importa: o personagem continua entrando uma vez só.
