# T-442 — Core: mecânicas novas do Arcanista

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-421 (regeneração de Mana vinda de um estado), T-441
- **Libera:** T-443
- **Áreas que pode alterar:** `src/Game.Core/` (habilidades, conjuração, efeitos, estados, movimento), `src/Game.Data/` (carga e validação dos campos novos), `data/schemas/`, `tests/Game.Core.Tests/`, `tests/Game.Data.Tests/`; `src/Game.Protocol/` só se os orbes ou a canalização precisarem ir para o cliente
- **Branch:** sai da `main` com a T-421 e a T-441 integradas. É integrada na `main` (seção 6i de `tasks/README.md`).

## Contexto

Design em `design/segunda-classe-v0.1.md`, seção 7 (Arcanista e a tabela "Mecânicas do Mago"). Aprovado pelo designer em 2026-11-22, com uma mudança: o **Sifão Arcano aumenta a regeneração de Mana** por 5 s em vez de devolver Mana na hora. Esta tarefa cria **as mecânicas** no Core, cada uma com teste e com dados de teste. As habilidades de verdade vêm na T-443.

O Arcanista tem Raio Arcano, Marca Arcana, Orbes Arcanos, Sifão Arcano e Feixe do Vazio (assinatura). Ele rende mais quanto mais tempo fica no mesmo alvo. A Marca Arcana cabe no que existe (pilhas com dano periódico).

O que já existe e pode ser reaproveitado:

- conjuração (`castTime`, cancelada ao se mover, `CastControl`) e habilidades com vários acertos (`hits.count`, `hits.interval`);
- regeneração de Mana vinda de um estado (T-421, Inspiração);
- mais dano recebido num estado (Marca do Caçador);
- invocações (`summon`, `maxAlive`), hoje usadas por monstros.

## Requisitos

1. **Canalização com pulsos:** a habilidade aplica os efeitos em pulsos (a cada 0,5 s) **enquanto** quem lança canaliza. Andar, ser atordoado ou silenciado corta a canalização, e os pulsos que faltam não saem. Se `hits` com conjuração já faz isso, ajustar só o que falta.
2. **Dano que sobe a cada pulso** (Raio Arcano): cada pulso causa uma fração a mais que o anterior (+15%, acumulando: pulso n = base × 1,15^(n−1)).
3. **Andar devagar canalizando** (Raio Arcano NH5, Feixe do Vazio): a habilidade pode deixar andar a 50% da velocidade sem cortar a canalização.
4. **Efeito ao terminar a canalização** (Feixe do Vazio NH3): se a canalização vai até o fim, um efeito final no alvo (+15% de dano recebido de todos por 6 s). Cortada no meio, não sai.
5. **Orbes que atiram sozinhos no alvo atual** (Orbes Arcanos): por 10 s, de 2 a 4 orbes atiram no alvo atual do Arcanista a cada 1,5 s. Definir no Core o que é o "alvo atual" (sugestão: o último alvo de unidade que o Arcanista escolheu, se ainda vivo e no alcance; sem alvo, os orbes não atiram). Sugestão de forma: um estado no Arcanista com efeito periódico no alvo atual, um disparo por orbe, sem entidades novas. Se preferir invocações, registrar por quê. Os disparos são dano do Arcanista (crítico, log e ameaça dele).
6. **Sifão Arcano:** confirmar com teste que a regeneração vinda de um estado (T-421) serve no próprio lançador (+6 Mana/s por 5 s).
7. Validação dos campos novos e schema.

Se o cliente precisar saber dos orbes (quantos e onde desenhar) ou da canalização em andamento além do que já vai no snapshot, subir `ProtocolVersion` a partir do número da `main` e registrar no Resultado.

## Critérios de aceite

Testes no Core:

- canalização de teste de 6 pulsos: parado, saem 6; andar depois do 3º corta e saem 3; atordoar corta;
- com +15% por pulso e base 100, os pulsos causam 100, 115, 132, 152… (arredondamento registrado);
- com "andar devagar", andar não corta e a velocidade cai à metade enquanto canaliza; volta ao normal ao acabar;
- Feixe de teste até o fim deixa o alvo com +15% de dano recebido por 6 s; cortado no meio, não deixa;
- Orbes de teste com 3 orbes atiram 3 vezes a cada 1,5 s no alvo atual; trocar de alvo muda o alvo dos orbes; sem alvo, não atiram;
- Sifão de teste dá +6 de Mana por segundo ao próprio Arcanista por 5 s;
- `./scripts/check.sh` completo passa.

