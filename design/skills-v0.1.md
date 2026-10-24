# Habilidades — v0.1: todos os níveis

Status: **proposta para revisão do designer** (T-034, 2026-10-19). Complementa `design/progression-v0.1.md` §7, que define as regras (1 ponto de habilidade por nível do personagem, requisito de nível `2n − 1` por NH, assinatura nos níveis 12, 13 e 15).

**Situação no jogo:** hoje só existe o **NH1** de cada habilidade (é o que o protótipo usa). Os níveis 2 a 5 estão só neste documento; a implementação está planejada na trilha de habilidades (`tasks/README.md`, T-060 a T-064).

## Como ler as tabelas

- **NH** = nível da habilidade (1–5; assinatura 1–3). Cada NH melhora a habilidade: dano/cura/escudo base, potência (duração, raio, alvos, acertos) ou tempo (recarga, conjuração).
- **Valor** = `base + coeficiente × atributo` (§5 do design de progressão). FOR, DES, INT e VIT são os atributos do personagem; ARMA é o dano da arma equipada.
- Regra dos números: em cada NH o valor do NH1 é multiplicado por **1,0 / 1,2 / 1,4 / 1,6 / 1,8** (assinatura **1,0 / 1,3 / 1,6**). Quando o NH aumenta o número de acertos (flechas, explosões, giros), **cada acerto** vale **85%** do NH1 no NH3 e no NH5 e **100%** no NH4 — no total 1,0 → 1,2 → 1,7 → 2,0 → 2,55 vezes o NH1.
- **Perfil nível 1** = o valor com os atributos e a arma do nível 1 (Guerreiro FOR 12 e espada 6; Arqueiro DES 12 e arco 6; Mago INT 13; Clérigo INT 12 e maça 5). Serve para comparar níveis; na prática o NH alto vem com o personagem mais forte (NH5 exige nível 9).
- Custo de Mana e recarga não mudam, salvo quando a coluna "Outros efeitos" diz.

## Guerreiro

Ataque básico (sem NH): **Golpe de Espada** — 1 × ARMA + 1 × FOR = 18, a cada 0,9 s.

### Golpe Pesado — dano alto e interrupção

Custo 30, recarga 6 s, conjuração 0,4 s, arco de 90° e alcance 1,9.

| NH | Acertos | Valor | Outros efeitos | Perfil nível 1 |
|---|---|---|---|---|
| 1 | 1 | dano 15 + 1 × ARMA + 2,5 × FOR | interrompe a conjuração do alvo | 51 |
| 2 | 1 | dano 18 + 1,2 × ARMA + 3 × FOR | interrompe | 61 |
| 3 | 1 | dano 21 + 1,4 × ARMA + 3,5 × FOR | interrompe e **atordoa 0,5 s** | 71 |
| 4 | 1 | dano 24 + 1,6 × ARMA + 4 × FOR | interrompe e atordoa 0,5 s | 82 |
| 5 | 1 | dano 27 + 1,8 × ARMA + 4,5 × FOR | interrompe, **atordoa 1 s** e o arco vai a **120°** | 92 |

### Provocação — atrai os monstros próximos

Sem custo. Monstros a até o raio passam a atacar o Guerreiro pela duração.

| NH | Efeito | Custo / recarga |
|---|---|---|
| 1 | raio 8, duração 3 s | 0 / 10 s |
| 2 | raio 8, duração **3,5 s** | 0 / 10 s |
| 3 | raio **10**, duração **4 s** | 0 / 10 s |
| 4 | raio 10, duração 4 s | 0 / **8 s** |
| 5 | raio 10, duração 4 s e o Guerreiro recebe **15% menos dano** por 4 s | 0 / 8 s |

### Golpe Circular — área curta em volta

Custo 25, recarga 5 s, raio 2,5. Cada giro também gera ameaça.

