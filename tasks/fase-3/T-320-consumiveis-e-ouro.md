# T-320 — Core: consumíveis como itens e ouro

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-310
- **Libera:** T-321
- **Áreas que pode alterar:** `src/Game.Core/Combat/Items/`, `src/Game.Core/Combat/Progression/` (só a queda de ouro), `src/Game.Core/Definitions/`, `src/Game.Data/`, `src/Game.Protocol/`, `src/Game.Server/Persistence/`, `data/items.json`, `data/abilities_items.json`, `data/classes.json`, `data/rules.json`, `data/schemas/`, `tests/Game.Sim/` e `tests/Game.Design.Tests/` (só o que mudar por causa das poções), testes correspondentes

## Contexto

**Esta tarefa autoriza explicitamente uma economia mínima** (ouro e consumíveis), pedida pelo designer no playtest da fase 2 (`tasks/reports/playtest-v0.2.md`, P3–P5). O `AGENTS.md` deixa economia fora de escopo, salvo pedido explícito: este é o pedido. Os limites ficam em "Fora do escopo", abaixo.

Hoje a poção (`item.minor_healing_potion`) é uma **habilidade** em `data/abilities_items.json`, com 3 cargas "por encontro", que toda classe recebe por `consumables` em `classes.json`. No mundo aberto ninguém sabe quando as cargas voltam (`design/itens.md`, pendências). Para o jogador ela aparece como habilidade, e não deveria.

O playtest pediu:
- consumíveis que se **compram** (poções de vida e de Mana);
- **ouro** que cai dos monstros;
- consumíveis na barra de atalhos (cliente, T-323).

## Requisitos

1. **Item consumível** em `data/items.json`: tipo novo `consumable`, com `stack` (máximo por espaço; proposta: 20), `useAbility` (id de uma habilidade em `abilities_items.json`, que define o efeito e a recarga) e `price` (preço base de venda da loja; o valor fica para a T-322). As habilidades de item deixam de ter `charges`.
2. **Inventário com pilhas:** um espaço guarda até `stack` unidades do mesmo consumível. Pegar e comprar empilham; largar larga a pilha inteira. `ItemInstance` ganha quantidade (ou um tipo próprio de pilha), sem quebrar os itens de equipamento.
3. **Usar consumível:** intenção nova `UseItemIntent(slot)`. Ela executa a habilidade do item no próprio personagem e gasta 1 unidade. A recarga é **por habilidade de item** (duas pilhas da mesma poção dividem a recarga). Recusas com motivo: em recarga, espaço vazio, não é consumível, morto.
4. **Poções:** `item.minor_healing_potion` (cura 60, recarga 20 s, como hoje) e uma nova `item.minor_mana_potion` (Mana 40, recarga 20 s; o número final fica com a T-322). Os ids das habilidades continuam `item.*`, e os itens consumíveis usam `consumable.*` (por exemplo `consumable.minor_healing_potion` → habilidade `item.minor_healing_potion`).
5. **Fim da poção como habilidade:** tirar `consumables` de `classes.json` e do Core. O personagem novo começa com **5 poções de vida** no inventário (`startingInventory` em `classes.json`). Os encontros de teste e o simulador que usavam a poção como habilidade passam a dar as poções no inventário e a usá-las pela intenção nova. Os resultados de `Game.Design.Tests` não podem mudar, ou devem mudar pouco e com explicação.
6. **Ouro:** componente `Wallet` (quantidade inteira, só o dono recebe no snapshot). Ao morrer, o monstro dá ouro: `goldPerMonsterLevel × nível × multiplicador da categoria`, com variação de ±20% sorteada pelo RNG da simulação. O ouro é dividido **igualmente entre quem recebeu XP** daquela morte (participantes e membros do grupo próximos, T-310), arredondando para baixo. O resto é perdido. O ouro vai direto para a carteira, sem objeto no chão, e gera o evento `GoldGained` (log, categoria Itens).
7. **Gravação:** `CharacterDocument` versão 2 com `gold`, e pilhas no inventário. O documento versão 1 continua carregando (ouro 0). Um personagem antigo que não tem poções recebe as 5 iniciais na primeira carga com o documento v1.
8. **Protocolo v+1:** `UseItemIntent`, `Wallet` no estado do dono, quantidade nos itens e os eventos novos.

