# T-406 — Core: mecânicas novas do Guardião

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** —
- **Libera:** T-407
- **Áreas que pode alterar:** `src/Game.Core/` (efeitos, estados, dano, ameaça), `src/Game.Data/` (carga e validação dos campos novos), `data/schemas/`, `tests/Game.Core.Tests/`, `tests/Game.Data.Tests/`
- **Branch:** sai de `trilha/segundas-classes` e é integrada nela, não na `main` (seção 6f de `tasks/README.md`).

## Contexto

Design em `design/segunda-classe-v0.1.md`, seção 5 (Guardião e a tabela "Mecânicas do Guerreiro: o que já existe e o que é novo"). Esta tarefa cria **as mecânicas** no Core, cada uma com teste e com dados de teste. As habilidades de verdade vêm na T-408.

O que já existe e pode ser reaproveitado:

- dano em `DamageResolver.Deal` (`src/Game.Core/Combat/Effects/CoreEffects.cs`), com escudos, defesa, multiplicadores de estado e reações depois do dano (`IDamageReaction`);
- estados com `modifiers`, `flags`, `frontal`, `reflect`, `immuneToTags` e efeitos periódicos (`data/statuses.json`, `src/Game.Core/Combat/Status/`);
- avanço (`displacement` com `kind: dash` e `direction: toward_target`), escudo, atordoamento, tirar estados (`remove_status`), estados em aliados num raio (Muralha Viva) e ameaça (`threat`).

## Requisitos

1. **Desviar dano de um aliado** (Interceptação, Juramento; depois o Elo da Vida do Sacerdote):
   - um estado no aliado aponta para quem recebe o desvio (quem aplicou o estado, ou outra entidade escolhida pelo efeito), com a **fração desviada** e uma **redução** opcional sobre a parte desviada;
   - no `DamageResolver`, antes de aplicar o dano no aliado, a fração sai do dano dele e vai para o protetor. A parte desviada passa pela **defesa do protetor** e pela redução do estado, mas não pelos escudos do aliado;
   - dano desviado **não é desviado de novo** (sem cadeia nem laço), e nada é desviado para um protetor morto ou longe demais (distância máxima no estado);
   - eventos e log de combate mostram o desvio (quem protegeu, quanto).
2. **Avançar até um aliado** (Interceptação): `displacement` com `toward_target` quando o alvo é um aliado. Se já funciona, só o teste; se não, o ajuste.
3. **Imunidade a empurrão** (Bastião NH5): flag de estado que ignora `displacement` do tipo `knockback` vindo de inimigos.
4. **Ameaça multiplicada pelo dano** (Golpe de Escudo): o efeito de dano (ou o de ameaça) pode gerar ameaça igual a N vezes o dano causado, em vez da ameaça padrão (`rules.threat.damageMultiplier`).
5. Validação dos campos novos e schema.

## Critérios de aceite

Testes no Core:

- aliado com desvio de 30% recebe 70% do dano; o protetor recebe 30% menos a defesa e a redução dele; o total bate com a conta;
- dois protetores em cadeia não fazem laço; protetor morto ou longe não recebe nada;
- escudo do aliado não absorve a parte desviada;
- Interceptação de teste leva o Guardião até o aliado;
- com a imunidade, um empurrão de monstro não move o alvo; sem ela, move;
- golpe com ameaça × 3 gera três vezes a ameaça do dano;
- `./scripts/check.sh` completo passa.

## Resultado

Feito em 2026-11-21, na VPS, branch `task/T-406-mecanicas-do-guardiao` (a partir de `main` em `c011054`).

### O que foi feito

1. **Desvio de dano** (`redirect` no status).
   - `StatusDefinition.Redirect` (`RedirectDefinition`): `fraction` (0, 1], `reduction` [0, 1) opcional e `maxDistance` (> 0, obrigatório).
   - Quem recebe: `StatusInstance.RedirectTo`, com `StatusInstance.Protector` = `RedirectTo` ou, sem ele, quem aplicou (`Source`).
     `StatusManager.Apply(..., redirectTo: id)` liga o status a outra entidade (para o Elo da Vida); sem o parâmetro, é quem
     aplicou. Renovar o status atualiza o vínculo.
   - `DamageResolver.Deal`: antes de tudo, `DamageRedirect.Shares` escolhe os desvios válidos do alvo (protetor vivo, que não é
     o próprio alvo, a até `maxDistance`); a fração sai do valor base; o alvo recebe o resto com os modificadores e escudos dele;
     cada protetor recebe `base × fração × (1 − reduction)` como um novo dano do mesmo atacante, com a defesa, a resistência,
     os modificadores e os escudos **do protetor** (não os do aliado). Esse dano não passa de novo pelo desvio (sem cadeia nem
     laço). Vários desvios no mesmo alvo somam no máximo 100% (o que vem primeiro leva a fração cheia).
   - Evento novo `DamageRedirected(Attacker, Target, Protector, SourceId, StatusId, Amount)`, publicado depois do
     `DamageDealt` no protetor; log: "Guardião protegeu Maga com Interceptação: 15 de dano desviado".
