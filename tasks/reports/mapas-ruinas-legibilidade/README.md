# Ruínas da Estrada Antiga: legibilidade do mapa

Ajuste no `data/maps/old_road_ruins.tmj` (saída oeste da Cidade) depois do playtest: era difícil saber onde dava para
andar, e alguns lugares não tinham saída. Imagens: `ruinas-antes-depois.png` e `leste-colisao-antes-depois.png`.

## O que estava errado

- **Borda das muralhas andável.** As muralhas usavam o bloco 3×3 de pedra do `buildings`; só o miolo bloqueava.
  A borda de 1 tile (≈1.650 células) parecia muralha, mas dava para andar nela.
- **Ruínas de casa sem colisão.** Paredes de 2 tiles de largura eram só borda, então não bloqueavam nada: o jogador
  atravessava a parede. Por dentro, a casa só tinha "saída" passando por cima da parede.
- **Muralha e chão com a mesma cara.** O topo das muralhas era a mesma pedra cinza clara do piso e da estrada, sem
  face nem sombra.
- **Buracos de terra no piso.** Os buracos no calçamento têm uma borda escura que parece um degrau ou uma mureta.
- **Becos sem saída:** uma faixa de grama a leste do vão da muralha norte terminava na mata; havia um bolsão de grama
  no pé da muralha sul e frestas de 1 tile entre os arbustos e as muralhas.

## O que mudou

- Toda célula com pedra de muralha agora bloqueia. A colisão é igual ao desenho.
- As muralhas ganharam **topo escuro** (ardósia, `buildings` linha 8) e uma **face de pedra clara virada para o sul**
  (a linha de baixo do bloco antigo). Dá para ver a altura da muralha e separar parede de chão.
- Cinco ruínas de casa que ficaram fechadas ganharam uma **porta de 2 tiles**, no lado sul. Os cantos internos de 1 tile
  que ficaram isolados foram bloqueados.
- O bolsão de grama lacrado no pé da muralha sul (136–145, 116–118) virou mata fechada.
- A faixa de grama do vão norte (136–146, 31–33) agora **continua até o campo leste**, num caminho de 4 tiles pela mata.
  O vão virou passagem entre os dois campos.
- Frestas de 1 tile sem saída viraram arbusto.
- Os buracos pequenos de terra no calçamento (até 40 células, 115 no total) foram calçados. As faixas de terra na beira
  da estrada continuam.

Os surgimentos, áreas, portões e a entrada da Cripta não mudaram. Toda célula livre é alcançável a pé a partir do
nascimento.

## Teste

`ZoneMapTests.Ruins_have_no_sealed_pockets_or_dead_end_slits`: toda célula livre das Ruínas é alcançável a pé, e
nenhuma célula livre fora de um portão é ponta de fresta (1 vizinho livre ou menos). O mapa antigo falhava: 112 células
livres eram inalcançáveis.

## Segunda rodada: arte do PixelLab

O designer achou que ainda não dava para ver o que é parede e o que é chão, e que a arte parecia inacabada. As Ruínas
foram redesenhadas com tilesets do PixelLab, mantendo o traçado, os monstros, as áreas e os portões. Imagens:
`ruinas-pixellab-antes-depois.png` e `piloto-pixellab-no-jogo.png` (o piloto do Pátio).

