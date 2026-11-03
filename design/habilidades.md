# Habilidades — referência dos dados

Status: **referência gerada a partir dos dados** (2026-11-02). Fonte: `data/abilities_warrior.json`, `abilities_archer.json`, `abilities_mage.json`, `abilities_cleric.json`, `abilities_items.json`, `abilities_monsters.json`, `data/statuses.json`, `data/areas.json` e `data/classes.json`. Os números saem desses arquivos; quando os dados mudarem, este documento deve ser atualizado.

Complementa `design/skills-v0.1.md` (proposta e "perfil nível 1" de cada NH) e `design/progression-v0.1.md` (regras de atributos e NH). Monstros em `design/bestiario.md`; itens em `design/itens.md`.

## Como ler

- **Custo** em Mana; "—" = sem custo. Todas as classes têm 100 de Mana máxima e regeneram 5 por segundo (`classes.json`), além do que vem dos atributos.
- **Recarga** e **conjuração** em segundos; sem conjuração = instantânea.
- **Alcance, raio, comprimento e largura** em tiles.
- **Alvo** (`targeting`):
  - *arco corpo a corpo* (`melee_arc`): alcance e abertura total em graus, à frente de quem usa;
  - *alvo único* (`unit`): uma unidade inimiga ou aliada até o alcance;
  - *si mesmo* (`self`);
  - *círculo em volta* (`circle_self`): raio centrado em quem usa;
  - *círculo no ponto* (`circle_at_point`): alcance até o ponto mirado e raio do círculo;
  - *linha* (`line`): comprimento × largura a partir de quem usa.
- **Valor** = `base + coeficiente × atributo`. FOR, DES, INT e VIT são os atributos do personagem; ARMA é o dano da arma equipada (`weapon`).
- **Escolas de dano**: físico, natureza, arcano, fogo, gelo, sagrado.
- **NH** = nível da habilidade (1–5; assinatura 1–3). Num NH, os campos que não aparecem são **herdados do NH anterior** (regra do esquema). Hoje os dados já trazem os NH2 a NH5.
- **Acertos**: quando a habilidade tem vários acertos (`hits`), o valor indicado é **por acerto**.
- **Aviso** (`telegraph`): área desenhada no chão antes do efeito; o efeito sai depois do atraso indicado.
- Dano e cura dos monstros são do nível 1; crescem com o nível do monstro (veja `design/bestiario.md`).

## Guerreiro (`class.warrior`)

### Golpe de Espada — `warrior.sword_strike` (ataque básico)

Custo —, recarga 0,9 s. Arco corpo a corpo, alcance 1,7, 100°. Dano físico **0 + 1 × ARMA + 1 × FOR**. Sem NH.

### Golpe Pesado — `warrior.heavy_strike`

Custo 30, recarga 6 s, conjuração 0,4 s. Arco corpo a corpo, alcance 1,9, 90°. Tags: melee, interrupt.

| NH | Dano físico | Outros efeitos |
|---|---|---|
| 1 | 15 + 1 × ARMA + 2,5 × FOR | interrompe a conjuração; +20 de ameaça |
| 2 | 18 + 1,2 × ARMA + 3 × FOR | interrompe; +20 de ameaça |
| 3 | 21 + 1,4 × ARMA + 3,5 × FOR | interrompe; **Atordoado** 0,5 s; +20 de ameaça |
| 4 | 24 + 1,6 × ARMA + 4 × FOR | interrompe; Atordoado 0,5 s; +20 de ameaça |
| 5 | 27 + 1,8 × ARMA + 4,5 × FOR | interrompe; **Atordoado 1 s**; +20 de ameaça; arco passa a **120°** |

### Golpe Circular — `warrior.whirlwind`

Custo 25, recarga 5 s. Círculo em volta, raio 2,5. Tags: melee, aoe.

| NH | Giros | Dano físico por giro | Outros efeitos |
|---|---|---|---|
| 1 | 1 | 5 + 0,5 × ARMA + 1,6 × FOR | +30 de ameaça |
| 2 | 1 | 6 + 0,6 × ARMA + 1,92 × FOR | +30 de ameaça |
| 3 | 2 (0,3 s entre giros) | 4 + 0,42 × ARMA + 1,36 × FOR | +30 de ameaça |
| 4 | 2 (herdado) | 5 + 0,5 × ARMA + 1,6 × FOR | +30 de ameaça |
| 5 | 3 (0,3 s) | 4 + 0,42 × ARMA + 1,36 × FOR | +30 de ameaça; aplica **Sangramento** (sem duração no efeito: usa a padrão, 6 s) |

