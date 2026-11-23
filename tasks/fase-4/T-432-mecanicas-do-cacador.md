# T-432 — Core: mecânicas novas do Caçador

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-431
- **Libera:** T-433
- **Áreas que pode alterar:** `src/Game.Core/` (efeitos, estados, dano, áreas, progressão das passivas), `src/Game.Data/` (carga e validação dos campos novos), `data/schemas/`, `tests/Game.Core.Tests/`, `tests/Game.Data.Tests/`
- **Branch:** sai da `main` com a T-431 integrada (ou da branch da T-431, se ela ainda não tiver entrado; registrar a base no Resultado). É integrada na `main` (seção 6h de `tasks/README.md`).

## Contexto

Design em `design/segunda-classe-v0.1.md`, seção 6 (Caçador e a tabela "Mecânicas do Arqueiro"). Aprovado pelo designer **como está** em 2026-11-22. Esta tarefa cria **as mecânicas** no Core, cada uma com teste e com dados de teste. As habilidades de verdade vêm na T-433.

O Caçador tem Flecha Envenenada (passiva), Ferida Aberta, Nuvem Tóxica, Flecha Dispersa e Praga (assinatura). Ele usa um veneno próprio, o **Veneno do Caçador**, que acumula em pilhas. O dano por pilha, o máximo de pilhas e a duração vêm do NH da Flecha Envenenada.

O que já existe e pode ser reaproveitado:

- estados com pilhas (`stacking: stack`, `maxStacks`) e dano periódico (Sangramento, veneno);
- áreas no chão (Campo de Relâmpagos), estados em área;
- passivas como estado permanente aplicado pelo NH (T-403);
- reações depois do dano (`IDamageReaction`, como a vida pelo dano da T-407).

## Requisitos

1. **Efeito em todo acerto** (Flecha Envenenada): uma passiva aplica um estado no alvo **a cada acerto** de quem a tem (ataque básico e qualquer habilidade de dano; não dano periódico, não dano refletido nem desviado). Cada acerto põe 1 pilha e renova a duração de todas.
2. **Estado que depende do NH de outra habilidade** (Veneno do Caçador): o veneno que a Nuvem Tóxica, a Flecha Dispersa e a Praga põem tem o dano, as pilhas e a duração do NH da Flecha Envenenada de quem lança. Decidir e registrar o que acontece sem pontos na Flecha Envenenada (sugestão: usa o NH1).
3. **Dano maior se o alvo andar** (Ferida Aberta): um dano periódico causa mais (+50%, +100% no NH5) nos pulsos em que o alvo se moveu desde o pulso anterior. Usar a posição do Core, não a física do Godot.
4. **Copiar estados para outros monstros** (Flecha Dispersa): o efeito copia estados do alvo com certas etiquetas (veneno e sangramento) para até N monstros a até 4 tiles, com as mesmas pilhas e o tempo que falta, e quem lançou como fonte.
5. **Estado que pula ao acabar** (Praga): enquanto o efeito da Praga dura, quando o Veneno do Caçador **acaba pelo tempo** num monstro, ele passa com todas as pilhas para o monstro mais próximo a até 4 tiles que ainda não tem o veneno. Sem cadeia infinita: o pulo só vale enquanto a Praga dura.
6. Validação dos campos novos e schema.

## Critérios de aceite

Testes no Core:

- com a passiva de teste, 5 disparos seguidos deixam o alvo com o máximo de pilhas (3 no NH1) e a duração renovada; um tique de veneno não põe pilha;
- a Nuvem Tóxica de teste põe o veneno com os números do NH da passiva de quem lança (NH1 e NH5) e, sem a passiva, a regra decidida;
- sangramento de teste com +50%: num alvo parado causa o valor base por pulso, num alvo que anda causa 1,5 vez;
- Flecha Dispersa de teste copia 2 pilhas de veneno e o sangramento para 2 monstros a até 4 tiles, e não para um a 5 tiles nem para um aliado;
- com a Praga ativa, o veneno que acaba num monstro pula para o mais próximo com todas as pilhas; depois que a Praga acaba, não pula mais;
- `./scripts/check.sh` completo passa.

## Resultado

Feito em 2026-11-22, **no Windows** (a tarefa não usa tela), branch `task/T-432-mecanicas-do-cacador`, a partir de `main` em
`c6fda57` (com T-421, T-422, T-431 e T-442 integradas). Feita em paralelo com a T-423 (só dados do Clérigo): não mexi em
`data/*.json`, só no schema. Rodado só o `./scripts/check.sh --fast`; o `check.sh` completo (com `Game.Design.Tests`) fica
para a integração, como combinado com o integrador.

