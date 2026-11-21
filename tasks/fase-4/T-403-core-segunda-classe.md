# T-403 — Core: segunda classe e habilidades passivas

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-401
- **Libera:** T-404, T-408
- **Áreas que pode alterar:** `src/Game.Core/` (progressão, definições, estados), `src/Game.Data/` (carga e validação), `data/classes.json`, `data/schemas/`, `tests/Game.Core.Tests/`, `tests/Game.Data.Tests/`, `tests/Game.Sim/` (perfil com segunda classe), `design/segunda-classe-v0.1.md` (só para registrar decisões)

## Contexto

Design em `design/segunda-classe-v0.1.md`, seções 1, 2 e 4. Regras nos dados pela T-401 (`secondClassLevel`, `secondClassAttributePoints`, `secondClassRankLevels`, `secondClassSignatureRankLevels`).

Hoje a progressão está em `src/Game.Core/Combat/Progression/Leveling.cs`:

- `CharacterBuild` guarda nível, XP, atributos e NH de cada habilidade;
- `IsLearnable` aceita as habilidades da classe (`ClassDefinition.Abilities`) menos o ataque básico;
- `RaiseSkillRank` checa ponto, NH máximo e nível exigido (`ProgressionRules.RequiredLevel`);
- `RebuildKnown` monta a barra: ataque básico mais as habilidades com NH ≥ 1, na ordem da classe;
- `ResetPoints` devolve todos os pontos.

Esta tarefa cria a segunda classe e as passivas **só no Core e nos dados**, com teste. Servidor, protocolo e gravação são da T-404. As habilidades do Guardião e do Berserker são da T-408: aqui bastam definições de teste.

## Requisitos

1. **Dados das segundas classes** em `data/classes.json`: cada classe base ganha a lista das suas segundas classes, cada uma com id, nome exibido e lista de habilidades. Ids sugeridos: `second_class.guardian`, `second_class.berserker` (com `class.warrior`). Nesta tarefa, só as duas do Guerreiro, ainda **sem habilidades** ou com habilidades de teste nos testes; a T-408 preenche. Validação: ids únicos, habilidades existentes, no máximo uma assinatura por segunda classe.
2. **Escolha da segunda classe** (`ChooseSecondClassIntent`, novo, com o id da segunda classe):
   - aceita só com nível ≥ `secondClassLevel`, sem segunda classe escolhida, fora de combate (`Leveling.InCombat`) e sem conjuração; a segunda classe precisa ser da classe base do personagem;
   - dá os `secondClassAttributePoints` pontos de atributo;
   - é **definitiva**: não há intenção de desfazer. Só a ferramenta de teste (T-405) desfaz;
   - motivos de recusa novos em `ProgressionRejectReason`;
   - evento `SecondClassChosen`.
   **Decisão desta fase:** a escolha é feita pela tela de habilidades (T-410), em qualquer lugar fora de combate. O "mestre da classe na Cidade" do design fica para quando os mapas forem refeitos; registrar no design.
3. **Componente replicável** com a segunda classe do personagem (ou dentro de `CharacterProgress`), para o cliente mostrar. `CharacterBuild` ganha o campo da segunda classe; `SpawnCharacter` e `BuildOf` leem e escrevem; `Validate` recusa segunda classe de outra classe base, segunda classe abaixo do `secondClassLevel` e NH da segunda classe sem segunda classe.
4. **Pontos e NH:**
   - `IsLearnable` passa a aceitar também as habilidades da segunda classe escolhida;
   - o nível exigido por NH usa as listas da segunda classe para as habilidades dela;
   - `AttributePointsAt` conta os +5 quando há segunda classe (`ResetPoints` e `Validate` usam a mesma conta);
   - `ResetPoints` devolve também os pontos das habilidades da segunda classe e **mantém** a segunda classe.
5. **Barra:** `RebuildKnown` põe as habilidades da base primeiro e as da segunda classe depois, cada grupo na ordem dos dados. Passivas **não** entram na barra.
6. **Habilidades passivas** (seção 4 do design): campo `passive: true` na habilidade. Uma passiva:
   - recebe pontos e NH como as outras e segue a mesma regra de nível por NH;
   - não pode ser usada (`UseAbilityIntent` recusa) e não tem custo nem recarga;
   - quando tem NH ≥ 1, mantém no personagem um **estado permanente** definido nos dados da habilidade, no NH atual (trocar de NH troca o estado; voltar a NH 0 tira). O estado sobrevive à morte e ao renascimento.
   Validação: passiva sem custo, sem recarga e sem alvo.
