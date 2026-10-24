# T-206 — Simulador: perfis por nível e equipamento, testes de design da progressão

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** T-202, T-203, T-205
- **Libera:** T-230
- **Áreas que pode alterar:** `tests/Game.Sim/`, `tests/Game.Design.Tests/`, `src/Game.Bots/` (gastar pontos/equipar nos perfis), `data/` (só números de balanceamento), `tasks/reports/`

## Contexto

`design/progression-v0.1.md` §8: perfis "esperado" (nível da área, atributo sugerido, equipamento comum do nível) e "fraco" (3 níveis abaixo), com metas por situação. A T-063 deixou ignorado o teste do Mago com NH alto "até existirem encontros por nível" (`balance-v0.6.md` §4).

## Requisitos

1. Simulador aceita perfil de personagem: nível, distribuição de atributos (sugerido/espalhado), equipamento (comum/incomum/raro do nível) e NH (perfis da T-063).
2. Encontros por nível: versões dos encontros do protótipo com monstros no nível da área (ou parâmetro de nível do encontro).
3. Testes de design da tabela da §8 (perfil esperado ≥ 85%, fraco ≤ 50%, sugerido × espalhado, distribuição de NH, diferença entre classes ≤ 1,6×). **Reativar** `RankDesignTests.Mage_rank_5_area_skill_does_not_solve_the_rats_alone` com o encontro no nível 9.
4. Ajustar números em `data/` se preciso e escrever `tasks/reports/balance-v0.7.md`.

## Critérios de aceite

- testes de design da §8 passando (ou meta revista com justificativa no relatório e aprovação do designer);
- relatório criado;
- `./scripts/check.sh` completo passa.

## Resultado

**Feito** (VPS, 2026-10-24). `./scripts/check.sh` completo passa: 62 testes de design, nenhum ignorado, em cerca de 4 min 15 s. Relatório: `tasks/reports/balance-v0.7.md`.

**Monstros por nível** (decisão do designer: regra no Core)
- O Core só usava o nível do monstro na XP e na defesa. Agora `rules.monsters` faz vida e potência crescerem por nível: `× (1 + porNível × (L − 1))`. A potência vale para dano, cura e escudo dos efeitos do monstro, inclusive áreas e status dele. Os números de `monsters.json` passam a ser o nível 1. Chefes têm crescimento próprio, e elites podem ter.
- Valores: normal e elite com vida 0,40 e potência 0,17; chefe com vida 0,30 e potência 0,38.
- O nível vem do surgimento (`enemies[].level`), do encontro (`monsterLevel`) ou da definição. Invocações herdam o nível de quem invoca.
- Os 5 encontros do protótipo ganharam `"monsterLevel": 1` e não mudaram. Os testes de design antigos passam sem alteração.

**Simulador**
- `CharacterProfile` define nível, atributos (sugerido ou espalhado), equipamento completo do nível (comum, incomum ou raro), NH (focado, espalhado ou explícitos) e par focado por classe.
- `RunSpec.Profile` e `RunSpec.MonsterLevel`, mais as opções `--level`, `--attrs`, `--gear`, `--gear-level`, `--skills`, `--focus` e `--monster-level`.
- Com perfil, os jogadores nascem com progressão (`Leveling.SpawnCharacter`). A distribuição de NH respeita os pontos e requisitos do nível, e no nível 15 reproduz os perfis da T-063.

**Testes de design** (`ProgressionDesignTests`, solos nos níveis 5 e 9 e Matriarca nível 8)
- Todas as metas da §8 passam:
  - esperado ≥ 85% em até 40 s;
  - fraco ≤ 50%;
  - sugerido mais rápido e espalhado ≥ 60%;
  - NH focado e espalhado ≥ 70%;
  - diferença entre classes;
  - Matriarca 60–90% em 2–4 min, com o grupo nível 7 e com o 8.
- `RankDesignTests.Mage_rank_5_area_skill_does_not_solve_the_rats_alone` foi reativado com o Mago nível 9 contra ratos nível 9.

**Metas revistas com aprovação do designer** (relatório §4)
- "Espalhado" passou a ser metade dos pontos no atributo sugerido e metade dividida entre os outros. Com os pontos divididos por igual entre os 5 atributos, o Mago vencia 7%.
- No NH alto, o spam pode vencer, desde que a rotação não seja mais que 1,5× mais lenta (opção 3 da v0.6). A Explosão NH5 fica em 1,44×.
- O Clérigo pode ser até 1,7× mais lento que a média (as outras classes, 1,6×). No nível 9 ele fica em 1,61×.

**Desvios registrados**
- O Clérigo fraco ainda vence o Guarda, porque se cura contra um único monstro lento. O teste exige que ele demore pelo menos 1,3× o esperado (hoje 1,6×).
- O encontro do Arqueiro dura 10–13 s, abaixo dos 15 s da meta. São só 2 monstros, e no protótipo já eram 7,8 s. O teste confere só o teto de 40 s.
- A §8 diz que os testes atuais passariam a rodar com o "perfil esperado nível 1". Mantive o caminho do protótipo, todas as normais no NH1. Com progressão, o nível 1 só tem 1 ponto de habilidade, o que mudaria a calibração da fase 0.

**Fora das áreas da tarefa** (exigido pela decisão de escalar no Core)
- Core: `Rules.cs`, `Definitions.cs`, `EntityFactory.cs`, `Summons.cs`, `CharacterStats.cs` (`MonsterPower`) e `CoreEffects.cs`.
- `Game.Data`: validação, e `data/schemas`.
- Dados: `data/encounters.json` (`monsterLevel`).
- Testes: `tests/Game.Core.Tests/MonsterScalingTests.cs` e `tests/Game.Data.Tests/MonsterScalingLoaderTests.cs`.
- Nenhum bot precisou mudar: os perfis criam o personagem já pronto.

**Pendências**
- Vigia Sem Rosto (fase 3). Nível 12+ precisa de nova medição: a assinatura muda a curva.
- O servidor (T-214/T-215) deve criar os monstros no nível da área. Hoje usa os encontros do protótipo, nível 1.
- Duração do encontro do Arqueiro quando os Campos tiverem surgimentos reais (T-215).
