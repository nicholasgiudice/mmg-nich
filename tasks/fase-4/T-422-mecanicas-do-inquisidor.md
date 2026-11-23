# T-422 — Core: mecânicas novas do Inquisidor

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-421
- **Libera:** T-423
- **Áreas que pode alterar:** `src/Game.Core/` (efeitos, estados, áreas, dano, cura, ataque básico), `src/Game.Data/` (carga e validação dos campos novos), `data/schemas/`, `tests/Game.Core.Tests/`, `tests/Game.Data.Tests/`
- **Branch:** sai da `main` com a T-421 integrada (ou da branch da T-421, se ela ainda não tiver entrado; registrar a base no Resultado). É integrada na `main` (seção 6g de `tasks/README.md`).

## Contexto

Design em `design/segunda-classe-v0.1.md`, seção 8 (Inquisidor e a tabela "Mecânicas do Clérigo"). Aprovado pelo designer **como está** em 2026-11-22. Esta tarefa cria **as mecânicas** no Core, cada uma com teste e com dados de teste. As habilidades de verdade vêm na T-423.

O Inquisidor tem Martelo da Justiça, Consagração, Selo da Luz, Julgamento e Ira Divina (assinatura). O Martelo e o Julgamento (até o NH4) cabem no que existe: dano sagrado, `bonusVsTags`, arco, atordoamento.

O que já existe e pode ser reaproveitado:

- áreas no chão (`spawn_area`, `data/*`, `src/Game.Core/Combat/Areas/`), cada uma com **um** filtro `affects` (inimigos ou aliados);
- vida pelo dano no **atacante** (`lifesteal` no estado, `LifestealReaction`, T-407), com as regras de dano indireto, cura recebida e evento `HealApplied`;
- alvos extras em volta do alvo (`extra_targets`), corrente (`chain`);
- o ataque básico de cada classe em `classes.json` (`basicAttack`; do Clérigo, `cleric.holy_strike`, alcance 6).

## Requisitos

1. **Área que fere e cura** (Consagração): no mesmo lugar e pela mesma duração, um pulso por segundo fere os monstros e cura os aliados. Se duas áreas lançadas juntas (uma com `affects: enemy`, outra com `affects: ally`) resolverem, só o teste; se não, o ajuste mínimo (por exemplo, filtro de facção por efeito dentro da área).
2. **Vida pelo dano para quem bate no alvo marcado** (Selo da Luz): um estado no **monstro** faz todo aliado de quem lançou que causar dano nele se curar de uma fração do dano. Reaproveitar as regras da vida pelo dano da T-407 (dano final, sem dano indireto, cura recebida, sem publicar cura sem efeito). A cura sai em nome de quem bateu, atribuída à habilidade do selo.
3. **Cura pelo dano do próprio golpe** (Julgamento NH5: 20% do dano): um efeito de dano pode curar quem causou numa fração do dano final.
4. **Ataque básico trocado por um estado** (Ira Divina): enquanto um estado está ativo, o ataque básico de quem o tem vira outra habilidade, dada pelo estado. O cliente continua mandando a intenção de ataque básico; quem troca é o Core. Sugestão: o estado aponta para uma habilidade nos dados (`inquisitor.divine_wrath_strike`) com o Golpe Sagrado mais forte e alvos extras num raio de 2 em volta do alvo.
5. **Cura dos aliados perto pelo dano** (Ira Divina): um efeito de dano pode curar os aliados a até N tiles de quem causou numa fração do dano final de cada acerto.
6. Validação dos campos novos e schema.

Se o cliente precisar saber da troca do ataque básico (ícone ou alcance), subir `ProtocolVersion` a partir do número que estiver na `main` e registrar no Resultado. Preferir não mandar nada novo: a troca pode ficar só no servidor.

## Critérios de aceite

Testes no Core:

- Consagração de teste: no mesmo pulso, o monstro no raio leva dano e o aliado no raio é curado; o monstro não é curado e o aliado não leva dano;
- com o Selo de 10% num monstro, dois aliados que causam 100 cada se curam 10 cada; quem não é aliado do lançador não se cura; dano refletido não cura;
- Julgamento de teste com 20% e 200 de dano cura 40 quem bateu;
- com o estado da Ira Divina, o ataque básico acerta todos no raio de 2 em volta do alvo com o dano maior; quando o estado acaba, volta ao Golpe Sagrado normal, com a recarga normal;
- cada acerto da Ira Divina de teste cura os aliados a até 4 tiles em 10% do dano e não cura quem está mais longe;
- `./scripts/check.sh` completo passa.

## Resultado

Feito em 2026-11-22, **no Windows** (a tarefa não usa tela), branch `task/T-422-mecanicas-do-inquisidor`, a partir de `main`
em `8f5c228` (com a T-421 integrada). Rodei `./scripts/check.sh --fast`; o `check.sh` completo (com `Game.Design.Tests`)
fica para a integração, como combinado com o integrador.

### O que foi feito

1. **Área que fere e cura** (Consagração): **já funcionava**, sem campo novo. Duas áreas lançadas juntas pela mesma
   habilidade (`spawn_area` duas vezes), uma com `affects: enemy` (dano) e outra com `affects: ally` (cura), nascem no mesmo
   ponto e no mesmo tick, com o mesmo intervalo, então pulsam juntas. Coberto por teste: no mesmo pulso o monstro no raio
   leva dano e o aliado (e o Inquisidor) é curado; o monstro não é curado, o aliado não leva dano; 6 pulsos em 6 s.
2. **Vida pelo dano para quem bate no alvo marcado** (Selo da Luz, `attackerLifesteal` no status): o mesmo formato de
   `lifesteal` da T-407 (`LifestealDefinition`), mas no **alvo**. Na `LifestealReaction` (`Combat/Status/Lifesteal.cs`), depois
   da vida pelo dano de quem bateu, cada status com `attackerLifesteal` em quem levou o dano cura quem bateu, se ele é da
   mesma facção de quem aplicou o status (o próprio lançador incluído). Mesmas regras: `Amount` (dano final antes dos
   escudos), × acúmulos do status, `belowHealth`/`killHeal` pela vida de quem bateu, cura recebida de quem bateu, não é
   crítica, sem dano indireto (refletido ou desviado), sem dano em si mesmo, sem publicar cura sem efeito. Sai como
   `HealApplied` de quem bateu nele mesmo, com `SourceId` = `attackerLifesteal.ability` (ou o status).
3. **Cura pelo dano do próprio golpe** (Julgamento NH5, `damage.selfHeal`): fração em [0, 1] do dano final do acerto (o valor
   que `DamageResolver.Deal` devolve: sem a parte desviada a um protetor). Cura quem causou, pela cura recebida dele, não é
   crítica, `SourceId` = a origem do dano (a habilidade). Lógica nova em `Combat/Effects/DamageHeals.cs`; `OnDamage` só
   chama `DamageHeals.Apply` depois do dano.
4. **Ataque básico trocado por um status** (Ira Divina, `basicAttack` no status), `Combat/BasicAttackOverride.cs`: enquanto
   o status está ativo, a intenção de ataque básico usa a **definição** da habilidade apontada (alvo, efeitos, recarga,
   conjuração), mas com o **id do ataque básico**. O cliente continua mandando a intenção do Golpe Sagrado e vê
   `AbilityStarted`/`AbilityExecuted` dele (`IsBasicAttack` verdadeiro), a recarga fica no mesmo `ReadyAtTick` do ataque
   básico (com a divisão pela AGI) e o dano sai no log como Golpe Sagrado. Quando o status acaba, o próximo uso volta à
   definição normal, com a recarga normal. A troca é aplicada no `AbilityValidator` (uso e fim de conjuração) e nos acertos
   extras agendados (`AbilitySystem`, três linhas).