| NH | Giros | Valor | Outros efeitos | Perfil nível 1 |
|---|---|---|---|---|
| 1 | 1 | dano por giro 5 + 0,5 × ARMA + 1,6 × FOR | — | 27 |
| 2 | 1 | dano por giro 6 + 0,6 × ARMA + 1,92 × FOR | — | 33 |
| 3 | 2 | dano por giro 4 + 0,42 × ARMA + 1,36 × FOR | **2 giros** seguidos (0,3 s entre eles) | 2 × 23 = **46** |
| 4 | 2 | dano por giro 5 + 0,5 × ARMA + 1,6 × FOR | 2 giros | 2 × 27 = **54** |
| 5 | 3 | dano por giro 4 + 0,42 × ARMA + 1,36 × FOR | **3 giros** e aplica **Sangramento** (2 por segundo, 6 s, acumula até 3) | 3 × 23 = **69** |

### Guarda Firme — reduz o dano de frente, anda mais devagar

Sem custo. Reduz o dano recebido pela frente (arco de 180°).

| NH | Efeito | Custo / recarga |
|---|---|---|
| 1 | recebe **40%** do dano frontal (−60%) por 4 s; velocidade −50% | 0 / 14 s |
| 2 | recebe 40% por **5 s**; velocidade −50% | 0 / 14 s |
| 3 | recebe **35%** (−65%) por 5 s; velocidade só **−30%** | 0 / 14 s |
| 4 | recebe 35% por 5 s; velocidade −30% | 0 / **12 s** |
| 5 | recebe 35% por 5 s; velocidade −30%; **devolve 20%** do dano corpo a corpo bloqueado a quem atacou | 0 / 12 s |

### Muralha Viva (assinatura, nível 12) — escudo no grupo

Custo 80, recarga 90 s. Escudo de 6 s em todos os aliados no raio (inclui o Guerreiro).

| NH | Aplicações | Valor | Outros efeitos | Perfil nível 1 |
|---|---|---|---|---|
| 1 | 1 | escudo 40 + 2 × VIT, raio 5 | — | 60 |
| 2 | 1 | escudo 52 + 2,6 × VIT, raio 7 | — | 78 |
| 3 | 1 | escudo 64 + 3,2 × VIT, raio 7 | também **remove atordoamento e enraizamento** dos aliados | 96 |

## Arqueiro

Ataque básico (sem NH): **Disparo** — 1 × ARMA + 0,92 × DES = 17, a cada 1,0 s. (Corrigido na T-202: a calibração da T-031 é 17; com 1 × DES dava 18.)

### Disparo Potente — tiro forte em um alvo

Custo 25, recarga 7 s, alcance 9. As flechas extras vão no mesmo alvo (0,15 s entre elas); se ele morrer, seguem para o inimigo mais próximo.

| NH | Flechas | Valor | Outros efeitos | Perfil nível 1 |
|---|---|---|---|---|
| 1 | 1 | dano por flecha 10 + 1 × ARMA + 2 × DES | — | 40 |
| 2 | 1 | dano por flecha 12 + 1,2 × ARMA + 2,4 × DES | — | 48 |
| 3 | 2 | dano por flecha 8 + 0,85 × ARMA + 1,7 × DES | **2 flechas** | 2 × 34 = **67** |
| 4 | 2 | dano por flecha 10 + 1 × ARMA + 2 × DES | 2 flechas | 2 × 40 = **80** |
| 5 | 3 | dano por flecha 8 + 0,85 × ARMA + 1,7 × DES | **3 flechas** | 3 × 34 = **100** |

### Marca do Caçador — o alvo recebe mais dano de todos

Custo 10, recarga 12 s, alcance 10. O bônus vale para todo dano no alvo, inclusive do grupo.

