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
| `spawns` | objetos | não (sim em mapa de zona) | servidor, testes | nascimento e pontos de surgimento (§5) |
| `areas` | objetos | não | servidor, testes | áreas de evento, subáreas e NPCs (§6) |
| `transitions` | objetos | não | servidor | transições de zona = portões (§7) |

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
| `player_spawn` | — | onde um jogador aparece; também é alvo de chegada de transição (§7). Num mapa de zona, o chamado **`spawn`** é o **nascimento da zona** (obrigatório) |
| `monster_spawn` | veja a tabela abaixo | ponto de surgimento de monstros de uma zona |

Propriedades do `monster_spawn` (T-331):

| Propriedade | Tipo no Tiled | Obrigatória | Padrão | Valor |
|---|---|---|---|---|
| `monster` | string | sim | — | id do monstro, ex.: `monster.grey_wolf` |
| `level` | int | não | o do monstro | nível (≥ 1) |
| `count` | int | não | 1 | quantos monstros o ponto mantém (≥ 1) |
| `radius` | float | não | 0 | raio em tiles, em volta do ponto, onde eles nascem (≥ 0) |
| `respawn` | float | não | 30 | segundos até renascer depois de morrer (> 0) |

Todo objeto tem **nome único na camada** (ex.: `spawn`, `wolf_1`). O nome aparece nas mensagens de erro e é como transições e código se referem ao ponto. Monstro desconhecido é erro de carga.

**Ordem.** Os `monster_spawn` valem na ordem do arquivo (a ordem em que foram criados no Tiled). Cada ponto é um grupo de aggro (puxar um monstro puxa os do mesmo ponto, T-301). Uma matilha de 3 lobos é **um** ponto com `count` 3, não três pontos.

**Subárea.** O ponto pertence à subárea do primeiro retângulo de classe `subarea` da camada `areas` que o contém (§6). O nome da subárea aparece para o jogador e é usado pelos testes de design (T-215). Ponto fora de qualquer subárea fica sem subárea.

Exemplo (como o Tiled salva; matilha de 3 lobos nível 3 que renasce em 40 s):

```json
{
 "id": 15, "name": "grey_wolf_3", "type": "monster_spawn", "point": true,
 "x": 1744, "y": 1488, "width": 0, "height": 0, "rotation": 0,
 "properties": [
  { "name": "monster", "type": "string", "value": "monster.grey_wolf" },
  { "name": "level", "type": "int", "value": 3 },
  { "name": "count", "type": "int", "value": 3 },
  { "name": "radius", "type": "float", "value": 1.5 },
  { "name": "respawn", "type": "float", "value": 40 }
 ]
}
```

Encontros de teste (`data/encounters.json`) continuam com as posições de jogadores e inimigos no próprio encontro, porque cada encontro é um cenário (classes, níveis, NH). Os pontos do mapa servem às zonas do mundo aberto (§11).

## 6. Áreas de evento (`areas`)

Retângulos (sem rotação) dentro do mapa. A **classe** do objeto diz o que a área é, e as **propriedades** levam os parâmetros. O Core guarda as propriedades como texto para quem implementar a mecânica.

| Classe (exemplos) | Uso |
|---|---|
| `altar` | altar ativável |
| `seal` | selo do encontro |
| `web` | teia (lentidão) |
| `subarea` | subárea de uma zona (o **nome** do objeto é o nome da subárea, ex.: `Fazenda Abandonada`); dá a subárea aos `monster_spawn` dentro dela (§5) |
| `npc` | NPC de uma zona (T-331). Propriedades: `npc` (obrigatória, id da definição, ex.: `npc.smith`), `shop` (opcional, id da loja, ex.: `shop.smith`); outras (ex.: `name`, `building`) ficam como texto. Posição = centro do retângulo. A lógica de NPC é da T-321 |

Classe vazia é erro. Nome único na camada. `npc` sem a propriedade `npc`, ou com id fora do padrão (`npc` e `shop` em snake_case com namespace por ponto), é erro.

## 7. Transições de zona (`transitions`)

Retângulos de classe `transition`. Entrar no retângulo leva a outra zona.

| Propriedade | Obrigatória | Valor |
|---|---|---|
| `to` | sim | id da zona de destino (`data/zones.json`), ex.: `zone.city` |
| `arrival` | não | nome de um `player_spawn` do mapa de destino. Sem ele, o personagem chega no nascimento da zona |

O carregador confere se a zona existe e se a chegada é um `player_spawn` do mapa de destino. Desde a T-331 **estas transições são os portões da zona** (§11). Também é erro: o nascimento da zona (`spawn`) dentro de uma transição do próprio mapa, e a chegada dentro de uma transição do destino (o personagem voltaria na hora).

