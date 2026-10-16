# T-010 — Recursos por classe orientados a eventos

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** T-008
- **Libera:** T-014, T-022
- **Áreas que pode alterar:** `src/Game.Core/Resources/`, `ClassDefinition` e carregador (campo de regras de recurso), registro de sistemas, `tests/`

## Contexto

Leia `design/classes-v0.1.md` e a seção 3 de `design/class-map-validation-v0.1.md`. Pontos-chave:

- **Vigor** (Guerreiro) deve recompensar decisões: mais ao bloquear golpe forte e interromper do que ao apenas apanhar;
- **Foco** (Arqueiro): acertos a distância segura e em alvo marcado;
- **Mana** (Mago): regeneração passiva, gasto relevante em área;
- **Fé** (Clérigo): curas relevantes, purificação, proteção preventiva, ataques contra corrompidos/mortos-vivos.

## Requisitos

1. **Regras de geração** declaradas em dados na `ClassDefinition` (`resourceRules`), cada uma com:
   - evento gatilho (`damage_taken`, `damage_dealt`, `heal_done`, `shield_applied`, `status_removed`, `interrupt_done`, `ability_executed`…);
   - quantidade fixa ou proporcional ao valor do evento;
   - condições opcionais: valor mínimo, tag do alvo, tag da habilidade, distância mínima/máxima ao alvo, alvo com status de tag X, fonte com status de tag X (ex.: dano recebido **enquanto** em Guarda Firme conta como bloqueio), cura efetiva mínima em % da vida máxima;
   - recarga interna opcional da regra.
2. **Regeneração/decaimento passivo** por segundo (já na definição da T-005), com opção de só regenerar fora de combate se fizer sentido — registrar decisão.
3. Sistema que avalia as regras sobre os eventos do tick.
4. Efeito `resource` (ganhar/gastar) registrado.
5. Evento `ResourceChanged` com motivo (regra/efeito/regeneração).

## Critérios de aceite

- Teste: Guerreiro ganha mais Vigor bloqueando em Guarda Firme do que recebendo o mesmo dano sem guarda;
- teste: Foco só é gerado acima da distância mínima;
- teste: cura com excedente total não gera Fé; purificação gera;
- teste: recarga interna da regra impede geração repetida;
- `./scripts/check.sh` passa.

## Resultado

**Feito:** módulo de recursos em `src/Game.Core/Combat/Resources/` (a pasta `Resources/` sugerida na tarefa ficou dentro de `Combat/`, conforme o guia da T-008): `ResourceSystem` (ordem `SystemOrder.Resources`), `ResourceModule` (efeito `resource` + `Change`), `ResourceChanged`/`ResourceChangeReason`, `ResourceState` (componente interno), `ResourceServices`/`IStatusQuery`. Definições novas em `Game.Core/Definitions/ResourceRules.cs`; `ClassDefinition.ResourceRules`; `ResourceDefinition.PassiveOnlyOutOfCombat` e `CombatTimeoutTicks`. Game.Data: validação das regras + schema JSON. 10 testes Core (`ResourceTests.cs`) e 2 de Data; `./scripts/check.sh` passa (87 Core + 64 Data).

**Regras (`resourceRules` na classe).** Campos: `id` (único na classe), `trigger`, `amount` (fixo, pode ser negativo) e `ratio` (fração do valor do evento; gerado = amount + ratio x valor), condições opcionais `minValue`, `targetTag`, `abilityTag`, `minDistance`, `maxDistance`, `targetHasStatusTag`, `sourceHasStatusTag`, `minEffectiveHealPercent`, `removedStatusTag`, `internalCooldown` (s). Gatilhos e "dono" (quem ganha, sempre o recurso da própria classe):
- `damage_taken`: dono = vítima; valor = dano final (`Amount`). `damage_dealt`: dono = fonte; valor = dano final.
- `heal_done`: dono = curandeiro; valor = cura **efetiva** (cura 100% excedente nunca dispara). `shield_applied`: dono = fonte; valor = escudo.
- `interrupt_done`: `CastInterrupted(Reason=Interrupted, By)`; dono = `By`; `abilityTag` refere-se à habilidade interrompida.
- `ability_executed`: dono = conjurador, alvo = primeiro alvo resolvido; dispara uma vez por uso (purificação = habilidade com tag `cleanse` + regra com `abilityTag`).
- `status_removed`: ver "Ligar com a T-009".
- Semântica das condições: "alvo" e "fonte" são os do evento (em `damage_taken` o alvo é o próprio dono). **`sourceHasStatusTag` verifica o DONO** da regra (quem ganha o recurso), para cobrir "dano recebido enquanto em Guarda Firme"; `targetHasStatusTag` verifica o alvo do evento. Distância = entre centros. `abilityTag` usa o `SourceId` do evento (id da habilidade; status/área sem habilidade ficam sem tags).
- Recarga interna por dono e por regra (`ResourceState.RuleReadyAt`, no hash); só começa quando a regra efetivamente dispara.

