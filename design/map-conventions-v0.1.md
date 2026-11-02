# Convenções de Mapas (Tiled) — Versão 0.1

Mapas são feitos no **Tiled** e salvos em JSON (`.tmj`). O mesmo arquivo é lido pelo servidor, pelo simulador e pelos testes (colisão, pontos de surgimento, áreas de evento, transições) e desenhado pelo cliente (T-052). Este documento diz como montar um mapa para os dois lados entenderem. Base: `tech/stack-proposal-v0.1.md` (Mapas) e `design/art-direction-v0.1.md` (§7 e §8).

## 1. Arquivos

```text
data/maps/
├── <nome>.tmj           # um mapa por arquivo; id no jogo = map.<nome>
└── tilesets/
    ├── collision.tsx    # tileset da camada de colisão (1 tile)
    ├── collision.png
    └── <tileset>.tsx    # tilesets do LPC Revised (T-052/T-055)
client/assets/art/tiles/ # imagens dos tilesets (art-direction §8)
```

- **Nome do arquivo em snake_case**: `maps/vigil_fields.tmj` vira a arena `map.vigil_fields`. Encontros e zonas apontam para ela pelo id: `"arena": "map.vigil_fields"`.
- Arenas ASCII (`data/arenas.json`) continuam valendo para quem ainda não tem mapa (hoje `arena.city` e `arena.vigil_fields`, que a T-055 converte).
- **Tilesets sempre externos** (`.tsx`), nunca embutidos no mapa. O `.tsx` aponta para a imagem por caminho relativo, ex.: `<image source="../../../client/assets/art/tiles/summer_ground.png" .../>`. Ao trazer um `.tsx` do pacote do LPC Revised, que vem com a imagem na mesma pasta, ajuste esse caminho.
- O Core **não lê** tilesets nem imagens, só o `.tmj`. Mapa com imagem faltando ainda carrega no servidor e nos testes.

## 2. Configuração do mapa no Tiled

| Opção (Mapa > Propriedades do mapa) | Valor |
|---|---|
| Orientação | Ortogonal |
| Largura/altura do tile | **32 × 32 px** (1 tile = 1 unidade de mundo) |
| Infinito | desmarcado |
| Formato da camada de tiles | **CSV** (no `.tmj` vira uma lista de números; fica legível no diff) |
| Ordem de desenho | Direita-baixo |
| Salvar como | JSON (`.tmj`), Tiled 1.10 ou mais novo |

O carregador recusa tile diferente de 32 px, mapa infinito e camada em Base64/compactada, com mensagem dizendo o que mudar.

## 3. Camadas

De baixo para cima. O nome da camada é o que importa (minúsculo, como na tabela). Camadas podem ficar dentro de grupos.

| Camada | Tipo | Obrigatória | Quem usa | Conteúdo |
|---|---|---|---|---|
| `ground` | tiles | para o cliente | cliente | chão: grama, terra, água, estrada. Pintar com o pincel de terreno (wangsets de canto do LPC Revised) |
| `decoration` | tiles | não | cliente | flores, pedras, sombras, detalhes sem colisão |
| `walls` | tiles | não | cliente | muros, troncos, rochas, penhascos, casas: tudo que **parece** bloquear |
| `above` | tiles | não | cliente | o que fica **por cima** dos personagens (copas, telhados). Opcional; T-052 define o desenho |
| `collision` | tiles | **sim** | servidor, testes, cliente (predição) | células bloqueadas para o jogo |
| `spawns` | objetos | não | servidor, testes | pontos de surgimento (§5) |
| `areas` | objetos | não | servidor, testes | áreas de evento: altares, selos, teias (§6) |
| `transitions` | objetos | não | servidor | transições de zona (§7) |

Outras camadas de tiles (ex.: uma segunda `decoration`) são aceitas e ignoradas pelo Core. Uma camada de **objetos** com nome fora da tabela é erro, para pegar erro de digitação (`spawn` em vez de `spawns`).

## 4. Colisão

