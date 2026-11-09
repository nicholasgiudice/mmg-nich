# T-089 — Interface da tela de equipamentos no tema pedra e ouro

- **Ambiente:** Windows
- **Modelo:** Humano + Leve
- **Depende de:** T-083 (inventário com boneco), T-086 (tema pedra e ouro)
- **Libera:** T-097, T-098, T-099 (a moldura de janela feita aqui serve às outras telas)
- **Áreas que pode alterar:** `client/scripts/Hud/` (`InventoryPanel`, `ProgressionPanel`, `HudTheme`), `client/assets/art/ui/`, `CREDITS.md`, `tasks/reports/interface/`

## Contexto

O designer pediu em 2026-11-09 para levar as janelas do personagem ao kit pedra e ouro, aprovado em 2026-11-06 (T-086 e T-087). Hoje o boneco e a mochila ficam na mesma janela (`InventoryPanel`, T-083), desenhada por código. Esta tarefa cuida da parte do equipamento: a moldura da janela, o fundo do boneco e os espaços de equipamento. A mochila fica para a T-099.

## Requisitos

1. Peças novas no estilo do kit pedra e ouro: moldura de janela (com faixa de título), fundo do boneco e espaço de equipamento (vazio e com item).
2. Pelo menos 5 opções geradas no PixelLab, no modo de maior qualidade disponível, com o kit atual como referência de estilo; o designer escolhe.
3. Prévia dentro do jogo, com a janela aberta, antes da aprovação final.
4. Arte em 1×, 9-slice quando for moldura; o texto continua do Godot. Sem os arquivos, a janela volta ao visual de antes.
5. Nenhuma regra muda: equipar, desequipar e largar funcionam como na T-083.
6. Créditos registrados.

## Critérios de aceite

- prancha com as opções em `tasks/reports/interface/`;
- captura no jogo com a janela de equipamentos aberta, aprovada pelo designer;
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Resultado

**Feito** (Windows, 2026-11-09). Aprovado pelo designer.

**Opções.** Quatro chamadas de `create_image_pro`, todas com `panel.png` como referência de estilo: duas de moldura de janela com placa de título (160×128, 4 candidatas cada), uma de nicho para o boneco (128×160, 4 candidatas) e uma de espaço de equipamento (44×44, 25 candidatas). Pranchas em `tasks/reports/interface/equip-amostra/` (`prancha-janelas.png`, `prancha-nichos.png`, `prancha-espacos.png`). O designer escolheu **janela 5** (`janela-b-1.png`), **nicho 1** e **espaço 3**.

**Peças** (`client/assets/art/ui/`):
- `window_frame.png` (153×115): a janela 5 sem a placa; a borda de cima embaixo da placa foi refeita repetindo um trecho da própria borda. 9-slice com margem de 18 px.
- `window_title.png` (68×20): a placa, recortada da mesma imagem. 9-slice com margem de 6 px; estica com o texto.
- `doll_niche.png` (108×140): o nicho 1, recortado; desenhado em 2× atrás da figura (que está em 3×).
- `equip_slot.png` (37×39): o espaço 3, recortado. 9-slice com borda de 9 px.

**No cliente.**
- `HudTheme`: `WindowFrame()`, `WindowTitlePlate()`, `EquipSlot()` e `DollNiche()`; todas devolvem null sem o arquivo.
- `ProgressionPanel`: `StoneWindow` (virtual, falso por padrão) troca o painel escuro pela moldura de pedra, e `WindowTitle(texto)` põe o título na placa, por cima da borda de cima. Só o `InventoryPanel` liga a moldura agora; as T-097 e T-098 ligam a das telas de habilidades e de atributos.
- `InventoryPanel`: nicho atrás do boneco; espaços do boneco de 50 px (borda de pedra de 9 px em volta do ícone de 32 px, em 1×), com a raridade num contorno fino por dentro da pedra e o nível no canto.
- **Ajuste pedido pelo designer:** o espaço da armadura cobria o rosto do boneco. Ele foi para a coluna da direita (armadura em cima, acessório embaixo), espelhando mãos e arma à esquerda, e a figura subiu 24 px para os pés não ficarem sob o espaço das botas.
- Sem as peças, a janela volta ao painel escuro e aos quadrados de antes. Equipar, desequipar e largar não mudaram.

**Captura:** `tasks/reports/interface/t089-local.png` (partida local, Guerreiro, tela cheia).

**Custo:** 55 gerações da assinatura.

**Pendências:** a grade da mochila, o ouro e o resumo de dano/armadura continuam no estilo antigo (T-099). Branch feita a partir de `docs/tarefas-interface-telas`, não da T-088: ao integrar as duas, o `CREDITS.md` pode pedir resolução manual (as duas acrescentam uma linha no mesmo ponto da tabela).
