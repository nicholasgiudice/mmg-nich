# T-008 — Pipeline de habilidades e efeitos

- **Ambiente:** VPS
- **Modelo:** **Forte** (base estendida por T-009 a T-013)
- **Depende de:** T-005, T-006, T-007
- **Libera:** T-009, T-010, T-011, T-013, T-021
- **Áreas que pode alterar:** `src/Game.Core/Combat/`, `src/Game.Core/Simulation/` (registro de sistemas, spawn), `tests/Game.Core.Tests/`

## Contexto

Leia `tech/stack-proposal-v0.1.md` (fluxo Intenção → Validação → Execução → Eventos) e `design/classes-v0.1.md`. Esta é a peça mais importante do protótipo.

## Objetivo

Entidades criadas a partir das definições usam habilidades por intenção, com validação, conjuração, resolução de alvos, efeitos e eventos.

## Requisitos

1. **Spawn por definição**: criar jogador a partir de `ClassDefinition` e monstro a partir de `MonsterDefinition` (vida, recurso, velocidade, raio, habilidades, tags, rank, resistências). Criar mundo a partir de `EncounterDefinition` + `ArenaDefinition`.
2. **Estado de habilidade** por entidade: recarga em ticks. Sem recarga global no protótipo; o ataque básico tem sua própria recarga (velocidade de ataque).
3. **Validação** de `UseAbilityIntent`, nesta ordem: vivo; pode agir (consultar um ponto de extensão `IActionGate`, que a T-009 implementa com stun/silence); habilidade conhecida; recarga pronta; recurso suficiente; alvo válido (facção, vivo); alcance; linha de visão. Falha publica `AbilityRejected` com motivo enumerado (para a UI mostrar "Em recarga", "Sem Mana", "Fora de alcance"…).
4. **Conjuração**: `castTicks > 0` → estado conjurando; mover cancela se `castWhileMoving` for falso; `interrupt` cancela. Custo e recarga aplicados ao **concluir** (registrar se preferir no início e por quê).
5. **Resolução de alvos** para todos os tipos de `TargetingDefinition`, usando as consultas da T-007.
6. **Efeitos** via registro extensível por `type` (outras tarefas adicionam tipos sem editar o switch central). Implementar aqui:
   - `damage`: `base × multiplicadores de causado × multiplicadores de recebido × (1 − resistência da escola)`; escudo absorve antes da vida; arredondamento definido e testado;
   - `heal`: limitado à vida máxima, registra cura efetiva e excedente;
   - `shield`: absorção com duração;
   - `displacement`: dash/knockback usando a T-007;
   - `interrupt`: cancela conjuração do alvo.

   Tipos `apply_status`, `remove_status`, `resource`, `threat`, `spawn_area`, `summon` ficam para T-009, T-010, T-011 e T-012; se usados antes de registrados, erro claro.
7. **Multiplicadores**: ponto de extensão `IModifierSource` (dano causado/recebido, velocidade) que a T-009 alimenta com status.
8. **Ataques à distância são instantâneos** (hitscan com linha de visão). Projéteis com tempo de voo ficam fora do protótipo.
9. **Morte**: vida 0 → `EntityDied`, sai de alvos válidos, cancela conjuração.
10. **Eventos**: `AbilityStarted`, `AbilityExecuted`, `AbilityRejected`, `CastInterrupted`, `DamageDealt` (fonte, alvo, habilidade, escola, valor, absorvido, excedente), `HealApplied`, `ShieldApplied`, `ShieldExpired`. Eventos precisam carregar dados suficientes para o log de combate (T-013) e para geração de recurso (T-010).

## Fora do escopo

Status, recursos por evento, IA, telegráfos, conteúdo real.

## Critérios de aceite

- Testes para cada motivo de rejeição;
- testes de cada tipo de alvo e de cada efeito implementado;
- conjuração cancelada por movimento e por interrupção;
- escudo absorvendo parcial e totalmente;
- um teste de ponta a ponta: encontro de fixture com 1 jogador × 1 monstro parado, jogador usa ataque básico até matar, eventos corretos;
- `./scripts/check.sh` passa.

## Resultado

**Feito:** pipeline Intenção -> Validação -> Conjuração -> Resolução de alvos -> Efeitos -> Eventos, em `src/Game.Core/Combat/`. 35 testes novos (`CombatTests.cs`, `EncounterTests.cs`); `./scripts/check.sh` passa (77 testes Core + 62 Data).

