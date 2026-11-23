# T-431 — Core: mecânicas novas do Atirador de Elite

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** —
- **Libera:** T-432
- **Áreas que pode alterar:** `src/Game.Core/` (efeitos, estados, estatísticas, dano, mira), `src/Game.Data/` (carga e validação dos campos novos), `data/schemas/`, `tests/Game.Core.Tests/`, `tests/Game.Data.Tests/`
- **Branch:** sai da `main` e é integrada na `main` (seção 6h de `tasks/README.md`).

## Contexto

Design em `design/segunda-classe-v0.1.md`, seção 6 (Atirador de Elite e a tabela "Mecânicas do Arqueiro"). Aprovado pelo designer em 2026-11-22, com duas mudanças: o **Tiro Certeiro sem conjuração** e o **Olho de Falcão de 15 a 20 s**. Esta tarefa cria **as mecânicas** no Core, cada uma com teste e com dados de teste. As habilidades de verdade vêm na T-433.

O Atirador tem Flecha Perfurante, Tiro Debilitante, Mira Firme (passiva), Tiro Certeiro e Olho de Falcão (assinatura). O Tiro Debilitante cabe no que existe: dano, lentidão e menos dano causado.

O que já existe e pode ser reaproveitado:

- crítico em `CharacterStats` (`StatMath.CritChance`): `critBase + AGI × critPerAgi + CritBonus`, com teto `critMax` 40% e multiplicador `critMultiplier` 1,5 (`data/rules.json`);
- mira em linha (`targeting.type: line`, Flecha Devastadora), alcance por habilidade;
- defesa e armadura em `DamageResolver.Deal` (`src/Game.Core/Combat/Effects/CoreEffects.cs`);
- passivas como estado permanente (T-403), estados com `modifiers` e `flags`.

## Requisitos

1. **Dano menor em quem está atrás** (Flecha Perfurante): numa habilidade em linha, o primeiro alvo (o mais perto de quem atira) leva o dano inteiro e os outros levam uma fração (60% no NH1, 80% a partir do NH3).
2. **Crítico pela distância até o alvo** (Mira Firme): um estado soma chance de crítico só contra alvos a N tiles ou mais (6 no design), medida no momento do acerto. No NH5, o crítico contra esses alvos usa outro multiplicador (× 1,65 em vez de × 1,5).
3. **Alcance a mais vindo de um estado** (Mira Firme NH3, Olho de Falcão NH3): soma tiles ao alcance das habilidades à distância de quem tem o estado (o ataque básico e as habilidades com alcance de unidade ou linha). Sugestão: só para habilidades com uma etiqueta (por exemplo, `ranged`), para não alongar golpes corpo a corpo.
4. **Ignorar parte da armadura** (Tiro Certeiro): o efeito de dano ignora uma fração da armadura do alvo (30%, 40%, 50%) antes de calcular a defesa.
5. **Crítico garantido** (Olho de Falcão): enquanto o estado está ativo, todo dano de habilidade e de ataque básico de quem o tem é crítico, acima do teto de 40%. Dano periódico (venenos, sangramento) segue a regra que já existe para crítico.
6. Validação dos campos novos e schema.

Se o cliente precisar do alcance novo para mostrar a mira, mandar o alcance já somado pelo caminho que já existe, ou subir `ProtocolVersion` a partir do número da `main` e registrar no Resultado.

## Critérios de aceite

Testes no Core:

- linha com 3 monstros e fração de 60%: o primeiro leva 100%, os outros dois 60%;
- Mira Firme de teste com +10%: a chance de crítico sobe 10 pontos contra um alvo a 7 tiles e não muda contra um alvo a 5; com o multiplicador de 1,65, o crítico a 7 tiles causa 1,65 vez o dano, e a 5 tiles causa 1,5;
- com +1 de alcance, um disparo de alcance 10 acerta um alvo a 11 e não acerta a 12; um golpe sem a etiqueta não muda;
- Tiro Certeiro de teste com 50% da armadura ignorada causa o dano da conta com metade da armadura;
- com o Olho de Falcão de teste, 100 disparos seguidos são todos críticos, mesmo com a chance base no teto; sem o estado, a chance volta à de antes;
- o resultado do sorteio segue o RNG da simulação (mesma seed, mesmos críticos);
- `./scripts/check.sh` completo passa.

## Resultado

Feito em 2026-11-22, **no Windows** (a tarefa não usa tela), branch `task/T-431-mecanicas-do-atirador`, a partir de `main` em `8f5c228`.

### O que foi feito

