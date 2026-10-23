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

_(preencher ao concluir)_
