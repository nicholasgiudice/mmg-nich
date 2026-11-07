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

**Feito (branch `task/T-331-zonas-lidas-do-mapa`):**

- **Fonte única no mapa.** O novo `src/Game.Data/ZoneComposer.cs` monta, na carga, a parte de posição de cada `ZoneDefinition` a partir do mapa do Tiled da zona:
  - nascimento = `player_spawn` chamado **`spawn`** (escolhi o nome, não a propriedade `default`);
  - portões = objetos de `transitions`, com a chegada resolvida no `player_spawn` de nome `arrival` do mapa de destino (sem `arrival`, o `spawn` do destino);
  - surgimentos = objetos `monster_spawn`, na ordem do arquivo, que é o grupo de aggro da T-301;
  - NPCs = áreas de classe `npc`, em `ZoneDefinition.Npcs` (`NpcPlacementDefinition`: nome, `npc`, `shop`, centro e demais propriedades). A lógica de NPC fica para a T-321.
- **Leitor do Tiled.** O `monster_spawn` ganhou `count` (padrão 1), `radius` (0) e `respawn` (segundos, padrão 30) em `MapSpawn`. A subárea do ponto é o primeiro retângulo `subarea` de `areas` que o contém. As áreas `npc` exigem `npc` com id válido, e `shop` é opcional, também com id válido.
- **`data/zones.json`** ficou só com id, nome, tipo, arena, descanso, corpo e a faixa de nível nova `levels` (`LevelRangeDefinition`; Campos 1–4, Covil 8–8, Cidade sem faixa).
  - O `GameJson` tira `spawn`, `gates`, `monsterSpawns` e `npcs` do contrato da zona, então esses campos no JSON viram erro de carga.
  - O schema e o `ValidateZone` acompanham. O que era posição passou para o leitor e para o compositor.
- **Migração sem mudar posições.**
  - Nos três mapas, o `player_spawn` `start` virou `spawn`.
  - Os 11 pontos dos Campos e o da Matriarca entraram como `monster_spawn` no formato do Tiled (script único, mesma ordem e mesmos números), com `nextobjectid` atualizado.
  - O `zones.json` antigo ficou em `tests/Game.Data.Tests/Fixtures/zones-before-t331.json`.
- **Validação** com arquivo do mapa e nome do objeto, em português:
  - monstro desconhecido, ponto na parede ou fora do mapa;
  - `count` ≤ 0, `radius` < 0, `respawn` ≤ 0, `level` < 1;
  - `to` para zona inexistente, chegada que não é `player_spawn` do destino;
  - zona segura com `monster_spawn`;
  - zona sem `spawn`, ou com arena ASCII;
  - nascimento dentro de uma transição, chegada dentro de uma transição do destino;
  - NPC sem `npc` ou com id inválido.
- **Testes:**
  - `ZoneMapEquivalenceTests`: para as três zonas, nascimento, portões e surgimentos montados do mapa são iguais aos da fixture, mais os NPCs da Cidade.
  - `ZoneLoaderTests` reescrito com mapas pequenos do Tiled: carga completa e cada erro do requisito 4.
  - `TiledMapLoaderTests` ajustado: as zonas de apoio agora usam um mapa com `spawn`.
  - `ZoneMapTests` (T-055) e `FieldsDesignTests` (T-215) continuam montando a partir da zona, agora vinda do mapa, e passam sem mudança de números.
- **Documentação:** `design/map-conventions-v0.1.md` ganhou as propriedades do `monster_spawn` com exemplo, `subarea` e `npc` em §6, as regras de §7 e a seção nova §11 sobre zonas do mundo aberto.
- **Bots de rede.** Antes, o `--roam` sempre usava o primeiro portão e nunca chegava ao Covil. Agora usa o portão seguinte a cada visita à zona, e o resumo mostra as zonas visitadas.
  - 8 bots por 120 s (servidor Release `--world --dev-login`): 81 trocas de zona pelas três zonas (15 entradas no Covil), 2 renascimentos, 0 quedas e nenhum erro no log.
- `./scripts/check.sh` completo passa.

**Decisões:**

- O nascimento é o `player_spawn` chamado `spawn`. É o mais simples de ver no Tiled e não precisa de propriedade booleana.
- NPCs continuam na camada `areas` (retângulos de classe `npc`), onde a T-055 já os tinha posto. Também continuam em `ArenaDefinition.Areas`, e a zona ganha a lista tipada.
- Subárea = primeiro `subarea` que contém o ponto. Ponto fora de qualquer subárea fica sem subárea.
- Faixa de nível no JSON como `levels: { min, max }`, opcional.
- Toda zona precisa de mapa do Tiled. Arena ASCII numa zona dá o erro "zona sem nascimento".

**Desvios:** `tests/Game.NetBots` (alternar portões no `--roam`) não estava nas áreas da tarefa. A mudança é mínima e foi feita para cumprir o critério dos bots nas três zonas.

**Pendências:** o site (`Game.Site`) ainda não mostra a faixa de nível nem os NPCs (fora das áreas desta tarefa).
