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

_(preencher ao concluir)_
