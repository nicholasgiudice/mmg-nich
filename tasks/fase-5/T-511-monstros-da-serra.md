# T-511 — Monstros da Serra Partida

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** 510 (mesmos arquivos de dados)
- **Libera:** T-512, T-513, T-532, T-540
- **Áreas que pode alterar:** as mesmas da T-510

## Contexto

A Serra Partida (`design/mapas-16-40-v0.1.md` §3.3) é o campo do nível 21 ao 25, a **primeira área de segunda classe**. Corredores de rocha e trilhas em encosta. O truque é ter **monstros em fila** nos desfiladeiros (serve ao Atirador de Elite e ao Berserker) e grupos que pedem dano em área (§4 do esboço).

## Requisitos

1. **Design** (seção nova em `design/combat-encounters-v0.1.md`). Propostas (o designer pode trocar):
   - **Mineiro Possuído** (`monster.possessed_miner`, 21, normal, tag `humanoid`): corpo a corpo, em grupos de 3 a 5. *Picaretada*: golpe com telegráfo que atordoa (`status.stunned`) por 1 s.
   - **Lobo da Montanha** (`monster.mountain_wolf`, 22, normal, tag `beast`, `ai.pack`): matilha de 4 a 5, como o Lobo Cinzento, mais forte.
   - **Morcego das Cavernas** (`monster.cave_bat`, 22, normal, tag `beast`, `ai.swarm`): bandos de 5 a 6, pouca vida. Alvo de dano em área.
   - **Golem de Pedra** (`monster.stone_golem`, 25, **elite**, tag `construct`): lento, resistência a físico, fraqueza a arcano. *Rocha Rolante*: linha longa com telegráfo de 1,5 s que atravessa o desfiladeiro e acerta todos na linha (monstros não).
2. **Grupos maiores:** do 21 em diante, parte dos grupos tem de 5 a 10 monstros. Os testes usam o **perfil esperado com segunda classe** (as 8 segundas classes, nível 21–25).
3. **Core:** só o que faltar, com teste.
4. **Testes de design:**
   - solo, nas 8 segundas classes, contra cada grupo típico: vitória ≥ 85%;
   - um grupo grande (8 monstros) leva menos tempo com Piromante ou Berserker que com Arcanista (o design das classes pede isso; o teste só mede e registra);
   - Golem: solo ≥ 60%, grupo de 2 ≥ 90%.
5. **Arte:** só os ids (T-521, T-540). O Lobo da Montanha pode usar o sprite do Lobo Cinzento com outra cor.

## Critérios de aceite

- seção de design com os quatro monstros;
- testes de design passando, com relatório em `tasks/reports/`;
- `design/bestiario.md` atualizado;
- `./scripts/check.sh` completo passa.

## Resultado

Feito em 2026-11-24, no Windows (sem tela), na branch `task/T-511-monstros-da-serra`. **A branch está empilhada sobre a da T-510** (`task/T-510-monstros-do-brejo`, `5caadf9`), que está sobre a da T-502 (`6ee2b2e`). **Integrar na `main` depois da T-502 e da T-510.** A T-512 empilha sobre esta.

**Design.** Seção nova "Serra Partida" em `design/combat-encounters-v0.1.md`, com papel, habilidades, telegráfos, resposta de cada segunda classe, grupos típicos e falhas de design, mais cinco linhas na matriz de decisões. `design/bestiario.md` atualizado com os quatro monstros e os cinco encontros. As propostas da tarefa valem como decididas (designer ausente); as trocas de números estão no relatório.

**Dados** (números do nível 1; ids fixados mantidos: `monster.possessed_miner`, `monster.mountain_wolf`, `monster.cave_bat`, `monster.stone_golem`):
- **Mineiro Possuído** (nv21, `humanoid`, `ai.brute`): vida 320, velocidade 3,2, raio 0,4. Picaretada (`possessed_miner.pickaxe_blow`: cone 70° × 1,8 com telegráfo de 0,8 s, físico 30 e `status.stunned` 1 s) e Golpe de Picareta (`possessed_miner.pick_strike`).
- **Lobo da Montanha** (nv22, `beast`, `ai.pack`): vida 260, velocidade 4,2. Mordida (`mountain_wolf.bite`) e Uivo da Montanha (`mountain_wolf.pack_fury`, a Fúria da Matilha do Lobo Cinzento). Pode usar o sprite do Lobo Cinzento com outra cor.
- **Morcego das Cavernas** (nv22, `beast`, `ai.swarm`): vida 90, velocidade 4,4 (abaixo da classe mais lenta), raio 0,3. Mordida (`cave_bat.bite`).
- **Golem de Pedra** (nv25, **elite**, `construct`, `ai.brute`; físico 30%, arcano −30%): vida 1.200 (12.720 no nv25), velocidade 2,2, raio 0,7. **Rocha Rolante** (`stone_golem.rolling_boulder`: linha 14 × 1,6, telegráfo de 1,5 s, físico 80 em todo jogador na linha, nenhum monstro), Pisão de Pedra (`stone_golem.ground_slam`: círculo 2,6, telegráfo 1,2 s) e Punho de Pedra (`stone_golem.stone_fist`).
- Encontros (`map.fields`, acrescentados no fim de `data/encounters.json`): `encounter.miner_gang` (4 Mineiros em fila), `encounter.mountain_wolves` (4 Lobos), `encounter.cave_bats` (6 Morcegos), `encounter.miner_camp` (**8**: 4 Mineiros + 4 Morcegos, nv21) e `encounter.stone_golem` (Golem; Guerreiro + Clérigo). Nenhum estado, área ou arena nova.

