# T-441 — Core: mecânicas novas do Piromante

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-432 (copiar estados para outros monstros)
- **Libera:** T-442
- **Áreas que pode alterar:** `src/Game.Core/` (efeitos, estados, mira, áreas, telegráfos), `src/Game.Data/` (carga e validação dos campos novos), `data/schemas/`, `tests/Game.Core.Tests/`, `tests/Game.Data.Tests/`
- **Branch:** sai da `main` com a T-432 integrada. É integrada na `main` (seção 6i de `tasks/README.md`).

## Contexto

Design em `design/segunda-classe-v0.1.md`, seção 7 (Piromante e a tabela "Mecânicas do Mago"). Aprovado pelo designer **como está** em 2026-11-22. Esta tarefa cria **as mecânicas** no Core, cada uma com teste e com dados de teste. As habilidades de verdade vêm na T-443.

O Piromante tem Combustão, Rajada de Chamas, Chão em Chamas, Bola de Fogo e Meteoro (assinatura). Todas aplicam a **Queimadura** (dano de fogo por segundo, 4 s) do NH da habilidade que queimou; uma nova renova a antiga. A Rajada de Chamas e o Chão em Chamas cabem no que existe (arco, área no chão, estado com dano periódico).

O que já existe e pode ser reaproveitado:

- estados com dano periódico e etiquetas; `remove_status` por etiqueta;
- arcos e áreas no chão (Golpe Pesado, Campo de Relâmpagos), mira em linha;
- telegráfo de área com aviso (Cataclismo Arcano);
- copiar estados para monstros perto (T-432, Flecha Dispersa).

## Requisitos

1. **Queimadura por NH:** cada habilidade aplica a versão da Queimadura do próprio NH. Se a Queimadura de um NH mais baixo renovar uma de NH mais alto, decidir e registrar a regra (sugestão: fica a mais forte, com a duração renovada).
2. **Dano só em quem tem um estado, e tirar o estado** (Combustão): um efeito de área em volta de quem lança causa dano só nos monstros que têm um estado com certa etiqueta (`burn`) e, conforme o NH, tira esse estado ou não. No NH5, a Queimadura de cada monstro atingido também passa para os monstros a até 2 tiles (reaproveitar a cópia de estados da T-432).
3. **Explodir no primeiro que acerta** (Bola de Fogo): um disparo em linha para no primeiro monstro que encontra e aplica os efeitos numa área em volta dele (raio 2 a 3). Sem monstro na linha, explode no fim do alcance ou não explode; decidir e registrar.
4. **Área depois do impacto** (Meteoro NH3): depois do aviso e do impacto, deixar uma área no chão (o Chão em Chamas NH1, por 5 s) no mesmo lugar. Se `spawn_area` já roda depois de um telegráfo, só o teste.
5. Validação dos campos novos e schema.

## Critérios de aceite

Testes no Core:

- Combustão de teste com 3 monstros no raio, 2 queimando: só os 2 levam dano; no NH1 a Queimadura acaba neles, no NH3 continua;
- no NH5, a Queimadura de um monstro atingido passa para outro a 2 tiles e não para um a 3;
- Bola de Fogo de teste com 2 monstros em fila: explode no primeiro e acerta os dois se o segundo estiver no raio; um monstro fora do raio não é atingido;
- Meteoro de teste no NH3 deixa a área no chão depois do impacto, e ela queima quem fica nela;
- a regra de Queimadura de NH diferentes vale como registrada;
- `./scripts/check.sh` completo passa.

## Resultado

Feito em 2026-11-23, **no Windows** (a tarefa não usa tela), branch `task/T-441-mecanicas-do-piromante`, a partir de `main`
em `9d3a372` (com T-421, T-422, T-431, T-432 e T-442 integradas). Feita em paralelo com a T-423 e a T-433 (só dados): não
mexi em `data/*.json`, só no schema. Rodado só o `./scripts/check.sh --fast`; o `check.sh` completo (com
`Game.Design.Tests`) fica para a integração, como combinado com o integrador.

### O que foi feito