## Resultado

Feito em 2026-11-22, **no Windows** (a tarefa não usa tela), branch `task/T-442-mecanicas-do-arcanista`, a partir de `main` em
`8f5c228` (com a T-421). Feita **em paralelo com a T-422 e a T-431**, sem esperar a T-441 (o integrador decidiu resolver os
conflitos dos arquivos em comum na integração). Rodado só o `./scripts/check.sh --fast`; o check completo (com
`Game.Design.Tests`) fica para a integração.

### O que foi feito

1. **Canalização com pulsos** (`channel` na habilidade, `ChannelDefinition`; lógica em `Combat/Channeling.cs`):
   - `channel: { pulses, interval, pulseGrowth?, moveSpeedMultiplier?, onComplete? }`. Ao usar, a habilidade **paga o custo e
     entra na recarga na hora** (como uma instantânea) e publica `AbilityExecuted`; os efeitos da habilidade não saem no uso,
     saem nos pulsos: o pulso n no tick início + n × intervalo (o primeiro 0,5 s depois de usar; o último fecha a canalização);
   - a canalização usa a mesma `AbilityState.Cast` da conjuração (`StartTick` = uso, `EndTick` = último pulso), então tudo que
     já cortava uma conjuração corta a canalização: atordoar, silenciar, medo, `interrupt`, morrer e andar. Os pulsos que faltam
     não saem e o custo não volta. Enquanto canaliza, outras habilidades são recusadas (`AlreadyCasting`);
   - o andamento (pulsos que já saíram e potência do uso) fica num componente interno, `ChannelProgress` (sem `IReplicated`);
   - cada pulso resolve o alvo de novo (alcance e linha de visão): fora deles, o pulso se perde e a canalização continua. Alvo de
     unidade morto ou sumido encerra a canalização **sem** `CastInterrupted` (nada foi interrompido; os pulsos só não têm em quem
     sair). Cada pulso publica `AbilityHit` (n de N), que o cliente já recebe;
   - os pulsos não revalidam o uso (custo e recarga foram no início).
2. **Dano que sobe a cada pulso** (`channel.pulseGrowth`): o pulso n sai com a potência × (1 + pulseGrowth)^(n−1), pela potência
   do uso (a mesma do "fortalecer o próximo uso" da T-061), então vale para dano, cura e escudo. O arredondamento é o de sempre,
   no valor final (metade para longe do zero): base 100 com 0,15 → **100, 115, 132, 152, 175, 201** (de 132,25; 152,09; 174,90;
   201,14).
3. **Andar devagar canalizando** (`channel.moveSpeedMultiplier`, em (0, 1]): com o campo, andar não corta a canalização e a
   velocidade é × o número (0,5 = metade) enquanto canaliza, por um `IModifierSource` novo (`Channeling.ChannelSpeed`). Acabou
   ou foi cortada, a velocidade volta. Sem o campo, andar corta.
4. **Efeito ao terminar a canalização** (`channel.onComplete`): depois do último pulso, os efeitos saem no alvo (resolvido de novo,
   com quem lançou como fonte). Cortada no meio, não saem.
5. **Orbes no alvo atual** (efeito novo `current_target`, `CurrentTargetEffect`; lógica em `Combat/Effects/CurrentTargetEffects.cs`):
   - **alvo atual** = o alvo de unidade da **última intenção de usar habilidade** da entidade (aceita ou recusada), se for outra
     entidade. Guardado num componente interno, `CurrentTarget` (sem `IReplicated`), pelo `AbilitySystem` ao receber a intenção.
     Usar uma habilidade sem alvo (os próprios Orbes, por exemplo) não apaga o alvo atual;
   - o efeito aplica os `effects` `count` vezes (um disparo por orbe, cada um com o próprio crítico) no alvo atual de **quem recebe
     o efeito**, com a fonte do efeito. Pensado para o `periodic` de um status que o Arcanista põe em si: a fonte é o Arcanista,
     então crítico, log e ameaça são dele. O `SourceId` dos disparos é o id do status;
   - não atira se não há alvo atual, se ele morreu, está fora de `range` (a partir de quem tem o status, contando o raio do alvo),
     sem linha de visão, não pode ser alvo ou não é da facção de `affects` (padrão `enemy`);
   - **sem entidades novas** (como sugerido): os orbes são um status no Arcanista. A quantidade de orbes por NH vem de um status
     por NH (como a Bênção e a Inspiração da T-421).
