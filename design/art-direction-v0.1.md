# Direção de Arte — Versão 0.1

Decisão do designer (2026-10-19): arte vinda de **pacotes de assets**, estilo **simples como o Tibia, mas não idêntico**. Este documento fixa o estilo, as regras de escala e legibilidade e os pacotes escolhidos. Ele é a base das tarefas T-052 (mapas no cliente), T-053 (sprites), T-054 (efeitos) e T-055 (Cidade e Campos).

Estado: **aprovado pelo designer em 2026-10-31** (T-050). Pacotes baixados fora do repositório (ver §8).

---

## 1. Estilo e referências

- **Família LPC (Liberated Pixel Cup)**: pixel art de 32 px, perspectiva de três quartos (visão de cima levemente inclinada), contorno escuro nos personagens, cores vivas mas sem neon.
- Referências de leitura: Tibia (clareza do mapa, personagem pequeno no mundo), Zelda: A Link to the Past e Chrono Trigger (a referência declarada do próprio LPC).
- O que **não** copiamos do Tibia: os sprites, a interface e a perspectiva diagonal dele. O LPC já é diferente o bastante: personagens mais altos (cerca de 1,5 tile) e terreno com transições suaves.
- Base preferida: **LPC Revised** (curadoria de Eliza Wyatt), que reorganiza e amplia o LPC original com uma paleta revisada. Os personagens vêm do **gerador Universal LPC**, que tem muito mais roupas, armas e corpos (inclusive esqueleto).

## 2. Paleta

- Cenário e objetos: **Liberated Palette** do LPC Revised (`_ Palette/palette.json` no repositório do LPC Revised, cerca de 170 cores em rampas). Retoques e recolorações usam essas rampas.
- **Cores reservadas para gameplay.** Estes tons saturados não aparecem no cenário, para que o que importa no combate sempre se destaque. São os valores já usados no cliente:

| Uso | Cor | Onde no código |
|---|---|---|
| Telegráfo hostil | `#FF5933` (laranja-vermelho) | `TelegraphView.Hostile` |
| Telegráfo aliado | `#59BFFF` (azul claro) | `TelegraphView.Friendly` |
| Jogador / jogador controlado | `#4D8CF2` / `#59D9FF` | `EntityView` |
| Monstro | `#D94D40` | `EntityView` |
| Seleção | `#FFD933` (amarelo) | `EntityView.SelectionColor` |
| Poça de veneno | `#73D940` | `AreaView` |

- O terreno fica um pouco menos saturado e mais escuro que os efeitos. Se um tileset tiver vermelho ou amarelo forte em grande área (flores, lava, telhados), ele vai para a decoração baixa e em pouca quantidade.

## 3. Escala

- **1 unidade do Core = 1 tile = 32 px.** Tiles de 32×32. O cliente já converte assim.
- Folhas do gerador Universal LPC: **quadros de 64×64**. O corpo do personagem ocupa cerca de 32 px de largura e um pouco menos de 1,5 tile de altura.
- **Círculo de colisão × sprite:** classes têm raio de 0,40 a 0,45 tile (26 a 29 px de diâmetro), que é a largura do corpo na altura dos pés. Regra: **o ponto entre os pés do sprite fica no centro do círculo de colisão**. O deslocamento (pivô) de cada folha fica nos dados do cliente (T-053), não em código.
- Monstros seguem o raio do `data/monsters.json`:

| Monstro | Raio | Diâmetro | Sprite base |
|---|---|---|---|
| Rato de Campo | 0,30 | ~19 px | rato gigante do pacote LPC Animals 2022 (quadros 80×64) |
| Aranha pequena | 0,35 | ~22 px | aranha LPC (`spider01`, verde-escura) |
| Lobo Cinzento | 0,40 | ~26 px | lobo LPC recolorido em cinza claro |
| Salteador | 0,40 | ~26 px | personagem do gerador |
| Guarda Esquelético | 0,50 | 32 px | corpo de esqueleto do gerador |
| Aranha grande | 0,60 | ~38 px | aranha LPC (`spider05`, cinza e vermelha) |
| Matriarca da Floresta | 0,90 | ~58 px | aranha LPC vermelha com marcas amarelas (`spider04`) em escala ×2 (opção A, escolhida pelo designer) |

