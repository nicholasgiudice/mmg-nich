# Bestiário

Status: **referência gerada a partir dos dados** (2026-11-02; Fada da Névoa e Ent Corrompido em 2026-11-06, T-340; Sacerdote Morto e Cavaleiro Espectral em 2026-11-09, T-341; Vigia Sem Rosto, Sombra e Selo do Juramento em 2026-11-10, T-351; Afogado, Homem-lagarto, Serpente do Charco e Bruxa do Brejo em 2026-11-24, T-510; Mineiro Possuído, Lobo da Montanha, Morcego das Cavernas e Golem de Pedra em 2026-11-24, T-511; Capataz Forjado em 2026-11-24, T-512). Fonte: `data/monsters.json`, `data/abilities_monsters.json`, `data/statuses.json`, `data/areas.json`, `data/zones.json`, `data/encounters.json`, `data/rules.json` e `data/items.json` (tabelas de queda). Os números saem desses arquivos; quando os dados mudarem, este documento deve ser atualizado.

As habilidades dos monstros estão detalhadas em `design/habilidades.md` (§ Monstros) e os itens em `design/itens.md`.

## Como ler

- **Vida** e dano das habilidades são os valores do **nível 1**. Pelo esquema de dados, os números de `monsters.json` valem para o nível 1 e crescem com o nível em que o monstro nasce (`rules.json`, seção `monsters`):
  - vida × (1 + **0,4** × (nível − 1)); potência (dano) × (1 + **0,17** × (nível − 1));
  - **chefe**: vida × (1 + **0,3** × (nível − 1)); potência × (1 + **0,38** × (nível − 1));
  - **elite** não tem crescimento próprio em `rules.json`, então usa o mesmo dos monstros normais.
- **Nível** = o campo `level` do monstro. Ele é o nível padrão: vale quando o ponto de nascimento (zona) ou o encontro não define outro. O nível define o XP ao matar e o nível do atacante no cálculo de defesa.
- **Velocidade** em tiles por segundo; **raio** (tamanho do corpo) em tiles.
- **Categoria** (`rank`): normal (sem o campo), elite ou chefe. Ela escolhe a tabela de queda e reduz a duração dos efeitos de controle recebidos (`rules.json` → `control.durationByRank`): normal **× 1,0**, elite **× 0,5**, chefe **× 0,25**.
- **Armadura e resistência**: nenhum monstro define `armor` ou `resistance` em `monsters.json` (campos opcionais do esquema). Não há defesa extra por esses campos hoje.
- **Resistências por escola** (`resistances`): fração do dano cortada; negativa = **fraqueza** (−0,3 = recebe 130%). O Ent Corrompido (físico 0,25; fogo −0,3), o Afogado (sagrado −0,4) e o Golem de Pedra (físico 0,3; arcano −0,3) definem.
- **Escolas de dano**: físico (`physical`), natureza (`nature`), arcano (`arcane`), fogo (`fire`; a Fornalha e a Escória do Capataz Forjado).

## Resumo

| Monstro | Id | Nível | Categoria | Vida (nv. 1) | Velocidade | Raio | Tags | IA |
|---|---|---|---|---|---|---|---|---|
| Rato de Campo | `monster.field_rat` | 1 | normal | 150 | 4,0 | 0,3 | beast, vermin | `ai.swarm` |
| Lobo Cinzento | `monster.grey_wolf` | 2 | normal | 220 | 4,0 | 0,4 | beast | `ai.pack` |
| Salteador da Estrada | `monster.road_bandit` | 3 | normal | 80 | 4,0 | 0,4 | humanoid | `ai.ranged_kiter` |
| Aranha Menor | `monster.spider_small` | 5 | normal | 70 | 4,0 | 0,35 | beast, spider | `ai.melee_chaser` |
| Aranha Grande | `monster.spider_big` | 6 | **elite** | 400 | 3,4 | 0,6 | beast, spider | `ai.melee_chaser` |
| Fada da Névoa | `monster.mist_fairy` | 6 | normal | 110 | 3,4 | 0,38 | fey | `ai.mist_kiter` |
| Ent Corrompido | `monster.corrupted_ent` | 7 | normal | 430 | 2,4 | 0,7 | plant, corrupted | `ai.brute` |
| Matriarca da Floresta | `monster.forest_matriarch` | 8 | **chefe** | 8800 | 3,4 | 0,9 | beast, spider, boss | `ai.boss` |
| Guarda Esquelético | `monster.skeleton_guard` | 9 | normal | 440 | 3,2 | 0,5 | undead | `ai.guardian` |
| Sacerdote Morto | `monster.dead_priest` | 10 | normal | 180 | 3,0 | 0,45 | undead | `ai.support` |
| Cavaleiro Espectral | `monster.spectral_knight` | 11 | **elite** | 4500 | 3,4 | 0,5 | undead | `ai.brute` |
| Sombra | `monster.shade` | 14 | normal | 150 | 3,6 | 0,4 | undead | `ai.melee_chaser` |
| Vigia Sem Rosto | `monster.faceless_watcher` | 15 | **chefe** | 45000 | 3,4 | 0,9 | undead, boss | `ai.boss` |
| Selo do Juramento (objetivo) | `monster.watcher_seal` | 15 | normal | 380 | 0 | 0,6 | seal | `ai.none` |
| Afogado | `monster.drowned` | 16 | normal | 300 | 2,6 | 0,4 | undead | `ai.brute` |
| Homem-lagarto do Brejo | `monster.marsh_lizardman` | 17 | normal | 440 | 4,0 | 0,4 | humanoid, amphibious | `ai.brute` |
| Serpente do Charco | `monster.marsh_serpent` | 18 | normal | 200 | 3,0 | 0,45 | beast | `ai.ranged_kiter` |
| Bruxa do Brejo | `monster.marsh_witch` | 20 | **chefe** | 5500 | 3,2 | 0,45 | humanoid, boss | `ai.boss` |
| Mineiro Possuído | `monster.possessed_miner` | 21 | normal | 320 | 3,2 | 0,4 | humanoid | `ai.brute` |
| Lobo da Montanha | `monster.mountain_wolf` | 22 | normal | 260 | 4,2 | 0,4 | beast | `ai.pack` |
| Morcego das Cavernas | `monster.cave_bat` | 22 | normal | 90 | 4,4 | 0,3 | beast | `ai.swarm` |
| Golem de Pedra | `monster.stone_golem` | 25 | **elite** | 1200 | 2,2 | 0,7 | construct | `ai.brute` |
| Capataz Forjado | `monster.forged_overseer` | 27 | **chefe** | 70000 | 3,0 | 0,9 | construct, boss | `ai.boss` |

