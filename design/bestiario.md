# Bestiário

Status: **referência gerada a partir dos dados** (2026-11-02; Fada da Névoa e Ent Corrompido em 2026-11-06, T-340; Sacerdote Morto e Cavaleiro Espectral em 2026-11-09, T-341). Fonte: `data/monsters.json`, `data/abilities_monsters.json`, `data/statuses.json`, `data/areas.json`, `data/zones.json`, `data/encounters.json`, `data/rules.json` e `data/items.json` (tabelas de queda). Os números saem desses arquivos; quando os dados mudarem, este documento deve ser atualizado.

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
- **Resistências por escola** (`resistances`): fração do dano cortada; negativa = **fraqueza** (−0,3 = recebe 130%). Só o Ent Corrompido define (físico 0,25; fogo −0,3).
- **Escolas de dano**: físico (`physical`), natureza (`nature`), arcano (`arcane`).

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

## Quedas (drops)

Nenhum monstro tem tabela própria: a queda depende só da categoria (`data/items.json` → `rules.items.drops`). As três tabelas usam a mesma lista de 21 itens base (todas as armas, armaduras e acessórios de `design/itens.md`), sorteados com o mesmo peso. O **nível do item** é o nível do monstro e os itens só caem para quem participou da morte.

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

Arenas fechadas usadas no protótipo e nos testes. Os cinco do protótipo usam `monsterLevel` 1; os da Floresta (T-340) e das Ruínas (T-341) definem o nível de cada monstro.

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

## Pontos em aberto nos dados

- O campo `level` de cada monstro (ex.: Guarda Esquelético 9, Aranha Menor 5) só vale quando o local de nascimento não define nível; nas zonas atuais o nível vem do ponto de nascimento e, nas invocações, de quem invoca.
- Elite não tem crescimento próprio (`rules.monsters.elite` ausente): usa o crescimento normal.
- O Frenesi dura 3600 s (`defaultDuration`), o que na prática cobre o resto da luta.
- A Teia Sufocante não tem `telegraph` (aviso), ao contrário das Poças de Veneno.
- `ringRadius` e `retargetChance` do Rato têm o significado descrito no código da IA (`src/Game.Core/Combat/Ai/Profiles`), não no esquema de dados.