### O que foi feito

1. **Efeito em todo acerto** (Flecha Envenenada, `hitEffects` no status; `HitEffectsReaction` em
   `Combat/Status/HunterMechanics.cs`, uma reação depois do dano como a vida pelo dano da T-407):
   - a cada dano de **habilidade** (a origem do dano é uma habilidade: ataque básico, habilidades de dano, acertos extras,
     `hits` como a Chuva de Flechas, telegráfos e a troca do ataque básico da T-422) que quem tem o status causa em outra
     entidade, os `hitEffects` rodam no alvo, com quem tem o status como fonte e o id do status como origem;
   - não contam: dano periódico de status, dano de área (`spawn_area`, a origem é a área), dano refletido e a parte desviada
     a um protetor (`DamageReport.Indirect`), dano em si mesmo e alvo que morreu no golpe;
   - uma vez por acerto (não × acúmulos); vários alvos no mesmo uso (linha, círculo) ganham um cada;
   - como a origem dos `hitEffects` é o status, um dano dentro deles não dispara de novo (sem laço);
   - "1 pilha e renova a duração de todas" é o `apply_status` de sempre num status `stacking: stack`.
2. **Status pelo NH de outra habilidade** (Veneno do Caçador, `byRank` no status; `RankedStatus.Resolve`): aplicar um status
   com `byRank: { ability, statuses }` aplica, no lugar dele, `statuses[NH − 1]` pelo NH de `ability` **de quem aplica**
   (resolvido no começo de `StatusManager.Apply`, então vale para `apply_status`, áreas, `hitEffects`, efeitos de status,
   tudo). O status com `byRank` é só um apelido e nunca fica numa entidade (os outros campos dele não valem). A Nuvem Tóxica,
   a Flecha Dispersa, a Praga e a própria passiva aplicam o apelido `status.hunter_venom`, e cada um vira o veneno do NH da
   Flecha Envenenada de quem lança.
3. **Dano maior se o alvo andar** (Ferida Aberta, `damage.movingBonus`; `MovingDamage` e o componente interno
   `PulseMotion`): num `periodic` de status, o pulso causa × (1 + `movingBonus`) se a **posição do Core** do alvo
   (`Transform.Position`) mudou mais de **0,1 tile** desde o pulso anterior. O primeiro pulso compara com a posição de quando o
   status pegou (guardada em `StatusManager.Apply`, uma linha). A marca é feita uma vez por pulso, antes dos acúmulos, então
   2 acúmulos num alvo que anda causam 2 × 1,5. Fora de um `periodic` o campo não faz nada. `PulseMotion` não tem
   `IReplicated` (fica no servidor).
4. **Copiar estados para outros monstros** (Flecha Dispersa, efeito novo **`copy_status`**; `StatusCopy` no mesmo arquivo),
   **reaproveitável** pela T-441 (ver "Como reaproveitar" abaixo):
   - para cada alvo do efeito, os status ativos dele com qualquer uma das `tags` (menos os permanentes) vão para até
     `count` entidades (0 = todas; padrão 1) a até `radius` tiles do alvo (o corpo conta, como nos alvos extras), da facção
     `affects` em relação a quem usou (padrão inimigos), as mais perto primeiro (empate: menor id), com linha de visão a partir
     do alvo e que podem ser alvo (não `untargetable`). O próprio alvo não recebe;
   - cada cópia tem os **mesmos acúmulos** e o **tempo que falta** (o mesmo `ExpiresAtTick`), e quem usou vira a fonte;
   - quem já tem o status fica com o maior número de acúmulos e o maior tempo (e a fonte nova), sem somar;
   - a cópia passa por `StatusManager.Apply` (imunidade por etiqueta, eventos `StatusApplied`/`StatusStacked` de sempre).
