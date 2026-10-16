# T-015 — Simulador headless de encontros e bots

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-011
- **Libera:** T-016, T-024
- **Áreas que pode alterar:** `src/Game.Bots/` (novo), `tests/Game.Sim/` (novo), `MmoRpg.sln`, `tests/`

## Contexto

Os documentos de design definem falhas mensuráveis ("se o Guerreiro vence só com Provocação e ataque básico, o combate está raso"). O simulador permite testá-las milhares de vezes sem abrir o jogo. Os bots também serão usados pelo cliente (companheiros controlados por IA no encontro de grupo, T-024) e, na fase 1, como clientes de carga.

## Requisitos

1. **`src/Game.Bots`** (biblioteca, `TargetFramework=$(SharedTargetFramework)`, referencia só `Game.Core`): interface de controlador que, a cada tick, lê o estado do mundo e devolve intenções para uma entidade jogador. Estratégias:
   - `idle` (parado, só ataque básico se houver alvo em alcance);
   - `basic_only` (persegue e usa só ataque básico);
   - `rotation` (lista de prioridade por classe com condições simples: recurso, recarga, vida, número de inimigos próximos);
   - `kite` (para classes à distância: mantém distância e usa obstáculos de forma simples);
   - variações por classe para testar falhas do design, ex.: Guerreiro `taunt_and_basic`, Mago `explosion_spam`, Clérigo `heal_only`.
   - Estratégias de grupo (Matriarca): cada bot com um papel; regra simples de sair de telegráfos e áreas perigosas (opcional por estratégia, para comparar "parado" vs "se move").
2. **`tests/Game.Sim`** (console): carrega `data/`, roda encontro × estratégias × N seeds, mede:
   - taxa de vitória, tempo até vitória (média/p50/p90);
   - dano recebido, cura, recurso gasto e desperdiçado (acima do máximo);
   - usos por habilidade, mortes, troca de fase de boss.
3. Saída em tabela no terminal e JSON (`--out arquivo.json`).
4. CLI exemplo: `dotnet run --project tests/Game.Sim -- --encounter encounter.warrior_wolves --strategy rotation --runs 200`.
5. Pode rodar em paralelo entre seeds (cada mundo é independente).
6. Até a T-014 entrar, usar fixtures de teste; depois funciona com `data/` real sem mudança de código.

## Critérios de aceite

- Mesma seed → mesmo resultado (teste);
- 1000 execuções de um encontro solo de fixture terminam em menos de 1 minuto na VPS (2 vCPU);
- README curto em `tests/Game.Sim/README.md` com exemplos;
- `./scripts/check.sh` passa.

## Resultado

**Feito:** `src/Game.Bots` (biblioteca, só referencia `Game.Core`), `tests/Game.Sim` (console) e `tests/Game.Sim.Tests` (xunit, 15 testes), todos no `MmoRpg.sln`. README em `tests/Game.Sim/README.md`. Fixtures próprias em `tests/Game.Sim/Fixtures/sim.json` (4 classes, 2 monstros `ai.pack`/`ai.melee_chaser`, 4 encontros: cavaleiro x 3 lobos, arqueiro x bruto, mago x 2 brutos, grupo cavaleiro+clérigo x lobos+bruto). `./scripts/check.sh` passa.