1. **Queimadura por NH** (`exclusive` no status, `StatusExclusiveDefinition`; `ExclusiveStatus` em
   `Combat/Status/PyromancerMechanics.cs`, chamado por uma linha em `StatusManager.Apply`, antes de criar ou renovar a
   instância):
   - um status por NH (`status.burn_1..5`), todos com `exclusive: { group, tier }` no mesmo grupo e `tier` = NH. Status do
     mesmo grupo não convivem numa entidade;
   - **regra registrada**: fica a Queimadura de `tier` mais alto (empate: a nova), e o fim passa a ser **o maior** entre o
     que faltava da antiga e a duração da nova. Ou seja: uma de NH mais baixo **não troca** a mais forte, só renova a
     duração dela (sem trocar a fonte nem os números; sai um `StatusRefreshed`); uma de NH igual ou mais alto **substitui** a
     antiga (`StatusRemoved` da antiga e `StatusApplied` da nova). Reaplicar a mesma Queimadura também nunca encurta (a da
     Rajada de Chamas NH5, de 6 s, não cai para 4 s com um pulso do Chão em Chamas);
   - vale para qualquer caminho que passe por `StatusManager.Apply`: `apply_status`, áreas, cópias (`copy_status`), pulos;
   - **sem `byRank`**: cada habilidade do Piromante já declara os efeitos de cada NH (`ranks[].effects`, e uma área por NH),
     então cada uma aplica direto o `status.burn_N` do próprio NH. O apelido `byRank` da T-432 escolhe pelo NH de **uma**
     habilidade fixa e não serve aqui.
2. **Dano só em quem tem um estado, e tirar o estado** (Combustão, efeito novo **`if_status`**, `IfStatusEffect`; lógica em
   `PyromancerMechanics`):
   - entre os alvos do uso (a Combustão é `circle_self` de raio 8), os que têm **no momento do uso** um status ativo (não
     permanente) com uma das `tags` recebem os `effects` (mesma fonte, habilidade, potência e acerto); os outros, nada;
   - com `consume: true`, depois dos efeitos, os status com essas etiquetas saem de cada um (`RemoveByTags`, sai
     `StatusRemoved` com o Piromante como autor). Sem `consume` (NH3+), a Queimadura continua;
   - quem é atingido é decidido **antes** de qualquer efeito: no NH5, um monstro que pegou fogo pela cópia de outro no mesmo
     uso não leva o dano da Combustão;
   - **NH5**: reaproveita o `copy_status` da T-432 dentro do `if_status` (ver o JSON abaixo): a Queimadura de cada monstro
     atingido vai, com o tempo que falta, para todos os inimigos a até 2 tiles dele (o corpo conta), com o Piromante como
     fonte. Quem já queima fica pela regra do item 1 (fica a mais forte).
3. **Explodir no primeiro que acerta** (Bola de Fogo, `line.explodeRadius`; `LineExplosion` em `PyromancerMechanics`, chamada
   por cinco linhas no caso `line` de `TargetResolver.Resolve`):
   - com o campo, a linha (mesmo comprimento, largura, alcance a mais da T-431 e linha de visão de antes) só serve para achar o
     **primeiro** alvo: o mais à frente de quem usou na direção da mira (empate: menor id). Os alvos passam a ser ele e todos
     os inimigos a até `explodeRadius` tiles dele (o corpo conta), com linha de visão a partir dele, os mais perto primeiro;
   - os efeitos da habilidade (dano, Queimadura) saem em todos eles. Como o NH3 e o NH5 mudam o raio, o NH declara o
     `targeting` (os `ranks[].targeting` que já existem);
   - **sem monstro na linha, não explode**: o uso acontece (custo e recarga), sem alvo, como uma linha que não acerta nada.
     Mais simples, e o disparo é instantâneo (hitscan, como os outros) e não tem ponto de chegada próprio.
4. **Área depois do impacto** (Meteoro NH3): **já funcionava**, sem campo novo. Num telegráfo, os efeitos saem na resolução com
   o ponto do telegráfo como mira, e o `spawn_area` nasce nesse ponto. Coberto por teste: depois do aviso de 2 s, o impacto
   acerta todos no raio 6, nasce a área no ponto e ela queima (dano e Queimadura) quem fica nela ou entra depois, por 5 s.
   Como a do Meteoro dura 5 s e a do Chão em Chamas NH1 6 s, a T-443 precisa de uma área própria (`area.meteor_flames`, com os
   números do Chão em Chamas NH1 e `duration: 5`).
