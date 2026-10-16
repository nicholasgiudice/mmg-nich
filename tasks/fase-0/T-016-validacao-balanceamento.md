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

_(preencher ao concluir)_
