# T-080 — Ícones de habilidades e itens

- **Ambiente:** Windows
- **Modelo:** Humano + Leve
- **Depende de:** —
- **Libera:** T-081, T-083, T-084
- **Áreas que pode alterar:** `client/assets/art/icons/` (novo), `client/assets/art/items/`, `client/scripts/Presentation/` (só um catálogo de ícones novo), `CREDITS.md`, `design/art-direction-v0.1.md` (§7 e §8)

## Contexto

O designer quer **um ícone para cada habilidade e cada item**, no espírito dos ícones quadrados de RPG clássico (Tibia, Ragnarok): pequenos, legíveis de longe e com o contorno marcado. A referência é de estilo, não de arte: nada pode ser copiado desses jogos.

Hoje o HUD desenha as habilidades como texto e os itens como losangos coloridos. `design/art-direction-v0.1.md` §7 já escolheu o pacote **[LPC] Items and game effects** (CC-BY-SA 3.0) para ícones de itens e registra os ícones de habilidade como **lacuna** ("sem pacote coerente").

Quem precisa de ícone (ids de `data/`; a lista completa está em `design/habilidades.md` e `design/itens.md`):
- as 24 habilidades das classes (ataque básico e 5 habilidades × 4 classes) e a Poção de Cura Menor;
- os 21 itens base (7 armas, 12 armaduras, anel e amuleto);
- um ícone de "item no chão" (saco ou baú) para a T-084;
- os 5 atributos (FOR, DES, AGI, INT, VIT) para a T-082, se o estilo escolhido tiver.

## Requisitos

1. **Escolha do pacote (humano decide):** proponha de 2 a 3 opções para habilidades e confirme a licença **na página do pacote**. Regras de licença em `design/art-direction-v0.1.md` §7: preferir CC0, OGA-BY ou CC-BY; CC-BY-SA só se não houver opção; GPL não entra sem decisão do designer. Mostre ao humano uma prancha com 6 ícones de cada opção ao lado de um sprite do jogo (64×64). Ele escolhe antes de você importar tudo.
2. **Tamanho e formato:** ícones de **32×32** (o tamanho do tile), desenhados sem borrão (filtro `Nearest`). Moldura e fundo ficam a cargo do HUD, não do PNG, para a mesma imagem servir na barra, nos painéis e no chão. Junte os ícones numa ou em poucas folhas PNG.
3. **Catálogo:** `client/assets/art/icons/icons.json` liga cada id de `data/` à região na folha (`{ "warrior.heavy_strike": { "sheet": "...", "x": 0, "y": 0 } }`), no mesmo espírito de `client/assets/art/sprites.json`. Escreva também uma classe `IconCatalog` no cliente: `Texture2D? For(string id)` (`AtlasTexture`) com um ícone padrão para id sem entrada.
4. **Cobertura:** todo id da lista acima tem ícone. Habilidade sem arte coerente no pacote pode receber um ícone simples próprio, desenhado no mesmo estilo; registre isso no Resultado. Ícones derivados de CC-BY-SA continuam CC-BY-SA (§7).
5. **Créditos:** uma entrada em `CREDITS.md` por pacote, com o arquivo de créditos original ao lado das imagens (regras no topo de `CREDITS.md`). Atualize a linha "Ícones das habilidades" das lacunas em §7.
6. **Prancha de conferência:** uma cena de teste (`client/scenes/tests/IconSheet.tscn` ou parecida) que mostra todos os ícones com o id embaixo. Ela é para o humano revisar e não entra no jogo.

## Critérios de aceite

- o humano aprovou o pacote e a prancha;
- `IconCatalog.For` devolve ícone para todos os ids listados (teste ou verificação no carregamento que lista os ids sem ícone; a lista tem de sair vazia);
- `CREDITS.md` e `design/art-direction-v0.1.md` atualizados;
- `./scripts/check.sh` passa e o cliente abre como antes (nenhuma tela ainda usa os ícones; isso é das T-081, T-083 e T-084).

## Resultado

Concluída em 2026-11-02 no Windows. O designer escolheu o pacote; nenhuma tela usa os ícones ainda (isso fica para as T-081, T-083 e T-084).

**Escolha do pacote**

Opções mostradas ao designer numa prancha (6 ícones de cada, ao lado do Guerreiro), com a licença conferida na página de cada pacote em 2026-11-02:

