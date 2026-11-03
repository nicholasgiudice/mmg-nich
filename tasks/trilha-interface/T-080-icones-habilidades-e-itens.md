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

_(preencher ao concluir)_