## Fora do escopo

Troca entre jogadores, leilão, mercado de jogadores, crafting, ouro como item no chão, banco ou armazém.

## Critérios de aceite

- testes do Core: empilhar até o máximo e abrir um espaço novo; usar gasta 1 e respeita a recarga; duas pilhas dividem a recarga; o ouro cai, a soma é dividida no grupo e é determinística com a seed;
- teste do servidor: ouro e pilhas gravados e carregados; documento v1 carrega com ouro 0 e as poções iniciais;
- `Game.Design.Tests` passando (com explicação de qualquer número que mudou);
- `./scripts/check.sh` completo passa.

## Resultado

**Feito (branch `task/T-320-consumiveis-e-ouro`):**

- **Dados.**
  - `data/items.json`: itens novos `consumable.minor_healing_potion` e `consumable.minor_mana_potion` (`type: consumable`, `stack` 20, `useAbility`, `price` 10).
  - `data/abilities_items.json`: a poção de vida perdeu `charges` (cura 60, recarga 20 s). Habilidade nova `item.minor_mana_potion` (efeito `resource` mana +40, recarga 20 s). Os números finais ficam com a T-322.
  - `data/classes.json`: `consumables` saiu. Toda classe tem `startingInventory: [{ "item": "consumable.minor_healing_potion", "count": 5 }]`.
  - `data/rules.json` (`progression`): `goldPerMonsterLevel` 2, `goldByRank` (normal 1, elite 4, chefe 20) e `goldVariance` 0,2. São uma proposta: a T-322 calibra.
  - O schema e o `GameDataValidator` cobrem tudo isso:
    - consumível sem `slot`, `armorType`, `stats` nem `classes`, com `useAbility` existente, instantânea, sem NH, sem telegráfo, sem cargas por recarga e com alvo `self`/`circle_self`;
    - equipamento com `slot`, `stack` 1 e sem `useAbility`;
    - `price` ≥ 0;
    - `startingInventory` com itens que existem, `count` ≥ 1 e cabendo no inventário;
    - consumível não pode ser equipamento inicial nem estar em tabela de queda.
- **Core.**
  - `ItemBaseDefinition` ganhou `Type` (`ItemKind.Equipment|Consumable`), `Slot` anulável, `Stack`/`MaxStack`, `UseAbility` e `Price`. `ItemInstance` ganhou `Count` (padrão 1, equipamento continua 1) e `ItemInstance.Stack(id, n)`.
  - `Gear.AddToInventory(world, e, item)`: completa as pilhas do mesmo item, em ordem de espaço, e o que sobra abre espaços novos. É tudo ou nada: se a pilha não cabe inteira, nada entra.
  - Pegar do chão usa o mesmo método. Largar larga a pilha inteira. Equipar um consumível é recusado com `NotEquippable`.
  - `UseItemIntent(entity, slot)`: tratada no `AbilitySystem` por `Items/Consumables.cs`. Ela valida e executa a habilidade do item com `AbilityValidator.Validate(..., itemUse: true)`, que não exige que a habilidade esteja entre as conhecidas. Depois tira 1 unidade (a última esvazia o espaço) e publica `AbilityStarted`/`AbilityExecuted` e `ItemUsed`.
  - A recarga fica em `AbilityState.ReadyAtTick` pelo id da habilidade, então duas pilhas do mesmo item dividem a recarga. A poção de Mana tem recarga própria.
  - Recusas: `ItemRejected(Use, …)` com `OnCooldown`, `EmptySlot`, `NotConsumable`, `Dead`, `UnknownItem` e `CannotUseNow` (conjurando, atordoado…).
  - Saíram `ClassDefinition.Consumables`, `AbilityDefinition.Charges`, `AbilityState.Consumables`/`ChargesUsed`/`ChargesRemaining`, `AbilityExecuted.ChargesRemaining` e `AbilityRejectReason.NoCharges`.
  - `ClassDefinition.StartingInventory`: o `EntityFactory.SpawnPlayer` dá o inventário inicial e uma `Wallet` vazia a todo personagem com atributos.
  - **Ouro:** componente `Wallet { Gold }`. Em `ProgressionSystem.AwardKill`, depois da queda de itens, `DropGold` calcula `goldPerMonsterLevel × nível × goldByRank` × (1 ± variância). É um sorteio do RNG da simulação, arredondado. O total é dividido igualmente entre a mesma lista que recebeu a XP (participantes e grupo próximo, T-310), com arredondamento para baixo, e o resto se perde. Evento `GoldGained` (log, categoria Itens). Sem ninguém, ou sem ouro configurado, o RNG não é consumido.
  - Formatadores de log de `ItemUsed`, `GoldGained` e dos motivos novos.
