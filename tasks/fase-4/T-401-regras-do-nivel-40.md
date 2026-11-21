# T-401 — Regras do nível 40 e da segunda classe nos dados

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** —
- **Libera:** T-402, T-403
- **Áreas que pode alterar:** `data/rules.json`, `data/schemas/`, `src/Game.Core/Definitions/` (regras novas), `src/Game.Data/` (validação), `src/Game.Site/` (página de classes, só o que as regras novas mudarem), `design/progression-v0.1.md`, `design/classes-v0.1.md`, `design/skills-v0.1.md`, `tests/`

## Contexto

Design em `design/segunda-classe-v0.1.md` (seções 1 e 2). O nível máximo sobe de 15 para **40**, e no nível **20** o personagem escolhe uma **segunda classe**, com 4 habilidades normais e 1 assinatura. As regras de progressão estão em `data/rules.json`, seção `progression`, lidas por `ProgressionRules` (`src/Game.Core/Definitions/Progression.cs`).

Esta tarefa só põe as **regras novas nos dados**, com validação, e atualiza os documentos de design. Quem usa as regras da segunda classe é a T-403.

## Requisitos

1. `progression.maxLevel` = **40**. A curva de XP não muda (`68 × L^1,8`).
2. Regras novas em `progression` (nomes sugeridos):
   - `secondClassLevel` = **20**: nível em que a segunda classe pode ser escolhida;
   - `secondClassAttributePoints` = **5**: pontos de atributo extras ao escolher a segunda classe;
   - `secondClassRankLevels` = **[21, 24, 27, 30, 33]**: nível exigido por NH nas habilidades normais da segunda classe;
   - `secondClassSignatureRankLevels` = **[35, 37, 40]**: o mesmo, na assinatura da segunda classe.
   As listas da classe base (`skillRankLevels` [1, 3, 5, 7, 9] e `signatureRankLevels` [12, 13, 15]) não mudam.
3. `ProgressionRules`: propriedades novas e um método que devolve o nível exigido para um NH sabendo se a habilidade é da classe base ou da segunda classe (o atual `RequiredLevel(signature, rank)` continua valendo para a base). `AttributePointsAt(level)` ganha uma versão que soma os 5 pontos quando a segunda classe foi escolhida.
4. Validação em `GameDataValidator`: listas crescentes, dentro de 1..`maxLevel`; `secondClassLevel` < primeiro nível de `secondClassRankLevels`; `secondClassAttributePoints` ≥ 0. Schema atualizado.
5. Documentos de design: `progression-v0.1.md` (§2 nível máximo e XP até o 40, §3 pontos de atributo, §7 pontos de habilidade e NH), `classes-v0.1.md` (segunda classe, com link para o design novo) e `skills-v0.1.md` (regra de nível por NH da segunda classe). Registrar que o detalhe das segundas classes está em `design/segunda-classe-v0.1.md`.
6. A página de classes do site (`src/Game.Site/Pages.Classes.cs`) mostra as regras novas, se já mostra as atuais.

## Fora do escopo

Usar as regras no Core (T-403), simulador e metas (T-402), qualquer habilidade nova.

## Critérios de aceite

- `data/rules.json` carrega e valida com as regras novas; dados inválidos de cada regra nova geram erro com teste;
- `ProgressionRules.XpToNext(39)` > 0 e `XpToNext(40)` = 0;
- testes atuais passam. Testes de design que quebrarem só porque o nível máximo mudou são ajustados, com o motivo no Resultado;
- documentos de design atualizados.

## Resultado

_(preencher ao concluir)_