**Efeito `resource`:** `ResourceEffect{resource, amount}` aplicado a cada alvo; positivo ganha, negativo gasta; limitado a 0..máx; ignora alvo morto ou com outro tipo de recurso.

**Evento `ResourceChanged(Entity, Resource, Delta, Current, Max, Reason, SourceId)`**, Reason = Rule (SourceId = id da regra) / Effect / Regen ("passive") / Cost. Só é publicado se a variação efetiva != 0. O custo de habilidade continua sendo descontado pelo `AbilitySystem` (não editei); o `ResourceSystem` apenas publica `ResourceChanged(Cost)` a partir de `AbilityExecuted`, com `Current` = valor no momento da publicação.

**Regeneração/decaimento passivo:** `RegenPerSecond` da classe, acumulado por tick em fração e aplicado em unidades inteiras (evita um evento por tick). Decisão: por padrão vale sempre; `resource.passiveOnlyOutOfCombat: true` faz valer só fora de combate. Combate = ter dado/recebido dano, curado, protegido ou usado habilidade nos últimos `resource.combatTimeout` segundos (padrão 5 s = 100 ticks). A classe decide por dados (ex.: Vigor decai só fora de combate; Mana regenera sempre).

**Decisões:** o `ResourceSystem` identifica a classe de uma entidade por `UnitInfo.DefinitionId` -> `world.Content.Classes`. `ResourceState` é criado sob demanda. Dono morto não gera. Efeitos de regra/regen usam o mesmo `ResourceModule.Change`. O teste antigo `Custom_effect_can_be_registered_without_touching_core` (T-008) usava `ResourceEffect` como efeito "sem handler"; troquei por um efeito local de teste (`CustomTestEffect`), pois `ResourceEffect` agora tem handler real.

**Ligar com a T-009 (ou o integrador):**
1. **Consulta de status:** implementar `Game.Core.Combat.Resources.IStatusQuery` (`bool HasStatusWithTag(World, Entity, string tag)`) e atribuir em `ResourceModule.For(s).StatusQuery = ...` no slot da T-009 em `CombatModules.Register` (`s` = `CombatServices`; `For` funciona em qualquer ordem de registro). Enquanto isso, o padrão (`NoStatusQuery`) retorna false e as regras com `sourceHasStatusTag`/`targetHasStatusTag` nunca disparam. A avaliação acontece em `SystemOrder.Resources` (4000), depois do sistema de status: um status que expira no mesmo tick já não conta.
2. **`status_removed`:** a regra e o gatilho já são aceitos nos dados. Ao remover um status, a T-009 deve chamar `ResourceModule.For(world.Combat).NotifyStatusRemoved(new StatusRemovalNotice(remover, holder, statusId, statusTags, sourceId))` no mesmo tick (antes de 4000), com `remover` = `EntityId` de quem causou a remoção (dono que ganha; `EntityId.None` = ninguém ganha) e `sourceId` = id da habilidade (para `abilityTag`). Alternativa: uma ponte que leia o evento `StatusRemoved` e faça a mesma chamada. Remoções por expiração natural não devem ser notificadas (ou usar `EntityId.None`).
3. Nenhum conteúdo (`data/*.json`) foi criado; as regras reais das 4 classes ficam para a T-014.

**Arquivos compartilhados editados (mínimo):** `Combat/CombatServices.cs` (1 linha no slot T-010), `Simulation/SystemsRegistry.cs` (1 linha), `Definitions/Definitions.cs` (`ResourceRules` em `ClassDefinition`), `Definitions/Common.cs` (2 campos em `ResourceDefinition`), `Game.Data/GameDataValidator.cs` (validação de regras), `data/schemas/game-data.schema.json`, `tests/Game.Core.Tests/CombatTests.cs` (teste T-008, ver acima), `tests/Game.Data.Tests/GameDataLoaderTests.cs`.

**Pendências:** ligações com a T-009 acima. Regras e números por classe: T-014.


**Integração (feita no merge):** `src/Game.Core/Combat/Integration/StatusResourceBridge.cs` liga `IStatusQuery` ao `StatusManager.HasTag` e o sistema `StatusRemovalBridgeSystem` (`SystemOrder.StatusResourceBridge` = 3500) converte `StatusRemoved` com `By != None` em `NotifyStatusRemoved` (sourceId = id do status). Testes em `tests/Game.Core.Tests/StatusResourceIntegrationTests.cs`.
