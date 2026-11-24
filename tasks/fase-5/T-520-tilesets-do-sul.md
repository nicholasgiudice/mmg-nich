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

**Parcial: parou antes da escolha do designer.** Branch `task/T-520-tilesets-do-sul`.

### O que foi feito

- **14 tilesets Pro** de 32 px (visão *low top-down*, mesma receita dos de hoje) no PixelLab, 3 opções por bioma:
  brejo (A, B, C; cada uma com lama→água rasa e água rasa→água funda, encadeadas), serra (A, B, C), mina (A, B, C) e forte
  (B e C; a opção A é a muralha da Cidade já existente, `pixellab_city_2`, sem gerar nada).
- Juncos (2) e trilho reto (1) de 32 px, gerados à parte (1 geração cada) e iguais nas três opções
  (`pixellab_sul_props.png`; o trilho vertical é o horizontal girado).
- **12 mapas de amostra** `data/maps/sample_<bioma>_<a|b|c>.tmj` (30×20, não são zonas) e **12 encontros**
  `encounter.sample_*` em `data/encounters.json` (Guerreiro e Lobo Cinzento nv 2; o nível 2 mantém fora da contagem dos 5 do protótipo, `PrototypeContentTests`), para abrir no jogo.
- Tudo importado lado a lado: `client/assets/art/tiles/pixellab_sul_*.png` (+ `.import`) e
  `data/maps/tilesets/pixellab_sul_*.tsx`. As folhas, o autotile e os mapas vêm de
  `tasks/reports/mapas-sul-tilesets/tools/build_samples.py`.
- Capturas no jogo, comparação A/B/C por bioma, custo e recomendação em
  **`tasks/reports/mapas-sul-tilesets/README.md`**.

### Custo

213 gerações: 14 × 15 (tilesets Pro) + 3 × 1 (sprites). Pagas com as 3 gerações do plano que restavam e US$ 1,62 em
créditos (saldo antes: US$ 2,52 e 6 gerações; depois: US$ 0,90 e 3). Sem retentativas.

### Decisões

- Pro e não Standard: o Standard só desenha em *high top-down*, e o resto do jogo é *low top-down*.
- Brejo com dois tilesets encadeados pela água rasa (lama→rasa e rasa→funda); a lama nunca encosta na água funda.
- Chão e água do brejo andáveis; a água funda lenta fica por conta da T-502/T-540. Serra, mina e forte bloqueiam o topo
  da parede e a face (lição das Ruínas).
- "Encosta" da serra = o próprio chão de cascalho; rampa de verdade (`step_slope`) custaria mais uma geração por opção.
- Trilhos só retos; curvas e cruzamentos ficam para quando o mapa da Mina precisar.

### Resultado da avaliação (minha leitura; a decisão é do designer)

Brejo: A ou C bons; **B reprovada** (água âmbar parece areia). Serra: C. Mina: A (ou B). Forte: A (muralha da Cidade).
Paliçada (B) só como parede de 1 tile.

### O que falta

1. **O designer escolher uma opção por bioma** (a pergunta está no README da pasta de relatório).
2. Depois: renomear a escolhida para o nome definitivo (`pixellab_brejo_*`, `pixellab_serra_*`, `pixellab_mina_*`,
   `pixellab_forte_*`), apagar as opções não escolhidas (imagens, `.tsx`, `sample_*.tmj`, os 12 `encounter.sample_*`),
   e deixar em `CREDITS.md` só o que ficou. Só então a T-520 fecha e libera a T-530 a T-533.
3. Os mapas de amostra e os encontros `sample_*` existem só para a escolha; não devem ir para a `main` depois dela.

### Verificação

`./scripts/check.sh` completo: format, build com `-warnaserror` e todos os testes passam, **exceto 4 em `Game.Design.Tests`**
(vitória do Vigia na Cripta com grupo de 4 e sem o Clérigo, e `Guardian_cuts_the_damage...` nos níveis 30 e 40), que são testes
de equilíbrio de combate sem relação com mapas. Confirmei que os dois do Guardião **também falham na `main`** (checkout limpo
de `main`); os do Vigia são estatísticos e não os repeti na `main` (12 min cada). `Game.Data.Tests` passou depois de trazer
os encontros de amostra para `monsterLevel` 2 (um teste conta os encontros de nível 1).

Nota: o `.uid` solto `client/scripts/Account/OnlineCounterText.cs.uid` gerado pelo build não entrou no commit.
