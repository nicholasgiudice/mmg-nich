# T-402 — Simulador até o nível 40 e metas dos testes de design

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-401
- **Libera:** T-408
- **Áreas que pode alterar:** `tests/Game.Sim/`, `tests/Game.Sim.Tests/`, `tests/Game.Design.Tests/`, `data/rules.json` (só os tetos de AGI, se a análise pedir), `design/progression-v0.1.md` (§8 metas), `design/segunda-classe-v0.1.md` (só para registrar números medidos), `tasks/reports/`
- **Branch:** sai de `trilha/segundas-classes` e é integrada nela, não na `main` (seção 6f de `tasks/README.md`).

## Contexto

O simulador (`tests/Game.Sim`) monta personagens com `CharacterProfile.Expected(level)` e `Weak(level)`: pontos divididos entre os atributos sugeridos da classe (`CharacterProfiles.Suggested`) e equipamento do nível. Os testes de design (`tests/Game.Design.Tests`) usam esses perfis contra os encontros de cada área.

Dois problemas para a fase 4:

1. Os perfis e os testes só foram pensados até o nível 15. Com o nível 40 (T-401), é preciso saber se as fórmulas continuam de pé: escala dos monstros por nível (`rules.monsters`), defesa (`defensePerAttackerLevel`, teto de 60%), tetos de AGI (velocidade de ataque no máximo com AGI 100, crítico com AGI 140) e itens por nível.
2. **19 casos de teste de design estão desligados** com o motivo `PlaytestRebalance.Pending` (`tests/Game.Design.Tests/PlaytestRebalance.cs`), desde o balanceamento do playtest de 2026-11-16 (`tasks/reports/playtest-v0.3.md`). O jogo ficou mais fácil que as metas.

## Requisitos

1. **Perfis até o 40:** `CharacterProfile` aceita qualquer nível até `maxLevel`, com os pontos de atributo do nível (incluindo os +5 da segunda classe a partir do 20, pela regra da T-401) e equipamento do nível. Sem segunda classe ainda (ela chega na T-403); deixar o perfil pronto para receber a segunda classe depois.
2. **Relatório `tasks/reports/balance-v0.18.md`** (ou o próximo número livre), com, do nível 1 ao 40, de 5 em 5:
   - vida, Mana e dano das habilidades principais de cada classe no perfil esperado;
   - redução de defesa de um personagem contra um monstro do mesmo nível;
   - quando o perfil esperado bate nos tetos de AGI e de defesa;
   - tempo para matar um monstro normal do próprio nível (usando monstros existentes com o nível ajustado) e XP por minuto que isso dá.
3. **Tetos de AGI:** se o relatório mostrar que o perfil esperado bate no teto antes do nível 40, propor o ajuste (teto maior ou valor por ponto menor) e aplicar em `data/rules.json`, com o antes e depois no relatório.
4. **Metas revistas:** rever as metas da §8 do design de progressão à luz do balanceamento de 2026-11-16 e reativar os 19 casos de `PlaytestRebalance.Pending`. Cada caso termina passando com uma meta nova justificada no relatório, ou removido com o motivo. `PlaytestRebalance.cs` é apagado se não sobrar nenhum uso.
5. **Metas da segunda classe** (para a T-408 usar): escrever na §8 do design de progressão as metas que um personagem de segunda classe deve cumprir, por exemplo:
   - no mesmo nível e equipamento, Berserker mata um grupo típico mais rápido que o Guerreiro só com a base;
   - Guardião reduz o dano recebido pelo grupo num encontro de grupo;
   - nenhuma segunda classe mais de 1,6× mais lenta que a média das outras no solo.

## Critérios de aceite

- `./scripts/check.sh` completo passa, sem nenhum `PlaytestRebalance.Pending` restante;
- relatório com as tabelas pedidas e as decisões tomadas;
- perfis de nível 21 a 40 funcionam no simulador (teste).

## Resultado

Concluída em 2026-11-21. Relatório: `tasks/reports/balance-v0.18.md`.

**Simulador até o 40**
- `CharacterProfile` já aceitava qualquer nível até `maxLevel` (o `BuildFor` limita pelo `rules.MaxLevel`, que a T-401 subiu para 40). **`tests/Game.Sim/CharacterProfiles.cs` não mudou**, para não conflitar com a T-403.
- **Desvio: os +5 de atributo da segunda classe não entram no perfil.** O `Leveling.Validate` do Core recusa mais pontos que `AttributePointsAt(nível)` enquanto não há segunda classe escolhida. O perfil passa a contá-los quando a T-403 puser nele o campo da segunda classe: basta trocar `rules.AttributePointsAt(level)` por `rules.AttributePointsAt(level, secondClassChosen)` em `BuildFor`. A diferença nos números é de uns 2%.
- Sem segunda classe, a base tem 23 NH: do 24 em diante sobram pontos de habilidade (17 no 40). O teste de perfis vai agora até o 40 e conta isso.
- Novo: `tests/Game.Sim/LevelCurve.cs` e a opção `--level-curve` (`Program.cs`, `README.md`). Para cada classe e nível, ela mostra números do perfil, redução de defesa contra o mesmo nível, tetos e tempo para matar um Lobo ou um Guarda do próprio nível, sem a assinatura.
- Testes novos em `tests/Game.Sim.Tests/CharacterProfileTests.cs`: perfis do 21 ao 40 em todas as classes (números crescem, duelo contra um Lobo do nível vencido no 21, 30 e 40) e o Arqueiro esperado abaixo do teto de velocidade de ataque no 40.

