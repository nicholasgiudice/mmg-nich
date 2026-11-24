# T-520 — Tilesets do sul no PixelLab: brejo, serra, mina e forte

- **Ambiente:** Windows
- **Modelo:** Humano + Médio
- **Depende de:** —
- **Libera:** T-530, T-531, T-532, T-533
- **Áreas que pode alterar:** `client/assets/art/tiles/`, `data/maps/tilesets/`, `data/maps/` (só um mapa de amostra por bioma, que não é zona), `client/` (só o que for preciso para abrir a amostra no jogo), `CREDITS.md`, `tasks/reports/`

## Contexto

Os mapas de hoje usam terrenos Wang do PixelLab (`data/maps/tilesets/pixellab_*`, commit `c841d41`) com objetos do LPC por cima (casas, árvores, cercas, pedras). A região do sul (`design/mapas-16-40-v0.1.md` §5) tem quatro biomas novos: **brejo**, **serra**, **mina** (interior) e **forte** (paliçada e pedra).

Lições do primeiro mapa (`tasks/reports/mapas-ruinas-legibilidade/README.md`): parede e chão precisam ter cara diferente, e toda pedra de parede bloqueia. O designer prefere arte simples e aprova uma amostra **dentro do jogo** antes de pintar o mapa inteiro.

## Requisitos

1. Para cada bioma, **3 opções** de tileset no PixelLab (terreno Wang, no mesmo tamanho e estilo dos de hoje), mostradas ao designer **dentro do jogo**, num mapa de amostra pequeno com um personagem e um monstro em cima.
2. **Brejo:** água rasa (andável) e **água funda** (andável e lenta, T-502) com cara claramente diferente uma da outra e do chão. Juncos e lama. Esse é o ponto mais importante de legibilidade.
3. **Serra:** rocha, encosta e penhasco, com a face do penhasco virada para o sul, como as muralhas das Ruínas.
4. **Mina:** piso de terra, paredes de rocha com face, trilhos.
5. **Forte:** chão batido, paliçada e muro de pedra. Pode reaproveitar a muralha da Cidade.
6. O designer escolhe uma opção por bioma. Registre a escolha, o custo em créditos e o que ficou de fora.

## Critérios de aceite

- tilesets escolhidos em `data/maps/tilesets/` e `client/assets/art/tiles/`, com créditos;
- imagens da amostra no jogo em `tasks/reports/mapas-sul-tilesets/`;
- `./scripts/check.sh` completo passa.

## Resultado

_(a preencher)_
