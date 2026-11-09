# T-321 — Servidor e Core: NPCs vendedores e lojas

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-320, T-331
- **Libera:** T-322, T-323
- **Áreas que pode alterar:** `src/Game.Core/` (NPC e loja), `src/Game.Data/`, `src/Game.Protocol/`, `src/Game.Server/`, `data/shops.json` (novo), `data/zones.json`, `data/schemas/`, `data/maps/city.tmj` (só os objetos de NPC), testes correspondentes

## Contexto

**Esta tarefa autoriza explicitamente uma economia mínima**, a mesma da T-320: comprar e vender com NPCs por preço fixo. O designer pediu NPCs para venda e lojas para comprar consumíveis (`tasks/reports/playtest-v0.2.md`, P4).

A T-055 já deixou no mapa da Cidade (`data/maps/city.tmj`, camada `areas`) marcadores de seis NPCs, com a classe `npc` e sem lógica: Templo, Salão de Treinamento, Ferreiro, Estalagem, Armazém e Mercado. A T-331 faz o servidor ler pontos e áreas do mapa.

## Requisitos

1. **NPC:** entidade nova, sem facção hostil, sem vida e sem IA de combate. Não vira alvo de habilidade nem de monstro (como o `GroundItem` da T-204). Tem id de definição, nome e posição. Os NPCs vêm dos objetos `npc` do mapa, com a propriedade `shop` apontando para uma loja de `data/shops.json`. Validação: a loja existe, e o NPC está no chão e numa zona segura.
2. **Lojas** (`data/shops.json`): id, nome e lista de ofertas (`item`, preço de compra; `stock` ilimitado). Nesta fase:
   - **Templo:** poções de vida e de Mana;
   - **Ferreiro:** armas e armaduras **comuns** do nível 2 ao 6, de todas as classes (o nível do item é fixo na oferta).
   Os outros NPCs ficam sem loja (Armazém, Treinamento e Estalagem são futuros). Os preços são da T-322: aqui use valores provisórios e marque no Resultado.
3. **Comprar:** `BuyIntent(npc, oferta, quantidade)`. Valida: está a até `rules.shops.interactRange` (proposta: 3 tiles), tem ouro, tem espaço (as pilhas contam). Desconta o ouro e coloca no inventário. Recusas com motivo: longe, sem ouro, sem espaço, oferta inválida.
4. **Vender:** `SellIntent(npc, slot, quantidade)` para **qualquer** NPC com loja. O preço de venda é `sellRatio × valor do item` (proposta: 25%). O valor de um equipamento sai de uma fórmula por nível e raridade em `rules.shops`, e o de um consumível é o `price` dele. Item equipado não pode ser vendido. Não há recompra.
5. **Atomicidade:** compra e venda acontecem num único tick e geram eventos (`ItemBought`, `ItemSold`) com o log. A gravação do personagem dispara depois de cada compra ou venda (gatilho novo na `CharacterPersistence`).
6. **Protocolo v+1:** NPC no snapshot (id, definição, posição) e as ofertas da loja. As ofertas podem ir no conteúdo, já que o cliente lê `data/` e a impressão digital confere a versão. Recomenda-se não mandar as ofertas pela rede.

## Fora do escopo

Estoque limitado, preço dinâmico, troca entre jogadores, armazém, missões.

## Critérios de aceite

- testes do Core e do servidor: comprar com e sem ouro, sem espaço, longe; vender um item e uma pilha parcial; não vender equipado; o ouro e o inventário gravados depois da compra;
- teste de dados: toda loja aponta para itens que existem; todo NPC com loja está numa zona segura;
- bots de rede: opção que anda até o Templo e compra uma poção;
- `./scripts/check.sh` completo passa.

## Resultado

**Feito (branch `task/T-321-npcs-e-lojas`):**

- **Dados.**
  - `data/shops.json` (novo):
    - `shop.temple` (Templo): poções de vida e de Mana;
    - `shop.smith` (Ferreiro): as 7 armas e as 12 armaduras, comuns, nos níveis 2 a 6 (95 ofertas; o nível é fixo em cada oferta);
    - `rules.shops`: `interactRange` 3, `sellRatio` 0,25, valor de equipamento `(10 + 5 × nível) × raridade` (comum 1, incomum 2, raro 4, épico 8) e `maxQuantity` 100 por pedido.
  - `data/maps/city.tmj`: a propriedade `shop` entrou só no Sacerdote (`shop.temple`) e no Ferreiro (`shop.smith`).
  - Schema e validador:
    - loja: id único, nome, item da oferta que existe, nível ≥ 1 (consumível só 1), preço ≥ 0 e oferta sem repetição;
    - regras: `rules.shops` com valores válidos;
    - NPC (no `ZoneComposer`, com arquivo do mapa e nome do objeto): só em zona segura, com o centro no chão, e `shop` que existe em `shops.json`.
- **Preços provisórios (a T-322 calibra).**
  - A oferta pode ter `price`. Ausente, vale o valor do item: o `price` do consumível (10, da T-320) ou a fórmula de equipamento comum (nível 2 = 20, nível 6 = 40 de ouro).
  - Nenhuma oferta real tem `price`: toda a calibração fica em `rules.shops` e no `price` dos itens.
