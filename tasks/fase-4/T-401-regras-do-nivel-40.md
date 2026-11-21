# T-401 — Regras do nível 40 e da segunda classe nos dados

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** —
- **Libera:** T-402, T-403
- **Áreas que pode alterar:** `data/rules.json`, `data/schemas/`, `src/Game.Core/Definitions/` (regras novas), `src/Game.Data/` (validação), `src/Game.Site/` (página de classes, só o que as regras novas mudarem), `design/progression-v0.1.md`, `design/classes-v0.1.md`, `design/skills-v0.1.md`, `tests/`
- **Branch:** sai de `trilha/segundas-classes` e é integrada nela, não na `main` (seção 6f de `tasks/README.md`).

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

Concluída em 2026-11-21, na VPS.

**Dados e schema**
- `data/rules.json`, `progression`: `maxLevel` 15 → **40**; regras novas `secondClassLevel` 20, `secondClassAttributePoints` 5, `secondClassRankLevels` [21, 24, 27, 30, 33] e `secondClassSignatureRankLevels` [35, 37, 40]. Curva de XP e listas da base sem mudança.
- `data/schemas/game-data.schema.json`: os quatro campos novos, com descrição.

**Core (`ProgressionRules`, `src/Game.Core/Definitions/Progression.cs`)**
- Propriedades `SecondClassLevel`, `SecondClassAttributePoints`, `SecondClassRankLevels`, `SecondClassSignatureRankLevels`, com os números do design como padrão. O padrão de `MaxLevel` também passou a 40.
- `RequiredLevel(signature, rank, secondClass)`: escolhe a lista certa; o `RequiredLevel(signature, rank)` de antes continua e chama a nova com `secondClass: false`.
- `AttributePointsAt(level, secondClassChosen)`: soma os 5 pontos quando a segunda classe foi escolhida; o de um parâmetro não muda.
- Nada no Core usa as regras novas ainda (T-403). `Leveling` já respeita `MaxLevel`, então personagens sobem até o 40 com a classe base.

**Validação (`GameDataValidator.ValidateProgression`)**
- `secondClassLevel` em 1..`maxLevel` e menor que `secondClassRankLevels[0]`; `secondClassAttributePoints` ≥ 0; as duas listas novas passam pela mesma checagem das listas da base (5 e 3 itens no mínimo, em 1..`maxLevel`, sem diminuir).
- Decisão: "crescentes" usa a regra que já valia para as listas da base (um NH não pode exigir nível menor que o anterior; nível igual é aceito), para as quatro listas seguirem a mesma regra.
- Consequência: um `maxLevel` menor que 40 sem trocar as listas da segunda classe agora é erro (as listas passam do teto). É de propósito: as regras da segunda classe precisam caber no jogo.

**Site:** a página de classes (`Pages.Classes.cs`, "Regras de todas as classes") mostra o nível da segunda classe, os pontos extras e os níveis por NH das normais e da assinatura da segunda classe.

**Documentos:** `design/progression-v0.1.md` (nota da fase 4 no topo; §2 nível máximo 40 e tabela de XP até o 39 → 40; §3 pontos de atributo 195 + 5 = 200; §7 40 pontos, 46 NH e tabela de NH da base e da segunda classe), `design/classes-v0.1.md` (seção nova "Segunda classe", com as opções de cada classe e link para `design/segunda-classe-v0.1.md`; regra de balanceamento com 40 pontos para 46 NH) e `design/skills-v0.1.md` (escada de nível por NH da segunda classe e onde ficam as tabelas dela).

**Testes**
- Novos: `ProgressionTests.Second_class_rules_follow_the_design` (listas, `RequiredLevel` com e sem segunda classe, NH fora da lista, 195/200 pontos de atributo, 40 de habilidade); `Xp_curve_follows_the_design_table` agora checa `XpToNext(39)` = 49 708 e `XpToNext(40)` = 0; `ProgressionLoaderTests` checa os padrões novos e tem um caso de erro para cada regra nova (nível 0, acima do máximo, não menor que o NH1, pontos negativos, lista curta, lista que diminui, nível fora de 1..40, `maxLevel` 30 com as listas padrão).
- Ajustados só porque o nível máximo mudou:
  - `ProgressionTests.Big_xp_climbs_several_levels_and_stops_at_the_cap`: o teto agora é 40 (39 subidas, 195 pontos de atributo, 40 de habilidade); 1 000 000 de XP ainda passa dos 717 404 até o 40.
  - `ProgressionTests.Invalid_builds_are_refused`: o caso "nível acima do máximo" passou de 16 para 41.
  - `ProgressionLoaderTests`: o teste de carga usava `maxLevel` 20, que agora fica abaixo das listas da segunda classe; passou para 45. O caso `signatureRankLevels` [12, 13, 16] (16 passava do teto 15) virou [12, 13, 41]. O teste dos dados reais espera `maxLevel` 40.
  - `Game.Sim.Tests.CharacterProfileTests.Every_level_gives_a_valid_build_that_spends_all_points`: ia até `MaxLevel`, e do 24 em diante a classe base (23 NH) não tem onde gastar todos os pontos. O laço agora para em `SecondClassLevel` (20); perfis com segunda classe são da T-402.
- `./scripts/check.sh` completo passando (inclui `Game.Design.Tests`).

**Desvios e observações**
- Áreas: tudo dentro das listadas (`tests/Game.Sim.Tests` está em `tests/`). Não mexi em efeitos, estados nem `DamageResolver`.
- `design/segunda-classe-v0.1.md` §3 diz "~255" monstros normais para ir do 39 ao 40; pela curva são ~212 (49 708 ÷ (6 × 39)). Os outros números dessa tabela batem. Não corrigi porque o arquivo não está nas áreas desta tarefa: fica para quem mexer nele.
- `CharacterProfiles.BuildFor` (simulador) aceita nível até `MaxLevel`, mas acima do 23 não gasta todos os pontos de habilidade. Nenhum perfil atual passa do 15; isso fica para a T-402.