5. **Cura dos aliados perto pelo dano** (Ira Divina, `damage.allyHeal`): `{ fraction, radius, includeSelf, ability }`. A
   cada acerto, os aliados vivos (mesma facção) a até `radius` tiles de quem causou (o corpo conta, como nas áreas) curam
   `fraction` do dano final daquele acerto, cada um pela própria cura recebida; não é crítica. `includeSelf` (padrão true)
   inclui quem bateu. `ability` dá a origem da cura no `HealApplied` e no log (padrão: a origem do dano). Mesmo arquivo
   `DamageHeals.cs`.
6. **Validação e schema**: `src/Game.Data/GameDataValidator.Inquisitor.cs` (arquivo novo, chamado de três linhas no
   validador) e `data/schemas/game-data.schema.json` (`damage.selfHeal`, `damage.allyHeal`, `attackerLifesteal`,
   `basicAttack`).

### Campos novos nos dados (para a T-423)

```json
{ "type": "damage", "base": 36, "scaling": { "weapon": 2.16, "int": 4.5 }, "school": "holy",
  "bonusVsTags": { "undead": 0.75 }, "selfHeal": 0.2 }

{ "id": "status.seal_of_light_1", "name": "Selo da Luz", "defaultDuration": 8,
  "attackerLifesteal": { "fraction": 0.1, "ability": "inquisitor.seal_of_light" } }

{ "id": "status.divine_wrath_1", "name": "Ira Divina", "defaultDuration": 10,
  "basicAttack": "inquisitor.divine_wrath_strike_1" }

{ "id": "inquisitor.divine_wrath_strike_1", "name": "Golpe Sagrado", "cooldown": 1,
  "targeting": { "type": "unit", "range": 6 },
  "effects": [
    { "type": "damage", "base": 9, "scaling": { "weapon": 1.2, "int": 1.5 }, "school": "holy",
      "allyHeal": { "fraction": 0.1, "radius": 4, "ability": "inquisitor.divine_wrath" } },
    { "type": "extra_targets", "radius": 2, "count": 0, "effects": [
      { "type": "damage", "base": 9, "scaling": { "weapon": 1.2, "int": 1.5 }, "school": "holy",
        "allyHeal": { "fraction": 0.1, "radius": 4, "ability": "inquisitor.divine_wrath" } } ] } ] }
```

(Os números do golpe acima são ilustrativos: o Golpe Sagrado da Ira é o de `cleric.holy_strike` × 1,5 no NH1/NH2 e × 1,8
no NH3, com os mesmos `bonusVsTags`.) Faixas: `selfHeal` em [0, 1]; `allyHeal.fraction` em (0, 1], `radius` > 0,
`ability` precisa existir; `attackerLifesteal` com as faixas de `lifesteal`; `basicAttack` precisa existir e não pode ser
passiva nem ter telegráfo.

Consagração (sem campo novo): duas áreas com o mesmo raio, duração e intervalo, lançadas juntas.

```json
{ "id": "area.consecration_harm_1", "radius": 3, "duration": 6, "interval": 1, "affects": "enemy",
  "effects": [ { "type": "damage", "base": 4, "scaling": { "int": 0.5 }, "school": "holy", "bonusVsTags": { "undead": 0.5 } } ] }
{ "id": "area.consecration_mend_1", "radius": 3, "duration": 6, "interval": 1, "affects": "ally",
  "effects": [ { "type": "heal", "base": 3, "scaling": { "int": 0.3 } } ] }

"targeting": { "type": "self" },
"effects": [ { "type": "spawn_area", "area": "area.consecration_harm_1" }, { "type": "spawn_area", "area": "area.consecration_mend_1" } ]
```

A Ira Divina é uma habilidade `self` com `{ "type": "apply_status", "status": "status.divine_wrath_1", "target": "self" }`;
um status (e um golpe) por NH quando os números mudam.

### Testes