| NH | Efeito | Custo / recarga |
|---|---|---|
| 1 | bônus 16% + 0,75% × DES (máx. 60%), 8 s — perfil nível 1: **+25%** | 10 / 12 s |
| 2 | bônus 16% + 0,75% × DES (máx. 60%), 10 s — perfil nível 1: **+25%** | 10 / 12 s |
| 3 | bônus 16% + 0,75% × DES (máx. 60%), 10 s; marca também o **inimigo mais próximo** do alvo — perfil nível 1: **+25%** | 10 / 12 s |
| 4 | bônus 21% + 0,75% × DES (máx. 65%), 10 s; 2 alvos — perfil nível 1: **+30%** | 10 / 12 s |
| 5 | bônus 21% + 0,75% × DES (máx. 65%), 10 s; 2 alvos e os marcados ficam **20% mais lentos** — perfil nível 1: **+30%** | 10 / 12 s |

### Recuo Rápido — cria distância

Sem custo. Salto para trás.

| NH | Efeito | Custo / recarga |
|---|---|---|
| 1 | salto de 5 tiles | 0 / 8 s |
| 2 | salto de 5 tiles | 0 / **7 s** |
| 3 | salto de **6 tiles**; o próximo Disparo ou Disparo Potente em até 2 s causa **+30%** | 0 / 7 s |
| 4 | 6 tiles e +30% | 0 / **6 s** |
| 5 | 6 tiles e +30%; **2 cargas** (pode recuar duas vezes seguidas) | 0 / 6 s |

### Chuva de Flechas — área com preparação

Custo 45, recarga 12 s, alcance 9. Telegráfo aliado de 0,6 s; depois as ondas caem a cada 0,5 s.

| NH | Ondas | Valor | Outros efeitos | Perfil nível 1 |
|---|---|---|---|---|
| 1 | 3 | dano por onda 10 + 1,4 × DES | raio 3 | 3 × 27 = **80** |
| 2 | 3 | dano por onda 12 + 1,68 × DES | raio 3 | 3 × 32 = **96** |
| 3 | 3 | dano por onda 14 + 1,96 × DES | raio **4** | 3 × 38 = **113** |
| 4 | 3 | dano por onda 16 + 2,24 × DES | raio 4 | 3 × 43 = **129** |
| 5 | 4 | dano por onda 18 + 2,52 × DES | **4 ondas**, raio 4 e **lentidão de 20%** na área | 4 × 48 = **193** |

### Flecha Devastadora (assinatura, nível 12) — disparo canalizado em linha

Custo 70, linha de 12 tiles. O Arqueiro fica parado canalizando; mover cancela.

| NH | Flechas | Valor | Outros efeitos | Perfil nível 1 |
|---|---|---|---|---|
| 1 | 1 | dano 40 + 1 × ARMA + 5 × DES | acerta o primeiro inimigo da linha; canalização 1,5 s; recarga 60 s | 106 |
| 2 | 1 | dano 52 + 1,3 × ARMA + 6,5 × DES | **atravessa todos** os inimigos da linha; canalização 1,5 s; recarga 60 s | 138 |
| 3 | 1 | dano 64 + 1,6 × ARMA + 8 × DES | atravessa todos; canalização **1,0 s**; recarga **45 s** | 170 |

## Mago

Ataque básico (sem NH, sem custo): **Projétil Arcano** — 3 + 0,9 × INT = 15, a cada 1,0 s.

### Explosão Elemental — área à distância, instantânea

Custo 80, recarga 8 s, alcance 9, raio 3. Os acertos extras caem no mesmo ponto (0,4 s entre eles).

| NH | Explosões | Valor | Outros efeitos | Perfil nível 1 |
|---|---|---|---|---|
| 1 | 1 | dano por explosão 18 + 2 × INT | — | 44 |
| 2 | 1 | dano por explosão 22 + 2,4 × INT | — | 53 |
| 3 | 2 | dano por explosão 15 + 1,7 × INT | **2 explosões** | 2 × 37 = **74** |
| 4 | 2 | dano por explosão 18 + 2 × INT | 2 explosões | 2 × 44 = **88** |
| 5 | 3 | dano por explosão 15 + 1,7 × INT | **3 explosões** | 3 × 37 = **111** |

