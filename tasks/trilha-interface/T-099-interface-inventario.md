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

_(preencher ao concluir)_
