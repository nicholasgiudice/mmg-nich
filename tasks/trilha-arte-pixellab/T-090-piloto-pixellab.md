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

Concluída em 2026-11-05. Relatório completo em `tasks/reports/arte-pixellab-piloto.md`.

**Decisão do designer (final):** o PixelLab **não atingiu a qualidade necessária**, e a trilha T-091 em diante não segue como está; o designer vai avaliar outras opções. O caminho até isso:
- a v1 (armadura detalhada) foi rejeitada por "muitos detalhes";
- a v2 (Guerreiro com o sprite LPC como referência de estilo) foi aprovada parada, mas reprovada no jogo: o andar e o ataque ficaram piores que os do LPC, e o Lobo deformava ao andar;
- uma rodada 3, focada em movimento, ataque e tamanho, melhorou o andar, mas o golpe de lado gira o personagem para a câmera em todas as tentativas. Veredito: "ainda ruim".

Detalhes e comparações em movimento no relatório.

**Encerramento (2026-11-05):** a trilha foi encerrada. As tarefas T-091 a T-096, os assets em `client/assets/art/pixellab/`, o tileset `data/maps/tilesets/pixellab_grass_dirt.tsx` e a variante `pixellab` do `sprites.json` foram removidos (recuperáveis pelo commit `49b9720`). O mecanismo `--art=<variante>` do `SpriteCatalog` ficou, para prévias de arte.

**Feito**
- **Guerreiro v2:** `create_character_pro_flash` com o sprite LPC como referência, 64 px. Parado, andar, atacar e morrer em `skeleton-v3`, que mantém escudo e espada. O ataque é o golpe `slash` com o escudo à frente.
- **Lobo Cinzento v2:** pro-flash, modelo `dog`, com o Guerreiro v2 como referência. Parado e andar por modelo pronto; atacar e morrer por texto (v3). Os dois personagens têm 4 direções.
- **Tileset e objeto:** grama × terra de 32 px (standard, 16 tiles) e um carvalho de 96×128 com fundo transparente.
- **Scripts em `client/assets/art/pixellab/tools/`:**
  - `build_sheet.py`: o ZIP do PixelLab vira uma folha em grade com o layout pronto (pivô = média dos pés nas 4 rotações);
  - `tileset_to_tiled.py`: PNG + metadata viram `.tsx` com wangset de canto (`data/maps/tilesets/pixellab_grass_dirt.tsx`);
  - `wang_test_map.py`: gera um mapa de teste escolhendo os tiles pelos cantos.
- **Cliente:** layouts `pixellab_warrior` e `pixellab_grey_wolf` e um bloco `variants.pixellab` no `sprites.json`. `SpriteCatalog` aplica a variante com `--art=pixellab`; sem o argumento, nada muda.
- **Registro:** `manifest.json` com cada chamada paga (ferramenta, prompt, seed, parâmetros, referência, custo e status), incluindo os testes descartados.
- **Capturas** LPC × v1 × v2 lado a lado, amostras de estilo, escala em 1× e uma cena com tileset e carvalho, em `tasks/reports/arte-pixellab-piloto/`.

**Tileset no Tiled:** o PixelLab não entrega `.tsx`, mas a conversão é automática. O próprio Tiled (`--export-tileset`) leu o wangset `corner` com as 16 combinações. Um mapa montado pelos cantos e renderizado pelo `tmxrasterizer` tem todas as emendas certas. Falta o humano pintar uma vez com o pincel de terreno na interface do Tiled.

**Custo:** 209 gerações, conferidas no saldo da conta: 76 nos assets da v2 e 133 em testes e amostras. Sobram 1.791 até 2026-12-05.

**Decisões e desvios**
- Ficou um layout por personagem (`pixellab_<id>`), e não um único `pixellab`: a célula (64 a 96 px) e o número de quadros mudam com o que o PixelLab entrega.
- O mapa de teste do wangset ficou em `tasks/reports/arte-pixellab-piloto/`, porque um `.tmj` em `data/maps/` é carregado como mapa do jogo e quebra os testes.
- Os arquivos `.import` do Godot das novas texturas foram commitados, como os do LPC. Quando uma folha muda de tamanho, é preciso rodar `Godot --headless --path client --import`, ou o jogo usa a importação antiga.
- A branch saiu de `task/trilha-arte-pixellab`, que tem os documentos da trilha e ainda não está em `main`.

**Pendências**
- Decidir o que fazer com a assinatura do PixelLab antes da renovação (2026-12-05).
- O teste do pincel de terreno na interface do Tiled, se o tileset ainda interessar.
- O parado do Guerreiro olhando para a esquerda tem a espada deformada nos quadros 2 a 4. Hoje não aparece no jogo; refazer custa 3 gerações.
- O `idle` do cliente mostra só o primeiro quadro, então a respiração ainda não aparece (T-094).
- O Lobo v2 ficou grande (56×54 px de lado). Se o designer quiser, dá para refazê-lo em 56 px.
- A cena e o quadro de escala em 1× do relatório ainda mostram a v1.