## Quedas (drops)

Nenhum monstro tem tabela própria: a queda depende só da categoria (`data/items.json` → `rules.items.drops`). As três tabelas usam a mesma lista de 21 itens base (todas as armas, armaduras e acessórios de `design/itens.md`), sorteados com o mesmo peso. O **nível do item** é o nível do monstro e os itens só caem para quem participou da morte. Cada item tem um dono escolhido entre os participantes, que pega sozinho nos primeiros 30 s; depois, qualquer jogador pega (`design/itens.md`, T-394).

| Categoria | Chance de queda | Itens | Raridade (comum / incomum / raro / épico) | Garantia |
|---|---|---|---|---|
| normal | 8% | 1 | 70 / 25 / 5 / 0 | — |
| elite | 40% | 1 | 40 / 45 / 15 / 0 | — |
| chefe | 100% | 2 | 0 / 60 / 35 / 5 | o primeiro item sai pelo menos **incomum** |

Os números de raridade são pesos relativos. Nenhum monstro deixa cair a Poção de Cura Menor (ela vem com a classe; veja `design/itens.md`).

## Onde aparecem

### Mundo aberto (`data/zones.json`)

**Cidade-Fortaleza** (`zone.city`) é zona segura, sem monstros.

**Campos da Vigília** (`zone.vigil_fields`): corpos ficam 5 s no chão.

| Subárea | Monstro | Quantidade | Nível | Renasce em |
|---|---|---|---|---|
| Estrada Leste | Rato de Campo | 2 (ponto 16,5; 36,5) | 1 | 40 s |
| Estrada Leste | Rato de Campo | 2 (ponto 36,5; 30,5) | 1 | 40 s |
| Fazenda Abandonada | Rato de Campo | 2 (ponto 27,5; 16,5) | 1 | 45 s |
| Fazenda Abandonada | Lobo Cinzento | 1 (ponto 28,5; 14,5) | 2 | 45 s |
| Fazenda Abandonada | Lobo Cinzento | 2 (ponto 42,5; 9,5) | 2 | 50 s |
| Clareira dos Caçadores | Lobo Cinzento | 3 (ponto 54,5; 46,5) | 3 | 40 s |
| Clareira dos Caçadores | Lobo Cinzento | 3 (ponto 68,5; 54,5) | 3 | 40 s |
| Acampamento dos Salteadores | Lobo Cinzento | 1 (ponto 79,5; 14,5) | 4 | 60 s |
| Acampamento dos Salteadores | Salteador da Estrada | 1 (ponto 82,5; 12,5) | 4 | 60 s |
| Acampamento dos Salteadores | Lobo Cinzento | 1 (ponto 80,5; 30,5) | 4 | 60 s |
| Acampamento dos Salteadores | Salteador da Estrada | 1 (ponto 83,5; 28,5) | 4 | 60 s |

**Covil da Matriarca** (`zone.matriarch_lair`), acessado pelo portão leste dos Campos da Vigília: corpos ficam 30 s no chão.

| Subárea | Monstro | Quantidade | Nível | Renasce em |
|---|---|---|---|---|
| Covil | Matriarca da Floresta | 1 (ponto 50,5; 16,5) | 8 | 300 s |

As aranhas não têm ponto de nascimento próprio: só aparecem invocadas pela Matriarca, com o nível dela (8).

### Encontros de teste (`data/encounters.json`)

Arenas fechadas usadas no protótipo e nos testes. Os cinco do protótipo usam `monsterLevel` 1; os da Floresta (T-340), das Ruínas (T-341) e o do Vigia (T-351) usam o nível de cada monstro.

