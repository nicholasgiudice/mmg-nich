# Relatório de balanceamento v0.25 (T-511): monstros da Serra Partida

Monstros do campo do nível 21 ao 25 (`design/mapas-16-40-v0.1.md` §3.3), a primeira área de segunda classe: **Mineiro Possuído** (nv21), **Lobo da Montanha** (nv22), **Morcego das Cavernas** (nv22) e o **Golem de Pedra** (nv25, elite). Design: `design/combat-encounters-v0.1.md` § Serra Partida. Testes: `tests/Game.Design.Tests/RidgeDesignTests.cs` (100 seeds por lote, 80 casos) e `tests/Game.Core.Tests/RidgeMonsterTests.cs`. Os números v0.23 (T-505, XP por hora) e v0.24 (T-510, Brejo) são de tarefas da mesma fase.

## 1. Como foi medido

**Perfis.** Cada classe base recebe cada uma das suas duas segundas classes, no nível do grupo (o maior nível dos monstros do encontro), atributos sugeridos com os +5 da segunda classe, equipamento comum do nível. Dois perfis, porque o primeiro não separa as segundas classes no 21–23:

- **esperado** — o perfil esperado com segunda classe (`design/progression-v0.1.md` §8; `SecondClassDesignTests.WithSecondClass`): os pontos de habilidade completam a base primeiro. A base de todas as classes enche com **23 pontos**, então no nível 21–23 a segunda classe **não tem nenhum NH** (só os +5 de atributo), e as duas segundas classes de cada base dão o mesmo resultado nos Mineiros, na Matilha, nos Morcegos e no Acampamento. No 24 entra o NH1 da primeira principal; no 25, o da segunda;
- **desde o 21** — a base parada no 20 e todo ponto do 21 em diante na segunda classe (o perfil do teste do nível 21 da T-408). É o jogador que acabou de escolher a segunda classe e investe nela; separa as 8.

**Bots.** A estratégia solo da classe base (`LevelCurve.SoloStrategy`: Guerreiro e Clérigo `rotation`, Arqueiro e Mago `kite`), todos com `+dodge` (desviam de telegráfos, como no Brejo), e as opções de bot de cada segunda classe (`SecondClassDesignTests.Specs`: a ordem do Berserker, a distância do Atirador, o Meteoro com 3 ou mais do Piromante...). Duplas: as opções de grupo das duas e o Clérigo seguindo o ferido. Arena: `map.fields` (aberta); os desfiladeiros são do mapa (T-532). Os Mineiros do teste nascem em fila na frente do jogador.

## 2. Números finais (nível 1; entre parênteses, no nível do monstro)

**Mineiro Possuído** (`monster.possessed_miner`, nv21, `ai.brute`, `humanoid`): vida 320 (2.880), velocidade 3,2, raio 0,4.

| Habilidade | Números |
|---|---|
| Picaretada | cone 70° × 1,8, telegráfo 0,8 s, físico 30 (132) + Atordoado 1 s, recarga 7 s |
| Golpe de Picareta | arco 90°, alcance 1,4, físico 14 (62), recarga 1,8 s |

**Lobo da Montanha** (`monster.mountain_wolf`, nv22, `ai.pack`, `beast`): vida 260 (2.444), velocidade 4,2, raio 0,4.

| Habilidade | Números |
|---|---|
| Mordida do Lobo da Montanha | arco 80°, alcance 1,3, físico 12 (55), recarga 1,4 s |
| Uivo da Montanha | Fúria da Matilha (`status.pack_aggression`, dano × 1,3, velocidade × 1,15) por 8 s, recarga 20 s |

**Morcego das Cavernas** (`monster.cave_bat`, nv22, `ai.swarm`, `beast`): vida 90 (846), velocidade 4,4, raio 0,3 (era 4,6: todo monstro normal fica abaixo dos 4,5 da classe mais lenta, `DesignTests.Normal_monsters_are_slower_than_the_slowest_class`).

| Habilidade | Números |
|---|---|
| Mordida do Morcego | arco 90°, alcance 1,1, físico 5 (23), recarga 1 s |

**Golem de Pedra** (`monster.stone_golem`, nv25, elite, `ai.brute`, `construct`; físico 30%, arcano −30%): vida 1.200 (12.720), velocidade 2,2, raio 0,7.

| Habilidade | Números |
|---|---|
| Rocha Rolante | linha 14 × 1,6, telegráfo 1,5 s, físico 80 (406) em todo jogador na linha, recarga 10 s |
| Pisão de Pedra | círculo 2,6 em volta dele, telegráfo 1,2 s, físico 45 (229), recarga 9 s |
| Punho de Pedra | arco 100°, alcance 2, físico 20 (102), recarga 2 s |

