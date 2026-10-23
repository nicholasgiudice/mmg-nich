# T-207 — Servidor e log: pendências da fase 1

- **Ambiente:** VPS
- **Modelo:** Leve
- **Depende de:** —
- **Libera:** T-230
- **Áreas que pode alterar:** `src/Game.Server/`, `src/Game.Core/CombatLog/`, `deploy/`, testes correspondentes

## Contexto

Pendências registradas nos Resultados da fase 1 (T-102, T-110) e no relatório `playtest-online-v0.1.md`.

## Requisitos

1. **Nível de log** do servidor por opção e variável de ambiente (`--log-level`, `MMO_LOG_LEVEL`), ligado no `deploy/compose.yml` (`GAME_LOG_LEVEL`).
2. **Parada limpa com SIGTERM** (`docker compose stop`): tratar o sinal como o Ctrl+C (log "Servidor parado", conexões avisadas).
3. **Log de combate de status** (T-102): formatadores para `StatusApplied`, `StatusRefreshed`, `StatusStacked`, `StatusExpired`, `StatusRemoved` e `StatusResisted`, no padrão dos outros textos em português.

## Critérios de aceite

- testes para os formatadores novos e para a opção de log;
- `./scripts/check.sh` completo passa.

## Resultado

**Feito**

- **Nível de log:**
  - `--log-level` e `MMO_LOG_LEVEL` aceitam `verbose`, `debug`, `info`, `warning`, `error` e `fatal`, além dos apelidos `info`/`warn`, sem diferenciar maiúsculas; a opção vence a variável. Fica em `ServerOptions.LogLevel` e o padrão é `info`.
  - O logger usa um `LoggingLevelSwitch`, ajustado logo depois de ler as opções; assim, erros de opção ainda aparecem no log.
  - `deploy/compose.yml` repassa `GAME_LOG_LEVEL` (padrão `info`).
- **SIGTERM:** `PosixSignalRegistration` para `SIGTERM` cancela o loop do mesmo jeito que o Ctrl+C. O servidor registra "Servidor parado" e, ao descartar o transporte (`NetManager.Stop`), manda a desconexão para os clientes.
- **Log de combate dos status** (`CoreCombatLogFormatters.RegisterStatus`, categoria Status):
  - aplicado: "Lobo Cinzento aplicou Sangramento em Você (2,0 s)". Sem fonte ou aplicado em si mesmo: "X recebeu Sangramento (3,0 s, 2 acúmulos)";
  - renovado: "Sangramento em Você foi renovado (2,0 s)";
  - acumulado: "Sangramento em Você: 3 acúmulos (2,5 s)";
  - expirado: "Sangramento em Você terminou";
  - removido: "Clérigo removeu Sangramento de Você" ou "Sangramento foi removido de Você";
  - resistido: "Lobo Cinzento resistiu a Sangramento de Você".
- **Testes:**
  - `tests/Game.Core.Tests/CombatLogTests.cs`: 5 testes, um por texto/variação dos status;
  - `tests/Game.Server.Tests/TickLoopTests.cs`: opção, variável, prioridade, apelidos e valores inválidos (incluindo número).

**Desvios**

- A linha de `GAME_LOG_LEVEL` no `README.md` da raiz (tabela do deploy) está fora das áreas da tarefa; a mudança foi mínima.
- `src/Game.Server/README.md` também foi atualizado.

**Pendências**

- A parada por SIGTERM não foi verificada num Linux real: no Windows não dá para mandar SIGTERM. Na VPS, conferir com `docker compose -f deploy/compose.yml stop` e `docker compose -f deploy/compose.yml logs game-server`: o log deve terminar em "Servidor parado".
