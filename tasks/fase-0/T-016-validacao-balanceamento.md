# T-016 — Testes de design e balanceamento inicial

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** T-014, T-015
- **Libera:** T-025
- **Áreas que pode alterar:** `data/` (números), `tests/Game.Design.Tests/` (novo), `tasks/reports/`

## Contexto

Leia as seções "Falha de design" de cada encontro em `design/combat-encounters-v0.1.md` e a seção 6 de `design/class-map-validation-v0.1.md`.

## Objetivo

Transformar critérios de design em testes automatizados e ajustar os números de `data/` até passarem.

## Requisitos

1. Projeto `tests/Game.Design.Tests` (xUnit) usando `Game.Bots` e o runner do simulador. Cada teste roda N seeds (padrão 100) e verifica limites.
2. Testes iniciais (limites ajustáveis; registrar os finais):

   | Encontro | Verificação |
   |---|---|
   | Guerreiro × 3 Lobos | `rotation` vence ≥ 90%; `taunt_and_basic` vence ≤ 50% **ou** leva ≥ 1,5× mais tempo |
   | Arqueiro × Lobo + Salteador | `kite` vence ≥ 85%; `idle` vence ≤ 40% |
   | Mago × 4 Ratos | `rotation` vence ≥ 85%; `explosion_spam` fica sem Mana antes do fim na maioria das execuções |
   | Clérigo × Guarda | `rotation` vence ≥ 85%; tempo médio 1,2–1,6× a média das outras classes nos seus encontros; `heal_only` não vence |
   | Grupo × Matriarca | grupo equilibrado que se move vence ≥ 70%; o mesmo grupo parado vence ≤ 20%; grupo sem Clérigo vence entre 30% e 70% |

3. Ajustar números em `data/` (não código) até os testes passarem. Se for impossível só com números, registrar o problema e propor mudança de regra — não alterar o Core nesta tarefa.
4. Relatório `tasks/reports/balance-v0.1.md` com tabelas do simulador antes/depois e as mudanças feitas.

## Critérios de aceite

- `dotnet test tests/Game.Design.Tests` passa;
- relatório criado;
- tempo total dos testes de design < 5 minutos na VPS (reduzir N se necessário e registrar);
- `./scripts/check.sh` passa.

## Resultado

**Feito.** Projeto `tests/Game.Design.Tests` (13 testes, no `MmoRpg.sln`) com os cinco encontros da tabela, mais um teste da fonte de perigos. N = 100 seeds por verificação (`DESIGN_RUNS` altera); tempo total cerca de 1 min 30 s na VPS (limite 5 min). Limites finais, antes/depois, mudanças de números e problemas de regra: `tasks/reports/balance-v0.1.md`.

**Áreas ampliadas (autorizado).** `src/Game.Bots/TelegraphHazardSource.cs` (nova): `IHazardSource` com estado que lê `TelegraphStarted/Resolved` e `AreaSpawned/Expired` (eventos do tick anterior, consumidos uma vez por tick) e só conta perigos de fontes não jogadoras. Uma instância por mundo (o cliente usa a mesma para bots companheiros). Áreas persistentes só entram com `includeAreas: true` (padrão `false`): como perigo elas fazem o bot oscilar na borda e perder dano. `tests/Game.Sim/Program.cs`: a CLI passa a ligar `TelegraphHazardSource` (`+dodge` funciona na CLI).

**Adaptações.** `taunt_and_basic` = Golpe Circular (ameaça) + básico (`class.warrior:taunt=warrior.whirlwind`), pois o Guerreiro não tem Provocação. Arqueiro "parado" = `idle` e `rotation`, os dois <= 40% (`idle` sozinho é fraco). Grupo da Matriarca "que se move" = todos `kite+dodge`; "parado" = `idle` e `rotation`; grupo sem Clérigo = encontro derivado em memória no teste.

**Números alterados em `data/`** (detalhe no relatório): Lobo mordida 6->7; Rato mordida 5->4; Matriarca: dano de mordidas/investida x0,5, vida 4500->3825; Aranhas x0,5; veneno 3->1 por tick; poça de veneno 4->1 por tick. Nada em `src/Game.Core`.

**Problemas de regra (relatório §4).** Sem sustentação fora do Clérigo (a Matriarca teve de ser enfraquecida muito para o grupo sem Clérigo vencer 30-70%; reverter quando houver consumíveis); `+dodge` oscila em áreas persistentes; sem Provocação; resultados quase binários; lacunas do Core da T-014; Mago e `explosion_spam` rápidos demais.

**Pendências.** Nenhuma bloqueante; nenhum teste ficou com `Skip`.
