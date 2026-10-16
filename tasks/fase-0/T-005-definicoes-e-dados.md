# T-005 — Definições de dados e carregador JSON

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** T-003
- **Libera:** T-008
- **Áreas que pode alterar:** `src/Game.Core/Definitions/`, `src/Game.Data/`, `tests/Game.Data.Tests/`, `data/schemas/`

## Contexto

Regras de jogo vivem em `data/*.json` e são lidas por cliente, servidor e testes. Leia `design/classes-v0.1.md`, `design/combat-encounters-v0.1.md` e a seção "Dados de design" de `tech/stack-proposal-v0.1.md`.

## Objetivo

Tipos imutáveis que descrevem o conteúdo do jogo e um carregador que lê, valida e entrega um `GameDatabase`.

## Requisitos

### Tipos em `Game.Core/Definitions` (records imutáveis)

- `ClassDefinition`: id, nome, vida, velocidade, raio de colisão, recurso (tipo, máximo, inicial, regeneração por segundo — pode ser negativa), id do ataque básico, ids de habilidades.
- `AbilityDefinition`: id, nome, tags, custo (recurso, quantidade), recarga, tempo de conjuração, `castWhileMoving`, alvo (`TargetingDefinition`), lista de efeitos.
- `TargetingDefinition` (polimórfico por `type`): `self`, `unit` (alcance, facção válida: `enemy`/`ally`/`any`), `melee_arc` (alcance, ângulo), `line` (comprimento, largura), `circle_at_point` (alcance, raio), `circle_self` (raio).
- `EffectDefinition` (polimórfico por `type`): `damage` (base, escola: `physical`/`arcane`/`fire`/`frost`/`holy`/`nature`), `heal`, `shield` (quantidade, duração), `apply_status` (status, duração opcional), `remove_status` (tag, quantidade), `displacement` (tipo `dash`/`knockback`, distância, direção `aim`/`away_from_source`/`toward_target`/`backward`), `interrupt`, `threat` (quantidade ou `taunt` com duração), `resource` (tipo, quantidade), `spawn_area` (id de área), `summon` (monstro, quantidade, raio).
- `StatusDefinition`: id, nome, tags, duração padrão, empilhamento (`refresh`/`stack` com máximo), efeitos periódicos (intervalo + efeitos), modificadores numéricos (velocidade, dano recebido, dano causado), flags (`stunned`, `rooted`, `silenced`).
- `AreaDefinition`: id, raio, duração, intervalo, modo (`periodic`/`on_enter_once`), facção afetada, efeitos.
- `MonsterDefinition`: id, nome, vida, velocidade, raio, rank (`normal`/`elite`/`boss`), tags (`beast`, `undead`, `corrupted`, `humanoid`...), resistências por escola, habilidades, perfil de IA (id + parâmetros livres em dicionário).
- `ArenaDefinition`: id, linhas ASCII (`#` parede, `.` chão; outros caracteres reservados), escala opcional.
- `EncounterDefinition`: id, nome, arena, jogadores (classe + posição), inimigos (monstro + posição), condição de vitória (`all_enemies_dead`), derrota (`all_players_dead`, tempo limite opcional).

Tempos ficam em **segundos** no JSON e são convertidos para **ticks** (20/s) no carregamento; os records expõem ticks.

### `Game.Data`

- Lê todos os `*.json` de um diretório (um arquivo pode ter vários itens: `{ "abilities": [ ... ] }` etc.).
- `System.Text.Json` com polimorfismo por `type`.
- Validação com lista de erros (arquivo + caminho + mensagem), não para no primeiro erro:
  - ids únicos por tipo;
  - todas as referências resolvem (habilidades, status, áreas, monstros, arenas, classes);
  - números em faixas válidas (vida > 0, recarga ≥ 0, raio > 0…);
  - arena retangular e com pelo menos uma célula livre.
- `GameDatabase` com dicionários somente leitura por id.

### Schemas

`data/schemas/*.schema.json` para editor (autocomplete). A validação que vale é a do C#.

## Fora do escopo

Conteúdo real do jogo (T-014) — use dados mínimos em `tests/Game.Data.Tests/Fixtures/`. Regras de recurso por evento (T-010 estende `ClassDefinition`). Fases de boss (T-012 estende `MonsterDefinition`).

## Critérios de aceite

- Fixture válida carrega com sucesso;
- testes cobrindo cada erro de validação acima;
- conversão segundos → ticks testada (incluindo arredondamento);
- `./scripts/check.sh` passa.

## Resultado

_(preencher ao concluir)_