| Encontro | Arena | Monstros | Jogadores | Limite |
|---|---|---|---|---|
| Três Lobos (`encounter.three_wolves`) | `map.fields` | 3 Lobos Cinzentos | Guerreiro | 120 s |
| Lobo e Salteador (`encounter.wolf_and_bandit`) | `map.camp` | 1 Salteador + 1 Lobo | Arqueiro | 120 s |
| Quatro Ratos de Campo (`encounter.four_rats`) | `map.farm` | 4 Ratos de Campo | Mago | 120 s |
| Guarda Esquelético no Portão Partido (`encounter.skeleton_guard`) | `map.broken_gate` | 1 Guarda Esquelético | Clérigo | 180 s |
| Matriarca da Floresta (`encounter.forest_matriarch`) | `map.matriarch_nest` | 1 Matriarca | Guerreiro, Arqueiro, Mago, Clérigo | 420 s |
| Duas Fadas da Névoa (`encounter.mist_fairies`) | `map.fields` | 2 Fadas nv6 | Guerreiro (os testes trocam a classe) | 120 s |
| Ent Corrompido (`encounter.corrupted_ent`) | `map.fields` | 1 Ent nv7 | Guerreiro (os testes trocam a classe) | 120 s |
| Emboscada na Névoa (`encounter.mist_ambush`) | `map.fields` | 3 Aranhas Menores nv5 + 1 Fada nv6 | Guerreiro, Arqueiro | 120 s |
| Pátio dos Mortos (`encounter.dead_courtyard`) | `map.broken_gate` | 3 Guardas Esqueléticos nv9 + 1 Sacerdote Morto nv10 | Guerreiro, Clérigo (os testes trocam as classes) | 240 s |
| Cavaleiro Espectral na Torre Quebrada (`encounter.spectral_knight`) | `map.broken_gate` | 1 Cavaleiro Espectral nv11 | Guerreiro, Arqueiro, Clérigo | 240 s |
| O Vigia Sem Rosto (`encounter.faceless_watcher`) | `map.watcher_hall` | 1 Vigia nv15; a proteger: 3 Selos do Juramento | Guerreiro, Arqueiro, Mago, Clérigo | 480 s |
| Bando de Afogados (`encounter.drowned_pack`) | `map.fields` | 3 Afogados nv16 | Guerreiro (os testes trocam a classe) | 120 s |
| Par de Homens-lagarto (`encounter.marsh_lizardmen`) | `map.fields` | 2 Homens-lagarto nv17 | Guerreiro (os testes trocam a classe) | 120 s |
| Serpentes do Charco (`encounter.marsh_serpents`) | `map.fields` | 2 Serpentes nv18 | Guerreiro (os testes trocam a classe) | 120 s |
| Palafitas Assombradas (`encounter.marsh_mixed`) | `map.fields` | 1 Afogado nv16 + 1 Homem-lagarto nv17 | Guerreiro (os testes trocam a classe) | 120 s |
| Bruxa do Brejo (`encounter.marsh_witch`) | `map.matriarch_nest` | 1 Bruxa nv20 | Guerreiro, Arqueiro, Clérigo | 360 s |
| Mineiros no Desfiladeiro (`encounter.miner_gang`) | `map.fields` | 4 Mineiros Possuídos nv21, em fila | Guerreiro (os testes trocam a classe e a segunda classe) | 120 s |
| Matilha da Montanha (`encounter.mountain_wolves`) | `map.fields` | 4 Lobos da Montanha nv22 | Guerreiro (os testes trocam a classe e a segunda classe) | 120 s |
| Bando de Morcegos (`encounter.cave_bats`) | `map.fields` | 6 Morcegos das Cavernas nv22 | Guerreiro (os testes trocam a classe e a segunda classe) | 120 s |
| Acampamento dos Mineiros (`encounter.miner_camp`) | `map.fields` | 4 Mineiros Possuídos + 4 Morcegos das Cavernas, nv21 | Guerreiro (os testes trocam a classe e a segunda classe) | 150 s |
| Golem de Pedra na Boca das Minas (`encounter.stone_golem`) | `map.fields` | 1 Golem de Pedra nv25 | Guerreiro, Clérigo (os testes trocam as classes e as segundas classes) | 240 s |
| O Capataz Forjado (`encounter.forged_overseer`) | `map.watcher_hall` (provisório até a Fundição da T-533) | 1 Capataz Forjado nv27 | Guerreiro, Arqueiro, Mago, Clérigo (os testes trocam as segundas classes) | 480 s |

## Monstros

### Rato de Campo — `monster.field_rat`

Nível 1, normal. Vida 150, velocidade 4,0, raio 0,3. Tags: beast, vermin.

- **Habilidades:** Mordida de Rato (`field_rat.bite`) — dano físico 3, arco de 90°, alcance 1,1, recarga 1 s.
- **IA:** `ai.swarm` — os ratos que miram o mesmo alvo se espalham em volta dele e atacam; de tempos em tempos um pode trocar de alvo. Parâmetros: `aggroRange` 14, `ringRadius` 1,3 (distância ao centro do alvo), `retargetChance` 0,25 (chance a cada verificação de trocar de alvo).
- **Onde aparece:** Campos da Vigília — Estrada Leste e Fazenda Abandonada, nível 1. Encontro de teste Quatro Ratos de Campo.
- **Quedas:** tabela normal.

### Lobo Cinzento — `monster.grey_wolf`

Nível 2, normal. Vida 220, velocidade 4,0, raio 0,4. Tag: beast.

- **Habilidades:**
  - Mordida do Lobo (`grey_wolf.bite`) — dano físico 12, arco de 80°, alcance 1,3, recarga 1,4 s.
  - Fúria da Matilha (`grey_wolf.pack_fury`, tag `ai.rally`) — em si mesmo, recarga 20 s: aplica **Fúria da Matilha** por 8 s (+30% de dano causado, +15% de velocidade).
- **IA:** `ai.pack` — a matilha foca o mesmo alvo (o líder escolhe a cada `focusSeconds` 4 s); quando um lobo morre, os outros tentam usar a habilidade de tag `ai.rally` dentro de `rallyWindowSeconds` 2 s. `aggroRange` 14.
- **Vida pelo nível de nascimento:** nv. 2 = 308; nv. 3 = 396; nv. 4 = 484.
- **Onde aparece:** Campos da Vigília — Fazenda Abandonada (nv. 2), Clareira dos Caçadores (nv. 3) e Acampamento dos Salteadores (nv. 4). Encontros de teste Três Lobos e Lobo e Salteador.
- **Quedas:** tabela normal.

### Salteador da Estrada — `monster.road_bandit`

Nível 3, normal. Vida 80, velocidade 4,0, raio 0,4. Tag: humanoid.

- **Habilidades:** Disparo de Besta (`road_bandit.shot`) — dano físico 20 num alvo inimigo a até 9, recarga 2 s.
- **IA:** `ai.ranged_kiter` — mantém distância de `preferredDistance` 7 e recua quando o alvo chega a `retreatDistance` 4; encurralado, para e atira. `aggroRange` 14.
- **Vida pelo nível de nascimento:** nv. 4 = 176.
- **Onde aparece:** Campos da Vigília — Acampamento dos Salteadores (nv. 4). Encontro de teste Lobo e Salteador.
- **Quedas:** tabela normal.

### Aranha Menor — `monster.spider_small`

Nível 5, normal. Vida 70, velocidade 4,0, raio 0,35. Tags: beast, spider.

- **Habilidades:** Picada da Aranha (`spider_small.bite`) — dano físico 9 e **Veneno** por 4 s; arco de 90°, alcance 1,1, recarga 1,2 s.
- **IA:** `ai.melee_chaser` — persegue e ataca com a primeira habilidade disponível. `aggroRange` 30.
- **Vida invocada pela Matriarca (nv. 8):** 266.
- **Onde aparece:** invocada pela Matriarca da Floresta (Chamar Aranhas: 2 por uso, no máximo 4 vivas) e no encontro de teste Emboscada na Névoa (nv. 5).
- **Quedas:** tabela normal.

### Aranha Grande — `monster.spider_big`

Nível 6, **elite**. Vida 400, velocidade 3,4, raio 0,6. Tags: beast, spider.

