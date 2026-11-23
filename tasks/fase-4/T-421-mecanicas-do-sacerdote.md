# T-421 — Core: mecânicas novas do Sacerdote

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** —
- **Libera:** T-422
- **Áreas que pode alterar:** `src/Game.Core/` (efeitos, estados, estatísticas, recursos, dano), `src/Game.Data/` (carga e validação dos campos novos), `data/schemas/`, `tests/Game.Core.Tests/`, `tests/Game.Data.Tests/`
- **Branch:** sai da `main` e é integrada na `main` (seção 6g de `tasks/README.md`).

## Contexto

Design em `design/segunda-classe-v0.1.md`, seção 8 (Sacerdote e a tabela "Mecânicas do Clérigo"). Aprovado pelo designer em 2026-11-22, com a **Inspiração** no lugar do Elo da Vida. Esta tarefa cria **as mecânicas** no Core, cada uma com teste e com dados de teste. As habilidades de verdade vêm na T-423.

O Sacerdote tem Oração Restauradora, Bênção, Graça Veloz, Inspiração e Santuário (assinatura). A Graça Veloz já cabe no que existe (velocidade, tirar estados, imunidade por etiqueta).

O que já existe e pode ser reaproveitado:

- estados com `modifiers` fixos, `flags` e efeitos periódicos (`periodic`), aplicados por `StatusManager` e rodados em `StatusModule` com a fonte do estado (`src/Game.Core/Combat/Status/`);
- cura (`heal`, com `scaling` pelas estatísticas da fonte), estados em aliados num raio (Muralha Viva, Bastião);
- estatísticas do personagem em `CharacterStats` (`src/Game.Core/Combat/Stats/`), hoje somadas da classe, dos pontos e do equipamento (`ItemMath`);
- regeneração de Mana em `ResourceSystem` (5/s + INT × 0,1 + afixo `ManaRegen` do equipamento);
- dano em `DamageResolver.Deal`, com desvio (T-406) e vida pelo dano (T-407).

## Requisitos

1. **Cura por segundo num estado** (Oração Restauradora): um estado com `periodic` de `heal` que escala pela INT **de quem lançou**. Provavelmente já funciona: confirmar com teste (cura pela INT da fonte, `healingTakenMultiplier` do alvo, evento `HealApplied` e log atribuídos à habilidade). Se não funcionar, o ajuste.
2. **Estatísticas vindas de um estado, pela INT de quem lançou** (Bênção e Inspiração):
   - um estado pode somar **atributos** (FOR, DES, AGI, INT, VIT, um a um ou todos) e **regeneração de Mana** a quem o tem;
   - o valor é `base + escala × estatística de quem lançou`, calculado **ao aplicar** e fixo enquanto o estado dura (renovar recalcula). Guardar o valor no estado, não reler a fonte a cada tick;
   - os atributos entram em `CharacterStats` como os do equipamento, e tudo que deriva deles muda junto (vida máxima pela VIT, Mana máxima pela INT, dano, crítico, velocidade pela AGI com os tetos atuais);
   - ao acabar, a vida e a Mana atuais não passam da nova máxima;
   - a regeneração a mais soma na de `ResourceSystem`. Isso não fere a regra "Mana vem só do tempo": é regeneração, não Mana na hora.
3. **Deixar quem lançou de fora** (Inspiração): o efeito que aplica estado nos aliados do raio pode excluir o próprio lançador. Se já existe uma opção, usá-la; se não, criar.
4. **Vida mínima de 1** (Santuário): flag de estado. Um dano que mataria quem tem o estado o deixa com 1 de vida. Vale para todo dano, inclusive periódico e a parte desviada para um protetor (T-406). Não muda cura, escudo nem os eventos de dano (o número mostrado é o dano aplicado de verdade).
5. **Efeito ao acabar o estado** (Santuário NH3): lista de efeitos que roda quando o estado **acaba pelo tempo** (não quando é tirado ou quando quem o tem morre), com a fonte do estado.
6. **Cura pela vida máxima do alvo** (Santuário NH3: 20% da vida máxima): a cura aceita um valor em fração da vida máxima de quem recebe, somado ao `base` e ao `scaling`.
7. Validação dos campos novos e schema.

Se algum campo novo for para o snapshot (por exemplo, para o cliente mostrar os atributos com a Bênção), subir `ProtocolVersion` a partir do número que estiver na `main` e registrar no Resultado. Preferir não mandar nada novo pela rede: vida e Mana máximas já vão no snapshot.

