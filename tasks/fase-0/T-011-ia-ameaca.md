# T-011 — IA de monstros e ameaça

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-008
- **Libera:** T-012, T-014, T-015
- **Áreas que pode alterar:** `src/Game.Core/AI/`, `src/Game.Core/Threat/`, registro de sistemas, `tests/`

## Contexto

Leia `design/combat-encounters-v0.1.md` (comportamento de cada inimigo) e a seção 6 de `design/class-map-validation-v0.1.md` ("controle de ameaça não pode ser botão automático").

## Requisitos

1. **IA produz intenções** (`MoveIntent`, `UseAbilityIntent`) iguais às do jogador. Nada de alterar estado diretamente.
2. **Tabela de ameaça** por monstro: dano gera ameaça (1:1 por padrão), cura gera ameaça reduzida para os monstros engajados com o alvo curado, efeito `threat` (somar ameaça ou `taunt` que força o alvo por uma duração). Valores globais em `data/rules.json` (se a T-009 já criou o arquivo e sua definição, estender; senão criar e a T-009 estende).
3. **Aggro** por proximidade (raio no perfil) e por dano recebido; monstros do mesmo grupo de spawn entram juntos em combate.
4. **Perfis de IA** configurados por id + parâmetros em `MonsterDefinition`:
   - `melee_chaser`: persegue pelo pathfinding e ataca;
   - `pack` (Lobo): o grupo foca o mesmo alvo por alguns segundos; quando um membro morre, os outros recebem um status de agressividade;
   - `ranged_kiter` (Salteador): mantém distância preferida, recua quando alguém se aproxima, usa linha de visão;
   - `swarm` (Rato): aproxima rápido tentando cercar (posições de ataque distribuídas em volta do alvo); um rato troca de alvo ocasionalmente (usa RNG da simulação);
   - `guardian` (Guarda Esquelético): alterna fase de guarda (status com modificador frontal da T-009) e golpe pesado com conjuração visível.
5. Seleção de habilidade do monstro: lista com prioridade/condições simples (alcance, recarga), sem árvore complexa.
6. Eventos: `AggroGained`, `TargetChanged`, `Taunted`.

## Fora do escopo

Fases de boss, invocações e telegráfos (T-012).

## Critérios de aceite

- Teste: provocação força o alvo pela duração e depois volta para a tabela;
- teste: lobos focam o mesmo alvo; morte de um aplica agressividade aos outros;
- teste: Salteador mantém distância de um jogador que se aproxima em campo aberto;
- teste: ratos se distribuem em volta do alvo (não ficam empilhados);
- teste: Guarda alterna guarda e golpe pesado;
- determinismo preservado;
- `./scripts/check.sh` passa.

## Resultado

**Feito:** IA de monstros e ameaça em `src/Game.Core/Combat/Ai/` (sistema, contexto, perfis) e `src/Game.Core/Combat/Threat/` (tabela, regras, eventos, efeito `threat`), conforme a instrução de usar pastas dentro de `Combat/` (a tarefa citava `AI/`/`Threat/` na raiz do Core). 19 testes de Core em `tests/Game.Core.Tests/AiThreatTests.cs` e 5 de Data em `tests/Game.Data.Tests/ThreatRulesLoaderTests.cs`; `./scripts/check.sh` passa.