- **Habilidades:** Picada da Aranha Grande (`spider_big.bite`) — dano físico 26 e **Veneno** por 5 s; arco de 100°, alcance 1,6, recarga 1,5 s.
- **IA:** `ai.melee_chaser`, `aggroRange` 30.
- **Vida invocada pela Matriarca (nv. 8):** 1520.
- **Controle:** efeitos de controle duram metade (× 0,5).
- **Onde aparece:** só invocada pela Matriarca ao entrar na fase Teia Sufocante (1, no máximo 1 viva).
- **Quedas:** tabela elite.

### Fada da Névoa — `monster.mist_fairy`

Nível 6, normal. Vida 110, velocidade 3,4, raio 0,38. Tag: fey. Design: `design/combat-encounters-v0.1.md` § Floresta Baixa.

- **Habilidades:**
  - Dardo de Névoa (`mist_fairy.mist_bolt`): dano arcano 14 e **Névoa Gelada** (`status.mist_chill`, −20% de velocidade) por 2,5 s; num alvo inimigo a até 9; recarga 2 s.
  - Véu de Névoa (`mist_fairy.mist_veil`, tag `ai.vanish`): conjuração de 0,6 s, em si mesma, recarga 8 s. Aplica **Na Névoa** (`status.in_mist`) por 2 s: **não pode ser alvo** (`flags.untargetable`) e +20% de velocidade.
- **IA:** `ai.mist_kiter`. Atira a `preferredDistance` 7 e usa o véu quando o alvo chega a `vanishDistance` 3,5 ou quando a vida cai a `vanishBelowHealth` 0,75. Na névoa não ataca e se afasta do alvo. Visível, não foge. `aggroRange` 14.
- **Vida pelo nível de nascimento:** nv. 6 = 330.
- **Onde aparece:** Clareira da Névoa (mapa da T-342). Encontros de teste Duas Fadas da Névoa e Emboscada na Névoa (nv. 6).
- **Arte:** sprite humanoide LPC ligado pela T-056 (`client/assets/art/sprites.json`).
- **Quedas:** tabela normal.

### Ent Corrompido — `monster.corrupted_ent`

Nível 7, normal. Vida 430, velocidade 2,4, raio 0,7. Tags: plant, **corrupted** (recebe +25% do dano sagrado do Clérigo). Resistências: **físico 25%**, **fogo −30%** (fraqueza). Design: `design/combat-encounters-v0.1.md` § Floresta Baixa.

- **Habilidades:**
  - Pisão Corrompido (`corrupted_ent.trampling_slam`): círculo de raio 2,6 em volta dele, com aviso de 1,2 s; dano físico 44; recarga 7 s.
  - Raízes Famintas (`corrupted_ent.grasping_roots`, tag `ai.area`): círculo de raio 1,6 no alvo, alcance 9, aviso de 1 s; dano de natureza 16 e **Preso** (`status.snared`) por 2 s; recarga 8 s.
  - Rajada de Espinhos (`corrupted_ent.thorn_volley`, tag `ai.ranged`): dano de natureza 14 num alvo inimigo a até 8; recarga 2,5 s.
  - Açoite de Galhos (`corrupted_ent.branch_lash`): dano físico 19, arco de 110°, alcance 1,8, recarga 1,8 s.
- **IA:** `ai.brute`. Só usa cada golpe com o alvo ao alcance (não golpeia o vazio). Raízes e espinhos só saem com o alvo a `farDistance` 3 ou mais. Fica parado até o próprio aviso resolver. Persegue até `approachRange` 1,8. `aggroRange` 14.
- **Vida pelo nível de nascimento:** nv. 7 = 1462.
- **Onde aparece:** Floresta Baixa (mapa da T-342). Encontro de teste Ent Corrompido (nv. 7).
- **Arte:** ainda não tem (encomenda; `tasks/reports/arte-monstros-fase-3.md`).
- **Quedas:** tabela normal.

### Matriarca da Floresta — `monster.forest_matriarch`

Nível 8, **chefe**. Vida 8800 no nível 1; no Covil (nv. 8) = **27 280** (8800 × 3,1); potência × 3,66. Velocidade 3,4, raio 0,9. Tags: beast, spider, boss.

- **IA:** `ai.boss` — persegue como corpo a corpo, mas antes de atacar tenta invocar (`ai.summon`) e criar áreas (`ai.area`), na ordem da lista. Habilidade fora da fase atual é rejeitada. `aggroRange` 40.
- **Controle:** efeitos de controle duram um quarto (× 0,25); no Frenesi, ainda × 0,5.
- **Habilidades** (detalhes em `design/habilidades.md`):

| Habilidade | Id | Resumo |
|---|---|---|
| Investida | `matriarch.charge` | linha 6 × 1,6 com aviso de 1 s; avança e causa dano físico 130; recarga 9 s |
| Mordida da Matriarca | `matriarch.bite` | dano físico 62, arco de 120°, alcance 2,3, recarga 1,5 s |
| Mordida Dilacerante | `matriarch.rending_bite` | igual à Mordida, mais **Sangramento** por 6 s |
| Poça de Veneno | `matriarch.venom_pool` | aviso de 1 s; cria Poça de Veneno (raio 2, 8 s) a até 12; recarga 12 s |
| Poça de Veneno Rápida | `matriarch.venom_pool_fast` | a mesma poça, recarga 6 s |
| Teia Sufocante | `matriarch.web_field` | cria a área `area.sticky_web` (raio 3, 18 s; aplica Teia, velocidade × 0,5) a até 12; recarga 14 s; sem aviso |
| Chamar Aranhas | `matriarch.summon_lesser` | invoca 2 Aranhas Menores em raio 2 (máximo 4 vivas); recarga 18 s |

- **Fases** (`phases`; a fase começa quando a vida cai até o percentual):

| Fase | Id | Vida | Ao entrar | Habilidades |
|---|---|---|---|---|
| Caçada | `phase.hunt` | 100% | — | Investida, Mordida, Poça de Veneno, Chamar Aranhas |
| Teia Sufocante | `phase.suffocating_web` | 66% | invoca 1 Aranha Grande em raio 2,5 (máximo 1 viva) | Investida, Mordida, Poça de Veneno Rápida, Teia Sufocante, Chamar Aranhas |
| Frenesi | `phase.frenzy` | 33% | aplica **Frenesi** em si; sem invocações (`noSummons`) | Investida, Mordida Dilacerante, Poça de Veneno Rápida |

