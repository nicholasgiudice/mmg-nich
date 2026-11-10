# T-097 — Interface da tela de habilidades no tema pedra e ouro

- **Ambiente:** Windows
- **Modelo:** Humano + Leve
- **Depende de:** T-081 (tela de habilidades), T-087 (ícones PixelLab), T-089 (moldura de janela)
- **Libera:** —
- **Áreas que pode alterar:** `client/scripts/Hud/` (`SkillPanel`, `ProgressionPanel`, `HudTheme`), `client/assets/art/ui/`, `CREDITS.md`, `tasks/reports/interface/`

## Contexto

O designer pediu em 2026-11-09 para levar a tela de habilidades (T-081) ao kit pedra e ouro. Os ícones já foram refeitos no mesmo estilo (T-087); falta a janela em volta deles.

## Requisitos

1. Peças no estilo do kit pedra e ouro para a tela de habilidades: a moldura de janela da T-089, a célula de cada habilidade (com ícone, nível e custo) e o que mais a tela pedir (botões de pontos, divisórias).
2. Pelo menos 5 opções geradas no PixelLab, no modo de maior qualidade disponível; o designer escolhe.
3. Prévia dentro do jogo, com a tela aberta, antes da aprovação final.
4. Arte em 1×; o texto continua do Godot. Sem os arquivos, a tela volta ao visual de antes.
5. Nenhuma regra muda: arrastar para a barra 1–6 e gastar pontos funcionam como antes.
6. Créditos registrados.

## Critérios de aceite

- prancha com as opções em `tasks/reports/interface/`;
- captura no jogo com a tela de habilidades aberta, aprovada pelo designer;
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
