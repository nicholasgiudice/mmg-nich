# T-322 — Dados: ouro, preços e poções

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** T-302, T-321
- **Libera:** —
- **Áreas que pode alterar:** `data/rules.json`, `data/items.json`, `data/abilities_items.json`, `data/shops.json`, `tests/Game.Design.Tests/`, `tests/Game.Data.Tests/`, `tasks/reports/`, `design/itens.md`

## Contexto

A T-320 criou ouro e consumíveis. A T-321 criou lojas com preços provisórios. Esta tarefa calibra os números da economia mínima, que foi autorizada explicitamente para a fase 3 (`tasks/reports/playtest-v0.2.md`, P3–P4): só ouro de monstros e NPCs com preço fixo.

## Requisitos

1. **Metas** (modelo de ritmo da T-215/T-302, caça solo):
   - nos Campos, 30 min de caça pagam **8–12 poções de vida**;
   - comprar um equipamento comum do próprio nível no Ferreiro custa **20–40 min** de caça na área desse nível, para que o equipamento que cai continue valendo;
   - vender tudo o que cai rende **menos de 30%** do ouro direto dos monstros, para a venda não virar a principal fonte de ouro.
2. Definir `goldPerMonsterLevel` e os multiplicadores por categoria, o preço das poções, a fórmula de valor do equipamento (nível × raridade) e o `sellRatio`.
3. **Poção de Mana:** número final, de modo que ela não resolva sozinha a falta de Mana. Medir com o simulador nos testes de design de solo (Mago e Clérigo): usar 1 poção de Mana por luta não pode elevar a vitória do perfil "fraco" acima da meta de 50% (`design/progression-v0.1.md` §8).
4. Teste de design novo: ouro por hora e o custo das metas acima, calculados a partir dos dados (sem números repetidos no teste).
5. Relatório `tasks/reports/balance-v0.10.md` e a seção de economia em `design/itens.md`.

## Critérios de aceite

- metas acima verificadas por teste;
- `design/itens.md` com ouro, preços e poções;
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
