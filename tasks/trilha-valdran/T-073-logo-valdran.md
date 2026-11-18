# T-073 — Logo do Valdran (PixelLab)

- **Ambiente:** Windows
- **Modelo:** Humano + Leve
- **Depende de:** T-070 (nome Valdran), T-088 (tela de login), kit pedra e ouro (`tasks/reports/interface/kit-pedra-ouro.md`)
- **Libera:** —
- **Áreas que pode alterar:** `client/scripts/Account/`, `client/assets/art/ui/`, `src/Game.Site/` (só a arte de interface e o `style.css`), `CREDITS.md`, `tasks/reports/interface/`

## Contexto

O designer pediu em 2026-11-17 um logo para o jogo, feito no PixelLab. Até agora o nome aparecia só em texto: um rótulo pequeno "Valdran" no painel de login e o título do site.

## Requisitos

1. Logo em pixel art com o nome "Valdran", num estilo medieval que combine com o kit pedra e ouro.
2. Arte simples, com pouco detalhe.
3. De 2 a 4 opções geradas no PixelLab. O designer escolhe uma, vendo-as como prévia dentro do jogo.
4. O logo aparece no cliente, pelo menos na tela de login, em pixel inteiro e sem borrão. Sem o arquivo, a tela volta ao nome em texto.
5. O logo fica salvo no repositório com o arquivo-fonte e os tamanhos usados. Créditos registrados.

## Critérios de aceite

- prancha com as opções em `tasks/reports/interface/`;
- captura da tela de login no jogo com o logo escolhido, aprovada pelo designer;
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Resultado

**Feito** (Windows, 2026-11-17). O designer escolheu a opção 2.

**Opções.** Gerei as opções com `create_image_pro` em 192×64, com fundo transparente e o painel do kit (`panel.png`) como referência de estilo. Cada chamada custou 10 gerações e deu 1 candidata. Em todas, "VALDRAN" saiu escrito certo.
- Rodada 1: 1 pedra entalhada, 2 ouro com espada, 3 ouro simples, 4 gótico com coroa. Prancha em `tasks/reports/interface/logo-amostra/rodada-1/prancha.png`.
- O designer não gostou e pediu outras. Rodada 2: 5 faixa vermelha, 6 brasão e letras claras, 7 castelo ao pôr do sol (com o fundo de login como referência), 8 ferro rebitado. Prancha em `logo-amostra/prancha-2.png`.
- Depois de ver a rodada 2, o designer escolheu a **opção 2 (ouro com espada)**, da rodada 1.
- As pranchas são recortes de capturas reais do cliente, com cada opção posta no lugar do logo. Cada captura inteira está em `previa-*.png`.

**Arquivos e tamanhos.**
- Fonte: `tasks/reports/interface/logo-amostra/rodada-1/opcao-2-espada.png`, 192×64, como o PixelLab entregou (asset `29a020ea-4b99-514b-92a6-c35e7dd5dc7d`, job `6d4ae78b-548c-4736-a1ca-14e797db9592`). A transparência é só 0 ou 255. O lint do `pixelart_workbench` não acusou erro, então a imagem não foi retocada.
- Cliente: `client/assets/art/ui/logo.png`, 188×43, que é a fonte recortada nas bordas transparentes. É desenhado em **2×** (376×86 na janela de 1280×720), com filtro `Nearest`.
- Site: o mesmo `logo.png`, copiado para `assets/ui/` pelo `UiArt` e mostrado em **3×** (564×129) no topo da página inicial. Em telas estreitas ele encolhe para caber.

**No cliente** (`AccountScreen`):
- O painel de pedra e o logo ficam numa coluna (`VBoxContainer`), na mesma âncora de antes (40% da largura, 24 px do topo). O logo fica em cima, centralizado, com 12 px de espaço até o painel.
- Com o logo presente, o `Clear()` deixa de pôr o rótulo pequeno "Valdran" no painel. Sem o arquivo, o rótulo volta e o painel fica como antes.
- Vale para as telas de login, personagens e criação, que usam a mesma coluna.

**No site:** `logo.png` entrou em `UiArt.Files`. No `style.css`, o `.hero h1` da página inicial mostra o logo como fundo. O texto "Valdran" continua no HTML, transparente, para leitores de tela e busca. O teste `Interface_art_from_the_game_is_copied_and_used` já cobre a cópia e o uso no CSS. A marca do cabeçalho (coroa + texto) não mudou.

**Capturas:** `tasks/reports/interface/t073-login.png` (cliente, 1920×1080) e `tasks/reports/interface/t073-site.png` (página inicial, 1280×800).

**Custo:** 80 gerações da assinatura (8 chamadas Pro de 10).

**Desvios:** a branch é `task/T-073-logo-valdran`. O pedido original sugeria `task/logo-valdran`, mas usei o padrão `task/T-XXX-nome-curto` do `AGENTS.md`. O site (`src/Game.Site/`) não estava no pedido original, mas a tarefa citava o site e a mudança é só de arte e CSS.

**Pendências:**
- As telas de personagens e de criação não foram capturadas (exigem entrar com uma conta). Elas são mais largas e o logo fica acima delas, então a tela fica mais alta.
- O ícone do app (`config/icon` do `project.godot`) continua o padrão do Godot: o logo é largo demais para virar ícone.
- O site em `valdran.nich.click` só mostra o logo depois de uma nova publicação (`./scripts/build-site.sh --publish`, na VPS).
