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

**Feito** (Windows, 2026-11-09). Aprovado pelo designer (combinação sugerida, sem trocas).

**Opções.** Cinco chamadas de `create_image_pro`, todas com `panel.png` como referência de estilo: célula de habilidade (48×48, 25 candidatas, com `equip_slot.png` como referência), botão + (24×24, 64 candidatas, com `button_normal.png`), marcas de NH cheia e vazia (32×16, 64 pares) e duas de divisória (128×16, 4 candidatas cada). Pranchas em `tasks/reports/interface/habilidades-amostra/` (`prancha-celulas.png`, `prancha-mais.png`, `prancha-marcas.png`, `prancha-divisorias.png`). Escolhidas: **célula 14**, **botão + 36**, **marcas 9** (gema redonda e encaixe vazio) e **divisória 2** (pedra com pontas douradas).

**Peças** (`client/assets/art/ui/`), recortadas nas bordas transparentes e sem as sobras de candidatas vizinhas:
- `skill_cell.png` (45×46): 9-slice com margem de 10 px.
- `skill_plus.png` (23×23): o + em 1×, com variação de cor no hover, no clique e desabilitado.
- `skill_rank_on.png` e `skill_rank_off.png` (14×15): os dois lados da marca 9.
- `divider.png` (121×5): desenhada na maior escala inteira que cabe na largura da grade (3×).

**No cliente.**
- `HudTheme`: `UiTexture()`, `SkillCell()`, `StonePlusButton()` e `Divider()`; todas voltam ao visual de antes sem o arquivo.
- `SkillPanel`: liga a janela de pedra (`StoneWindow`) e a placa do título; a célula é de pedra, e a cor de destaque (azul do ataque básico, dourado da assinatura) virou um contorno fino por dentro; os pontos do NH são gemas no lugar de ●/○; o + é o botão de pedra; a divisória substitui o `HSeparator`; "Redistribuir pontos" usa o botão de pedra (`StoneButton`).
- Nenhuma regra mudou: o + manda o mesmo `RaiseSkillRankIntent`, e o ícone continua sendo arrastado para a barra.

**Capturas** (`tasks/reports/interface/`): `t097-before.png` (antes), `t097-preview.png` (servidor local `--world --dev-login`, personagem novo com 1 ponto livre e os +), `t097-preview-nh.png` e `t097-preview-nh-zoom.png` (encontro de teste, com as gemas cheias).

**Custo:** 50 gerações da assinatura.

**Decisões e pendências.**
- As células ficaram um pouco mais largas que antes (a borda de pedra soma à largura de 112 px), e a janela cresceu junto; o designer aprovou assim.
- Clicar no + e arrastar para a barra não foram testados à mão nesta sessão (só capturas); o código desses dois caminhos não mudou.
- O `PlusButton` da tela de atributos continua no estilo antigo; a T-098 pode reaproveitar `StonePlusButton()` e `Divider()`.
