# T-102 — Log de combate a partir de eventos e snapshots (sem `World`)

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** fase 0 concluída (T-025)
- **Libera:** T-107
- **Áreas que pode alterar:** `src/Game.Core/CombatLog/`, `tests/Game.Core.Tests/`, `client/scripts/Authority/` (só para acompanhar a API)

## Contexto

O log de combate (T-013) é formatado pelo `CombatLogRecorder`, que precisa do `World` para resolver nomes e o conteúdo (`CombatLogContext`). No cliente de rede (T-107) não existe `World`: o cliente recebe eventos e snapshots. Hoje a `LocalAuthority` grava o log e o expõe por `ICombatLogSource` (T-022).

## Requisitos

1. `CombatLogContext` passa a depender de uma interface pequena (ex.: `ICombatLogLookup`: nome de entidade por id, definições de conteúdo, tick), com uma implementação sobre `World` (servidor/local) e outra sobre `WorldSnapshot` + `ContentCatalog` (cliente de rede).
2. `CombatLogRecorder` aceita eventos + lookup (sem `World`), mantendo o mesmo texto das linhas e o resumo de morte.
3. Teste: o mesmo encontro gravado pelo caminho `World` e pelo caminho "eventos + snapshots" produz linhas idênticas.
4. `LocalAuthority` continua funcionando (sem mudança visível no HUD).

## Critérios de aceite

- nenhuma linha do log muda de texto;
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
