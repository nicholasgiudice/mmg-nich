# Relatório de balanceamento v0.18 (T-402): simulador até o nível 40 e metas revistas

Fase 4: nível máximo 40 e segunda classe no 20 (T-401). Esta tarefa leva os perfis do simulador até o 40, mede as fórmulas nesses níveis, revê os tetos de AGI e as metas dos testes de design depois do balanceamento do playtest de 2026-11-16 (`playtest-v0.3.md`), e escreve as metas que as segundas classes vão ter de cumprir (T-408).

## 1. Método

- **Perfis:** `CharacterProfile.Expected(nível)` de cada classe, do 1 ao 40: pontos de atributo divididos entre os atributos sugeridos, equipamento comum do nível em todos os espaços e pontos de habilidade no perfil focado (o Clérigo solo com Corrente e Escudo). Sem segunda classe: ela chega na T-403. Por isso:
  - os pontos de atributo do 20 em diante **não contam os +5 da segunda classe** (195 no 40, não 200). O Core recusa os +5 sem segunda classe escolhida (`Leveling.Validate`); o perfil passa a contá-los quando a T-403 puser a segunda classe nele. A diferença é de 2 a 5 pontos por atributo, uns 2% nos números abaixo;
  - a classe base tem 23 NH: do nível 24 em diante sobram pontos de habilidade (17 no 40).
- **Ferramenta:** `dotnet run --project tests/Game.Sim -- --level-curve --runs 50` (nova, `tests/Game.Sim/LevelCurve.cs`) gera as tabelas da seção 2.
  - **Habilidades:** valor do NH do perfil, sem defesa nem crítico; acertos × valor por acerto. Cura e escudo entram como valor.
  - **Redução de defesa:** armadura (física) e resistência (mágica) do personagem contra um atacante do mesmo nível: `D / (D + 40 + 8 × nível)`, teto 60%.
  - **Tempo para matar:** a classe sozinha contra **um** monstro normal do próprio nível (Lobo Cinzento, médio; Guarda Esquelético, pesado; vida e potência por `rules.monsters`), na arena dos Três Lobos, 50 seeds, a mesma estratégia dos testes de design (Guerreiro e Clérigo `rotation`, Arqueiro e Mago `kite`). **A assinatura fica de fora**: com recarga de 60–120 s ela não entra em toda luta de quem caça um monstro atrás do outro (com ela, o Mago mata um Lobo do próprio nível em 2 s do 15 em diante). XP por minuto conta só o tempo de luta (sem descanso nem caminhada).
- **Testes de design reativados:** os 19 casos marcados com `PlaytestRebalance.Pending` rodaram com 100 seeds, como na suíte (34 casos com os das teorias que já passavam; 16 falhavam). Os números estão na seção 4.

## 2. Do nível 1 ao 40

Saída de `--level-curve --runs 50`, já com o teto de velocidade de ataque novo (seção 3; só muda o Arqueiro no nível 40: +50% → +52%). Números com ponto decimal, como a ferramenta imprime.

### Guerreiro