## 8. Coordenadas

- Origem no canto superior esquerdo, y para baixo, como no Tiled e no jogo.
- O carregador divide pixels por 32: um ponto em (112, 48) px fica em (3,5; 1,5) tiles, o centro da célula (3, 1).
- Para cair no centro de uma célula, ligue *Encaixar na grade fina* (Editar > Preferências) ou digite a posição no painel de propriedades.

## 9. Validação

`GameDataLoader` lê `data/**/*.tmj` junto com os JSON (`TiledMapReader`) e acumula todos os erros com o arquivo e o caminho do problema, ex.:

```text
maps/fields.tmj: layers: Camada 'collision' faltando: crie uma camada de tiles com esse nome e pinte as células bloqueadas.
maps/fields.tmj: layers[spawns].objects[wolf_1]: Monstro desconhecido: 'monster.wolf'.
maps/vigil_fields.tmj: layers[spawns].objects[rats]: Propriedade 'count' (quantos monstros) deve ser um inteiro ≥ 1 (valor: '0').
maps/city.tmj: layers[spawns].objects[rat_1]: Zona segura 'zone.city' não tem monstros: tire este monster_spawn.
maps/lower_forest.tmj: layers[spawns]: Zona 'zone.lower_forest' sem nascimento: crie um player_spawn chamado 'spawn'.
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
- **Objetos:** `player_spawn` de nascimento e de chegada (`spawn`, desde a T-331; antes `start`; `from_fields`, `from_city`); `transitions` com os portões; em `areas`, marcadores de NPC (classe `npc`, propriedades `npc`, `name`, `building`; sem lógica ainda), a praça (`plaza`) e as subáreas dos Campos (`subarea`, o nome é o da subárea).
- ~~`zones.json` continua sendo a fonte dos portões e dos surgimentos de monstros.~~ Desde a T-331 o mapa é a fonte única (§11).

**Covil da Matriarca.** Zona aberta para testar a Matriarca online, sem instância: portão leste dos Campos (borda aberta em x=95, y=44–47; chegada `from_lair`) leva a uma antessala estreita e, por uma passagem, ao covil (40 × 32 tiles livres, seis afloramentos de rocha para se esconder da investida). A Matriarca (nível 8) nasce no fundo do covil (50,5; 16,5), a 46 tiles da chegada — fora do alcance de agressão dela (40) —, e renasce 5 min depois de morrer. O grupo se reúne na antessala; quem avança além de x≈10 já entra na linha de visão dela pela passagem. Mesmos tiles do ninho (`matriarch_nest.tmj`); o arquivo foi gerado por script, como as arenas da T-052, e pode ser retocado no Tiled.

No cliente (T-052): as camadas de tiles são desenhadas na ordem do arquivo, menos `collision`. `above` vai por cima das unidades e fica transparente num raio de 2 tiles em volta do jogador. O cliente acha a imagem do `.tsx` pelo nome do arquivo em `client/assets/art/tiles/`. Espelhamento horizontal e vertical são desenhados; a rotação de 90° (bit diagonal) não.

## 11. Zonas do mundo aberto (T-331)

O mapa do Tiled é a **fonte única** de tudo que é posição numa zona. O `data/zones.json` só tem o resto:

```json
{ "id": "zone.vigil_fields", "name": "Campos da Vigília", "arena": "map.vigil_fields", "kind": "field", "corpse": 5, "levels": { "min": 1, "max": 4 } }
```

| Em `zones.json` | Do mapa (`arena`) |
|---|---|
| `id`, `name`, `kind` (`safe`/`field`), `arena` (precisa ser um mapa do Tiled), `restPerSecond` (só `safe`), `corpse` (segundos), `levels` (faixa de nível mostrada, opcional) | nascimento = `player_spawn` chamado `spawn` (§5); portões = `transitions` (§7); surgimentos = `monster_spawn` (§5); NPCs = áreas `npc` (§6) |

`spawn`, `gates`, `monsterSpawns` e `npcs` no `zones.json` são erro de carga. Erros da zona saem com o arquivo do mapa e o nome do objeto (§9): monstro desconhecido, ponto na parede ou fora do mapa, `count`/`radius`/`respawn`/`level` inválidos, `to` para zona inexistente, chegada que não é `player_spawn` do destino, zona segura com `monster_spawn`, zona sem `spawn`.

Para criar uma zona nova: desenhe o mapa (§2–§4), ponha o `player_spawn` `spawn`, as `transitions` com `to`/`arrival` (e os `player_spawn` de chegada do outro lado), os `monster_spawn` e as `subarea`; depois uma linha em `zones.json`. `./scripts/check.sh --fast` confere tudo.