Os nomes das fases acima são tradução dos ids; os dados não têm nome exibido para fases.

- **Onde aparece:** Covil da Matriarca (nv. 8, renasce em 300 s). Encontro de teste Matriarca da Floresta (nv. 1).
- **Quedas:** tabela chefe (2 itens garantidos, o primeiro pelo menos incomum).

### Guarda Esquelético — `monster.skeleton_guard`

Nível 9, normal. Vida 440, velocidade 3,2, raio 0,5. Tag: undead (recebe +25% do dano sagrado do Clérigo).

- **Habilidades:**
  - Golpe de Lâmina (`skeleton_guard.slash`) — dano físico 9, arco de 90°, alcance 1,8, recarga 1,6 s.
  - Erguer Escudo (`skeleton_guard.raise_shield`, tag `ai.guard`) — em si, recarga 8 s: **Escudo Erguido** por 3 s (recebe 30% do dano vindo da frente, arco de 140°).
  - Golpe Esmagador (`skeleton_guard.crushing_blow`, tag `ai.heavy`) — cone de 2,4 × 100° com aviso de 1 s; dano físico 30; recarga 8 s.
- **IA:** `ai.guardian` — ciclo perseguir → guarda → golpe pesado → recuperar. Na guarda fica parado e só se vira para o alvo a cada `faceIntervalSeconds` 1 s (janela para atacar pelo flanco). Parâmetros: `aggroRange` 14, `guardSeconds` 3, `recoverSeconds` 1,5.
- **Onde aparece:** hoje **não aparece no mundo aberto**; só nos encontros de teste Guarda Esquelético no Portão Partido (nv. 1) e Pátio dos Mortos (nv. 9).
- **Quedas:** tabela normal.

### Sacerdote Morto — `monster.dead_priest`

Nível 10, normal. Vida 180, velocidade 3,0, raio 0,45. Tag: undead (recebe +25% do dano sagrado do Clérigo). Design: `design/combat-encounters-v0.1.md` § Ruínas.

- **Habilidades:**
  - Remendar os Mortos (`dead_priest.mend_dead`, tag `ai.heal`): **conjuração de 2 s**, cura 105 num aliado a até 9; recarga 6 s. Interrompível.
  - Maldição Definhante (`dead_priest.withering_curse`): dano arcano 6 e **Maldição Definhante** (`status.withering_curse`, tags debuff e curse: **cura recebida −50%**) por 8 s, num inimigo a até 9; recarga 12 s. A Purificação do Clérigo remove.
  - Dardo Sepulcral (`dead_priest.grave_bolt`): dano arcano 10 num inimigo a até 9; recarga 2,2 s.
- **IA:** `ai.support`. Cura o aliado mais ferido com vida até `healBelow` 0,7 (nunca a si mesmo). Fica `behindDistance` 3 tiles atrás do centro dos protetores (os outros monstros do grupo), do lado oposto ao alvo; sem protetores, a `preferredDistance` 7. Ataca só com as instantâneas, andando ou não. `aggroRange` 14.
- **Vida pelo nível de nascimento:** nv. 10 = 828.
- **Onde aparece:** Pátio dos Mortos nas Ruínas (mapa da T-343). Encontro de teste Pátio dos Mortos (nv. 10).
- **Arte:** sprite humanoide LPC ligado pela T-056 (`client/assets/art/sprites.json`).
- **Quedas:** tabela normal.

### Cavaleiro Espectral — `monster.spectral_knight`

Nível 11, **elite**. Vida 4500, velocidade 3,4, raio 0,5. Tag: undead. Design: `design/combat-encounters-v0.1.md` § Ruínas.

- **Habilidades:**
  - Escudo Espectral (`spectral_knight.spectral_shield`, tag `ai.buff`): em si, recarga 15 s. **Escudo Espectral** (`status.spectral_shield`) por 6 s: **devolve 30%** do dano direto recebido a quem atacou (`reflect`); **quebra** ao receber 8% da vida máxima em dano ou com uma interrupção (`breaks`).
  - Golpe Giratório (`spectral_knight.whirling_strike`): círculo de raio 2,4 em volta dele, com aviso de 1 s; dano físico 130; recarga 9 s.
  - Lâmina Espectral (`spectral_knight.spectral_blade`): dano físico 55, arco de 100°, alcance 1,9, recarga 1,6 s.
  - Investida Espectral (`spectral_knight.spectral_charge`, tag `ai.ranged`): linha de 8 × 1,4 com aviso de 1 s; dano físico 140 e o Cavaleiro avança ao longo da linha; recarga 10 s.
- **IA:** `ai.brute`. Ergue o escudo com o alvo a até `buffDistance` 3; investe com o alvo a `farDistance` 3 ou mais. Persegue até `approachRange` 1,9. `aggroRange` 14.
- **Vida pelo nível de nascimento:** nv. 11 = 22 500.
- **Onde aparece:** Torre Quebrada, nas Ruínas (mapa da T-343), opcional. Encontro de teste Cavaleiro Espectral na Torre Quebrada (nv. 11).
- **Arte:** sprite humanoide LPC ligado pela T-056 (`client/assets/art/sprites.json`).
- **Quedas:** tabela elite.

### Sombra — `monster.shade`

Nível 14, normal. Vida 150, velocidade 3,6, raio 0,4. Tag: undead. Design: `design/combat-encounters-v0.1.md` § Cripta do Vigia.

- **Habilidade:** Toque Gélido (`shade.cold_touch`) — dano arcano 18, arco de 90°, alcance 1,6, recarga 1,4 s.
- **IA:** `ai.melee_chaser`, `aggroRange` 20.
- **Onde aparece:** só invocada pelo Vigia (Sombras do Rosto Vazio), com o nível dele (15: vida 990).
- **Arte:** sprite humanoide LPC ligado pela T-056 (`client/assets/art/sprites.json`).
- **Quedas:** tabela normal.

### Vigia Sem Rosto — `monster.faceless_watcher`

Nível 15, **chefe**. Vida 45 000 no nível 1; no nível 15 = **234 000** (45 000 × 5,2); potência × 6,32. Velocidade 3,4, raio 0,9. Tags: undead, boss. Design: `design/combat-encounters-v0.1.md` § Cripta do Vigia.