**Tetos de AGI:** o Arqueiro esperado batia no teto de velocidade de ataque no nível 39. Em `data/rules.json`, `stats.attackSpeedMax` passou de **0,5 para 0,6** (teto com AGI 120). Nada muda abaixo de AGI 100. O crítico (AGI 140) e a defesa (60%) não são alcançados pelo perfil esperado e ficaram como estavam.
- Fora das áreas listadas, o mínimo: o texto de §3 e §4 de `design/progression-v0.1.md` que citava "+50% (AGI 100)".

**Metas revistas:** `PlaytestRebalance.cs` apagado e nenhum `PlaytestRebalance.Pending` restante. Dados de habilidades e monstros não mudaram. Dos 34 casos reativados, 18 passavam com a meta antiga e ficam com ela. Os 16 que falhavam, para o designer revisar (antes → depois):

| Teste | Antes | Depois |
|---|---|---|
| Perfil fraco solo (`ProgressionDesignTests`, `ForestDesignTests`; 6 casos falhavam) | vitória ≤ 50% (Clérigo: ou ≥ 1,3× o tempo) | vitória ≤ 50% ou ≥ **1,25×** o tempo do esperado, todas as classes (o Clérigo baixou de 1,3× para 1,25×). Métodos renomeados `..._or_takes_1_25_times_as_long` |
| Matriarca nv8 com incomuns (`ProgressionDesignTests`) | 60–90%, 2–4 min | **60–97%**, 2–4 min |
| Matriarca, grupo do protótipo (`DesignTests`) | 60–90% | **60–97%** |
| 1,6× no nível 9 (`ProgressionDesignTests`) | Clérigo até 1,7× | Clérigo até **1,8×** (os outros 1,6×) |
| Poção de Mana, Mago fraco nv9 (`EconomyDesignTests`) | vitória ≤ 50% | ≤ 50% ou a poção acrescenta **no máximo 15 pontos** à vitória sem poção |
| Explosão sozinha × rotação (`DesignTests`) | ≤ 50% ou mais lenta que a rotação | ≤ 50% ou **no máximo 10% mais rápida** |
| Três encontros entre 5% e 95% (`DesignTests`) | ≥ 3 encontros | **removido**: os encontros do protótipo ficaram em 96–100%; coberto pelo Pátio solo, a Matriarca sem Clérigo e o perfil fraco |
| Vigia com raros (`WatcherDesignTests`) | média 50–80%, cada nível 40–90%, 4–6 min | média **80–99%**, cada nível **≥ 70%**, 4–6 min |
| Vigia na sala real (`CryptDesignTests`) | igual ao Vigia | igual ao Vigia novo |
| Cavaleiro Espectral, grupo de 3 (`RuinsDesignTests`) | média 70–90% | média **85–99%** |
| Ida completa à Cripta (`CryptDesignTests`) | 25–40 min | **20–40 min** |

Medidas e justificativa de cada caso: relatório §4. A §8 de `design/progression-v0.1.md` foi reescrita com as metas novas, a meta da curva do 15 ao 40 e as **metas da segunda classe** para a T-408 (Berserker ≤ 85% do tempo do Guerreiro só com a base e ≤ 1,5× o dano; Guardião −15% de dano nos outros membros do grupo; nenhuma segunda classe > 1,6× mais lenta que a média; NH1 útil no 21; base × segunda classe ≥ 70%). `design/segunda-classe-v0.1.md` §3 registra os números medidos.

**Pendências e alertas**
- **Clérigo contra o Lobo no 40:** fica a 1,92× da média (a meta da curva é 1,8×). Os de longe aceleram com o nível. Rever com as segundas classes do Clérigo.
- **Mana do Guerreiro:** fica em 88 até o 40 (não põe INT). A Tempestade de Aço do Berserker custa 90 no design: a T-408 precisa conferir.
- **Margens apertadas** em algumas metas novas (Vigia 97,5% de média contra o teto de 99%; perfil fraco 1,28× contra 1,25×). São deliberadas: um novo ganho de força volta a falhar.
