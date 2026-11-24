# Relatório de balanceamento v0.23 (T-505): XP por hora de cada área e metas do 16 ao 27

Fase 5, medida de ritmo. `design/segunda-classe-v0.1.md` §3 diz que "o que decide se fica bom é a XP por minuto, não a XP por monstro" e que "a meta a definir é a XP por hora de cada área". Esta tarefa cria a **medida** (uma só, para qualquer zona), mede as zonas de hoje (Campos, Floresta, Ruínas e Cripta) como referência do que o designer já jogou e aprovou, e deixa prontas as **metas** das zonas novas (Brejo, Serra, Minas), que ligam sozinhas quando os mapas entram. Nenhum dado do jogo mudou; a curva de XP fica como está (`design/mapas-16-40-v0.1.md` §8, decisão 3).

**Resumo:**

- Campos 1→5: **0,92 h**, 1.759 XP/h por pessoa. Floresta 5→8: **1,30 h**, 4.001 XP/h. Ruínas 9→12: **3,20 h**, 4.046 XP/h. Cripta (grupo de 4, 12→15): **~2,9 h**, ~7.300 XP/h por pessoa. Somando o que o designer jogou, do 1 ao 15 dá ~8,3 h, perto das ~9 h que o esboço previa para a curva (§1).
- Para a curva dar certo, o **Brejo precisa render ~9.400 XP/h** (16→20 em 5 h) e a **Serra ~10.600 XP/h** (21→25 em 7 h): o dobro e mais do que as Ruínas rendem hoje (§3). É esperado, porque o nível sobe e cada nível pede mais XP; o número é o que as tarefas de mapa precisam alcançar.
- A **Cripta rende 1,8×** as Ruínas por pessoa (3,0× contra as Ruínas fora da faixa deles, no mesmo nível). A meta de 1,3× das Minas é, portanto, mais baixa do que o que o designer já aprovou na Cripta.

## 1. A medida

`tests/Game.Design.Tests/ZonePace.cs` (`ZonePace`): a mesma ideia do modelo de ritmo dos Campos (`FieldGroups.Pace`, T-215), mas para uma zona qualquer pelo id, com tamanho de grupo e segunda classe. O código fica em `tests/Game.Design.Tests` e não em `tests/Game.Sim`, porque os grupos de monstros de uma zona (`FieldGroups`, `FieldGroup`) e o encontro de teste deles já vivem lá, junto dos testes de zona. A medida só chama o simulador (`Batch.Run`), sem mudar nada nele.

Para um nível L (um por vez, de `from` a `to − 1`) e um tamanho de grupo n:

| Parte | Como conta |
|---|---|
| **O que se caça** | os grupos de monstros da zona (pontos de surgimento da mesma subárea a até 4 tiles, como nos testes de zona) do **maior nível de grupo que não passa de L** (ou o menor, se L está abaixo de todos). Sem chefe e sem objetivo (`ZonePace.IsHuntable`); quem chama pode filtrar áreas que não são caça solo (as Ruínas tiram o Pátio e a Torre) |
| **Luta** | tempo médio de vitória do simulador, perfil esperado do **nível do grupo** (como as Ruínas e a Floresta), os monstros nos níveis do mapa, 100 seeds. Solo: média das classes (4 bases antes do 20; as **8 segundas classes** do 20 em diante, `SecondClassDesignTests.WithSecondClass`). Grupo: um grupo de 4 (4 bases antes do 20; Guardião, Caçador, Piromante e Sacerdote do 20 em diante). Sem vitória, conta o limite de tempo (120 s solo, 180 s em grupo, 360 s chefe) e a taxa de vitória denuncia |
| **Deslocamento** | circuito fechado pelos grupos caçados (sempre o mais perto que falta), caminhada **pelo mapa** (`Pathfinder`), à velocidade média das classes. Antes eram 10 s fixos dentro dos 60 s do modelo dos Campos |
| **Descanso** | 50 s por grupo (os 60 s do modelo dos Campos menos os ~10 s que agora são caminhada medida) no campo; 45 s por sala na masmorra (`CryptDesignTests.PauseSeconds`) |
| **Renascimento** | no campo, o jogador nunca limpa mais grupos do que renascem: `min(1 / ciclo, Σ 1 / renascimento de cada grupo)` |
| **Divisão de XP** (T-310) | cada pessoa leva `rules.Share(XP do monstro, n)` = `XP × (1 + 10% × (n − 1)) / n` (32,5% por pessoa num grupo de 4), com o modificador de nível calculado pelo nível do jogador (`MonsterXp(nível do monstro, categoria, nível do jogador)`) |