### Prisão de Gelo — congela um alvo

Custo 25, recarga 10 s, alcance 8. Congelado = não anda e recebe +10% de dano.

| NH | Acertos | Valor | Outros efeitos | Perfil nível 1 |
|---|---|---|---|---|
| 1 | 1 | dano 3 + 0,45 × INT | congela **2,5 s** | 9 |
| 2 | 1 | dano 4 + 0,54 × INT | congela **3 s** | 11 |
| 3 | 1 | dano 4 + 0,63 × INT | congela 3 s; congela também inimigos a até **2 tiles** do alvo | 12 |
| 4 | 1 | dano 5 + 0,72 × INT | congela **3,5 s**, em área | 14 |
| 5 | 1 | dano 5 + 0,81 × INT | congela 3,5 s, em área; congelados recebem **+20%** de dano (em vez de +10%) | 16 |

### Barreira Arcana — escudo próprio

Custo 30, recarga 15 s, duração 6 s.

| NH | Alvos | Valor | Outros efeitos | Perfil nível 1 |
|---|---|---|---|---|
| 1 | 1 | escudo 19 + 2 × INT | — | 45 |
| 2 | 1 | escudo 23 + 2,4 × INT | — | 54 |
| 3 | 1 | escudo 27 + 2,8 × INT | ao acabar, devolve em **Mana 30%** do escudo que sobrou | 63 |
| 4 | 1 | escudo 30 + 3,2 × INT | devolve 30% em Mana | 72 |
| 5 | 1 | escudo 34 + 3,6 × INT | devolve 30% em Mana e protege também o **aliado mais próximo** (50% do valor) | 81 |

### Campo de Relâmpagos — zona de dano à distância

Custo 40, recarga 12 s, alcance 9, raio 3,5. Um relâmpago no impacto e depois um pulso por segundo na zona.

| NH | Pulsos | Valor | Outros efeitos | Perfil nível 1 |
|---|---|---|---|---|
| 1 | 5 | impacto 4 + 0,6 × INT; pulso 2 + 0,45 × INT | — | 12 + 5 × 8 = **51** |
| 2 | 5 | impacto 5 + 0,72 × INT; pulso 2 + 0,54 × INT | — | 14 + 5 × 9 = **59** |
| 3 | 7 | impacto 6 + 0,84 × INT; pulso 3 + 0,63 × INT | dura **7 s** | 17 + 7 × 11 = **95** |
| 4 | 7 | impacto 6 + 0,96 × INT; pulso 3 + 0,72 × INT | dura 7 s | 18 + 7 × 12 = **105** |
| 5 | 7 | impacto 7 + 1,08 × INT; pulso 4 + 0,81 × INT | dura 7 s e raio **4,5** | 21 + 7 × 15 = **123** |

### Cataclismo Arcano (assinatura, nível 12) — área grande com telegráfo

Custo 100, recarga 120 s, alcance 10, telegráfo aliado de 1,5 s.

| NH | Acertos | Valor | Outros efeitos | Perfil nível 1 |
|---|---|---|---|---|
| 1 | 1 | dano 60 + 6 × INT | raio 5 | 138 |
| 2 | 1 | dano 78 + 7,8 × INT | raio **6** | 179 |
| 3 | 1 | dano 96 + 9,6 × INT | raio 6 e deixa o **chão em chamas** por 4 s (10% do dano por segundo) | 221 |

## Clérigo

Ataque básico (sem NH): **Golpe Sagrado** — 1 × ARMA + 0,6 × INT = 12, a cada 1,1 s, +25% contra mortos-vivos e corrompidos.

### Cura Direta — cura um aliado

Custo 20, recarga 4 s, alcance 8.