- **Core** (`Combat/Shops/`).
  - Componente `NpcInfo` (definição, nome e loja): o NPC tem só posição e `NpcInfo`, sem vida, facção, colisor nem IA. Por isso não é alvo de habilidade (o alvo exige facção) nem de monstro.
  - `Shop.SpawnNpc` usa como nome a propriedade `name` do mapa (ex.: "Sacerdote").
  - `ShopSystem` (ordem nova `SystemOrder.Shops` = 9700, depois dos itens) trata `BuyIntent(npc, oferta, quantidade)` e `SellIntent(npc, espaço, quantidade)`.
  - Cada pedido valida tudo antes e só então mexe no ouro e no inventário, no mesmo tick. Sai `ItemBought` ou `ItemSold`, ou `ShopRejected` com motivo: sem loja, longe, oferta inválida, quantidade inválida, sem ouro, sem espaço (pilhas parciais contam), espaço vazio ou item desconhecido.
  - Vender: qualquer NPC com loja compra; preço = ⌊`sellRatio` × valor⌋ por unidade; venda parcial de pilha.
  - Item equipado não está no inventário, então não dá para vendê-lo (a venda só aponta para espaços do inventário).
  - Log na categoria Itens.
- **Protocolo v10.**
  - Componente `NpcInfo` (tag 21) e eventos `ItemBought` (108), `ItemSold` (109) e `ShopRejected` (110).
  - `NetIntentKind.Buy`/`Sell`: `Target` = NPC, `Slot` = oferta ou espaço, e o campo novo `Quantity`.
  - As ofertas não vão pela rede: o cliente lê `data/shops.json`, e o fingerprint confere.
- **Servidor.**
  - A `OpenZone` cria os NPCs do mapa (`OpenZone.Npcs`); a Cidade tem os 6.
  - Gatilho novo de gravação: depois de `ItemBought` ou `ItemSold` o personagem é gravado na hora (motivo "compra"/"venda"), no `WorldServer` e na `ZoneRoom`.
  - `Shops` entrou no `ContentCatalog` (servidor, bots, simulador e cliente leem do `GameDatabase`).
- **Bots de rede.** `--shop`: o bot segue um caminho A* até o NPC do Templo. Como o bot de `--dev-login` nasce sem ouro, vende as pilhas do inventário (as 5 poções iniciais dão 10 de ouro) e compra 1 poção de vida. No fim sai a linha `LOJA`. README atualizado.
  - Servidor Release `--world --dev-login --port 7791` com 4 bots por 60 s: `LOJA chegaram ao NPC 4/4 · compraram 4/4 · compras 4 · vendas 4 · recusas 0`, 0 quedas e nenhum erro no log do servidor.
- **Testes:**
  - Core (`ShopTests`):
    - o NPC não tem vida nem facção, o tiro recusa o NPC como alvo e o golpe em arco não o atinge;
    - compra de 25 poções (20 + 5) desconta o ouro;
    - compra de equipamento comum no nível da oferta;
    - sem ouro, sem espaço (com pilha parcial aceitando o que cabe), longe, oferta, quantidade e NPC inválidos;
    - NPC sem loja não negocia;
    - venda de pilha parcial e depois do resto, e de equipamento raro pela fórmula;
    - equipado não se vende;
    - preços do Ferreiro crescem com o nível.
  - Servidor (`WorldTests`): a Cidade tem os 6 NPCs e o cliente recebe o `NpcInfo`; a compra pela rede de um personagem com conta grava ouro e inventário na hora, ainda conectado.
  - Dados (`ShopLoaderTests` e `ZoneLoaderTests`):
    - carga das lojas e regras, e os erros de cada uma;
    - as lojas reais apontam para itens que existem, e todo NPC com loja está em zona segura;
    - o Templo vende as duas poções, e o Ferreiro todas as armas e armaduras do nível 2 ao 6;
    - erro de NPC com loja inexistente, fora de zona segura e dentro de parede.
  - `ZoneMapEquivalenceTests`: o Ferreiro e o Sacerdote agora têm loja.
  - Protocolo: ida e volta de `BuyIntent`/`SellIntent` (e recusa sem NPC) e dos 6 NPCs.
- `./scripts/check.sh` completo passa (Design 101/101). O cliente Godot compila sem avisos.

**Decisões:**

- Não criei `npcs.json`. O id de definição (`npc.priest`) e o nome vêm do objeto do mapa; ainda não há nada de definição de NPC para guardar.
- O **Mercado** (`npc.merchant`) ficou sem loja, como o Armazém, a Estalagem e o Treinamento. A tarefa só define Templo e Ferreiro. Se o designer quiser um lugar só para vender, basta pôr `shop` no objeto do mapa e criar a loja (pode ter `offers` vazio).
- O preço da oferta é opcional (padrão = valor do item). Assim a T-322 calibra por fórmula, sem reescrever 95 ofertas.
- Compra de equipamento com quantidade > 1 cria itens separados, um por espaço.
- NPC é validado no `ZoneComposer`, que tem o mapa e o nome do objeto, junto com as outras regras de mapa da T-331.
- O "gatilho novo na `CharacterPersistence`" ficou nos donos dos gatilhos, que chamam a `CharacterPersistence.Save` com o motivo "compra"/"venda". A própria fila não mudou: ela já junta pedidos seguidos.

**Desvios:**

- `tests/Game.NetBots` (opção `--shop` e README), `client/scripts/Data/ContentLoader.cs` (uma linha: `Shops`) e `tests/Game.Sim/ContentLoading.cs` (uma linha) não estavam nas áreas listadas.
- Na VPS já havia um servidor v8 escutando na porta 7777. Não mexi nele e usei a 7791 para o teste dos bots.

**Pendências:**

- Calibrar preços e valores: T-322.
- Mostrar os NPCs e a janela de loja no cliente: T-323. Hoje o cliente recebe o `NpcInfo`, mas não desenha nada, porque só desenha entidades com vida.
- O site ainda não mostra lojas.
- `design/map-conventions-v0.1.md` não cita a propriedade `shop` como obrigatória para vender (fora das áreas).
