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

**Feito.**

**Causa.** O campo de quantidade foi deixado não editável (`LineEdit.Editable = false`), para que digitar não apertasse as teclas 1 a 6. No Godot, isso desliga o `SpinBox` inteiro: ele ignora qualquer entrada quando o campo não é editável. As setas e a roda do mouse pararam junto.
- Reproduzido contra o servidor local, no Templo: três cliques na seta de cima e a quantidade continua em 1 (`antes-setas-nao-respondem.png`).

**Correção** (`client/scripts/Hud/ShopPanel.cs`). O `SpinBox` saiu. Cada linha de consumível agora tem **−**, o campo, **+** e **Máx.**
- **Máx.:**
  - **Comprar:** o que cabe no ouro, no espaço da mochila (pilhas iguais com folga e espaços vazios) e no limite da loja (`rules.shops.maxQuantity`, hoje 100).
  - **Vender:** a pilha inteira.
- **Teto do campo e do +:**
  - **Comprar:** o limite da loja e o espaço na mochila. O ouro não limita, para o jogador ver o total em vermelho.
  - **Vender:** a pilha.
- **Digitar:**
  - Clicar no campo seleciona o número.
  - Só dígitos contam, e o número fica entre 1 e o teto.
  - **Enter** compra (ou vende, passando pela mesma confirmação de item raro).
  - **Esc** só tira o foco.
  - Clicar fora do campo também tira o foco.
- **Teclas do jogo com o campo em foco:** `ShopPanel.IsTyping` entra no `Hud.IsTyping`, o mesmo caminho do campo de convite do grupo. Com o campo em foco, as teclas 1 a 6, C/K/I/B, Esc e o ataque não agem.
  - O `IsTyping` continua valendo no frame em que o campo perde o foco. Assim o Esc que tira o foco não fecha a loja nem limpa o alvo, e o clique fora não vira ataque.
- **Preço:** mostra o **total**, com "N de ouro cada" embaixo quando a quantidade passa de 1. Na compra, fica vermelho quando falta ouro.
  - Ele muda junto com a quantidade, sem refazer a lista, então o foco não se perde enquanto se digita.
- **Aba Vender:** o mesmo controle. Ela já começava com a pilha inteira, e isso continua.
- **Conta sem Godot:** as contas estão em `client/scripts/Hud/ShopQuantity.cs`, para poderem ser testadas. O cálculo de espaço repete o `Gear.Room` do Core, que é privado para essa assinatura; o Core não está nas áreas da tarefa.
- A janela ficou mais larga (`ListWidth` de 440 para 580) para caber o controle.

**Teste contra o servidor local** (`--world --dev-login --start-zone zone.city`, Guerreiro).
- **Mudança só no teste:** como na T-323, rodei com `sellRatio` 1,0 em `data/shops.json`, para ter ouro sem caçar. Voltou a 0,1 antes do commit.
- **Comprar 5 de uma vez:** vendeu as 5 poções iniciais (75 de ouro). "Máx." pôs 5 (75 de ouro), "Comprar" trouxe "Comprou 5× Poção de Cura Menor por 75 de ouro". A mochila mostra a pilha de 5 e o ouro 0 (`depois-max-5.png`, `depois-comprou-5.png`).
- **Vender 3 de uma vez:** na aba Vender, a pilha de 5; duas vezes − levaram a 3 (45 de ouro), e "Vender" trouxe "Vendeu 3×". A mochila ficou com 2 e o ouro com 45 (`depois-vender-3-escolhido.png`, `depois-vendeu-3.png`).
- **Digitar:** clique no campo, "4" (total 60), Enter comprou 4. "12" na Poção de Mana com 15 de ouro mostra 180 em vermelho. Esc tirou o foco: a loja continuou aberta, e um "3" teclado depois disso não mudou o campo (`depois-digitado-4.png`, `depois-esc-total-vermelho.png`).
  - O roteiro não tem comando de teclado. Para esse teste usei um comando provisório no `Main.cs` (`type:`/`key:`, eventos de tecla pelo `Input.ParseInputEvent`), que não entrou no commit.
- **Capturas:** em `tasks/reports/interface/loja-quantidade/`.

**Testes:** `tests/Game.Client.Tests/ShopQuantityTests.cs` cobre o espaço na mochila, o "Máx." (ouro, mochila, limite da loja, sem ouro e mochila cheia), o teto do campo, o total e o texto digitado.
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

**Regras do servidor:** nenhuma mudou. A `BuyIntent` e a `SellIntent` levam a quantidade, como antes.

**Desvios:**
- **Branch:** criada a partir de `docs/correcoes-playtest-v0.3`, onde está o arquivo desta tarefa, e não de `main`.
- **Fora das áreas da tarefa:** `tests/Game.Client.Tests/Game.Client.Tests.csproj`, com o link do `ShopQuantity.cs`.

**Pendências:**
- **Clique simulado pelo roteiro:** o primeiro clique num botão, sem um `hover:` antes, às vezes não conta. É do jeito como o roteiro gera o mouse, não da loja. Os roteiros acima passam o mouse antes de clicar.
- **Comprar mais de um equipamento de uma vez:** o servidor aceita, mas a janela continua oferecendo só 1, como antes.
- **Ao integrar:** a T-383 também mexe no `Hud.cs`, em outro trecho.
