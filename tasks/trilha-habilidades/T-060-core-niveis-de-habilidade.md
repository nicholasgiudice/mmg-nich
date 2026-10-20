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

Concluída em 2026-10-19 no Windows (a tarefa é de VPS; feita aqui a pedido do designer — só `dotnet` e testes).

**Feito:**
1. **Formato nos dados** (`AbilityDefinition`, sem código de leitura novo — o `System.Text.Json` mapeia os campos): `signature` (máx. 3 NH), `hits: { count, interval }` (segundos → ticks), `cooldownCharges` e `ranks` — lista do NH2 em diante, cada nível com sobrescritas parciais (`cost`, `cooldown`, `castTime`, `targeting`, `telegraph`, `effects` (substitui a lista inteira), `hits`, `cooldownCharges`) que **herdam do nível anterior**. JSON Schema atualizado. Validação: níveis 2, 3... em ordem; máximo 5 (3 na assinatura); `hits.count` 1–5; `cooldownCharges` ≥ 1; cada nível validado como a base (alvo, telegráfo, efeitos); NH dos encontros referem habilidades existentes e dentro do intervalo.
2. **Resolução**: `AbilityRanks.Resolve` monta a definição efetiva de um NH; `ContentCatalog.AbilityAt(id, nh)` devolve a versão **pré-calculada** na criação do catálogo (imutável, segura com vários mundos em paralelo; NH fora do intervalo é limitado). `AbilitySystem` (validação, conjuração, execução, cargas) e a resolução dos telegráfos (`TelegraphState.Rank`) usam o NH da entidade.
3. **Estado**: `AbilityState.Ranks` (NH por habilidade, ausente = NH1; replicado e no hash) e `CooldownChargesSpent`. Os dois dicionários são criados sob demanda, para o protótipo (tudo NH1) não pagar alocação nos snapshots.
4. **Múltiplos acertos**: o 1º acerto é o próprio uso; os demais vão para `PendingHits` (componente **interno**, não replicado) e saem no tick previsto, publicando `AbilityHit` (índice/total e alvos) e aplicando os efeitos (ou um telegráfo por acerto, para habilidades com telegráfo como a futura Chuva de Flechas). Alvo único morto/fora de alcance → o acerto vai para o inimigo (ou aliado, conforme a habilidade) mais próximo ao alcance; sem ninguém, o acerto se perde. Fonte morta → acertos pendentes cancelados. Acertos extras não custam recurso nem mexem na recarga.
5. **Cargas por recarga** (`cooldownCharges` > 1): cada uso gasta uma carga; a recarga começa na primeira e as cargas voltam uma a uma; "em recarga" só quando todas estão gastas. `AbilityState.IsOnCooldown` / `CooldownChargesReady`.
6. **NH na criação**: `EntityFactory.SpawnPlayer(..., ranks)` e `PlayerSpawnDefinition.Ranks` (encontros de teste com NH por jogador).

**Testes:** `tests/Game.Core.Tests/AbilityRankTests.cs` (13: herança entre níveis, 1/1/2/3 flechas com o dano de cada NH, intervalo e `AbilityHit`, flecha extra indo para o próximo inimigo, acerto perdido sem alvo, área com 3 acertos em todos, fonte morta, cargas por recarga, NH1 sem cargas, NH do encontro replicado) e `tests/Game.Data.Tests/AbilityRankLoaderTests.cs` (5: carregamento e conversão de segundos, ordem dos níveis, limite 5/3, campos inválidos, NH do encontro). Todos os testes anteriores passam sem mudança (protótipo em NH1 inalterado); snapshot replicado da Matriarca 7,9 KB (antes 7,3 KB).

**Fora das áreas listadas:** nenhuma (`data/schemas/` incluído).

**Pendências / para as próximas tarefas:** NH 0 ("habilidade não aprendida") ainda não existe — fica para a tela de pontos (T-064/fase 2); HUD e bots ainda leem a definição base (custo/recarga do NH1) — T-063 e T-064; `AbilityHit` sem linha própria no log (o dano de cada acerto já aparece).