2. **Avançar até um aliado**: `displacement` `dash` + `toward_target` já mirava em aliado, mas andava a distância cheia e
   passava do alvo. Ajuste: o avanço para no contato (soma dos raios), sem passar do alvo; alvo longe continua andando só a
   `distance`. Nenhum dado atual usava `toward_target` em `dash`, então nada mudou no jogo de hoje.
3. **Imunidade a empurrão**: `flags.knockbackImmune`. `displacement` `knockback` vindo de inimigo (ou sem fonte) não move
   quem tem o status (e não publica `Displaced`); empurrão de aliado continua valendo.
4. **Ameaça multiplicada pelo dano**: `damage.threatMultiplier` (≥ 0). A ameaça desse dano é `threatMultiplier × dano`, no
   lugar de `rules.threat.damageMultiplier`. Vai do efeito ao `AiSystem` por uma propriedade interna do `DamageDealt`
   (`ThreatMultiplier`, `internal`): só o servidor usa e ela fica fora da rede.
5. **Validação e schema**: `GameDataValidator` (faixas de `redirect.*` e `threatMultiplier`) e `data/schemas/game-data.schema.json`
   (`redirect`, `flags.knockbackImmune`, `damage.threatMultiplier`).

### Campos novos nos dados (para a T-408)

```json
{ "id": "status.intercepted", "defaultDuration": 4, "redirect": { "fraction": 0.3, "maxDistance": 8 } }
{ "id": "status.oath",        "defaultDuration": 8, "redirect": { "fraction": 0.5, "reduction": 0.4, "maxDistance": 6 } }
{ "id": "status.bastion",     "defaultDuration": 8, "flags": { "knockbackImmune": true } }
{ "type": "damage", "base": 10, "scaling": { "weapon": 0.8, "vit": 1.5 }, "threatMultiplier": 3 }
{ "type": "displacement", "kind": "dash", "distance": 8, "direction": "toward_target" }
```

### Testes

- `tests/Game.Core.Tests/GuardianMechanicsTests.cs` (15): 30% desviado → aliado 70, Guardião `30 × (1 − redução) × (1 − defesa)`
  e o total bate; a parte desviada ignora os modificadores do aliado e usa os do protetor; escudo do aliado não absorve a
  parte desviada; cadeia A→B→C→A só dá um salto; protetor morto ou longe não recebe nada; desvios somam até 100%; vínculo a
  outra entidade (`redirectTo`) e renovação; vínculo na cópia e no hash; log de combate; Interceptação de teste leva o Guardião
  até o aliado (para no contato) e o protege; avanço longe anda só a distância; empurrão de monstro com e sem imunidade;
  empurrão de aliado com imunidade; Golpe de Escudo × 3 gera três vezes a ameaça do dano.
- `tests/Game.Data.Tests/GuardianMechanicsLoaderTests.cs` (7): carga dos campos e erros de validação.
- `tests/Game.Protocol.Tests/ProtocolTests.cs`: evento e vínculo passam pela rede.

### Desvios (fora das áreas listadas, o mínimo)

- `src/Game.Protocol/`: o evento novo precisa de formato de rede (o teste exige um para todo evento do Core) e o vínculo do
  status ativo vai no snapshot. Tag de evento `26` (`DamageRedirected`), campo `RedirectTo` no `StatusEffects` e
  **`ProtocolVersion` 16**. Cliente e servidor precisam sair do mesmo commit, como sempre.
- `tests/Game.Protocol.Tests/ProtocolTests.cs`: o teste da T-395 fixava a versão 15; passou a `>= 15`, e o teste novo fixa a 16.
  Se outra tarefa da onda também subir a versão, o conflito é só nesse número e no comentário de `ProtocolVersion`.
- `src/Game.Core/Combat/Ai/AiSystem.cs` (ameaça) e o log de combate (`CoreCombatLogFormatters`) estão no Core, dentro da área.

### Decisões

- A parte desviada sai do valor **base** (antes dos multiplicadores do alvo), então um aliado vulnerável não passa a
  vulnerabilidade ao protetor, e o Bastião no Guardião reduz o que ele recebe pelo desvio.
- Dano sem modificadores (`applyModifiers: false`, como os reflexos, que já vêm calculados) não é desviado.
- A parte desviada não conta como corpo a corpo no protetor (não dispara o reflexo frontal da Guarda Firme nele).
- Acúmulos não aumentam a fração desviada.

### Pendências

- As habilidades de verdade (Interceptação, Golpe de Escudo, Bastião, Juramento) e os textos do site são da T-408.
- O Elo da Vida (Sacerdote) precisa de um efeito que escolha "o aliado com mais vida perto" e passe `redirectTo`; o Core já
  aceita o vínculo, falta a escolha.
- O cliente ainda não mostra o `DamageRedirected` (número flutuante ou linha entre os dois); o log de combate já mostra.
