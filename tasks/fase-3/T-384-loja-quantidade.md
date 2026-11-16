# T-384 — Loja: escolher a quantidade de poções

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** —
- **Libera:** —
- **Áreas que pode alterar:** `client/scripts/Hud/` (`ShopPanel`, `Hud`), `tests/Game.Client.Tests/`, `tasks/reports/interface/`

## Contexto

Relato do designer no playtest da fase 3 (T-380), em 2026-11-16: na loja de poções não dá para comprar mais de uma de uma vez. O campo de quantidade existe (`ShopPanel.QuantityBox`, um `SpinBox` de 1 até `min(stack, rules.shops.maxQuantity)`, hoje 20), mas não funciona.

Pistas no código:
- o campo foi deixado **não editável** e sem foco, para que digitar não apertasse as teclas 1 a 6 da barra; só as setas mudariam o valor, e elas não estão respondendo;
- a lista da loja é refeita quando o ouro ou o inventário mudam (`StateKey`), e o valor fica em `_quantities`;
- o preço mostrado é o de uma unidade, e a cor do preço não acompanha a quantidade.

## Requisitos

1. Reproduzir contra o servidor local e achar a causa (registrar no Resultado).
2. A quantidade passa a funcionar, com **botões − e +** visíveis ao lado do número, e um botão **"Máx."** que põe o máximo que cabe no ouro, no espaço do inventário e no limite da loja.
3. Digitar o número também funciona. Enquanto o campo tem o foco, as teclas do jogo ficam desligadas (o `Hud` já tem `IsTyping` para o campo de convite do grupo; estenda o mesmo caminho). Enter compra, Esc tira o foco.
4. O preço mostra o **total** (preço × quantidade) e fica vermelho quando falta ouro.
5. A aba **Vender** ganha o mesmo controle.
6. A `BuyIntent` e a `SellIntent` já levam a quantidade; nenhuma regra do servidor muda.

## Critérios de aceite

- contra o servidor local: comprar 5 poções de uma vez no Templo e vender 3 de uma vez; o inventário mostra as quantidades certas;
- capturas da loja com a quantidade escolhida;
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
