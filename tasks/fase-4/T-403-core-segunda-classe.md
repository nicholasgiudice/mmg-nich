# T-403 — Core: segunda classe e habilidades passivas

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-401
- **Libera:** T-404, T-408
- **Áreas que pode alterar:** `src/Game.Core/` (progressão, definições, estados), `src/Game.Data/` (carga e validação), `data/classes.json`, `data/schemas/`, `tests/Game.Core.Tests/`, `tests/Game.Data.Tests/`, `tests/Game.Sim/` (perfil com segunda classe), `design/segunda-classe-v0.1.md` (só para registrar decisões)
- **Branch:** sai de `trilha/segundas-classes` e é integrada nela, não na `main` (seção 6f de `tasks/README.md`).

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

_(preencher ao concluir)_