7. **Simulador:** `CharacterProfile` aceita uma segunda classe e uma distribuição de pontos dela, para a T-408.

## Critérios de aceite

Testes no Core para cada caso:

- escolher com nível 19 é recusado; com 20 é aceito e dá +5 pontos de atributo; escolher de novo é recusado; escolher em combate é recusado; escolher a segunda classe de outra classe base é recusado;
- NH1 de uma habilidade da segunda classe exige o nível 21; o NH5 exige 33; a assinatura exige 35, 37 e 40;
- `ResetPoints` devolve os pontos da segunda classe e mantém a escolha;
- a barra fica com a base primeiro e a segunda classe depois, sem passivas;
- passiva com NH1 aplica o estado; subir para NH2 troca o estado; o estado continua depois de morrer e renascer; usar a passiva é recusado;
- `BuildOf` → `SpawnCharacter` devolve o mesmo personagem, com a segunda classe;
- `./scripts/check.sh` completo passa.

## Resultado

Concluída em 2026-11-21, na VPS, branch `task/T-403-core-segunda-classe`.

### Nomes para a T-404 (servidor, protocolo, gravação) e a T-410 (tela)

| O quê | Nome |
|---|---|
| Dados: segundas classes da classe base | `classes[].secondClasses[]` → `ClassDefinition.SecondClasses` (`SecondClassDefinition`: `Id`, `Name`, `Abilities`) |
| Dados: passiva | `abilities[].passive` → `AbilityDefinition.Passive`; `abilities[].passiveStatus` (e `ranks[].passiveStatus`) → `PassiveStatus` |
| Intenção | `ChooseSecondClassIntent(Entity, SecondClassId)` (em `Game.Core.Intents`), tratada pelo `ProgressionSystem` |
| Lógica | `Leveling.ChooseSecondClass(world, e, id)`; ajudantes `Leveling.SecondClassOf(cls, id)`, `Leveling.SecondClassOf(world, e)`, `Leveling.IsLearnable(cls, second, id)`, `Leveling.IsSecondClassAbility(cls, second, id)` |
| Evento | `SecondClassChosen(Entity, SecondClassId, AttributePoints)` |
| Recusas novas | `ProgressionRejectReason.SecondClassAlreadyChosen`, `ProgressionRejectReason.NotASecondClass` (nível baixo usa `LevelTooLow`; em combate ou conjurando, `InCombat`); `AbilityRejectReason.Passive` (usar uma passiva) |
| Componente (replicável) | `CharacterProgress.SecondClass` (`string?`; entra no hash só quando há) |
| Gravação | `CharacterBuild.SecondClass` (`string?`); `SpawnCharacter` lê e `BuildOf` escreve |
| Estado permanente | `StatusManager.PermanentTick` (`long.MaxValue` em `ExpiresAtTick`), `StatusManager.IsPermanent`, `ApplyPermanent`, `RemovePermanent` |

### O que foi feito

