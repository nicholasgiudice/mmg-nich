# Relatório de balanceamento v0.10 (T-322): ouro, preços e poções

A T-320 criou o ouro e os consumíveis, e a T-321 as lojas, com números provisórios. Este relatório calibra a economia mínima autorizada para a fase 3 (`playtest-v0.2.md`, P3–P4): ouro só dos monstros, e NPCs com preço fixo.

Base: o modelo de ritmo solo dos Campos da T-215/T-302 (`balance-v0.8.md` §3, `balance-v0.9.md` §2, `FieldGroups.Pace`). Os tempos de luta são medidos no simulador (perfil esperado do nível do grupo, média das 4 classes) e somados a 60 s fora da luta por grupo. O modelo agora calcula também o ouro e a venda por minuto, a partir dos dados (`Economy` em `FieldsDesignTests.cs`). Testes: `tests/Game.Design.Tests/EconomyDesignTests.cs`.

## 1. Números alterados

| Dado | Provisório (T-320/T-321) | Final |
|---|---|---|
| `rules.progression.goldPerMonsterLevel` | 2 | **1** |
| `rules.progression.goldByRank` | normal 1, elite 4, chefe 20 | sem mudança |
| `rules.progression.goldVariance` | ±20% | sem mudança |
| Preço da Poção de Cura Menor (`price`) | 10 | **15** |
| Preço da Poção de Mana Menor | 10 | **15** |
| Poção de Mana: efeito | +40 de Mana, recarga 20 s | sem mudança (§4) |
| `rules.shops.equipmentValueBase` | 10 | **0** |
| `rules.shops.equipmentValuePerLevel` | 5 | **45** |
| `rules.shops.equipmentRarityMultiplier` | 1 / 2 / 4 / 8 | sem mudança |
| `rules.shops.sellRatio` | 0,25 | **0,10** |

Valor de um equipamento: 45 × nível × raridade. O comum do Ferreiro custa 90 (nv2), 135, 180, 225 e 270 (nv6). Nenhuma oferta tem preço próprio: o preço é sempre o valor do item.

Por que essas três alavancas:
- o ouro por morte fixa a renda;
- o preço da poção acerta a meta das poções;
- o valor do equipamento acerta a meta do Ferreiro;
- o `sellRatio` controla a venda sem mexer no preço de compra.

## 2. Ouro por hora nos Campos

| Nível do jogador | Grupos caçados | Minutos no nível | Ouro/h | Venda de tudo o que cai/h |
|---|---|---|---|---|
| 1 | nv1 | 7,4 | 92 | 21,9 |
| 2 | nv2 | 13,9 | 178 | 44,3 |
| 3 | nv3 | 14,7 | 335 | 82,8 |
| 4 | nv4 | 22,8 | 362 | 91,2 |
| 5 | nv4 | 43,0 | 362 | 91,2 |

Do nível 1 ao 5 (~102 min), a média pesada pelo tempo é de **~313 de ouro por hora**. Nos grupos nv4, a renda é limitada pelo renascimento (o mesmo teto da XP; balance-v0.9 §2).

Com o valor provisório (2 por nível, poção a 10), 30 min pagavam 31 poções e o item do Ferreiro custava 3 min de caça.

## 3. Metas

| Meta (T-322) | Medido | Teste |
|---|---|---|
| 30 min de caça nos Campos pagam 8–12 poções de vida | **10,4** (156 de ouro) | `Thirty_minutes_in_the_fields_pay_8_to_12_healing_potions` |
| Equipamento comum do próprio nível no Ferreiro = 20–40 min de caça na área desse nível | nv2 **30,3**, nv3 **24,2**, nv4 **29,8** min | `A_common_item_of_your_level_costs_20_to_40_minutes_of_hunting` |
| Vender tudo o que cai < 30% do ouro direto | **25,1%** | `Selling_every_drop_yields_less_than_30_percent_of_the_monster_gold` |
| Ouro por hora cresce com o nível dos grupos | 92 → 178 → 335 → 362 | `Gold_per_hour_grows_with_the_hunted_level` |

- **Ferreiro:** só os níveis 2 a 4 têm área própria nos Campos. Os níveis 5 e 6 do Ferreiro seguem a mesma fórmula, mas a área deles (Floresta, T-342) ainda não entra no modelo de ritmo.
- **Venda:** o valor esperado de um item que cai considera os pesos de raridade da tabela (70/25/5) e o nível do monstro. Com `sellRatio` 0,25, a venda daria ~63% do ouro direto; 0,10 deixa 25%, com folga para a queda subir para 5–6% (balance-v0.9 §3) sem passar de 30%.
- **Preço de um nível acima:** um item comum um nível acima do jogador custa um pouco mais de 40 min de caça (nv3 na área nv2: 135 ÷ 178/h ≈ 45 min). É a "compra de reforço": cara, mas possível.

## 4. Poção de Mana

Pergunta: beber 1 poção de Mana por luta deixa o perfil fraco (3 níveis abaixo) vencer mais de 50% (`design/progression-v0.1.md` §8)?

Medição: nos solos do Mago (Quatro Ratos) e do Clérigo (Guarda Esquelético), nos níveis 5 e 9, a classe começa com 1 poção de Mana a mais, e o bot bebe com **metade** da Mana. É o caso mais favorável à poção. Para isso foi preciso a opção nova `manaPotionBelow` no `Game.Bots`, desligada por padrão (§6).

| Solo | Perfil fraco sem poção | Com 1 poção de Mana | Poções bebidas/luta |
|---|---|---|---|
| Mago, Quatro Ratos nv5 | 0% | **0%** | 1,00 |
| Mago, Quatro Ratos nv9 | 6% | **6%** | 1,00 |
| Clérigo, Guarda nv5 | 100% (exceção §8) | 100%, 61,4 s | 0,00 |
| Clérigo, Guarda nv9 | 100% (exceção §8) | 100%, 60,3 s | 0,00 |

- **Mago fraco:** perde por dano recebido, não por falta de Mana. A poção (+40, cerca de um terço da Mana do Mago nesses níveis) não muda a vitória.
- **Clérigo:** contra o Guarda, a Mana dele não chega à metade, então ele nem bebe. A meta dele continua a da exceção da §8: demorar pelo menos 1,3× o perfil esperado (vale).
- **Número final:** +40 de Mana, recarga 20 s, preço 15 (igual à poção de vida, para a escolha ser pelo efeito, não pelo preço).
- Teste: `One_mana_potion_per_fight_does_not_carry_the_weak_profile`.

## 5. Conferências

- Os outros testes de design não mudaram. O ouro não entra nas lutas, e a opção nova do bot vem desligada.
- `./scripts/check.sh` completo passa.

## 6. Desvios e pendências

- **Fora das áreas:** `src/Game.Bots/ConfigurableBot.cs` ganhou `manaPotionBelow` (fração do recurso; padrão 0 = desligado), espelho do `potionBelow`, e `BotOptions.cs` cita a opção. Sem isso, o simulador não sabe beber a poção de Mana, e o requisito 3 não teria como ser medido.
- **Teto de renda:** acima do nível 4, nos Campos, a renda fica no teto do renascimento. Quando a Floresta (nv5–7) entrar no modelo, conferir de novo os níveis 5 e 6 do Ferreiro.
- **Destino do ouro:** hoje o ouro só sai pelas lojas. Redistribuir pontos pago (`progression-v0.1.md` §10) pode virar outro destino.