6. **Sifão Arcano**: confirmado com teste que `stats.manaRegen` (T-421) serve no próprio lançador: `apply_status` com
   `target: self` dá +6 de Mana/s por 5 s e para ao acabar. Nenhum campo novo.
7. **Validação e schema**: `GameDataValidator.Arcanist.cs` (parcial nova), chamada por uma linha em `ValidateAbility` e uma no
   `switch` de efeitos; `data/schemas/game-data.schema.json` (`channel` na habilidade e no NH, `$defs/channel`, efeito
   `current_target`):
   - `pulses` em 1..40, `interval` ≥ 1 tick, `pulseGrowth` ≥ 0, `moveSpeedMultiplier` em (0, 1], `onComplete` com os efeitos
     válidos de sempre;
   - canalização não combina com `castTime`, `hits` nem `telegraph`, conferido **em cada NH efetivo** (erro em "channel (NH2)" se
     o NH2 põe `castTime` numa canalização), nem com passiva;
   - `current_target`: `range` > 0, `count` ≥ 1, `effects` não vazio e válido.
8. **NH**: `channel` declarado no NH substitui a canalização inteira (como `hits`); ausente, herda (`AbilityRanks.Resolve`).

### Campos novos nos dados (para a T-443)

Raio Arcano (NH3 passa a 8 pulsos; NH5 anda devagar):

```json
{ "id": "arcanist.arcane_ray", "name": "Raio Arcano", "cost": { "resource": "mana", "amount": 40 }, "cooldown": 8,
  "targeting": { "type": "unit", "range": 9 },
  "effects": [ { "type": "damage", "school": "arcane", "base": 6, "scaling": { "int": 0.5 } } ],
  "channel": { "pulses": 6, "interval": 0.5, "pulseGrowth": 0.15 },
  "ranks": [
    { "rank": 3, "channel": { "pulses": 8, "interval": 0.5, "pulseGrowth": 0.15 } },
    { "rank": 5, "channel": { "pulses": 8, "interval": 0.5, "pulseGrowth": 0.15, "moveSpeedMultiplier": 0.5 } }
  ] }
```

(os `effects` de cada NH como de costume). Feixe do Vazio NH3 (NH1 e NH2 sem `onComplete`):

```json
"channel": { "pulses": 8, "interval": 0.5, "moveSpeedMultiplier": 0.5,
             "onComplete": [ { "type": "apply_status", "status": "status.void_exposed" } ] }

{ "id": "status.void_exposed", "name": "Feixe do Vazio", "defaultDuration": 6,
  "modifiers": { "damageTakenMultiplier": 1.15 } }
```

Orbes Arcanos (um status por quantidade de orbes; a habilidade é `targeting: self` com um `apply_status` dele):

```json
{ "id": "status.arcane_orbs_3", "name": "Orbes Arcanos", "defaultDuration": 10,
  "periodic": [ { "interval": 1.5, "effects": [
    { "type": "current_target", "range": 9, "count": 3,
      "effects": [ { "type": "damage", "school": "arcane", "base": 2.8, "scaling": { "int": 0.28 } } ] } ] } ] }
```

Sifão Arcano (sem campo novo):

```json
"castTime": 1,
"effects": [ { "type": "damage", "school": "arcane", "base": 20, "scaling": { "int": 1.5 } },
             { "type": "apply_status", "status": "status.arcane_siphon_1", "target": "self" } ]

{ "id": "status.arcane_siphon_1", "name": "Sifão Arcano", "defaultDuration": 5, "stats": { "manaRegen": { "base": 6 } } }
```

Os orbes atiram pela primeira vez 1,5 s depois de usar e pela última aos 9 s (6 vezes em 10 s; o `periodic` não sai depois do
fim do status).

### Testes

