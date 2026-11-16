# T-099 — Interface do inventário (mochila) no tema pedra e ouro

- **Ambiente:** Windows
- **Modelo:** Humano + Leve
- **Depende de:** T-083 (inventário com boneco), T-089 (moldura de janela e espaço de equipamento), T-320 (consumíveis empilháveis e ouro)
- **Libera:** —
- **Áreas que pode alterar:** `client/scripts/Hud/` (`InventoryPanel`, `ItemText`, `HudTheme`), `client/assets/art/ui/`, `CREDITS.md`, `tasks/reports/interface/`

## Contexto

O designer pediu em 2026-11-09 para levar o inventário ao kit pedra e ouro. A T-089 cuida do boneco e dos espaços de equipamento; esta tarefa cuida da mochila: a grade de espaços, a quantidade dos itens empilháveis e o ouro (T-320).

## Requisitos

1. Peças no estilo do kit pedra e ouro para a mochila: espaço da grade (vazio e com item, combinando com a moldura de raridade) e o mostrador de ouro.
2. Pelo menos 5 opções geradas no PixelLab, no modo de maior qualidade disponível; o designer escolhe.
3. Prévia dentro do jogo, com o inventário aberto e cheio, antes da aprovação final.
4. Arte em 1×; o texto continua do Godot. Sem os arquivos, a janela volta ao visual de antes.
5. Nenhuma regra muda: arrastar, duplo clique e largar no chão funcionam como antes.
6. Créditos registrados.

## Critérios de aceite

- prancha com as opções em `tasks/reports/interface/`;
- captura no jogo com o inventário aberto, aprovada pelo designer;
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Resultado

**Feito** (Windows, 2026-11-16). Aprovado pelo designer (opção 3, sem trocas).

**Opções.** Quatro chamadas de `create_image_pro`, todas com `panel.png` como referência de estilo (`style_copy` outline, detail, shading):
- espaço da grade simples (44×44, 25 candidatas);
- espaço da grade com cantos dourados (44×44, 25 candidatas, com `equip_slot.png` como referência);
- placa do ouro (112×24, 4 candidatas, com `window_title.png` como referência);
- moeda e plaquinha da quantidade lado a lado (32×16, 64 pares).

Pranchas das candidatas em `tasks/reports/interface/inventario-amostra/`: `prancha-espacos-a.png`, `prancha-espacos-b.png`, `prancha-placas.png` e `prancha-moedas.png`. Seis combinações foram mostradas no jogo, com o inventário aberto e cheio (25/30 itens de todas as raridades, poções empilhadas e 12.480 de ouro). As pranchas são `prancha-opcoes.png` (a janela inteira) e `prancha-detalhe.png` (a mochila em 2×, com o "antes"). O designer escolheu a **opção 3**: espaço B1, placa 1 e par 30.

**Peças** (`client/assets/art/ui/`), recortadas nas bordas transparentes:
- `bag_slot.png` (43×43): espaço de pedra lisa com cantos dourados, irmão do `equip_slot` do boneco. É um 9-slice com margem de 10 px, desenhado em 44 px; o ícone de 32 px fica em 1×, a 6 px da borda.
- `bag_count.png` (13×13): plaquinha escura com aro dourado atrás da quantidade. É um 9-slice com margem de 4 px que alarga com o número ("12").
- `bag_gold.png` (106×24): placa de pedra clara com cantos dourados, desenhada no tamanho natural (não encolhe).
- `bag_coin.png` (13×15): a moeda, separada da mesma candidata da plaquinha.

**No cliente.**
- `HudTheme`: só acrescentei `BagSlot()`, `BagCountTag()`, `BagGoldPlate()` e `BagCoin()`, logo depois de `Crown()`. Todas devolvem null sem o arquivo.
- `InventoryPanel`:
  - a grade usa o espaço de pedra (44 px, 2 px entre espaços);
  - a raridade virou um contorno fino por dentro da pedra, com o fundo tingido, como no boneco (T-089). O nível continua no canto, e o X dos itens que a classe não usa também;
  - os consumíveis mostram a quantidade numa plaquinha no canto de baixo à direita;
  - o ouro virou a placa com a moeda e só o número ("12.480"); o tooltip explica o que é.
- `ItemSlotButton` ganhou `Inset` (distância até o ícone) e `CountTag`. Os espaços do boneco não mudaram.
- `ItemText`: `GoldAmount()` (só o número); `Gold()` passou a usá-lo e devolve o mesmo texto de antes.
- O texto continua do Godot. Sem os arquivos, a grade volta aos quadrados de 42 px e o ouro, à linha "N de ouro" com a moeda desenhada.

**Regras de jogo:** nenhuma mudou. No teste da captura final (`t099-final-acoes.png`), o duplo clique equipou o machado (a espada voltou para a mochila), o botão direito largou a pilha de poções de mana e arrastar o elmo para fora da janela também o largou (25 → 23/30).

**Capturas** (`tasks/reports/interface/`):
- `t099-before.png` (antes);
- `t099-final.png` (inventário cheio com a arte final);
- `t099-final-acoes.png` (depois de equipar e largar).

Todas foram feitas com servidor local (`--world --dev-login`, porta 7792) e cliente em 1920×1080.

**Custo:** 40 gerações da assinatura.

**Decisões e observações.**
- Para encher a mochila, um ajuste provisório no inventário inicial (`EntityFactory`) e no ouro (`CharacterState`), ligado por variável de ambiente, foi desfeito antes do commit. A escolha da opção também usou uma variável de ambiente provisória em `HudTheme`, já removida.
- A grade ficou 12 px mais larga (6 × 44 + 5 × 2 contra 6 × 42 + 5 × 3), e a janela cresceu junto.
- As peças são desenhadas em 1× nas unidades da interface. Em 1920×1080, o Godot amplia a interface toda em 1,5×, como já acontece com as peças das T-089 e T-097.
- O `ShopPanel` continua com a moeda desenhada e a linha de ouro antigas (fora das áreas desta tarefa). Pode reaproveitar `BagGoldPlate()`/`BagCoin()` numa tarefa própria.
- A importação do Godot nesta worktree gerou `BossPanel.cs.uid` e `InstanceText.cs.uid`, que não são desta tarefa e ficaram fora do commit.