- **Protocolo v9.**
  - Componente `Wallet` (tag 20), que só o dono recebe (`ClientView.OwnerOnly`).
  - `Count` em todo item serializado.
  - `NetIntentKind.UseItem` (campo `Slot`).
  - Eventos `ItemUsed` (106) e `GoldGained` (107).
  - Saíram as cargas do `AbilityState` e do `AbilityExecuted`.
- **Gravação.** `CharacterDocument` versão 2: `gold` e `count` nos itens (ausente = 1). Lê v1 e v2.
  - Na v1 o ouro é 0 e `GrantStartingInventory` fica ligado. Se o inventário gravado não tem nenhum item do inventário inicial da classe, o personagem recebe as 5 poções na carga. Na próxima gravação o documento já sai v2, e a regra não se repete.
  - Ouro negativo e quantidade < 1 contam como documento corrompido. Pilha acima do máximo é cortada na carga.
  - Personagem gravado começa com o inventário salvo (o inicial da classe é limpo). Personagem novo (`{}`) fica com o inicial.
  - A troca de zona leva ouro, pilhas e a recarga das habilidades de item (`AbilityTimers` aceita as habilidades usadas por consumíveis).
- **Bots e simulador.** `ConfigurableBot.TryPotion` usa o primeiro espaço do inventário com um consumível de cura, por `UseItemIntent` (mesmo limiar `potionBelow`). Encontros e simulador recebem as poções pelo `startingInventory`, sem código próprio. `Game.Sim` e `Game.Design.Tests` não precisaram de mudança.
- **Testes.**
  - Core (`ConsumableTests`):
    - empilhar até 20 e abrir espaço novo, sem misturar itens;
    - pilha que não cabe inteira não entra;
    - largar a pilha inteira e pegar de volta empilhando;
    - consumível não se equipa;
    - usar cura, gasta 1 e respeita a recarga;
    - duas pilhas dividem a recarga, e a de Mana não;
    - a última unidade esvazia o espaço;
    - recusas com motivo;
    - personagem novo com 5 poções e ouro 0;
    - ouro dentro da variação, dividido igualmente no grupo (estranho não recebe), determinístico com a seed;
    - sem regra, sem ouro.
  - Servidor:
    - ouro e pilhas gravados e carregados (v2) num servidor novo;
    - documento v1 carrega com ouro 0, mantém o inventário e ganha as 5 poções uma vez só;
    - ida e volta do documento (count, gold, v1, valores inválidos);
    - `Wallet` só para o dono;
    - poções e recarga sobrevivem à troca de zona.
  - Dados: carga e erros de consumível e de inventário inicial. Protocolo: `UseItemIntent`, `Wallet` e `Count`.
- `./scripts/check.sh` completo passa, com `Game.Design.Tests` 101/101. O cliente Godot (`client/MmoRpgClient.csproj`) compila sem avisos na VPS.