As aranhas LPC têm uns 42 px de largura em ×1. Isso serve para a aranha grande, mas fica grande para o raio da pequena. Na T-053, decidir entre aceitar a diferença e reduzir o raio de colisão (decisão de dados, não de arte).

- Escala de sprite **somente inteira** (×1, ×2) com filtro *nearest*. Nada de escala fracionária nem rotação de sprite, que borram o pixel. A câmera também usa zoom inteiro.

## 4. Perspectiva e direções

- Três quartos, luz vinda de cima à esquerda (padrão LPC).
- **4 direções** (cima, esquerda, baixo, direita, na ordem das folhas LPC). A direção vem do movimento ou do `Facing` do Core, arredondada para a mais próxima. As diagonais usam esquerda ou direita.

## 5. Animações mínimas

| Estado | Personagens (gerador LPC) | Monstros |
|---|---|---|
| Parado | `idle` | primeiro quadro do andar, ou `idle` se houver |
| Andando | `walk` (9 quadros) | `walk` / `run` da folha |
| Atacando | Guerreiro: `slash`; Arqueiro: `shoot`; Mago: `spellcast`; Clérigo: `slash` | `attack` / `bite` da folha |
| Conjurando | `spellcast` em laço enquanto `AbilityState.Cast` durar | o mesmo ataque, mais lento, ou parado com brilho |
| Tomando dano | piscar branco no sprite (já existe hoje no círculo) | piscar branco |
| Morrendo | `hurt` (a queda do LPC), último quadro fica no chão e esmaece | `death` da folha, se houver; senão, queda e esmaecimento |

O `hurt` do LPC é a animação de cair, não de recuo, por isso é usado para morrer. "Tomando dano" fica como efeito por cima do sprite, sem animação própria.

## 6. Legibilidade

- **Ordem de desenho** (de baixo para cima): chão < decoração baixa < **áreas e telegráfos** < unidades mortas < unidades < decoração alta (copas, telhados). A decoração alta fica transparente quando o jogador está atrás dela. Os telegráfos nunca ficam embaixo de decoração.
- Telegráfos e áreas continuam **procedurais** (contorno e preenchimento progressivo, T-023), com as cores reservadas do §2. A arte de efeito (T-054) entra por baixo do contorno, nunca por cima.
- Por cima do sprite continuam: nome, barra de vida, ícones de status, marcas de controle, anel de seleção e números flutuantes.
- **Cores de facção** ficam no anel ou sombra sob a unidade (azul jogador, vermelho monstro), não pintadas no sprite. Assim um Salteador e um Guerreiro com o mesmo equipamento continuam distinguíveis.
- Personagens dos jogadores têm contorno escuro de 1 px (padrão LPC). O cenário nunca usa contorno tão forte.

## 7. Pacotes escolhidos

Licenças conferidas nas páginas dos pacotes em 2026-10-31. Quando um pacote oferece várias licenças, **usamos a mais permissiva** (coluna "Adotada").

