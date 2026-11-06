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

_(preencher ao concluir)_
