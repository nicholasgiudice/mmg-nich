# T-074 — Tela de seleção de personagem e ícone do executável

- **Ambiente:** Windows
- **Modelo:** Humano + Leve
- **Depende de:** T-073 (logo), T-220 (contas e personagens), kit pedra e ouro (`tasks/reports/interface/kit-pedra-ouro.md`)
- **Libera:** —
- **Áreas que pode alterar:** `client/scripts/Account/`, `client/assets/art/ui/`, `client/project.godot` (só o ícone), `client/export_presets.cfg` (só o ícone), `tasks/reports/interface/`

## Contexto

O designer pediu em 2026-11-18 uma tela de seleção de personagem de verdade e o logo no ícone do executável. A tela de personagens era uma lista de texto (`ItemList`). O executável usava o ícone padrão do Godot, e a T-073 deixou como pendência que o logo é largo demais para virar ícone.

## Requisitos

1. Cartões lado a lado, um por personagem, com o sprite da classe, o nome, a classe, o nível e a zona. O selecionado ganha borda dourada. O cartão de criar personagem fica no fim da fileira. Os botões ficam embaixo. Este layout foi escolhido pelo designer entre "cartões" e "lista + prévia grande".
2. Os cartões usam o kit pedra e ouro.
3. Ícone quadrado no estilo do logo, gerado no PixelLab, escolhido pelo designer numa prancha que mostra cada opção nos tamanhos reais.
4. O ícone vale para o `.exe` exportado e para a janela do jogo.

## Critérios de aceite

- captura da tela de seleção no jogo, aprovada pelo designer;
- prancha com as opções de ícone em `tasks/reports/interface/`;
- `Valdran.exe` exportado com o ícone escolhido;
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Resultado

**Feito** (Windows, 2026-11-18). O designer aprovou a tela de seleção (`t074-selecao-3.png`) e escolheu o ícone **B3**.

**Seleção de personagem** (`AccountScreen.ShowCharacters` e o novo `CharacterCard`):
- Cada cartão tem 150 px de largura e usa a moldura `skill_cell.png` (9-slice). Dentro, o nicho de pedra `doll_niche.png` em 1×, com o sprite da classe em 2× e olhando para baixo. Embaixo: o nome (branco, dourado quando selecionado), a classe (dourado), "Nível N" e a zona. Os textos longos terminam em reticências.
- O selecionado ganha uma borda dourada de 3 px e anda; os outros ficam parados no primeiro quadro. Passar o mouse mostra uma borda clara.
- O cartão de criar personagem tem o nicho esmaecido com o `skill_plus.png` em 2× e abre a tela de criação. Por isso o botão "Criar personagem" saiu da fileira de botões.
- Comandos: clique seleciona, clique duplo joga. Setas trocam a seleção e Enter joga. Os botões ficam embaixo: Jogar, Apagar e, à direita, Sair da conta.
- O `SpritePreview` ganhou zoom, fundo (`Backdrop`) e animação que liga e desliga. A tela de criação continua igual (zoom 3, fundo verde).
- Com o limite de 5 personagens por conta, o painel mede cerca de 1000 px e cabe na janela de 1280×720 (`t074-selecao-5.png`).
- Prévia sem conta: o argumento `-- --account-sample[=n]` abre a seleção com n personagens de exemplo (padrão 3, de 0 a 5), um de cada classe e com zonas reais, fora a de teste. Jogar, criar e apagar só mostram um aviso. Serve para capturas, porque a API usa Postgres e não roda local.

**Ícone:**
- Duas chamadas `create_image_pro` em 64×64, cada uma com 16 candidatas, usando o logo da T-073 como imagem de estilo. A: "V" dourado com espada. B: escudo de pedra com "V" e espada. As pranchas `icone-amostra/prancha-a.png` e `prancha-b.png` mostram cada candidata em 3×, 32 px e 16 px. As candidatas brutas estão em `icone-amostra/a/` e `b/`, numeradas a partir de 0, então a B3 é o arquivo `b/2.png`.
- Fonte: `tasks/reports/interface/icone-amostra/b/2.png`, 64×64, com transparência só 0 ou 255, sem retoque (job `31cdcd21-7978-4f6e-ae77-7e0a1bec62d3`, asset `331855f8-d11e-5155-a250-d954d786a7fe`).
- `client/assets/art/ui/icon.ico` reúne os tamanhos 256, 128 e 64 ampliados sem filtro, e os tamanhos 48, 32 e 16 reduzidos com Lanczos. `icon.png` é o de 256×256.
- `project.godot`: `config/icon` (ícone da janela) e `config/windows_native_icon`. `export_presets.cfg`: `application/icon` e `console_wrapper_icon` apontam para o `.ico`, com `icon_interpolation=0` (sem filtro).
- Conferido: o ícone extraído do `build\windows\Valdran.exe` exportado é o escudo.

**Fora das áreas listadas:** o crédito do ícone entrou no `CREDITS.md`, como fez a T-073 com o logo, e a T-074 entrou no índice do `tasks/README.md`.

**Custo:** 20 gerações da assinatura (2 chamadas Pro de 10).

**Pendências:**
- A seleção não foi testada com uma conta real. Na captura, usei os personagens de exemplo; Jogar, Apagar e o cartão de criar passam pelo mesmo código de antes.
- O ícone da janela (barra de tarefas) com o jogo aberto não foi conferido a olho; só o do `.exe`.
- O cliente publicado no download ainda não tem esta tarefa: falta um export e um `deploy-download.ps1` depois da integração.
- Os botões Jogar, Apagar e Sair continuam com o tema padrão, como os da tela de login. Dá para passar para o `HudTheme.StoneButton` numa tarefa à parte.
