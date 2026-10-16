# T-017 — Mecânicas faltantes do design

- **Ambiente:** VPS
- **Modelo:** Forte
- **Depende de:** T-016
- **Libera:** T-018
- **Áreas que pode alterar:** `src/Game.Core/` (exceto `Authority/`, que é do cliente), `src/Game.Data/`, `data/`, `tests/Game.Core.Tests/`, `tests/Game.Data.Tests/`, `scripts/check.sh`, `AGENTS.md` (só a menção ao modo rápido), `tasks/README.md` (tabela da seção 5)

## Contexto

As tarefas T-014 e T-016 registraram lacunas entre o design e o Core (ver `## Resultado

**Feito.** Os 12 requisitos. Core (sem tocar em `Authority/`), dados, schema, validação, 16 testes de Core (`tests/Game.Core.Tests/MissingMechanicsTests.cs`), 6 de Data (`MissingMechanicsLoaderTests.cs` + 4 em `PrototypeContentTests.cs`), `check.sh --fast`. `./scripts/check.sh` completo passa (195 Core, 98 Data, 15 Sim, 13 Design). Só acrescentei APIs; nenhuma foi renomeada ou removida.

**Formatos JSON novos**

- Alvo por efeito (em qualquer efeito): `{ "type": "heal", "base": 10, "target": "self" }` (`targets` é o padrão). Efeitos `self` ignoram a regra de facção (só os `targets` decidem se a habilidade é benéfica) e são pulados se o conjurador morreu.
- `bonusVsTags` no `damage`: `{ "type": "damage", "school": "holy", "base": 26, "bonusVsTags": { "undead": 0.25, "corrupted": 0.25 } }` (bônus de tags diferentes somam; valor deve ser > -1).
- `remove_status` aceita `tags` (qualquer uma): `{ "type": "remove_status", "tags": ["poison", "curse"], "count": 1 }`. `tag` continua valendo (agora opcional se houver `tags`); remove os mais antigos primeiro.
- Cargas: `"charges": 3` na habilidade (inteiro >= 1; ausente = ilimitado). Consumíveis: `"consumables": ["item.minor_healing_potion"]` na classe.
- `modifiers.controlDurationMultiplier` no status: `"modifiers": { "controlDurationMultiplier": 0.5 }`.
- `"armTime": 0.5` na área (segundos; deve ser menor que `duration`).

**APIs novas para o cliente**

- `ClassDefinition.Consumables`; no jogador spawnado, `AbilityState.Consumables` (lista separada de `Known`; `Knows(id)` cobre os dois). Os bots e `ClassAbilities` leem só `Known`, então não usam consumíveis (a T-018 decide isso).
- Cargas: `AbilityState.ChargesRemaining(AbilityDefinition)` -> `int?` (null = ilimitada); gastas ficam em `ChargesUsed` (entram no hash/snapshot). `AbilityExecuted.ChargesRemaining` (campo novo opcional no fim do record, `int?`). Cargas são gastas ao concluir a conjuração, como custo e recarga.
- `AbilityRejectReason.NoCharges` (valor novo no fim do enum). Ordem de validação: ... conhecida, **cargas**, recarga, recurso, alvo, alcance, LoS. Texto de log: "sem cargas".
- `AreaArmed(Area, AreaId, Owner)` (novo evento, só para áreas com `armTime` > 0; log "X está armada"). `AreaSpawned` ganhou `ArmedAtTick` (opcional, no fim) para a UI desenhar "armando". Áreas armando não disparam (periódicas e `on_enter_once`), mas a duração corre.
- Novos tipos/propriedades: `EffectTarget` (enum), `EffectDefinition.Target`, `DamageEffect.BonusVsTags`, `RemoveStatusEffect.Tags`, `AbilityDefinition.Charges`, `StatModifiers.ControlDurationMultiplier`, `AreaDefinition.ArmTimeTicks`, `StatusManager.RemoveByTags` e `StatusManager.ControlDurationMultiplier(world, entity)`.

**Conteúdo e números**

- `warrior.taunt` (Provocação): `circle_self` raio 8, sem custo, recarga 10 s, `threat` 40 + provocação 3 s.
- `cleric.purify` (Purificação): aliado (ou o próprio), alcance 8, recarga 8 s, remove 1 status `poison` ou `curse`; duas regras de Fé `status_removed` (+12 cada, uma por tag). **Sem custo de Fé**: com custo 10 o bot `rotation` a gastava à toa (não checa se há o que purificar) e a Corrente Sagrada deixava de ser usada. Ela só gera Fé quando remove algo.
- `cleric.holy_chain`: dano sagrado 26 com `bonusVsTags` undead/corrupted +25%, mais cura própria `target: self` de 10. `cleric.holy_strike` (básico) também tem +25% contra undead/corrupted. Removi `resistances.holy = -0.25` do Guarda Esquelético (o bônus por tag a substitui, com o mesmo efeito de +25%).
- `item.minor_healing_potion` (novo arquivo `data/abilities_items.json`): `self`, cura 60, 3 cargas por encontro, recarga 20 s; em `consumables` das 4 classes.
- `status.frenzy`: `controlDurationMultiplier` 0,5 (multiplica com o multiplicador de rank de boss das `rules`). `area.containment_trap`: `armTime` 0,5 s.
- `tasks/README.md` (seção 5) atualizado; `AGENTS.md` menciona `--fast`; `scripts/check.sh --fast` pula `tests/Game.Design.Tests` (sem argumento roda tudo).

**Desvios / efeito nos testes de design**

- `src/Game.Bots/AbilityInfo.cs` (fora das áreas listadas, mudança mínima): o papel da habilidade agora considera só efeitos com `target: targets`. Sem isso a Corrente Sagrada (que tem cura própria) virava papel `Heal` e o `rotation` do Clérigo parava de usá-la (Guarda Esquelético 48 s em vez de 26 s). Comportamento idêntico para habilidades sem `target: self`.
- Com isso, Clérigo x Guarda ficou em 26,3 s (igual a antes) usando bônus de +25%. Bônus maior (+50%) deixava o Clérigo em 21,7 s e quebrava o teste 1,2 a 1,6.
- Matriarca: vida 3825 -> 4000. A Provocação (usada pelo `rotation`/`kite` do Guerreiro) levou o grupo sem Clérigo a 72% (limite 70%). Sem outros ajustes. Hoje: grupo `kite+dodge` 100%, `rotation` parado 4%.
- `taunt_and_basic` segue com `warrior.whirlwind` via `--opt` (mantido como está).
- Pendências para a T-018: bots não usam poção nem checam se Purificação tem algo a remover; o grupo sem Clérigo ainda depende da Matriarca enfraquecida (agora que há poção, dá para reverter parte); Altar do Guarda Esquelético continua de fora.