| Nível | FOR/DES/AGI/INT/VIT | ARMA | Vida | Mana (regen/s) | Crítico | Vel. ataque | Red. física | Red. mágica | Golpe de Espada | Golpe Pesado | Golpe Circular | Muralha Viva |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | 12/3/6/4/10 | 5 | 240 | 88 (5.4) | 6.5 % | +3 % | 38 % | 11 % | 17 | NH1: 50 | — | — |
| 5 | 22/3/6/4/20 | 9 | 396 | 88 (5.4) | 6.5 % | +3 % | 43 % | 13 % | 31 | NH2: 95 | NH1: 45 | — |
| 10 | 35/3/6/4/32 | 14 | 586 | 88 (5.4) | 6.5 % | +3 % | 45 % | 14 % | 49 | NH4: 186 | NH4: 2 × 68 = 136 | — |
| 15 | 47/3/6/4/45 | 19 | 786 | 88 (5.4) | 6.5 % | +3 % | 46 % | 14 % | 66 | NH5: 273 | NH5: 3 × 76 = 228 | NH3: 208 |
| 20 | 60/3/6/4/57 | 24 | 976 | 88 (5.4) | 6.5 % | +3 % | 47 % | 15 % | 84 | NH5: 340 | NH5: 3 × 96 = 287 | NH3: 246 |
| 25 | 72/3/6/4/70 | 29 | 1176 | 88 (5.4) | 6.5 % | +3 % | 47 % | 15 % | 101 | NH5: 403 | NH5: 3 × 114 = 342 | NH3: 288 |
| 30 | 85/3/6/4/82 | 34 | 1366 | 88 (5.4) | 6.5 % | +3 % | 47 % | 15 % | 119 | NH5: 471 | NH5: 3 × 134 = 402 | NH3: 326 |
| 35 | 97/3/6/4/95 | 39 | 1566 | 88 (5.4) | 6.5 % | +3 % | 47 % | 15 % | 136 | NH5: 534 | NH5: 3 × 152 = 457 | NH3: 368 |
| 40 | 110/3/6/4/107 | 44 | 1756 | 88 (5.4) | 6.5 % | +3 % | 47 % | 15 % | 154 | NH5: 601 | NH5: 3 × 172 = 516 | NH3: 406 |

### Arqueiro

| Nível | FOR/DES/AGI/INT/VIT | ARMA | Vida | Mana (regen/s) | Crítico | Vel. ataque | Red. física | Red. mágica | Disparo | Disparo Potente | Chuva de Flechas | Flecha Devastadora |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | 4/12/6/5/8 | 5 | 130 | 90 (5.5) | 6.5 % | +3 % | 29 % | 11 % | 16 | NH1: 39 | — | — |
| 5 | 4/22/16/5/8 | 9 | 166 | 90 (5.5) | 9.0 % | +8 % | 33 % | 13 % | 29 | NH2: 76 | NH1: 3 × 41 = 122 | — |
| 10 | 4/35/28/5/8 | 14 | 211 | 90 (5.5) | 12.0 % | +14 % | 35 % | 14 % | 46 | NH4: 2 × 94 = 188 | NH1: 3 × 59 = 177 | — |
| 15 | 4/47/41/5/8 | 19 | 256 | 90 (5.5) | 15.3 % | +21 % | 36 % | 14 % | 62 | NH5: 3 × 104 = 312 | NH1: 3 × 76 = 227 | NH3: 470 |
| 20 | 4/60/53/5/8 | 24 | 301 | 90 (5.5) | 18.2 % | +27 % | 37 % | 15 % | 79 | NH5: 3 × 130 = 391 | NH3: 3 × 132 = 395 | NH3: 582 |
| 25 | 4/72/66/5/8 | 29 | 346 | 90 (5.5) | 21.5 % | +33 % | 37 % | 15 % | 95 | NH5: 3 × 155 = 465 | NH5: 4 × 199 = 798 | NH3: 686 |
| 30 | 4/85/78/5/8 | 34 | 391 | 90 (5.5) | 24.5 % | +39 % | 37 % | 15 % | 112 | NH5: 3 × 181 = 544 | NH5: 4 × 232 = 929 | NH3: 798 |
| 35 | 4/97/91/5/8 | 39 | 436 | 90 (5.5) | 27.8 % | +46 % | 37 % | 15 % | 128 | NH5: 3 × 206 = 618 | NH5: 4 × 262 = 1050 | NH3: 902 |
| 40 | 4/110/103/5/8 | 44 | 481 | 90 (5.5) | 30.7 % | +52 % | 37 % | 15 % | 145 | NH5: 3 × 232 = 697 | NH5: 4 × 295 = 1181 | NH3: 1014 |

### Mago