**Como funciona**
- `AiSystem` (`SystemOrder.Ai`, depois do pipeline de habilidades) lê `DamageDealt`/`HealApplied` do tick e converte em ameaça; depois, por monstro (ordem de id): poda mortos da tabela, aggro por proximidade (jogador mais próximo dentro de `aggroRange`, com linha de visão), escolha de alvo, `TargetChanged`, e chama o perfil. O perfil só emite `MoveIntent`/`StopIntent`/`UseAbilityIntent` (aplicadas no início do tick seguinte, igual a um jogador). Monstro conjurando não decide nada (só para).
- Componentes novos (entram em hash/snapshot): `ThreatTable` (entradas por id de entidade em `SortedDictionary`, provocação) e `AiState` (alvo, grupo, estado do perfil, cache de caminho). Criados de forma preguiçosa (`AiState.Ensure`) para qualquer entidade de facção monstro cujo `UnitInfo.DefinitionId` está em `Content.Monsters`; `EntityFactory` não foi alterado.
- Ameaça: dano = `Amount x damageMultiplier`; cura efetiva = `Effective x healMultiplier` para cada monstro que tem o curado na tabela (cria entrada para o curador); efeito `threat` (`amount` soma, `tauntDuration` provoca; só em monstros com IA). Troca de alvo exige superar o atual por `overtakeRatio` (1,1). Provocação tem prioridade sobre perfil e tabela, expira por tick ou se o provocador morre, e a tabela volta a valer. Eventos: `AggroGained(Monster, Target, Reason)`, `TargetChanged(Monster, Previous, Current)`, `Taunted(Taunter, Monster, UntilTick)`.
- Grupo de spawn: `AiState.GroupId` (padrão 0 = todos os monstros do mundo/encontro). Quando um monstro sai de "sem aggro" para engajado, os outros do grupo ainda sem aggro ganham `groupAggroThreat` contra o mesmo alvo. Para separar grupos, defina `AiState.Ensure(world, monster).GroupId` após o spawn (a T-014/T-012 pode fazer isso por encontro).
- Regras globais: `ThreatRules` (`Combat/Threat/ThreatRules.cs`: `damageMultiplier`, `healMultiplier`, `overtakeRatio`, `groupAggroThreat`, `proximityThreat`), em `ContentCatalog.Threat`; `data/rules.json` seção `"threat"`; `GameDatabase.Threat`; o loader aceita a seção `threat` (objeto) em qualquer arquivo, independente das demais seções, e valida faixas. Schema atualizado.
- Seleção de habilidade: `Known` na ordem da definição = prioridade; usa a primeira que passa em `AbilityValidator` (recarga, recurso, alcance, LoS). Habilidades com tag que começa com `ai.` são papéis especiais e ficam fora dessa seleção: `ai.guard`, `ai.heavy` (guardian), `ai.rally` (pack).

**Perfis (ids em `MonsterDefinition.Ai.Id`; parâmetros em `Ai.Parameters`, tempos em segundos com sufixo `Seconds`)**
- `ai.none`: passivo (acumula ameaça, não age).
- `ai.melee_chaser`: ataca ou persegue (reto com LoS, senão A* com cache de caminho refeito a cada 1 s ou se o alvo se move >1 tile). Parâmetros: `aggroRange` (8), `approachRange` (menor alcance das habilidades ofensivas).
- `ai.pack`: igual ao anterior; o líder (engajado de menor id) escolhe o alvo pela tabela a cada `focusSeconds` (4) e os outros o seguem (provocação ainda vence). Quando o número de membros vivos do grupo cai, cada sobrevivente usa a habilidade de tag `ai.rally` (auto-alvo) dentro de `rallyWindowSeconds` (2).
- `ai.ranged_kiter`: recua (`retreatDistance` 4) escolhendo direção livre (sondagem de parede), só usando instantâneas enquanto recua; reaproxima se `distance > preferredDistance (7) + approachSlack (1,5)` ou sem LoS; encurralado, para e atira.
- `ai.swarm`: N ratos que miram o mesmo alvo ocupam N ângulos igualmente espaçados (por ordem de id) a `ringRadius` do alvo; troca de alvo por sorteio com `world.Random` a cada `retargetIntervalSeconds` (2) com chance `retargetChance` (0,25), por `retargetHoldSeconds` (3), entre jogadores a `retargetRadius` (12).
- `ai.guardian`: fases em `AiState.Phase`: 0 perseguir, 1 guarda (usa `ai.guard`, fica parado e só reorienta a cada `faceIntervalSeconds`, dando janela de flanco), 2 golpe pesado (`ai.heavy`, conjuração visível), 3 recuperar. Parâmetros `guardSeconds` (3), `recoverSeconds` (1), `faceIntervalSeconds` (1).