- `tests/Game.Core.Tests/InquisitorMechanicsTests.cs` (17): Consagração no mesmo pulso fere o monstro e cura o aliado e o
  Inquisidor, sem o contrário, 6 pulsos em 6 s e dano e cura nos mesmos ticks; Selo de 10% com dois aliados causando 100
  cura 10 cada (origem: a habilidade do selo), pela habilidade marca o monstro, monstro que bate não se cura, dano
  refletido no marcado não cura, cura recebida pela metade e fim do selo, vida cheia não publica cura; Julgamento de 20%
  com 200 de dano cura 40 (e 20 com cura recebida pela metade); com a Ira, o ataque básico acerta o alvo e os dois a até 2
  dele com 75 (não o de longe), com o id e a recarga do ataque básico; ao acabar, volta aos 50 e à recarga normal; a
  habilidade trocada não pode ser usada direto; a troca só vale para quem tem o status; cada acerto da Ira cura em 10% os
  aliados a até 4 tiles (e quem bateu), não o de longe nem o monstro perto; `includeSelf: false` e origem padrão da cura.
- `tests/Game.Data.Tests/InquisitorMechanicsLoaderTests.cs` (16): carga dos campos e erros de validação.
- `./scripts/check.sh --fast` passou no Windows: formatação, build sem avisos e todos os testes menos `Game.Design.Tests`
  (Core 523, Data 405, Client 128, Server 117, Sim 75, Protocol 74, Api 39, Site 15, Persistence 2; os ignorados são os que
  pedem banco, como antes). O `check.sh` completo fica para a integração; nenhum dado real usa os campos novos.

### Decisões

- **Sem protocolo novo.** `ProtocolVersion` continua **19**. A troca do ataque básico fica só no servidor: o cliente vê o
  Golpe Sagrado (mesmo alcance de 6 nos dados de exemplo) e as curas pelos `HealApplied` de sempre.
- A troca guarda o id do ataque básico (em vez de usar o id da habilidade trocada) para a recarga, o botão da barra e o log
  continuarem sendo do Golpe Sagrado. A habilidade trocada não precisa ser conhecida nem estar numa classe e usa o NH1;
  para números por NH, um status por NH apontando para um golpe por NH. Com dois status de troca ativos, vale o último da
  lista.
- Consagração com duas áreas, como a tarefa sugeria, em vez de filtro por efeito: o cliente desenha dois círculos iguais
  no mesmo lugar (dois `AreaSpawned`).
- "Aliado de quem lançou" no Selo é a facção de quem aplicou o status por último (`StatusInstance.Source`); se essa
  entidade sumiu do mundo, ninguém se cura.
- `allyHeal` inclui quem bateu por padrão (os aliados no raio, como na Bênção); `includeSelf: false` deixa de fora.
- `selfHeal` e `allyHeal` usam o dano devolvido pelo `DamageResolver.Deal` (sem a parte desviada); o Selo usa o `Amount`
  do `DamageReport`, como a vida pelo dano da T-407.

### Desvios

- Feita no Windows, e não na VPS; `check.sh` só no modo rápido (o completo fica para a integração).
- Para não conflitar com as T-431 e T-442 em paralelo, a lógica nova ficou em arquivos novos (`BasicAttackOverride.cs`,
  `DamageHeals.cs`, `GameDataValidator.Inquisitor.cs`) e os campos novos em blocos próprios no fim de `DamageEffect` e de
  `StatusDefinition`; nos arquivos existentes só há linhas de chamada.

### Pendências

- As habilidades de verdade (Martelo da Justiça, Consagração, Selo da Luz, Julgamento, Ira Divina), os golpes da Ira por
  NH e os textos do site (descrever `selfHeal`, `allyHeal`, `attackerLifesteal` e `basicAttack`) são da T-423.
- O cliente não mostra que o ataque básico mudou (nem ícone nem efeito visual diferente) e desenha a Consagração como dois
  círculos sobrepostos.