| Nível | FOR/DES/AGI/INT/VIT | ARMA | Vida | Mana (regen/s) | Crítico | Vel. ataque | Red. física | Red. mágica | Projétil Arcano | Explosão Elemental | Prisão de Gelo | Barreira Arcana | Campo de Relâmpagos | Cataclismo Arcano |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | 3/3/5/13/6 | 3 | 110 | 106 (6.3) | 6.2 % | +2 % | 19 % | 20 % | 15 | NH1: 53 | — | — | — | — |
| 5 | 3/3/5/35/6 | 5 | 138 | 150 (8.5) | 6.2 % | +2 % | 24 % | 25 % | 35 | NH2: 117 | NH1: 19 | NH1: 89 | NH1: 25 | — |
| 10 | 3/3/5/62/6 | 8 | 173 | 204 (11.2) | 6.2 % | +2 % | 25 % | 25 % | 59 | NH4: 2 × 151 = 302 | NH1: 31 | NH1: 143 | NH4: 66 | — |
| 15 | 3/3/5/90/6 | 11 | 208 | 260 (14.0) | 6.2 % | +2 % | 26 % | 26 % | 84 | NH5: 4 × 176 = 704 | NH1: 44 | NH1: 199 | NH5: 104 | NH3: 960 |
| 20 | 3/3/5/117/6 | 14 | 243 | 314 (16.7) | 6.2 % | +2 % | 26 % | 26 % | 108 | NH5: 4 × 222 = 888 | NH4: 89 | NH3: 355 | NH5: 133 | NH3: 1219 |
| 25 | 3/3/5/145/6 | 17 | 278 | 370 (19.5) | 6.2 % | +2 % | 26 % | 26 % | 134 | NH5: 4 × 270 = 1078 | NH5: 122 | NH5: 556 | NH5: 164 | NH3: 1488 |
| 30 | 3/3/5/172/6 | 20 | 313 | 424 (22.2) | 6.2 % | +2 % | 26 % | 26 % | 158 | NH5: 4 × 315 = 1262 | NH5: 144 | NH5: 653 | NH5: 193 | NH3: 1747 |
| 35 | 3/3/5/200/6 | 23 | 348 | 480 (25.0) | 6.2 % | +2 % | 26 % | 27 % | 183 | NH5: 4 × 363 = 1452 | NH5: 167 | NH5: 754 | NH5: 223 | NH3: 2016 |
| 40 | 3/3/5/227/6 | 26 | 383 | 534 (27.7) | 6.2 % | +2 % | 27 % | 26 % | 207 | NH5: 4 × 409 = 1636 | NH5: 189 | NH5: 851 | NH5: 252 | NH3: 2275 |

### Clérigo

| Nível | FOR/DES/AGI/INT/VIT | ARMA | Vida | Mana (regen/s) | Crítico | Vel. ataque | Red. física | Red. mágica | Golpe Sagrado | Escudo Sagrado | Cura Direta | Corrente Sagrada | Milagre | Purificação |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | 6/2/4/12/8 | 4 | 150 | 104 (6.2) | 6.0 % | +2 % | 29 % | 11 % | 11 | NH1: 45 | — | — | — | — |
| 5 | 6/2/4/22/18 | 8 | 290 | 124 (7.2) | 6.0 % | +2 % | 33 % | 13 % | 21 | NH1: 65 | NH1: 70 | NH2: 83 | — | — |
| 10 | 6/2/4/35/30 | 12 | 460 | 150 (8.5) | 6.0 % | +2 % | 35 % | 14 % | 33 | NH4: 146 | NH1: 96 | NH4: 152 | — | — |
| 15 | 6/2/4/47/43 | 17 | 640 | 174 (9.7) | 6.0 % | +2 % | 36 % | 14 % | 45 | NH5: 207 | NH1: 120 | NH5: 232 | NH3: 397 | — |
| 20 | 6/2/4/60/55 | 21 | 810 | 200 (11.0) | 6.0 % | +2 % | 37 % | 15 % | 57 | NH5: 254 | NH4: 234 | NH5: 279 | NH3: 480 | — |
| 25 | 6/2/4/72/68 | 26 | 990 | 224 (12.2) | 6.0 % | +2 % | 37 % | 15 % | 69 | NH5: 297 | NH5: 306 | NH5: 322 | NH3: 557 | NH5: 92 |
| 30 | 6/2/4/85/80 | 30 | 1160 | 250 (13.5) | 6.0 % | +2 % | 37 % | 15 % | 81 | NH5: 344 | NH5: 353 | NH5: 369 | NH3: 640 | NH5: 105 |
| 35 | 6/2/4/97/93 | 35 | 1340 | 274 (14.7) | 6.0 % | +2 % | 37 % | 15 % | 93 | NH5: 387 | NH5: 396 | NH5: 412 | NH3: 717 | NH5: 117 |
| 40 | 6/2/4/110/105 | 39 | 1510 | 300 (16.0) | 6.0 % | +2 % | 37 % | 15 % | 105 | NH5: 434 | NH5: 443 | NH5: 459 | NH3: 800 | NH5: 130 |