### Guarda Firme — `warrior.firm_guard`

Custo —, recarga 14 s. Em si mesmo. Tag: defensive.

| NH | Efeito | Recarga |
|---|---|---|
| 1 | **Guarda Firme** (`status.firm_guard`) por 4 s | 14 s |
| 2 | Guarda Firme por 5 s | 14 s |
| 3 | Guarda Firme reforçada (`status.firm_guard_strong`) por 5 s | 14 s |
| 4 | igual ao NH3 | **12 s** |
| 5 | Guarda Firme com reflexo (`status.firm_guard_reflect`) por 5 s | 12 s |

### Provocação — `warrior.taunt`

Custo —, recarga 10 s. Círculo em volta, raio 8. Tags: taunt, aoe. Gera 40 de ameaça e força os monstros no raio a atacar o Guerreiro pela duração da provocação.

| NH | Raio | Duração da provocação | Outros efeitos | Recarga |
|---|---|---|---|---|
| 1 | 8 | 3 s | — | 10 s |
| 2 | 8 | 3,5 s | — | 10 s |
| 3 | **10** | 4 s | — | 10 s |
| 4 | 10 | 4 s | — | **8 s** |
| 5 | 10 | 4 s | o Guerreiro recebe **Determinação** por 4 s | 8 s |

### Muralha Viva — `warrior.living_wall` (assinatura)

Custo 80, recarga 90 s. Círculo em volta, raio 5. Tags: defensive, group, signature.

| NH | Raio | Escudo (dura 6 s) | Outros efeitos |
|---|---|---|---|
| 1 | 5 | 40 + 2 × VIT | — |
| 2 | **7** | 52 + 2,6 × VIT | — |
| 3 | 7 | 64 + 3,2 × VIT | remove **todos** os status com tag stun, root ou freeze |

## Arqueiro (`class.archer`)

### Disparo — `archer.shot` (ataque básico)

Custo —, recarga 1 s. Alvo único inimigo, alcance 9. Dano físico **0 + 1 × ARMA + 0,92 × DES**. Sem NH.

### Disparo Potente — `archer.power_shot`

Custo 25, recarga 7 s. Alvo único inimigo, alcance 9. Tags: ranged, damage.

| NH | Acertos | Dano físico por acerto |
|---|---|---|
| 1 | 1 | 10 + 1 × ARMA + 2 × DES |
| 2 | 1 | 12 + 1,2 × ARMA + 2,4 × DES |
| 3 | 2 (0,15 s entre eles) | 8 + 0,85 × ARMA + 1,7 × DES |
| 4 | 2 (herdado) | 10 + 1 × ARMA + 2 × DES |
| 5 | 3 (0,15 s) | 8 + 0,85 × ARMA + 1,7 × DES |

### Marca do Caçador — `archer.hunters_mark`

Custo 10, recarga 12 s. Alvo único inimigo, alcance 10. Tags: ranged, debuff.

| NH | Status no alvo | Alvos extras |
|---|---|---|
| 1 | **Marca do Caçador** (`status.hunters_mark`) por 8 s | — |
| 2 | Marca do Caçador por 10 s | — |
| 3 | Marca do Caçador por 10 s | 1 inimigo a até 5 do alvo recebe a mesma marca |
| 4 | Marca reforçada (`status.hunters_mark_strong`) por 10 s | 1 extra, raio 5 |
| 5 | Marca que torna lento (`status.hunters_mark_slowing`) por 10 s | 1 extra, raio 5 |

Os dados não informam `count` nos alvos extras; o padrão do código é **1 alvo** (o mais próximo). Ver "Pontos em aberto".

### Recuo Rápido — `archer.quick_retreat`

Custo —, recarga 8 s. Em si mesmo. Tag: mobility. Salto (dash) para trás.

| NH | Distância | Outros efeitos | Recarga |
|---|---|---|---|
| 1 | 5 | — | 8 s |
| 2 | 5 | — | **7 s** |
| 3 | **6** | **Foco do Recuo** em si por 2 s | 7 s |
| 4 | 6 | Foco do Recuo | **6 s** |
| 5 | 6 | Foco do Recuo | 6 s, **2 cargas** que recarregam uma a uma |

