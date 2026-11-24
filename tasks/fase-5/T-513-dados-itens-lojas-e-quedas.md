# T-513 — Dados: itens, loja do Forte, poções e quedas do 16 ao 27

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** 510, 511, 512
- **Libera:** T-531, T-550
- **Áreas que pode alterar:** `data/items.json`, `data/shops.json`, `data/monsters.json` (só tabelas de queda e ouro), `data/rules.json` (só itens e economia), `design/itens.md`, `tests/Game.Design.Tests/EconomyDesignTests.cs` e testes de design de itens, `tasks/reports/`

## Contexto

Os itens escalam pelo nível (a loja oferece o item com um `level`, e a queda sai no nível do monstro). Hoje a loja do Ferreiro e o Templo, na Cidade, vendem para o primeiro mapa. O Forte do Passo (`design/mapas-16-40-v0.1.md` §3.2) precisa de uma loja para o 16–30, e as poções de hoje podem curar pouco para a vida de um personagem do 25.

## Requisitos

1. **Loja do Forte** `shop.pass_fort` (o Intendente): armas e armaduras das 4 classes no nível 16 e no 21; poções.
2. **Poções:** conferir se as de hoje (`consumable.greater_healing_potion`, `consumable.greater_mana_potion`) ainda fazem diferença no nível 25 (fração da vida e da Mana que devolvem). Se não, criar a próxima (superior), com preço e recarga, e registrar.
3. **Quedas e ouro** dos monstros novos (T-510 a T-512), no padrão de hoje para normal, elite e chefe. A Bruxa e o Capataz com tabela de chefe.
4. **Economia:** o teste de economia de hoje (`EconomyDesignTests`) estendido até o 27, para o ouro de uma hora de caça pagar as poções que essa hora gasta, como no primeiro mapa.
5. Atualizar `design/itens.md`.

## Critérios de aceite

- testes de economia e de itens passando, com relatório em `tasks/reports/`;
- `./scripts/check.sh` completo passa.

## Resultado

**Base empilhada:** branch `task/T-513-dados-itens-lojas-e-quedas` criada a partir de `origin/task/T-512-capataz-forjado` (`2d5ef60`, sobre a T-511 `52bb459`, a T-510 `5caadf9` e a T-502 `6ee2b2e`). Esta tarefa entra na `main` **depois da 502, 510, 511 e 512**. Relatório: `tasks/reports/balance-v0.27.md` (a próxima tarefa usa v0.28).

**O que foi feito**

- **Poções (requisito 2).** Medidas no perfil esperado de cada nível de 16 a 27: a Cura Maior devolve só 44% da vida do Guerreiro no nível 27 e a Mana Maior 31% da Mana do Mago (44% já no 16). Criadas `consumable.superior_healing_potion` (cura 300 + 7 × VIT, preço 125) e `consumable.superior_mana_potion` (+300 de Mana, preço 125), recarga de 12 s como as outras (`item.superior_healing_potion`, `item.superior_mana_potion` em `abilities_items.json`). Pior caso com elas: 66% (cura, Guerreiro nv27) e 77% (Mana, Mago nv27).
- **Loja `shop.pass_fort`** (Intendente do Forte do Passo) em `data/shops.json`: as 6 poções (Menor, Maior e Superior, cura e Mana) e as 7 armas e 12 armaduras comuns nos níveis **16 e 21** (38 ofertas), preço pela regra de hoje (45 × nível: 720 e 945). O NPC e o mapa são da T-531, que deve ligar o NPC a `shop.pass_fort`.
- **Quedas e ouro (requisito 3).** São por categoria (`rules.items.drops` e `goldByRank`), não por monstro: os 9 monstros do 16 ao 27 já usam normal, elite (Golem de Pedra) e chefe (Bruxa, Capataz). Nada mudou em `monsters.json` nem em `rules.json`; um teste confere que cada um tem queda e ouro e que os dois chefes usam a tabela de chefe.
- **Economia (requisito 4).** `tests/Game.Design.Tests/SouthEconomyDesignTests.cs` (7 testes) estende o modelo da T-322 com o tempo de luta do simulador (média das 4 classes, solo, perfil esperado): ouro/h Brejo ≈ 1.544 e Serra ≈ 4.098; uma hora paga 12 (Brejo) a 33 (Serra) Superiores.
- `design/itens.md` atualizado (poções, quedas, ouro, lojas, economia do sul).

**Decisões e desvios**

- A meta do primeiro mapa (16–24 poções por hora) não cabe com um preço só (Serra rende 2,6× o Brejo); janela do teste de **10 a 40 por hora** e preço 125. O designer pode trocar.
- Equipamento do nível 21 sai em 13,8 min de caça (meta do primeiro mapa: 20–40); teste com piso de 10 min, sem mudar o preço global (opções no relatório §3).
- O modelo não tem o limite de renascimento (mapas do sul sem pontos de nascimento ainda); a T-550 confere.
- Criei também a Cura Superior (a tarefa só pedia "a próxima" se faltasse), porque a Cura Maior também falha na meta de 50%.
- Fora do escopo, registrado: os nomes das 4 habilidades de item antigas em `abilities_items.json` estão com acentuação duplamente codificada (não aparecem ao jogador); as novas estão corretas. Anel e Amuleto só caem, nenhuma loja vende.

**Ids para a T-531:** loja `shop.pass_fort`; itens novos `consumable.superior_healing_potion`, `consumable.superior_mana_potion`; as já existentes `consumable.{minor,greater}_{healing,mana}_potion` e os 19 equipamentos base nos níveis 16 e 21.

**Testes:** `./scripts/check.sh` completo: build sem avisos; todos os projetos passam, inclusive os 7 testes novos de `SouthEconomyDesignTests`. `Game.Design.Tests` tem **4 falhas, as mesmas já existentes na `main`** (`WatcherDesignTests.Without_the_cleric_the_win_rate_drops_clearly`, `CryptDesignTests.A_group_of_four_with_rares_beats_the_watcher...` e `SecondClassDesignTests.Guardian_cuts_the_damage...` nos níveis 30 e 40); não são desta tarefa. Duas correções que os dados novos pediram fora da área listada: `tests/Game.Data.Tests/ShopLoaderTests.cs` (a lista de lojas reais ganhou `shop.pass_fort`) e `client/assets/art/icons/icons.json`/`icons.png` (o teste de ícones exige um ícone para cada item; as Superiores usam o frasco da Maior com o matiz deslocado, dourado e violeta, nas células livres (68,340) e (102,340); é arte provisória, a T-550 ou o designer podem trocar).