| NH | Alvos | Valor | Outros efeitos | Perfil nível 1 |
|---|---|---|---|---|
| 1 | 1 | cura 26 + 2 × INT | conjuração 0,8 s | 50 |
| 2 | 1 | cura 31 + 2,4 × INT | conjuração 0,8 s | 60 |
| 3 | 1 | cura 36 + 2,8 × INT | conjuração **0,5 s** | 70 |
| 4 | 1 | cura 42 + 3,2 × INT | conjuração 0,5 s | 80 |
| 5 | 2 | cura 47 + 3,6 × INT | conjuração 0,5 s; cura também o **aliado mais ferido** próximo (50%) | 90 |

### Corrente Sagrada — dano e cura própria

Custo 25, recarga 6 s, alcance 7. +25% contra mortos-vivos e corrompidos. Os saltos vão para inimigos a até 4 tiles e causam 70%.

| NH | Alvos | Valor | Outros efeitos | Perfil nível 1 |
|---|---|---|---|---|
| 1 | 1 | dano 15 + 2 × INT; cura própria 10 | — | 39 |
| 2 | 1 | dano 18 + 2,4 × INT; cura própria 12 | — | 47 |
| 3 | 2 | dano 21 + 2,8 × INT; cura própria 14 | **salta** para um 2º inimigo | 55 + 1 × 38 = **93** |
| 4 | 2 | dano 24 + 3,2 × INT; cura própria 16 | salta para 2 inimigos | 62 + 1 × 44 = **106** |
| 5 | 3 | dano 27 + 3,6 × INT; cura própria 36 | salta para **3 inimigos**; cura própria **dobra** | 70 + 2 × 49 = **168** |

### Escudo Sagrado — escudo em um aliado

Custo 15, recarga 10 s, alcance 8, duração 6 s.

| NH | Alvos | Valor | Outros efeitos | Perfil nível 1 |
|---|---|---|---|---|
| 1 | 1 | escudo 21 + 2 × INT | — | 45 |
| 2 | 1 | escudo 25 + 2,4 × INT | — | 54 |
| 3 | 1 | escudo 29 + 2,8 × INT | também **remove um veneno** do alvo | 63 |
| 4 | 1 | escudo 34 + 3,2 × INT | remove um veneno | 72 |
| 5 | 2 | escudo 38 + 3,6 × INT | remove um veneno e protege também o **aliado mais ferido** | 81 |

### Purificação — remove veneno ou maldição

Sem custo, alcance 8.

| NH | Efeito | Custo / recarga |
|---|---|---|
| 1 | remove **1** efeito (veneno ou maldição) | 0 / 8 s |
| 2 | remove 1 efeito | 0 / **7 s** |
| 3 | remove **2** efeitos | 0 / 7 s |
| 4 | remove 2 efeitos | 0 / **6 s** |
| 5 | remove 2 efeitos, **cura 20 + 1 × INT** (32 no perfil nível 1) e dá **2 s de imunidade** a veneno e maldição | 0 / 6 s |

### Milagre (assinatura, nível 12) — cura grande no grupo

Custo 90, recarga 120 s. Cura todos os aliados a até 8 tiles (inclui o Clérigo).

| NH | Aplicações | Valor | Outros efeitos | Perfil nível 1 |
|---|---|---|---|---|
| 1 | 1 | cura 60 + 4 × INT | — | 108 |
| 2 | 1 | cura 78 + 5,2 × INT | — | 140 |
| 3 | 1 | cura 96 + 6,4 × INT | também **levanta um aliado caído** próximo com 30% da vida | 173 |

## Depois do MVP

Habilidades fora da barra de 5 teclas (especializações): Guerreiro — Investida, Interceptação; Arqueiro — Armadilha de Contenção, Flecha Perfurante, Tiro Debilitante; Mago — Combustão, Dissipar Magia; Clérigo — Bênção, Oração Restauradora. Recebem tabela de níveis quando entrarem no jogo.