5. **Estado que pula ao acabar** (Praga, `jumpOnExpire` no status; `StatusJump`, chamado pelo `StatusSystem` logo depois do
   `onExpire` da T-421):
   - a Praga é um status **em quem lança** (`apply_status` com `target: self`) com `jumpOnExpire: { tags, radius }`;
   - enquanto ele dura, quando um status com uma dessas etiquetas **aplicado por quem tem a Praga** acaba **pelo tempo** num
     monstro vivo, o mesmo status (mesmo id, mesmo NH) passa, com **todos os acúmulos** e a duração padrão, para o inimigo
     mais perto a até `radius` tiles (o corpo conta; empate: menor id; com linha de visão) que **ainda não tem nenhum status
     com essas etiquetas**. A fonte continua sendo o Caçador, então o veneno pode pular de novo;
   - não pula quando o status é tirado, quando o monstro morre, sem a Praga ativa (acabou, foi tirada) nem se não há para
     quem pular. Sem cadeia infinita: o pulo só vale enquanto a Praga dura, e cada pulo dura a duração padrão (≥ 1 tick);
   - um pulo por veneno que acaba, mesmo com duas Pragas.
6. **Acúmulos de uma vez** (`apply_status.stacks`, para a Praga pôr 3 a 5 pilhas): depois da aplicação normal, soma os
   acúmulos que faltam, até o máximo do status, com um `StatusStacked` só. `HunterMechanics.AddStacks` faz o mesmo direto do
   código.
7. **Validação e schema**: `src/Game.Data/GameDataValidator.Hunter.cs` (arquivo novo, chamado por cinco linhas no validador)
   e `data/schemas/game-data.schema.json` (`damage.movingBonus`, `apply_status.stacks`, efeito `copy_status`, `hitEffects`,
   `byRank` e `jumpOnExpire` no status). O tipo `copy_status` entra em `GameJson` num bloco próprio, como o da T-442.

### Campos novos nos dados (para a T-433)

Flecha Envenenada (passiva) e o Veneno do Caçador por NH (um status por NH, com a etiqueta `hunter_venom`; um apelido com
`byRank`). Como o apelido resolve pelo NH, a passiva pode usar o **mesmo** `passiveStatus` em todos os NH:

```json
{ "id": "hunter.poisoned_arrow", "name": "Flecha Envenenada", "passive": true, "passiveStatus": "status.poisoned_arrow",
  "targeting": { "type": "self" } }

{ "id": "status.poisoned_arrow", "name": "Flecha Envenenada", "defaultDuration": 1,
  "hitEffects": [ { "type": "apply_status", "status": "status.hunter_venom" } ] }

{ "id": "status.hunter_venom", "name": "Veneno do Caçador", "defaultDuration": 4,
  "byRank": { "ability": "hunter.poisoned_arrow",
              "statuses": ["status.hunter_venom_1", "status.hunter_venom_2", "status.hunter_venom_3",
                           "status.hunter_venom_4", "status.hunter_venom_5"] } }

{ "id": "status.hunter_venom_1", "name": "Veneno do Caçador", "tags": ["debuff", "poison", "dot", "hunter_venom"],
  "defaultDuration": 4, "stacking": "stack", "maxStacks": 3,
  "periodic": [ { "interval": 1, "effects": [ { "type": "damage", "school": "nature", "base": 1, "scaling": { "dex": 0.12 } } ] } ] }
{ "id": "status.hunter_venom_5", "name": "Veneno do Caçador", "tags": ["debuff", "poison", "dot", "hunter_venom"],
  "defaultDuration": 6, "stacking": "stack", "maxStacks": 5,
  "periodic": [ { "interval": 1, "effects": [ { "type": "damage", "school": "nature", "base": 1.8, "scaling": { "dex": 0.216 } } ] } ] }
```

Ferida Aberta (NH5 com `movingBonus: 1`):

```json
{ "id": "status.open_wound_1", "name": "Ferida Aberta", "tags": ["debuff", "bleed", "dot"], "defaultDuration": 6,
  "periodic": [ { "interval": 1, "effects": [ { "type": "damage", "school": "physical", "base": 6, "scaling": { "dex": 0.6 },
                                                "movingBonus": 0.5 } ] } ] }
```

Nuvem Tóxica: duas áreas lançadas juntas (como a Consagração da T-422), uma com o dano a cada 1 s e outra com o veneno a
cada 2 s:

```json
{ "id": "area.toxic_cloud_1", "radius": 3, "duration": 6, "interval": 1,
  "effects": [ { "type": "damage", "school": "nature", "base": 4, "scaling": { "dex": 0.7 } } ] }
{ "id": "area.toxic_cloud_venom_1", "radius": 3, "duration": 6, "interval": 2,
  "effects": [ { "type": "apply_status", "status": "status.hunter_venom" } ] }
```

