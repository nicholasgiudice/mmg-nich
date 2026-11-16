# T-382 — Lista de batalha compacta, com miniatura e ícone novo de ataque

- **Ambiente:** Windows
- **Modelo:** Humano + Médio
- **Depende de:** —
- **Libera:** —
- **Áreas que pode alterar:** `client/scripts/Hud/` (`BattleList`, `BattleListPanel`, `Hud`, `HudTheme`), `client/scripts/Presentation/` (só para ler o quadro do sprite), `client/assets/art/ui/`, `tests/Game.Client.Tests/`, `CREDITS.md`, `tasks/reports/interface/`

## Contexto

Pedido do designer durante o playtest da fase 3 (T-380), em 2026-11-16. A lista de batalha (T-312) ocupa muito espaço da tela, os nomes sozinhos não ajudam a reconhecer o monstro, e o "!" de texto que indica que o monstro está atacando o jogador é feio.

## Requisitos

1. **No máximo 5 linhas visíveis.** A ordem continua a da T-312 (alvo atual primeiro, depois por distância), e a lista não rola mais: mostra os 5 primeiros. O título continua mostrando o total ("Batalha (B) · 12"). O Tab continua passando por **todos** os monstros da lista (não só os 5 mostrados).
2. **Miniatura do monstro à esquerda do nome:** o primeiro quadro da animação parada, virado para baixo, do sprite do monstro (`SpriteCatalog`), recortado no conteúdo e desenhado em escala inteira (sem borrar), num quadrado do tamanho da linha. Monstro sem sprite: um quadrado com a inicial do nome.
3. **Ícone novo para "está te atacando"** no lugar do "!" de texto, feito no **PixelLab**, no estilo do kit pedra e ouro:
   - pelo menos 5 opções, no modo de maior qualidade disponível, com o `panel.png` como referência de estilo;
   - as opções mostradas **dentro do jogo**, com a lista aberta, e não soltas (ver o kit em `tasks/reports/interface/kit-pedra-ouro.md`);
   - o designer escolhe; a arte fica em 1× em `client/assets/art/ui/`; sem o arquivo, volta o "!".
4. Nenhuma regra muda: o clique na linha seleciona, o mouse sobre a linha destaca o monstro, B esconde e mostra.
5. Créditos registrados.

## Critérios de aceite

- contra o servidor local (`--world --dev-login --start-zone zone.vigil_fields`): captura com mais de 5 monstros na tela, mostrando 5 linhas com miniatura e o ícone novo; Tab chega a um monstro que não está entre os 5 mostrados;
- prancha com as opções do ícone e a escolha do designer registrada;
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Resultado

**Feito.**

**5 linhas, sem rolar** (`BattleList`, `BattleListPanel`):
- `BattleList.Shown` dá as `ShownRows` (5) primeiras linhas de `Entries`, na ordem da T-312: alvo atual primeiro, depois por distância.
- O painel não tem mais `ScrollContainer`. O título mostra o total da lista ("Batalha (B) · 8").
- O Tab continua andando pela `TabOrder`, que tem **todos** os monstros. Ao escolher um monstro de fora das 5 linhas, ele vira o alvo e sobe para a primeira linha.

**Miniatura** (`MonsterThumbnail`, dentro do `BattleListPanel`):
- Usa o primeiro quadro de `idle`, virado para baixo, do sprite no `SpriteCatalog`, com o mesmo tom (`tint`) do mapa.
- O quadro é recortado no conteúdo (`Image.GetUsedRect`) e desenhado num quadrado de 28 px, com filtro *nearest*.
- A conta fica em `BattleThumbnail.Fit`, sem Godot e com testes.
  - Conteúdo menor que o quadrado é ampliado pelo maior inteiro que cabe.
  - Conteúdo maior é reduzido por um inteiro: ½ no Rato de Campo, ⅓ no lobo.
- Monstro sem sprite: quadrado com a inicial do nome.
- O recorte de cada definição é calculado uma vez e guardado.

**Escolha do designer, miniatura:** monstro inteiro reduzido (A), e não um recorte do topo do quadro em 1× (B). Nos animais virados para baixo, o topo do quadro é o rabo: no rato, o busto mostrava só o rabo. O modo B foi testado no jogo e saiu do código depois da escolha. Prancha: `miniatura-inteira-ou-busto.png`.

**Ícone "está te atacando"** (`client/assets/art/ui/attack_marker.png`, 24×24, desenhado em 1×):
- Fica à direita da linha, no lugar do "!".
- Sem o arquivo, o "!" de texto volta.
- **Geração:**
  - `create_image_pro`, o modelo de maior qualidade, com o `panel.png` do kit pedra e ouro como referência de estilo (contorno, detalhe e sombra);
  - dois pedidos: espadas cruzadas e escudo de aviso;
  - 128 candidatas por 20 gerações.
  - Vários candidatos do escudo vieram partidos ao meio e ficaram de fora.
- **Opções mostradas:** 7, cada uma numa execução do jogo, com a lista aberta e os ratos atacando.
  - `opcoes-icone-no-jogo.png`: as 7 no jogo.
  - `opcoes-icone-ampliadas.png`: as 7 ampliadas 4×, só para ver o desenho.
- **Escolha do designer:** opção **4** (espadas cruzadas, sem fundo).

**Teste contra o servidor local** (`--world --dev-login --start-zone zone.vigil_fields`):
- O personagem anda para a direita a partir do nascimento e junta os ratos de dois grupos.
- **Lista:** 6 ratos na tela, título "Batalha (B) · 6", 5 linhas com miniatura e o ícone novo em todas (`lista-5-linhas.png`).
- **Tab:**
  - verificado com um registro provisório no `Hud`, que imprimia se o novo alvo estava entre as 5 linhas mostradas;
  - com 6 monstros na lista, os 5 primeiros Tabs passaram pelos 5 mostrados;
  - o **6º chegou ao monstro 6, que não estava entre os 5 mostrados**; o 7º voltou ao primeiro;
  - o registro não entrou no commit.
- **Sem mudança:** o código do clique na linha, do destaque no mapa ao passar o mouse e do B não foi mexido. Esses três não foram testados de novo no jogo.

**Testes** (`tests/Game.Client.Tests/BattleListCompactTests.cs`):
- com 8 monstros, só 5 linhas, com o alvo primeiro;
- com poucos monstros, todos aparecem;
- o Tab passa pelos 8;
- a conta da miniatura amplia, reduz e trata conteúdo vazio.

**Créditos:** linha nova em `CREDITS.md`.

`dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

**Desvios:**
- **Branch:** criada a partir de `docs/correcoes-playtest-v0.3`, onde está o arquivo desta tarefa, e não de `main`.
- **Miniatura:** feita como classe interna do `BattleListPanel`, que lê o `SpriteCatalog` por conta própria, como o `InventoryPanel` faz. Nada mudou em `client/scripts/Presentation/`.

**Pendências:**
- **Lobo e humanos:** a miniatura só foi vista no jogo com o Rato de Campo, o único monstro perto do nascimento nos Campos.
  - Lobo (23×58) fica em ⅓ e humanos (30×47) em ½, pela conta testada.
  - Vale olhar no playtest se o lobo ficou pequeno demais.
- **Escala da tela:** em 1920×1080 a interface é ampliada 1,5× pelo Godot, então o ícone em 1× aparece com 36 px. É o mesmo que acontece com o resto do kit.
