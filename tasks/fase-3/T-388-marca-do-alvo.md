# T-388 — Marca melhor no monstro que estou atacando

- **Ambiente:** Windows
- **Modelo:** Humano + Médio
- **Depende de:** —
- **Libera:** —
- **Áreas que pode alterar:** `client/scripts/Presentation/` (`EntityView`, `WorldView`), `client/assets/art/ui/` ou `client/assets/art/fx/`, `CREDITS.md`, `tasks/reports/interface/`

## Contexto

Pedido do designer durante o playtest da fase 3 (T-380), em 2026-11-16: melhorar a marca do monstro selecionado. Hoje é um arco desenhado em código (`EntityView._Draw`, `DrawArc` de 2,5 px em volta do monstro), que some no meio do terreno e dos efeitos.

## Requisitos

1. Uma marca de alvo em pixel art, no chão sob o monstro (aos pés, atrás do sprite), que acompanha o tamanho do monstro (raio) e se lê sobre grama, terra, pedra e no escuro da Cripta. Uma animação leve (pulso ou giro lento) é bem-vinda, sem distrair.
2. **Pelo menos 5 opções**, feitas no PixelLab (modo de maior qualidade) ou desenhadas à mão com o `pixelart_workbench`, no estilo do kit pedra e ouro. Ideias: anel dourado com quatro pontas, colchetes nos cantos, seta sobre a cabeça, círculo rúnico.
3. As opções mostradas **dentro do jogo**, com o monstro selecionado em pelo menos dois terrenos (Campos e Cripta). O designer escolhe.
4. A marca de "mouse sobre a linha da lista de batalha" (T-312, anel branco) continua diferente da marca do alvo.
5. Arte em escala inteira (sem borrar). Sem o arquivo, volta o arco de hoje.
6. Créditos registrados.

## Critérios de aceite

- prancha com as opções em jogo e a escolha do designer registrada;
- capturas com a marca escolhida nos Campos e na Cripta;
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Resultado

**Feito.**

**Marca nova** (`client/scripts/Presentation/EntityView.cs`, `DrawTargetMark`):
- **Onde fica:** a arte `client/assets/art/ui/target_mark.png` (32×32) é desenhada no chão, centrada nos pés (o pivô do sprite). Ela sai antes do sprite, então fica atrás dele.
- **Tamanho:** o diâmetro visado é o maior entre **60 px** e **3× o raio** do corpo. A escala é o inteiro mais próximo disso, sem borrar.
  - Nos monstros comuns, isso dá 2× (64 px).
  - Nos chefes de raio 0,9 (Matriarca, Vigia), 3× (96 px).
  - **Por que não só o raio:** a primeira versão media a marca só pelo raio (2 × raio + 6 px). No rato, de raio 0,3, a marca ficava em 1× e quase toda escondida atrás do desenho. Por isso entrou o mínimo de 60 px.
- **Animação:** pulso leve de transparência, de 0,7 a 1, em 1,6 s, sem girar, para não borrar.
- **Sem o arquivo:** volta o arco dourado de antes.
- **Mouse sobre a linha da lista de batalha:** a marca da T-312 continua o anel branco fino desenhado em código. Ela é diferente da marca do alvo; o código dela não foi mexido.

**Opções** (`create_image_pro`, o modelo de maior qualidade):
- **Referência de estilo:** o `panel.png` do kit pedra e ouro (contorno, detalhe e sombra).
- **Gerações:** quatro pedidos de 32×32, com 64 candidatas cada, por 40 gerações no total. Os temas foram anel com quatro pontas, colchetes, círculo rúnico e anel vermelho com setas.
- **Mostradas:** 7 opções, cada uma numa execução do jogo, com servidor novo a cada vez para os monstros estarem parados.
  - **Nos Campos:** um Rato de Campo a ~18 tiles, fora do alcance de agressão, selecionado com Tab.
  - **Na Cripta:** um Guarda Esquelético do primeiro salão, selecionado com Tab.
- **Pranchas** em `tasks/reports/interface/marca-do-alvo/`:
  - `opcoes-marca-no-jogo.png`: as 7 no jogo, nos dois terrenos, ampliadas 2×;
  - `opcoes-marca-ampliadas.png`: a arte ampliada 4× sobre grama e sobre pedra.
- **Escolha do designer:** opção **1**, anel dourado com quatro pontas.
- **Capturas com a marca escolhida:** `escolhida-campos.png` e `escolhida-cripta.png`.

**Como a Cripta foi testada:** ela é uma instância, e o servidor só aceita nascer em zona aberta. Durante as capturas, o `kind` da `zone.watcher_crypt` em `data/zones.json` foi trocado para `field`. A troca foi desfeita depois e não entrou no commit.
- Por causa dela, as capturas da Cripta mostram o painel do Vigia no alto.

**Ajuste provisório:** a arte de cada opção foi trocada por uma variável de ambiente lida no `EntityView`. Ela foi removida antes do commit.

**Créditos:** linha nova em `CREDITS.md`.

`dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

**Desvios:**
- **Branch:** criada a partir de `docs/correcoes-playtest-v0.3`, onde está o arquivo desta tarefa, e não de `main`.
- **Pasta da arte:** a marca ficou em `client/assets/art/ui/`, junto com o resto do kit.

**Pendências:**
- **Mouse sobre a linha:** não foi visto no jogo junto com a marca nova. Com o monstro selecionado, o anel branco (raio + 9 px) cai por dentro do anel dourado (64 px). Eles continuam diferentes, mas vale olhar no playtest.
- **Chefes:** a marca não foi vista no jogo em chefes nem em monstros grandes (escala 3×).
- **Anel vermelho dos guardas:** na Cripta, os guardas têm um anel vermelho próprio, que aparece nas capturas. Ele se confundiria com as opções vermelhas (6 e 7), mas não com a marca dourada escolhida.
