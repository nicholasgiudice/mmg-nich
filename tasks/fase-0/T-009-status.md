# T-009 — Efeitos de status e retorno decrescente

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** T-008
- **Libera:** T-012, T-014
- **Áreas que pode alterar:** `src/Game.Core/Status/`, registro de sistemas, `src/Game.Core/Definitions/` (regras globais), `src/Game.Data/` (carregar regras), `tests/`

## Contexto

Veneno, maldição, medo, lentidão, congelamento, Marca do Caçador, Guarda Firme e Bênção são status. O design exige que controle seja reduzido contra elites e bosses (`design/classes-v0.1.md`, regras de balanceamento).

## Requisitos

1. **Sistema de status**: aplicar, renovar ou empilhar conforme a definição; duração em ticks; expiração; remoção por tag com quantidade.
2. **Efeitos periódicos** (dano/cura ao longo do tempo) usando o registro de efeitos da T-008, com a fonte original preservada nos eventos.
3. **Modificadores** agregados (velocidade, dano recebido, dano causado) alimentando o `IModifierSource` da T-008. Regra de agregação definida e testada (multiplicativa recomendada).
4. **Modificador frontal**: redução de dano recebido apenas quando a fonte está dentro de um arco à frente do alvo (para o Guarda Esquelético e Guarda Firme). Parâmetros: ângulo e multiplicador.
5. **Flags**: `stunned` (não age, não move), `rooted` (não move), `silenced` (não usa habilidades exceto ataque básico). Implementar o `IActionGate` da T-008 e o bloqueio de movimento.
6. **Controle reduzido**: status com tag `control` têm duração multiplicada por rank do alvo e por retorno decrescente em aplicações repetidas dentro de uma janela. Valores em `data/rules.json` (novo arquivo, ex.: `{ "controlDurationByRank": { "normal": 1.0, "elite": 0.5, "boss": 0.25 }, "controlDiminishing": { "windowSeconds": 15, "steps": [1.0, 0.5, 0.25, 0.0] } }`). Adicionar a definição e o carregamento em `Game.Data`.
7. Efeitos `apply_status` e `remove_status` registrados.
8. Eventos: `StatusApplied`, `StatusRefreshed`, `StatusStacked`, `StatusExpired`, `StatusRemoved` (com quem removeu, para Fé/Purificação).

## Critérios de aceite

- Testes: renovar vs empilhar, expiração exata em ticks, DoT com fonte correta;
- stun impede habilidade e movimento; root só movimento; silence permite ataque básico;
- controle em boss dura 25% e retorno decrescente chega a imunidade;
- modificador frontal reduz só quando atacado pela frente;
- `./scripts/check.sh` passa.

## Resultado

**Feito:** sistema de status em `src/Game.Core/Combat/Status/` (`StatusManager`, `StatusModule` com `StatusSystem`/`StatusGate`/`StatusModifierSource`, `StatusComponents`, `StatusEvents`), regras globais em `data/rules.json` (`RulesDefinition`/`ControlRules` em `Definitions/Rules.cs`, carregadas por `Game.Data`). 25 testes novos (`StatusTests.cs` com 20 no Core, `RulesLoaderTests.cs` com 5 em Data); `./scripts/check.sh` passa (97 Core + 67 Data).

**Arquivos compartilhados editados (mínimo):** `CombatServices.cs` (1 linha no slot T-009), `SystemsRegistry.cs` (1 linha, `SystemOrder.Status`), `ContentCatalog.cs` (propriedade `Rules { get; init; }`), `Definitions.cs` (`FrontalModifier` + `StatusDefinition.Frontal`), `GameDataLoader.cs`/`GameDatabase.cs`/`GameDataValidator.cs` (seção `rules`, `Rules`, validação), `data/schemas/game-data.schema.json`, e `CombatTests.cs` (teste "efeito não registrado" usava `apply_status`, agora usa um efeito de teste que nunca é registrado).

