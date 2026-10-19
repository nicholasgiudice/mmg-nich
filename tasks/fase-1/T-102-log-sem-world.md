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

Concluída em 2026-10-18 no Windows (a tarefa é de VPS; feita aqui a pedido do designer — só `dotnet` e testes).

**Feito** (`src/Game.Core/CombatLog/`):
1. `ICombatLogLookup` (`CombatLogLookup.cs`): conteúdo, tick, nome de uma unidade e unidades com nome presentes. Duas implementações: `WorldLogLookup` (mundo — cliente local e servidor) e `SnapshotLogLookup` (`WorldSnapshot` + `ContentCatalog` — cliente de rede).
2. `CombatLogContext` depende do lookup (propriedades `Lookup` e `Content`; a propriedade `World` saiu). O único formatador que lia `c.World.Content` passou a usar `c.Content`.
3. `CombatLogRecorder.Record(events, lookup)` grava sem `World`; `Record(world)` continua existindo como atalho (`WorldLogLookup` + `LastTickEvents`), então `LocalAuthority`, testes e simulador não mudaram. A proteção contra gravar o mesmo tick duas vezes usa `lookup.Tick`.
4. Nenhuma linha muda de texto.

**Testes** (`tests/Game.Core.Tests/CombatLogLookupTests.cs`):
- Matriarca com dados reais, 1 200 ticks, os 4 jogadores usando ataque básico e todas as habilidades/poções na Matriarca: o log gravado pelo `World` e o gravado só com **eventos + snapshot replicado + conteúdo** têm exatamente as mesmas linhas (tick, categoria, texto) e os mesmos resumos de morte (vítima, linhas, dano por fonte).
- Gravar o mesmo tick duas vezes pelo caminho novo não duplica linhas.
- `RealContent.cs`: carregamento do conteúdo real compartilhado pelos testes (antes estava dentro dos testes da T-101).

**Achado (pendência, fora do escopo):** `StatusApplied`, `StatusRefreshed`, `StatusStacked`, `StatusExpired` e `StatusRemoved` **não têm formatador** no log da T-013 — em "Status" só aparecem escudos. Veneno, congelamento, marca etc. nunca aparecem no log (o HUD mostra os ícones, mas o filtro "Status" do log fica quase vazio). Como esta tarefa exige não mudar o texto do log, ficou para uma tarefa própria.