- **IA:** `ai.boss`, `aggroRange` 40, `approachRange` 2,3. Antes de atacar tenta, na ordem da lista, o Último Comando no **selo mais ferido** (tag nova `ai.objective`), as invocações (`ai.summon`) e o Chão do Pavor (`ai.area`).
- **Controle:** efeitos de controle duram um quarto (× 0,25).
- **Habilidades:**

| Habilidade | Id | Resumo |
|---|---|---|
| Último Comando | `watcher.last_command` | **conjuração de 4 s** num selo a até 40; dano arcano 2000 (destrói o selo); recarga 10 s, **também quando interrompida** |
| Chamado da Guarda | `watcher.call_guards` | conjuração de 2,5 s; invoca 2 Guardas Esqueléticos em raio 2,5 (máximo 2 vivos); recarga 24 s |
| Sombras do Rosto Vazio | `watcher.call_shades` | conjuração de 2 s; invoca 3 Sombras em raio 2,5 (máximo 3 vivas); recarga 20 s |
| Chão do Pavor | `watcher.dread_ground` | círculo de raio 2,5 a até 14 com aviso de 1,2 s; dano arcano 70 e **Pavor** (`status.dread`, medo por 2 s); recarga 8 s |
| Investida de Ferro | `watcher.iron_charge` | linha 8 × 1,8 com aviso de 1,2 s; avança e causa dano físico 150; recarga 12 s |
| Corte do Juramento | `watcher.oath_cleave` | cone frontal de 90° × 4 com aviso de 1,2 s; dano físico 120; recarga 7 s |
| Golpe de Ferro | `watcher.iron_strike` | dano físico 38, arco de 100°, alcance 2,3, recarga 1,6 s |
| Golpe Espectral | `watcher.spectral_strike` | dano arcano 36, arco de 100°, alcance 2,3, recarga 1,6 s |

- **Fases** (`phases`):

| Fase | Id | Vida | Ao entrar | Habilidades |
|---|---|---|---|---|
| Juramento de Ferro | `phase.iron_oath` | 100% | nos selos: **Selo Intacto** (`status.seal_intact`, não pode ser alvo) | Chamado da Guarda, Investida de Ferro, Corte do Juramento, Golpe de Ferro |
| O Rosto Vazio | `phase.empty_face` | 65% | em si: **Rosto Vazio** (`status.empty_face`, dano sagrado recebido × 1,4) | Sombras do Rosto Vazio, Chão do Pavor, Corte do Juramento, Golpe Espectral |
| Último Comando | `phase.last_command` | 25% | nos selos: tira o Selo Intacto e aplica **Selo em Ruína** (`status.seal_crumbling`, dano arcano 4 por segundo); sem invocações (`noSummons`) | Último Comando, Chão do Pavor, Corte do Juramento, Golpe Espectral |

- **Onde aparece:** Sala do Vigia, na Cripta (dungeon da T-352). Encontro de teste O Vigia Sem Rosto (nv. 15).
- **Arte:** sprite LPC em escala ×2 ligado pela T-056 (`client/assets/art/sprites.json`).
- **Quedas:** tabela chefe.

### Selo do Juramento — `monster.watcher_seal` (objetivo)

Nível 15, normal. Vida 380 (2 508 no nível 15), velocidade 0, raio 0,6. Tag: seal. Não é inimigo: nasce pelo campo `objectives` do encontro, **do lado dos jogadores**, sem IA, e não se move nem é empurrado. Pode ser curado e receber escudo; não puxa aggro e não é revivido. **Se os três caem, a luta falha** (`EncounterFailed`). Sem sprite próprio ainda (T-345).

### Afogado — `monster.drowned`

Nível 16, normal. Vida 300, velocidade 2,6, raio 0,4. Tag: **undead** (recebe +25% do dano sagrado do Clérigo). Resistências: **sagrado −40%** (fraqueza). Design: `design/combat-encounters-v0.1.md` § Brejo dos Afogados.

- **Habilidades:**
  - Agarrão (`drowned.grab`): cone de 90° × 1,6 à frente, com aviso de 0,6 s; dano físico 26 e **Preso** (`status.snared`) por 1,5 s; recarga 6 s.
  - Garra do Afogado (`drowned.claw`): dano físico 22, arco de 90°, alcance 1,4, recarga 1,8 s.
- **IA:** `ai.brute`. Persegue até `approachRange` 1,5; fica parado até o próprio aviso resolver. `aggroRange` 14.
- **Vida pelo nível de nascimento:** nv. 16 = 2100; nv. 20 (invocado pela Bruxa) = 2580.
- **Onde aparece:** Brejo dos Afogados (mapa da T-530), em bandos de 3 a 4; invocado pela Bruxa do Brejo. Encontros de teste Bando de Afogados e Palafitas Assombradas.
- **Arte:** sprite LPC da T-521.
- **Quedas:** tabela normal.

### Homem-lagarto do Brejo — `monster.marsh_lizardman`

Nível 17, normal. Vida 440, velocidade 4,0, raio 0,4. Tags: humanoid, **amphibious** (não sente a água funda: as áreas `slow_terrain` do Brejo usam `ignoreTags = amphibious`). Design: `design/combat-encounters-v0.1.md` § Brejo dos Afogados.

- **Habilidades:**
  - Bote (`marsh_lizardman.lunge`): linha 4 × 1,2 com aviso de 0,6 s; avança e causa dano físico 40; recarga 6 s.
  - Garra do Lagarto (`marsh_lizardman.slash`): dano físico 16, arco de 90°, alcance 1,5, recarga 1,4 s.
- **IA:** `ai.brute`. O Bote sai sempre que o alvo está na linha, também colado nele. Persegue até `approachRange` 1,5. `aggroRange` 14.
- **Vida pelo nível de nascimento:** nv. 17 = 3256.
- **Onde aparece:** Brejo dos Afogados, em pares. Encontros de teste Par de Homens-lagarto e Palafitas Assombradas.
- **Arte:** sprite LPC da T-521.
- **Quedas:** tabela normal.

### Serpente do Charco — `monster.marsh_serpent`

