# T-505 — Simulador: XP por hora de cada área e metas do 16 ao 27

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** —
- **Libera:** T-530, T-532, T-533
- **Áreas que pode alterar:** `src/Game.Sim/`, `tests/Game.Design.Tests/`, `tests/Game.Sim.Tests/`, `tasks/reports/`

## Contexto

`design/segunda-classe-v0.1.md` §3: "o que decide se fica bom é a XP por minuto, não a XP por monstro", e "a meta a definir é a XP por hora de cada área". Hoje os testes de design das zonas (`ForestZoneDesignTests`, `RuinsZoneDesignTests`) medem o ritmo de uma faixa de níveis com um modelo de ritmo, sem meta.

O designer decidiu **manter a curva de XP** (`design/mapas-16-40-v0.1.md` §8, decisão 3). Com ela, do 15 ao 20 são ~5 h e do 20 ao 25 ~8 h (§1 do esboço). As áreas novas precisam render perto disso, e a masmorra mais que o campo, porque dá mais trabalho e pede grupo.

## Requisitos

1. Uma medida só de **XP por hora** de uma zona, para um perfil (classe, nível, segunda classe, equipamento esperado) e um tamanho de grupo, contando:
   - o tempo de luta de cada grupo de monstros (simulador);
   - o deslocamento entre os grupos (distância no mapa a pé), o descanso e o renascimento dos monstros, como o modelo de ritmo de hoje;
   - a divisão de XP do grupo (T-310).
2. Medir as zonas de hoje (Campos, Floresta, Ruínas, Cripta) e registrar no relatório, como referência do que o designer já jogou e aprovou.
3. **Metas** para as zonas novas, em testes de design que os mapas (T-530, T-532, T-533) vão usar:
   - **campo:** com o perfil esperado solo, subir a faixa da zona leva entre 75% e 130% do tempo da curva (Brejo 16→20: ~5 h; Serra 21→25: ~7 h);
   - **masmorra:** XP por hora, por pessoa, num grupo de 4, pelo menos **1,3 ×** a do campo da mesma faixa;
   - **chefe de campo:** só a medida, sem meta.
   O designer pode trocar os números. Registre o que for decidido.
4. A medida aceita uma zona qualquer pelo id, para a fase 6 reaproveitar.

## Critérios de aceite

- relatório `tasks/reports/balance-v0.23.md` (ou o próximo número livre) com a XP por hora das zonas de hoje e as metas;
- testes de design das metas prontos para receber as zonas novas (podem ficar marcados como pendentes até os mapas existirem, como o `PlaytestRebalance.Pending`);
- `./scripts/check.sh` completo passa.

## Resultado

**Feito (branch `task/T-505-xp-por-hora-por-area`):**

- **Medida de XP por hora** (`tests/Game.Design.Tests/ZonePace.cs`): uma zona qualquer pelo id (`ZonePace.Field`, `FieldBoss`, `Dungeon`), com perfil (4 bases antes do 20; as 8 segundas classes solo e o grupo Guardião + Caçador + Piromante + Sacerdote do 20 em diante), tamanho de grupo, luta do simulador (perfil esperado do nível do grupo), caminhada pelo mapa (`Pathfinder`, circuito dos grupos), descanso (50 s por grupo no campo, 45 s por sala na masmorra), renascimento (teto do que renasce) e divisão de XP da T-310 (`rules.Share`). Detalhes e premissas em `tasks/reports/balance-v0.23.md` §1.
- **Zonas de hoje medidas** (`ZonePaceDesignTests`, sem meta; reaproveitam os lotes de `FieldsDesignTests`, `ForestZoneDesignTests`, `RuinsZoneDesignTests` e `CryptDesignTests`, então não rodam lutas a mais): Campos 1→5 **0,92 h** (1.759 XP/h; bate com os 55–65 min do teste da T-215), Floresta 5→8 **1,30 h** (4.001 XP/h), Ruínas 9→12 **3,20 h** (4.046 XP/h), Cripta em grupo de 4 do 12 ao 14 **7.296 XP/h por pessoa** (~2,9 h para o 12→15; **1,8×** as Ruínas na faixa delas, **3,0×** no mesmo nível).
- **Metas das zonas novas** (`ZoneGoalDesignTests`): Brejo 16→20 entre 75% e 130% de 5 h (3,75–6,5 h); Serra 21→25 entre 75% e 130% de 7 h (5,25–9,1 h), com segunda classe; Minas em grupo de 4 ≥ **1,3×** o XP/h do campo (Serra solo, níveis 24 e 25), por pessoa; chefe do Brejo só medido.
- **Testes do modelo** (rápidos, luta fixa): ciclo, teto de renascimento, divisão de XP do grupo, piso de XP, zona qualquer pelo id, ida à masmorra, perfis com segunda classe do 20 em diante.
- Relatório `tasks/reports/balance-v0.23.md`.

