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

_(preencher ao concluir)_
