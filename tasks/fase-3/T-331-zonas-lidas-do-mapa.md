# T-331 — Zonas lidas do mapa: portões, surgimentos e NPCs no Tiled

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** T-301
- **Libera:** T-321, T-330, T-342, T-343, T-350
- **Áreas que pode alterar:** `src/Game.Core/Definitions/Zones.cs`, `src/Game.Core/Spatial/MapObjects.cs`, `src/Game.Data/`, `src/Game.Server/Zones/`, `data/zones.json`, `data/maps/*.tmj` (só as camadas de objetos), `data/schemas/`, `design/map-conventions-v0.1.md`, testes correspondentes

## Contexto

A fase 3 traz mapas bem maiores (pedido do playtest, `tasks/reports/playtest-v0.2.md`, P6) e três áreas novas. Hoje os portões e os pontos de surgimento ficam em `data/zones.json`, com coordenadas digitadas à mão, e só **conferidos** contra as `transitions` do mapa (T-055). Com mapas de 200 tiles, editar coordenadas em JSON fica inviável: o designer precisa pôr portões, monstros e NPCs **no Tiled**.

As convenções (`design/map-conventions-v0.1.md`) já preveem as camadas `spawns` (`player_spawn`, `monster_spawn` com `monster` e `level`), `areas` e `transitions`. O `TiledMapReader` (T-051) já lê essas camadas para `ArenaDefinition.Spawns/Areas/Transitions`, mas ninguém usa os surgimentos.

## Requisitos

1. **Fonte única no mapa** para o que é posição:
   - portões = objetos de `transitions` (`to`, `arrival`);
   - nascimento da zona = `player_spawn` com o nome `spawn` (ou propriedade `default: true`; escolha uma e documente);
   - pontos de surgimento = objetos `monster_spawn` com as propriedades `monster`, `level`, `count`, `radius` e `respawn` (segundos). A subárea (`area`) vem do retângulo de `areas` que contém o ponto;
   - NPCs = objetos com classe `npc` e propriedades `npc` (id de definição) e, opcionalmente, `shop`. Esta tarefa só lê e valida esses objetos; a lógica de NPC é da T-321.
2. `data/zones.json` fica só com o que não é posição: id, nome, tipo, arena, descanso, tempo do corpo e faixa de nível exibida. Os campos `spawn`, `gates` e `monsterSpawns` saem. O validador e o schema acompanham.
3. Migrar os dados de hoje (`zone.city`, `zone.vigil_fields`, `zone.matriarch_lair`) para os objetos dos mapas, **sem mudar nenhuma posição**: o teste de equivalência deve provar que a `ZoneDefinition` montada a partir do mapa é igual à de antes.
4. Validação com arquivo e nome do objeto, em português: monstro desconhecido, ponto na parede, `count` ≤ 0, `to` apontando para zona inexistente, chegada que não é um `player_spawn` do destino, zona segura com `monster_spawn`, zona sem nascimento.
5. Atualizar `design/map-conventions-v0.1.md` com as propriedades novas e um exemplo.
6. `FieldsDesignTests` (T-215) e o `ZoneMapTests` (T-055) continuam montando os grupos a partir da zona, agora vinda do mapa.

## Critérios de aceite

- teste: a zona carregada do mapa é igual à de antes (portões, nascimento e surgimentos);
- os erros de validação do requisito 4 cobertos por teste;
- `Game.Design.Tests` com os mesmos números;
- bots `--roam` andando entre as três zonas sem quedas;
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