**Como adicionar um novo perfil de IA (para a T-012, fases de boss)**
1. Crie uma classe em `src/Game.Core/Combat/Ai/Profiles/` que implemente `IAiProfile` (ou herde `MeleeChaserProfile`): `Think(AiContext)` emite intenções; opcional `SelectTarget(ctx, fromTable)` para alterar o alvo (não é chamado sob provocação). Estado vai em `AiState` (campos genéricos `Phase`, `PhaseUntilTick`, `PhaseEmitTick`, ... ou adicione campos novos com Clone/WriteHash), nunca em campos do perfil (o perfil é um singleton sem estado).
2. Registre com UMA linha em `AiProfileRegistry.CreateDefault()` (`.Register("ai.boss_matriarch", new ...)`). Perfil desconhecido lança `InvalidOperationException` clara no primeiro tick.
3. Use `AiContext`: `Param/ParamTicks`, `TryUseBest`, `Use`/`UseTagged(tag, alvo)`, `FindByTag`, `MoveToward`/`MoveDirection`/`FaceToward`/`Stop`, `GroupMembers`, `DistanceTo`, `HasLineOfSightTo`. Para fases por vida, leia `Health` e guarde a fase em `AiState.Phase`; para ações de fase (invocar, áreas) use habilidades com tags `ai.*` cujos efeitos são `summon`/`spawn_area` (T-012 registra os handlers), assim a IA continua só emitindo `UseAbilityIntent`. Teste de exemplo: `New_profile_can_be_registered_and_produces_intents` (monta o `AiSystem` com um registro próprio).

**Dependências da T-009 (status)**
- A guarda do `guardian` e a agressividade do `pack` são habilidades de dados com efeito `apply_status` (tags `ai.guard` / `ai.rally`); o modificador frontal e o status de agressividade em si são da T-009/T-014. Nos testes, `NewWorld` registra um handler vazio de `apply_status` só se ainda não houver um (`EffectRegistry.IsRegistered`), e define o status `status.test` no catálogo, então os testes continuam válidos depois do merge com a T-009. Os testes verificam as intenções/habilidades usadas, não o efeito do status.
- `IActionGate` (stun/silence) já é respeitado: a IA usa `AbilityValidator`, então monstro atordoado não emite ataque válido.

**Arquivos compartilhados editados (mínimo):** `Combat/CombatServices.cs` (1 linha no slot T-011), `Simulation/SystemsRegistry.cs` (1 linha), `Combat/ContentCatalog.cs` (propriedade `Threat`), `Game.Data/GameDatabase.cs` (propriedade `Threat`), `Game.Data/GameDataLoader.cs` (seção `threat`, `ReadObject`, `ValidateThreat`, campo em `RawContent`), `data/schemas/game-data.schema.json`, `data/rules.json` (novo). Conflitos esperados no merge com a T-009: `data/rules.json` (ambas criam; unir as seções `threat` e `control`), `GameDataLoader.cs`/`GameDatabase.cs`/`ContentCatalog.cs` (cada tarefa acrescenta sua seção/propriedade) e o schema.

**Decisões / limitações**
- Aggro por proximidade exige linha de visão; sem LoS só entra em combate por dano, cura, `threat` ou grupo. Não há leash/reset de combate (monstro sem alvos válidos apenas para).
- Provocação não zera nem eleva a tabela; só força o alvo (o `amount` do efeito, se houver, soma ameaça).
- Ameaça de dano usa o dano final incluindo o absorvido por escudo.
- Rato: a troca de alvo é um override temporário (não altera a tabela); o RNG só é consumido quando o intervalo vence, preservando o determinismo (teste de hash com ratos e salteador).
- Convenção de dados: parâmetros de IA são `double`; a conversão de segundos para ticks é feita pelo perfil (`ParamTicks`), pois `Parameters` não passa pela conversão de sufixo `Ticks` do loader.
- Pendências: nenhuma para o escopo; telegráfos, fases de boss e invocação ficam para a T-012; conteúdo real (ids/tags nas definições de monstros e habilidades) para a T-014.

