# T-098 — Interface da tela de atributos no tema pedra e ouro

- **Ambiente:** Windows
- **Modelo:** Humano + Leve
- **Depende de:** T-082 (tela de atributos), T-089 (moldura de janela)
- **Libera:** —
- **Áreas que pode alterar:** `client/scripts/Hud/` (`CharacterPanel`, `ProgressionPanel`, `HudTheme`), `client/assets/art/ui/`, `CREDITS.md`, `tasks/reports/interface/`

## Contexto

O designer pediu em 2026-11-09 para levar a tela de atributos (T-082, janela Personagem) ao kit pedra e ouro.

## Requisitos

1. Peças no estilo do kit pedra e ouro para a tela de atributos: a moldura de janela da T-089, a linha de cada atributo e o botão de somar ponto.
2. Pelo menos 5 opções geradas no PixelLab, no modo de maior qualidade disponível; o designer escolhe.
3. Prévia dentro do jogo, com a tela aberta, antes da aprovação final.
4. Arte em 1×; o texto continua do Godot. Sem os arquivos, a tela volta ao visual de antes.
5. Nenhuma regra muda: distribuir pontos funciona como antes.
6. Créditos registrados.

## Critérios de aceite

- prancha com as opções em `tasks/reports/interface/`;
- captura no jogo com a tela de atributos aberta (com e sem pontos livres), aprovada pelo designer;
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
