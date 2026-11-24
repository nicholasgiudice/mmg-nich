# Relatório de balanceamento v0.26 (T-512): o Capataz Forjado

Chefe das **Minas Fundas** (`design/mapas-16-40-v0.1.md` §3.4), masmorra instanciada do nível 24 ao 27 para 4 pessoas, a primeira com papel de grupo de verdade. Design: `design/combat-encounters-v0.1.md` § Minas Fundas. Testes: `tests/Game.Design.Tests/OverseerDesignTests.cs` (100 seeds por lote) e `tests/Game.Core.Tests/OverseerMonsterTests.cs`. Os números v0.23 (T-505), v0.24 (T-510) e v0.25 (T-511) são de tarefas da mesma fase, em outras branches.

## 1. Como foi medido

**Perfil.** Grupo de 4 do nível 27, cada membro com o perfil esperado com segunda classe (`SecondClassDesignTests.WithSecondClass`, perfil de grupo: o Clérigo sem o par solo), atributos sugeridos com os +5 da segunda classe e **raros** do nível, como o Vigia (T-351). No 27 a base está cheia (23 pontos) e a segunda classe tem 4 pontos: as quatro normais no NH1. Os perfis das quatro classes base são juntados num só (cada classe base aparece uma vez no grupo).

**Bots.** Cada classe com a estratégia solo dela e `+dodge` (Guerreiro e Clérigo `rotation`, Arqueiro e Mago `kite`), as opções de grupo de cada segunda classe (`SecondClassDesignTests.Specs`: o Guardião sem o Golpe de Escudo, o Sacerdote abrindo com Bênção e Inspiração) e o Clérigo seguindo o ferido (`followInjured`). Arena: `map.watcher_hall` (salão aberto de 40 × 28), provisória até a Fundição da T-533.

**Grupos.**

| Grupo | Guerreiro | Arqueiro | Mago | Clérigo |
|---|---|---|---|---|
| com os papéis | Guardião | Atirador de Elite | Piromante | Sacerdote |
| sem os papéis | Berserker | Atirador de Elite | Piromante | Inquisidor |
| só o tanque (medido) | Guardião | Atirador de Elite | Piromante | Inquisidor |
| só a cura (medido) | Berserker | Atirador de Elite | Piromante | Sacerdote |
| outro dano (medido) | Guardião | Caçador | Arcanista | Sacerdote |

**Medida nova no simulador:** `RunResult.DamageTakenBySource` / `BatchSummary.DamageTakenBySource`, o dano recebido pelos jogadores por id da habilidade ou estado que o causou (usado para ver quem bate em quem).

## 2. Números finais (nível 1; entre parênteses, no nível 27)

**Capataz Forjado** (`monster.forged_overseer`, nv27, chefe, `ai.boss`, `construct`): vida 70.000 (616.000), velocidade 3,0, raio 0,9. Escala de chefe: vida × 8,8 e potência × 10,88. `meleeTargetRange` 3,5, `approachRange` 2,4.

| Habilidade | Fases | Números |
|---|---|---|
| Punho de Ferro | todas | arco 100°, alcance 2,4, físico 62 (675), recarga 1,6 s |
| Escória Ardente | todas | `ai.ranged` (alvo a 3 tiles ou mais), alcance 14, fogo 25 (272), recarga 2,5 s |
| Martelo | 1 e 2 | cone 100° × 3,5, telegráfo 1 s, físico 90 (979), recarga 9 s |
| Sopro da Fornalha | 2 e 3 | círculo de raio 30 em volta dele, sem telegráfo, fogo 22 (239), recarga 5 s |
| Chamar a Equipe | 3 | conjuração 2 s, 1 Mineiro Possuído, no máximo 3 vivos, recarga 20 s |
| Martelo Acelerado | 3 | o Martelo com recarga de 6 s |

| Fase | Vida | Ao entrar |
|---|---|---|
| Martelo (`phase.hammer`) | 100% | — |
| Fornalha (`phase.furnace`) | 60% | Fornalha Esquentando (`status.overseer_heat`, 190 s); ao acabar, Fornalha Transbordando (`status.overseer_meltdown`, dano × 4 até o fim) |
| Chamar a Equipe (`phase.call_the_crew`) | 30% | 2 Mineiros Possuídos (nv27: vida 3.648); Fornalha Acesa (`status.overseer_overdrive`: velocidade × 1,3, dano × 1,15) |

**Golpe por classe** (um golpe, no nível 27 com raros; medido com o Core):

| | Vida | Martelo | Punho de Ferro | Sopro da Fornalha | Escória Ardente |
|---|---|---|---|---|---|
| Guardião | 2.084 | 521 (25%) | 359 (17%) | 204 | 231 |
| Atirador de Elite | 1.174 | 617 (53%) | 425 | 204 | 231 |
| Piromante | 1.102 | 724 (66%) | 499 | 177 | 201 |
| Sacerdote | 1.890 | 617 (33%) | 425 | 204 | 231 |

## 3. Resultados (100 seeds)

| Grupo | Vitória | Tempo (vitórias) | Mortes por luta | Dano do Punho recebido | Meta |
|---|---|---|---|---|---|
| com os papéis | **97%** | **273 s** (p90 280) | 0,14 | 30.400 | ≥ 75%, 3–6 min ✔ |
| sem os papéis | **7%** | 341 s (p90 347) | 3,80 | 52.200 | ≤ 40% ✔ |
| só o tanque | 92% | 304 s | 0,53 | 35.800 | medido |
| só a cura | 100% | 283 s | 0,03 | 39.500 | medido |
| outro dano (Caçador, Arcanista) | 100% | 275 s | 0,02 | 32.700 | medido |

