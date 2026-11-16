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

**Feito** (Windows, 2026-11-16). Aprovado pelo designer.

**Opções.** Quatro chamadas de `create_image_pro`, todas com `panel.png` como referência de estilo (`style_copy` outline, detail, shading):
- linha com encaixe de ícone (128×40, 4 candidatas);
- faixa de pedra (128×32, 4 candidatas);
- placa de pedra (64×24, 36 candidatas);
- botão quadrado sem símbolo (32×32, 64 candidatas, com `button_normal.png` como referência).

Delas saíram seis combinações de linha e botão, mostradas no jogo com a janela aberta, 10 pontos livres e o mouse no +5 de Agilidade. Estão em `tasks/reports/interface/atributos-amostra/`:
- `prancha-opcoes.png`: as seis no jogo;
- `opcao-1.png` … `opcao-6.png`: cada uma em 1×;
- `prancha-pecas.png`: as peças de cada opção em 3×;
- `prancha-linhas.png`, `prancha-placas.png` e `prancha-botoes.png`: todas as candidatas.

O designer escolheu a **opção 1** (encaixe com borda dourada, faixa de pedra cinza e botão 4) e pediu dois ajustes: tirar a moldura dourada do atributo sugerido da classe e tirar a divisória acima de "Redistribuir pontos".

**Peças** (`client/assets/art/ui/`), recortadas nas bordas transparentes:
- `attr_socket.png` (28×32): o encaixe do ícone, cortado da linha na divisória de pedra. É um 9-slice que estica só o fundo escuro, com o ícone de 34 px no meio.
- `attr_row.png` (94×32): o resto da linha, a partir da mesma divisória. 9-slice com bordas diferentes em cada lado (4, 7, 7, 8).
- `attr_add.png` (27×27): o botão de somar. 9-slice de 8 px; o texto "+1"/"+5" é do Godot, dourado com contorno.

**No cliente.**
- `HudTheme` só ganhou membros: `AttributeRow()`, `AttributeSocket()`, `StoneAddButton()` e o auxiliar privado `UiPiece9()` (9-slice com bordas diferentes em cada lado). Todos devolvem null ou deixam o botão como antes quando falta o arquivo.
- `CharacterPanel`:
  - liga a janela de pedra (`StoneWindow`) e a placa do título;
  - cada linha é o encaixe com o ícone, colado na faixa de pedra;
  - os +1/+5 são o botão de pedra (mais claro no hover, mais escuro no clique);
  - "Redistribuir pontos" usa o botão de pedra (`StoneButton`);
  - sem a divisória acima do botão;
  - o atributo sugerido fica marcado só pelo ★ e pelo nome e "sugerido para a classe" em dourado.
- **Sem os arquivos `attr_*`:** a linha volta ao fundo escuro com a borda dourada no sugerido, os botões voltam ao tema padrão e a linha acima do botão volta. A moldura da janela é a da T-089 e depende só de `window_frame.png`. Testado no jogo, com as peças tiradas da pasta: `t098-sem-arte.png`.
- Nenhuma regra mudou: os botões mandam os mesmos `SpendAttributePointIntent`.

**Verificação no jogo.** Servidor local `--world --dev-login --port 7791 --start-zone zone.vigil_fields --data <cópia>`. A cópia muda só o teste e ficou fora do repositório: `xpCurveBase` 1, um Rato de Campo com 12 de vida perto do nascimento e renascimento de 2 s. O mesmo foi copiado para `client/data`, que o build restaura.

O Guerreiro mata o rato e sobe para o nível 3, com 10 pontos. Com o mouse no +5 de AGI, crítico e velocidade aparecem em verde. Dois cliques no +5 de VIT levam a VIT 20, vida 368 e 0 pontos, e as linhas ficam sem botões.

**Capturas** (`tasks/reports/interface/`):
- `t098-pontos.png`: 10 pontos livres;
- `t098-sem-pontos.png`: depois dos dois +5;
- `t098-sem-arte.png`: sem as peças `attr_*`.

**Custo:** 40 gerações da assinatura.

**Verificação final.** `dotnet build client/` sem avisos. No `./scripts/check.sh` completo, a formatação, o build e todos os testes passaram, inclusive os 229 de `Game.Design.Tests`, menos um. O teste do servidor `WorldTests.Buying_saves_gold_and_inventory_right_away` esgotou o tempo de carregamento uma vez, com a máquina ocupada pelos testes de design. Rodado de novo, `Game.Server.Tests` passou inteiro (74 de 74). Esta tarefa não mexe no servidor.

**Decisões e pendências.**
- A linha gerada tem o encaixe e a faixa numa só imagem. Ela foi cortada em duas peças para o encaixe crescer até caber o ícone de 34 px em 1×, já que o encaixe original tinha uns 16 px por dentro.
- As linhas ficaram um pouco mais altas que antes (46 px contra 40). A janela ainda cabe acima da barra de ações em 1920×1080.
- Integração: a T-099 (inventário, em paralelo) também acrescenta membros ao `HudTheme` e uma linha ao `CREDITS.md`. Os dois arquivos podem pedir resolução manual, mas basta manter as duas partes.