- **Qualquer tile pintado na camada `collision` bloqueia a célula inteira.** Célula vazia = livre. Use o tile de `tilesets/collision.tsx` (vermelho translúcido). Deixe a camada visível no editor e com opacidade baixa. O cliente não a desenha no jogo.
- A colisão é por tile inteiro. Uma árvore cujo tronco ocupa meio tile bloqueia o tile todo, ou nenhum. Decida pela jogabilidade (por onde o jogador deve passar), não pelo contorno do desenho.
- Toda célula de `walls` que deve bloquear precisa de tile em `collision`. O contrário também vale: colisão invisível só onde for intencional (borda do mapa, por exemplo).
- **Os objetos "Collision" dentro dos tiles do LPC Revised são ignorados pelo jogo.** Eles são formas de física por tile, e a regra do projeto é que física do Godot nunca decide gameplay (`AGENTS.md`). Servem como guia visual ao pintar a camada `collision`. Se pintar a colisão à mão ficar trabalhoso, uma regra de *Automapping* do Tiled (tile com colisão em `walls` → tile em `collision`) pode gerar a camada; isso fica para a T-055 se for preciso.
- Fora do mapa conta como bloqueado. O mapa precisa de pelo menos uma célula livre.

## 5. Pontos de surgimento (`spawns`)

Objetos do tipo **ponto** (ferramenta *Inserir ponto*). Se usar retângulo, vale o centro. O ponto não pode cair numa célula bloqueada nem fora do mapa.

| Classe do objeto | Propriedades | Uso |
|---|---|---|
| `player_spawn` | — | onde um jogador aparece; também é alvo de chegada de transição (§7) |
| `monster_spawn` | `monster` (texto, obrigatório): id do monstro, ex.: `monster.grey_wolf`; `level` (inteiro ≥ 1, opcional) | onde um monstro aparece |

Todo objeto tem **nome único na camada** (ex.: `start`, `wolf_1`). O nome aparece nas mensagens de erro e é como transições e código se referem ao ponto. Monstro desconhecido é erro de carga.

Encontros de teste (`data/encounters.json`) continuam com as posições de jogadores e inimigos no próprio encontro, porque cada encontro é um cenário (classes, níveis, NH). Os pontos do mapa servem às zonas do mundo aberto. A T-055 decide como `zones.json` passa a usá-los.

## 6. Áreas de evento (`areas`)

Retângulos (sem rotação) dentro do mapa. A **classe** do objeto diz o que a área é, e as **propriedades** levam os parâmetros. O Core guarda as propriedades como texto para quem implementar a mecânica.

| Classe (exemplos) | Uso |
|---|---|
| `altar` | altar ativável |
| `seal` | selo do encontro |
| `web` | teia (lentidão) |

Classe vazia é erro. Nome único na camada.

## 7. Transições de zona (`transitions`)

Retângulos de classe `transition`. Entrar no retângulo leva a outra zona.

| Propriedade | Obrigatória | Valor |
|---|---|---|
| `to` | sim | id da zona de destino (`data/zones.json`), ex.: `zone.city` |
| `arrival` | não | nome de um `player_spawn` do mapa de destino. Sem ele, o personagem chega no nascimento da zona |

O carregador confere se a zona existe e, quando a zona de destino usa um mapa do Tiled, se a chegada é um `player_spawn` desse mapa. Hoje os portões das zonas ainda estão em `zones.json` (T-214). A T-055 move os portões para esta camada.

## 8. Coordenadas

- Origem no canto superior esquerdo, y para baixo, como no Tiled e no jogo.
- O carregador divide pixels por 32: um ponto em (112, 48) px fica em (3,5; 1,5) tiles, o centro da célula (3, 1).
- Para cair no centro de uma célula, ligue *Encaixar na grade fina* (Editar > Preferências) ou digite a posição no painel de propriedades.

## 9. Validação

`GameDataLoader` lê `data/**/*.tmj` junto com os JSON (`TiledMapReader`) e acumula todos os erros com o arquivo e o caminho do problema, ex.:

```text
maps/fields.tmj: layers: Camada 'collision' faltando: crie uma camada de tiles com esse nome e pinte as células bloqueadas.
maps/fields.tmj: layers[spawns].objects[wolf_1]: Monstro desconhecido: 'monster.wolf'.
```

Para conferir um mapa editado, rode `./scripts/check.sh --fast` (os testes carregam `data/` inteiro). Os mapas entram na impressão digital do conteúdo: cliente e servidor precisam ter os mesmos.

## 10. Mapas atuais

