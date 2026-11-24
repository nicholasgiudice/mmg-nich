# Tilesets do sul (T-520): 3 opções por bioma, no jogo

Quatro biomas novos (`design/mapas-16-40-v0.1.md` §5), três opções de terreno cada, **todas importadas lado a lado**.
Cada opção está num mapa de amostra pequeno (30×20, não é zona) com o Guerreiro e um Lobo Cinzento em cima, aberto no
jogo de verdade (capturas desta pasta). **O designer escolhe uma opção por bioma.**

Para ver no jogo (Windows, a partir da raiz do repositório):

```powershell
Godot_v4.7.2-stable_mono_win64.exe --path client res://scenes/Main.tscn -- --encounter=encounter.sample_brejo_a --class=class.warrior
```

Troque `brejo_a` por `brejo_b`, `brejo_c`, `serra_a`... `forte_c` (12 amostras). Imagens: `comparacao-<bioma>.png` (as três
opções lado a lado), `<bioma>-<a|b|c>.png` (tamanho cheio) e `folhas-dos-tilesets.png` (as folhas do PixelLab como vieram).

## Como foram feitas

- Mesmo estilo e tamanho dos tilesets de hoje: PixelLab **Pro**, tiles de **32 px**, visão *low top-down*, descrições
  curtas e "simple" no fim, para a arte ficar simples.
- Mesmo traçado de mapa nas três opções de cada bioma; só o tileset muda. A montagem é feita por
  `tools/build_samples.py` (terreno por célula, cada tile centrado no canto entre quatro células, como nas Ruínas;
  paredes com face pelo autotile `pattern_4x4` do PixelLab: o chão logo ao sul de uma parede vira a face dela).
- Colisão dos mapas de serra, mina e forte: bloqueiam o topo da parede **e** a face (a lição do mapa das Ruínas). No brejo
  nada bloqueia (a água funda lenta é da T-502/T-540).
- Os juncos do brejo e os trilhos da mina são sprites de 32 px gerados à parte (1 geração cada) e iguais nas três
  opções: `pixellab_sul_props.png`.

## Brejo (o ponto mais importante: chão × água rasa × água funda)

![brejo](comparacao-brejo.png)

| | Chão (lama) | Água rasa (andável) | Água funda (andável, lenta) | Leitura |
|---|---|---|---|---|
| **A** | lama marrom-oliva com tufos | verde-azulada clara, marolas brancas | azul-marinho quase preto | **Muito boa.** Três tons bem diferentes; a funda é a que mais salta aos olhos. |
| **B** | turfa marrom escura com musgo | âmbar (cor de chá), parece **areia** | marrom quase preto, parece **terra** | **Ruim.** A água não parece água: lê-se como deserto. Só entra como alternativa. |
| **C** | lama cinza-parda lisa | verde-água coberta de **vitórias-régias** | azul-tinta escuro | **Muito boa e a mais "brejo"**: as vitórias-régias dizem "água rasa" sem precisar de legenda. Chão cinza é um pouco apagado. |

Recomendação: **A** (mais limpa e mais parecida com o resto do jogo) ou **C** (se o designer quiser o brejo com cara
própria). B não.

## Serra (rocha, encosta e penhasco com face virada para o sul)

![serra](comparacao-serra.png)

| | Chão (encosta) | Topo do platô | Face do penhasco | Leitura |
|---|---|---|---|---|
| **A** | cascalho cinza-marrom | granito cinza-claro | cinza-escuro, camadas | Legível, mas **sem cor**: o chão e o platô ficam parecidos em tom. |
| **B** | terra ocre seca | arenito ferrugem | marrom-avermelhado em camadas | **Legível e quente**, destaca bem do verde dos campos. Lembra deserto/mesa. |
| **C** | cascalho cinza-esverdeado com musgo | ardósia azul-acinzentada | azul-escuro quase preto | **Melhor contraste** de platô × face × chão; fria e séria, combina com "Serra Partida". |

Recomendação: **C** (a face escura destaca o penhasco em qualquer luz). B é a escolha se quiser o sul mais quente.

Nota: "encosta" ficou como o próprio chão de cascalho. Uma rampa de verdade (transição de 0,25 com `step_slope`) é mais
uma geração por opção; só vale a pena se o designer quiser.

## Mina (piso de terra, parede de rocha com face, trilhos)

![mina](comparacao-mina.png)

| | Piso | Parede (topo e face) | Leitura |
|---|---|---|---|
| **A** | terra marrom escura | rocha cinza-marrom com veios laranja | **Muito boa.** Parede e piso bem separados; é a "mina de terra". |
| **B** | cascalho carvão | terra escorada por **vigas e tábuas** de madeira | Boa e **mais "mina"** pelas escoras; o piso escuro e o topo marrom ficam próximos (o contorno de madeira salva). |
| **C** | pedra cinza-azulada clara | rocha azul-escura com cristais e ouro | Mais bonita, mas é **caverna de cristal**, não mina de terra; e o piso claro cansa. |

Trilhos: o mesmo sprite nas três (reto, horizontal e vertical). Curvas e cruzamentos não foram feitos; viram peça nova
quando o mapa pedir.

Recomendação: **A** (ou **B**, se o designer preferir as escoras de madeira).

## Forte (chão batido, paliçada e muro de pedra)

![forte](comparacao-forte.png)

| | Parede | Leitura |
|---|---|---|
| **A** | **A muralha da Cidade, sem gerar nada** (`pixellab_city_2`): pedra cinza com ameias | **A mais legível.** Topo cinza e face de tijolo contra a terra batida. Custo zero e já combina com a Cidade. |
| **B** | **Paliçada** de troncos pontudos | A **face** (troncos) lê bem; o **topo** tem a cor da terra, então uma parede grossa parece chão. Funciona só como parede de 1 tile. |
| **C** | Muro de **pedra bruta** com chão de lajes claras e face escura | Legível e diferente da Cidade, mas o topo claro parece pavimento. |

Recomendação: **A** para o muro de pedra (zero custo). Se o designer quiser a paliçada, **B** exige desenhar o Forte
com parede de 1 tile (o mapa de amostra usa 3, que é onde ela mais sofre).

## Custo

Saldo antes: US$ 2,52 em créditos e 6 gerações do plano. Depois: US$ 0,90 e 3 gerações.
- 14 tilesets Pro de 32 px: 6 do brejo (3 opções × lama→rasa e rasa→funda), 3 da serra, 3 da mina, 2 do forte
  (a opção A é a muralha da Cidade, já existente): 15 gerações cada = **210 gerações**.
- 3 sprites de 32 px (2 juncos, 1 trilho): 1 geração cada = **3 gerações**.
- Total: **213 gerações**, pagas com as 3 do plano que restavam e US$ 1,62 em créditos. Nenhuma retentativa. Nenhum
  job falhou (um pedido de água funda foi recusado por um parâmetro e refeito sem custo).
- Sobram US$ 0,90 em créditos (cada tileset Pro custa cerca de US$ 0,12, ou seja, uns 7). As 6 gerações do plano e a
  cota do mês voltam em 2026-12-05.

## A escolha

**Qual opção (A, B ou C) você quer para cada bioma: brejo, serra, mina e forte?** Minha sugestão: brejo A (ou C),
serra C, mina A, forte A.

## O que falta

1. O designer escolher **uma opção por bioma**.
2. Depois da escolha: apagar as opções não escolhidas (imagens, `.tsx`, mapas de amostra e os 12 encontros
   `encounter.sample_*` de `data/encounters.json`), e passar a escolhida para o nome definitivo
   (`pixellab_brejo_*` etc.). As amostras ficam fora do histórico só depois disso.
