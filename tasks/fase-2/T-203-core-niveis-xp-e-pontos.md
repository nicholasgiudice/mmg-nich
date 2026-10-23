# T-203 — Core: níveis, XP, pontos de atributo e de habilidade

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-201
- **Libera:** T-205, T-206, T-212, T-221
- **Áreas que pode alterar:** `src/Game.Core/`, `src/Game.Data/`, `src/Game.Protocol/`, `data/rules.json`, `data/monsters.json` (nível e categoria), `data/schemas/`, testes correspondentes

## Contexto

`design/progression-v0.1.md` §2, §3 e §7: nível máximo 15, XP por monstro com modificador de diferença de nível e divisão em grupo, 5 pontos de atributo livres por nível, 1 ponto de habilidade por nível, requisito de nível por NH (`2n − 1`; assinatura 12/13/15). Os NH já existem no Core (T-060); falta ligá-los a pontos ganhos.

## Requisitos

1. **Nível e XP** no personagem (componente replicável, com XP para o próximo nível): curva `round(50 × L^1,8)`; nível máximo 15. Monstros ganham `level` e `category` (normal/elite/chefe) nos dados.
2. **XP ao matar**: `6 × nível do monstro × multiplicador da categoria` (1/5/30) × modificador de diferença (+10% por nível acima, máx. +30%; −20% por nível abaixo; 0 com 5 ou mais abaixo). Dividida entre quem participou (causou dano ou curou/protegeu quem lutou nos últimos N s) com +10% por membro além do primeiro. Eventos `XpGained` e `LevelUp`.
3. **Pontos**: ao subir de nível, +5 pontos de atributo livres e +1 ponto de habilidade. Intenções novas: `SpendAttributePoint(atributo)` e `RaiseSkillRank(habilidade)`, validadas no Core (pontos disponíveis, requisito de nível por NH, máximo da habilidade, assinatura a partir do 12). A assinatura entra na barra no NH1, como hoje.
4. **Derivados por nível**: vida máxima com o crescimento por nível da classe (§4); nível do atacante na fórmula de defesa (T-201).
5. **Redistribuição**: intenção que devolve todos os pontos (atributos e habilidades). Na fase 2 é **grátis** e só é permitida fora de combate (economia está fora de escopo; o custo em moedas da §10 fica para depois). Registrar no Resultado.
6. Protocolo: componentes e intenções novos → `ProtocolVersion` + 1.

## Critérios de aceite

- testes do Core: curva de XP, modificador de diferença, divisão em grupo, subir de nível, gastar pontos, requisitos de NH, redistribuição;
- um personagem nível 1 sem pontos gastos continua igual ao de hoje (testes de design passam);
- `./scripts/check.sh` completo passa.

## Resultado

**Feito**

- **Regras** em `data/rules.json`, seção `progression` (`ProgressionRules`, padrões iguais ao design): nível máximo 15, curva `round(50 × L^1,8)`, XP `6 × nível × categoria` (1/5/30), modificador de diferença (+10%/nível acima até +30%, −20%/nível abaixo, 0 com 5 ou mais abaixo), bônus de grupo +10% por membro extra, janela de participação 30 s, `combatTimeout` 5 s, 5 pontos de atributo e 1 de habilidade por nível, níveis por NH `[1, 3, 5, 7, 9]` e assinatura `[12, 13, 15]`. Validação em `Game.Data` e no JSON Schema.
- **Monstros:** `level` nos dados (padrão 1) e em `UnitInfo.Level`. A categoria é o `rank` que já existia (normal/elite/boss), sem campo novo. Níveis do design: Rato 1, Lobo 2, Salteador 3, Aranha pequena 5, Aranha grande (elite) 6, Matriarca (chefe) 8, Guarda Esquelético 9. `StatMath.LevelOf` passa a usar o nível do monstro na fórmula de defesa.
- **`src/Game.Core/Combat/Progression/`:**
  - `CharacterProgress` (replicável, tag 15): XP no nível, XP para o próximo, pontos livres de atributo e de habilidade e pontos distribuídos por atributo. O nível continua em `CharacterStats.Level`; os NH em `AbilityState.Ranks`.
  - `CombatActivity` e `XpContributors` (internos): último tick em combate, quem cada personagem curou/protegeu e quem bateu em cada monstro.
  - `ProgressionSystem` (`SystemOrder.Progression` = 9500, depois de `Death`): registra dano/cura/escudo do tick, dá XP pelas mortes e trata as intenções.
  - `Leveling`: `SpawnCharacter` (a partir de um `CharacterBuild` salvo), `BuildOf` (o que salvar), `Validate`, `GrantExperience`, `SpendAttributePoint`, `RaiseSkillRank`, `ResetPoints`, `InCombat`, `RefreshDerived`.
