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

_(preencher ao concluir)_
