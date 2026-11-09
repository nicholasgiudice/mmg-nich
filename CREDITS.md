# Créditos

Arte de terceiros usada no jogo, com a atribuição que cada licença exige. A escolha dos pacotes e as regras de licença estão em `design/art-direction-v0.1.md` (§7).

## Como adicionar créditos

1. Antes de incluir um asset novo no repositório, confira a licença **na página do pacote** e escolha a mais permissiva oferecida. GPL sozinha só entra com decisão do designer.
2. Guarde o arquivo de créditos original ao lado da imagem (`Credits.txt`, `credits.txt` ou o `.csv`/`.txt` exportado pelo gerador LPC).
3. Acrescente aqui, no mesmo commit, uma entrada com: nome do asset, autor(es), licença adotada (com link), página de origem, onde o arquivo está em `client/assets/art/` e se foi modificado (recolorido, recortado, redimensionado).
4. Asset derivado de um arquivo CC-BY-SA continua CC-BY-SA. Registre isso na entrada.
5. Nenhum asset CC-BY ou CC-BY-SA pode ir para um build com proteção técnica (criptografia do `.pck`).

Licenças:
[CC-BY 3.0](https://creativecommons.org/licenses/by/3.0/) ·
[CC-BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/) ·
[OGA-BY 3.0](https://static.opengameart.org/OGA-BY-3.0.txt) ·
[CC0](https://creativecommons.org/publicdomain/zero/1.0/)

## Assets incluídos

| Asset | Autor(es) | Licença | Origem | Onde está | Modificado |
|---|---|---|---|---|---|
| Rato gigante | Sevarihk; adaptado por tapatilorenzo | [CC-BY 4.0](https://creativecommons.org/licenses/by/4.0/) | https://opengameart.org/content/lpc-bears-deer-lions-and-more | `client/assets/art/monsters/field_rat/` | não |
| Lobo (`wolfsheet1`) | Stephen Challener (Redshrike), publicado por William.Thompsonj | OGA-BY 3.0 | https://opengameart.org/content/lpc-wolf-animation | `client/assets/art/monsters/grey_wolf/` | recolorido em cinza claro |
| Tileset de terreno de verão (`terrain_summer`) | Eliza Wyatt (ElizaWy), Lanea Zimmerman (Sharm), Stephen Challener (Redshrike) e outros do LPC Revised; configurado para o Tiled por JaidynReiman | OGA-BY 3.0 | https://opengameart.org/content/lpc-revised-fully-configured-4-seasons-tilesets-for-tiled-map-editor | `client/assets/art/tiles/` (`.tsx` em `data/maps/tilesets/`) | não |
| Tileset de construções (`buildings`): muros de tijolo, telhados, janelas, portas e placas | Eliza Wyatt (ElizaWy), Lanea Zimmerman (Sharm), Stephen Challener (Redshrike) e outros do LPC Revised; configurado para o Tiled por JaidynReiman | OGA-BY 3.0 | https://opengameart.org/content/lpc-revised-fully-configured-4-seasons-tilesets-for-tiled-map-editor | `client/assets/art/tiles/buildings.png` (`.tsx` em `data/maps/tilesets/`) | não |
| Aranhas (`spider01`, `spider04`, `spider05`) | Stephen Challener (Redshrike), publicado por William.Thompsonj | OGA-BY 3.0 | https://opengameart.org/content/lpc-spider | `client/assets/art/monsters/spiders/` | não |
| Guerreiro, Arqueiro, Mago, Clérigo, Salteador, Guarda Esquelético | vários artistas do LPC, peça a peça em `<personagem>-credits.txt` | por peça: OGA-BY 3.0 quando oferecida; CC-BY-SA 3.0 em peças do Guerreiro (corpo musculoso, cabelo, escudo), do Arqueiro (cabelo, arco) e do Mago (cabelo, sobrecasaca) | https://github.com/LiberatedPixelCup/Universal-LPC-Spritesheet-Character-Generator | `client/assets/art/characters/` | não (folhas exportadas do gerador, combinações no §7 de `design/art-direction-v0.1.md`) |
| Cavaleiro Espectral, Sacerdote Morto, Vigia Sem Rosto, Sombra, Fada da Névoa (fase 3, T-056) | vários artistas do LPC, peça a peça em `<monstro>-credits.txt` | por peça: OGA-BY 3.0 quando oferecida; CC-BY-SA 3.0 no manto (*robe*: Sacerdote, Vigia, Sombra), no escudo *kite* (Cavaleiro) e no vestido da Fada | https://github.com/LiberatedPixelCup/Universal-LPC-Spritesheet-Character-Generator | `client/assets/art/characters/` | não (folhas exportadas do gerador, montagens em `tasks/reports/arte-monstros-fase-3.md`) |
| Efeitos (`effects.png`): magia, veneno, chamas, eletricidade, congelamento, luz, golpe e tiro errado; a folha inteira foi incluída | Tuomo Untinen; caveira de Johannes Sjölund (Wulax) | CC-BY-SA 3.0 | https://opengameart.org/content/lpc-items-and-game-effects | `client/assets/art/effects/effects.png` (créditos em `effects-credits.txt`) | não |
| Escudo de gelo (`magic_iceshield.png`) | Daniel Eddeland (daneeklu) | CC-BY-SA 3.0 | https://opengameart.org/content/lpc-farming-tilesets-magic-animations-and-ui-elements | `client/assets/art/effects/magic_iceshield.png` (créditos em `magic_iceshield-credits.txt`) | não (arquivo renomeado de `magic_iceshield_sheet.png`) |
| Ícones de habilidades, itens e atributos (`icons.png`): 54 ícones do pacote *496 pixel art icons for medieval/fantasy RPG* | Henrique Lazarini (7Soul1), enviado por gnola14 | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) (crédito não exigido; registrado por cortesia) | https://opengameart.org/content/496-pixel-art-icons-for-medievalfantasy-rpg | `client/assets/art/icons/icons.png` (créditos em `icons-credits.txt`; arquivo de origem de cada ícone em `icons.json`) | não (só reunidos numa folha, 34×34 px) |
| Ícones das 25 habilidades de classe (T-087) | gerados no PixelLab (modelo Pro) pelo designer (Nicholas Giudice) | arte própria do projeto (assinatura PixelLab) | https://www.pixellab.ai | `client/assets/art/icons/icons.png` (linhas novas; `source` = `pixellab:<id>#n` em `icons.json`) | não (só reunidos na folha, 32 px com 1 px de margem) |
| Kit de interface pedra e ouro (`panel`, `frame_member`, `button_*`, `crown`) | gerado no PixelLab pelo designer (Nicholas Giudice), com recortes e variações de tom feitos à mão | arte própria do projeto (gerada com assinatura PixelLab, que dá ao assinante os direitos sobre o que gera) | https://www.pixellab.ai | `client/assets/art/ui/` | sim (aba "BUTTON" apagada, canto refeito, estados do botão por tom; escolha e prévias em `tasks/reports/interface/kit-pedra-ouro.md`) |
| Fundo da tela de login (`login_background.png`, T-088) | gerado no PixelLab (modelo Pro) pelo designer (Nicholas Giudice) | arte própria do projeto (assinatura PixelLab) | https://www.pixellab.ai | `client/assets/art/ui/login_background.png` | não (opção 8 de 9; as outras em `tasks/reports/interface/login-amostra/`) |

Sprites contributed as part of the Liberated Pixel Cup project from OpenGameArt.org: http://opengameart.org/content/lpc-collection. Detailed credits: `client/assets/art/characters/*-credits.txt`.

## Pacotes aprovados

Escolhidos na T-050 e aprovados pelo designer. Cada um recebe sua entrada em "Assets incluídos" quando um arquivo dele entrar no repositório (T-052, T-053, T-054).

| Pacote | Autor(es) | Licença adotada | Origem |
|---|---|---|---|
| LPC Revised | Eliza Wyatt (ElizaWy), Lanea Zimmerman (Sharm), Stephen Challener (Redshrike) e outros, conforme `Credits.txt` | OGA-BY 3.0 | https://github.com/ElizaWy/LPC |
| [LPC Revised] Fully Configured 4-Seasons Tilesets for Tiled | JaidynReiman, sobre o LPC Revised | OGA-BY 3.0 | https://opengameart.org/content/lpc-revised-fully-configured-4-seasons-tilesets-for-tiled-map-editor |
| Universal LPC Spritesheet Character Generator | por peça, conforme o arquivo de créditos exportado de cada folha | por peça | https://github.com/LiberatedPixelCup/Universal-LPC-Spritesheet-Character-Generator |
| [LPC] Wolf Animation | William.Thompsonj | OGA-BY 3.0 | https://opengameart.org/content/lpc-wolf-animation |
| [LPC] bears, deer, lions and more | tapatilorenzo; rato gigante adaptado de Sevarihk | CC-BY 4.0 | https://opengameart.org/content/lpc-bears-deer-lions-and-more |
| [LPC] Spider | William.Thompsonj | OGA-BY 3.0 | https://opengameart.org/content/lpc-spider |
| [LPC] Items and game effects | Reemax e outros, conforme `credits.txt` | CC-BY-SA 3.0 | https://opengameart.org/content/lpc-items-and-game-effects |
| [LPC] Farming tilesets, magic animations and UI elements | Daniel Eddeland (daneeklu) | CC-BY-SA 3.0 | https://opengameart.org/content/lpc-farming-tilesets-magic-animations-and-ui-elements |

Sprites do Liberated Pixel Cup, projeto do OpenGameArt.org: http://opengameart.org/content/lpc-collection