1. **Dano menor em quem está atrás** (`damage.behindFraction`, em (0, 1]): entre os alvos do uso, o mais perto de quem usou (empate: menor id) leva o dano inteiro e os outros levam a fração. Quem morre no próprio acerto continua sendo o primeiro (os outros não passam a levar o dano inteiro). Com um alvo só, nada muda. Vale para qualquer mira com vários alvos, não só linha (`PierceFalloff`, `Combat/MarksmanMechanics.cs`).
2. **Crítico vindo de um status** (`crit` no status, `StatusCritDefinition`, `StatusCrit` em `Combat/MarksmanMechanics.cs`):
   - `crit.bonus` (× acúmulos) soma à chance de crítico contra alvos a `crit.minDistance` tiles ou mais, medida no acerto **como o alcance**: do centro de quem ataca à borda do alvo (distância − raio do alvo). `minDistance` 0 = qualquer distância;
   - `crit.multiplier` troca o `critMultiplier` (1,5) contra esses alvos; com vários status, vale o maior;
   - `crit.guaranteed`: todo dano de quem tem o status é crítico, acima do teto de 40%, sem consumir o RNG;
   - vale só para dano com habilidade de origem (habilidades e ataque básico, inclusive alvos extras, saltos e telegráfos). Dano periódico de status e de área segue a regra de sempre (`StatMath.RollCrit`);
   - sem status que valha contra aquele alvo, o sorteio é o mesmo de antes, com o mesmo consumo do RNG. Por isso nenhum dado atual muda de resultado;
   - `StatusCrit.Chance(world, fonte, alvo, habilidade)` dá a chance efetiva (para testes e, se quiserem, para a tela).
3. **Alcance a mais vindo de um status** (`rangeBonus` no status, tiles × acúmulos): soma no alcance das habilidades com a tag **`ranged`** de quem tem o status. Vale para o alcance de `unit`, o comprimento de `line` e o alcance de `circle_at_point` (não o raio). `melee_arc`, `circle_self` e habilidades sem a tag não mudam. O Disparo e as habilidades do Arqueiro já têm a tag `ranged`. Também vale na troca de alvo dos acertos extras (`AbilitySystem.ExtraHit`). Função `RangeBonus.Of(content, statusEffects, tick, ability)` para o cliente (ver Pendências).
4. **Ignorar parte da armadura** (`damage.ignoreArmor`, em [0, 1]): `DamageResolver.Deal(..., ignoreArmor)` → `StatMath.DefenseReduction(..., ignore)`, que faz `D × (1 − ignore)` antes da conta `D / (D + base + porNível × nível)`. Vale para a defesa da escola do dano (armadura contra físico, resistência contra o resto) e também na parte desviada a um protetor (T-406).
5. **Validação e schema**: `GameDataValidator.Marksman.cs` (arquivo novo; uma linha em `GameDataValidator.cs` para danos e uma para status) e `data/schemas/game-data.schema.json` (`behindFraction`, `ignoreArmor`, `crit`, `rangeBonus`).

### Campos novos nos dados (para a T-433)

```json
{ "type": "damage", "base": 14, "scaling": { "weapon": 1.4, "dex": 2.8 }, "behindFraction": 0.6 }
{ "type": "damage", "base": 25, "scaling": { "weapon": 2, "dex": 4 }, "ignoreArmor": 0.3 }

{ "id": "status.steady_aim_1", "name": "Mira Firme", "defaultDuration": 1,
  "crit": { "bonus": 0.06, "minDistance": 6 } }
{ "id": "status.steady_aim_3", "name": "Mira Firme", "defaultDuration": 1,
  "crit": { "bonus": 0.10, "minDistance": 6 }, "rangeBonus": 1 }
{ "id": "status.steady_aim_5", "name": "Mira Firme", "defaultDuration": 1,
  "crit": { "bonus": 0.12, "minDistance": 6, "multiplier": 1.65 }, "rangeBonus": 1 }

{ "id": "status.hawk_eye_1", "name": "Olho de Falcão", "defaultDuration": 15, "crit": { "guaranteed": true } }
{ "id": "status.hawk_eye_3", "name": "Olho de Falcão", "defaultDuration": 20, "crit": { "guaranteed": true }, "rangeBonus": 2 }
```

Faixas: `behindFraction` em (0, 1]; `ignoreArmor` em [0, 1]; `crit.bonus` em [0, 1], `crit.minDistance` ≥ 0, `crit.multiplier` ≥ 1, e `crit` precisa de pelo menos `bonus`, `multiplier` ou `guaranteed`; `rangeBonus` ≥ 0. A Mira Firme é passiva (`passive` + `passiveStatus` por NH, T-403); o Olho de Falcão é um `apply_status` com `target: self`. O Tiro Certeiro fica sem conjuração (sem `castTime`). O Tiro Debilitante não precisa de nada novo (dano, `speedMultiplier` e `damageDealtMultiplier` num status).

### Testes