**Números do `Game.Design.Tests` / simulador:**

- `Game.Sim --encounter all --strategy rotation --runs 100`: saída **idêntica** à de `main`. Os personagens de encontro não têm progressão, então não há ouro nem saque, e a poção é usada igual.
- Com perfil de personagem (`--level 8 --gear incomum --monster-level 8`, `kite+dodge,rotation+dodge`), os números mudam pouco, por dois motivos:
  1. Os personagens com progressão ganham ouro, e o sorteio da variação consome o RNG da simulação. Isso troca a sequência de acasos das lutas seguintes: é ruído com a mesma distribuição. Com `goldPerMonsterLevel` 0, só a Matriarca da Floresta difere de `main`.
  2. Na luta longa da Matriarca da Floresta, os bots tinham 3 cargas e agora têm 5 poções. Vitória com `kite+dodge`: 93,3% em `main`, 91,7% sem ouro e 90,0% com ouro. Em 60 execuções, essa diferença está dentro do ruído.

**Decisões:**

- `UseItemIntent` é tratada pelo `AbilitySystem`, porque a execução da habilidade mora lá. Mexi no mínimo em `Combat/AbilitySystem.cs`, `Components.cs`, `CombatEvents.cs`, `EntityFactory.cs` e `CombatLog/CoreCombatLogFormatters.cs` (fora das áreas listadas) para tirar cargas e consumíveis e executar a habilidade de item.
- A intenção de um morto o `World` já descarta antes dos sistemas. A recusa `Dead` aparece para quem chama `Consumables.Use` direto (servidor e testes).
- O ouro é `long` na carteira e `int` no evento. Arredondamento do total: `AwayFromZero`. A parte de cada um é arredondada para baixo.
- As regras do ouro ficam em `rules.progression`, ao lado das de XP, porque usam o mesmo nível e a mesma categoria.
- `Count` do item é sempre gravado na rede. No documento, só quando é maior que 1.
- **Áreas fora da lista**, mexidas no mínimo para tudo continuar compilando e funcionando sem as cargas:
  - `src/Game.Bots/ConfigurableBot.cs` (poção pelo inventário);
  - `src/Game.Site` (página de itens mostra os consumíveis com pilha, preço e inventário inicial; a de classe mostra o inventário inicial);
  - cliente (`client/scripts`, ícones);
  - `tests/Game.Server.Tests`, `Game.Protocol.Tests`, `Game.Client.Tests` e `Game.Data.Tests` (testes antigos de cargas removidos ou adaptados).
  - Nos testes antigos de itens, o `Hero()` limpa o inventário inicial, para os espaços continuarem previsíveis.
  - O teto do snapshot inicial da Matriarca no teste de protocolo subiu de 2048 para 2304 B: cada personagem agora manda uma pilha e uma carteira, e o snapshot ficou com 2074 B.
- **Cliente (mínimo, a T-323 faz o de verdade):**
  - Q usa o primeiro consumível do inventário com `UseItemIntent`. Um `consumable.*` na barra também funciona.
  - O espaço Q e a barra mostram o item com a recarga da habilidade dele e a quantidade.
  - A tela de habilidades lista os consumíveis do inventário inicial, mas eles não se arrastam.
  - O tooltip do item mostra "consumível · n/20".
  - Ícones: a chave da poção de vida virou `consumable.minor_healing_potion`. A poção de Mana usa o `P_Red01.png` recolorido para azul (CC0; anotado em `icons-credits.txt`) na célula vazia (204, 204).

**Pendências:**

- Loja, NPC e preço de venda: T-321/T-322.
- Calibrar ouro, preços e a poção de Mana: T-322.
- Barra de consumíveis, ouro no HUD e inventário com pilhas no cliente: T-323. Testar no Windows.
- `design/itens.md` ainda descreve a poção como habilidade com cargas. Atualizar quando a T-322 fechar os números.
- A poção de Mana não faz nada para quem não usa Mana. Hoje todas as classes usam.
