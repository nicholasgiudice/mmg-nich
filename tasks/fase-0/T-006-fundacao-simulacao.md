# T-006 — Fundação da simulação

- **Ambiente:** VPS
- **Modelo:** **Forte** (base que todas as outras tarefas do Core vão estender)
- **Depende de:** T-003
- **Libera:** T-007, T-008
- **Áreas que pode alterar:** `src/Game.Core/Simulation/`, `src/Game.Core/Events/`, `src/Game.Core/Intents/`, `tests/Game.Core.Tests/`

## Contexto

Leia `tech/stack-proposal-v0.1.md` (seção "Game.Core — a simulação") e `AGENTS.md` (regras de arquitetura). O mesmo Core vai rodar dentro do Godot (protótipo) e no servidor (fase 1). As decisões aqui determinam como as tarefas 007–013 vão estender o sistema.

## Objetivo

Um mundo simulado em ticks fixos, determinístico, com entidades, intenções, sistemas ordenados e eventos.

## Requisitos

1. **`World`**: contador de tick, `TickRate = 20`, `Step()` avança exatamente um tick.
2. **Entidades**: `EntityId` (struct), criação/remoção, iteração em **ordem determinística** (por id). Componentes iniciais:
   - posição (`System.Numerics.Vector2`) e direção (facing);
   - vida (atual, máxima, vivo/morto);
   - facção (`player`, `monster`);
   - recurso (tipo, atual, máximo);
   - velocidade de movimento, raio de colisão.

   O formato (classe com componentes opcionais, stores por tipo, ECS leve) é decisão sua; priorize clareza e facilidade de estender. Justifique no Resultado.
3. **Intenções**: fila aplicada no início de cada tick. Tipos: `MoveIntent(entity, direção)`, `StopIntent`, `UseAbilityIntent(entity, abilityId, targetEntity?, aimPoint?)` (só o tipo; tratamento na T-008). Intenção de entidade morta ou inexistente é descartada.
4. **Sistemas**: interface simples com ordem explícita de execução, registrados em **um único lugar** (ex.: `WorldBuilder`/`SystemsRegistry`), para que tarefas paralelas adicionem sistemas com conflitos mínimos.
5. **Eventos**: cada tick produz uma lista de eventos (`GameEvent` com tick). Sistemas podem publicar e reagir a eventos do mesmo tick numa ordem definida. Consumidores externos (cliente, log, rede) leem os eventos do último tick. Eventos iniciais: `EntitySpawned`, `EntityDied`, `EntityRemoved`.
6. **RNG determinístico** (ex.: PCG ou xorshift) com seed no construtor do mundo.
7. **Snapshot/consulta**: API de leitura para cliente e testes listarem entidades e seus estados sem acesso mutável.
8. **Hash de estado** (para testes de determinismo).
9. Movimento simples sem colisão (posição += direção × velocidade × dt) — a T-007 substitui pelo sistema com colisão.

## Fora do escopo

Colisão, habilidades, dano, status, IA, carregamento de JSON.

## Critérios de aceite

- Teste de determinismo: dois mundos com mesma seed e mesmas intenções têm o mesmo hash após 1000 ticks;
- teste de que seeds diferentes produzem sequências RNG diferentes;
- testes de intenção descartada para entidade morta;
- teste de ordem de sistemas e de eventos publicados/lidos no mesmo tick;
- nenhuma referência a `DateTime`, `Stopwatch`, `Random` não seedado no Core;
- `./scripts/check.sh` passa.

## Resultado

**Feito:** `World` com tick fixo 20 Hz, entidades, intenções, sistemas ordenados, eventos, RNG PCG32, snapshot e hash de estado. 17 testes em `tests/Game.Core.Tests/SimulationTests.cs`; `./scripts/check.sh` passa. Nenhum uso de `DateTime`/`Stopwatch`/`Random` não seedado no Core.

**Modelo de entidades: ECS leve "entidade + componentes por tipo".** `Entity` (classe) tem `EntityId` e um dicionário `Tipo -> IComponent` (no máximo um por tipo). Componentes são classes mutáveis só com estado; sistemas têm a lógica. Escolhido porque é o mais simples que atende "adicionar componente sem mexer em código alheio" (T-009 adiciona `StatusEffects`, T-011 `AiState`, etc. sem tocar em `Entity`/`World`) e segue a proposta (sem framework ECS). Custo: lookup por dicionário (irrelevante nesta escala). Entidades ficam em `SortedDictionary` por id; `world.Entities` é uma lista congelada em ordem de id (seguro dar Spawn/Remove durante o laço; use `entity.IsRemoved`/`IsAlive` para checar).

