# T-352 — Mapa e instância: Cripta do Vigia

- **Ambiente:** Windows (Tiled)
- **Modelo:** Humano + Médio
- **Depende de:** T-343, T-350, T-351
- **Libera:** T-380
- **Áreas que pode alterar:** `data/maps/` (mapa novo `watcher_crypt.tmj`, `old_road_ruins.tmj` para o portão da entrada), `data/zones.json`, `data/maps/tilesets/`, `client/assets/art/tiles/` (com créditos), `CREDITS.md`, `tests/` (percurso e design da Cripta), `tasks/reports/`

## Contexto

A Cripta do Vigia (`design/first-map-v0.1.md` §5) é a dungeon de grupo, níveis 12–14, com o Vigia Sem Rosto (nível 15) no fim. A sequência é: Entrada → **Salão dos Escudos** (guardas; portas bloqueadas por grupos) → **Galeria das Cinzas** (sacerdotes; áreas de maldição) → **Câmara dos Juramentos** (duas rotas curtas: mais monstros e mais saque, ou menos combate) → **Sala do Vigia** (arena ampla, três selos visíveis nas bordas, sem obstáculos que atrapalhem a leitura).

A instância por grupo vem da T-350. O chefe e os selos vêm da T-351. A entrada fica nas Ruínas (`crypt_entrance`, T-343) e, nesta fase, fica **aberta**, sem chave (decisão da T-240).

## Requisitos

1. **Mapa** `data/maps/watcher_crypt.tmj` com as cinco salas em sequência. Arena do chefe com cerca de 30×24 e os selos em posições marcadas (`spawns` ou objetos próprios, conforme a T-351 definiu).
2. **Zona** `zone.watcher_crypt` do tipo `Instance` (T-350), com os monstros por sala nos níveis 12–14 (guardas, sacerdotes, e o Cavaleiro Espectral como elite numa das rotas da Câmara) e o Vigia nível 15.
3. **Portas bloqueadas por grupos** (Salão dos Escudos): se a T-350 não trouxer portas, use a forma mais simples, que são salas ligadas por corredores, sem portas que abrem. Registre a escolha. As portas que abrem ficam para depois.
4. **Galeria:** "inimigos que retornam uma vez se o altar não for desativado" **fica fora** desta fase, a não ser que já exista mecânica. Registre a decisão.
5. **Portão** das Ruínas (`crypt_entrance`) para a instância, e a saída da instância de volta para a entrada.
6. **Teste de design da Cripta inteira** (opcional, se o simulador permitir encadear salas): no mínimo, cada sala como um encontro com um grupo de 4 no nível 12–14 vencendo ≥ 80%, e o chefe com a meta da T-351.
7. Duração total esperada: 25–40 min. Registre a estimativa.
8. O designer aprova o mapa.

## Critérios de aceite

- teste de percurso: da entrada ao chefe, alcançável a pé;
- testes de design das salas passando;
- online (servidor local): dois clientes no mesmo grupo entram juntos na mesma cópia e chegam à sala do chefe;
- `./scripts/check.sh` completo passa.

## Resultado

**Feito.** Relatório: `tasks/reports/balance-v0.15.md`; imagens em `tasks/reports/mapas-t352/`.

- **Mapa** `data/maps/watcher_crypt.tmj` (140 × 76): Entrada → Salão dos Escudos → Galeria das Cinzas → Câmara dos Juramentos → Passagem Estreita (rota segura) ou Rota dos Mortos (rota de risco, com o Cavaleiro) → Antessala → Sala do Vigia.
  - A Sala do Vigia tem 38 × 26 livres, sem obstáculos, e uma área `boss_arena` (`watcher_arena`).
  - Os selos são pontos `monster_spawn` com `objective = true`, ao norte, ao sul e a leste.