| # | Pacote | Autor(es) | Licenças na página | Adotada | Cobre |
|---|---|---|---|---|---|
| 1 | [LPC Revised](https://github.com/ElizaWy/LPC) | Eliza Wyatt (ElizaWy), Lanea Zimmerman (Sharm), Stephen Challener (Redshrike) e outros (`Credits.txt` de cada pasta) | CC-BY 3.0 ou OGA-BY 3.0 | **OGA-BY 3.0** | terreno nas 4 estações, árvores, plantas, penhascos, água, estruturas (muros, telhados, portas, cercas, pontes), móveis e objetos, paleta e guia de escala |
| 2 | [[LPC Revised] Fully Configured 4-Seasons Tilesets for Tiled](https://opengameart.org/content/lpc-revised-fully-configured-4-seasons-tilesets-for-tiled-map-editor) | JaidynReiman, sobre o LPC Revised | CC-BY 3.0, OGA-BY 3.0 | **OGA-BY 3.0** | a arte do item 1 já configurada como tilesets do Tiled (terrenos e colisão), base para T-051 e T-055 |
| 3 | [Universal LPC Spritesheet Character Generator](https://github.com/LiberatedPixelCup/Universal-LPC-Spritesheet-Character-Generator) ([site](https://liberatedpixelcup.github.io/Universal-LPC-Spritesheet-Character-Generator/)) | dezenas de artistas; créditos por peça no `CREDITS.csv` e no arquivo de créditos exportado pelo gerador | por peça: CC0, CC-BY, CC-BY-SA, OGA-BY ou GPL | a mais permissiva de cada peça; **nunca GPL sozinha** (usar o filtro de licença do gerador) | as 4 classes com equipamento visível, Salteador, Guarda Esquelético (corpo de esqueleto), armas, flechas |
| 4 | [[LPC] Wolf Animation](https://opengameart.org/content/lpc-wolf-animation) | William.Thompsonj | CC-BY 4.0/3.0, GPL 3.0/2.0, OGA-BY 3.0 | **OGA-BY 3.0** | Lobo Cinzento (recolorido em cinza claro) |
| 5 | [[LPC] bears, deer, lions and more](https://opengameart.org/content/lpc-bears-deer-lions-and-more) | tapatilorenzo; rato gigante adaptado de Sevarihk | CC-BY 4.0 | **CC-BY 4.0** | Rato de Campo (rato gigante); ursos, cervos, raposas e camundongos para depois |
| 6 | [[LPC] Spider](https://opengameart.org/content/lpc-spider) | William.Thompsonj | CC-BY 4.0/3.0, GPL 3.0/2.0, OGA-BY 3.0 | **OGA-BY 3.0** | aranhas pequena e grande; base da Matriarca |
| 7 | [[LPC] Items and game effects](https://opengameart.org/content/lpc-items-and-game-effects) | Reemax e outros (`credits.txt`) | CC-BY-SA 3.0, GPL 3.0/2.0 | **CC-BY-SA 3.0** | ícones de itens (inventário e chão), alguns efeitos |
| 8 | [[LPC] Farming tilesets, magic animations and UI elements](https://opengameart.org/content/lpc-farming-tilesets-magic-animations-and-ui-elements) | Daniel Eddeland (daneeklu) | CC-BY-SA 3.0, GPL 3.0 | **CC-BY-SA 3.0** | animações de magia (T-054), elementos de interface |

**Reserva**, se faltar algo no item 1: [LPC Tile Atlas](https://opengameart.org/content/lpc-tile-atlas) e [LPC Tile Atlas2](https://opengameart.org/content/lpc-tile-atlas2) (adrix89 e autores do LPC, CC-BY-SA 3.0 / GPL 3.0). São o LPC original, com paleta mais antiga.

**Descartados:**
- [LPC] Rat, Cat and Dog (Tuomo Untinen, CC-BY 3.0): o rato de 32 px ficou feio na cena de teste e foi trocado pelo rato gigante (decisão do designer, 2026-10-31).
- Arachne (AntumDeluge, OGA-BY): mulher-aranha de 48×64 baseada nas aranhas LPC. É pequena e com traço diferente para ser a Matriarca.
- Kenney *UI Pack (RPG Expansion)* (CC0, conferido): traço vetorial liso, destoa do pixel art.
- Ninja Adventure: tiles de 16 px, escala diferente da nossa.

### O que as licenças exigem de nós

- **Atribuição sempre** (exceto CC0), em `CREDITS.md` e numa tela de créditos acessível de dentro do jogo. Essa tela ainda não existe (ver lacunas).
- **CC-BY e CC-BY-SA proíbem proteção técnica** (DRM). Enquanto houver asset nessas licenças, não usamos criptografia do `.pck` do Godot. OGA-BY permite.
- **CC-BY-SA (itens 7 e 8, e algumas peças do gerador):** uma arte *derivada* (recolorida ou editada) desses arquivos tem de ser distribuída também em CC-BY-SA. Isso não afeta o código do jogo nem os outros assets. Preferimos editar assets OGA-BY/CC-BY quando houver escolha.
- **GPL:** evitada. Só entra se não houver outra licença para a peça, e precisa de decisão do designer.

### Lacunas (nenhum pacote cobre)

| Lacuna | Proposta |
|---|---|
| Sprite próprio da Matriarca | aranha vermelha do item 6 (`spider04`) em escala ×2; arte própria fica para depois |
| Fadas da Névoa, Cavaleiro Espectral, Vigia Sem Rosto (fase 3) | procurar quando a fase 3 for detalhada |
| Ícones das habilidades | sem pacote coerente; ícones simples próprios ou procurar na T-053/T-054 |
| Tema de interface (fonte, painéis, botões) | elementos do item 8 são candidatos; hoje a interface segue procedural (`HudTheme`). Fora do escopo desta tarefa |
| Ícones de status | continuam procedurais (`StatusVisuals`) |
| Tela de créditos no jogo | exigida pelas licenças; entra numa tarefa de cliente antes de qualquer distribuição pública |
| Sons | fora do escopo da trilha de arte |

### Configuração dos personagens no gerador

Combinações usadas na cena de teste (2026-10-31). Para reproduzir, cole o trecho depois de `#` no endereço do [gerador](https://liberatedpixelcup.github.io/Universal-LPC-Spritesheet-Character-Generator/). Na T-053, exporte a folha com o botão *Spritesheet (PNG)* e os créditos com *Credits (CSV)*.

| Personagem | Configuração |
|---|---|
| Guerreiro | `sex=muscular&body=Body_Color_light&head=Human_Male_light&expression=Neutral_light&hair=High_and_tight_dark_brown&shoulders=Pauldrons_gray&arms=Armour_steel&bracers=Bracers_steel&legs=Pants_charcoal&shoes=Armour_steel&shield=Kite_kite_blue_gray&weapon=Arming_Sword_steel` |
| Arqueiro | `sex=male&body=Body_Color_light&head=Human_Male_light&expression=Neutral_light&hair=Ponytail_ginger&armour=Leather_leather&legs=Pants_walnut&shoes=Basic_Boots_brown&weapon=Recurve_medium&ammo=Ammo_arrow&quiver=Quiver_quiver` |
| Mago | `sex=male&body=Body_Color_light&head=Human_Male_light&expression=Neutral_light&hair=Long_gray&beard=Medium_Beard_gray&clothes=Longsleeve_navy&jacket=Frock_coat_blue&legs=Formal_Pants_navy&shoes=Basic_Boots_brown&weapon=Gnarled_staff_medium` |
| Clérigo | `sex=male&body=Body_Color_light&head=Human_Male_light&expression=Neutral_light&hair=Curly_short_light_brown&clothes=Longsleeve_tan&jacket=Tabard_white&legs=Pants_tan&shoes=Basic_Boots_brown&weapon=Mace_mace` |
| Salteador | `sex=male&body=Body_Color_light&head=Human_Male_light&expression=Neutral_light&armour=Leather_maroon&legs=Pants_brown&shoes=Basic_Boots_walnut&bandana=Bandana_red&weapon=Arming_Sword_iron` |
| Guarda Esquelético | `sex=male&body=Skeleton_white&head=Skeleton_white&armour=Leather_walnut&weapon=Arming_Sword_iron` |

Ajustes do designer (2026-10-31):
- Guerreiro com corpo musculoso, sem capacete e com cabelo. O corpo musculoso não aceita armadura de tronco no gerador, então ele usa ombreiras, braçais e proteção de braços e pés.
- Arqueiro, Mago e Clérigo com cabelo.
- O Mago estava "sem roupa" porque o manto (*Robe*) do gerador só existe para corpo feminino. Agora usa camisa, sobrecasaca azul, calça e cajado.

Licenças mostradas pelo gerador: quase todas as peças têm OGA-BY 3.0 como opção. Algumas peças do Guerreiro, do Arqueiro e do Mago só oferecem **CC-BY-SA 3.0** (ou CC-BY-SA/GPL). São aceitas pela regra do §7, e a conferência peça a peça é feita pelo CSV de créditos na exportação.

### Risco a conferir

Os personagens do gerador seguem o LPC original, e o terreno escolhido é do LPC Revised, com paleta revisada. As duas famílias são feitas para conviver, mas a mistura **precisa ser vista lado a lado** pelo designer antes de aprovar. Se destoar, o plano B é usar o terreno do LPC original (Tile Atlas, reserva).

## 8. Onde os assets ficam no repositório

```text
client/assets/art/
├── tiles/        # imagens de tileset (LPC Revised); os .tsx/.tmj ficam em data/maps/ (T-051)
├── characters/   # folhas geradas por classe, cada uma com o .json exportado do gerador
├── monsters/     # uma pasta por monstro (monster.grey_wolf → grey_wolf/)
├── effects/      # T-054
├── items/        # ícones de itens
└── ui/           # se o tema de interface vier a usar arte
```

- Cada pasta guarda, junto da imagem, o arquivo de créditos original do pacote (`Credits.txt`, `credits.txt` ou o `.csv` exportado pelo gerador).
- **Só entram no Git as imagens usadas pelo jogo**, já recortadas. São PNGs pequenos, de dezenas a centenas de KB cada.
- **Sem Git LFS por enquanto.** Os pacotes completos (o LPC Revised inteiro tem cerca de 300 MB) e os arquivos-fonte ficam **fora do repositório**, numa pasta local ao lado do clone (sugestão: `..\mmg-art-src\`). Se `client/assets/` passar de ~100 MB, reavaliar LFS.
- O caminho exato dos tilesets entre `data/maps/` (lido pelo Core) e `client/assets/art/tiles/` (desenhado pelo cliente) é decisão da T-051/T-052. Recomendação: o `.tsx` aponta para a imagem por caminho relativo.

### Downloads (confirmados e feitos em 2026-10-31, em `..\mmg-art-src\`)

Cada pacote fica numa pasta própria, com os zips extraídos em `extracted/`. As cenas de teste estão em `..\mmg-art-src\preview\`: `cena-personagens.png` (terreno + classes, Salteador, Esqueleto, Lobo, Rato) e `cena-monstros.png` (escala dos monstros com o círculo de colisão, com rato gigante, lobo cinza claro e três opções de Matriarca). O lobo recolorido está em `lpc-wolf\gray-light\`. Também foi baixado, com confirmação, `lpc_animals_2022_v1.1.zip` (421 KB, item 5). Observações:
- O zip do item 2 traz só os `.png` e `.tsx`, sem arquivo de créditos. A atribuição vem da página (§7).
- O zip do item 7 traz `credits.txt`. O do item 8 traz `readme.txt`.
- As folhas do gerador (item 3) não foram baixadas. As cenas usam quadros capturados do próprio gerador. A exportação definitiva fica para a T-053.

| Arquivo | Origem | Tamanho |
|---|---|---|
| `lpc-revised-exterior-tilesets.zip` | OpenGameArt, item 2 | 3,5 MB |
| `wolfsheet1.png` … `wolfsheet6.png`, `howl.png` | OpenGameArt, item 4 | ~86 KB cada, 2 KB |
| `lpccatratdog.png` | OpenGameArt, item 5 | 27 KB |
| `LPC_Spiders.zip` | OpenGameArt, item 6 | 1 MB |
| `ItemsAndEffects.zip` | OpenGameArt, item 7 | 181 KB |
| `submission_daneeklu.zip` | OpenGameArt, item 8 | 557 KB |
| `palette.json` e `Scale Guide.png` | GitHub, item 1 (só esses dois, não o repositório inteiro) | pequenos |
| folhas das 4 classes, Salteador e Guarda Esquelético | geradas no site do item 3, com o `.json` e os créditos exportados | ~100–500 KB cada |
