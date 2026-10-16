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

_(preencher: modelo de entidades escolhido e por quê; como outras tarefas registram sistemas e eventos)_
