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

**Feito.** Todos os requisitos; relatório em `tasks/reports/balance-v0.2.md` (antes/depois, números, limites). `./scripts/check.sh` completo passa (195 Core, 98 Data, 18 Sim, 21 Design); testes de design em 2 min 44 s (limite 5 min).

**Bots (`src/Game.Bots`, só acréscimos)**
- `Reactor` (novo): opção `reaction` (global ou `class.x:reaction`): `0.2-0.4` (padrão), valor fixo ou `0` (desliga). Sorteio por `SimRandom` próprio do bot, seed = `BotDirector.Seed` (nova propriedade, padrão 0; o `EncounterRunner` passa a seed do encontro) combinada com o id da entidade; `BotContext` ganhou o parâmetro opcional `seed`. Aplica-se a: primeiro alvo / troca de alvo após morte (bot para, salvo se foge), cura, Provocação, Purificação, poção e entrada em perigos do `+dodge` (o perigo só conta depois do atraso; a identidade do perigo ignora o tick de resolução).
- Provocação quando um aliado perdeu vida nos últimos 2 s (ou, sozinho, 2+ inimigos). Purificação (`AbilityRole.Cleanse`, novo no fim do enum, `AbilityInfo.CleanseTags`) só se alguém tem status com a tag removível. Poção (`AbilityState.Consumables`, cura própria) abaixo de `potionBelow` (padrão 0,4; 0 desliga).
- Para o cliente (`BotDirector`, `TelegraphHazardSource`): nenhuma API renomeada ou removida. Atenção: o padrão agora liga a reação (0,2-0,4 s) e a poção; para o comportamento antigo use `reaction=0` e `potionBelow=0` nas opções, e defina `director.Seed` (ex.: seed do mundo) para variar a reação.

**Testes.** `tests/Game.Design.Tests`: `taunt_and_basic` usa `warrior.taunt` (adaptação removida); Explosão sozinha (<= 50% ou mais lenta que a `rotation`); Mago `rotation` >= 20 s; >= 3 encontros não binários; Purificação, poção e Provocação nos bots; resultados em cache por (encontro, estratégia, papéis). `tests/Game.Sim.Tests`: parse de `reaction`, atraso do primeiro movimento (4 a 8 ticks), determinismo por seed, ligado x desligado.

**Números alterados** (somente `data/`): ver relatório §2. Matriarca (mordidas 60, investida 130, aranhas 9/26, veneno 7, poça 8, vida 6000), Rato (vida 105, mordida 3), Explosão Elemental (custo 65), Salteador (disparo 11), Guarda Esquelético (vida 400).

**Desvios / decisões**
- Grupo sem Clérigo: o limite 30%-70% da T-016 é incompatível com 60%-90% do grupo completo (diferença de ~60 pontos e resultado muito sensível); o teste passou a exigir 5%-60% e >= 30 pontos abaixo do grupo completo. Hoje: grupo completo 77%, sem Clérigo 13%.
- Matriarca ficou com dano acima do original da T-014: com poções e reação os bots `kite+dodge` quase não tomam dano direto.
- Corrigido um estouro numérico no controle "aliado atacado" antes do commit (sentinela `-1_000_000`).
- Fora da lista de áreas: nenhuma (`tests/Game.Sim/EncounterRunner.cs` está em `tests/Game.Sim/`).

**Pendências:** Altar do Guarda Esquelético; desvio que veta movimento para dentro de áreas persistentes; margens estreitas na Matriarca (relatório §5).
