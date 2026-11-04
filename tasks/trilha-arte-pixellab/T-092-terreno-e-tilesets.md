# T-092 — Terreno e tilesets (PixelLab)

- **Ambiente:** Windows (Tiled)
- **Modelo:** Humano + Médio
- **Depende de:** T-091
- **Libera:** T-093
- **Áreas que pode alterar:** `client/assets/art/pixellab/`, `client/assets/art/tiles/`, `data/maps/tilesets/`, `data/maps/*.tmj` (só as camadas visuais: `ground`, `decoration`, `walls`, `above`), `client/scripts/` (só se o desenho do mapa precisar), `CREDITS.md`, `design/map-conventions-v0.1.md` (§10)

## Contexto

Os mapas (`data/maps/*.tmj`) usam o terreno do LPC Revised com wangsets de canto (T-052, T-055). A direção de arte v0.2 (T-091) fixou o estilo do PixelLab. A ferramenta de tileset do PixelLab (`create_topdown_tileset`) gera transições Wang entre dois terrenos e pode ser encadeada (por exemplo grama → terra → pedra).

**A camada `collision` nunca muda nesta tarefa:** a colisão é do Core, e os testes de equivalência e de design dependem dela.

## Requisitos

1. **Terrenos por área**, de 32 px, todos com a referência de estilo:
   - Cidade: pedra de rua, terra batida, grama, areia da praça;
   - Campos: grama, terra, estrada, plantação;
   - Floresta: chão de mata, folhas, terra úmida, teia no chão;
   - Ruínas: piso antigo quebrado, terra, entulho;
   - Cripta: pedra escura, cinzas;
   - água e margem, se algum mapa usar.
   Gere só o que os mapas existentes ou planejados (fase 3) vão usar.
2. **Wangsets no Tiled:** cada transição configurada como terreno (canto ou borda, o que o PixelLab entregar), para o designer pintar com o pincel de terreno. Os `.tsx` ficam em `data/maps/tilesets/`, conforme `design/map-conventions-v0.1.md`.
3. **Repintar as camadas visuais** dos mapas existentes (Cidade, Campos, Covil da Matriarca e as arenas dos encontros) com os tilesets novos, sem tocar em `collision`. Se os mapas grandes da fase 3 (T-330, T-342, T-343, T-352) já estiverem em andamento, combine: eles passam a usar os tilesets novos.
4. Tirar os tilesets LPC que deixarem de ser usados e atualizar o `CREDITS.md`.
5. Registrar cada geração no `manifest.json`.

## Critérios de aceite

- os testes de equivalência de mapas (T-051/T-055) e `Game.Design.Tests` sem mudança (a colisão está intacta);
- capturas de cada mapa repintado, aprovadas pelo designer;
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