Nível 18, normal. Vida 200, velocidade 3,0, raio 0,45. Tag: beast. Design: `design/combat-encounters-v0.1.md` § Brejo dos Afogados.

- **Habilidades:**
  - Cuspe Venenoso (`marsh_serpent.spit`): dano de natureza 5 num alvo inimigo a até 9 e **Veneno** (`status.poison`) por 1,5 s; recarga 2,8 s.
  - Picada da Serpente (`marsh_serpent.bite`): dano físico 30, arco de 90°, alcance 1,4, recarga 1,6 s.
- **IA:** `ai.ranged_kiter`. Distância preferida 6; recua de quem chega a menos de 3, usando só as instantâneas (as duas são). `aggroRange` 14.
- **Vida pelo nível de nascimento:** nv. 18 = 1560.
- **Onde aparece:** Charco Fundo, no Brejo, em pares. Encontro de teste Serpentes do Charco.
- **Arte:** sprite da T-521.
- **Quedas:** tabela normal.

### Bruxa do Brejo — `monster.marsh_witch`

Nível 20, **chefe** (chefe de campo). Vida 5500 (36 850 no nível 20), velocidade 3,2, raio 0,45. Tags: humanoid, boss. Design: `design/combat-encounters-v0.1.md` § Brejo dos Afogados.

| Habilidade | Id | Resumo |
|---|---|---|
| Lodo que Puxa | `marsh_witch.pulling_mud` | conjuração de 1 s; cria o Lodo (`area.pulling_mud`, raio 4,5, 6 s, **preso a ela**): **Água Funda** (`status.deep_water`, velocidade × 0,6) por 0,5 s em todo inimigo dentro, a cada 0,25 s; recarga 14 s |
| Chamar os Afogados | `marsh_witch.call_drowned` | conjuração de 1,5 s (interrompível); invoca 1 Afogado em raio 2,5 (máximo 2 vivos); recarga 20 s |
| Poça Venenosa | `marsh_witch.poison_puddle` | aviso de 1 s; cria a Poça Venenosa (`area.witch_poison_puddle`, raio 2, 8 s, natureza 3,5 por segundo) a até 12; recarga 9 s |
| Poça Venenosa Rápida | `marsh_witch.poison_puddle_fast` | a mesma poça, recarga 7 s |
| Golpe de Cajado | `marsh_witch.staff_strike` | dano físico 8, arco de 100°, alcance 2, recarga 1,6 s |
| Dardo do Brejo | `marsh_witch.hex_bolt` | dano de natureza 10 num alvo a até 10; recarga 2,2 s |
| Dardo do Lodo | `marsh_witch.bog_bolt` | dano de natureza 10 num alvo a até 12; recarga 1,6 s |

- **Fases** (`phases`):

| Fase | Id | Vida | Ao entrar | Habilidades |
|---|---|---|---|---|
| Poças Venenosas | `phase.poison_puddles` | 100% | — | Poça Venenosa, Golpe de Cajado, Dardo do Brejo |
| Chamado dos Afogados | `phase.drowned_call` | 66% | invoca 2 Afogados em raio 2,5 (máximo 3 vivos) | Chamar os Afogados, Poça Venenosa, Golpe de Cajado, Dardo do Brejo |
| Lodo que Puxa | `phase.pulling_mud` | 33% | sem invocações (`noSummons`) | Lodo que Puxa, Poça Venenosa Rápida, Golpe de Cajado, Dardo do Lodo |

- **IA:** `ai.boss`. `aggroRange` 40.
- **Onde aparece:** Cabana da Bruxa, no Brejo (mapa da T-530; renasce em 10 min). Encontro de teste Bruxa do Brejo (nv. 20).
- **Arte:** sprite LPC da T-521.
- **Quedas:** tabela chefe.

### Mineiro Possuído — `monster.possessed_miner`

Nível 21, normal. Vida 320, velocidade 3,2, raio 0,4. Tag: humanoid. Design: `design/combat-encounters-v0.1.md` § Serra Partida.

- **Habilidades:**
  - Picaretada (`possessed_miner.pickaxe_blow`): cone de 70° × 1,8 à frente, com aviso de 0,8 s; dano físico 30 e **Atordoado** (`status.stunned`) por 1 s; recarga 7 s.
  - Golpe de Picareta (`possessed_miner.pick_strike`): dano físico 14, arco de 90°, alcance 1,4, recarga 1,8 s.
- **IA:** `ai.brute`. Persegue até `approachRange` 1,5; fica parado até o próprio aviso resolver. `aggroRange` 14.
- **Vida pelo nível de nascimento:** nv. 21 = 2880.
- **Onde aparece:** Serra Partida (mapa da T-532): Desfiladeiro, em grupos de 3 a 5, e Acampamento dos Mineiros, de 6 a 10. Minas Fundas (T-533), no nível 24 a 25, e invocado pelo Capataz Forjado, no nível dele (27: vida 3648). Encontros de teste Mineiros no Desfiladeiro e Acampamento dos Mineiros.
- **Arte:** sprite LPC da T-521.
- **Quedas:** tabela normal.

### Lobo da Montanha — `monster.mountain_wolf`

Nível 22, normal. Vida 260, velocidade 4,2, raio 0,4. Tag: beast. Design: `design/combat-encounters-v0.1.md` § Serra Partida.

- **Habilidades:**
  - Mordida do Lobo da Montanha (`mountain_wolf.bite`): dano físico 12, arco de 80°, alcance 1,3, recarga 1,4 s.
  - Uivo da Montanha (`mountain_wolf.pack_fury`, `ai.rally`): **Fúria da Matilha** (`status.pack_aggression`: dano × 1,3, velocidade × 1,15) por 8 s; recarga 20 s.
- **IA:** `ai.pack`, como o Lobo Cinzento (`focusSeconds` 4, `rallyWindowSeconds` 2). `aggroRange` 14.
- **Vida pelo nível de nascimento:** nv. 22 = 2444.
- **Onde aparece:** Serra Partida, em matilhas de 4 a 5; Minas Fundas (T-533), na rota opcional, no nível 26. Encontro de teste Matilha da Montanha.
- **Arte:** o sprite do Lobo Cinzento com outra cor (T-521/T-540).
- **Quedas:** tabela normal.

### Morcego das Cavernas — `monster.cave_bat`