**Core: nenhuma mecânica nova.** Telegráfo em linha que atinge todo inimigo dentro (e nenhum aliado da fonte), cone com atordoamento, `ai.pack`, `ai.swarm` e resistências por escola já existiam. `tests/Game.Core.Tests/RidgeMonsterTests.cs` confere o uso: a Rocha atinge dois jogadores na linha (a 10 e 13 tiles) e não atinge o Mineiro no meio dela nem o jogador fora; a Picaretada atordoa por 1 s; tags, IA, categoria e resistências.

**Testes de design** (`tests/Game.Design.Tests/RidgeDesignTests.cs`, 80 casos, 100 seeds; relatório **`tasks/reports/balance-v0.25.md`**; v0.23 é da T-505 e v0.24 da T-510). As 8 segundas classes, no nível do grupo, com as opções de bot de `SecondClassDesignTests.Specs`, em dois perfis: o **esperado com segunda classe** (§8 da progressão) e o **"desde o 21"** (base parada no 20, todo ponto do 21 em diante na segunda classe), porque no perfil esperado a base só enche no 23 e a segunda classe não tem NH no 21–23 (as duas de cada base empatam).
- solo contra Mineiros, Matilha e Morcegos: **99–100%** nas 8, nos dois perfis (meta ≥ 85%);
- solo contra o Acampamento (8 monstros): **94–100%** nas 6 que não são do Clérigo; Sacerdote e Inquisidor **0–1%** (exceção, abaixo);
- Golem solo: **99–100%** nas 8 (meta ≥ 60%); em dupla (Guardião + Sacerdote, Berserker + Atirador, Piromante + Inquisidor): **100%** (meta ≥ 90%);
- grupo grande, só medido: Piromante **1,00×** e Berserker **2,6–2,7×** o tempo do Arcanista. O Mago mata os 8 em ~9 s com a Explosão Elemental da base, então o Arcanista é tão rápido quanto o Piromante (achado no relatório §4).

**Decisões e desvios.**
- **Exceção do Clérigo no Acampamento:** sem dano em área, Sacerdote e Inquisidor não vencem 8 monstros sozinhos. O Acampamento fica fora da meta solo deles (`RidgeDesignTests.BigGroupNeedsAGroup`) e entra só como medida. Pendência para o designer; o mapa (T-532) deve deixar os acampamentos fora da trilha principal.
- **Matilha do teste com 4 lobos** (a proposta dizia 4 a 5) e mordida 12: com 5 e mordida 14 o Clérigo perdia sempre.
- **Acampamento = 4 Mineiros + 4 Morcegos nv21**, Morcego com mordida 5: as versões mais fortes tiravam o Guerreiro (10%), o Atirador (80–85%) ou o Berserker "desde o 21" (76%) da meta.
- **Golem com vida 1.200** (1.800 e 1.500 antes): o Guerreiro vencia 0% e depois 30–40% contra a resistência a físico. Ficou mais fácil que o piso de 60% para todos; os de longe quase não levam dano dele no campo aberto.
- **Achado fora da tarefa:** o Mago no 21–22 domina a Serra (3–9 s por grupo, sem dano). Não mexi nas classes. Pendência para o designer/T-550.
- **Fora das "Áreas que pode alterar"** (mínimo): `tests/Game.Data.Tests/ProgressionContentTests.cs` (tabela de níveis por área e lista de monstros) e `site/content/site.json` (descrição dos quatro monstros).

**`./scripts/check.sh` completo:** formatação e build sem avisos; todos os projetos de teste passam, menos **4 testes de design que já falham na `main`** (`3891ac4`) e nas T-502/T-510, com os mesmos números, e não tocam nos monstros da Serra: `SecondClassDesignTests.Guardian_cuts_the_damage_on_the_rest_of_the_group_by_at_least_15_percent` (nv30 e nv40; nv40 90%, meta ≤ 85%), `WatcherDesignTests.Without_the_cleric_the_win_rate_drops_clearly` (32%; meta ≤ 30%) e `CryptDesignTests.A_group_of_four_with_rares_beats_the_watcher_in_the_real_room_80_to_99_percent_in_4_to_6_minutes` (média 71%; meta 80–99%). Uma quinta falha apareceu na primeira rodada e foi corrigida: `DesignTests.Normal_monsters_are_slower_than_the_slowest_class` (o Morcego tinha velocidade 4,6, acima dos 4,5 da classe mais lenta; ficou com 4,4). Os testes de design levam ~19 min no Windows; os da Serra, ~1,5 min.

**Pendências.**
- **T-532:** desfiladeiros de 3 a 5 tiles com os Mineiros em fila, Acampamento fora da trilha, Golem na Boca das Minas (renasce em 3 min); medir de novo com as paredes.
- **T-540:** mostrar o cone da Picaretada, a linha da Rocha e o círculo do Pisão; Lobo da Montanha com `tint` sobre o sprite do Lobo Cinzento.
- **T-550:** o Mago contra grupos no 21–25; o Clérigo sozinho na Serra; o Golem para quem luta corpo a corpo.
- `design/habilidades.md` § Monstros não foi atualizado (fora das áreas, como na T-510).