### Tetos

Velocidade de ataque no máximo com AGI 120; crítico no máximo com AGI 140 (sem bônus de itens); defesa no máximo em 60 %.

| Classe | AGI no nível máximo | Teto da vel. de ataque | Teto do crítico | Teto da defesa física | Teto da defesa mágica |
|---|---|---|---|---|---|
| Guerreiro | 6 | não bate | não bate | não bate | não bate |
| Arqueiro | 103 | não bate | não bate | não bate | não bate |
| Mago | 5 | não bate | não bate | não bate | não bate |
| Clérigo | 4 | não bate | não bate | não bate | não bate |

### Tempo para matar: Lobo Cinzento do próprio nível (50 execuções)

Vida do monstro × fator de nível; sem a assinatura (ritmo de caça); XP por minuto só com o tempo de luta. Cada célula: vitória, tempo médio, dano recebido em % da vida máxima, XP/min.

| Nível | Vida do monstro | Potência | XP | Guerreiro | Arqueiro | Mago | Clérigo |
|---|---|---|---|---|---|---|---|
| 1 | 220 | ×1.00 | 6 | 100 %, 9.9 s, 15 %, 36 | 100 %, 8.5 s, 0 %, 42 | 100 %, 11.6 s, 0 %, 31 | 100 %, 22.5 s, 73 %, 16 |
| 5 | 572 | ×1.68 | 30 | 100 %, 12.2 s, 18 %, 148 | 100 %, 8.8 s, 0 %, 205 | 100 %, 10.9 s, 1 %, 165 | 100 %, 18.7 s, 52 %, 96 |
| 10 | 1012 | ×2.53 | 60 | 100 %, 11.7 s, 14 %, 309 | 100 %, 7.7 s, 0 %, 465 | 100 %, 8.6 s, 0 %, 417 | 100 %, 18.9 s, 51 %, 190 |
| 15 | 1452 | ×3.38 | 90 | 100 %, 10.8 s, 11 %, 502 | 100 %, 7.8 s, 0 %, 695 | 100 %, 5.5 s, 0 %, 975 | 100 %, 18.9 s, 48 %, 285 |
| 20 | 1892 | ×4.23 | 120 | 100 %, 10.8 s, 9 %, 668 | 100 %, 6.4 s, 0 %, 1128 | 100 %, 5.5 s, 0 %, 1311 | 100 %, 19.1 s, 47 %, 377 |
| 25 | 2332 | ×5.08 | 150 | 100 %, 11.4 s, 10 %, 793 | 100 %, 6.2 s, 0 %, 1450 | 100 %, 4.5 s, 0 %, 2008 | 100 %, 19.6 s, 48 %, 458 |
| 30 | 2772 | ×5.93 | 180 | 100 %, 11.4 s, 10 %, 950 | 100 %, 6.0 s, 0 %, 1803 | 100 %, 5.2 s, 0 %, 2061 | 100 %, 19.9 s, 49 %, 542 |
| 35 | 3212 | ×6.78 | 210 | 100 %, 11.7 s, 11 %, 1075 | 100 %, 6.0 s, 0 %, 2087 | 100 %, 5.2 s, 0 %, 2404 | 100 %, 20.6 s, 49 %, 612 |
| 40 | 3652 | ×7.63 | 240 | 100 %, 11.8 s, 11 %, 1221 | 100 %, 5.6 s, 0 %, 2577 | 100 %, 5.3 s, 0 %, 2725 | 100 %, 21.0 s, 49 %, 686 |

