# T-087 — Ícones das habilidades refeitos no PixelLab

- **Ambiente:** Windows
- **Modelo:** Leve
- **Depende de:** T-080 (folha de ícones), T-086 (tema pedra e ouro)
- **Libera:** —
- **Áreas que pode alterar:** `client/assets/art/icons/`, `CREDITS.md`, `tasks/reports/interface/`

## Contexto

O designer pediu em 2026-11-06 para refazer os ícones de todas as habilidades. Os da T-080 vinham de um pacote genérico, e alguns não combinavam com a habilidade (o Projétil Arcano usava um machado).

## Requisitos

1. Um ícone novo para cada uma das 25 habilidades de classe, no estilo do kit pedra e ouro.
2. Amostra aprovada pelo designer antes de gerar todos.
3. Ícones de itens, atributos e a poção (que vira item na T-320) continuam como estão.
4. Créditos registrados.

## Critérios de aceite

- capturas da barra e da tela de habilidades de cada classe;
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Resultado

**Feito** (Windows, 2026-11-06).

**Como chegamos ao estilo.** Duas amostras com `create_image_pixflux` em 64 px (uma escura, outra colorida) foram reprovadas. A aprovada usa `create_image_pro` em 32 px, com o painel pedra e ouro como referência de estilo (contorno, sombra e detalhe): ícones clássicos de RPG, fundo de ardósia e moldura fina dourada. Cada chamada devolve 64 candidatos; escolhi um por habilidade, e o designer trocou a Chuva de Flechas pelo candidato #10. Prancha final em `tasks/reports/interface/icones-amostra/escolhidos.png`; folhas de candidatos em `icones-amostra/pro/<id>-folha.png`; escolhas em `icones-amostra/pro/escolhas.json`.

**No cliente.** Os 25 ícones entraram em linhas novas no fim de `icons.png` (272×374), cada um com 1 px de margem dentro da célula de 34 px, e as entradas de `icons.json` apontam para eles (`source` = `pixellab:<id>#n`). Nenhum código mudou. Os ícones antigos dessas habilidades ficam na folha, sem uso.

**Capturas:** `tasks/reports/interface/t087-warrior.png`, `t087-archer.png`, `t087-mage.png`, `t087-cleric.png` (tela de habilidades aberta) e `t087-barras.png` (barra das 4 classes).

**Custo:** cerca de 290 gerações da assinatura (amostras reprovadas, 25 folhas Pro, a Marca do Caçador refeita porque a primeira folha veio desalinhada do PixelLab, e uma rodada extra da Chuva de Flechas que não foi usada).

**Pendências:** os ícones de itens e atributos continuam do pacote da T-080; a poção ganha ícone próprio quando virar item (T-320/T-323).
