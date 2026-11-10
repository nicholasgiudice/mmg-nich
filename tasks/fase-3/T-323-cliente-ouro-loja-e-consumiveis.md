# T-323 — Cliente: ouro, janela da loja e consumíveis na barra

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** T-321
- **Libera:** —
- **Áreas que pode alterar:** `client/` (inclusive o Input Map em `project.godot`)

## Contexto

A T-320 transformou as poções em itens de inventário (com pilha e recarga por tipo) e criou o ouro. A T-321 criou os NPCs com loja. O playtest (`tasks/reports/playtest-v0.2.md`, P4–P5) pediu que a poção deixe de aparecer como habilidade e que a barra de atalhos aceite consumíveis além de habilidades.

A barra 1–6 configurável (T-081, `HotbarStore`, arrastar e soltar) e o inventário com o boneco (T-083) já existem.

## Requisitos

1. **Ouro:** a quantidade aparece no inventário (embaixo da grade) e sobe "+N ouro" sobre o personagem a cada `GoldGained`.
2. **Consumíveis no inventário:** ícone com a quantidade no canto, e tooltip com o efeito e a recarga. Clique duplo (ou botão direito, escolha uma e registre) usa o item (`UseItemIntent`).
3. **Barra de atalhos com consumíveis:** arrastar uma poção do inventário para a barra cria um atalho para **aquele tipo** de consumível (não para o espaço). O atalho mostra a quantidade total no inventário e a recarga. Com 0 unidades, fica esmaecido. O `HotbarStore` grava o tipo do atalho (habilidade ou item).
4. **A poção sai da tela de habilidades** (T-081) e da barra padrão. A tecla Q, se ainda estiver ligada à poção, vira um atalho de consumível que usa a primeira poção de vida do inventário (ou é removida; registre a escolha).
5. **NPC no mapa:** desenhado com um sprite (do pacote LPC já usado ou um provisório) e com o nome em cima. Clicar no NPC a até o alcance da loja abre a **janela da loja**. Longe, o personagem anda até ele, ou aparece a mensagem "Chegue mais perto".
6. **Janela da loja:**
   - aba **Comprar**: ofertas com ícone, nome, preço e quantidade (para consumíveis), e o botão Comprar;
   - aba **Vender**: o inventário, com o preço de venda no tooltip; vender pede confirmação só para item raro ou épico;
   - recusas (sem ouro, sem espaço, longe) como texto curto.
   A janela fecha se o personagem se afastar.

## Critérios de aceite

- contra o servidor local (`--world --dev-login`): matar monstros e ver o ouro subir, ir ao Templo, comprar poções, arrastar para a barra, usar na luta e ver a recarga, vender um item no Ferreiro;
- capturas da loja, da barra com a poção e do inventário com o ouro;
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Resultado

**Feito** (Windows, 2026-11-09), branch `task/T-323-cliente-ouro-loja-e-consumiveis`.

**Ouro.**
- Inventário: linha com uma moeda desenhada e o valor ("15 de ouro"), embaixo da grade (`InventoryPanel.GoldLine`).
- `GoldGained` do jogador: "+N de ouro" sobe sobre o personagem, em dourado (`WorldView`; cor nova `HudTheme.Gold`).

**Consumíveis no inventário.**
- A pilha mostra a quantidade no canto (de 2 para cima). O nível sai dos consumíveis, porque não diz nada.
- O tooltip mostra o efeito e a recarga da habilidade do item (`ItemText.ConsumableLines`) e a dica de uso.
- **Duplo clique usa** (`UseItemIntent`). O botão direito continua largando no chão, como nos outros itens.

**Barra de atalhos.**
- Arrastar uma poção do inventário para uma tecla de 1 a 6 cria um atalho para **aquele tipo** de consumível. O dado arrastado leva o id do item, o mesmo da tela de habilidades.
- O atalho mostra o total no inventário e a recarga. Com 0 unidades fica escurecido, e o tooltip diz "compre no Templo".
- O tipo do atalho é o do próprio id: id de item (`consumable.*`) é consumível, o resto é habilidade. Por isso o formato do `HotbarStore` não mudou, e barras já gravadas continuam valendo.

**A poção saiu da tela de habilidades e da barra padrão.**
- A barra padrão agora põe as habilidades da classe de 1 a 6 (`HotbarLayout.Defaults(abilities)`).
- **Q ficou**: usa a primeira poção de **vida** do inventário (`ItemText.IsHealingConsumable`) e só aparece na barra se a poção não estiver numa tecla.