**Encontros** (`data/encounters.json`, todos em `map.fields`): Mineiros no Desfiladeiro (4 Mineiros nv21, em fila), Matilha da Montanha (4 Lobos nv22), Bando de Morcegos (6 Morcegos nv22), **Acampamento dos Mineiros** (8: 4 Mineiros + 4 Morcegos, todos nv21) e Golem de Pedra na Boca das Minas (Golem nv25; Guerreiro + Clérigo).

## 3. Resultados (100 seeds)

**Solo** (meta: vitória ≥ 85% contra cada grupo típico; Golem ≥ 60%). Vitória e tempo médio das vitórias.

| Segunda classe (perfil) | Mineiros (21) | Matilha (22) | Morcegos (22) | Acampamento (21, 8 monstros) | Golem solo (25) |
|---|---|---|---|---|---|
| Guardião (esperado) | 99%, 24,9 s | 100%, 18,7 s | 100%, 12,3 s | 96%, 23,7 s | 100%, 109,7 s |
| Berserker (esperado) | 99%, 24,9 s | 100%, 18,7 s | 100%, 12,3 s | 96%, 23,7 s | 100%, 94,5 s |
| Atirador de Elite (esperado) | 100%, 36,3 s | 100%, 27,6 s | 100%, 17,6 s | 94%, 43,2 s | 99%, 62,3 s |
| Caçador (esperado) | 100%, 35,6 s | 100%, 26,9 s | 100%, 16,8 s | 98%, 42,8 s | 100%, 53,1 s |
| Piromante (esperado) | 100%, 8,7 s | 100%, 8,8 s | 100%, 4,5 s | 100%, 8,7 s | 100%, 20,5 s |
| Arcanista (esperado) | 100%, 8,7 s | 100%, 8,8 s | 100%, 4,5 s | 100%, 8,7 s | 100%, 21,9 s |
| Sacerdote (esperado) | 100%, 81,4 s | 100%, 68,7 s | 100%, 38,6 s | *0%* (exceção) | 100%, 104,8 s |
| Inquisidor (esperado) | 100%, 81,4 s | 100%, 68,7 s | 100%, 38,6 s | *0%* (exceção) | 100%, 76,3 s |
| Guardião (desde o 21) | 99%, 25,0 s | 100%, 21,2 s | 100%, 13,7 s | 98%, 23,7 s | 100%, 126,6 s |
| Berserker (desde o 21) | 99%, 23,5 s | 100%, 18,3 s | 100%, 11,7 s | 95%, 22,1 s | 100%, 91,7 s |
| Atirador de Elite (desde o 21) | 100%, 38,0 s | 100%, 32,0 s | 100%, 16,3 s | 94%, 45,1 s | 100%, 59,4 s |
| Caçador (desde o 21) | 100%, 31,2 s | 100%, 27,0 s | 100%, 14,5 s | 98%, 37,1 s | 100%, 50,0 s |
| Piromante (desde o 21) | 100%, 8,7 s | 100%, 7,5 s | 100%, 2,6 s | 100%, 8,7 s | 100%, 19,8 s |
| Arcanista (desde o 21) | 100%, 8,6 s | 100%, 7,5 s | 100%, 3,9 s | 100%, 8,6 s | 100%, 21,7 s |
| Sacerdote (desde o 21) | 100%, 76,9 s | 100%, 63,7 s | 100%, 37,0 s | *0%* (exceção) | 100%, 104,0 s |
| Inquisidor (desde o 21) | 100%, 66,5 s | 100%, 50,6 s | 100%, 26,4 s | *1%* (exceção) | 100%, 71,8 s |

**Golem em dupla** (meta ≥ 90%), perfil esperado do nível 25:

| Dupla | Vitória | Tempo | Dano recebido (soma) |
|---|---|---|---|
| Guardião + Sacerdote | 100% | 49,0 s | 959 |
| Berserker + Atirador de Elite | 100% | 33,3 s | 251 |
| Piromante + Inquisidor | 100% | 17,0 s | 3 |

**Grupo grande (Acampamento, 8 monstros): Piromante e Berserker contra o Arcanista** (só medido, sem meta):

| Perfil | Piromante ÷ Arcanista | Berserker ÷ Arcanista |
|---|---|---|
| esperado | 1,00× (8,7 s × 8,7 s) | 2,72× (23,7 s) |
| desde o 21 | 1,01× (8,7 s × 8,6 s) | 2,57× (22,1 s) |