Saídas: `ZonePace.Field` (por nível: XP por grupo, luta, caminhada, XP/h, minutos para o próximo nível; no total: horas e XP/h da faixa), `ZonePace.FieldBoss` (chefe de campo: XP por pessoa por ciclo de renascimento, só a medida) e `ZonePace.Dungeon` (uma ida completa: lutas + pausas + caminhada, XP por pessoa, XP/h repetindo a ida).

**O que a medida não conta** (premissas iguais às do modelo de ritmo anterior): mortes e o tempo de voltar do renascimento (a taxa de vitória mínima vai junto de cada medida); tempo de saque, venda e poção; reentrar na masmorra; a luta na arena aberta do protótipo em vez do corredor do mapa; o jogador escolhendo os grupos mais fáceis. A medida dá um **ritmo de referência**, não o tempo real: quem diz o tempo real é o playtest (T-550).

### Conferência com o modelo anterior

Nos Campos, a medida usa os mesmos lotes de `FieldsDesignTests` (solo, 4 classes). Resultado: nível 5 em **0,92 h = 55 min**, dentro dos 55–65 min que o teste da T-215/T-302 exige (e a caminhada agora medida no mapa dá 5–6 s por grupo em vez dos 10 s fixos). Para a Floresta e as Ruínas a medida reaproveita os lotes de `ForestZoneDesignTests` e `RuinsZoneDesignTests`, então não roda lutas a mais.

## 2. Medido: as zonas de hoje (referência, sem meta)

Solo, perfil esperado sem segunda classe. XP por pessoa por hora; `h` = horas para a faixa.

| Zona | Faixa | Horas | XP/h (média) | Por nível |
|---|---|---|---|---|
| Campos da Vigília | 1 → 5 | **0,92** | 1.759 | nv1 585 · nv2 1.087 · nv3 2.154 · nv4 2.297 |
| Floresta Baixa | 5 → 8 | **1,30** | 4.001 | nv5 3.568 · nv6 4.168 · nv7 4.149 |
| Ruínas da Estrada Antiga | 9 → 12 | **3,20** | 4.046 | nv9 3.461 · nv10 4.171 · nv11 4.458 |

Detalhe do ciclo (luta + caminhada por grupo): Campos 18–35 s + 5–6 s; Floresta 8–15 s + 8–13 s; Ruínas 40–46 s + 6–8 s. Menor taxa de vitória: Campos 100%, Floresta 97%, Ruínas 100%.

A Floresta tem luta curta (8–15 s por grupo) e a caminhada pesa quase o mesmo que a luta (8–13 s). As Ruínas têm lutas de ~40 s e rendem o mesmo XP/h da Floresta com monstros de nível maior.

### Cripta do Vigia (grupo de 4, uma ida completa)

Rotas e lutas de `CryptDesignTests` (salas no perfil esperado do nível do grupo, o Vigia com os selos na sala real, 24–26 min por ida). XP de cada pessoa já dividida (32,5% por monstro), no nível do jogador:

| Nível do jogador | Rota segura (sem a Rota dos Mortos) | Rota de risco (sem a Passagem Estreita) |
|---|---|---|
| 12 | 3.317 XP em 24,2 min = **8.220 XP/h** | 3.661 XP em 26,1 min = **8.402 XP/h** |
| 13 | 2.937 XP = **7.278 XP/h** | 3.252 XP = **7.463 XP/h** |
| 14 | 2.459 XP = **6.094 XP/h** | 2.752 XP = **6.316 XP/h** |

Média: **7.296 XP/h** por pessoa. Do 12 ao 15 só na Cripta: 5.957 + 6.880 + 7.862 XP ÷ as taxas acima = **2,96 h** (rota segura) ou **2,88 h** (rota de risco). A ida é o que as salas e o Vigia dão e a pausa de 45 s por sala pesa quase a metade do tempo (11–12 min de 24–26). A taxa de vitória mínima (a pior luta da ida) é 64%.

**Cripta contra o campo:**

