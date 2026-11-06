# T-302 — Dados: ritmo de XP e taxa de queda de itens

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** —
- **Libera:** T-322
- **Áreas que pode alterar:** `data/rules.json`, `data/items.json` (só as tabelas de queda e `rules.items`), `tests/Game.Design.Tests/`, `tests/Game.Data.Tests/`, `tasks/reports/`, `design/progression-v0.1.md` (§2 e §6, só os números)

## Contexto

O playtest da fase 2 (`tasks/reports/playtest-v0.2.md`, "Ajustes de balanceamento") achou duas coisas:

- **A progressão está rápida.** Hoje, o modelo de ritmo da T-215 (`FieldsDesignTests`) dá nível 4 em 26 min, 5 em 43 e 6 em 75. O designer pediu para **aumentar um pouco o XP necessário por nível**, sem mudar o nível máximo (15).
- **Cai item demais.** As tabelas de `data/items.json` dão chance de 8% no monstro normal, 40% no elite e 100% no chefe (2 itens).

A curva de XP é `round(xpCurveBase × L^xpCurveExponent)` (`data/rules.json`: base 50, expoente 1,8; `design/progression-v0.1.md` §2).

## Requisitos

1. **XP:** aumentar o XP por nível em cerca de 30% (meta de ritmo solo nos Campos: nível 4 em 35–40 min e nível 5 em 55–65 min, pelo modelo da T-215). Preferir mudar só `xpCurveBase`. Mudar o expoente só se a forma da curva também precisar mudar, e explicar o motivo no relatório.
2. Atualizar a meta do teste de ritmo (`FieldsDesignTests`) para a nova faixa e registrar a tabela nova de XP por nível em `design/progression-v0.1.md` §2.
3. **Queda:** reduzir a chance no monstro normal para **3–4%** e no elite para **20–25%**. O chefe continua com queda garantida. Medir com o simulador quantos itens caem por hora no modelo de ritmo, antes e depois, e escolher números que deem algo como **um item a cada 6–10 minutos** de caça solo nos Campos.
4. Conferir se o perfil "esperado" de cada área continua com equipamento compatível. Os testes de design usam o equipamento do nível da área, não o que caiu, então eles não devem mudar. Se algum mudar, explicar.
5. Relatório curto em `tasks/reports/balance-v0.9.md`: números antigos e novos, tempo até cada nível, itens por hora.

## Critérios de aceite

- testes de design de ritmo atualizados e passando;
- `tests/Game.Data.Tests/ProgressionContentTests.cs` atualizado com as chances novas;
- `balance-v0.9.md` com a tabela antes e depois;
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
