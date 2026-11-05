# T-090 — Piloto: arte gerada pelo PixelLab no jogo

- **Ambiente:** Windows
- **Modelo:** Humano + Médio
- **Depende de:** humano com conta paga do PixelLab (Tier 1 ou superior) e o MCP configurado no Claude Code
- **Libera:** T-091
- **Áreas que pode alterar:** `client/assets/art/pixellab/` (novo), `client/assets/art/sprites.json`, `client/scripts/Presentation/` (só o necessário para o formato novo de folha), `data/maps/tilesets/` (só um tileset de teste), `tasks/reports/`

## Contexto

Hoje a arte vem de pacotes LPC (`design/art-direction-v0.1.md`, trilha `tasks/trilha-arte/`). O designer quer refazer a arte com o **PixelLab** (https://www.pixellab.ai/docs). Esse gerador de pixel art faz personagens com 4 ou 8 direções e animação a partir de texto, tilesets Wang de terreno (16 ou 32 px) e objetos de mapa com fundo transparente. Ele tem um servidor MCP para o Claude Code: `create_character`, `animate_character`, `create_topdown_tileset`, `create_map_object`, `create_image_pro`...

Pelo FAQ do PixelLab, as imagens podem ser usadas comercialmente. A única restrição é não usá-las para treinar modelos.

**Planos** (https://www.pixellab.ai/#checkout, consultado em 2026-11-04):

| Plano | Preço | Gerações por mês | Observação |
|---|---|---|---|
| Free | — | 40 rápidas, depois 5 por dia | inclui API e MCP; **sem ferramentas de mapa** (sem tileset) |
| Tier 1 | US$ 12 | 2.000 | ferramentas de animação e de mapa; imagens até 320×320 |
| Tier 2 | US$ 24 | 5.000 | fila prioritária, 10 tarefas em paralelo, imagens até 512×512 |
| Tier 3 | US$ 50 | 10.000 | 20 tarefas em paralelo |

Licença em todos os planos: "o usuário detém os direitos das gerações", exceto para treinar modelos. Algumas ferramentas gastam mais de uma geração por imagem. Para o piloto, o **Tier 1** basta; o Free não gera tileset.

**Antes de refazer tudo, este piloto responde:** a qualidade e a consistência servem? O formato encaixa no cliente (`sprites.json`, T-053) e no Tiled (wangsets, T-051/T-052)? Quanto custa em gerações?

**Configuração (humano, uma vez):** o token fica só na máquina, nunca no repositório.

```bash
claude mcp add pixellab https://api.pixellab.ai/mcp -t http -H "Authorization: Bearer SEU_TOKEN"
```

## Requisitos

1. **Guerreiro:** personagem com 4 direções, vista "low top-down", com a altura do LPC (o corpo com cerca de 1,5 tile, em quadro de 64×64; `design/art-direction-v0.1.md` §3). Animações: parado, andar, atacar e morrer. Se "morrer" não sair bem por texto, registre e tente outra descrição ou a ferramenta de esqueleto.
2. **Lobo Cinzento:** quadrúpede, 4 direções, parado, andar e atacar.
3. **Tileset de grama com terra**, de 32 px, para a vista de cima. Confira se o arquivo exportado abre no Tiled como **wangset** (terreno de canto) e se o pincel de terreno funciona. Se não abrir direto, descreva a conversão necessária.
4. **Um objeto de mapa** (uma árvore ou uma pedra grande) com fundo transparente, casando com o tileset.
5. **No jogo:** um layout novo `pixellab` no `sprites.json` para o formato de folha que o PixelLab entrega. Monte a folha a partir dos quadros, se precisar, com um script em `client/assets/art/pixellab/tools/` ou um passo documentado. O Guerreiro e o Lobo devem aparecer nos Campos com o sprite novo, por uma opção de execução (`--art=pixellab`), sem apagar o LPC.
6. **Registro de cada geração** em `client/assets/art/pixellab/manifest.json`: ferramenta, descrição (prompt), seed, tamanho, referência de estilo usada e custo em gerações. Assim um asset pode ser refeito.
7. **Prompts** descrevem o que queremos (por exemplo "medieval warrior, chainmail, sword, dark outline"). **Nunca** peça o estilo de um jogo ou de um artista específico (por exemplo "Tibia style"). A direção é "simples como o Tibia, mas não idêntico".
8. **Relatório** `tasks/reports/arte-pixellab-piloto.md`, com:
   - capturas lado a lado (LPC e PixelLab) nos Campos;
   - legibilidade em zoom 1×;
   - consistência entre o Guerreiro e o Lobo;
   - o que funcionou e o que não funcionou;
   - gerações gastas e uma estimativa para refazer todo o jogo (todas as classes, todos os monstros, inclusive os da fase 3, os tilesets e os prédios).

## Critérios de aceite

- o Guerreiro e o Lobo animados no jogo com `--art=pixellab`, e o modo normal (LPC) inalterado;
- o resultado do teste do tileset no Tiled;
- o relatório com a **decisão do designer**: seguir com a trilha (T-091 em diante), ajustar, ou desistir e voltar ao LPC;
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Resultado

Concluída em 2026-11-05, exceto a **decisão do designer**, que está pendente no relatório. Relatório completo em `tasks/reports/arte-pixellab-piloto.md`.

**Feito**
- Guerreiro (v3, 64 px) com parado, andar, atacar e morrer, e Lobo Cinzento (pro-flash, modelo `dog`, com o Guerreiro como referência de estilo) com parado, andar, atacar e morrer. Os dois têm 4 direções. A animação de morrer saiu bem nos dois: modelo `falling-back-death` no Guerreiro e texto v3 no Lobo.
- Tileset grama × terra de 32 px (standard, 16 tiles) e um carvalho de 96×128 com fundo transparente.
- `client/assets/art/pixellab/tools/build_sheet.py`: o ZIP do PixelLab vira uma folha em grade com o layout pronto (pivô = média dos pés nas 4 rotações; `headPx` medido).
- `tools/tileset_to_tiled.py`: PNG + metadata do PixelLab viram `.tsx` com wangset de canto (`data/maps/tilesets/pixellab_grass_dirt.tsx`).
- `tools/wang_test_map.py`: gera um mapa de teste escolhendo os tiles pelos cantos.
- `sprites.json`: layouts `pixellab_warrior` e `pixellab_grey_wolf`, e um bloco `variants.pixellab`. `SpriteCatalog` aplica a variante com `--art=pixellab`; sem o argumento, nada muda.
- `manifest.json` com as 12 chamadas pagas: ferramenta, prompt, seed, parâmetros, referência e custo. O teste de escala em 52 px está só no relatório.
- Capturas lado a lado (início e combate), um quadro em 1× real e uma cena só com arte PixelLab, em `tasks/reports/arte-pixellab-piloto/`.

**Tileset no Tiled:** o PixelLab não entrega `.tsx`, mas a conversão é automática. O próprio Tiled (`--export-tileset`) leu o wangset `corner` com as 16 combinações. Um mapa montado pelos cantos e renderizado pelo `tmxrasterizer` tem todas as emendas certas. Falta o humano pintar uma vez com o pincel de terreno na interface do Tiled.

**Custo:** 44 gerações, conferidas no saldo da conta. A estimativa para refazer o jogo inteiro é de ~1.500 gerações, que cabem em um mês do Tier 1.

**Decisões e desvios**
- Ficou um layout por personagem (`pixellab_<id>`), e não um único `pixellab`: a célula (64 a 84 px) e o número de quadros mudam com o que o PixelLab entrega.
- O Guerreiro em 64 px tem 58 px de altura (1,8 tile). Um teste em `size=52` deu 48 px (1,5 tile, como o LPC). Ele não foi animado: a recomendação para a trilha é `size=52`.
- As animações por modelo pronto apagam o escudo do Guerreiro. Para a T-094, sugiro `skeleton-v3`.
- O mapa de teste do wangset ficou em `tasks/reports/arte-pixellab-piloto/`, porque um `.tmj` dentro de `data/maps/` é carregado como mapa do jogo e quebra os testes.
- A branch saiu de `task/trilha-arte-pixellab`, que tem os documentos da trilha e ainda não está em `main`.

**Pendências**
- A decisão do designer, no fim do relatório.
- O teste do pincel de terreno na interface do Tiled.
- O `idle` do cliente mostra só o primeiro quadro, então a respiração gerada ainda não aparece (T-094).