- **Zona** `zone.watcher_crypt` (instância, nível 12–15, 4 jogadores, 300 s vazia, 30 s para quem sai do grupo) em `data/zones.json`. A `zone.test_instance` da T-350 continua lá, para os testes do servidor.
- **Bordas e transições** (pedido do designer na revisão):
  - Toda parede ao norte de um piso da Cripta tem face de pedra de 3 tiles, e há pedra sob as paredes, para os cantos não mostrarem terra.
  - A escada dupla da T-343 (um tile com uma escada que sobe e outra que desce, lado a lado) saiu.
  - **Nas Ruínas,** a entrada da Cripta é um mausoléu com fachada de pedra, um medalhão e um arco escuro onde a escada desce. O portão `crypt_gate` é a base do arco, dentro da área `crypt_entrance`. A volta chega em `from_crypt`, diante do arco.
  - **Na Cripta,** a saída é uma escada que sobe na parede norte da Entrada, entre lampiões.
  - **A borda leste das Ruínas** passou a ser a muralha oeste da Cidade, com o portão de 2 tiles na estrada. É o mesmo padrão do portão Campos ↔ Cidade da T-330, e o portão `east_gate` ficou com 2 × 4, como lá.
- **Testes:**
  - percurso (`ZoneMapTests`): tudo alcançável a pé, a sequência de salas, o Vigia e os 3 selos, e o portão dentro da entrada da Cripta;
  - servidor (`CryptInstanceTests`): da Cidade até a mesma cópia em grupo, com os selos como objetivos; a perda dos selos tira todos e acaba a cópia;
  - leitura do mapa (`TiledMapLoaderTests`): a propriedade `objective`;
  - design (`CryptDesignTests`).
- **Resultados:**
  - todos os grupos das salas vencem ≥ 86% com 4 jogadores;
  - o Vigia na sala real vence 69% no nível 14 e 77% no nível 15, em 356 s e 330 s (meta da T-351);
  - a duração estimada é de 25,6 min pela rota segura e 27,7 min pela de risco;
  - online, 2 bots em grupo entraram na mesma cópia e chegaram à Sala do Vigia;
  - o `./scripts/check.sh` completo passa.

**Decisões:**
- **Portas bloqueadas por grupos:** a T-350 não tem portas. O Salão dos Escudos tem três alas ligadas por passagens estreitas, cada uma guardada por um grupo (dois Cavaleiros e um grupo com Sacerdotes). As portas que abrem ficam para depois.
- **Galeria:** os inimigos que voltam se o altar não for desativado ficaram fora. As áreas `curse_area` estão marcadas no mapa, sem mecânica.
- **Selos no mundo aberto:** antes os selos só existiam em encontros do simulador. Agora a propriedade `objective` de um `monster_spawn` faz o monstro nascer como objetivo (aliado dos jogadores, sem IA).
- **Falha do encontro:** quando o `EncounterFailed` da T-351 chega numa instância, todos saem pela saída com o aviso `Removed` e a cópia acaba (a pendência da T-351). A próxima entrada recomeça a Cripta inteira. Pode ser duro; fica para o designer avaliar no playtest.
- **Sala do Vigia com 38 × 26**, e não 30 × 24, e **porta a leste**, atrás do Vigia. Com 30 × 24 e a porta a oeste, a luta ficou abaixo da meta da T-351 (relatório §4).
- **Cavaleiros como porteiros:** Guardas e Sacerdotes morrem em 10–20 s para 4 jogadores. Os Cavaleiros (elite) dão o peso e a duração.
- Como na T-330 e na T-343, o gerador do mapa ficou fora do repositório. O `.tmj` é a fonte daqui em diante.

**Fora das áreas da tarefa** (mínimo):
- `src/Game.Core/Spatial/MapObjects.cs` e `src/Game.Core/Definitions/Zones.cs`: o campo `Objective`;
- `src/Game.Data/TiledMapReader.cs` e `src/Game.Data/ZoneComposer.cs`: leitura e cópia da propriedade `objective`;
- `src/Game.Server/Zones/OpenZone.cs`: o objetivo nasce com `ObjectiveService.Spawn`, e `LivingMonsters` não conta objetivos;
- `src/Game.Server/Zones/WorldServer.cs`: o encontro que falha encerra a cópia.

Em `tests/`, também mudei `FieldGroups` (posições para 4 jogadores e para grupos de até 7 monstros).

**Pendências:**
- **T-345:** mostrar no cliente o portão da Cripta, os selos e o aviso `Removed` quando o encontro falha.
- **Depois do playtest (T-380):**
  - talvez uma versão "de dungeon" dos monstros (rank elite), se as salas parecerem fáceis;
  - decidir se a falha nos selos deve recomeçar só a Sala do Vigia;
  - a maldição da Galeria;
  - os fragmentos da chave (T-240).
- **Tempo do Vigia no nível 14:** fica a 4 s do teto de 6 min. Uma mudança no chefe ou na sala pode quebrar a meta.
