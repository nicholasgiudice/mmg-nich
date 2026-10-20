# T-060 — Core: níveis de habilidade (NH) e múltiplos acertos

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-034 (tabelas de níveis aprovadas)
- **Libera:** T-061, T-062
- **Áreas que pode alterar:** `src/Game.Core/`, `src/Game.Data/`, `data/schemas/`, `tests/Game.Core.Tests/`, `tests/Game.Data.Tests/`

## Contexto

`design/progression-v0.1.md` §7 e `design/skills-v0.1.md` definem que cada habilidade tem **níveis de habilidade (NH)** — 5 nas normais, 3 na assinatura — e que cada NH muda valores (dano, cura, escudo, duração, raio, recarga) e às vezes o comportamento (número de flechas, explosões, giros, alvos, cargas). Hoje o jogo só tem o NH1. Esta tarefa cria o mecanismo genérico; os efeitos novos (atordoar, saltos, devolver Mana...) ficam na T-061 e os dados na T-062.

## Requisitos

1. **Formato nos dados**: cada habilidade pode declarar `ranks` (lista do NH2 em diante) com sobrescritas parciais: valores dos efeitos (`base`, `scaling` quando existir, `duration`, `radius`), `cooldown`, `castTime`, `cost`, `hits`/`hitScale` (acertos extras e o valor de cada um) e `charges`. Validação no `GameDataValidator` (NH contíguos, máximo 5 — 3 para habilidades marcadas como assinatura).
2. **Estado**: o NH de cada habilidade conhecida fica no `AbilityState` (replicado); `EntityFactory` aceita os NH na criação (padrão NH1 para todas, mantendo o protótipo igual); encontros de teste podem definir NH por jogador.
3. **Resolução**: ao usar uma habilidade, o Core resolve a definição efetiva do NH atual (cache por habilidade+NH, sem alocar por uso).
4. **Múltiplos acertos**: `hits` com intervalo entre acertos (ex.: flechas a cada 0,15 s, explosões a cada 0,4 s), cada acerto com `hitScale`; acertos extras em alvo único seguem para o inimigo mais próximo se o alvo morrer. Eventos e log mostram cada acerto.
5. **Cargas** (`charges` por recarga, ex.: Recuo Rápido NH5 com 2 usos) — diferente das cargas por encontro da poção (T-017).
6. Testes: NH1 sem `ranks` = comportamento atual (todos os testes existentes passam sem mudança); NH3 do Disparo Potente dispara 2 flechas com 85%; NH5 da Explosão dá 3 acertos; cargas do Recuo.

## Critérios de aceite

- protótipo inalterado com NH1;
- definições por NH carregadas e validadas;
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