**Decisões**
- **Tempo:** status aplicado no tick T com duração D está ativo em T..T+D-1 (`IsActive` = `ExpiresAtTick > world.Tick`, vale mesmo antes do `StatusSystem` rodar). `StatusExpired` sai no tick T+D. DoT dispara em T+k*intervalo, incluindo o tick final (<= ExpiresAt): 3s com intervalo 1s = 3 ticks de dano.
- **Instância:** uma por (entidade, status). Refresh: duração = agora + nova duração (não soma), fonte passa a ser o último aplicador. Stack: +1 acúmulo até `MaxStacks` (`StatusStacked`), renova a duração de todos; no máximo emite `StatusRefreshed`. Efeitos periódicos rodam 1x por acúmulo.
- **Agregação:** multiplicativa; cada status contribui com `mult^acúmulos`; todos se multiplicam. Velocidade 0 se stunned ou rooted (via `SpeedMultiplier`, que o `MovementSystem` já consome).
- **Frontal:** `frontal: { arcDegrees, damageTakenMultiplier }` na definição do status. Arco total centrado no facing do alvo; fonte na mesma posição ou dano sem fonte = fora do arco.
- **Flags:** stun aplicado cancela a conjuração (`CastInterrupted(Disabled)`); silence cancela só conjuração que não seja o ataque básico. Gate: `Stunned` bloqueia tudo; `Silenced` bloqueia tudo menos o ataque básico.
- **Controle:** status com tag `control`: duração = round(base x multiplicador do rank do alvo x passo do retorno decrescente), mínimo 1 tick. Retorno decrescente é por alvo (um balde só), janela contada a partir da PRIMEIRA aplicação (reinicia ao fim dela). Passo 0 = imune: publica `StatusResisted`, não aplica e não conta. Renovar um controle conta como aplicação.
- **Regras:** `rules` é um objeto raiz no JSON, uma seção por assunto (`control` agora; T-011 adiciona `threat`). Seções de arquivos diferentes se combinam; a mesma seção em dois arquivos é erro. Para nova seção: uma propriedade em `RulesDefinition` (marcador de comentário no arquivo). Seção ausente = padrão neutro (sem redução). A janela no JSON é `"window"` em segundos (convenção do sufixo `Ticks`), em vez de `windowSeconds` do exemplo.
- Entidade morta: `StatusSystem` ignora (sem DoT); `StatusManager.Apply` em morta retorna null. Fonte removida do mundo: o DoT continua, mas `Source` do evento vira `None`.
- Pendências: nenhuma. Obs.: `LoadDirectory("data")` também lê `data/schemas/*.json` (comportamento anterior da T-005, não alterado).

**Como outras tarefas aplicam/consultam status (guia)**
- **Aplicar:** `StatusManager.Apply(world, source, target, "status.id", durationTicks?)` retorna a instância ou null (alvo morto, ou controle imune). Em dados: efeito `apply_status` (`status`, `duration` opcional) ou `remove_status` (`tag`, `count`), já registrados.
- **Remover:** `StatusManager.RemoveByTag(world, target, tag, count, byEntityId)` (mais antigos primeiro; retorna quantos) e `StatusManager.Remove(world, target, statusId, by)`. Gera `StatusRemoved` com `By`.
- **Consultar:** `Has/Get/Stacks/HasTag/IsStunned/IsRooted/IsSilenced(world, entity, ...)`, `Active(world, entity)`. Dados brutos no componente `StatusEffects` (`entity.TryGet<StatusEffects>()`). Sempre use `StatusManager.IsActive` (não leia `Active` direto) porque instâncias expiradas podem ficar na lista até o `StatusSystem` do tick.
- **Eventos** (`Game.Core.Combat.Status`): `StatusApplied(Source, Target, StatusId, InstanceId, Stacks, ExpiresAtTick, DurationTicks)`, `StatusRefreshed`, `StatusStacked`, `StatusExpired(Entity, StatusId, InstanceId)`, `StatusRemoved(Entity, StatusId, InstanceId, By)`, `StatusResisted(Source, Target, StatusId)`. Fé/Purificação: ler `StatusRemoved.By`. T-012 (Marca do Caçador, Guarda Firme, bênção) e T-014 só precisam de dados de status; IA (T-011) deve consultar `IsStunned`/`IsRooted` antes de emitir intenções (o gate e o movimento já bloqueiam de qualquer forma).
- **Dano de DoT:** passa por `EffectApplier`/`DamageResolver` com `SourceId` = id do status e `Source` = quem aplicou por último, então ameaça (T-011) e log (T-013) funcionam sem código extra.
- **Testes:** helpers em `StatusTests.cs` (`St`, `Unit`, `NewWorld`) montam mundo com `ContentCatalog { Rules = ... }`. Eventos publicados entre ticks (ex.: `Apply` chamado fora do `Step`) aparecem no `LastTickEvents` do próximo `Step`.
