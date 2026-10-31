# T-052 — Cliente: desenhar os mapas do Tiled com o tileset

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** T-050, T-051
- **Libera:** T-055
- **Áreas que pode alterar:** `client/`

## Contexto

Hoje o cliente desenha a arena com quadrados (`ArenaView`). Com os mapas do Tiled (T-051) e o tileset escolhido (T-050), o cliente passa a desenhar o mapa real. A colisão continua vindo do Core, nunca da física do Godot.

## Requisitos

1. Importar o JSON do Tiled no Godot (`TileMapLayer` por camada, ou um importador próprio) usando o mesmo arquivo que o Core lê.
2. Ordem de desenho: chão < decoração baixa < áreas e telegráfos < unidades < decoração alta (copas, telhados), com transparência quando o jogador fica atrás.
3. Câmera continua limitada às bordas do mapa.
4. `ArenaView` com quadrados fica só para depuração (argumento de linha de comando).

## Critérios de aceite

- os 5 encontros desenhados com o tileset, mesmo comportamento de colisão;
- `dotnet build client/` e `./scripts/check.sh` passam.

## Resultado

**Feito** (Windows, 2026-10-31). `dotnet build client/` sem avisos, `./scripts/check.sh` completo passa. Conferido no cliente com capturas dos 5 encontros, da transparência da copa e de `--debug-grid`.

**Cliente**
- `TiledVisual` (novo) lê o mesmo `.tmj` que o Core (`res://data/<Source>`) e os `.tsx` externos (XML). Guarda as camadas de tiles visíveis, inclusive dentro de grupos, e desenha cada tile com o tileset certo pelo `firstgid`. Espelhamento horizontal e vertical é desenhado; a rotação (bit diagonal) não. Tiles mais altos que 32 px ficam presos pelo canto de baixo, como no Tiled.
- A imagem do tileset é achada pelo **nome do arquivo** em `res://assets/art/tiles/`. O caminho relativo do `.tsx` (`../../../client/assets/...`) não existe dentro de `res://`. Tileset sem imagem (o de colisão) é ignorado.
- `ArenaView.Show(map, arena)` desenha `ground`, `decoration`, `walls` e as outras camadas de tiles na ordem do arquivo, menos `collision` e `above`. Fica em z 0, abaixo de áreas, telegráfos e unidades. Um filho desenha `above` em `ZLayers.Above` (6, acima das unidades). As células num raio de 2 tiles do jogador ficam com 35% de opacidade, e só redesenha quando o jogador muda de célula. O `Main` passa a posição desenhada do jogador a cada frame.
- Sem mapa do Tiled (arenas ASCII: `arena.city`, `arena.vigil_fields`), com erro de leitura ou com `--debug-grid`, volta a grade de quadrados de antes.
- A colisão continua só no Core (`GridMap`). Nada de física do Godot.
- A câmera continua limitada pelo `GridMap` (`CameraRig.SetBounds`), que tem o tamanho do mapa.
- `MmoRpgClient.csproj` copia também `data/**/*.tsx` para `res://data`.

**Arte e mapas**
- `client/assets/art/tiles/terrain_summer.png`, do LPC Revised configurado para o Tiled (OGA-BY 3.0, `CREDITS.txt` na pasta e entrada em `CREDITS.md`). O `.tsx` original virou `data/maps/tilesets/terrain_summer.tsx`, com o caminho da imagem conforme `design/map-conventions-v0.1.md` §1. Wangsets e objetos de colisão do pacote foram mantidos; o Core não lê `.tsx`.
- Os 5 mapas tiveram as camadas visuais **preenchidas por script**, a partir da colisão (decisão combinada com o designer, para não ficarem em branco até a T-055). O script ficou fora do repositório, já que foi uma vez só. A pintura:
  - grama variada em `ground` e tufos em `decoration` (5% das células livres);
  - arbustos na borda e em blocos com 12 células ou mais, em `walls`;
  - rocha grande em cada 2×2 dos obstáculos menores e rocha pequena nas sobras, em `walls`;
  - pinheiros na borda de baixo: o tronco em `walls` e a copa numa camada nova `above` (2 linhas para dentro da arena), para mostrar a transparência.
- A camada `collision` não mudou. Os testes de equivalência da T-051 e os 83 testes de design passam iguais.

**Desvios**
- Fora de `client/`: `data/maps/*.tmj` (camadas visuais e o tileset), `data/maps/tilesets/terrain_summer.tsx`, `design/map-conventions-v0.1.md` (§10 descreve a pintura e o desenho no cliente) e `CREDITS.md`.

**Pendências**
- Abrir os `.tmj` no **Tiled** no Windows (pendência da T-051): o Tiled não está instalado nesta máquina. Ao abrir, conferir que o tileset aparece e retocar os mapas à vontade (T-055).
- A copa (`above`) fica por cima das barras de vida e dos nomes das unidades embaixo dela, mas fica transparente perto do jogador. Se incomodar com monstros sob árvores, dá para desenhar barras e nomes num nível acima.
- Exportação do jogo: o filtro de arquivos não-recurso precisa incluir `data/*.tmj` e `data/*.tsx` (pendência da T-051).