### Chuva de Flechas — `archer.rain_of_arrows`

Custo 45, recarga 12 s. Círculo no ponto, alcance 9, raio 3; aviso de 0,6 s. Tags: ranged, aoe.

| NH | Raio | Acertos (0,5 s entre eles) | Dano físico por acerto | Outros efeitos |
|---|---|---|---|---|
| 1 | 3 | 3 | 10 + 1,4 × DES | — |
| 2 | 3 | 3 | 12 + 1,68 × DES | — |
| 3 | **4** | 3 | 14 + 1,96 × DES | — |
| 4 | 4 | 3 | 16 + 2,24 × DES | — |
| 5 | 4 | **4** | 18 + 2,52 × DES | no **primeiro acerto** cria a área `area.rain_slow` (Lentidão) |

### Flecha Devastadora — `archer.devastating_arrow` (assinatura)

Custo 70, recarga 60 s, conjuração 1,5 s. Tags: ranged, channel, signature.

| NH | Alvo | Dano físico | Conjuração / recarga |
|---|---|---|---|
| 1 | alvo único inimigo, alcance 12 | 40 + 1 × ARMA + 5 × DES | 1,5 s / 60 s |
| 2 | **linha 12 × 1,2** (atravessa) | 52 + 1,3 × ARMA + 6,5 × DES | 1,5 s / 60 s |
| 3 | linha 12 × 1,2 | 64 + 1,6 × ARMA + 8 × DES | **1 s / 45 s** |

### Armadilha de Contenção — `archer.containment_trap`

Custo 25, recarga 14 s. Círculo no ponto, alcance 8, raio 1,2. Tags: control, trap. Cria a área `area.containment_trap`: fica 30 s no chão, arma em 0,5 s e dispara **uma vez** quando um inimigo entra: dano físico 8 e **Preso** por 3 s. Sem NH.

**Atenção:** a habilidade existe nos dados, mas **não está na lista de habilidades do Arqueiro** em `classes.json` (lá estão Disparo Potente, Marca do Caçador, Recuo Rápido, Chuva de Flechas e Flecha Devastadora).

## Mago (`class.mage`)

### Projétil Arcano — `mage.arcane_missile` (ataque básico)

Custo —, recarga 1 s. Alvo único inimigo, alcance 9. Dano arcano **3 + 0,9 × INT**. Sem NH.

### Explosão Elemental — `mage.elemental_blast`

Custo 90, recarga 8 s. Círculo no ponto, alcance 9, raio 3. Tags: aoe, ranged.

| NH | Acertos | Dano de fogo por acerto |
|---|---|---|
| 1 | 1 | 18 + 2 × INT |
| 2 | 1 | 22 + 2,4 × INT |
| 3 | 2 (0,4 s entre eles) | 15 + 1,7 × INT |
| 4 | 2 (herdado) | 18 + 2 × INT |
| 5 | 3 (0,4 s) | 15 + 1,7 × INT |

### Prisão de Gelo — `mage.ice_prison`

Custo 25, recarga 10 s. Alvo único inimigo, alcance 8. Tags: control, ranged.

| NH | Dano de gelo | Status no alvo | Em volta do alvo |
|---|---|---|---|
| 1 | 3 + 0,45 × INT | **Congelado** (`status.frozen`) 2,5 s | — |
| 2 | 4 + 0,54 × INT | Congelado 3 s | — |
| 3 | 4 + 0,63 × INT | Congelado 3 s | **todos** os inimigos a até 2: Congelado 3 s |
| 4 | 5 + 0,72 × INT | Congelado 3,5 s | todos a até 2: Congelado 3,5 s |
| 5 | 5 + 0,81 × INT | **Congelado profundo** (`status.frozen_deep`) 3,5 s | todos a até 2: Congelado profundo 3,5 s |

### Barreira Arcana — `mage.arcane_barrier`

Custo 30, recarga 15 s. Em si mesmo. Tag: defensive. Escudo de 6 s.