- `tests/Game.Core.Tests/MarksmanMechanicsTests.cs` (16): linha com 3 monstros e 60% (o mais perto leva 100, os outros 60, criados fora de ordem); o primeiro morrendo no acerto continua sendo o primeiro; alvo só leva o dano inteiro; Mira Firme +10% sobe a chance a 7 tiles e não a 5; com 1,65 o crítico a 7 tiles causa 165 e a 5 tiles 150; críticos sorteados da Mira Firme NH5 sempre com 1,65; +1 de alcance: alcance 10 acerta a 11 e não a 12, sem a tag não muda, a linha também cresce, acaba com o status; Tiro Certeiro com 50% contra armadura 100 causa a conta com armadura 50 (49, contra 32 sem ignorar); Olho de Falcão: 100 disparos seguidos críticos com a chance no teto, e sem o status a chance volta a 40% e há disparos sem crítico; veneno de quem tem o Olho de Falcão segue a regra normal; o status só vale para quem o tem; mesma seed = mesmos críticos, outra seed = outros; status que não vale contra o alvo mantém o sorteio igual ao de quem não o tem.
- `tests/Game.Data.Tests/MarksmanMechanicsLoaderTests.cs` (12): carga dos campos, padrões sem os campos e erros de validação.
- `./scripts/check.sh --fast` passou no Windows: formatação, build sem avisos e os testes sem `Game.Design.Tests` (Core 522, Data 401, os outros sem mudança; os ignorados são os de banco, como antes). O `check.sh` completo (com `Game.Design.Tests`) fica para a integração, combinado com o integrador. Como nenhum dado real usa os campos novos e o RNG é consumido igual sem eles, os testes de design não devem mudar.

### Decisões

- **Sem protocolo novo.** `ProtocolVersion` continua **19**. O alcance a mais é resolvido no Core (servidor). O cliente já recebe os status ativos no snapshot (`StatusEffects`) e tem os dados, então pode somar o alcance com `RangeBonus.Of` sem nada novo na rede.
- O bônus de crítico da Mira Firme soma **antes** do teto de 40%, como o `CritBonus` dos afixos; só o crítico garantido passa do teto. No perfil do nível 21 (AGI 56) a chance é 19%, longe do teto.
- A distância da Mira Firme é medida como o alcance (até a borda do alvo), para "alcance 10" e "a 6 tiles" usarem a mesma régua. Um alvo grande conta como mais perto.
- Crítico garantido não sorteia (não consome o RNG); os outros casos consomem um número, como antes.
- `crit` vale só em dano com habilidade de origem. Áreas no chão (`spawn_area`) e dano periódico ficam com a regra normal, como pede o requisito 5 para venenos e sangramento. Cura e escudo não mudam.
- O primeiro alvo da Flecha Perfurante é o mais perto de quem atira no momento do acerto, entre todos os alvos resolvidos.
- `ignoreArmor` vale também para resistência quando a escola não é física. O nome segue o requisito (o Tiro Certeiro é físico).
- O alcance a mais usa a tag fixa `ranged` (`RangeBonus.Tag`), que os dados já usam, em vez de uma tag por status.

### Desvios

- Feita no Windows, e não na VPS, a pedido do designer.
- Para facilitar a integração com a T-422 e a T-442, a lógica nova está em arquivos novos (`Combat/MarksmanMechanics.cs`, `Definitions/MarksmanDefinitions.cs`, `GameDataValidator.Marksman.cs`), e os campos novos de `DamageEffect` e `StatusDefinition` estão em blocos marcados `T-431` no meio dos registros (não no fim). `CoreEffects.OnDamage` mudou em três linhas (crítico, fração de quem está atrás e `ignoreArmor`). `DamageResolver.Deal` ganhou o parâmetro opcional `ignoreArmor` no fim.
- `StatMath.CritChance` e `StatMath.DefenseReduction` ganharam um parâmetro opcional (padrão sem efeito); `TargetResolver` e `AbilitySystem` mudaram nas linhas do alcance.

### Pendências

- As habilidades de verdade (Flecha Perfurante, Tiro Debilitante, Mira Firme, Tiro Certeiro, Olho de Falcão) e os textos do site são da T-433.
- O cliente ainda não soma o alcance a mais: `PlayerInput.InReach` não manda o ataque básico a um alvo além do alcance base, `ClampAim` limita o círculo no ponto ao alcance base, e a mira desenha o alcance base. Basta usar `RangeBonus.Of(content, self.StatusEffects, tick, def)` nesses lugares (tarefa de cliente do Arqueiro, T-434 ou a de playtest). Os bots (`Game.Bots/AbilityInfo`) também usam o alcance base.
- O simulador deve dizer se o Tiro Certeiro sem conjuração e o Olho de Falcão de 15 a 20 s passam das metas (design, seção 6).
