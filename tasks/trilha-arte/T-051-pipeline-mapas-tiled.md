# T-051 — Pipeline de mapas: Tiled → Core

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** —
- **Libera:** T-052, T-055
- **Áreas que pode alterar:** `src/Game.Core/Spatial/`, `src/Game.Data/`, `data/maps/` (novo), `tests/`, `design/` (convenções)

## Contexto

`tech/stack-proposal-v0.1.md` (Mapas): **Tiled** é a fonte de verdade, exportando JSON; o servidor e os testes leem colisão, spawns e áreas de evento do mesmo arquivo que o cliente desenha. Hoje as arenas são ASCII em `data/arenas.json`. Esta é a tarefa "convenções de camadas do Tiled e carregador de mapa no Core" da fase 3, antecipada.

## Requisitos

1. Convenções (documento em `design/map-conventions-v0.1.md`): camadas de tiles (chão, decoração, paredes), camada de **colisão**, camada de objetos para **spawns** (jogadores, monstros com id), **áreas de evento** (altares, selos, teias) e **transições de zona**; tile de 32 px = 1 unidade.
2. Carregador em `Game.Data` (JSON do Tiled → `GridMap` + spawns + áreas), validado com mensagens claras (camada faltando, monstro desconhecido).
3. Encontros podem apontar para um mapa do Tiled em vez de uma arena ASCII; as 5 arenas atuais convertidas para `data/maps/*.tmj` com o mesmo resultado no simulador (hash de estado ou taxas iguais).
4. Testes do carregador e da conversão.

## Critérios de aceite

- arenas atuais rodando a partir de mapas do Tiled, com os testes de design iguais;
- `./scripts/check.sh` completo passa.

## Resultado

Feito na VPS, na branch `task/T-051-pipeline-mapas-tiled`. A `main` local foi atualizada antes (fast-forward de `origin/main`, que trazia a T-050) para usar o §7 e o §8 de `design/art-direction-v0.1.md`.

**O que foi feito**
- `design/map-conventions-v0.1.md`: arquivos e ids (`data/maps/<nome>.tmj` → arena `map.<nome>`), configuração do Tiled (ortogonal, 32 px, não infinito, camada em CSV, tilesets externos), camadas `ground`/`decoration`/`walls`/`above` (visuais), `collision` (tiles, obrigatória), `spawns`, `areas` e `transitions` (objetos), classes e propriedades dos objetos, coordenadas e validação.
- Compatível com o LPC Revised configurado para o Tiled: tiles de 32 px. Os wangsets de canto são usados nas camadas visuais e o Core os ignora. Os `.tsx` ficam em `data/maps/tilesets/` e apontam para as imagens em `client/assets/art/tiles/` por caminho relativo. Os objetos "Collision" dentro dos tiles **não** decidem colisão (regra "física do Godot nunca decide gameplay"). Servem de guia para pintar a camada `collision`, e o *Automapping* fica como opção para a T-055.
- `src/Game.Core/Spatial/MapObjects.cs`: `MapSpawn` (`player_spawn`/`monster_spawn` com `monster` e `level`), `MapArea` (classe + retângulo + propriedades) e `MapTransition` (retângulo, `to`, `arrival`).
- `src/Game.Data/TiledMapReader.cs`: converte o `.tmj` numa `ArenaDefinition` (grade em `Lines`, mais `Source`, `Spawns`, `Areas` e `Transitions`). Qualquer gid ≠ 0 em `collision` bloqueia, inclusive com os bits de espelhamento. Lê camadas dentro de grupos e aceita a classe do objeto em `type` (Tiled 1.8/1.10+) ou em `class` (1.9). Erros com arquivo e caminho, em português: camada faltando, camada de objetos desconhecida, tile ≠ 32 px, mapa infinito, camada em Base64, tamanho errado, ponto na parede ou fora do mapa, forma que não é retângulo, nome repetido, propriedade faltando, nome de arquivo fora de snake_case.
- `GameDataLoader`: lê `*.tmj` junto com os `*.json`. O novo `EnumerateContentFiles` passou a ser usado também pelo simulador e por `RealContent` dos testes. Os mapas entram na impressão digital do conteúdo. O `GameDataValidator` confere as referências: monstro desconhecido, zona de destino inexistente e chegada que não é `player_spawn` do mapa de destino.
- As 5 arenas dos encontros viraram `data/maps/{fields,camp,farm,broken_gate,matriarch_nest}.tmj`, só com a camada `collision` preenchida e as camadas visuais vazias para a T-052/T-055. Também há um tileset `tilesets/collision.tsx` com `collision.png`, só para o editor. Os encontros apontam para `map.*`. As 5 arenas ASCII saíram de `arenas.json` e ficaram em `tests/Game.Sim.Tests/MapFixtures/ascii-arenas.json`. `arena.city` e `arena.vigil_fields` continuam ASCII (a T-055 converte).

**Testes**
- `tests/Game.Data.Tests/TiledMapLoaderTests.cs` (13): conversão completa, colisão, grupos, todos os erros acima e leitura por diretório com impressão digital.
- `tests/Game.Sim.Tests/MapConversionTests.cs` (7): todo encontro usa mapa do Tiled; grade igual à ASCII célula a célula; e, nos 5 encontros, o simulador dá o mesmo hash de estado, os mesmos ticks e o mesmo resultado com o mapa e com a ASCII (4 seeds cada, `kite+dodge`). Conferi que o teste falha ao mudar uma célula de um mapa.
- Simulador antes/depois (`--encounter all --strategy rotation,kite+dodge --runs 60 --per-run`): JSON idêntico, exceto `wallSeconds`. Os testes de design (83) passam sem mudança de números. `./scripts/check.sh` completo passa.

**Decisões**
- O id do mapa vem do nome do arquivo (`map.<nome>`), e o encontro aponta para ele no mesmo campo `arena`. Assim não muda o esquema de `encounters.json` nem nenhum consumidor de `ArenaDefinition` (servidor, cliente, simulador).
- As posições de jogadores e inimigos dos encontros continuam em `encounters.json`, porque cada encontro é um cenário com classe, nível e NH. Os pontos de surgimento do mapa servem às zonas, e a T-055 decide como `zones.json` passa a usá-los e move os portões para `transitions`.
- Só aceita camada em CSV, para os diffs ficarem legíveis e o leitor ficar simples.

**Desvios (fora das áreas listadas, mínimo necessário)**
- `src/Game.Core/Definitions/Definitions.cs`: `ArenaDefinition` ganhou `Source`, `Spawns`, `Areas` e `Transitions` (com padrão vazio).
- `client/MmoRpgClient.csproj` (copia também `data/**/*.tmj`) e `client/scripts/Data/ContentLoader.cs` (lê `res://data/maps/*.tmj`, com o mesmo nome relativo do servidor para a impressão digital bater). Sem isso o cliente não carregaria os encontros. `dotnet build client/` passa na VPS; não testado no editor.
- `tests/Game.Design.Tests/FieldsDesignTests.cs`: `arena.fields` → `map.fields`.

**Pendências**
- Na exportação do cliente (Godot), o filtro de arquivos não-recurso precisa incluir `data/*.tmj` além de `*.json` (`export_presets.cfg` não está no Git). Conferir na T-052 ou na próxima exportação.
- Abrir um `.tmj` no Tiled no Windows para confirmar que o editor lê os arquivos gerados aqui (foram escritos no formato do Tiled 1.10/1.11, sem o editor à mão).