| Comparação | Cripta (média) | Campo | Razão |
|---|---|---|---|
| Ruínas solo **nos mesmos níveis** (12, 13, 14; os grupos de nível 11 dão XP reduzido) | 7.296 | 2.420 | **3,02×** |
| Ruínas solo **na própria faixa** (9 → 12) | 7.296 | 4.046 | **1,80×** |

Como o XP/h cresce com o nível, a segunda linha é a comparação mais justa com "o campo da mesma faixa", mas o campo da faixa 12–15 não existe hoje (as Ruínas acabam no 11). O que a Cripta mostra: **1,8× a 3,0× o campo por pessoa**, bem acima do 1,3× pedido para as Minas.

Somando o que o designer jogou: 0,92 (Campos) + 1,30 (Floresta) + 3,20 (Ruínas 9→12) + ~2,9 (Cripta 12→15) = **~8,3 h**, mais o trecho do nível 8 ao 9, que a medida não cobre, perto das **~9 h** do 1 ao 15 do esboço (`design/mapas-16-40-v0.1.md` §1).

## 3. Metas das zonas novas

Os números vêm da curva mantida (XP para o próximo nível `68 × L^1,8`, `design/mapas-16-40-v0.1.md` §1: do 15 ao 20 ~5 h e do 20 ao 25 ~8 h) e da tarefa (75% a 130% do tempo da curva). O designer pode trocar qualquer um: estão como constantes em `ZoneGoalDesignTests` (`BrejoHours`, `SerraHours`, `FieldMin`, `FieldMax`, `DungeonOverField`).

| Zona | Meta | Tempo da curva | Janela de tempo (75%–130%) | XP total da faixa | XP/h por pessoa que isso exige |
|---|---|---|---|---|---|
| **Brejo dos Afogados** (campo), 16 → 20, solo, classe base | `Marsh_solo_from_level_16_to_20_…` | ~5 h | **3,75 h a 6,5 h** | 47.131 | 7.251 a 12.568 (alvo 9.426) |
| **Serra Partida** (campo), 21 → 25, solo, com segunda classe | `Ridge_solo_with_a_second_class_from_level_21_to_25_…` | ~7 h | **5,25 h a 9,1 h** | 74.007 | 8.133 a 14.097 (alvo 10.573) |
| **Minas Fundas** (masmorra, 4), 24–27 | `Mines_in_a_party_of_four_give_at_least_1_3_times_…` | — | — | — | por pessoa, num grupo de 4: **≥ 1,3 ×** o XP/h do campo da faixa (Serra solo nos níveis 24 e 25) |
| **Bruxa do Brejo** (chefe de campo, nv20) | `Marsh_field_boss_is_measured_without_a_goal` | — | só a medida | — | XP por pessoa por ciclo de renascimento (10 min) |

Decisões (o designer pode trocar):

- **Serra: 7 h, não 8 h.** O esboço dá 8 h do 20 ao 25; a Serra começa no 21 e o 20 → 21 cai no fim do Brejo e na viagem até a Serra. As ~7 h da tarefa são essas 8 h menos ~1 h do nível 20.
- **"Campo da mesma faixa" das Minas** = a Serra, solo, nos níveis 24 e 25 (a faixa 24–27 das Minas começa dentro da Serra e passa dela). Compara-se a média da masmorra (ida completa, repetida) com a média do campo nos dois níveis.
- **Chefe da masmorra** (Capataz Forjado): na meta das Minas ele entra com a luta de grupo na arena aberta (limite de 360 s), como aproximação. A T-533 pode trocar pela luta real posta na sala do mapa, como a Cripta faz com o Vigia (`CryptDesignTests.Boss`).
- **Grupo de referência com segunda classe:** Guardião (tanque), Caçador (área), Piromante (área) e Sacerdote (cura). Solo: a média das 8 segundas classes (como o resto dos testes da fase 4).
- **Filtro de grupos solo:** `SoloHunt` em `ZoneGoalDesignTests` (hoje: tudo menos chefe e objetivo). Se a Serra ou o Brejo tiverem áreas feitas para grupo (como o Pátio dos Mortos), a T-530 e a T-532 as tiram ali, como as Ruínas.

### Como ligam (T-530, T-532, T-533)

