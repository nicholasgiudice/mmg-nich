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

Concluída em 2026-11-06. Relatório: `tasks/reports/balance-v0.9.md`.

**Feito**

- `data/rules.json`: `xpCurveBase` 50 → **68** (+36%); expoente 1,8 mantido (a forma da curva não precisava mudar). +30% exatos (base 65) dariam o nível 4 em 34,3 min, abaixo da meta; 68 é a menor base que cumpre as duas metas com folga.
- Ritmo solo nos Campos (modelo da T-215): nível 4 em 26 → **36 min**, nível 5 em 43 → **59 min**, nível 6 em 75 → 102 min.
- `data/items.json`: queda do monstro normal 8% → **4%**, elite 40% → **25%**; chefe sem mudança (100%, 2 itens).
- `FieldsDesignTests`: o modelo de ritmo (`FieldGroups.Pace`) passou a calcular também itens por minuto (monstros mortos × chance da categoria). Teste de ritmo trocado para a faixa nova (`Solo_player_reaches_level_4_in_35_to_40_minutes_and_level_5_in_55_to_65`) e teste novo de queda (`Solo_hunting_in_the_fields_drops_an_item_every_12_to_18_minutes`).
- `ProgressionContentTests`: chances novas.
- `design/progression-v0.1.md` §2 (fórmula, tabela de XP por nível, total de ~775 monstros / 8–9 h) e §6 (tabela de queda). A coluna "monstros normais do próprio nível" foi recalculada como XP ÷ (6 × nível); a antiga tinha valores inconsistentes (ex.: ~34 no nível 4).

**Desvio**

- A meta "um item a cada 6–10 min" não é compatível com 3–4% de chance no modelo: ~100 monstros/h dão 1 item a cada 7,4 min com 8% (antes) e a cada **14,9 min com 4%** (depois). Segui a faixa de chance pedida (decisão explícita do designer: "cai item demais") no topo dela, e o teste de queda guarda 12–18 min. Se o playtest achar raro demais, subir para 5–6%. Detalhes em balance-v0.9 §3. Os 4% foram confirmados pelo humano em 2026-11-06.

**Conferências**

- Os outros testes de design não mudaram: usam o equipamento comum do nível da área e não dependem da curva de XP nem da queda.
- **Fora das áreas listadas (mínimo):** alguns testes leem `data/` e fixavam a curva antiga ou a chance de 8%. Atualizados só os números: `tests/Game.Core.Tests/ProgressionTests.cs` (tabela de XP, XP concedida para subir 1 e 2 níveis, XP para o próximo no nível 7, build inválida com XP = XP do nível 1), `tests/Game.Core.Tests/ItemTests.cs` (`Normal_monsters_drop_about_four_percent_of_the_time`: 140–260 de 5000) e `tests/Game.Data.Tests/ItemLoaderTests.cs` (chances). Nenhum código de `src/` mudou; o padrão de `ProgressionRules` no código continua 50 (só vale quando os dados não definem a base).

**Observação de ambiente**

- Outro agente (T-301) trabalhava ao mesmo tempo no checkout `/root/mmo-rpg`. Esta tarefa foi feita num worktree separado (`/root/mmo-rpg-T302`) para não misturar mudanças; pode ser removido com `git worktree remove /root/mmo-rpg-T302` depois da integração.