### Tempo para matar: Guarda Esquelético do próprio nível (50 execuções)

Vida do monstro × fator de nível; sem a assinatura (ritmo de caça); XP por minuto só com o tempo de luta. Cada célula: vitória, tempo médio, dano recebido em % da vida máxima, XP/min.

| Nível | Vida do monstro | Potência | XP | Guerreiro | Arqueiro | Mago | Clérigo |
|---|---|---|---|---|---|---|---|
| 1 | 440 | ×1.00 | 6 | 100 %, 23.1 s, 38 %, 16 | 100 %, 20.6 s, 0 %, 18 | 100 %, 24.7 s, 0 %, 15 | 100 %, 44.6 s, 135 %, 8 |
| 5 | 1144 | ×1.68 | 30 | 100 %, 30.4 s, 40 %, 59 | 100 %, 24.9 s, 0 %, 72 | 100 %, 26.2 s, 0 %, 69 | 100 %, 35.3 s, 80 %, 51 |
| 10 | 2024 | ×2.53 | 60 | 100 %, 29.6 s, 37 %, 121 | 100 %, 23.2 s, 0 %, 155 | 100 %, 24.3 s, 0 %, 148 | 100 %, 37.6 s, 77 %, 96 |
| 15 | 2904 | ×3.38 | 90 | 100 %, 28.4 s, 34 %, 190 | 100 %, 18.7 s, 0 %, 289 | 100 %, 17.4 s, 0 %, 310 | 100 %, 38.8 s, 78 %, 139 |
| 20 | 3784 | ×4.23 | 120 | 100 %, 29.5 s, 27 %, 244 | 100 %, 15.9 s, 0 %, 452 | 100 %, 17.6 s, 0 %, 408 | 100 %, 41.8 s, 87 %, 172 |
| 25 | 4664 | ×5.08 | 150 | 100 %, 29.9 s, 27 %, 301 | 100 %, 14.8 s, 0 %, 609 | 100 %, 17.2 s, 0 %, 524 | 100 %, 42.5 s, 86 %, 212 |
| 30 | 5544 | ×5.93 | 180 | 100 %, 30.0 s, 27 %, 360 | 100 %, 14.5 s, 0 %, 745 | 100 %, 17.4 s, 0 %, 620 | 100 %, 43.0 s, 87 %, 251 |
| 35 | 6424 | ×6.78 | 210 | 100 %, 30.3 s, 27 %, 416 | 100 %, 14.5 s, 0 %, 871 | 100 %, 17.5 s, 0 %, 719 | 100 %, 43.1 s, 87 %, 292 |
| 40 | 7304 | ×7.63 | 240 | 100 %, 30.4 s, 27 %, 474 | 100 %, 14.2 s, 0 %, 1015 | 100 %, 17.7 s, 0 %, 816 | 100 %, 43.2 s, 87 %, 333 |

### O que a curva mostra

1. **As fórmulas ficam de pé até o 40.** A vida dos monstros cresce linear (`1 + 0,4 × (nível − 1)`, ×16,6 no 40) e o dano do personagem também (atributos, arma e NH crescem linear). O resultado é um **tempo para matar quase constante do 15 ao 40**:

   | Classe | Lobo, nv15 → nv40 | Guarda, nv15 → nv40 |
   |---|---|---|
   | Guerreiro | 10,8 → 11,8 s (+9%) | 28,4 → 30,4 s (+7%) |
   | Arqueiro | 7,8 → 5,6 s (−28%) | 18,7 → 14,2 s (−24%) |
   | Mago | 5,5 → 5,3 s (−4%) | 17,4 → 17,7 s (+2%) |
   | Clérigo | 18,9 → 21,0 s (+11%) | 38,8 → 43,2 s (+11%) |

   Com a XP por monstro crescendo com o nível (`6 × nível`), a **XP por minuto cresce linear**: o Guerreiro vai de ~500 (Lobo) e ~190 (Guarda) por minuto de luta no 15 a ~1 220 e ~470 no 40. A XP para o próximo nível cresce com `nível^1,8`, então o tempo por nível cresce com `nível^0,8`, como previsto em `design/segunda-classe-v0.1.md` §3. Do 39 ao 40 (49 708 XP), o Guerreiro leva ~41 min de luta só contra Lobos do nível, ~105 min só contra Guardas: as 2 a 3 h por nível alto do design, contando descanso e caminhada.
