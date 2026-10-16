# T-012 — Telegráfos, áreas persistentes, invocação e fases de boss

- **Ambiente:** VPS
- **Modelo:** Forte
- **Depende de:** T-009, T-011
- **Libera:** T-014, T-023
- **Áreas que pode alterar:** `src/Game.Core/Telegraphs/`, `src/Game.Core/Areas/`, `src/Game.Core/AI/` (fases), `MonsterDefinition` e carregador (fases), registro de sistemas, `tests/`

## Contexto

Leia o Encontro 5 (Matriarca da Floresta) em `design/combat-encounters-v0.1.md`. O jogador precisa ver o perigo antes de ser atingido ("o jogador sabe por que tomou dano").

## Requisitos

1. **Telegráfos**: entidade sem vida com forma (`circle`, `cone`, `line`), posição/orientação, tempo de preparação em ticks; ao resolver, aplica efeitos a quem estiver dentro naquele tick. Habilidades podem declarar telegráfo (estender `AbilityDefinition`). Eventos `TelegraphStarted` (com forma e tick de resolução) e `TelegraphResolved`.
2. **Áreas persistentes** (`spawn_area`): poça de veneno, teia (lentidão), armadilha (`on_enter_once`, prende e some). Duração, intervalo, facção afetada. Eventos `AreaSpawned`, `AreaTriggered`, `AreaExpired`.
3. **Investida**: deslocamento até o alvo ou ao longo de uma linha telegrafada, aplicando efeito em quem for atingido.
4. **Invocação** (`summon`): cria monstros em volta da fonte, com limite de invocações vivas por fonte.
5. **Fases de boss** em `MonsterDefinition`: lista de fases com limiar de vida (%), ações ao entrar (invocar, aplicar status em si, criar áreas), conjunto de habilidades disponíveis e flags (ex.: `noSummons`). Evento `PhaseChanged`.
6. Fixture de teste com uma Matriarca simplificada de 3 fases.

## Critérios de aceite

- Teste: telegráfo acerta quem está dentro no tick de resolução e não quem saiu antes;
- teste: armadilha dispara uma vez e prende;
- teste: poça aplica dano periódico só enquanto a entidade está dentro;
- teste: boss troca de fase no limiar, uma única vez, e a fase 3 não invoca;
- `./scripts/check.sh` passa.

## Resultado

**Feito:** telegráfos, áreas persistentes, investida, invocação e fases de boss em `src/Game.Core/Combat/{Telegraphs,Areas,Phases}/` (mesma convenção das T-009/T-011: pastas dentro de `Combat/`), módulo `Combat/EncounterModule.cs` (ocupa o slot T-012 de `CombatModules`) e perfil de IA `ai.boss`. 34 testes de Core (`TelegraphPhaseTests.cs`) + 4 de Data (`TelegraphPhaseLoaderTests.cs`); fixture da Matriarca em 3 fases em `tests/Game.Core.Tests/Fixtures/matriarch.json`. `./scripts/check.sh` passa (179 Core + 78 Data).

**Ordem dos sistemas (importante).** `Telegraphs = 2500`, `Areas = 2600`, `Phases = 2700` (todos entre `Abilities` 2000 e `Status` 3000). Status, ponte de status, Recursos e IA só leem os eventos do tick corrente; resolvendo em 6000 o dano de telegráfo/área não geraria recurso nem ameaça. Os testes provam: dano de telegráfo e de poça gera `ResourceChanged` (regra `damage_taken`) e ameaça/`AggroGained` no mesmo tick. O comentário de `SystemOrder.Telegraphs` foi atualizado e a constante antiga de 6000 removida.

**Formato de dados (JSON, tempos em segundos)**
- Habilidade com telegráfo (`telegraph` em `abilities`): os `effects` da habilidade NÃO são aplicados ao concluir a conjuração; nasce a entidade telegráfo e os efeitos são aplicados a quem estiver na forma no tick de resolução (efeitos PerCast, como `spawn_area`/`summon`, rodam 1x).
  `"telegraph": { "shape": "line", "length": 6, "width": 1.5, "delay": 1, "anchor": "source", "affects": "enemy", "charge": true }`
  `shape`: `circle` (`radius`), `cone` (`length` + `angle` em graus, abertura total), `line` (`length` + `width`). `anchor`: `source` (posição da fonte, direção da mira; padrão) ou `aim` (ponto mirado / posição do alvo). `affects`: facção relativa à fonte (padrão `enemy`). `charge` (só `line`): depois dos efeitos a fonte faz dash de `length` ao longo da linha (para na parede; evento `Displaced` Dash).
- Área (`areas`, já existia): `mode` `periodic` (`interval`) ou `on_enter_once` (armadilha). Criada pelo efeito `{ "type": "spawn_area", "area": "area.venom_pool" }` no ponto mirado (ou no centro do telegráfo `aim`; sem mira, posição do 1o alvo; senão a fonte).
- Invocação: `{ "type": "summon", "monster": "monster.spider_small", "count": 2, "radius": 2, "maxAlive": 4 }` (`maxAlive` novo, 0 = sem limite).
- Fases (`monsters[].phases`):
  `[ { "id": "hunt", "healthPercent": 100, "abilities": ["matriarch.charge","matriarch.bite"] }, { "id": "suffocating_web", "healthPercent": 66, "onEnter": [ { "type": "summon", "monster": "monster.spider_big", "count": 1, "maxAlive": 1 } ] }, { "id": "frenzy", "healthPercent": 33, "noSummons": true, "onEnter": [ { "type": "apply_status", "status": "status.frenzy" } ] } ]`
  `healthPercent`: entra quando vida% <= limiar; a 1a fase deve ser 100 e os limiares decrescem. `abilities` ausente = todas. `noSummons`. `onEnter` = efeitos com o boss como fonte e alvo.