**Bots (`Game.Bots`)**
- `IBotController.Think(BotContext, List<Intent>)`: lê o mundo e devolve `MoveIntent`/`StopIntent`/`UseAbilityIntent` (nunca altera o mundo). `BotDirector(world, hazards?)` liga controladores a entidades e chama `world.Enqueue` a cada `Tick()` (chamar antes de `world.Step()`); é o ponto de uso para os companheiros de IA do cliente (T-024).
- Uma única classe `ConfigurableBot(BotPlan)`; as estratégias são presets (`BotStrategies`): `idle`, `basic_only`, `rotation`, `kite`, `taunt_and_basic`, `explosion_spam`, `heal_only`, mais o sufixo `+dodge` (`DodgingController`).
- Guiado por dados: `AbilityInfo` deduz o papel da habilidade dos efeitos (taunt = `threat` com `tauntDuration`; heal; defensive; dash/recuo; interrupt; damage; área), sem ids. Uso sempre validado por `AbilityValidator`. Condições simples por papel (vida, recurso/recarga via validador, nº de inimigos ao alcance, distância). Habilidade com tempo de conjuração emite `StopIntent` junto (andar cancelaria).
- Seleção por dados configurável (`BotOptions`, chaves globais ou `class.x:chave`, valor = ids ou `tag:nome` separados por vírgula): `taunt`, `explosion`, `heal`, `order`, `skip`, `healBelow`, `minAoe`, `kiteDistance`, `retreatDistance`. **T-014/T-016:** se os papéis deduzidos não bastarem para o conteúdo real, passar `--opt class.warrior:taunt=warrior.taunt`, `--opt class.mage:explosion=mage.explosion` etc. (ou preencher `RunSpec.Options`) e papéis de grupo com `--role class.cleric=heal_only`; nada precisa mudar em código.
- `kite`: recua escolhendo entre 16 direções (afasta do inimigo, espaço livre por raycast, bônus por ficar fora da linha de visão = usar obstáculos, estabilidade contra zigue-zague); encurralado, para e luta; fugindo usa só instantâneas e o dash para trás se existir.
- Ponto de extensão para desvio: `IHazardSource.Collect(world, List<Hazard>)` com `CircleHazard`/`RectHazard`/`ConeHazard` (tick de resolução incluso). Não depende de tipos da T-012. A T-016 implementa a fonte lendo `TelegraphStarted` e a passa em `RunSpec.HazardSourceFactory` (ou `BotDirector`). `+dodge` sai do perigo por cima do movimento do bot interno (mantém só instantâneas que não são deslocamento); sem fonte equivale à estratégia base.

**Simulador (`Game.Sim`)**
- `ContentLoading` converte `GameDatabase` -> `ContentCatalog` (inclui `Rules` e `Threat`; o helper ficou no Sim, não em `Game.Data`) e ignora `data/schemas/*.json` (o `LoadDirectory` os leria como conteúdo; usei `LoadSources`, sem alterar `Game.Data`).
- `EncounterRunner.Run(content, spec, seed)` roda um mundo até vitória (todos os monstros vivos mortos), derrota (todos os jogadores mortos) ou tempo (`defeat.timeLimit` do encontro, senão `--max-seconds`, padrão 300). `Batch.Run` usa `Parallel.For` entre seeds; agregação em ordem de seed, então paralelo = sequencial (teste).
- Métricas por execução e agregadas: ver README. Fase de boss é contada por eventos cujo nome de tipo contém "Phase" (sem depender da T-012; se a T-012 usar outro nome, ajustar em `EncounterRunner.Metrics.Consume`).
- `--jitter` (padrão 0,5 tile): desloca posições iniciais por seed (Random com seed explícita, só no Sim, não no Core) porque sem isso execuções são quase idênticas (o RNG da simulação mal é usado). `--jitter 0` desliga.
- CLI: `--data` (padrão `data/`), `--encounter`, `--strategy`, `--role`, `--opt`, `--runs`, `--seed`, `--max-seconds`, `--parallel`, `--out`, `--per-run`, `--list`. Com `data/` atual (sem encontros) a CLI informa "Nenhum encontro encontrado" e sai com código 2.

**Desempenho:** 1000 execuções de `encounter.fixture_knight_wolves` (3 lobos) em ~4,4 s; teste automatizado de 1000 execuções do encontro solo (arqueiro x bruto) exige < 1 min.

**Decisões / limites**
- Recurso desperdiçado: o Core não publica `ResourceChanged` quando o ganho é totalmente perdido; é exato para efeitos `resource` de habilidades e parcial para regras de quantidade fixa; complementado por "tempo com recurso no máximo".
- Uma habilidade por tick por bot; decisões a cada tick (sem tempo de reação humano).
- Alterações fora das áreas listadas: nenhuma (só `MmoRpg.sln`, que está na lista).

**Pendências:** nenhuma para a T-015. Para T-016: fonte de perigos a partir dos telegráfos, números do conteúdo real, ajuste de `--opt` por classe, e conferir o nome do evento de fase.
