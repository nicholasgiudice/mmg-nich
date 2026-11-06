# Relatório de balanceamento v0.9 (T-302): ritmo de XP e queda de itens

Resposta ao playtest da fase 2 (`playtest-v0.2.md`, "Ajustes de balanceamento"): a progressão estava rápida e caía item demais. Base: o modelo de ritmo da T-215 (`balance-v0.8.md` §3, `FieldGroups.Pace`), com os tempos de luta medidos no simulador (100 seeds por grupo e classe, perfil esperado do nível do grupo).

## 1. Números alterados

| Dado | Antes | Depois |
|---|---|---|
| `rules.progression.xpCurveBase` | 50 | **68** (+36%) |
| `rules.progression.xpCurveExponent` | 1,8 | 1,8 (sem mudança) |
| Queda, monstro normal (`rules.items.drops.normal.chance`) | 8% | **4%** |
| Queda, elite | 40% | **25%** |
| Queda, chefe | 100%, 2 itens | 100%, 2 itens (sem mudança) |

Só a base da curva mudou: o pedido era "um pouco mais de XP por nível", sem mudar a forma da curva. Como o modelo é linear na XP exigida, todos os tempos sobem na mesma proporção. Os +30% sugeridos dariam o nível 4 em 34,3 min, abaixo da meta de 35–40; 68 é a menor base redonda que cumpre as duas metas (nível 4 e nível 5) com folga.

## 2. Ritmo solo nos Campos (modelo da T-215)

XP por minuto e tempo de luta não mudam; só a XP exigida.

| Nível | XP para o próximo (antes → depois) | XP/min | Minutos no nível (antes → depois) | Acumulado (antes → depois) |
|---|---|---|---|---|
| 1 | 50 → 68 | 9,2 | 5,4 → 7,4 | 5 → 7 |
| 2 | 174 → 237 | 17,1 | 10,2 → 13,9 | 16 → 21 |
| 3 | 361 → 491 | 33,5 | 10,8 → 14,7 | 26 → **36** |
| 4 | 606 → 825 | 36,2 | 16,7 → 22,8 | 43 → **59** |
| 5 | 906 → 1 232 | 28,7 | 31,6 → 43,0 | 75 → 102 |

- **Nível 4 em 36 min** (meta 35–40) e **nível 5 em 59 min** (meta 55–65). O nível 6 sai dos Campos: 102 min.
- Até o nível 15: 43 325 XP (eram 31 856), cerca de 775 monstros normais do próprio nível, na ordem de 8–9 horas (eram 6–7). A tabela completa está em `design/progression-v0.1.md` §2.
- Teste: `FieldsDesignTests.Solo_player_reaches_level_4_in_35_to_40_minutes_and_level_5_in_55_to_65`.

## 3. Itens por hora

Mesmo modelo: monstros mortos por minuto × chance da categoria. Os Campos só têm monstros normais.

| Nível do jogador | Monstros/h | Itens/h com 8% (antes) | Itens/h com 4% (depois) |
|---|---|---|---|
| 1 | ~92 | 7,4 | 3,7 |
| 2 | ~112 | 8,9 | 4,5 |
| 3 | ~112 | 8,9 | 4,5 |
| 4–5 | ~90 | 7,2 | 3,6 |
| **Média, nível 1 → 5** | ~100 | **8,0 (1 item a cada 7,4 min)** | **4,0 (1 item a cada 14,9 min)** |

**Desvio da tarefa.** A tarefa pedia chance de 3–4% no normal **e** um item a cada 6–10 min. As duas metas não cabem juntas no modelo: com ~100 monstros por hora, 6–10 min por item exige 6–10% de chance, e a taxa de antes (8%) já dava 7,4 min, que foi justamente o que o designer achou alto. Segui a faixa de chance (a decisão explícita do designer) e escolhi o topo dela, **4%**, o mais perto da meta de tempo: **1 item a cada ~15 min** solo. Com 3% seriam ~20 min.

Possíveis motivos da diferença: o playtest foi em dupla e o jogador real deve matar mais rápido que o modelo (o intervalo de 60 s fora da luta é premissa, `balance-v0.8` §4). Com 30 s de intervalo, 4% daria cerca de 1 item a cada 10 min. Se o próximo playtest achar a queda rara demais, o ajuste é subir a chance para 5–6%, sem mexer no resto.

Elite: **25%**, topo da faixa pedida (20–25%), pela mesma razão. Os Campos não têm elites, então não há medida por hora; a Floresta (fase 3) terá. A razão elite/normal fica em ~6 (era 5), coerente com a XP de elite (5× a do normal) e o tempo maior para matá-lo.

Teste: `FieldsDesignTests.Solo_hunting_in_the_fields_drops_an_item_every_12_to_18_minutes`; `ProgressionContentTests` confere as chances novas.

## 4. Equipamento dos perfis esperados

Os testes de design montam o perfil esperado com o equipamento comum do nível da área (`CharacterProfile.Expected`), não com o que caiu, e não dependem da curva de XP. Nenhum resultado dos outros testes de design mudou (os lotes são os mesmos, com as mesmas seeds). Com menos queda, o jogador real fica mais tempo com o equipamento inicial ou de vendedor: isso só reforça que o perfil "esperado" (comum do nível) é o caso base, e não um caso otimista. Conferir no playtest da fase 3 se o jogador chega à Floresta (nível 5) com equipamento perto do nível.