**Como estender (resumo para outros agentes)**
- **Novo componente:** classe `: IComponent` (em `Game.Core/Simulation/Components/` ou na pasta da sua tarefa). Implemente `Clone()` (cópia profunda) e `WriteHash(StateHasher)` (TODO estado que afeta a simulação, ordem fixa). Uso: `world.Spawn(new Transform(), new Health(100), ...)`, `entity.Add(x)`, `entity.Get<T>()`, `entity.TryGet<T>()` (null se ausente), `entity.Has<T>()`. Componentes iniciais: `Transform` (Position, Facing), `Health` (Current, Max, IsAlive), `FactionTag`, `ResourcePool` (Type string, Current, Max), `Movement` (Speed, Direction), `Collider` (Radius).
- **Novo sistema:** classe `: ISimulationSystem` (`Update(World)`); constante em `SystemOrder` (já há slots reservados para T-007..T-013) e UMA linha em `SystemsRegistry.Register` (`b.AddSystem(SystemOrder.X, new MySystem())`). Ordem repetida lança exceção. A T-007 troca a implementação na linha de `SystemOrder.Movement`. Sistemas não devem guardar estado de jogo fora de componentes (senão fica fora do hash/snapshot). Testes de um sistema isolado: `new WorldBuilder(seed).AddSystem(ordem, sys).Build()`; mundo completo: `WorldBuilder.CreateDefault(seed)`.
- **Eventos:** `public sealed record Foo(EntityId X) : GameEvent;` e `world.Events.Publish(new Foo(...))` (o tick é carimbado pelo bus). Reagir: sistemas posteriores leem `world.Events.Current` / `world.Events.Of<Foo>()` (inclui o que sistemas anteriores publicaram no mesmo tick; não há callbacks, a ordem de reação é a ordem dos sistemas). Para reagir a evento no mesmo tick, seu sistema precisa ter ordem MAIOR que a do publicador. Consumidores externos: `world.LastTickEvents` após `Step()`. Eventos publicados entre ticks (ex.: spawn inicial) são carimbados com o próximo tick e aparecem em `LastTickEvents` do próximo `Step()`. Eventos iniciais: `EntitySpawned`, `EntityDied`, `EntityRemoved`.
- **Intenções:** `world.Enqueue(intent)`. No início de `Step()`, intenções de entidade inexistente/morta (ou `MoveIntent` inválida/sem `Movement`) são descartadas; `MoveIntent`/`StopIntent` são aplicadas pelo próprio `World` (escrevem `Movement.Direction`, normalizada a no máximo 1); as válidas ficam em `world.TickIntents` durante o tick. **T-008:** o sistema de habilidades lê `UseAbilityIntent` de `world.TickIntents` (não há handler registrado; só o tipo existe). Novos tipos de intenção: `record : Intent(EntityId Entity)` em `Intents/` e tratar no sistema correspondente (ou, se for movimento-like, no `switch` de `World.ApplyIntents`).
- **Morte:** dano deve reduzir `Health.Current`; `DeathSystem` (ordem 9000) chama `world.Kill(entity)` quando `Current <= 0` (publica `EntityDied` uma vez, zera movimento). Também pode chamar `world.Kill` direto. Entidades mortas permanecem no mundo até `world.Remove(id)` (publica `EntityRemoved`).
- **RNG:** `world.Random` (`NextUInt`, `NextInt(max)`, `NextFloat`); seed no `WorldBuilder`. Entra no hash.
- **Snapshot/consulta:** `world.Snapshot()` retorna `WorldSnapshot(Tick, Entities)`; cada `EntitySnapshot` tem `Get<T>()/TryGet<T>()` com componentes clonados (qualquer componente novo aparece automaticamente). `world.ComputeStateHash()` cobre tick, RNG, próximo id e todos os componentes.

**Decisões/desvios:** `Health.IsAlive` só muda via `World.Kill` (setter interno). `Entity.IsAlive` = não removida e (sem Health ou Health vivo). A fila de intenções pendentes não entra no hash. Hash usa FNV-1a e `BitConverter` de floats (determinístico na mesma plataforma/runtime; entre plataformas diferentes, cuidado com float). Movimento sem colisão, facing = direção normalizada ao andar. Nada fora das áreas permitidas foi alterado. Pendência: nenhuma.
