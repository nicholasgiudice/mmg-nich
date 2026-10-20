# T-061 — Core: efeitos novos usados pelos níveis de habilidade

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-060
- **Libera:** T-062
- **Áreas que pode alterar:** `src/Game.Core/`, `src/Game.Data/`, `data/schemas/`, `tests/Game.Core.Tests/`

## Contexto

Vários NH de `design/skills-v0.1.md` pedem comportamentos que o Core ainda não tem. Esta tarefa implementa os efeitos genéricos (configuráveis por dados), sem mexer nos números das habilidades.

## Requisitos

Efeitos/mecânicas novos, cada um com teste:

1. **Atordoar** junto do dano (Golpe Pesado NH3/NH5) — usar o status de controle com retorno decrescente já existente (T-009).
2. **Saltos** para inimigos próximos com fração do dano (Corrente Sagrada NH3/NH5).
3. **Segundo alvo automático** ("também o inimigo mais próximo do alvo" — Marca NH3; "também o aliado mais ferido" — Cura Direta NH5, Escudo Sagrado NH5; "aliado mais próximo" — Barreira NH5).
4. **Área em volta do alvo** para status (Prisão de Gelo NH3+).
5. **Devolver Mana** quando um escudo acaba, proporcional ao que sobrou (Barreira NH3+).
6. **Refletir** parte do dano corpo a corpo bloqueado (Guarda Firme NH5).
7. **Imunidade temporária** a tags de status (Purificação NH5).
8. **Bônus no próximo ataque** depois de um deslocamento (Recuo Rápido NH3+).
9. **Levantar aliado caído** com fração da vida (Milagre NH3) — exige estado "caído" em vez de remoção imediata do jogador morto em grupo; registrar a decisão.
10. **Remover controle** dos aliados (Muralha Viva NH3).
11. **Área persistente deixada pela habilidade** reaproveitando `spawn_area` (chão em chamas do Cataclismo NH3, lentidão da Chuva NH5).

## Critérios de aceite

- cada efeito novo coberto por teste no Core;
- nenhum número de habilidade alterado (isso é a T-062);
- `./scripts/check.sh` completo passa.

## Resultado

Feito no Windows (pedido do humano). Todos os efeitos são genéricos e configurados por dados; nenhum número de habilidade foi alterado (`data/*.json` intactos — só o schema mudou).

**Como cada item ficou (JSON → Core):**

| # | Pedido | Como usar nos dados |
|---|---|---|
| 1 | Atordoar junto do dano | `apply_status` de um status com tag `control` e `flags.stunned`, com `duration`. Já existia; passa pelo retorno decrescente da T-009. Falta criar o status de atordoamento em `statuses.json` (T-062). |
| 2 | Saltos | efeito novo `chain`: `jumps`, `radius`, `multiplier` (fração da potência), `affects`, `effects` aninhados. Salta do último atingido para o mais próximo ainda não atingido, com linha de visão. Evento `ChainJumped` (para o cliente desenhar o raio). |
| 3 | Segundo alvo automático | efeito novo `extra_targets`: `select` (`nearest` \| `most_injured`), `radius`, `count`, `multiplier`, `affects`, `effects`. Os alvos originais nunca são escolhidos; o conjurador pode ser (ex.: "aliado mais ferido"). |
| 4 | Área em volta do alvo | `extra_targets` com `count: 0` (todos no raio) — Prisão de Gelo: dano só no alvo, congelar no alvo + `extra_targets`. |
| 5 | Devolver Mana do escudo | `shield.refund: { resource, fraction }`. Ao **expirar** com sobra, devolve `round(sobra × fraction)` a quem criou o escudo. Escudo esgotado não devolve. |
| 6 | Refletir corpo a corpo bloqueado | `frontal.reflectMeleeFraction` no status. Corpo a corpo = habilidade com mira `melee_arc`. Bloqueado = dano sem a redução frontal − dano recebido; o reflexo não passa por multiplicadores (sai com o id do status). |
| 7 | Imunidade a tags | `immuneToTags` no status (ex.: `status.purified` de 2 s). Status novos com essas tags são resistidos (`StatusResisted`). |
| 8 | Bônus no próximo ataque | `empower: { abilities, multiplier }` no status. O próximo uso de uma dessas habilidades tem a potência multiplicada (dano/cura/escudo, **em todos os acertos** daquele uso) e o status é consumido. Recuo aplica o status em si (`target: self`). |
| 9 | Levantar caído | efeito novo `revive`: `radius`, `healthFraction`, `count`. Levanta aliados mortos perto do alvo (mais perto primeiro), sem os status de antes. Evento `EntityRevived` (+ linha no registro de combate). |
| 10 | Remover controle dos aliados | `remove_status` com `tags: ["control"]` e o novo `count: 0` (= todos). Em áreas, a habilidade só com efeitos benéficos afeta aliados (regra que já existia). |
| 11 | Área persistente | `spawn_area` (já existia) + novo `onHit` em qualquer efeito: em habilidades com vários acertos, só aplica naquele acerto (a área da Chuva nasce só na 1ª onda). Em habilidade com telegráfo, a área nasce no ponto do impacto. |

**Peças gerais criadas:**
- *Potência do uso* (`EffectContext.Power`): multiplica dano, cura e escudo. Usada por saltos/alvos extras (frações) e pelo `empower`. É guardada nos acertos agendados (`PendingHit.Power`) e no telegráfo (`TelegraphState.Power`/`HitIndex`), então vale para todos os acertos do uso.
- `IModifierSource.ConsumeAbilityPower` (chamado uma vez por uso, ao executar) e `IDamageReaction` (reação a dano já aplicado) em `CombatServices`, no padrão de pontos de extensão que já existia.
- `DamageResolver.Deal` ganhou `melee` e `applyModifiers`.

**Decisão — estado "caído":** o Core já não removia mortos: `World.Kill` só marca `Health.IsAlive = false` e a entidade fica no mundo. Mantive assim: **morto = caído** até alguém removê-lo; `World.Revive` devolve a vida. Não há tempo limite para levantar nem remoção automática; se um dia for preciso (ex.: sumir o corpo de monstros), isso entra em outra tarefa. A derrota continua sendo "todos os jogadores mortos". O cliente ainda não trata `EntityRevived` (fica para a T-064).

**Desvios:** além das áreas listadas, mexi em `tests/Game.Data.Tests/` (teste novo `RankEffectLoaderTests`, formato JSON e validação) e em `src/Game.Core/CombatLog/` (texto do evento `EntityRevived`).

**Testes:** `tests/Game.Core.Tests/RankEffectTests.cs` (17, cobre os 11 itens) e `tests/Game.Data.Tests/RankEffectLoaderTests.cs` (2). `./scripts/check.sh` completo passa; o cliente Godot compila.

**Pendências para a T-062:** criar os status novos nos dados (atordoamento, purificado, foco do Recuo, lentidão da Chuva, chão em chamas) e escrever os NH com estes efeitos.
