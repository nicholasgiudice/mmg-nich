# T-088 — Imagem de fundo da tela de login (PixelLab)

- **Ambiente:** Windows
- **Modelo:** Humano + Leve
- **Depende de:** T-220 (tela de login), kit pedra e ouro (`tasks/reports/interface/kit-pedra-ouro.md`)
- **Libera:** —
- **Áreas que pode alterar:** `client/scripts/Account/`, `client/assets/art/ui/`, `CREDITS.md`, `tasks/reports/interface/`

## Contexto

O designer pediu em 2026-11-09 uma imagem nova para a tela de login, que hoje é só um fundo escuro liso (`AccountScreen`). A imagem é arte parada, o ponto forte do PixelLab (o PixelLab foi reprovado só para personagens animados, piloto T-090).

## Requisitos

1. Cena 2D **vista de lado** (não de cima): personagens das classes do jogo de costas ou de perfil, olhando para um castelo bem distante ao fundo.
2. As classes que aparecem são escolhidas pelo designer antes de gerar (o jogo tem quatro: Guerreiro, Arqueiro, Mago e Clérigo, `design/classes-v0.1.md`).
3. Arte simples, com pouco detalhe.
4. Pelo menos 5 opções geradas no PixelLab, no modo de maior qualidade disponível; o designer escolhe uma.
5. A imagem cobre a tela de login (e as de personagens e criação), sem distorcer, com o formulário legível por cima. Sem o arquivo, a tela volta ao fundo liso.
6. Créditos registrados.

## Critérios de aceite

- prancha com as opções em `tasks/reports/interface/`;
- captura da tela de login no jogo com a imagem escolhida, aprovada pelo designer;
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
