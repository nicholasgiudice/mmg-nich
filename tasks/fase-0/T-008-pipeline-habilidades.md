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

_(preencher: pontos de extensão criados e como usá-los)_
