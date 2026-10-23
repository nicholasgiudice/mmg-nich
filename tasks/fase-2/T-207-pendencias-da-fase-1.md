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

_(preencher ao concluir)_
