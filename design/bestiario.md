# Bestiário

Status: **referência gerada a partir dos dados** (2026-11-02). Fonte: `data/monsters.json`, `data/abilities_monsters.json`, `data/statuses.json`, `data/areas.json`, `data/zones.json`, `data/encounters.json`, `data/rules.json` e `data/items.json` (tabelas de queda). Os números saem desses arquivos; quando os dados mudarem, este documento deve ser atualizado.

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
- **Escolas de dano**: físico (`physical`), natureza (`nature`).

## Resumo

| Monstro | Id | Nível | Categoria | Vida (nv. 1) | Velocidade | Raio | Tags | IA |
|---|---|---|---|---|---|---|---|---|
| Rato de Campo | `monster.field_rat` | 1 | normal | 150 | 4,0 | 0,3 | beast, vermin | `ai.swarm` |
| Lobo Cinzento | `monster.grey_wolf` | 2 | normal | 220 | 4,0 | 0,4 | beast | `ai.pack` |
| Salteador da Estrada | `monster.road_bandit` | 3 | normal | 80 | 4,0 | 0,4 | humanoid | `ai.ranged_kiter` |
| Aranha Menor | `monster.spider_small` | 5 | normal | 70 | 4,0 | 0,35 | beast, spider | `ai.melee_chaser` |
| Aranha Grande | `monster.spider_big` | 6 | **elite** | 400 | 3,4 | 0,6 | beast, spider | `ai.melee_chaser` |
| Matriarca da Floresta | `monster.forest_matriarch` | 8 | **chefe** | 8800 | 3,4 | 0,9 | beast, spider, boss | `ai.boss` |
| Guarda Esquelético | `monster.skeleton_guard` | 9 | normal | 440 | 3,2 | 0,5 | undead | `ai.guardian` |

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

Arenas fechadas usadas no protótipo e nos testes. Todos usam `monsterLevel` 1.

| Encontro | Arena | Monstros | Jogadores | Limite |
|---|---|---|---|---|
| Três Lobos (`encounter.three_wolves`) | `map.fields` | 3 Lobos Cinzentos | Guerreiro | 120 s |
| Lobo e Salteador (`encounter.wolf_and_bandit`) | `map.camp` | 1 Salteador + 1 Lobo | Arqueiro | 120 s |
| Quatro Ratos de Campo (`encounter.four_rats`) | `map.farm` | 4 Ratos de Campo | Mago | 120 s |
| Guarda Esquelético no Portão Partido (`encounter.skeleton_guard`) | `map.broken_gate` | 1 Guarda Esquelético | Clérigo | 180 s |
| Matriarca da Floresta (`encounter.forest_matriarch`) | `map.matriarch_nest` | 1 Matriarca | Guerreiro, Arqueiro, Mago, Clérigo | 420 s |

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
- **Onde aparece:** só invocada pela Matriarca da Floresta (Chamar Aranhas: 2 por uso, no máximo 4 vivas).
- **Quedas:** tabela normal.

### Aranha Grande — `monster.spider_big`

Nível 6, **elite**. Vida 400, velocidade 3,4, raio 0,6. Tags: beast, spider.

- **Habilidades:** Picada da Aranha Grande (`spider_big.bite`) — dano físico 26 e **Veneno** por 5 s; arco de 100°, alcance 1,6, recarga 1,5 s.
- **IA:** `ai.melee_chaser`, `aggroRange` 30.
- **Vida invocada pela Matriarca (nv. 8):** 1520.
- **Controle:** efeitos de controle duram metade (× 0,5).
- **Onde aparece:** só invocada pela Matriarca ao entrar na fase Teia Sufocante (1, no máximo 1 viva).
- **Quedas:** tabela elite.

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
- **Onde aparece:** hoje **não aparece no mundo aberto**; só no encontro de teste Guarda Esquelético no Portão Partido (nv. 1).
- **Quedas:** tabela normal.

## Pontos em aberto nos dados

- O campo `level` de cada monstro (ex.: Guarda Esquelético 9, Aranha Menor 5) só vale quando o local de nascimento não define nível; nas zonas atuais o nível vem do ponto de nascimento e, nas invocações, de quem invoca.
- Elite não tem crescimento próprio (`rules.monsters.elite` ausente): usa o crescimento normal.
- O Frenesi dura 3600 s (`defaultDuration`), o que na prática cobre o resto da luta.
- A Teia Sufocante não tem `telegraph` (aviso), ao contrário das Poças de Veneno.
- `ringRadius` e `retargetChance` do Rato têm o significado descrito no código da IA (`src/Game.Core/Combat/Ai/Profiles`), não no esquema de dados.