| Arquivo | Id | Origem | Usado por |
|---|---|---|---|
| `fields.tmj` | `map.fields` | `arena.fields` (ASCII) | `encounter.three_wolves` |
| `camp.tmj` | `map.camp` | `arena.camp` | `encounter.wolf_and_bandit` |
| `farm.tmj` | `map.farm` | `arena.farm` | `encounter.four_rats` |
| `broken_gate.tmj` | `map.broken_gate` | `arena.broken_gate` | `encounter.skeleton_guard` |
| `matriarch_nest.tmj` | `map.matriarch_nest` | `arena.matriarch_nest` | `encounter.forest_matriarch` |
| `city.tmj` | `map.city` | `arena.city` (T-214) | `zone.city` (T-055) |
| `vigil_fields.tmj` | `map.vigil_fields` | `arena.vigil_fields` (T-214) | `zone.vigil_fields` (T-055) |
| `matriarch_lair.tmj` | `map.matriarch_lair` | novo (sem ASCII) | `zone.matriarch_lair` |

Convertidos na T-051 com a mesma grade célula a célula; as ASCII originais ficam em `tests/Game.Sim.Tests/MapFixtures/ascii-arenas.json` para o teste de equivalência. Na T-052, as camadas visuais foram preenchidas automaticamente a partir da colisão, com o tileset `tilesets/terrain_summer.tsx` (LPC Revised): grama variada em `ground`, tufos em `decoration`, arbustos na borda e em blocos grandes, rochas nos obstáculos pequenos em `walls`, e pinheiros na borda de baixo (copa em `above`). É um ponto de partida para retocar no Tiled; a colisão não mudou.

**Cidade e Campos (T-055).** Mesma colisão das arenas ASCII da T-214, célula a célula (estão no mesmo arquivo de fixtures), para não mexer nos pontos de `zones.json` nem nos testes de design dos Campos. O visual foi montado com os wangsets do LPC Revised, como o pincel de terreno do Tiled: Summer Terrain (grama, terra, areia), Fences (cercas), Brick Walls e Flat Roofs do tileset novo `tilesets/buildings.tsx` (muros, fachadas e telhados). Camadas extras: `paths` (canteiro arado, por cima do chão) e `walls_detail` (portas, janelas, placas e telhados, por cima de `walls`).
- Cidade: muralha de tijolo cinza aberta no portão leste, ruas de terra, praça de areia com canteiros, seis prédios (Templo, Salão de Treinamento, Ferreiro, Estalagem, Armazém, Mercado) com porta, janelas e placa.
- Campos: Estrada Leste do portão oeste para o leste, ramais à Fazenda, ao Acampamento e ao sul da Clareira; Fazenda com cerca, casa e canteiro arado; Acampamento com paliçada e chão batido; pedras, arbustos e árvores nos obstáculos.
- **Objetos:** `player_spawn` de nascimento e de chegada (`start`, `from_fields`, `from_city`); `transitions` com os portões; em `areas`, marcadores de NPC (classe `npc`, propriedades `npc`, `name`, `building`; sem lógica ainda), a praça (`plaza`) e as subáreas dos Campos (`subarea`, o nome é o da subárea).
- **`zones.json` continua sendo a fonte dos portões e dos surgimentos de monstros.** O servidor ainda lê os portões de lá (T-214); as `transitions` dos mapas repetem os mesmos retângulos e chegadas, e o teste `ZoneMapTests` garante que batem. Passar o servidor a ler os portões e surgimentos do mapa é mudança de código (`src/`), fica para uma tarefa própria.

**Covil da Matriarca.** Zona aberta para testar a Matriarca online, sem instância: portão leste dos Campos (borda aberta em x=95, y=44–47; chegada `from_lair`) leva a uma antessala estreita e, por uma passagem, ao covil (40 × 32 tiles livres, seis afloramentos de rocha para se esconder da investida). A Matriarca (nível 8) nasce no fundo do covil (50,5; 16,5), a 46 tiles da chegada — fora do alcance de agressão dela (40) —, e renasce 5 min depois de morrer. O grupo se reúne na antessala; quem avança além de x≈10 já entra na linha de visão dela pela passagem. Mesmos tiles do ninho (`matriarch_nest.tmj`); o arquivo foi gerado por script, como as arenas da T-052, e pode ser retocado no Tiled.

No cliente (T-052): as camadas de tiles são desenhadas na ordem do arquivo, menos `collision`. `above` vai por cima das unidades e fica transparente num raio de 2 tiles em volta do jogador. O cliente acha a imagem do `.tsx` pelo nome do arquivo em `client/assets/art/tiles/`. Espelhamento horizontal e vertical são desenhados; a rotação de 90° (bit diagonal) não.
