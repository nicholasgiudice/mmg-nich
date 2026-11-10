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

**Feito** (Windows, 2026-11-09). Aprovado pelo designer.

**Escolha.** O designer pediu as 4 classes e um entardecer dourado. Gerei 9 opções com `create_image_pro` em 640×360 (no jogo, 2× exato em 1280×720), uma por chamada, com fundo opaco. Seis vieram com uma borda lisa em volta da cena (a cena não ocupava a tela); as opções 4, 6 e 8 preenchem a tela toda. O designer escolheu a **opção 8**: as quatro classes de costas num penhasco, à direita, e o castelo distante contra o sol, à esquerda. Prancha em `tasks/reports/interface/login-amostra/prancha.png`; cada opção em `login-amostra/opcao-N.png`.

**No cliente.**
- `client/assets/art/ui/login_background.png` (a opção 8, sem mudanças).
- `AccountScreen.AddBackground()`: `TextureRect` com `KeepAspectCovered` e filtro `Nearest`, por cima do fundo liso de antes, que fica como reserva quando falta o arquivo. Vale para as telas de login, personagens e criação.
- O formulário fica num painel de pedra (`HudTheme.StonePanel()`), ancorado a 40% da largura e 24 px do topo, entre o castelo e o grupo.
- A largura mínima do formulário caiu de 700 para 420 px (o painel largo cobria a cabeça dos personagens); as dicas e a mensagem de status quebram linha pela largura do painel. A lista de personagens e a tela de criação continuam com 700 px e alargam o painel.

**Captura:** `tasks/reports/interface/t088-login.png` (tela cheia, 1920×1080).

**Custo:** 225 gerações da assinatura (9 chamadas Pro de 25).

**Pendências:** as telas de personagens e de criação, mais largas, cobrem parte do castelo ou do grupo; não foram capturadas (exigem entrar com uma conta). Os botões e campos ainda são os padrões do Godot, não o kit pedra e ouro.
