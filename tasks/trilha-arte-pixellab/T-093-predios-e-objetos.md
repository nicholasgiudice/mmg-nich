# T-093 — Prédios e objetos de mapa (PixelLab)

- **Ambiente:** Windows (Tiled)
- **Modelo:** Humano + Médio
- **Depende de:** T-092
- **Libera:** —
- **Áreas que pode alterar:** `client/assets/art/pixellab/`, `client/assets/art/tiles/`, `data/maps/tilesets/`, `data/maps/*.tmj` (só as camadas visuais), `client/scripts/Presentation/` (só se o desenho do mapa precisar), `CREDITS.md`, `design/map-conventions-v0.1.md`

## Contexto

Os prédios, muros, cercas, árvores e pedras dos mapas vêm hoje do LPC Revised (`buildings`, `terrain_summer`). O PixelLab tem a ferramenta `create_map_object` (objeto com fundo transparente, com o estilo casado com o mapa) e a de tiles "Pro" (kits de construção). **Os objetos gerados pelo MCP são apagados do servidor deles depois de 8 horas: baixe e registre na hora.**

O cliente (`TiledVisual`, T-052) desenha tiles maiores que 32 px presos pelo canto de baixo, e a camada `above` (copas e telhados) fica transparente perto do jogador.

## Requisitos

1. **Cidade:** muralha com os portões (leste, norte e oeste; o sul fechado), e os seis prédios com identidade visível: Templo, Salão de Treinamento, Ferreiro, Estalagem, Armazém e Mercado. Telhados na camada `above`.
2. **Natureza:** árvores (pelo menos 3 tipos e 2 tamanhos), arbustos, pedras, tocos, flores, cercas, plantação.
3. **Floresta, Ruínas e Cripta** (fase 3): teias, árvores retorcidas, colunas quebradas, estátuas, lápides, portões antigos, sarcófagos, altares, os três selos do Vigia e a placa da entrada da Cripta. Gere o que os mapas da fase 3 vão precisar; se eles ainda não existirem, siga `design/first-map-v0.1.md`.
4. **Formato no Tiled:** tilesets de objetos. Confira se o `TiledVisual` lê tilesets de "coleção de imagens". Se não ler, junte os objetos num atlas com grade de 32 px e registre.
5. **Repintar** os mapas existentes com os objetos novos, sem mudar a `collision`. Um objeto que ficar com tamanho diferente do bloqueio atual precisa de aprovação do designer antes de mudar a colisão. Mudar a colisão sai desta tarefa e vira pedido à tarefa do mapa.
6. Tirar o LPC que deixar de ser usado e atualizar `CREDITS.md` e o `manifest.json`.

## Critérios de aceite

- capturas dos mapas com os prédios e objetos novos, aprovadas pelo designer;
- os testes de mapa e de design sem mudança;
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
