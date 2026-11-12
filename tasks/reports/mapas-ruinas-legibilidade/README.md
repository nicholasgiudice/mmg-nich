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

## Fora daqui

A Cripta do Vigia e o lado oeste da Cidade foram conferidos com a colisão por cima e estão coerentes. Não mudaram.