- **Dados:** `data/classes.json` dá ao Guerreiro `second_class.guardian` (Guardião) e `second_class.berserker` (Berserker), ainda sem habilidades (a T-408 preenche). Schema atualizado (`secondClasses`, `passive`, `passiveStatus`).
- **Validação (`Game.Data`):** ids de segunda classe válidos e únicos entre todas as classes, nome obrigatório, habilidades existentes, sem repetir, fora da lista da base e sem o ataque básico, no máximo uma assinatura. Passiva: sem custo, recarga, conjuração, telegráfo, efeitos, acertos nem cargas, com targeting `self`, em todos os NH, e com `passiveStatus` existente; só passiva pode ter `passiveStatus`.
- **Escolha:** nível ≥ `secondClassLevel`, sem segunda classe, fora de combate e sem conjurar, só entre as da classe base; dá `secondClassAttributePoints`. Definitiva: não há intenção de desfazer (a T-405 faz isso pela ferramenta de teste). Ordem das recusas: já escolhida, não é da classe, nível, combate.
- **Pontos e NH:** `IsLearnable` passa a aceitar as habilidades da segunda classe escolhida; o NH delas usa `secondClassRankLevels` e `secondClassSignatureRankLevels`; `AttributePointsAt(nível, segundaClasse)` em `Validate`, `SpawnCharacter` e `ResetPoints`. `Validate` recusa segunda classe de outra base, segunda classe abaixo do nível 20 e NH de habilidade de segunda classe sem ela escolhida. `ResetPoints` devolve também os pontos da segunda classe e mantém a escolha.
- **Barra:** ataque básico, depois a base e depois a segunda classe, cada grupo na ordem dos dados, sem passivas.
- **Passivas:** recebem pontos como as outras; não entram na barra; `UseAbilityIntent` é recusado com `AbilityRejectReason.Passive` (antes dos gates). Com NH ≥ 1, o personagem tem o status `passiveStatus` do NH atual como **permanente**; subir de NH troca o status, redistribuir tira. `SpawnCharacter` aplica, então o renascimento no servidor (que recria o personagem pelo que foi salvo) e a troca de zona mantêm o estado. Morto, o status continua (o `StatusSystem` não mexe em mortos).
- **Simulador:** `CharacterProfile.SecondClass` (classe base → segunda classe) e `CharacterProfile.SecondClassRanks` (classe base → NH, somados aos da base). Com segunda classe, os pontos de atributo do perfil contam os +5.
- **Log de combate:** `SecondClassChosen` tem texto; status permanente aparece como "(permanente)"; os novos motivos de recusa têm texto; `SourceLabel` acha o nome da segunda classe.
- **Testes:** `tests/Game.Core.Tests/SecondClassTests.cs` (todos os critérios de aceite), `tests/Game.Data.Tests/SecondClassLoaderTests.cs` (carga e validação), `tests/Game.Sim.Tests/SecondClassProfileTests.cs` (perfil). `BuildComparer` dos testes de progressão passou a comparar a segunda classe.

### Desvios e decisões

- **Protocolo (fora das áreas da tarefa):** o teste `Every_game_event_of_the_core_has_a_codec` exige formato de rede para todo evento do Core, e não há jeito de marcar um evento como só do servidor. Mudança mínima em `src/Game.Protocol/`: codec do `SecondClassChosen` (tag 96, depois de `ProgressionRejected`) e `ProtocolVersion` **15 → 16**; `tests/Game.Protocol.Tests/ProtocolTests.cs` passou a esperar 16. O codec do `CharacterProgress` (campo `SecondClass`), o da intenção e o resto continuam com a T-404. Por isso a T-403 entra na `main` junto com a rodada do protocolo.
- **Estados (área da T-407):** em `StatusManager`, status permanente = `ExpiresAtTick == PermanentTick`, sem campo novo (o hash e o formato de rede não mudam). `ApplyPermanent` ignora imunidade e regras de controle e vale com o alvo morto; `RemovePermanent` é o único jeito de tirar. `Remove`, `RemoveAll` (renascer pela Milagre) e `RemoveByTags` (purificação) pulam os permanentes; `Apply` do mesmo status não tira a permanência. Status permanente é só das passivas: `SyncPassives` tira qualquer permanente que nenhuma passiva pede. `DamageResolver` e efeitos não foram tocados.
- **Passiva "sem alvo":** `targeting` continua obrigatório no JSON (não quis abrir uma exceção no schema e na carga); a passiva usa `{ "type": "self" }` e a validação recusa qualquer outro.
- `BuildOf` lê os NH de `AbilityState.Ranks`, não mais da barra, porque a passiva tem NH e não fica na barra.
- `AbilityRejectReason.Passive` e os novos `ProgressionRejectReason` ficam no fim dos enums (o protocolo serializa o número).
- Recusa por nível na escolha reusa `LevelTooLow` (o texto passou a ser "nível insuficiente", que serve aos dois casos).
- Design: registrada em `design/segunda-classe-v0.1.md` §4 a decisão de escolher pela tela de habilidades nesta fase.

### Pendências

- T-404: codec do `CharacterProgress.SecondClass` e da `ChooseSecondClassIntent`, gravação de `CharacterBuild.SecondClass`.
- T-405: desfazer a segunda classe na ferramenta de teste.
- T-408: habilidades do Guardião e do Berserker (a Sede de Sangue como passiva).
- T-402: os perfis automáticos (`SkillRanks`) ainda só distribuem pontos na base; com `SecondClassRanks`, quem monta o perfil precisa deixar pontos livres.