**Como funciona**
- Telegráfo: entidade com `Transform` (origem/direção) + `TelegraphState` (forma, tick de resolução; sem `FactionTag`/`Health`, então consultas espaciais e alvos o ignoram). Eventos `TelegraphStarted(Telegraph, Source, AbilityId, Shape, Origin, Direction, Radius, Length, Width, AngleDegrees, ResolveAtTick)` e `TelegraphResolved(Telegraph, Source, AbilityId, Targets, Cancelled)`. Se a fonte morre antes, `Cancelled = true` e nada é aplicado. Alvos: vivos com facção e vida, corpo tocando a forma e linha de visão da origem. A forma e a origem ficam fixas desde o início.
- Áreas: entidade com `Transform` + `AreaState`. Periódica aplica a cada intervalo (1a aplicação em criação + intervalo, inclui o tick final da duração) só a quem está dentro naquele tick; `AreaTriggered` só sai quando há alvos. `on_enter_once` dispara no 1o tick com algum afetado dentro, aplica a todos os que estão dentro e some (`AreaExpired(Triggered)`). Teia = área periódica com `apply_status` de duração curta (lentidão some logo após sair). Eventos: `AreaSpawned`, `AreaTriggered`, `AreaExpired(Reason Duration|Triggered)`.
- Invocação: em círculo em volta da fonte (ângulos igualmente espaçados a partir do facing, pulando paredes), `SummonedBy` marca o dono; limite `maxAlive` conta vivos do MESMO monstro por fonte (só nasce o que cabe); invocados entram no `GroupId` da fonte e herdam o alvo dela como ameaça de grupo. Evento extra `MonsterSummoned`.
- Fases: `PhaseSystem` mantém `PhaseState` (índice só cresce). Cruzar vários limiares de uma vez passa por todas as fases em ordem. `PhaseChanged(Entity, MonsterId, Phase, Previous, Index, HealthPercent)`. O conjunto de habilidades e `noSummons` são aplicados por um `IActionGate` (`PhaseGate`, rejeita com `CannotAct`); o efeito `summon` também checa `noSummons`.
- IA: perfil `ai.boss` (herda o perseguidor): antes de atacar tenta as habilidades de tag `ai.summon` e `ai.area` (a primeira que passa na validação, incluindo o gate de fase); o resto (investida, mordida) segue a prioridade da lista `abilities`. A IA só emite `UseAbilityIntent`; invocar/criar áreas são habilidades de dados.
- Investida: coberta por `charge` (linha telegrafada). "Deslocamento até o alvo" sem telegráfo já existe como `displacement` `dash` `toward_target` (T-008).
- Log (T-013): `EncounterModule.RegisterLog` registra os 6 eventos pedidos mais `MonsterSummoned`, encadeado em `CombatLogFormatters.CreateDefault()`.

**Arquivos compartilhados editados (mínimo):** `Combat/CombatServices.cs` (1 linha no slot T-012), `Simulation/SystemOrder.cs` (3 constantes, removida a de 6000), `Simulation/SystemsRegistry.cs` (3 linhas), `Combat/AbilitySystem.cs` (em `Execute`: se a habilidade tem telegráfo, inicia o telegráfo em vez de aplicar os efeitos), `Combat/Ai/AiProfileRegistry.cs` (1 linha, `ai.boss`), `CombatLog/CombatLogFormatters.cs` (1 chamada em `CreateDefault`), `Definitions/{Definitions,Effects,Enums}.cs` (`TelegraphDefinition`, `PhaseDefinition`, `AbilityDefinition.Telegraph`, `MonsterDefinition.Phases`, `SummonEffect.MaxAlive`, enums), `Game.Data/GameDataValidator.cs` (validação de telegráfo/fases/maxAlive) e `data/schemas/game-data.schema.json`. O loader (`GameDataLoader`) e `GameJson` não precisaram de mudança (desserialização direta dos records).

**Decisões / limitações**
- Telegráfo é criado ao CONCLUIR a conjuração (cast time + delay somam); para boss, o normal é `castTime` 0 e o tempo de aviso em `telegraph.delay`. Stun/interrupt na fonte não cancela o telegráfo já posto (só a morte da fonte cancela).
- Telegráfos/áreas não são atingíveis nem alvo de nada (sem `FactionTag`/`Health`); entidades de telegráfo/área são removidas com `world.Remove` (publica `EntityRemoved`).
- Armadilha não tem tempo de armar (`interval` é ignorado em `on_enter_once`); a investida atravessa os alvos (não para no primeiro atingido).
- Fases e `onEnter` de poças/áreas em onEnter nascem na posição do boss. A 1a fase (limiar 100) é "entrada" no primeiro tick (evento `PhaseChanged` com `Previous = -1`).
- Sem conteúdo real (Matriarca de verdade fica para a T-014); só a fixture de teste.
- Pendências: nenhuma.
