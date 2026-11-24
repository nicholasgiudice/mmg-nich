# T-510 — Monstros do Brejo dos Afogados e a Bruxa do Brejo

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** — (integrar depois da T-502, que traz a água funda)
- **Libera:** T-511, T-513, T-530, T-540
- **Áreas que pode alterar:** `design/bestiario.md`, `design/combat-encounters-v0.1.md`, `data/monsters.json`, `data/abilities_monsters.json`, `data/statuses.json`, `data/encounters.json`, `data/maps/` (arenas de teste novas), `src/Game.Core/Combat/Ai/` (perfil novo, se precisar), `src/Game.Core/` (só mecânica nova exigida pelos monstros), `tests/`

## Contexto

O Brejo dos Afogados (`design/mapas-16-40-v0.1.md` §3.1) é o campo do nível 16 ao 20, a ponte entre o primeiro mapa e a segunda classe. Dá para jogar sozinho com a classe base. O truque da área é a água funda (T-502). Termina com a **Bruxa do Brejo**, chefe de campo nível 20.

Mesmo formato da T-340 e da T-341: design primeiro, depois dados, Core só no que faltar, e testes de design no simulador.

## Requisitos

1. **Design** (seção nova em `design/combat-encounters-v0.1.md`), com papel, habilidades, telegráfos e a resposta esperada de cada classe. Propostas (o designer pode trocar; registre o que for decidido):
   - **Afogado** (`monster.drowned`, 16, normal, tag `undead`): lento, em bandos de 3 a 4. *Agarrão*: telegráfo curto e `status.snared` por 1,5 s. Ensina a não ficar cercado na água.
   - **Homem-lagarto do brejo** (`monster.marsh_lizardman`, 17, normal, tags `humanoid` e `amphibious`): rápido, em pares. Não sente a água funda. *Bote*: investida em linha com telegráfo, que pune quem luta parado.
   - **Serpente do Charco** (`monster.marsh_serpent`, 18, normal, tag `beast`): à distância, foge de quem chega perto (`ai.ranged_kiter`). *Cuspe*: projétil com `status.poison`.
   - **Bruxa do Brejo** (`monster.marsh_witch`, 20, **chefe**, tag `humanoid`): três fases, como a Matriarca:
     1. poças venenosas no chão, com telegráfo;
     2. invoca afogados (`summon`, como a Matriarca invoca aranhas);
     3. *Lodo que Puxa*: área grande de `status.deep_water` em volta dela por alguns segundos, mais projéteis.
2. **Dados** com nível, categoria, vida e potência no nível 1, como os monstros de hoje. Tags que as classes já usam (`undead`) mantêm o bônus do Clérigo.
3. **Core:** só o que faltar, cada peça com teste. A área de `status.deep_water` da Bruxa usa o estado da T-502.
4. **Encontros de teste e testes de design**, com o perfil esperado do nível (T-206/T-402):
   - solo, nas 4 classes base, contra cada grupo típico (afogados, lagartos, serpentes, misto): vitória ≥ 85%;
   - perfil fraco (3 níveis abaixo): ≤ 50%;
   - **Bruxa:** grupo de 3 do nível 19–20 vence ≥ 80%; solo ≤ 30%.
5. **Arte:** só os ids. Os sprites vêm da T-521 e o cliente liga na T-540. Use os ids acima: a T-521 monta os sprites com eles. Raio de 0,35 a 0,45 para os humanoides LPC.

## Critérios de aceite

- seção de design com os quatro monstros;
- testes de design passando, com o relatório `tasks/reports/balance-v0.NN.md` (próximo número livre);
- `design/bestiario.md` atualizado;
- `./scripts/check.sh` completo passa.

## Resultado

Feito em 2026-11-24, no Windows (sem tela), na branch `task/T-510-monstros-do-brejo`. **A branch está empilhada sobre a da T-502** (`task/T-502-terreno-que-deixa-lento`, commit `6ee2b2e`): usa o `status.deep_water` dela e muda o `TerrainSystem` dela. **Integrar depois da T-502.** As T-511/T-512/T-513 empilham sobre esta.

**Design.** Seção nova "Brejo dos Afogados" em `design/combat-encounters-v0.1.md`, com papel, habilidades, telegráfos, resposta de cada classe, grupos típicos e falhas de design, mais quatro linhas na matriz de decisões. `design/bestiario.md` atualizado com os quatro monstros e os cinco encontros. As propostas da tarefa valem como decididas (designer ausente); as trocas de números estão no relatório.

**Dados** (números do nível 1; ids fixados mantidos: `monster.drowned`, `monster.marsh_lizardman`, `monster.marsh_serpent`, `monster.marsh_witch`; raio 0,4–0,45):
- **Afogado** (nv16, `undead`, sagrado −40%, `ai.brute`): vida 300, velocidade 2,6. Agarrão (cone com telegráfo de 0,6 s, `status.snared` 1,5 s) e Garra.
- **Homem-lagarto** (nv17, `humanoid` + `amphibious`, `ai.brute`): vida 440, velocidade 4,0. Bote (linha de 4 com telegráfo de 0,6 s e avanço) e Garra.
- **Serpente do Charco** (nv18, `beast`, `ai.ranged_kiter`): vida 200, velocidade 3,0. Cuspe Venenoso (`status.poison`) e Picada forte para quem a alcança.
- **Bruxa do Brejo** (nv20, chefe, `ai.boss`): vida 5.500 (36.850 no nv20). Fase 1 poças venenosas com telegráfo; fase 2 invoca Afogados (conjuração interrompível, como a Matriarca); fase 3 *Lodo que Puxa*: área presa a ela (`followOwner`, raio 4,5, 6 s) que mantém `status.deep_water` em quem está dentro, mais poças rápidas e o Dardo do Lodo.
- Áreas novas `area.witch_poison_puddle` e `area.pulling_mud` em `data/areas.json`. Nenhum estado novo em `statuses.json`.
- Encontros `encounter.drowned_pack`, `encounter.marsh_lizardmen`, `encounter.marsh_serpents`, `encounter.marsh_mixed` (`map.fields`) e `encounter.marsh_witch` (`map.matriarch_nest`). Nenhuma arena nova.