## Critérios de aceite

Testes no Core:

- estado de cura por segundo com escala de INT cura pela INT de quem lançou, não pela do alvo; cura recebida pela metade cura metade;
- estado de atributos com `2 + 0,08 × INT` e lançador com INT 62 dá +7 em cada atributo (6,96; a regra de arredondamento fica registrada no Resultado); a vida máxima do aliado sobe com a VIT e volta ao acabar, sem a vida atual passar da máxima;
- o valor não muda se a INT do lançador mudar com o estado ativo; renovar recalcula;
- estado de regeneração de Mana com +4/s: o aliado recupera 4 de Mana a mais por segundo, e para ao acabar;
- Inspiração de teste num grupo de 3: os outros dois ganham o estado, o lançador não;
- com a vida mínima, um golpe de 500 num alvo com 100 de vida deixa 1; sem o estado, mata; vale para dano periódico e para a parte desviada;
- efeito ao acabar roda quando o tempo acaba e não roda quando o estado é tirado (`remove_status`) ou quando o alvo morre;
- cura de 20% da vida máxima cura 20% de quem recebe;
- `./scripts/check.sh` completo passa.

## Resultado

Feito em 2026-11-22, **no Windows** (a pedido do designer; a tarefa não usa tela), branch `task/T-421-mecanicas-do-sacerdote`, a partir de `main` em `6856916`.

### O que foi feito

1. **Cura por segundo num status** (Oração Restauradora): já funcionava. O efeito periódico roda com a fonte do status, então `heal` com `scaling.int` usa a INT de quem lançou. Ficou coberto por teste: INT 62 cura 29 por pulso (o "29/s" do design), a cura recebida pela metade cura metade, e o `HealApplied` sai com a fonte certa.
2. **Atributos e Mana por segundo vindos de um status** (`stats` no status, `StatusStatsDefinition`):
   - `stats.attributes` com chaves `str`, `dex`, `agi`, `int`, `vit` ou `all` (os cinco), cada uma `{ base, scaling }`; `stats.manaRegen` também `{ base, scaling }`;
   - o valor é calculado **ao aplicar**, pelas estatísticas de quem aplicou, e fica guardado por instância num componente interno do servidor (`StatusStatBonuses`, fora da rede). Renovar recalcula; × acúmulos;
   - atributos arredondados para inteiro (metade para longe do zero): `2 + 0,08 × 62 = 6,96` → **+7**;
   - `StatusStats.Sync` soma a diferença em `CharacterStats` (os atributos finais, que o cliente já vê) e chama `Leveling.RefreshDerived`, como o equipamento: vida e Mana máximas, dano, crítico e velocidade de ataque mudam juntos. Roda ao aplicar, ao tirar (`Remove`, `RemoveByTags`, `RemoveAll`, `RemovePermanent`) e quando o status acaba;
   - `Gear.Refresh` refaz as estatísticas do zero; depois dele, `StatusStats.AfterRecompute` soma de novo o que vem de status (teste: trocar equipamento não apaga a Bênção);
   - a Mana por segundo soma em `CharacterStats.ManaRegen`, que o `ResourceSystem` já usa. Não dá Mana na hora.
3. **Deixar quem lançou de fora** (Inspiração): **já existia**. Alvo `self` com `extra_targets` (`affects: ally`, `count: 0`, raio): os alvos extras nunca incluem o alvo principal, como no Bastião do Guardião. Sem campo novo. A Bênção, que inclui o Sacerdote, usa o mesmo com um `apply_status` em si.
4. **Vida mínima de 1** (`flags.undying`): no `DamageResolver`, um dano que mataria quem tem o status o deixa com 1 de vida (`Applied` = vida − 1, `Overkill` 0, `Killed` falso). Vale para todo dano que passa pelo `DamageResolver`: direto, periódico, de área e a parte desviada a um protetor. O `Amount` do evento continua sendo o dano calculado (é o que a vida pelo dano e a ameaça usam).
5. **Efeito ao acabar** (`onExpire` no status): roda quando o status acaba **pelo tempo**, com a fonte do status e quem o tinha como alvo. Não roda quando o status é tirado, quando quem o tem está morto, nem em status permanente.
6. **Cura pela vida máxima** (`heal.maxHealthFraction`, 0..1): soma a fração da vida máxima de quem recebe à base e à escala, antes de crítico, potência e cura recebida.
7. **Validação e schema**: `GameDataValidator` (chaves de atributo, escalas, `stats` vazio, `onExpire` com os mesmos efeitos válidos, `maxHealthFraction` em 0..1) e `data/schemas/game-data.schema.json` (`stats`, `scaledValue`, `onExpire`, `flags.undying`, `heal.maxHealthFraction`).