2. **O perigo também fica constante.** A potência dos monstros (×7,6 no 40) cresce no mesmo ritmo da vida do personagem (Guerreiro ×7,3) e a redução da defesa para de crescer. O Guerreiro perde ~27% da vida contra um Guarda do nível do 20 em diante (34–40% até o 15); o Clérigo, que se cura, ~87%.
3. **Defesa:** a armadura cresce com o nível do item e o divisor com o nível do atacante, então a redução contra o mesmo nível converge: Guerreiro (pesada) 47% física e 15% mágica, Arqueiro e Clérigo (média) 37% e 15%, Mago (leve) 27% e 26%. **Ninguém chega ao teto de 60%**, nem com raros (afixo de armadura `2 × nível` em uma peça não muda a ordem de grandeza). O teto continua valendo para buffs (Guarda Firme, Muralha Viva, Bastião).
4. **Diferença entre classes:** os de longe ficam relativamente mais rápidos com o nível. O Arqueiro ganha velocidade de ataque pela AGI e a Chuva de Flechas no NH5 (nível 25); o Mago tem a Explosão no NH5 desde o 9. Contra o Guarda no 40, a média é 26,4 s: Clérigo 1,64×, Guerreiro 1,15×. Contra o Lobo no 40, a média é 10,9 s e o **Clérigo fica em 1,92×**, acima do 1,8× da meta (era 1,61× no 10). Fica como alerta para as segundas classes do Clérigo (Inquisidor é a de dano); o Guerreiro está dentro.
5. **Pontos sem uso:** sem segunda classe, a classe base tem 23 NH e sobram 17 pontos de habilidade no 40. Os números do 24 em diante são, então, o piso do personagem: a segunda classe só soma.
6. **Mana:** o Mago chega a 534 de Mana e 27,7/s no 40; Guerreiro e Arqueiro ficam em 88–90 (5,4–5,5/s), porque não põem pontos em INT. O custo das habilidades não cresce com o nível, então a Mana deixa de limitar o Mago e continua limitando Guerreiro e Arqueiro como no 1. Rever com os custos das segundas classes (T-408): Fúria 30, Execução 30, Tempestade de Aço 90 contra 88 de Mana do Guerreiro.

## 3. Tetos de AGI

| Teto | Regra | AGI do teto | Perfil esperado bate? |
|---|---|---|---|
| Velocidade de ataque | `attackSpeedPerAgi` 0,005, `attackSpeedMax` | 100 (antes) → **120** (depois) | Arqueiro: AGI 101 no 39 e 103 no 40 → **batia no nível 39** (antes); não bate mais (depois) |
| Crítico | `critPerAgi` 0,0025, base 5%, `critMax` 40% | 140 | não: o Arqueiro chega a 30,7% (AGI 103) no 40 |
| Defesa | 60% | — | não: a armadura pesada do Guerreiro para em ~47% contra o mesmo nível (seção 2) |

Só o Arqueiro tem AGI entre os atributos sugeridos; nas outras classes a AGI fica a do perfil inicial (4 a 6).

**Ajuste aplicado em `data/rules.json`:** `stats.attackSpeedMax` **0,5 → 0,6** (velocidade de ataque até +60%, teto com AGI 120).