| NH | Escudo | Outros efeitos |
|---|---|---|
| 1 | 19 + 2 × INT | — |
| 2 | 23 + 2,4 × INT | — |
| 3 | 27 + 2,8 × INT | ao expirar com sobra, devolve 30% da sobra em Mana |
| 4 | 30 + 3,2 × INT | devolve 30% da sobra em Mana |
| 5 | 34 + 3,6 × INT | devolve 30% da sobra; 1 aliado a até 8 recebe o mesmo escudo com **50%** da potência (sem devolução) |

### Campo de Relâmpagos — `mage.lightning_field`

Custo 40, recarga 12 s. Círculo no ponto, alcance 9, raio 3,5. Tags: aoe, ranged, zone. Causa dano arcano no impacto e deixa uma área que causa dano arcano a cada 1 s.

| NH | Raio | Dano arcano no impacto | Área | Dano da área por segundo | Duração da área |
|---|---|---|---|---|---|
| 1 | 3,5 | 4 + 0,6 × INT | `area.lightning_field` | 8 | 5 s |
| 2 | 3,5 | 5 + 0,72 × INT | `area.lightning_field_2` | 9 | 5 s |
| 3 | 3,5 | 6 + 0,84 × INT | `area.lightning_field_3` | 11 | 7 s |
| 4 | 3,5 | 6 + 0,96 × INT | `area.lightning_field_4` | 12 | 7 s |
| 5 | **4,5** | 7 + 1,08 × INT | `area.lightning_field_5` (raio 4,5) | 15 | 7 s |

### Cataclismo Arcano — `mage.arcane_cataclysm` (assinatura)

Custo 100, recarga 120 s. Círculo no ponto, alcance 10, raio 5; aviso de 1,5 s. Tags: aoe, ranged, signature.

| NH | Raio | Dano arcano | Outros efeitos |
|---|---|---|---|
| 1 | 5 | 60 + 6 × INT | — |
| 2 | **6** | 78 + 7,8 × INT | — |
| 3 | 6 | 96 + 9,6 × INT | cria `area.cataclysm_fire`: raio 6, 4 s, dano de fogo 22 por segundo |

## Clérigo (`class.cleric`)

### Golpe Sagrado — `cleric.holy_strike` (ataque básico)

Custo —, recarga 1,1 s. Alvo único inimigo, alcance 6. Dano sagrado **0 + 1 × ARMA + 0,6 × INT**, **+25%** contra alvos com tag undead e +25% contra corrupted (os bônus somam). Sem NH.

### Cura Direta — `cleric.direct_heal`

Custo 20, recarga 4 s, conjuração 0,8 s. Alvo único aliado, alcance 8. Tag: heal.

| NH | Cura | Conjuração | Outros efeitos |
|---|---|---|---|
| 1 | 26 + 2 × INT | 0,8 s | — |
| 2 | 31 + 2,4 × INT | 0,8 s | — |
| 3 | 36 + 2,8 × INT | **0,5 s** | — |
| 4 | 42 + 3,2 × INT | 0,5 s | — |
| 5 | 47 + 3,6 × INT | 0,5 s | o aliado **mais ferido** a até 8 do alvo também é curado com 50% da potência |

### Escudo Sagrado — `cleric.sacred_shield`

Custo 15, recarga 10 s. Alvo único aliado, alcance 8. Tags: defensive, preventive. Escudo de 6 s.

| NH | Escudo | Outros efeitos |
|---|---|---|
| 1 | 21 + 2 × INT | — |
| 2 | 25 + 2,4 × INT | — |
| 3 | 29 + 2,8 × INT | remove 1 status com tag poison |
| 4 | 34 + 3,2 × INT | remove 1 poison |
| 5 | 38 + 3,6 × INT | remove 1 poison; o aliado mais ferido a até 8 recebe o mesmo escudo (potência cheia) |

### Corrente Sagrada — `cleric.holy_chain`

Custo 25, recarga 6 s. Alvo único inimigo, alcance 7. Tags: holy, ranged. O dano sagrado tem +25% contra undead e corrupted, também nos saltos.

| NH | Dano sagrado | Cura em si | Saltos (raio 4, 70% da potência) |
|---|---|---|---|
| 1 | 15 + 2 × INT | 10 | — |
| 2 | 18 + 2,4 × INT | 12 | — |
| 3 | 21 + 2,8 × INT | 14 | 1 salto |
| 4 | 24 + 3,2 × INT | 16 | 1 salto |
| 5 | 27 + 3,6 × INT | **36** | **2 saltos** |