5. **Validação e schema**: `src/Game.Data/GameDataValidator.Pyromancer.cs` (arquivo novo, chamado por três linhas no
   validador) e `data/schemas/game-data.schema.json` (efeito `if_status`, `exclusive` no status, `explodeRadius` na linha).
   O tipo `if_status` entra em `GameJson` num bloco próprio, como os da T-442 e da T-432.

### Campos novos nos dados (para a T-443)

Queimadura por NH (um status por NH, mesmo grupo; os números são os do design, seção 7):

```json
{ "id": "status.burn_1", "name": "Queimadura", "tags": ["debuff", "burn", "dot"], "defaultDuration": 4,
  "exclusive": { "group": "pyromancer_burn", "tier": 1 },
  "periodic": [ { "interval": 1, "effects": [ { "type": "damage", "school": "fire", "base": 3, "scaling": { "int": 0.3 } } ] } ] }
{ "id": "status.burn_5", "name": "Queimadura", "tags": ["debuff", "burn", "dot"], "defaultDuration": 4,
  "exclusive": { "group": "pyromancer_burn", "tier": 5 },
  "periodic": [ { "interval": 1, "effects": [ { "type": "damage", "school": "fire", "base": 5.4, "scaling": { "int": 0.54 } } ] } ] }
```

Rajada de Chamas NH5 (Queimadura de 6 s): `{ "type": "apply_status", "status": "status.burn_5", "duration": 6 }`.

Combustão (NH1/NH2 com `consume`; NH3/NH4 sem; NH5 sem e com a cópia):

```json
"targeting": { "type": "circle_self", "radius": 8 },
"effects": [ { "type": "if_status", "tags": ["burn"], "consume": true,
               "effects": [ { "type": "damage", "school": "fire", "base": 20, "scaling": { "int": 1.5 } } ] } ]

{ "rank": 5, "effects": [ { "type": "if_status", "tags": ["burn"],
    "effects": [ { "type": "damage", "school": "fire", "base": 36, "scaling": { "int": 2.7 } },
                 { "type": "copy_status", "tags": ["burn"], "radius": 2, "count": 0 } ] } ] }
```

Bola de Fogo (raio 2; o NH3 e o NH5 trocam o `targeting` para 2,5 e 3):

```json
"targeting": { "type": "line", "length": 10, "width": 1, "explodeRadius": 2 },
"effects": [ { "type": "damage", "school": "fire", "base": 38, "scaling": { "int": 2.8 } },
             { "type": "apply_status", "status": "status.burn_1" } ],
"ranks": [ { "rank": 3, "targeting": { "type": "line", "length": 10, "width": 1, "explodeRadius": 2.5 }, "effects": [ ... ] } ]
```

Meteoro NH3 (sem campo novo; telegráfo como o Cataclismo Arcano):

```json
"targeting": { "type": "circle_at_point", "range": 10, "radius": 6 },
"telegraph": { "shape": "circle", "delay": 2, "radius": 6, "anchor": "aim" },
"effects": [ { "type": "damage", "school": "fire", "base": 144, "scaling": { "int": 14.4 } },
             { "type": "apply_status", "status": "status.burn_3" },
             { "type": "spawn_area", "area": "area.meteor_flames" } ]

{ "id": "area.meteor_flames", "radius": 3, "duration": 5, "interval": 1,
  "effects": [ { "type": "damage", "school": "fire", "base": 5, "scaling": { "int": 0.6 } },
               { "type": "apply_status", "status": "status.burn_1" } ] }
```

Faixas: `if_status.tags` não vazio e sem etiqueta vazia, `if_status.effects` não vazio e com os efeitos válidos de sempre,
`consume` padrão false; `exclusive.group` não vazio, `exclusive.tier` ≥ 1; `line.explodeRadius` > 0 (ausente = linha comum).

### Testes