**NPC no mapa.**
- `NpcView`: o sprite parado, de frente, e o nome em cima. NPC com loja tem o nome dourado com "· loja" e um anel no chão com o mouse em cima.
- Os sprites são **provisórios**, folhas que já temos (`sprites.json`, `npc.*`): Sacerdote = Clérigo, Ferreiro = Guerreiro, Instrutor = Arqueiro, Mercador = Salteador, Estalajadeira = Mago, Guarda do Armazém = Clérigo com outro tom.
- **Clique no NPC.**
  - Com loja, a até o alcance (3 tiles), abre a janela.
  - Longe, aparece **"Chegue mais perto"** sobre o personagem. Escolhi isso em vez de andar até ele: é mais simples e não precisa de caminho A* no cliente.
  - NPC sem loja diz que não tem nada para vender.
  - O clique no NPC não vira ataque.

**Janela da loja** (`ShopPanel`, nova).
- **Abas e cabeçalho.**
  - Título com o nome da loja e o NPC, abas Comprar e Vender, o ouro no rodapé e "Fechar (Esc)".
  - Abre o inventário junto e fica à esquerda dele. Se não couber entre o quadro do personagem e o inventário, desce para a coluna da esquerda.
- **Comprar.**
  - Cada oferta mostra ícone (tooltip completo), nome, tipo e nível, e o preço (em laranja quando falta ouro). Consumíveis têm a quantidade (1 a 20) e mostram "você tem N".
  - Equipamento que a classe não usa aparece apagado, com "sua classe não usa".
  - A lista rola: o Ferreiro tem 95 ofertas.
- **Vender.**
  - O inventário em linhas, com o preço por unidade na linha e no tooltip, e a quantidade para pilhas.
  - Raro e épico pedem um segundo clique ("Confirmar?", 3 s).
- **Quantidade:** só pelas setas. Digitar no campo apertaria também as teclas 1 a 6.
- **Mensagens:** compra e venda feitas aparecem em verde no rodapé da janela. Recusas aparecem como texto curto (`ItemText.ShopRejection`: "Ouro insuficiente", "Sem espaço no inventário", "Chegue mais perto"…).
- **Fecha** ao se afastar mais que o alcance + 1 tile, se o NPC some (troca de zona) ou com Esc.

**Roteiro de teste** (`--demo-input`):
- `wait:s`;
- `shop` (clica no NPC com loja mais perto);
- `buy:oferta;qtd`;
- `sell:espaço;qtd`;
- `use:espaço`.

**Teste contra o servidor local** (`--world --dev-login`, Guerreiro).
- **Mudança só no teste:** para ter ouro sem caçar, rodei com `sellRatio` 1,0 em `data/shops.json` (no servidor e no cliente). O valor voltou a 0,1 antes do commit.
- O personagem andou até o Templo e abriu a loja. Vendeu 3 poções (45 de ouro) e comprou 2 (30): a pilha e o ouro atualizaram no inventário, na janela e no log.
- Arrastou a poção para a tecla 1 e usou: a quantidade caiu de 4 para 3 e a recarga apareceu (17,8 s).
- No Ferreiro: a lista longa rola, e comprar sem ouro mostrou "Ouro insuficiente".
- Capturas em `tasks/reports/interface/`:
  - `t323-loja-comprar.png`: Templo, depois da compra;
  - `t323-barra-pocao.png`: poção na tecla 1, em recarga, e a aba Vender;
  - `t323-ferreiro.png`.

**Testes.** `HotbarLayoutTests` atualizado: barra padrão sem poção, e um atalho de consumível gravado e lido de volta. `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

**Desvios:** `tests/Game.Client.Tests/HotbarLayoutTests.cs` (fora de `client/`), por causa da barra padrão nova.

**Pendências (para o playtest do designer):**
- Não vi no jogo o "+N de ouro" ao matar um monstro: o roteiro não chega aos Campos.
- Não testei usar a poção numa luta de verdade.
- Não testei a confirmação ao vender item raro ou épico, porque o personagem de teste não tinha um.
- Os NPCs ainda não têm arte própria.
- A branch saiu de `main`, sem a T-089. As duas mexem em `InventoryPanel` (desenho do espaço de item e boneco): ao integrar, o git deve pedir resolução manual nesse arquivo.