### Campos novos nos dados (para a T-423)

```json
{ "id": "status.blessing_1", "name": "Bênção", "defaultDuration": 15,
  "stats": { "attributes": { "all": { "base": 2, "scaling": { "int": 0.08 } } } } }

{ "id": "status.inspiration_1", "name": "Inspiração", "defaultDuration": 12,
  "stats": { "manaRegen": { "base": 1.5, "scaling": { "int": 0.04 } } } }

{ "id": "status.sanctuary_3", "name": "Santuário", "defaultDuration": 6, "flags": { "undying": true },
  "onExpire": [ { "type": "heal", "base": 0, "maxHealthFraction": 0.2 } ] }

{ "id": "status.prayer_1", "name": "Oração Restauradora", "defaultDuration": 8,
  "periodic": [ { "interval": 1, "effects": [ { "type": "heal", "base": 4, "scaling": { "int": 0.4 } } ] } ] }
```

Inspiração (só os outros aliados) e Bênção (com o Sacerdote):

```json
"targeting": { "type": "self" },
"effects": [ { "type": "extra_targets", "radius": 6, "count": 0, "affects": "ally",
               "effects": [ { "type": "apply_status", "status": "status.inspiration_1" } ] } ]
```

Para a Bênção, um `{ "type": "apply_status", "status": "status.blessing_1" }` antes do `extra_targets`.

### Testes

- `tests/Game.Core.Tests/PriestMechanicsTests.cs` (18): cura por segundo pela INT de quem lançou e pela metade com cura recebida reduzida; Bênção +7 com INT 62 e vida máxima pela VIT; ao acabar, atributos e vida máxima voltam sem a vida passar da máxima; valor fixo com a INT do lançador mudando e recalculado ao renovar; tirar o status tira os atributos; `Gear.Refresh` mantém a Bênção; Bênção pega no Sacerdote e no aliado perto, não no longe; +4 de Mana/s e para ao acabar; Inspiração pega nos dois aliados e não no Sacerdote; vida mínima contra golpe de 500, contra veneno e na parte desviada; efeito ao acabar cura 20% da vida máxima e não roda com o status tirado nem com o alvo morto; cura de 20% da vida máxima; cópia da contabilidade para o snapshot.
- `tests/Game.Data.Tests/PriestMechanicsLoaderTests.cs` (9): carga dos campos e erros de validação.
- `./scripts/check.sh` completo passou no Windows: formatação, build sem avisos e todos os testes. Inclui os 248 de `Game.Design.Tests` (10 min), sem mudança, porque nenhum dado real usa os campos novos. Os ignorados (Persistence, Api, Server) são os que pedem banco, como antes.

### Decisões

- **Sem protocolo novo.** `ProtocolVersion` continua **19**. Os atributos finais já vão no `CharacterStats` e a vida e a Mana máximas no `Health` e no `ResourcePool`. A contabilidade por instância é interna (`StatusStatBonuses`, sem `IReplicated`).
- Ao acabar a Bênção, a vida atual cai junto com a máxima (a mesma diferença, mínimo 1), como quando se tira um equipamento com VIT. Assim quem foi curado até o máximo com a Bênção volta ao máximo sem ela.
- Em monstros, `stats` não faz nada (eles não têm `CharacterStats`).
- A cura da Oração sai com o id do status (`HealApplied.SourceId`), não da habilidade. O log mostra o nome do status; nos dados da T-423, basta o status ter o mesmo nome da habilidade, como os do Guerreiro.
- O requisito 3 não precisou de campo: `extra_targets` já deixa o alvo principal de fora.

### Desvios

- Feita no Windows, e não na VPS, a pedido do designer.
- `src/Game.Core/Combat/Items/Gear.cs` (fora de "efeitos, estados, estatísticas, recursos, dano", mas no Core): uma linha para somar de novo os atributos de status depois de refazer as estatísticas pelo equipamento.

### Pendências

- As habilidades de verdade (Oração Restauradora, Bênção, Graça Veloz, Inspiração, Santuário) e os textos do site são da T-423.
- O cliente não mostra de onde vem o atributo a mais (só o número final na tela de personagem).
