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

_(preencher ao concluir)_
