# Relatório de balanceamento v0.27 (T-513): itens, loja do Forte, poções e quedas do 16 ao 27

Tarefa de dados: nenhum monstro, habilidade nem regra do Core mudou. Testes: `tests/Game.Design.Tests/SouthEconomyDesignTests.cs` (7 testes; o tempo de luta vem dos lotes de 100 seeds de `MarshDesignTests` e `RidgeDesignTests`). Os relatórios v0.23 (T-505), v0.24 (T-510), v0.25 (T-511) e v0.26 (T-512) são de tarefas da mesma fase.

## 1. Poções: as Maiores de hoje não bastam do 16 ao 27

Fração da vida (cura) e da Mana máximas que a poção devolve, no perfil esperado de cada nível (`LevelCurve.Measure`, atributos sugeridos, equipamento comum do nível). Cura Maior = 180 + 5 × VIT; Mana Maior = +120.

| | Cura Maior (pior classe) | Mana Maior (Mago) |
|---|---|---|
| nível 16 a 27, pior caso | **44%** (Guerreiro, nível 27) | **31%** (nível 27) |
| meta | ≥ 50% | ≥ 50% |

A Mana Maior já devolve menos de metade da Mana do Mago no nível 16 (44%) e chega a 31% no 27. A Cura Maior cai abaixo de metade na vida do Guerreiro nos níveis mais altos da faixa (44% no 27); no Arqueiro, no Mago e no Clérigo ela ainda é ≥ 50%. As duas **não cumprem** a meta; as Superiores foram criadas:

| Item | Id | Efeito | Recarga | Pilha | Preço | Pior caso do 16 ao 27 |
|---|---|---|---|---|---|---|
| Poção de Cura Superior | `consumable.superior_healing_potion` | cura 300 + 7 × VIT | 12 s | 20 | 125 | 66% (Guerreiro nv27); Clérigo 77%; Arqueiro e Mago > 98% |
| Poção de Mana Superior | `consumable.superior_mana_potion` | +300 de Mana | 12 s | 20 | 125 | 77% (Mago nv27); nv16 111% |

A recarga é a mesma das outras poções (12 s). Itens diferentes têm recarga separada (a regra de hoje), então uma poção de cura e uma de Mana podem sair em sequência.

## 2. Ouro por hora e o preço das poções

Modelo da T-322 estendido: o jogador limpa o grupo típico (os encontros de `data/encounters.json`), em solo com o perfil esperado do nível do grupo; tempo de luta = média das 4 classes (no Brejo as 4 classes base; na Serra o Guardião, o Atirador de Elite, o Piromante e o Sacerdote), mais 60 s de deslocamento e descanso (`OverheadSeconds`). **Sem o limite de renascimento**: os mapas do sul ainda não têm os pontos de nascimento (T-530, T-532); a T-550 confere no playtest. Ouro por morte = nível × categoria (§ em `design/itens.md`); só os grupos normais (sem a Bruxa, o Golem e o Capataz).

| Zona | Níveis | Grupos medidos | Ciclo | Ouro/grupo | Ouro/hora |
|---|---|---|---|---|---|
| Brejo | 16 a 20 | Afogados, Homens-lagarto, Serpentes, mistos | 88 s | 38 | **1.544** |
| Serra e Minas | 21 a 27 | Mineiros, Lobos, Morcegos | 89 s | 101 | **4.098** |

A meta do primeiro mapa (30 min de caça pagam 8–12 poções, ou **16–24 por hora**) não cabe com um preço só: a Serra rende 2,6× o ouro do Brejo (grupos de 4 a 6 monstros, contra 2 a 3 no Brejo). Com o preço de **125**, uma hora de caça paga **12,4** Superiores no Brejo e **32,8** na Serra. A janela do teste é de **10 a 40 por hora** (a média geométrica dos dois, 125 = √(1.544 × 4.098) / 20, deixa os dois lados a ~1,6× da meta). O designer pode trocar o preço (`items.json`) ou subir a janela para a faixa do primeiro mapa se achar a Serra rica demais.

## 3. Equipamento do Forte

Equipamento comum do nível no Intendente: `45 × nível` (regra de hoje, sem `price` próprio) = **720 (nv16)** e **945 (nv21)**.

| Nível | Ouro/hora do nível | Minutos de caça por peça |
|---|---|---|
| 16 | 1.544 | **28,0** |
| 21 | 4.098 | **13,8** |

A meta do primeiro mapa é 20–40 min. No nível 16 cumpre. No 21 a peça sai em 14 min: o preço cresce linear com o nível e o ouro por hora cresce mais (grupos maiores). O teste usa 10–40 min. Não foi mexido no preço (`equipmentValuePerLevel` é global): se o designer achar a Serra barata para equipamento, a saída é a oferta do nível 21 com `price` próprio (sobre 1.700–3.300 ouro) ou subir `equipmentValuePerLevel` junto com os preços do Ferreiro (T-550).

## 4. Quedas e ouro dos monstros novos

Quedas e ouro são **por categoria**, não por monstro (`rules.items.drops`; `rules.progression.goldByRank`), e os monstros novos usam a categoria que já têm (normal, elite, chefe). Não foi preciso mudar tabela nenhuma; o teste confere que os 9 monstros do 16 ao 27 têm queda e ouro e que a Bruxa e o Capataz usam a tabela de chefe.

| Monstro | Categoria | Ouro (média) | Queda |
|---|---|---|---|
| Afogado nv16, Homem-lagarto nv17, Serpente do Charco nv18 | normal | 16, 17, 18 | 4% de 1 item (70/25/5/0) |
| Mineiro Possuído nv21, Lobo da Serra nv22, Morcego nv22 | normal | 21, 22, 22 | idem |
| Golem de Pedra nv25 | elite | 100 | 25% de 1 item (40/45/15/0) |
| Bruxa do Brejo nv20 | chefe | 400 (dividido pelo grupo) | 100% de 2 itens (0/60/35/5), o primeiro ao menos incomum |
| Capataz Forjado nv27 | chefe | 540 (dividido pelo grupo) | idem |

Os itens caem no nível do monstro (16 a 27), com o item da tabela inteira (as 7 armas, as 12 armaduras e os 2 acessórios). Os acessórios (Anel e Amuleto) só caem; nenhuma loja vende (como hoje).

## 5. Loja `shop.pass_fort` (o Intendente do Forte do Passo)

- Poções: Menor, Maior e Superior de cura e de Mana (6 ofertas).
- Equipamento comum: as 7 armas e as 12 armaduras, nos níveis **16** e **21** (38 ofertas). Cobre as 4 classes (arma e as 4 peças de armadura de cada uma, pelo teste).

O NPC e o mapa do Forte são da T-531 (`shop.pass_fort` precisa ser ligado ao Intendente lá). O Ferreiro e o Templo da Cidade continuam iguais.

## 6. Pendências

- **Playtest (T-550):** confirmar o ouro por hora com os pontos de nascimento reais dos mapas e o preço das Superiores.
- **Acessórios sem loja:** o Anel e o Amuleto só caem; um jogador do 16 ao 27 só os acha em queda.
- **Nomes das habilidades de item** (`data/abilities_items.json`): as quatro já existentes estão com a acentuação duplamente codificada ("PoÃ§Ã£o"); as novas estão certas. O nome da habilidade não aparece para o jogador (o nome do item vem de `items.json`).