### Purificação — `cleric.purify`

Custo —, recarga 8 s. Alvo único aliado, alcance 8. Tags: cleanse, holy. Remove status com tag poison ou curse.

| NH | Status removidos | Outros efeitos | Recarga |
|---|---|---|---|
| 1 | 1 | — | 8 s |
| 2 | 1 | — | **7 s** |
| 3 | **2** | — | 7 s |
| 4 | 2 | — | **6 s** |
| 5 | 2 | cura 20 + 1 × INT e aplica **Purificado** por 2 s | 6 s |

### Milagre — `cleric.miracle` (assinatura)

Custo 90, recarga 120 s. Círculo em volta, raio 8. Tags: heal, group, signature.

| NH | Cura | Outros efeitos |
|---|---|---|
| 1 | 60 + 4 × INT | — |
| 2 | 78 + 5,2 × INT | — |
| 3 | 96 + 6,4 × INT | levanta aliados caídos a até 8 do Clérigo com 30% da vida máxima |

## Itens (`abilities_items.json`)

### Poção de Cura Menor — `item.minor_healing_potion`

Custo —, recarga 20 s, **3 cargas**. Em si mesmo. Tags: consumable, heal. Cura **60** (sem atributo). Todas as classes a têm (`consumables` em `classes.json`). Mais em `design/itens.md`.

## Monstros (`abilities_monsters.json`)

Valores do nível 1. Sem custo de Mana. As tags `ai.*` dizem à IA para que serve a habilidade (veja `design/bestiario.md`).

| Habilidade | Id | Monstro | Recarga | Alvo | Efeitos |
|---|---|---|---|---|---|
| Mordida de Rato | `field_rat.bite` | Rato de Campo | 1 s | arco 1,1 / 90° | dano físico 3 |
| Mordida do Lobo | `grey_wolf.bite` | Lobo Cinzento | 1,4 s | arco 1,3 / 80° | dano físico 12 |
| Fúria da Matilha | `grey_wolf.pack_fury` | Lobo Cinzento | 20 s | si mesmo | **Fúria da Matilha** por 8 s (tag `ai.rally`) |
| Disparo de Besta | `road_bandit.shot` | Salteador da Estrada | 2 s | alvo único inimigo, 9 | dano físico 20 |
| Golpe de Lâmina | `skeleton_guard.slash` | Guarda Esquelético | 1,6 s | arco 1,8 / 90° | dano físico 9 |
| Erguer Escudo | `skeleton_guard.raise_shield` | Guarda Esquelético | 8 s | si mesmo | **Escudo Erguido** por 3 s (tag `ai.guard`) |
| Golpe Esmagador | `skeleton_guard.crushing_blow` | Guarda Esquelético | 8 s | arco 2,4 / 100°; aviso em cone 2,4 × 100°, 1 s | dano físico 30 (tag `ai.heavy`) |
| Picada da Aranha | `spider_small.bite` | Aranha Menor | 1,2 s | arco 1,1 / 90° | dano físico 9; **Veneno** 4 s |
| Picada da Aranha Grande | `spider_big.bite` | Aranha Grande | 1,5 s | arco 1,6 / 100° | dano físico 26; **Veneno** 5 s |
| Mordida da Matriarca | `matriarch.bite` | Matriarca | 1,5 s | arco 2,3 / 120° | dano físico 62 |
| Mordida Dilacerante | `matriarch.rending_bite` | Matriarca | 1,5 s | arco 2,3 / 120° | dano físico 62; **Sangramento** 6 s |
| Investida | `matriarch.charge` | Matriarca | 9 s | linha 6 × 1,6; aviso de 1 s; avança pela linha | dano físico 130 |
| Poça de Veneno | `matriarch.venom_pool` | Matriarca | 12 s | círculo no ponto, alcance 12, raio 2; aviso de 1 s | cria `area.venom_pool` (tag `ai.area`) |
| Poça de Veneno Rápida | `matriarch.venom_pool_fast` | Matriarca | 6 s | igual à Poça de Veneno | cria `area.venom_pool` (tag `ai.area`) |
| Teia Sufocante | `matriarch.web_field` | Matriarca | 14 s | círculo no ponto, alcance 12, raio 3; sem aviso | cria `area.sticky_web` (tag `ai.area`) |
| Chamar Aranhas | `matriarch.summon_lesser` | Matriarca | 18 s | si mesmo | invoca 2 Aranhas Menores em raio 2, no máximo 4 vivas (tag `ai.summon`) |