- **Arte:** 9 tilesets Wang Pro de 32 px, com visão *low top-down*:
  - piso de lajes, entulho, grama no tom da grama LPC (#468232), mata fechada;
  - muralha com topo de pedra, ameias e face de tijolo;
  - todos encadeados pelo mesmo piso, pela mesma grama e pela mesma muralha.
  
  Custo: 165 gerações.
- **Mapa:**
  - montado no Map Workshop do PixelLab ("Valdran - Ruínas da Estrada Antiga"). O designer pode abrir e ajustar lá;
  - cada célula do mapa antigo virou um terreno (piso, parede, grama, mata ou entulho);
  - pedras, colunas, lampiões e a fachada da Cripta continuam como objetos LPC por cima.
- **Conversão:** o export do PixelLab para Godot traz cada tile com sua posição. Ele vira camadas `terrain_*` no `.tmj`.
  - O PixelLab desenha numa grade dupla: cada tile fica centrado no canto entre quatro células. Por isso essas camadas
    têm `offsetx`/`offsety` de -16 px.
  - `TiledVisual` passou a respeitar o deslocamento de camada do Tiled.
- **Colisão:** sai das células de terreno do PixelLab, e coincide com o desenho:
  - bloqueiam o topo e a face da muralha e a mata, incluindo a faixa de troncos na borda;
  - também bloqueiam as pedras e a fachada que eram bloqueadas antes;
  - piso, grama e entulho são livres.
  
  No piloto, o personagem encosta na borda das ameias.
- **Regras do PixelLab que mudaram o traçado:**
  - um canto onde três terrenos se encontram vira emenda reta, e fica preto se um deles for a faixa da face;
  - degraus de uma linha na base de uma parede ou da mata ficam pretos.
  
  Para isso:
  - os cantos diagonais da Torre ficaram retos;
  - a muralha na borda leste do mapa virou mata, como nas outras bordas;
  - a faixinha de mata entre o campo oeste e a estrada virou piso;
  - algumas pontas de muro recuaram para dentro da mata;
  - o topo do campo nordeste ficou reto.
  
  Os surgimentos, portões e a entrada da Cripta não mudaram.
- O teste de percurso, o de frestas sem saída e os de design passam com o mapa novo.

## Cripta do Vigia

A instância da Cripta (`data/maps/watcher_crypt.tmj`) foi refeita do mesmo jeito.

- **Arte:** 1 tileset Pro: chão de lajes escuras e parede de basalto com face de tijolo com runas. Custo: 30 gerações.
  - Um tileset de entulho com ossos foi gerado, mas ficou claro demais no chão escuro e não foi usado.
- **Mapa:**
  - montado no Map Workshop ("Valdran - Cripta do Vigia");
  - continuam por cima: colunas, lampiões, a escada de saída e as pedras pequenas.
- **Colisão:** é a mesma de antes, com duas exceções:
  - os toquinhos de muro de 1 linha entre as salas oeste ganharam mais uma linha (5 células bloqueadas);
  - um degrau de uma linha na entrada do corredor leste virou chão (3 células livres).
  
  O PixelLab não tem tile para nenhum dos dois casos.
- **Visual:** a borda andável das salas, que antes era desenhada como parede, agora é chão.

## Cidade, Campos, Floresta e Covil

Os outros quatro mapas foram refeitos com o mesmo processo, aproveitando a grama, a mata, as lajes e a muralha das
Ruínas.

- **Arte:** 6 tilesets novos (90 gerações):
  - terra batida↔grama, terra↔mata, terra↔muralha e terra↔lajes;
  - uma mata sem faixa de troncos (só a borda das copas), com grama e terra.
  
  A mata sem troncos é para as bordas irregulares da Floresta e dos Campos. A versão com troncos não tem tile para
  degraus de uma linha, e a Floresta tinha mais de 200 deles.
- **Mapas no Map Workshop:**
  - "Valdran - Cidade-Fortaleza"
  - "Valdran - Campos da Vigília"
  - "Valdran - Floresta Baixa"
  - "Valdran - Covil da Matriarca"
- **O que continua como objeto LPC por cima, com a colisão de antes:** casas (inclusive a de pedra cinza da Cidade),
  árvores e moitas soltas, cercas, plantações, pedras e o portão do Covil.
  - Só massas de moita e árvore com 12 células ou mais viraram mata.
  - Só a pedra de muralha ligada à borda do mapa virou muralha.
- **Colisão:** igual à de antes nos Campos, na Floresta e no Covil. Na Cidade, a muralha norte tem 1 linha de altura e
  a face dela ocupa a primeira linha da rua norte: 74 células bloqueadas, e a rua fica com 3 de largura em vez de 4.
- **Ferramenta:** o processo inteiro está num script (`mapkit.py`, fora do repositório, como os geradores anteriores):
  1. classifica as células do mapa antigo;
  2. confere as regras do PixelLab (cantos com faixa, degraus de uma linha) e corrige sem mudar onde se anda;
  3. gera os retângulos de pintura em camadas;
  4. junta o export com os objetos e os pontos do mapa antigo.

## Fora daqui

A Cripta do Vigia e o lado oeste da Cidade foram conferidos com a colisão por cima e estão coerentes. Não mudaram.