| Opção | Pacote | Licença | Avaliação |
|---|---|---|---|
| **A (escolhida)** | [496 pixel art icons for medieval/fantasy RPG](https://opengameart.org/content/496-pixel-art-icons-for-medievalfantasy-rpg), Henrique Lazarini | CC0 | pixel art no traço dos sprites LPC; tem magias, armas, armaduras, acessórios, poções e baús |
| B | [LPC] Items and game effects (item 7 do §7) | CC-BY-SA 3.0 | só itens; os desenhos ficam pequenos no quadrado de 32 px |
| C | [Painterly Spell Icons](https://opengameart.org/content/painterly-spell-icons-part-1), J. W. Bjerk | CC-BY 3.0 (entre outras) | pintado e brilhante, destoa do pixel art; quase sem armas e flechas |
| D | [game-icons.net](https://game-icons.net) | CC-BY 3.0 | silhuetas vetoriais de duas cores; serviria só para tapar falta |

Decisão do designer: **A para tudo** (habilidades, itens, atributos e item no chão). O pacote (`496_RPG_icons.zip`, 1,5 MB, 496 PNGs com nome) foi baixado para `..\mmg-art-src\lazarini-496\`, fora do repositório.

**O que foi feito**

- `client/assets/art/icons/icons.png`: folha de 8 colunas com os 54 ícones usados, sem modificação. `icons.json`: id → `x`/`y` na folha, `size` 34, `fallback` `ui.unknown` e o arquivo de origem de cada ícone (`source`), para refazer a folha. `icons-credits.txt` ao lado.
- Cobertura: as 24 habilidades das classes (ataque básico e 5 por classe), a Armadilha de Contenção (fora da lista do Arqueiro, mas nos dados), a Poção de Cura Menor, os 21 itens base, os 5 atributos (`attribute.str` ... `attribute.vit`), `ui.ground_item` (baú, para a T-084) e `ui.unknown` (pergaminho, ícone padrão).
- `client/scripts/Presentation/IconIndex.cs`: leitura do `icons.json` sem Godot, a lista de ids que as telas mostram (`RequiredIds`: habilidades e consumíveis de cada classe, itens, atributos, item no chão) e `Missing`. `IconCatalog.cs`: `For(id)` devolve um `AtlasTexture` da folha (com cache), ou o ícone padrão para id sem entrada; `Load()` no estilo do `SpriteCatalog`.
- `client/scenes/tests/IconSheet.tscn` + `IconSheet.cs`: prancha de conferência com todos os ícones ampliados 2× e o id embaixo. Fora do jogo.
- Teste `tests/Game.Client.Tests/IconIndexTests.cs`: todo id exigido tem ícone próprio, o padrão existe, e as regiões cabem na folha, na grade e sem repetição. O `IconIndex.cs` entra no projeto de teste por link, como o `SkillPlan.cs`. O `LoadContent` do `SkillPlanTests` virou `internal` para ser reaproveitado.
- `CREDITS.md`: entrada do pacote (CC0 não exige crédito; registrado por cortesia). `design/art-direction-v0.1.md`: item 9 no §7, a lacuna "Ícones das habilidades" resolvida, o item 7 deixa de cobrir ícones de itens e a pasta `icons/` no §8.

**Desvios**

- **34×34 em vez de 32×32.** Os ícones do pacote são 34×34, e 22 dos 54 escolhidos têm contorno encostando na borda: cortar 1 px de cada lado comeria o contorno. Ficaram no tamanho original (`size` 34 no `icons.json`); o HUD escala ou usa espaços de 34–36 px.
- **Luvas** não existem no pacote: usei os ícones de arma de punho (`W_Fist001/003/004`), que são luvas e manoplas.
- **Nenhum ícone próprio foi desenhado**; todos vêm do pacote.

**Escolhas que o designer pode trocar** (basta mudar o `source` e a região no `icons.json` e refazer a folha):

- Guarda Firme: uma figura cinza parada (`S_Buff12`). É a mais fraca da lista; o pacote não tem um escudo simples.
- Muralha Viva: pilar de pedra (`S_Earth03`).
- Corrente Sagrada: brilho dourado (`S_Light01`); o pacote não tem corrente.
- Cota de Malha: armadura de couro marrom (`A_Armour01`). A prateada parecia demais com o Peitoral de Placas.

**Verificação**

- `./scripts/check.sh` completo: OK.
- A cena `IconSheet.tscn` rodou no Godot 4.7.2 e mostrou os 54 ícones nítidos, com o id certo embaixo de cada um.