Os testes usam `[FactIfZone("zone.drowned_marsh")]` (`ZoneGoalDesignTests.cs`): se a zona **não existe** em `data/zones.json`, o teste fica **pulado** com a mensagem "pendente: a zona … ainda não existe"; assim que a zona entra nos dados, ele **liga sozinho** e passa a valer na hora (sem trocar `Skip` na mão, que é o que o `PlaytestRebalance.Pending` exigia). Os ids são os das tarefas de mapa: `zone.drowned_marsh` (T-530), `zone.broken_ridge` (T-532), `zone.deep_mines` (T-533; a meta da masmorra pede também as duas anteriores, porque compara com a Serra). Se um teste ligado falhar, a tarefa de mapa ajusta o mapa (mais monstros, grupos mais perto, renascimento mais curto) ou leva o número ao designer; ela não desliga o teste.

Para ver os números de uma zona nova (ex.: no `dotnet test`):

```bash
dotnet test tests/Game.Design.Tests --filter "FullyQualifiedName~ZoneGoalDesignTests" --logger "console;verbosity=detailed"
```

## 4. Alerta para as tarefas de mapa

- **O Brejo precisa de ~9.400 XP/h e a Serra de ~10.600**, contra ~4.000 das Ruínas. Os monstros dão `6 × nível` (96–114 no Brejo contra 66 no nível 11) e cada nível pede ~104–119 monstros do próprio nível no Brejo, 130–149 na Serra. Para chegar lá, o campo precisa de **lutas curtas**, grupos **perto uns dos outros** e **renascimento rápido** (se o jogador limpa os grupos mais depressa do que eles renascem, quem limita o XP/h é o renascimento; a medida mostra o teto de cada nível).
- A **Floresta** (8–15 s de luta por grupo) rende por hora o mesmo que as Ruínas com monstros de nível menor; as Ruínas têm lutas mais longas (~40 s por grupo).
- A medida usa a **arena aberta**: monstros que atrapalham o caminho (água funda do Brejo, que deixa lento: T-502; corredores da Serra) deixam a caminhada medida acima do que o jogador gasta de verdade, e a luta pode ficar mais fácil ou mais difícil do que no mapa. Registrar o tempo real no playtest.

## 5. Testes

`tests/Game.Design.Tests/ZonePaceDesignTests.cs` (rápidos, sem lotes de simulador, com a luta trocada por um valor fixo):

- o ritmo de campo é a XP por grupo sobre o ciclo (caminhada + descanso), limitado pelo renascimento (`Field_pace_is_the_xp_per_group_over_the_cycle_of_walk_and_rest`, `Slower_respawn_never_makes_the_pace_faster_than_what_respawns`);
- a divisão de XP do grupo é a da T-310 (`Party_members_get_the_t310_share_of_each_monsters_xp`) e o piso de XP de quem está muito acima vale (`A_character_far_above_the_monsters_gets_the_xp_floor`);
- aceita qualquer zona pelo id e falha com `KeyNotFoundException` numa zona que não existe;
- uma ida à masmorra soma as salas e o chefe (`A_dungeon_run_sums_the_rooms_and_the_boss`);
- do 20 em diante o perfil tem segunda classe e as lutas rodam (`From_level_20_the_profiles_have_a_second_class_and_the_fights_run`: um grupo das Ruínas levado ao nível 21, solo e em grupo de 4).

Medidas das zonas de hoje (com lotes reaproveitados dos testes de zona; só a medida, sem meta): `Fields_solo_from_level_1_to_5_is_measured`, `Forest_solo_from_level_5_to_8_is_measured`, `Ruins_solo_from_level_9_to_12_is_measured`, `Crypt_in_a_party_of_four_from_level_12_to_14_is_measured_against_the_ruins_solo`.

Metas pendentes (`ZoneGoalDesignTests.cs`, ligam quando a zona existe): Brejo 16→20, Serra 21→25, Minas ≥ 1,3×, chefe do Brejo (só a medida).

Reproduzir a medida das zonas de hoje:

```bash
dotnet test tests/Game.Design.Tests --filter "FullyQualifiedName~ZonePaceDesignTests" --logger "console;verbosity=detailed"
```

(100 seeds por lote; ~2 min com os lotes das outras zonas já no cache do processo, ~10 min isolado numa máquina com poucos núcleos por causa da Cripta.)
