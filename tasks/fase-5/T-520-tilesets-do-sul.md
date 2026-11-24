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

Concluída. Branch `task/T-520-tilesets-do-sul`. O designer escolheu (2026-11-24): **brejo C, serra B, mina B, forte C**.

### O que foi feito

- **14 tilesets Pro** de 32 px (visão *low top-down*, mesma receita dos de hoje) no PixelLab, 3 opções por bioma, mais juncos
  (2) e um trilho reto (1) de 32 px gerados à parte. Cada opção foi montada num mapa de amostra de 30×20 (não é zona) com o
  Guerreiro e um Lobo Cinzento e aberta no jogo; as 12 capturas, a comparação A/B/C e a recomendação estão em
  **`tasks/reports/mapas-sul-tilesets/README.md`**.
- **Escolhidos** (em `client/assets/art/tiles/` e `data/maps/tilesets/`, com `.import` e `.tsx`):
  - brejo C: `pixellab_brejo_0` (lama→água rasa com vitórias-régias) e `pixellab_brejo_1` (água rasa→água funda azul-tinta);
  - serra B: `pixellab_serra_0` (chão ocre, platô de arenito ferrugem, face do penhasco virada para o sul);
  - mina B: `pixellab_mina_0` (piso de cascalho carvão, parede de terra escorada por vigas);
  - forte C: `pixellab_forte_0` (terra batida, muro de pedra bruta com topo de lajes e face escura);
  - `pixellab_sul_props`: juncos e trilho reto (horizontal e vertical).
- Metadados das 5 folhas escolhidas (cantos, `pattern_4x4`) em `tasks/reports/mapas-sul-tilesets/tilesets/`, e o montador das
  amostras em `tasks/reports/mapas-sul-tilesets/tools/build_samples.py`, para a T-530 a T-533.
- Créditos em `CREDITS.md`.

### Custo

213 gerações: 14 × 15 (tilesets Pro) + 3 × 1 (sprites). Pagas com as 3 gerações do plano que restavam e US$ 1,62 em créditos
(saldo antes: US$ 2,52 e 6 gerações; depois: US$ 0,90 e 3). Sem retentativas. Dos 14 tilesets, 5 ficaram (os outros 9 foram
gerados só para a escolha; o forte A, a muralha da Cidade, não foi gerado).

### Decisões

- Pro e não Standard: o Standard só desenha em *high top-down*, e o resto do jogo é *low top-down*.
- Brejo com dois tilesets encadeados pela água rasa; a lama nunca encosta na água funda (não há tile para isso).
- Água funda lenta (andável) é da T-502/T-540; os tilesets só dão a cara. Nas amostras, serra, mina e forte bloqueavam o topo
  da parede **e** a face (lição das Ruínas); é a regra a seguir nos mapas reais.
- "Encosta" da serra = o próprio chão ocre; rampa de verdade (`step_slope`) custaria mais uma geração. Trilhos só retos.
- Depois da escolha, removi as opções não escolhidas, os 12 mapas `sample_*` e os 12 encontros `encounter.sample_*`
  (`data/encounters.json` voltou a ser igual ao da `main`). Os ids das folhas descartadas estão no README do relatório.
- O `.import` das 5 folhas escolhidas foi regenerado com o Godot (`--headless --import`) depois de renomear.

### Verificação

`./scripts/check.sh` completo antes da escolha (com as amostras): format, build com `-warnaserror` e todos os testes passam,
**exceto 4 em `Game.Design.Tests`** (vitória do Vigia na Cripta com grupo de 4, sem o Clérigo, e `Guardian_cuts_the_damage...`
nos níveis 30 e 40), testes de equilíbrio de combate sem relação com mapas; os dois do Guardião também falham na `main`
(confirmado em checkout limpo). Depois da limpeza: `Game.Data.Tests` e `dotnet format` conferidos de novo.

### O que falta

Nada nesta tarefa. Libera a T-530, T-531, T-532 e T-533. O Forte (T-531) deve desenhar o muro com 1 a 2 tiles de espessura.
