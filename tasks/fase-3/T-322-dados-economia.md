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

Concluída em 2026-11-09. Relatório: `tasks/reports/balance-v0.10.md`.

**Números finais:**

- `data/rules.json`: `goldPerMonsterLevel` 2 → **1**. `goldByRank` ficou como estava (normal 1, elite 4, chefe 20), e `goldVariance` também (±20%).
- `data/items.json`: Poção de Cura Menor e Poção de Mana Menor, `price` 10 → **15**.
- `data/abilities_items.json`: a Poção de Mana fica com **+40 de Mana**, recarga 20 s (sem mudança; medida no §4 do relatório).
- `data/shops.json` (`rules.shops`): `equipmentValueBase` 10 → **0**, `equipmentValuePerLevel` 5 → **45** (valor = 45 × nível × raridade 1/2/4/8; o comum do Ferreiro custa 90 no nv2 e 270 no nv6), `sellRatio` 0,25 → **0,10**.

**Metas** (modelo de ritmo solo dos Campos):

| Meta | Resultado |
|---|---|
| 30 min pagam 8–12 poções de vida | 10,4 |
| Item comum do próprio nível no Ferreiro = 20–40 min de caça | nv2 30,3 / nv3 24,2 / nv4 29,8 min |
| Vender tudo o que cai < 30% do ouro direto | 25,1% |
| 1 poção de Mana por luta não leva o perfil fraco acima de 50% | Mago 0% (nv5) e 6% (nv9), iguais aos de sem poção; o Clérigo não chega a beber |

Renda nos Campos: ~313 de ouro por hora do nível 1 ao 5 (92/h nos grupos nv1, 362/h nos nv4).

**Testes:**

- `FieldGroups.Pace` passou a calcular o ouro por minuto e a venda por minuto a partir dos dados (classe `Economy`), sem números repetidos no teste. Os tempos de luta ficam em cache para os testes de ritmo e de economia.
- Novo `EconomyDesignTests`: as três metas, o ouro por hora crescendo com o nível dos grupos e a poção de Mana nos solos do Mago e do Clérigo (nv5 e nv9).
- Nenhum teste de `Game.Data.Tests` precisou mudar.
- `design/itens.md`:
  - consumíveis como itens;
  - seção nova "Economia" (ouro, lojas, preços, venda e metas);
  - tabela de queda corrigida para 4% e 25% (estava com os números de antes da T-302).
- `./scripts/check.sh` completo passa.

**Desvios:**

- `src/Game.Bots/ConfigurableBot.cs` e `BotOptions.cs` estão fora das áreas listadas. Entrou a opção `manaPotionBelow` (fração do recurso; padrão 0 = desligado), espelho do `potionBelow`. Sem ela, o simulador não bebe a poção de Mana e o requisito 3 não teria como ser medido. Desligada por padrão, ela não muda nenhum outro resultado.
- A medição da poção de Mana usa limiar de 50% da Mana, o caso mais favorável à poção. Com 30%, o Clérigo também não bebe, e o Mago dá o mesmo resultado.

**Pendências:**

- Os níveis 5 e 6 do Ferreiro não têm área nos Campos. Conferir a meta de 20–40 min quando a Floresta (T-342) entrar no modelo de ritmo.
- O ouro só tem as lojas como destino. A redistribuição de pontos paga (progression §10) pode virar outro.