## Status (`statuses.json`)

"Renova" = aplicar de novo reinicia a duração (`refresh`); "acumula" = soma acúmulos até o máximo (`stack`). Multiplicadores: × 0,5 de velocidade = metade da velocidade; × 1,25 de dano recebido = 25% a mais.

| Status | Id | Tipo | Duração padrão | Acúmulo | Efeito | Aplicado por |
|---|---|---|---|---|---|---|
| Veneno | `status.poison` | debuff (poison, dot) | 5 s | renova | dano de natureza 7 por segundo | Picada da Aranha, Picada da Aranha Grande, `area.venom_pool` |
| Sangramento | `status.bleed` | debuff (bleed, dot) | 6 s | acumula até 3 | dano físico 2 por segundo | Golpe Circular NH5, Mordida Dilacerante |
| Teia | `status.web` | debuff (slow) | 1,5 s | renova | velocidade × 0,5 | `area.sticky_web` |
| Lentidão | `status.slow` | debuff (slow, control) | 3 s | renova | velocidade × 0,6 | **nenhuma habilidade ou área atual** |
| Congelado | `status.frozen` | debuff (control, freeze) | 2,5 s | renova | preso no lugar; dano recebido × 1,1 | Prisão de Gelo NH1–4 |
| Preso | `status.snared` | debuff (control, root) | 3 s | renova | preso no lugar | `area.containment_trap` |
| Marca do Caçador | `status.hunters_mark` | debuff (mark) | 8 s | renova | dano recebido × 1,25 | Marca do Caçador NH1–3 |
| Guarda Firme | `status.firm_guard` | buff (guard) | 4 s | renova | dano vindo da frente (180°) × 0,4; velocidade × 0,5 | Guarda Firme NH1–2 |
| Escudo Erguido | `status.skeleton_guard` | buff (monster_guard) | 3 s | renova | dano vindo da frente (140°) × 0,3 | Erguer Escudo |
| Fúria da Matilha | `status.pack_aggression` | buff (pack) | 8 s | renova | dano causado × 1,3; velocidade × 1,15 | Fúria da Matilha |
| Frenesi | `status.frenzy` | buff (frenzy) | 3600 s | renova | velocidade × 1,35; dano causado × 1,15; dano recebido × 1,2; duração de controle recebido × 0,5 | fase Frenesi da Matriarca |
| Atordoado | `status.stunned` | debuff (control, stun) | 0,5 s | renova | atordoado (não age) | Golpe Pesado NH3–5 |
| Congelado (profundo) | `status.frozen_deep` | debuff (control, freeze) | 3,5 s | renova | preso no lugar; dano recebido × 1,2 | Prisão de Gelo NH5 |
| Marca do Caçador (reforçada) | `status.hunters_mark_strong` | debuff (mark) | 10 s | renova | dano recebido × 1,3 | Marca do Caçador NH4 |
| Marca do Caçador (lenta) | `status.hunters_mark_slowing` | debuff (mark, slow) | 10 s | renova | dano recebido × 1,3; velocidade × 0,8 | Marca do Caçador NH5 |
| Guarda Firme (reforçada) | `status.firm_guard_strong` | buff (guard) | 5 s | renova | dano vindo da frente (180°) × 0,35; velocidade × 0,7 | Guarda Firme NH3–4 |
| Guarda Firme (reflexo) | `status.firm_guard_reflect` | buff (guard) | 5 s | renova | como a reforçada e devolve 20% do dano corpo a corpo bloqueado a quem atacou | Guarda Firme NH5 |
| Determinação | `status.taunt_resolve` | buff (guard) | 4 s | renova | dano recebido × 0,85 | Provocação NH5 |
| Foco do Recuo | `status.retreat_focus` | buff (focus) | 2 s | renova | o próximo Disparo ou Disparo Potente sai com potência × 1,3; é consumido no uso | Recuo Rápido NH3–5 |
| Lentidão (chuva) | `status.rain_slow` | debuff (slow) | 1 s | renova | velocidade × 0,8 | `area.rain_slow` |
| Purificado | `status.purified` | buff (cleanse) | 2 s | renova | imune a status com tag poison ou curse | Purificação NH5 |