Flecha Dispersa (o dano vem antes: o acerto põe a pilha da passiva no alvo e a cópia já a leva):

```json
"effects": [ { "type": "damage", "base": 8, "scaling": { "weapon": 0.6, "dex": 1.2 } },
             { "type": "copy_status", "tags": ["hunter_venom", "bleed"], "radius": 4, "count": 2 } ]
```

Praga:

```json
"targeting": { "type": "circle_at_point", "range": 9, "radius": 6 },
"effects": [ { "type": "apply_status", "status": "status.hunter_venom", "stacks": 3 },
             { "type": "apply_status", "status": "status.plague_1", "target": "self" } ]

{ "id": "status.plague_1", "name": "Praga", "defaultDuration": 8, "jumpOnExpire": { "tags": ["hunter_venom"], "radius": 4 } }
```

Faixas: `movingBonus` ≥ 0; `stacks` ≥ 1; `copy_status.tags` não vazio e sem etiqueta vazia, `radius` > 0, `count` ≥ 0;
`byRank.ability` precisa existir, `byRank.statuses` não vazio, cada um existe, não é o próprio status nem outro com `byRank`;
`jumpOnExpire.tags` não vazio, `radius` > 0; `hitEffects` com os efeitos válidos de sempre.

### Como reaproveitar a cópia (T-441, Combustão NH5)

- **Pelos dados**: um `copy_status` nos efeitos que a Combustão aplica em cada monstro atingido, **antes** de tirar a
  Queimadura (nos NH em que ela acaba), por exemplo
  `{ "type": "copy_status", "tags": ["burn"], "radius": 2, "count": 0 }` (todos a até 2 tiles). A fonte das cópias é quem
  lança; os acúmulos e o tempo que falta são os da Queimadura do monstro atingido.
- **Pelo código**: `StatusCopy.Spread(world, source, from, tags, radius, count, affects)` devolve quem recebeu;
  `StatusCopy.CopyTo(world, source, to, instance)` copia uma instância só. Os dois estão em
  `src/Game.Core/Combat/Status/HunterMechanics.cs`.
- Os monstros que já queimam ficam com o maior número de acúmulos e o maior tempo (não somam nem encurtam).

### Testes

- `tests/Game.Core.Tests/HunterMechanicsTests.cs` (22): 5 disparos seguidos deixam 1, 2, 3, 3, 3 pilhas (máximo 3 no NH1) e
  a duração renovada a cada disparo, no veneno do NH1 e com o Caçador como fonte (o apelido nunca fica no monstro); um tique de
  veneno causa 10 e não põe pilha nem renova; a linha põe uma pilha em cada alvo; dano de área e disparo de quem não tem a
  passiva não põem pilha; dano indireto não dispara; a Nuvem Tóxica de teste põe o veneno do NH1 (3 pilhas, 4 s, 10 por
  pilha) e do NH5 (5, 6 s, 18) pelo NH da passiva de quem lança, e o do NH1 sem pontos (NH 0) e sem NH guardado; a nuvem põe 1
  pilha a cada 2 s; monstro como fonte usa o NH1; Ferida Aberta de teste com +50%: 10 no alvo parado e 15 no que anda (pelo
  `MoveIntent` do Core), volta a 10 quando ele para e a 15 quando anda de novo; NH5 (+100%) causa 20 depois de um empurrão;
  mudança abaixo de 0,1 tile não conta; Flecha Dispersa de teste copia 2 pilhas de veneno e o sangramento (mesmo tempo, o
  Caçador como fonte) para os monstros a 2 e a 4 tiles, e não para o de 5 tiles, nem para o aliado, nem um status sem a
  etiqueta; com 3 no raio e `count` 2, vão os 2 mais perto; quem já tem fica com o maior; com a Praga ativa, o veneno que acaba
  pula para o mais perto com as 3 pilhas (não para o de 3 tiles nem para o de 5) e, depois que a Praga acaba, o que pulou
  acaba sem pular; o pulo ignora quem já tem o veneno e encadeia enquanto a Praga dura (A → C → A); sem a Praga, só acaba; a
  Praga de teste põe 3 pilhas em todos no raio e o máximo do veneno limita `stacks`; mesma seed, mesmo hash.