- `tests/Game.Core.Tests/PyromancerMechanicsTests.cs` (13): Combustão de teste com 3 monstros no raio, 2 queimando (e o
  terceiro com outro estado): só os 2 levam o dano; no NH1 a Queimadura acaba neles (2 `StatusRemoved`), no NH3 continua, e
  o outro estado fica; sem ninguém queimando, a Combustão é usada e não causa dano; no NH5 a Queimadura do monstro atingido
  passa (mesmo NH, mesmo tempo que falta, o Piromante como fonte) para o monstro a 2 tiles e não para o de 3, a dele continua,
  e o que pegou fogo no mesmo uso não leva o dano; Queimadura NH1 sobre a NH3 deixa a NH3 (mesma instância, duração renovada,
  continua causando o dano do NH3); NH5 sobre a NH1 troca pela NH5; a de 6 s não encurta com a mesma de 4 s nem com uma mais
  fraca; status fora do grupo não mudam; Bola de Fogo de teste explode no primeiro e acerta o de trás (a 1,5) e um ao lado da
  linha (a 1,5), e não o que está na linha a 4 do primeiro; com o segundo a 3 tiles, só o primeiro; sem monstro na linha, não
  explode (nem num monstro perto do fim do alcance); Meteoro de teste NH3: depois do aviso, o impacto acerta o centro e a
  borda, a área nasce no ponto e queima (5 pulsos e a Queimadura) quem fica nela e quem entra depois, não o da borda, e some;
  mesma seed, mesmo hash.
- `tests/Game.Data.Tests/PyromancerMechanicsLoaderTests.cs` (10): carga dos campos (inclusive no NH), padrões sem eles e erros
  de validação.
- `./scripts/check.sh --fast` passou no Windows: formatação, build sem avisos e os testes sem `Game.Design.Tests` (Core 590,
  Data 459, Client 128, Server 117, Sim 75, Protocol 74, Api 39, Site 15, Persistence 2; os ignorados são os que pedem banco,
  como antes). O `check.sh` completo fica para a integração. Nenhum dado real usa os campos novos e o RNG não é consumido
  pelas mecânicas novas, então os testes de design não devem mudar.

### Decisões

- **Queimaduras de NH diferentes**: fica a mais forte (maior NH), com a duração renovada; a duração nunca encurta (fica a maior
  entre o que faltava e a nova). Quando a mais fraca chega, a mais forte continua com a fonte e os números dela (com um
  Piromante só, a fonte é a mesma).
- **Bola de Fogo sem monstro na linha: não explode** (sem alvo; custo e recarga gastos, como qualquer linha que não acerta).
- O "primeiro" da Bola de Fogo é o mais à frente na direção da mira (projeção na linha), não o mais perto em linha reta.
- A explosão é uma mira (`line.explodeRadius`), não um efeito: assim todos os efeitos da habilidade (e os de `ranks`) valem
  para todos na explosão, e a fração de `behindFraction` e o crítico da T-431 seguem funcionando como em qualquer mira.
- A Combustão escolhe quem queima antes de aplicar qualquer efeito (a cópia do NH5 não faz o vizinho levar o dano).
- Status permanentes não contam para `if_status` nem para `exclusive` (como na cópia da T-432).
- **Sem protocolo novo.** `ProtocolVersion` continua **19**: nenhum evento novo (troca de Queimadura sai como
  `StatusRemoved`/`StatusApplied`/`StatusRefreshed`; a explosão sai como os `DamageDealt` de sempre), nenhum campo novo no
  snapshot.

### Desvios

- Feita no Windows, e não na VPS; `check.sh` só no modo rápido (o completo fica para a integração).
- Lógica nova em arquivos novos (`Combat/Status/PyromancerMechanics.cs`, `Definitions/PyromancerDefinitions.cs`,
  `GameDataValidator.Pyromancer.cs`); `exclusive` num bloco marcado `T-441` no fim de `StatusDefinition` e `explodeRadius`
  num bloco `T-441` em `LineTargeting`. Linhas de chamada em código existente: `StatusManager.Apply` (o grupo exclusivo),
  `TargetResolver.Resolve` (a explosão), `CombatModules` (slot T-441), `GameJson` e o validador.

### Pendências

- As habilidades de verdade (Combustão, Rajada de Chamas, Chão em Chamas, Bola de Fogo, Meteoro), as Queimaduras por NH, a área
  do Meteoro e os textos do site são da T-443. O site (`Game.Site/Describe.cs` e `Usage.cs`) ainda não descreve `if_status`
  (nem segue os efeitos dentro dele até os status usados), `exclusive` nem `explodeRadius`.
- O cliente desenha a mira da Bola de Fogo como uma linha comum e não mostra o ponto da explosão (só vê os danos); um efeito
  visual da explosão pediria um evento novo e subir o `ProtocolVersion`. Os bots (`Game.Bots/AbilityInfo`) e a IA tratam a
  Bola de Fogo como uma linha.
- O simulador deve dizer se a Combustão NH5 (que espalha a Queimadura) e a Bola de Fogo passam das metas (design, seção 7).