**Arquivos:** `ContentCatalog` (definições entregues ao Core), `Components.cs` (`UnitInfo`, `AbilityState`, `ShieldState`), `CombatEvents.cs`, `CombatServices.cs` (`IActionGate`, `IModifierSource`, `CombatModules`), `Effects/EffectRegistry.cs` + `Effects/CoreEffects.cs` (damage, heal, shield, interrupt, displacement; `DamageResolver`, `CastControl`), `TargetResolver.cs`, `AbilitySystem.cs` (`AbilityValidator` + sistema), `EntityFactory.cs` (`SpawnPlayer`, `SpawnMonster`, `EncounterSetup.Create`).

**Fora de `Combat/` (mínimo):** `World` (props `Content`, `Combat`), `WorldBuilder` (`WithContent`, `Combat`, `CreateDefault(seed, map, content)` chama `CombatModules.Register`), `SystemsRegistry` (1 linha: `AbilitySystem` em `SystemOrder.Abilities`), `MovementSystem` (velocidade x `world.Combat.SpeedMultiplier`), comentário em `Intents.cs`, e `tests/Game.Core.Tests/Game.Core.Tests.csproj` (referência a Game.Data **só nos testes** + fixture `Fixtures/combat.json`).

**Decisões**
- Core recebe `ContentCatalog` (dicionários por id). Quem carrega converte `GameDatabase` -> `ContentCatalog` (ver `EncounterTests.LoadFixture`).
- Ordem de validação: vivo, conjurando (`AlreadyCasting`), gates, conhecida (`UnknownAbility`), recarga, recurso, alvo (`InvalidTarget`), alcance (`OutOfRange`), LoS (`NoLineOfSight`). Gates retornam `CannotAct`/`Stunned`/`Silenced`.
- **Custo e recarga são aplicados ao concluir** a conjuração (cancelar/interromper não custa nada, melhor para o jogador). Ao concluir, tudo é revalidado (gate, recurso, alvo, alcance, LoS); se falhar publica `AbilityRejected`. Recarga = `Tick + CooldownTicks` (pronta quando `Tick >= ReadyAtTick`). Instantâneas: `AbilityStarted(CastTicks=0)` e `AbilityExecuted` no mesmo tick.
- Cast sem `castWhileMoving` é cancelado se `Movement.Direction != 0` (checado no tick seguinte ao início; segurar a tecla de andar cancela). Cast com `castWhileMoving` liga `Movement.LockFacing` até terminar. Facing é ajustado para a mira ao iniciar e ao executar.
- Mira: `AimPoint` > posição do `Target` > facing. Tipos de área (arc/line/circle) nunca falham por acertar ninguém (gasta custo/recarga). `unit` exige alvo; alcance = distância entre centros - raio do alvo. `circle_at_point` usa `AimPoint` ou posição do `Target`.
- Facção em áreas: habilidade só com efeitos benéficos (heal/shield/remove_status) afeta aliados (inclui o conjurador); as demais, inimigos. Hitscan: parede entre origem e alvo exclui o alvo.
- Dano: `round(base x causado x recebido x max(0, 1 - resistência))`, arredondamento para inteiro com metades para longe de zero, mínimo 0. Escudos absorvem na ordem de criação. `DamageDealt`: `Amount` (final), `Absorbed`, `Applied` (vida removida), `Overkill`, `Killed`. Morte é imediata no resolvedor (`CastInterrupted(Died)` + `EntityDied`), sem esperar o `DeathSystem`.
- Cura sem multiplicadores (adicionar método default em `IModifierSource` se T-009 precisar). Escudo: `Id`, duração em ticks absolutos, `ShieldExpired` com `Reason` Expired/Depleted.
- `displacement` é `PerCast`: dash move o conjurador (uma vez); knockback move cada alvo vivo. Direções: Aim, Backward, AwayFromSource, TowardTarget (em knockback = puxar até a fonte). Usa `ForcedMovement.Displace`.
- Interrupt: cancela a conjuração do alvo sem custo nem recarga. Efeitos por alvo ignoram alvos mortos.
- Arena: só `#` é parede (outros caracteres viram chão); `Scale` é ignorado no protótipo. Posições de spawn usadas como estão (tiles). Jogadores spawnam antes dos inimigos (ids crescentes).
- Efeito sem handler lança `InvalidOperationException("No handler registered for effect type 'XxxEffect' ...")` ao executar.
- Projéteis/hitscan: tudo instantâneo, sem projétil.

**Como estender (T-009, T-010, T-011, T-012, T-013)**

Regra geral: crie seu módulo na SUA pasta (ex.: `src/Game.Core/Combat/Status/`, `.../Resources/`, `.../Ai/`) e ocupe só o SEU slot em `CombatModules.Register` (`Combat/CombatServices.cs`; há um slot comentado por tarefa, separados por comentários para não conflitar no merge) e, se tiver sistema, UMA linha em `SystemsRegistry` com a constante de `SystemOrder`. Eventos novos vão em arquivo seu (`public sealed record Foo(...) : GameEvent;`).