Os nomes entre parênteses distinguem variantes que, nos dados, têm o mesmo nome exibido (ex.: as três "Marca do Caçador").

Efeitos de controle (tag control) duram menos em monstros elite (× 0,5) e chefes (× 0,25), e sofrem retorno decrescente: dentro de 15 s, a 1ª, 2ª, 3ª e 4ª aplicação duram × 1,0 / 0,5 / 0,25 / 0 (`rules.json` → `control`).

## Áreas (`areas.json`)

Áreas criadas por habilidades. Nenhuma tem nome exibido nos dados; os nomes abaixo vêm da habilidade que a cria. Todas afetam **inimigos** de quem a criou.

| Área | Id | Raio | Duração | Disparo | Efeitos | Criada por |
|---|---|---|---|---|---|---|
| Armadilha de Contenção | `area.containment_trap` | 1,2 | 30 s (arma em 0,5 s) | uma vez, ao entrar | dano físico 8; Preso 3 s | Armadilha de Contenção |
| Poça de Veneno | `area.venom_pool` | 2 | 8 s | a cada 1 s | dano de natureza 8; Veneno 4 s | Poça de Veneno, Poça de Veneno Rápida |
| Teia Sufocante | `area.sticky_web` | 3 | 18 s | a cada 0,5 s | Teia 1,5 s | Teia Sufocante |
| Campo de Relâmpagos NH1 | `area.lightning_field` | 3,5 | 5 s | a cada 1 s | dano arcano 8 | Campo de Relâmpagos NH1 |
| Campo de Relâmpagos NH2 | `area.lightning_field_2` | 3,5 | 5 s | a cada 1 s | dano arcano 9 | Campo de Relâmpagos NH2 |
| Campo de Relâmpagos NH3 | `area.lightning_field_3` | 3,5 | 7 s | a cada 1 s | dano arcano 11 | Campo de Relâmpagos NH3 |
| Campo de Relâmpagos NH4 | `area.lightning_field_4` | 3,5 | 7 s | a cada 1 s | dano arcano 12 | Campo de Relâmpagos NH4 |
| Campo de Relâmpagos NH5 | `area.lightning_field_5` | 4,5 | 7 s | a cada 1 s | dano arcano 15 | Campo de Relâmpagos NH5 |
| Chuva de Flechas (lentidão) | `area.rain_slow` | 4 | 2 s | a cada 0,5 s | Lentidão (chuva) 1 s | Chuva de Flechas NH5 |
| Fogo do Cataclismo | `area.cataclysm_fire` | 6 | 4 s | a cada 1 s | dano de fogo 22 | Cataclismo Arcano NH3 |

## Pontos em aberto nos dados

- **Armadilha de Contenção** está em `abilities_archer.json`, mas fora da lista de habilidades do Arqueiro em `classes.json`; hoje nenhum personagem a usa.
- **Lentidão** (`status.slow`) não é aplicada por nenhuma habilidade nem área atual.
- **Alvos extras sem `count`** (Marca do Caçador NH3–5, Barreira Arcana NH5, Cura Direta NH5, Escudo Sagrado NH5): o esquema não define o padrão; o código (`src/Game.Core/Definitions/Effects.cs`) usa **1 alvo**, escolha **o mais próximo** e potência **× 1** quando os campos faltam. Se a intenção era "todos no raio", o dado precisa de `count: 0`.
- **Sangramento do Golpe Circular NH5** não informa duração; vale a padrão do status (6 s).
- **Milagre NH3**: o efeito de levantar aliados não informa quantos (`count`); o documento assume "os caídos no raio", sem limite confirmado nos dados.
- **Muralha Viva e Milagre** usam círculo em volta sem dizer a facção; pelo tipo de efeito (escudo, cura) e pela tag group, valem para aliados.
- **Dano das áreas** (Campo de Relâmpagos, Fogo do Cataclismo, Poça de Veneno) é fixo nos dados, sem coeficiente de atributo.
- **Poção de Cura Menor**: o esquema descreve `charges` como "usos por encontro"; no mundo aberto (zonas) o momento em que as cargas voltam não está nos dados.