- **Por que teto maior, e não valor por ponto menor:** abaixo de AGI 100 nada muda, então o jogo do 1 ao 15, os testes de design e a calibração do nível 1 (§1 do design de progressão) ficam iguais. Cortar o valor por ponto (0,005 → 0,004) mudaria o ataque básico de toda classe em todo nível.
- **Antes → depois, Arqueiro esperado:** nível 39 +50% (no teto) → +50%; nível 40 +50% (no teto, AGI 103) → **+52%**. Com os +5 da segunda classe (AGI ~106 no 40) ficaria em +53%: ainda abaixo do teto.
- **Crítico mantido:** o perfil esperado não chega a AGI 140. Quem puser tudo em AGI chega lá por volta do nível 27; isso é escolha do jogador, não do perfil. Rever com as segundas classes que vivem de crítico (Atirador de Elite: Mira Firme e Olho de Falcão), que somam crítico de habilidade por cima.
- Teste novo: `Expected_archer_stays_below_the_attack_speed_cap_up_to_level_40` (`tests/Game.Sim.Tests/CharacterProfileTests.cs`).

## 4. Metas revistas (casos reativados)

O balanceamento do playtest de 2026-11-16 (regeneração de Mana por INT 0,05 → 0,1; Explosão Elemental +50% e 4 acertos no NH5; poções com recarga de 12 s e cura 60 + 2 × VIT) deixou o jogo mais fácil que as metas, e o designer aprovou o jogo assim ao fechar o playtest v0.3. **Nenhum dado de habilidade ou monstro mudou:** onde a meta caiu, ela foi ajustada ao jogo aprovado, com margem pequena, para que um novo ganho de força volte a falhar.

`PlaytestRebalance.cs` foi apagado. Dos 34 casos que rodaram, 18 já passavam com a meta antiga e continuam com ela:

- `ProgressionDesignTests`: perfil fraco (Lobos nv5, Lobo e Salteador nv9, Ratos nv5, Guarda nv5 e nv9); 1,6× no nível 5; Matriarca no nível 7;
- `ForestDesignTests`: perfil fraco (Arqueiro, Mago e Clérigo nas Fadas; Arqueiro e Clérigo no Ent);
- `EconomyDesignTests`: poção de Mana no Mago nv5 e no Clérigo nv5 e nv9;
- `RuinsDesignTests` e `RuinsZoneDesignTests`: Pátio dos Mortos solo, "possível mas arriscado" (Guerreiro 3–5%, Arqueiro 33–41%, Mago e Clérigo 100%).

Os 16 que falhavam:

| Teste | Medido (100 seeds) | Meta antes | Meta depois | Por quê |
|---|---|---|---|---|
| Perfil fraco solo (`ProgressionDesignTests`, `ForestDesignTests`), 6 casos: Lobo e Salteador nv5 (Arqueiro), Três Lobos nv9 (Guerreiro), Quatro Ratos nv9 (Mago), Fadas e Ent (Guerreiro), Ent (Mago) | fraco vence 89–100%; o mais justo, Guerreiro nas Fadas: 34,0 s contra 26,6 s do esperado (1,28×) | vitória ≤ 50% (o Clérigo: ≤ 50% ou ≥ 1,3× o tempo) | vitória ≤ 50% **ou ≥ 1,25× o tempo do esperado**, para todas as classes | com poção curando pela VIT e mais Mana, o fraco 3 níveis abaixo passou a vencer as lutas da área. O designer achou o ritmo bom; a diferença de nível continua visível no tempo. 1,25× é o maior fator que o caso mais justo cumpre (1,28×). Para o Clérigo a meta baixou de 1,3× para 1,25× (ele já cumpria 1,3×) |
| Matriarca nv8, grupo nv8 com incomuns | 94%, 134 s | 60–90%, 2–4 min | **60–97%**, 2–4 min | o chefe ficou mais fácil com as poções; o nv7 (já dentro) e o nv8 cabem com margem |
| Matriarca, grupo do protótipo em movimento (`DesignTests`) | 96%, 182 s | 60–90% | **60–97%** | o mesmo |
| 1,6× no nível 9 | Clérigo 37,1 s contra média 21,2 s = 1,75× | Clérigo até 1,7× | Clérigo até **1,8×** | a Explosão +50% acelerou o Mago e baixou a média; o Clérigo não mudou. As outras classes continuam com 1,6× |
| Poção de Mana, Mago fraco nv9 (`EconomyDesignTests`) | com poção 100%; sem poção 98% | vitória ≤ 50% | vitória ≤ 50% **ou a poção acrescenta no máximo 15 pontos** à vitória sem poção | o teste quer saber se a poção carrega o fraco; no nv9 ele já vence sem poção (+2 pontos com ela). No nv5 continua valendo o ≤ 50% (16% com poção, 14% sem) |
| Explosão sozinha × rotação, Mago nos Quatro Ratos | só Explosão 100%, 25,7 s; rotação 100%, 27,4 s (94%) | ≤ 50% ou mais lenta que a rotação | ≤ 50% ou **no máximo 10% mais rápida** que a rotação | a Explosão +50% foi pedida pelo designer. A meta passa a ser "não claramente melhor que a rotação"; a gestão de Mana continua testada (`Mage_explosion_spam_runs_out_of_mana_before_the_end_in_most_runs`, que passa) |
| Pelo menos três encontros com uma estratégia entre 5% e 95% (`DesignTests`) | só a Matriarca sem Clérigo (28%) | ≥ 3 encontros | **removido** | os encontros do protótipo (nível 1) ficaram em 96–100%. Resultados não binários continuam cobertos por testes com perfil: Pátio dos Mortos solo (média 40–70%), Matriarca sem Clérigo (5–60%) e perfil fraco |
| Vigia, grupo de 4 com raros (`WatcherDesignTests`) | nv14 96%, 287 s; nv15 99%, 268 s; média 97,5%; ainda perde selos (0,26–0,37 por luta) e gente (0,05–0,16 mortes) | média 50–80%, cada nível 40–90%, 4–6 min | média **80–99%**, cada nível **≥ 70%**, 4–6 min | o chefe final ficou mais fácil com o grupo bem montado; o designer não se queixou da Cripta. O teto de 99% exige que ele ainda derrote às vezes |
| Vigia na Sala do Vigia real (`CryptDesignTests`) | nv14 92%, 326 s; nv15 94%, 307 s | a mesma do Vigia | a mesma do Vigia (80–99%, ≥ 70%) | o mesmo |
| Cavaleiro Espectral, grupo de 3 com incomuns (`RuinsDesignTests`) | 93%, 98%, 100%, 100% (média 97,8%), 68–80 s | média 70–90%, 60–120 s | média **85–99%**, 60–120 s | o elite opcional ficou mais fácil; a duração continua na meta |
| Ida completa à Cripta (`CryptDesignTests`) | rota segura 24,2 min, rota de risco 26,2 min (lutas 11,9–13,0 min) | 25–40 min | **20–40 min** | as lutas ficaram mais curtas; a rota segura caiu 0,8 min abaixo do mínimo |

## 5. Metas da segunda classe

Escritas na §8 de `design/progression-v0.1.md` ("Metas da segunda classe"), para a T-408 testar com o Guardião e o Berserker:

- **Berserker** mata um grupo típico (3 monstros normais do nível) mais rápido que o Guerreiro só com a base: em no máximo 85% do tempo, vencendo ≥ 85%, e recebendo no máximo 1,5× o dano.
- **Guardião** reduz em pelo menos 15% o dano que os outros membros recebem num encontro de grupo, sem baixar a vitória; solo, vence ≥ 85% mesmo mais devagar.
- **Nenhuma segunda classe** mais de 1,6× mais lenta que a média das outras no solo.
- **No nível 21** a segunda classe já é útil: escolher não deixa o personagem mais lento que só a base.
- Pôr os pontos do 21 em diante na base ou na segunda classe: as duas vencem ≥ 70%.

Níveis de teste: 25, 30 e 40, monstros normais do próprio nível. Com a seção 2 como referência, o Guerreiro só com a base mata um Lobo do próprio nível em ~11–12 s e um Guarda em ~30 s em qualquer nível do 15 ao 40.