O design das classes pede que o Piromante e o Berserker sejam os mais rápidos contra grupos grandes. **Na Serra isso não acontece** (§4).

## 4. Decisões, desvios e achados

- **Exceção do Clérigo no Acampamento.** O Sacerdote e o Inquisidor não têm dano em área: sozinhos contra 8 monstros quase sempre morrem (0–1%, nos dois perfis). Para não tirar dano do grupo grande das outras seis (que é onde o Berserker e o Piromante devem brilhar), o Acampamento fica fora da meta solo do Clérigo e entra só como medida (`RidgeDesignTests.BigGroupNeedsAGroup`). O design da classe já diz que o Clérigo pede grupo para o dano em todos; o mapa (T-532) deve deixar os acampamentos fora da trilha principal. **Pendência para o designer.**
- **Matilha de 4, não de 5.** Com 5 lobos e mordida 14 o Clérigo perdia sempre (0%). O encontro de teste ficou com 4 e a mordida com 12; matilhas de 5 no mapa pedem grupo para o Clérigo.
- **Acampamento = 4 Mineiros + 4 Morcegos, todos nv21, mordida do Morcego 5.** Com 5 Mineiros + 3 Morcegos nv22 o Guerreiro vencia 10%; com 3 + 5 o Atirador ficava em 80–85% (os morcegos o alcançam); com os morcegos no nv22 o Berserker "desde o 21" ficava em 76%.
- **Golem com vida 1.200** (era 1.800 na primeira versão, depois 1.500): o Guerreiro, sem dano à distância e contra 30% de resistência a físico, vencia 0% e depois 30–40%. Agora vence sempre em 90–125 s. A meta solo (≥ 60%) é um piso; o Golem ficou mais fácil que isso para todos. Para os de longe (Arqueiro e Mago) ele é quase inofensivo: lento demais, e os bots desviam da Rocha. No mapa, o desfiladeiro (3 a 5 tiles) deixa menos espaço para recuar.
- **Achado (classes, fora desta tarefa): o Mago domina a Serra.** No nível 21–22 o Mago mata qualquer grupo, até os 8 do Acampamento, em 3–9 s sem levar dano, com a Explosão Elemental da base (as duas segundas classes ainda têm 0–2 NH). Por isso o Arcanista é tão rápido quanto o Piromante contra o grupo grande, e o Berserker leva 2,6–2,7× o tempo do Arcanista. Não mexi nas classes (fora das áreas da tarefa). **Pendência para o designer/T-550:** conferir no playtest se o Mago no 21–25 é forte demais contra grupos, e se o Piromante precisa se separar do Arcanista já no 21–25 (hoje só se separa quando a segunda classe tem pontos, no 24 em diante no perfil esperado).
- **Perfil esperado com segunda classe no 21–23 = base com +5 de atributo.** A regra do §8 completa a base primeiro e a base enche com 23 pontos. Medi também o perfil "desde o 21" (§1). Os dois passam em todas as metas.
- **Core: nada novo.** A Rocha Rolante é um telegráfo em linha que já atinge todos os inimigos dentro (e nenhum aliado do Golem); a Picaretada é um cone com telegráfo e `status.stunned`; a Fúria da Matilha e os perfis `ai.pack`/`ai.swarm` são os do Lobo Cinzento e do Rato. `RidgeMonsterTests` confere esse uso: a Rocha atinge dois jogadores na linha a 10 e 13 tiles e não atinge o Mineiro no meio dela nem o jogador fora; a Picaretada atordoa por 1 s.
- **Fora das "Áreas que pode alterar"** (mínimo): `tests/Game.Data.Tests/ProgressionContentTests.cs` (a tabela de níveis por área e a lista de monstros ganharam os quatro) e `site/content/site.json` (descrição dos quatro monstros no site).

## 5. Pendências

- **T-532 (mapa da Serra):** desfiladeiros de 3 a 5 tiles com os Mineiros em fila; Acampamento fora da trilha principal; Golem na Boca das Minas, renascendo em 3 min; medir os grupos de novo com as paredes (a Rocha no corredor).
- **T-540 (cliente):** cone da Picaretada (0,8 s), linha longa da Rocha (1,5 s) e círculo do Pisão; Lobo da Montanha com o sprite do Lobo Cinzento e outra cor.
- **T-550 (playtest):** o Mago no 21–25 contra grupos grandes; o Clérigo sozinho na Serra (lutas de 40–80 s); o Golem para quem luta corpo a corpo.
- `design/habilidades.md` § Monstros não foi atualizado (fora das áreas, como na T-510).
