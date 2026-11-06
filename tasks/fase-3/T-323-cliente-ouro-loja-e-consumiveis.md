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

_(preencher ao concluir)_