- `tests/Game.Core.Tests/ArcanistMechanicsTests.cs` (16): canalização de 6 pulsos parada (nada no uso, recarga no início, fim da
  barra no último pulso, `AbilityHit` 1..6); andar depois do 3º corta e saem 3 (`CastInterrupted` `Moved`); atordoar e silenciar
  cortam; alvo morto encerra sem `CastInterrupted`; outra habilidade é recusada canalizando; +15% por pulso com base 100 dá
  100, 115, 132, 152, 175, 201; "andar devagar" não corta, anda à metade e volta ao normal ao acabar; Feixe até o fim deixa +15% de
  dano recebido de todos por 6 s (e acaba), cortado no meio não deixa; 3 orbes atiram 3 vezes a cada 1,5 s no alvo atual, com o
  Arcanista como fonte; trocar de alvo muda o alvo dos orbes; sem alvo não atiram; alvo longe ou morto não leva disparo; Sifão
  dá +6 de Mana/s ao próprio Arcanista por 5 s e para; estado interno copiado e fora da rede.
- `tests/Game.Data.Tests/ArcanistMechanicsLoaderTests.cs` (16): carga de `channel` (com NH) e de `current_target`; erros de
  validação dos campos, da mistura com `castTime`/`hits`/`telegraph` (também vinda de um NH) e do `current_target`.
- `./scripts/check.sh --fast` passou no Windows: formatação, build sem avisos e todos os testes menos `Game.Design.Tests` (os
  ignorados são os que pedem banco, como antes). Nenhum dado real usa os campos novos, então os testes de design não devem mudar;
  o check completo fica para a integração.

### Decisões

- **Sem protocolo novo.** `ProtocolVersion` continua **19**. A canalização vai no `AbilityState.Cast` que o snapshot já leva
  (início e fim); os pulsos vão no `AbilityHit`; os orbes são um status (`StatusEffects`, já no snapshot) e os disparos são
  `DamageDealt` com o id do status. `ChannelProgress` e `CurrentTarget` são internos.
- Custo e recarga no **início** da canalização (a conjuração cobra ao concluir): os pulsos já são o efeito.
- O alvo atual vem da intenção de usar habilidade, porque o servidor não recebe a seleção de alvo do cliente. Na prática, é o
  alvo do último ataque básico ou habilidade em unidade. Uma intenção recusada (fora de alcance, em recarga, canalizando) também
  troca o alvo, para os orbes seguirem o que o jogador escolheu.
- O alvo atual é o de quem **recebe** o efeito `current_target`, e a fonte do dano é a do efeito; no Arcanista são o mesmo.
- Canalização não aceita `castTime` (não há "conjura e depois canaliza"); o Arcanista não precisa.
- `onHit` continua limitado a 1..5 (não foi estendido aos pulsos 6 a 8).

### Desvios

- Feita no Windows, e não na VPS, a pedido do integrador, em paralelo com a T-422 e a T-431 e antes da T-441.
- Arquivos compartilhados com as outras tarefas tocados só com blocos próprios: `Definitions.cs` (um campo em
  `AbilityDefinition` e um em `AbilityRankDefinition`), `AbilityRanks.cs` (uma linha), `AbilitySystem.cs` (três trechos: o
  `else if` da canalização em `AdvanceCasts`, guardar o alvo atual em `HandleUse` e começar a canalização em `Execute`),
  `CombatServices.cs` (slot T-442 em `CombatModules`), `GameJson.cs` (bloco próprio para o tipo `current_target`),
  `GameDataValidator.cs` (duas linhas) e o schema. A lógica nova está em arquivos novos (`Definitions/Channel.cs`,
  `Combat/Channeling.cs`, `Combat/Effects/CurrentTargetEffects.cs`, `GameDataValidator.Arcanist.cs`).

### Pendências

- As habilidades de verdade (Raio Arcano, Marca Arcana, Orbes Arcanos, Sifão Arcano, Feixe do Vazio) e os textos do site são da
  T-443. O site ainda mostra `CurrentTargetEffect` pelo nome do tipo e não descreve a canalização.
- Cliente (T-444): a previsão de movimento (`NetAuthority.SpeedMultiplier`) não sabe da velocidade pela metade canalizando;
  andar no Raio NH5 ou no Feixe vai ser corrigido pelo servidor a cada snapshot (pequenos puxões). Basta multiplicar por
  `channel.moveSpeedMultiplier` da habilidade em `AbilityState.Cast`. A barra de canalização usa a da conjuração (enche em vez de
  esvaziar), e os orbes precisam ser desenhados a partir do status (quantos: `count` do `current_target` dele).
- A IA dos monstros e os bots do simulador já param quando há `Cast` (canalização sem `castWhileMoving`), sem mudança.
