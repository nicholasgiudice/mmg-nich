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

_(preencher ao concluir)_
