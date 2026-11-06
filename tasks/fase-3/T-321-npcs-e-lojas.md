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

_(preencher ao concluir)_
