# T-055 — Mapas: Cidade-Fortaleza e Campos da Vigília

- **Ambiente:** Windows (Tiled)
- **Modelo:** Humano + Médio
- **Depende de:** T-051, T-052, T-053
- **Libera:** fase 2 ("Cidade + Campos online") e fase 3
- **Áreas que pode alterar:** `data/maps/`, `data/` (spawns e encontros dos mapas), `design/`

## Contexto

`design/first-map-v0.1.md` descreve a Cidade-Fortaleza (hub seguro, portões) e os Campos da Vigília (Estrada Leste, Fazenda Abandonada, Clareira dos Caçadores, Acampamento dos Salteadores). A fase 2 precisa dos dois jogáveis online; Floresta, Ruínas e Cripta ficam para a fase 3.

## Requisitos

1. Cidade-Fortaleza no Tiled: ruas, praça, pontos de NPC (marcadores, sem lógica ainda), portões com transição de zona.
2. Campos da Vigília: as quatro subáreas com spawns dos monstros dos níveis 1–4 (`design/progression-v0.1.md` §2) e o Acampamento como encontro de Salteadores.
3. Colisão, spawns e áreas seguindo as convenções da T-051; validação do carregador sem erros.
4. Teste no simulador: um personagem nível 1–4 percorre os Campos (bots) sem ficar preso em colisão.

## Critérios de aceite

- os dois mapas carregam no Core e aparecem no cliente com a arte;
- percurso dos bots sem travar;
- aprovado pelo designer (playtest de exploração).

## Resultado

**Feito, aguardando a aprovação do designer** (Windows, 2026-10-31). `./scripts/check.sh` completo passa. Os dois mapas carregam no Core, aparecem no cliente com a arte e foram testados contra o servidor local (`--world --dev-login`): nascer na Cidade, andar até o portão leste e chegar aos Campos.

**Mapas** (`data/maps/city.tmj` 40×30 e `data/maps/vigil_fields.tmj` 96×64)
- **Colisão** igual à das arenas ASCII da T-214, célula a célula. Assim, nascimentos, portões e surgimentos de `zones.json` e os testes de design dos Campos (T-215) continuam valendo. As ASCII foram para `tests/Game.Sim.Tests/MapFixtures/ascii-arenas.json`, e o teste de equivalência da T-051 passou a cobrir 7 mapas.
- **Visual** montado com os wangsets do LPC Revised, como faria o pincel de terreno do Tiled (detalhes em `design/map-conventions-v0.1.md` §10).
  - Cidade:
    - muralha de tijolo aberta no portão leste;
    - ruas de terra e praça de areia com canteiros;
    - seis prédios com telhado, porta, janelas e placa: Templo (poção), Salão de Treinamento (escudo), Ferreiro (espada), Estalagem (INN), Armazém e Mercado.
  - Campos:
    - Estrada Leste, com ramais à Fazenda, ao Acampamento e à Clareira;
    - Fazenda com cerca de madeira, casa, canteiro arado e flores;
    - Acampamento com paliçada e chão batido;
    - pedras, arbustos e árvores nos obstáculos, e pinheiros na borda sul.
- **Tileset novo:** `buildings` do LPC Revised (OGA-BY 3.0, em `CREDITS.md`).
- **Objetos:**
  - `player_spawn` de nascimento e de chegada;
  - `transitions` com os portões;
  - em `areas`, marcadores dos seis NPCs (classe `npc`, sem lógica), a praça e as quatro subáreas dos Campos.
- **Zonas:** `zones.json` agora aponta para `map.city` e `map.vigil_fields`. As arenas ASCII saíram de `data/arenas.json`, que ficou vazio.

**Teste de percurso** (`tests/Game.Sim.Tests/ZoneMapTests.cs`): do nascimento de cada zona, uma busca a pé alcança todos os surgimentos de monstros e todos os portões, e as `transitions` dos mapas batem com os portões de `zones.json`. É uma busca na grade, não bots andando de verdade. O percurso real com bots já existia (`tests/Game.NetBots --roam`, T-214) e a colisão não mudou.

**Decisões e desvios**
- Os mapas foram gerados por script (fora do repositório), como na T-052, e são o ponto de partida para retocar no Tiled. Os `.tmj` são a fonte daqui em diante.
- `zones.json` continua sendo a fonte dos portões e surgimentos que o servidor usa. Mover para o mapa exige mudar `src/` (fora das áreas): fica para uma tarefa própria. Os mapas já têm as `transitions` iguais, conferidas por teste.
- **Os surgimentos de monstros não foram para a camada `spawns`.** Ficariam duplicados sem que ninguém os lesse.
- Fora das áreas da tarefa:
  - `client/assets/art/tiles/` (imagem do tileset novo e créditos) e `CREDITS.md`;
  - `tests/` (fixtures, contagem de 5 para 7, ids das arenas em `WorldTests` e o teste novo).

**Pendências**
- **Aprovação do designer** (critério de aceite). Pode ser no playtest de exploração ou pelas imagens, com retoques no Tiled se quiser.
- Fazer o servidor ler portões e surgimentos do mapa (tarefa de código).
- Portões norte (Floresta) e oeste (Ruínas) da Cidade: na fase 3, com as zonas novas.