1. **Registrar um efeito** (`apply_status`, `remove_status`, `resource`, `threat`, `spawn_area`, `summon`): no seu módulo `static void Register(CombatServices s)`, `s.Effects.Register<ApplyStatusEffect>((ctx, e) => ...)`; segundo argumento opcional `EffectScope.PerCast` (roda 1x por uso, `ctx.Target == null`; use para `summon`/`spawn_area`) ou o padrão `PerTarget` (1x por alvo vivo). Chame `YourModule.Register(s)` no seu slot. Registrar duas vezes o mesmo tipo lança erro. `EffectContext`: `World`, `Source` (Entity?, pode estar morta/nula), `Target`, `Targets`, `SourceId` (id da habilidade/status/área, vai nos eventos), `Ability` (null em status/área), `AimPoint`, `AimDirection`, `SourceEntityId`.
2. **Executar efeitos fora de habilidades** (tick periódico de status, área): `EffectApplier.Apply(world, effects, source, targets, sourceId, ability: null, aimPoint, aimDirection)`. Dano direto: `DamageResolver.Deal(world, source, target, sourceId, base, school)` (aplica multiplicadores, resistência, escudo, morte e eventos).
3. **Gate (stun/silence, T-009):** implemente `IActionGate.CanUse(world, actor, abilityId, ability, isBasicAttack)` retornando `AbilityRejectReason.Stunned/Silenced/CannotAct` ou null; registre com `s.Gates.Add(new MyGate())`. Consultado na validação e ao concluir a conjuração. Para cancelar uma conjuração em andamento ao aplicar stun: `CastControl.Cancel(world, entity, CastInterruptReason.Disabled, by)`.
4. **Modificadores (T-009):** implemente `IModifierSource` (todos os métodos têm padrão 1.0; sobrescreva só os que usa): `DamageDealtMultiplier(world, source, target, school)`, `DamageTakenMultiplier(world, target, source?, school)`, `SpeedMultiplier(world, entity)` (0 = enraizado/atordoado; o `MovementSystem` já consome). Registre com `s.Modifiers.Add(...)`; várias fontes se multiplicam.
5. **Eventos existentes** (`Game.Core.Combat`, todos `: GameEvent` com `Tick`): `AbilityStarted(Source, AbilityId, CastTicks, EndTick, Target?, AimPoint?)`; `AbilityExecuted(Source, AbilityId, Targets, CostResource?, CostAmount, IsBasicAttack, CooldownReadyTick)`; `AbilityRejected(Source, AbilityId, Reason)`; `CastInterrupted(Entity, AbilityId, Reason, By?)`; `DamageDealt(Source, Target, SourceId, School, Amount, Absorbed, Applied, Overkill, Killed)` (Source = `EntityId.None` se sem fonte); `HealApplied(Source, Target, SourceId, Amount, Effective, Overheal)`; `ShieldApplied(Source, Target, SourceId, ShieldId, Amount, ExpiresAtTick)`; `ShieldExpired(Entity, ShieldId, Remaining, Reason)`; `Displaced(Source, Entity, SourceId, Kind, From, To, Distance)`; mais `EntitySpawned/EntityDied/EntityRemoved`. Sistemas de ordem MAIOR que `SystemOrder.Abilities` (2000) leem os eventos do tick com `world.Events.Of<T>()` (T-010 gera recurso por `DamageDealt`/`AbilityExecuted`; T-011 ameaça por `DamageDealt`/`HealApplied`; T-013 lê tudo em `SystemOrder.CombatLog`). Status que causa dano/cura deve passar por `EffectApplier`/`DamageResolver` para gerar os mesmos eventos.
6. **Componentes:** `UnitInfo` (DefinitionId, Name, Tags, Rank, Resistances), `AbilityState` (Known, BasicAttackId, ReadyAtTick, Cast), `ShieldState`. Para recurso (T-010): `ResourcePool` já existe; custo é descontado direto em `AbilitySystem.Execute` (se T-010 precisar de hook, troque aquele trecho por chamada ao seu serviço). Para IA (T-011): leia `world.Content.Monsters[unitInfo.DefinitionId].Ai` e emita `UseAbilityIntent`/`MoveIntent`. Para status/áreas/invocação (T-009/T-012): definições em `world.Content.Statuses/Areas/Monsters`; para invocar use `EntityFactory.SpawnMonster(world, def, pos)`.
7. **Testes:** helpers em `CombatTests.cs` (`Ab`, `Fighter`, `NewWorld`) mostram como montar um mundo com `ContentCatalog` feito à mão; `EncounterTests` mostra o caminho via `GameDatabase` -> `ContentCatalog` -> `EncounterSetup.Create`.

**Pendências:** nenhuma. Não implementado (fora do escopo): status, recursos por evento, IA, telegráficos, escala de arena.