**Decisões:**

- **Pendentes sem `Skip` manual:** o `PlaytestRebalance.Pending` foi removido na T-402. Em vez dele, `[FactIfZone(...)]` pula o teste enquanto a zona não existe em `data/zones.json` e o **liga sozinho** quando ela entra. Os ids são os das tarefas: `zone.drowned_marsh` (T-530), `zone.broken_ridge` (T-532), `zone.deep_mines` (T-533, que também exige as duas anteriores por comparar com a Serra). Se o teste ligado falhar, a tarefa de mapa ajusta o mapa ou leva o número ao designer.
- **Onde fica o código:** em `tests/Game.Design.Tests` (não em `tests/Game.Sim`), porque os grupos de uma zona (`FieldGroups`) e o encontro deles já vivem lá. Nada mudou em `tests/Game.Sim`. Tornei internos `ForestZoneDesignTests.Run/Typical`, `RuinsZoneDesignTests.Run/Typical` e `CryptDesignTests.Room/Boss` para reaproveitar os lotes.
- **Números das metas:** Brejo 5 h; Serra **7 h** (as ~8 h do 20→25 menos ~1 h do nível 20, como pede a tarefa). XP/h que isso exige: Brejo ~9.400 (7.250–12.570), Serra ~10.600 (8.130–14.100), contra ~4.000 das Ruínas hoje (relatório §3 e §4). Nenhum número do jogo mudou; os da meta ficam como constantes em `ZoneGoalDesignTests` para o designer trocar.
- **Campo "da mesma faixa" das Minas** = a Serra, solo, nos níveis 24 e 25. O chefe da masmorra entra com a luta de grupo na arena aberta (360 s) até a T-533 trocar pela luta real na sala.
- **Relatório = v0.23** (último era v0.22). A T-510 roda em paralelo e também pode criar relatório de balanceamento: ela deve usar o **v0.24** (não existia nenhum v0.23 além deste na branch dela).

**Pendências / alertas:** as metas do Brejo, Serra e Minas só medem de verdade quando os mapas existirem (T-530, T-532, T-533). A medida usa a arena aberta e não conta mortes, saque nem reentrada; o tempo real vem do playtest (T-550).

**Verificação (`./scripts/check.sh` completo):** `dotnet format --verify-no-changes` e `dotnet build -warnaserror` passaram, e todos os projetos de teste passaram, menos o `Game.Design.Tests`: **4 falhas em 319** (311 aprovados, 4 pulados: as 4 metas das zonas novas), 46 min nesta máquina. As 4 falhas são **as mesmas que a T-500 já registrou na `main`** (`2cd506b`), anteriores a esta tarefa, e não têm relação com ela:
- `WatcherDesignTests.Without_the_cleric_the_win_rate_drops_clearly` (99% sem o Clérigo);
- `CryptDesignTests.A_group_of_four_with_rares_beats_the_watcher_in_the_real_room_80_to_99_percent_in_4_to_6_minutes` (71%, meta 80–99%);
- `SecondClassDesignTests.Guardian_cuts_the_damage_on_the_rest_of_the_group_by_at_least_15_percent` (níveis 30 e 40).

Todos os testes novos (`ZonePaceDesignTests`, 11) passam. A mudança em `CryptDesignTests`, `ForestZoneDesignTests` e `RuinsZoneDesignTests` é só de visibilidade (`private` → `internal`).
