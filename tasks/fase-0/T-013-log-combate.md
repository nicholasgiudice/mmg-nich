# T-013 — Log de combate e resumo de morte

- **Ambiente:** VPS
- **Modelo:** Leve
- **Depende de:** T-008
- **Libera:** T-022
- **Áreas que pode alterar:** `src/Game.Core/CombatLog/`, `tests/`

## Contexto

Critério de design repetido em todos os documentos: "o jogador deve entender por que tomou dano ou perdeu o combate".

## Requisitos

1. Consumidor de eventos que produz linhas em português com nomes das definições, por exemplo:
   - "Lobo Cinzento usou Mordida em Você: 12 de dano (4 absorvidos)";
   - "Você foi atordoado por Golpe Pesado (2,0 s)";
   - "Matriarca da Floresta entrou na fase 2".
2. Cada linha tem tick, categoria (dano, cura, status, recurso, fase, morte) e ids envolvidos, para o cliente filtrar e colorir.
3. **Resumo de morte**: ao morrer, as últimas N entradas (padrão 10 ou últimos 5 segundos) que afetaram a entidade, com dano total por fonte.
4. Novos tipos de evento das tarefas 009–012 devem ser fáceis de adicionar (formatador por tipo). Formatar os que existirem quando esta tarefa rodar; registrar os que ficaram para depois.

## Critérios de aceite

- Testes com eventos sintéticos para cada formato;
- resumo de morte com agregação por fonte;
- `./scripts/check.sh` passa.

## Resultado

**Feito:** log de combate em `src/Game.Core/CombatLog/`, sem sistema: a classe `CombatLogRecorder` consome `world.LastTickEvents` depois de cada `world.Step()` (chamar `Record(world)`, que é idempotente por tick). Devolve os `DeathSummary` gerados nos `EntityDied` do tick. 17 testes novos em `tests/Game.Core.Tests/CombatLogTests.cs`; `./scripts/check.sh` passa (94 testes Core + 62 Data).

**Arquivos:** `CombatLogModel.cs` (`LogCategory`, `CombatLogEntry`, `DeathSummary`, `DamageBySource`), `CombatLogContext.cs` (nomes, "Você", números em pt-BR, `Entry(...)`), `CombatLogFormatters.cs` (registro Type -> função), `CoreCombatLogFormatters.cs` (formatadores de T-008), `CombatLogRecorder.cs`.

**Decisões**
- Classe chama `CombatLogRecorder` (não `CombatLog`): o nome `CombatLog` colide com o namespace `Game.Core.CombatLog` para quem está fora dele.
- Sem sistema: nenhuma linha em `SystemsRegistry`, nenhuma mudança em `SystemOrder`. O recorder precisa ser chamado após cada `Step`. Quem usar o log (servidor/cliente) faz essa chamada.
- Categorias: as seis pedidas (dano, cura, status, recurso, fase, morte) mais `Habilidade` (conjuração, rejeição, interrupção e deslocamento não cabem nas seis). O cliente filtra por `LogCategory`.
- `AbilityExecuted` e `AbilityStarted` instantâneo (`CastTicks = 0`) são **suprimidos** (formatador retorna null): o resultado de cada efeito já gera a linha, e a linha da execução duplicaria "X usou Y".
- `viewer` (`EntityId?`) no construtor: a entidade do jogador aparece como "Você". Sem viewer, todos usam o nome do `UnitInfo`.
- Nomes: `UnitInfo.Name` para entidades; habilidade/status pelo `Content` (áreas não têm nome, usam o id). Entidades removidas depois do evento usam o último nome visto no `Record`; se nunca foram vistas, "Entidade #id".
- Resumo de morte: entradas que envolvem a vítima **e** ocorreram nos últimos 5 s (100 ticks) **e** são as últimas 10 (os dois limites valem juntos; interpretei "ou" do requisito como "e"). Dano por fonte: soma de `Amount` (total, incluindo absorvido), `Absorbed` e `Hits`, agrupado por `(Source, SourceId)`, só das linhas dentro da janela. A linha de morte já está no resumo.
- Capacidade padrão 1000 linhas (as mais antigas saem).

**Como adicionar um formatador (T-009 a T-012)**
No módulo da tarefa, defina um método `static CombatLogFormatters Register(CombatLogFormatters f)` (ou chame `f.Register<MeuEvento>((ctx, e) => ctx.Entry(e, LogCategory.Status, "texto", ids...))`) e chame-o onde o log é criado (hoje `CombatLogFormatters.CreateDefault()`, que chama `CoreCombatLogFormatters.Register`). Registrar o mesmo tipo duas vezes lança erro. Retornar `null` suprime a linha. Não é preciso editar `CoreCombatLogFormatters` nem `CombatLogRecorder`.

**Eventos sem formatação (ficaram de fora)**
- `EntitySpawned` e `EntityRemoved`: estruturais, sem linha de combate; aparecem em `UnformattedTypes` do recorder por design.
- Eventos das tarefas 009–012 (ainda não existem): status (aplicação/expiração de status), recurso (T-010), ameaça (T-011), fase e telegráficos (T-012). Quando existirem, cada um registra seu formatador no módulo.

**Pendências:** nenhuma para T-013. Não há push nem merge.
