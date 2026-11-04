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

_(preencher ao concluir)_