- **XP ao matar:** participa quem causou dano ao monstro na janela e quem curou/protegeu um desses na janela. Cada um recebe `round(XP × modificador do próprio nível × (1 + 0,1 × (n − 1)) / n)`. Sobe quantos níveis couber; no 15 a XP para de acumular.
- **Subir de nível:** +5 pontos de atributo, +1 de habilidade, vida máxima com o crescimento da classe (o atual sobe junto). Eventos `XpGained` e `LevelUp`.
- **Intenções** `SpendAttributePointIntent`, `RaiseSkillRankIntent` e `ResetPointsIntent`, validadas no Core. Rejeição publica `ProgressionRejected` com o motivo (`NoProgression`, `NoAttributePoints`, `NoSkillPoints`, `NotLearnable`, `MaxRank`, `LevelTooLow`, `InCombat`). Eventos de sucesso: `AttributeRaised`, `SkillRankRaised`, `PointsReset`. VIT e INT atualizam na hora vida e Mana máximas.
- **NH:** NH0 não está na barra; NH1 põe a habilidade na barra, na ordem da classe (a assinatura também). Ataque básico e consumíveis não recebem pontos.
- **Log de combate:** formatadores dos eventos novos, na categoria nova `LogCategory.Progressao`.
- **Protocolo v4:** componente `CharacterProgress`, `Level` no `UnitInfo`, eventos 90–95 e `NetIntentKind` `SpendAttributePoint`, `RaiseSkillRank` e `ResetPoints` (campo novo `Attribute` no `NetIntent`).
- **Testes:** `tests/Game.Core.Tests/ProgressionTests.cs` (curva, modificador, divisão, XP ao matar e janela, curandeiro participa, subir vários níveis e teto, pontos de atributo, NH e requisitos, assinatura, redistribuição dentro e fora de combate, nível 1 igual ao de hoje nas 4 classes, build ida e volta e builds inválidos), `tests/Game.Data.Tests/ProgressionLoaderTests.cs` e dois testes novos em `ProtocolTests`.

**Decisões**

- **Progressão é opcional por entidade.** Só quem é criado por `Leveling.SpawnCharacter` tem `CharacterProgress`. Os encontros, o simulador e o servidor atual continuam com `EntityFactory.SpawnPlayer` (todas as habilidades no NH1, sem XP), então os testes de design não mudaram. O critério "nível 1 sem pontos gastos igual ao de hoje" vale para os números (vida, Mana, atributos): um personagem novo pelo design só tem o ataque básico e 1 ponto de habilidade, então a barra é diferente da dos encontros de teste.
- **Redistribuição:** grátis e só fora de combate (sem causar ou receber dano há 5 s, nem curar/proteger quem está lutando, e sem conjurar). Devolve todos os pontos; a barra fica só com o ataque básico. O custo em moedas e a regra "só na cidade" (§10) ficam para quando houver economia e zonas.
- **Subir de nível não cura por completo:** vida e Mana máximas crescem e o atual sobe a mesma diferença.
- **Participação** inclui personagens mortos no momento da morte do monstro (lutaram; não perdem a XP).
- Gastar pontos de atributo e de habilidade é permitido em combate; só a redistribuição não.

**Pendências**

- Cliente: o painel de log não tem filtro para `Progressao` (as linhas usam a cor padrão) e não há telas de atributos e habilidades (T-221).
- Servidor: ainda cria jogadores sem progressão; a T-212 deve usar `Leveling.SpawnCharacter`/`BuildOf` com o banco.