Nível 22, normal. Vida 90, velocidade 4,4, raio 0,3. Tag: beast. Design: `design/combat-encounters-v0.1.md` § Serra Partida.

- **Habilidades:**
  - Mordida do Morcego (`cave_bat.bite`): dano físico 5, arco de 90°, alcance 1,1, recarga 1 s.
- **IA:** `ai.swarm`, como o Rato (`ringRadius` 1,2, `retargetChance` 0,25). `aggroRange` 14.
- **Vida pelo nível de nascimento:** nv. 21 = 810; nv. 22 = 846.
- **Onde aparece:** Serra Partida (Ninho dos Morcegos e Acampamento dos Mineiros), em bandos de 5 a 6; Minas Fundas (T-533), no nível 24 a 25. Encontros de teste Bando de Morcegos e Acampamento dos Mineiros.
- **Arte:** sprite da T-521.
- **Quedas:** tabela normal.

### Golem de Pedra — `monster.stone_golem`

Nível 25, **elite**. Vida 1200 (12 720 no nível 25), velocidade 2,2, raio 0,7. Tag: construct. Resistências: **físico 0,3**, **arcano −0,3** (fraqueza). Design: `design/combat-encounters-v0.1.md` § Serra Partida.

- **Habilidades** (na ordem de prioridade da IA):
  - Rocha Rolante (`stone_golem.rolling_boulder`, `ai.ranged`): linha de 14 × 1,6 com aviso de 1,5 s; dano físico 80 em todo inimigo na linha (monstros não); recarga 10 s.
  - Pisão de Pedra (`stone_golem.ground_slam`): círculo de raio 2,6 em volta dele, com aviso de 1,2 s; dano físico 45; recarga 9 s.
  - Punho de Pedra (`stone_golem.stone_fist`): dano físico 20, arco de 100°, alcance 2, recarga 2 s.
- **IA:** `ai.brute`. A Rocha é a primeira da lista e sai sempre que está pronta (o alcance de 14 cobre o alvo de perto e de longe); de longe, só ela alcança. Persegue até `approachRange` 2. `aggroRange` 14.
- **Onde aparece:** Boca das Minas, na Serra Partida (mapa da T-532; renasce em 3 min); Minas Fundas (T-533), na rota opcional, no nível 26. Encontro de teste Golem de Pedra na Boca das Minas.
- **Arte:** sprite da T-521.
- **Quedas:** tabela elite.

### Capataz Forjado — `monster.forged_overseer`

Nível 27, **chefe** (chefe das Minas Fundas, masmorra para 4). Vida 70000 (616 000 no nível 27), velocidade 3,0, raio 0,9. Tags: construct, boss. Design: `design/combat-encounters-v0.1.md` § Minas Fundas.

| Habilidade | Id | Resumo |
|---|---|---|
| Chamar a Equipe | `forged_overseer.call_crew` | conjuração de 2 s (interrompível); invoca 1 Mineiro Possuído em raio 2,5 (máximo 3 vivos); recarga 20 s |
| Sopro da Fornalha | `forged_overseer.furnace_blast` | círculo de raio 30 em volta dele (a sala toda), sem aviso; dano de fogo 22 em todo inimigo; recarga 5 s |
| Escória Ardente | `forged_overseer.slag_throw` | `ai.ranged`: só com o alvo a 3 tiles ou mais; dano de fogo 25 num alvo a até 14; recarga 2,5 s |
| Martelo | `forged_overseer.hammer` | cone de 100° × 3,5 à frente, com aviso de 1 s; dano físico 90; recarga 9 s |
| Martelo Acelerado | `forged_overseer.hammer_fast` | o mesmo Martelo, recarga 6 s |
| Punho de Ferro | `forged_overseer.iron_fist` | dano físico 62, arco de 100°, alcance 2,4, recarga 1,6 s |

- **Fases** (`phases`):

| Fase | Id | Vida | Ao entrar | Habilidades |
|---|---|---|---|---|
| Martelo | `phase.hammer` | 100% | — | Escória Ardente, Martelo, Punho de Ferro |
| Fornalha | `phase.furnace` | 60% | **Fornalha Esquentando** (`status.overseer_heat`, 190 s); quando acaba, **Fornalha Transbordando** (`status.overseer_meltdown`: dano × 4 até o fim) | Sopro da Fornalha, Escória Ardente, Martelo, Punho de Ferro |
| Chamar a Equipe | `phase.call_the_crew` | 30% | invoca 2 Mineiros Possuídos em raio 2,5 (máximo 3 vivos); **Fornalha Acesa** (`status.overseer_overdrive`: velocidade × 1,3, dano × 1,15, até o fim) | Chamar a Equipe, Sopro da Fornalha, Escória Ardente, Martelo Acelerado, Punho de Ferro |

- **IA:** `ai.boss`. `aggroRange` 40, `approachRange` 2,4, **`meleeTargetRange` 3,5**: entre os da tabela de ameaça a até 3,5 tiles, bate no de maior ameaça; sem ninguém perto, segue a tabela (T-512).
- **Onde aparece:** Fundição, no fundo das Minas Fundas (mapa da T-533). Encontro de teste O Capataz Forjado (nv. 27).
- **Arte:** não tem corpo no gerador LPC; a T-521 resolve (encomenda ou pacote).
- **Quedas:** tabela chefe.

## Pontos em aberto nos dados

- O campo `level` de cada monstro (ex.: Guarda Esquelético 9, Aranha Menor 5) só vale quando o local de nascimento não define nível; nas zonas atuais o nível vem do ponto de nascimento e, nas invocações, de quem invoca.
- Elite não tem crescimento próprio (`rules.monsters.elite` ausente): usa o crescimento normal.
- A Poça Venenosa da Bruxa não aplica o estado Veneno (só dano de natureza na área), ao contrário da Poça de Veneno da Matriarca (T-510, balance-v0.24).
- O Frenesi dura 3600 s (`defaultDuration`), o que na prática cobre o resto da luta.
- A Teia Sufocante não tem `telegraph` (aviso), ao contrário das Poças de Veneno.
- `ringRadius` e `retargetChance` do Rato têm o significado descrito no código da IA (`src/Game.Core/Combat/Ai/Profiles`), não no esquema de dados.