**Core.** Uma mudança: o `TerrainSystem` (T-502) tirava `status.deep_water` de quem estivesse fora da água do mapa, viesse de onde viesse, o que apagaria o Lodo da Bruxa no Brejo real. Agora ele tira só o estado que ele mesmo pôs (sem fonte); o da Bruxa acaba pela duração. Testes novos em `tests/Game.Core.Tests/MarshMonsterTests.cs` (terreno, Lodo que segue a Bruxa e atinge só inimigos, tags e fases); os testes da T-502 continuam passando.

**Testes de design** (`tests/Game.Design.Tests/MarshDesignTests.cs`, 38 casos; relatório **`tasks/reports/balance-v0.24.md`**; o v0.23 é da T-505), 100 seeds:
- solo, perfil esperado, 4 classes × 4 grupos: 92–100% (meta ≥ 85%);
- perfil fraco: 0–24% para Guerreiro e Clérigo nos grupos corpo a corpo; quando vence, 1,28–2,22× o tempo (meta ≤ 50% ou ≥ 1,25×). O Clérigo fraco nas Serpentes e nas Palafitas: 1,13× e 1,15× (exceção abaixo);
- Bruxa, grupo de 3 (Guerreiro + Arqueiro + Clérigo): **93% no nv19, 100% no nv20** (meta ≥ 80%), ~81 s; solo no nv20: **0%** nas 4 classes (meta ≤ 30%).

**Decisões e desvios.**
- **Afogado fraco a sagrado** (−40%), que não estava na proposta: sem isso o Clérigo perdia para o bando.
- **Homem-lagarto resistente, não frágil** (vida 440, golpes menores; Bote de 40 e linha de 4): o Bote forte matava o Arqueiro de um golpe e a Garra forte derrubava o Clérigo.
- **Serpente com a força na Picada**, cuspe e veneno fracos, mais lenta (3,0) e recuando menos (3): assim o Arqueiro sobrevive ao dano à distância e o Guerreiro a alcança (com a proposta, vencia 0%). As duas Serpentes do encontro começam afastadas.
- **Grupo misto = 1 Afogado + 1 Homem-lagarto.** Os mistos de 3 monstros tiravam o Guerreiro (47–73%) ou o Arqueiro (7–13%) da meta.
- **Exceção do Clérigo no perfil fraco: ≥ 1,10× o tempo** (a regra geral é 1,25×), como na Floresta e no Guarda. Ele se cura e alcança as Serpentes de longe, e no nível 15–18 três níveis valem menos que no 5–9. Pendência para o designer (relatório §3).
- **Poças da Bruxa sem Veneno e com dano baixo** (3,5 por segundo): os bots não saem de áreas já no chão, e a poça matava o tanque. Quem sai delas leva menos do que o simulador mostra.
- **Bruxa no `map.matriarch_nest`** até o mapa do Brejo (T-530).
- **Fora das "Áreas que pode alterar"** (mínimo): `data/areas.json` (as duas áreas novas); `src/Game.Core/Combat/Areas/TerrainSystem.cs` (está em `src/Game.Core/`, mas é da T-502); `tests/Game.Data.Tests/ProgressionContentTests.cs` (a tabela de níveis por área ganhou os quatro monstros); `site/content/site.json` (nomes das três fases da Bruxa e descrição dos quatro monstros, senão o site mostra o id da fase e `SiteTests.Ids_never_show_up_in_the_text` falha).

**`./scripts/check.sh` completo:** build sem avisos e todos os projetos de teste passam, menos **4 testes de design que já falham na `main`** (`3891ac4`) e na T-502 (`6ee2b2e`), com os mesmos números, e não tocam nos monstros do Brejo: `SecondClassDesignTests.Guardian_cuts_the_damage_on_the_rest_of_the_group_by_at_least_15_percent` (nv30: 94%, nv40: 90%; meta ≤ 85%), `WatcherDesignTests.Without_the_cleric_the_win_rate_drops_clearly` (32%; meta ≤ 30%) e `CryptDesignTests.A_group_of_four_with_rares_beats_the_watcher_in_the_real_room_80_to_99_percent_in_4_to_6_minutes` (média 71%; meta 80–99%). Não investiguei a causa; ficam para quem integrar a fase 5.

**Pendências.**
- **T-530:** pôr `ignoreTags = amphibious` nas áreas de água funda e medir os grupos de novo com água no mapa.
- **T-540:** mostrar o cone curto do Agarrão, a linha do Bote, a conjuração e a área do Lodo.
- **T-550:** conferir as poças da Bruxa com jogadores de verdade e se o Clérigo fraco vencendo no Charco incomoda.
- `design/habilidades.md` § Monstros não foi atualizado (fora das áreas, como na T-340 e na T-341).
