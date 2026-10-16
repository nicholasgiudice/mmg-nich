# T-018 — Bots com tempo de reação e rebalanceamento

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** T-017
- **Libera:** T-025
- **Áreas que pode alterar:** `src/Game.Bots/`, `tests/Game.Sim/`, `tests/Game.Sim.Tests/`, `tests/Game.Design.Tests/`, `data/` (números), `tasks/reports/`

## Contexto

`tasks/reports/balance-v0.1.md` mostra: resultados quase binários (bots reagem no mesmo tick), Matriarca enfraquecida demais por falta de sustentação, e Mago forte demais (`explosion_spam` vence mais rápido que `rotation`). A T-017 adicionou Provocação, Purificação, cura própria na Corrente Sagrada e poção de cura.

## Requisitos

1. **Tempo de reação dos bots**: opção `reaction` (segundos, faixa mín–máx, padrão ~0,2–0,4 s) aplicada a reagir a perigos e a mudanças de alvo/estado, com aleatoriedade **determinística** (seed derivada da seed do encontro, sem `Random` não seedado). Opção para desligar (`reaction=0`).
2. **Bots usam o que a T-017 adicionou**: Provocação quando um aliado está sob ataque, Purificação em aliado com veneno/maldição, poção abaixo de um limiar de vida (configurável, `potionBelow`).
3. **Teste de design do Mago** conforme o design ("se Explosão Elemental resolver o encontro sozinha sem exigir movimento ou gestão de recurso, o Mago está forte demais"): `explosion_spam` vence ≤ 50% **ou** leva mais tempo que `rotation`.
4. **Teste da Provocação**: `taunt_and_basic` passa a usar `warrior.taunt` (remover a adaptação da T-016).
5. **Rebalancear** `data/` (somente números) com bots com reação e poções: reverter parte do enfraquecimento da Matriarca feito na T-016 (ela deve ser desafiadora para o grupo completo: vitória entre 60% e 90% com `kite+dodge`), mantendo todos os testes de design passando. Mago `rotation` com duração média ≥ 20 s.
6. Relatório `tasks/reports/balance-v0.2.md` com antes/depois e o que mudou.

## Critérios de aceite

- `./scripts/check.sh` completo passa, tempo dos testes de design < 5 min;
- resultados não binários em pelo menos 3 encontros (taxas entre 5% e 95% em alguma estratégia relevante);
- relatório criado.

## Resultado

_(preencher ao concluir)_