- `tests/Game.Data.Tests/HunterMechanicsLoaderTests.cs` (16): carga dos campos, padrões sem eles e erros de validação.
- `./scripts/check.sh --fast` passou no Windows: formatação, build sem avisos e os testes sem `Game.Design.Tests`
  (Core 577, Data 449, Client 128, Server 117, Sim 75, Protocol 74, Api 39, Site 15, Persistence 2; os ignorados são os que
  pedem banco, como antes). O `check.sh` completo fica para a integração. Nenhum dado real usa os campos novos e o RNG não é consumido
  pelas mecânicas novas, então os testes de design não devem mudar.

### Decisões

- **Sem pontos na Flecha Envenenada, o Veneno do Caçador é o do NH1** (como a tarefa sugeria). Vale também para fonte sem NH
  (monstros) e sem fonte. NH acima da lista usa o último. O NH é lido de `AbilityState.RankOf` de quem aplica, na hora de
  aplicar (a Nuvem e as cópias usam o NH de quem lança naquele momento).
- **Um status por NH** (`status.hunter_venom_1..5`) com um apelido `byRank`, em vez de um status cujos números mudam com o NH:
  cabe no formato de status que já existe, e o cliente e o log mostram um status comum. Consequência: o veneno de dois
  Caçadores em NH diferentes num mesmo monstro são dois status separados (cada um com as suas pilhas). O "já tem o veneno" da
  Praga olha a etiqueta (`hunter_venom`), então qualquer veneno do Caçador conta.
- A passiva só conta dano **de habilidade** (origem = uma habilidade). A área da Nuvem Tóxica não põe pilha pelo dano (só pelas
  pilhas a cada 2 s), senão seriam 1,5 pilha por segundo. Um golpe todo absorvido por escudo conta como acerto.
- "Andou" = a posição do Core mudou mais de 0,1 tile desde o pulso anterior. Empurrão e avanço também contam (a posição mudou);
  empurrõezinhos de colisão ficam abaixo do limite.
- A cópia não soma acúmulos em quem já tem o status (fica o maior), para a Flecha Dispersa não empilhar em cadeia num grupo.
- A Praga fica em quem lança, não nos monstros: o pulo só vale para o veneno daquele Caçador e acaba junto com o status dele.
  O veneno que pula começa a duração padrão do status (ele tinha acabado) e pode voltar a um monstro que já o perdeu.
- `apply_status.stacks` respeita o máximo do status: a Praga NH3 (5 pilhas) com a passiva no NH1 põe 3.
- **Sem protocolo novo.** `ProtocolVersion` continua **19**: nenhum evento novo (cópias e pulos saem como `StatusApplied` /
  `StatusStacked` / `StatusRefreshed`), nenhum campo novo no snapshot (`PulseMotion` é interno).

### Desvios

- Feita no Windows, e não na VPS; `check.sh` só no modo rápido (o completo fica para a integração).
- A base é a `main` local em `c6fda57`: o worktree começou em `8f5c228` (só com a T-421); a `main` já tinha T-422, T-431 e T-442
  integradas, então a branch saiu dela.
- Lógica nova em arquivos novos (`Combat/Status/HunterMechanics.cs`, `Definitions/HunterDefinitions.cs`,
  `GameDataValidator.Hunter.cs`); os campos novos ficam em blocos marcados `T-432` no fim de `StatusDefinition` e de
  `DamageEffect`, e `stacks` no fim de `ApplyStatusEffect`. Linhas de chamada em código existente: `StatusManager.Apply`
  (resolver o `byRank`; guardar a posição), `StatusSystem` (pulso e pulo), `StatusModule` (o registro de `apply_status` chama
  os acúmulos a mais), `CoreEffects.OnDamage` (o fator do movimento), `CombatModules` (slot T-432), `GameJson` e o validador.

### Pendências

- As habilidades de verdade (Flecha Envenenada, Ferida Aberta, Nuvem Tóxica, Flecha Dispersa, Praga), os status por NH e os
  textos do site são da T-433. O site (`Game.Site/Describe.cs` e `Usage.cs`) ainda não descreve `copy_status`, `hitEffects`,
  `byRank`, `jumpOnExpire`, `movingBonus` nem `stacks`, e o `Usage` não segue o apelido `byRank` até os status de cada NH.
- O cliente não mostra o pulo nem a cópia de forma especial (só vê os status novos nos monstros); um efeito visual de "pulou de
  A para B" pediria um evento novo e subir o `ProtocolVersion`.
- O simulador deve dizer se o veneno da passiva em todos os alvos (com a Chuva de Flechas) passa das metas (design, seção 6).