Dano causado por segundo (média por segundo de luta):

| Grupo | Guerreiro | Arqueiro | Mago | Clérigo |
|---|---|---|---|---|
| com os papéis | 508 | 578 | **987** | 274 |
| sem os papéis | 457 | 477 | **589** | 259 |
| só o tanque | 361 | 476 | 936 | 330 |
| só a cura | 603 | 668 | 721 | 272 |

## 4. O que decidiu o desenho

**a) O tanque não segura o chefe pela ameaça.** Na primeira versão, o Capataz seguia a tabela de ameaça, e o Piromante (≈1.000 de dano por segundo) tirava o alvo do Guardião em segundos: a Provocação dá 40 de ameaça e 3 s de alvo forçado a cada 10 s, e o Golpe de Escudo, 3× o dano dele. O chefe passava a luta andando atrás do Mago, que corre mais (4,5 contra 3,0), e o Punho de Ferro acertava um golpe a cada ~8 s. **Mecânica nova:** `meleeTargetRange` no `ai.boss`, o Capataz bate no de maior ameaça entre quem está a até 3,5 tiles dele. Com ela, o Punho passou a acertar o tanque o tempo todo, e o Berserker leva **1,7×** o dano do Punho que o Guardião leva.

**b) Quem foge do chefe não paga nada.** Sem o tanque, quando o Berserker caía o chefe ia atrás do Mago, que ficava 100% da luta vivo correndo pela sala, e o grupo ainda vencia. **Mecânica nova:** o papel `ai.ranged` no `ai.boss` (habilidade que só sai com o alvo a `farDistance`, 3, ou mais, como no `ai.brute`), usado pela **Escória Ardente**.

**c) A sobrevivência não separava os grupos.** Subindo o Punho e a Fornalha, os dois grupos caíam juntos, num degrau: com Punho 70 e Fornalha 30, 100% × 90%; com Punho 80 e Fornalha 34, 40% × 20%; com 90 e 40, 10% × 0%. Em lotes de 30, combinações vizinhas davam resultados opostos (Punho 70/Fornalha 32: sem papéis 7%; 68/27: 90%). Os dois grupos aguentam quase o mesmo dano: o **Inquisidor cura tanto quanto o Sacerdote** (Selo da Luz e Consagração; 80 mil de cura contra 57 mil na luta) e a **Sede de Sangue** do Berserker compensa a defesa do Guardião.

**d) O que separa é o tempo.** Com o chefe parado na frente do Guardião, o fogo do Piromante (que fica no chão) acerta: o Mago rende **987** de dano por segundo com o Guardião e **589–721** com o Berserker. A Bênção e a Inspiração do Sacerdote somam 10–20% no Atirador e no Mago. A luta leva **273 s** com os dois papéis e **~360 s** sem eles, com pouca variação (p90 a 7 s da média). **Decidido:** o contador da **Fornalha Transbordando** (190 s depois da entrada na fase 2, que acontece em ~110 s com os papéis): o grupo com os papéis termina com ~25 s de folga; o sem papéis é pego pela fúria (dano × 4). O contador usa o `onExpire` de estado (T-421) e começa na fase 2 porque a fase 1 é entrada quando o chefe nasce, antes de o grupo chegar.

Sondagens com o contador (30 seeds, Punho 62, Fornalha 22): 200 s → com papéis 97%, sem 40%; **190 s → 97% e 7–8%** (100 seeds); 180 s → 97% e 0%, só o tanque 37%.

**e) O Martelo quase não acerta.** Os bots saem do cone no telegráfo (reação de 0,2–0,4 s): o Martelo causou 50–450 de dano por luta. Ficou com físico 90 para cumprir a proposta ("quem não é tanque cai em 2 golpes": Arqueiro e Mago, ~620 e ~720 de 1.174 e 1.102) e punir quem não lê o chão; não pesa na meta.

**f) A Fornalha Acesa sem bônus de dano** deixava o grupo sem papéis a 87% (Punho 80, Fornalha 24, sem contador): a fase 3 é onde o tanque cai. Ficou com dano × 1,15.

## 5. Pendências

- **T-533:** a Fundição (sala aberta, ~40 × 28, carrinhos de mina como cobertura) e as salas propostas no design; medir de novo com as paredes. Uma derrota do grupo deve reiniciar o chefe (vida, fase e a Fornalha); hoje o Core não tem "reset" de chefe sem jogadores.
- **T-540:** o cone do Martelo, a fornalha acendendo a cada 5 s, o contador da Fornalha Esquentando visível (é o limite de tempo da luta) e a Fornalha Transbordando.
- **T-550 / designer:** a ameaça do tanque no nível 27 é pequena perto do dano do Piromante; o `meleeTargetRange` resolve só para o Capataz. O mesmo deve pesar nos chefes do 28 ao 40 e é a causa provável da falha antiga `Guardian_cuts_the_damage_on_the_rest_of_the_group_by_at_least_15_percent` (nv30/40). O Inquisidor cura quase tanto quanto o Sacerdote em grupo.
- A meta "sem papéis ≤ 40%" depende da duração: um grupo sem papéis com muito mais dano (ou um com papéis com muito menos) muda de lado. Com Caçador e Arcanista no lugar de Atirador e Piromante, o grupo com papéis venceu 100% em 275 s.
