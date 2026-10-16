# Relatório de balanceamento v0.2 (T-018)

Simulador `tests/Game.Sim`, conteúdo real de `data/`, 100 seeds por linha (seed inicial 1, desvio de posição 0,5). Testes em `tests/Game.Design.Tests` (21 testes, cerca de 2 min 45 s na VPS de 2 vCPU, Debug; `DESIGN_RUNS` muda o N). Base de comparação: `balance-v0.1.md`.

## 1. O que mudou nos bots

- **Tempo de reação** (`Reactor`): opção `reaction` (global ou `class.x:reaction`), faixa em segundos `0.2-0.4` (padrão), valor fixo (`0.3`) ou `0` (desliga). O atraso é sorteado por um `SimRandom` próprio de cada bot, com seed = seed do encontro (`BotDirector.Seed`, `BotContext.Seed`) combinada com o id da entidade; não toca o RNG da simulação. Mesma seed, mesmo resultado. Vale para: escolher o primeiro alvo e trocar de alvo depois de uma morte (o bot fica parado, salvo se estiver fugindo); curar, provocar, purificar e beber poção (a condição precisa durar o atraso); entrar em um perigo anunciado (`+dodge` só "enxerga" o telegráfo depois do atraso).
- **Provocação**: o bot usa quando um aliado perdeu vida nos últimos 2 s ou, estando sozinho, quando há 2 ou mais inimigos.
- **Purificação** (novo papel `AbilityRole.Cleanse`, deduzido de `remove_status`): só quando um aliado (ou o próprio) tem status com uma das tags removíveis (`poison`, `curse`); o mais ferido primeiro.
- **Poção**: consumíveis de cura própria de `AbilityState.Consumables` quando a vida cai a `potionBelow` (padrão 0,4; `0` desliga).
- APIs só acrescentadas: `Reactor`, `BotContext.Seed` (parâmetro opcional), `BotDirector.Seed`, `AbilityRole.Cleanse` (no fim do enum), `AbilityInfo.CleanseTags`. O simulador passa a seed do encontro ao diretor.

## 2. Números alterados em `data/`

| Id | Antes | Depois | Motivo |
|---|---|---|---|
| `matriarch.bite`, `matriarch.rending_bite` base | 9 | 60 | com poções e reação o grupo completo vencia 100%; reverte (e passa) o valor original da T-014 (18) |
| `matriarch.charge` base | 19 | 130 | idem (original 38) |
| `spider_small.bite` base | 2 | 9 | idem (original 4) |
| `spider_big.bite` base | 5,5 | 26 | idem (original 11) |
| `status.poison` dano por tick | 1 | 7 | idem (original 3) |
| `area.venom_pool` dano por tick | 1 | 8 | idem (original 4) |
| `monster.forest_matriarch` vida | 4000 | 6000 | alonga a luta e custa recursos do grupo (original 4500) |
| `monster.field_rat` vida | 62 | 105 | Explosão sozinha não pode resolver o encontro |
| `field_rat.bite` base | 4 | 3 | compensa a vida maior para a `rotation` do Mago |
| `mage.elemental_blast` custo | 45 | 65 | `explosion_spam` fica sem Mana e perde |
| `road_bandit.shot` base | 9 | 11 | com poções o Arqueiro "que persegue" vencia 100% |
| `monster.skeleton_guard` vida | 300 | 400 | mantém a razão do Clérigo em 1,2 a 1,6 (os outros encontros ficaram mais longos) |

A Matriarca precisou de mais dano do que o original porque o grupo `kite+dodge` quase não toma golpes diretos: o dano vem do veneno e de erros. Com a Provocação, a Purificação e as poções, o veneno e a poça passaram a pesar bem mais que antes.

## 3. Antes / depois (100 seeds)

"Antes" = estado da T-017 (relatório v0.1 + T-017) sem reação e sem poções. "Depois" = atual.

| Encontro / estratégia | Antes: vitória, tempo | Depois: vitória, tempo |
|---|---|---|
| 3 Lobos / `rotation` | 100%, 20,2 s | 100%, 20,4 s |
| 3 Lobos / `taunt_and_basic` (agora `warrior.taunt`) | 0% (com Golpe Circular) | 0% |
| Lobo+Salteador / `kite` | 100%, 25,5 s | 100%, 25,6 s |
| Lobo+Salteador / `rotation` | 0% | **6%**, 24,5 s |
| Lobo+Salteador / `idle` | 0% | 0% |
| 4 Ratos / `rotation` | 99%, 17,6 s | 97%, 36,2 s |
| 4 Ratos / `explosion_spam` | 98%, 9,8 s | **30%**, 28,2 s |
| Guarda / `rotation` | 100%, 26,3 s | 100%, 37,3 s (1,36x a média das outras) |
| Guarda / `heal_only` | 0% | 0% |
| Matriarca, grupo de 4 / `kite+dodge` | 100%, 140 s | **77%**, 310 s |
| Matriarca, grupo de 4 / `rotation` | 4% | 0% |
| Matriarca, grupo de 4 / `idle` | 0% | 0% |
| Matriarca sem Clérigo / `kite+dodge` | 72% (limite 70%, após a vida 4000) | **13%**, 349 s |

Efeito isolado de reação e poção na Matriarca (grupo completo, `kite+dodge`): sem os dois (`reaction=0`, `potionBelow=0`) 9% de vitória e 27 execuções com tempo esgotado; com os dois, 77%. As poções são, portanto, uma parte grande da sustentação que o v0.1 reclamava faltar.

Resultados entre 5% e 95% em estratégia relevante: Lobo+Salteador `rotation` (6%), Ratos `explosion_spam` (30%) e `rotation` (97% não conta), Matriarca `kite+dodge` (77%) e sem Clérigo (13%): 4 encontros.

## 4. Testes de design (21)

Alterados: `taunt_and_basic` sem a adaptação da T-016; Matriarca em `kite+dodge` entre 60% e 90% (era >= 70%); grupo sem Clérigo entre 5% e 60% **e** pelo menos 30 pontos abaixo do grupo completo (era 30% a 70%; ver limites abaixo); resultados em cache por (encontro, estratégia, papéis) para não repetir lotes. Novos: Explosão sozinha (`explosion_spam` vence <= 50% **ou** mais lento que a `rotation`), Mago `rotation` >= 20 s, pelo menos 3 encontros não binários, Purificação (com veneno e sem nada a remover), poção (limiar, desligar, mover o limiar) e Provocação (aliado ferido x ninguém ferido, com tempo de reação). Em `tests/Game.Sim.Tests`: faixa/parse da opção `reaction`, atraso do primeiro movimento entre 4 e 8 ticks, determinismo por seed, diferença entre ligado e desligado.

Limite do grupo sem Clérigo: o intervalo 30% a 70% da T-016 não é alcançável junto com 60% a 90% do grupo completo; a diferença Clérigo x sem Clérigo, com bots que quase não tomam dano direto, é de cerca de 60 pontos e o resultado é muito sensível (variar a vida da Matriarca em 200 já move o grupo completo de 77% para 92%). O teste passou a exigir "difícil, mas não impossível" e "claramente pior que o grupo completo".

## 5. Observações

- O resultado do Core é caótico perto do limite: ajustes de 5% no dano da Matriarca movem a vitória em até 15 pontos; margens são pequenas (77% em 60-90, 13% em 5-60).
- Mago `rotation` ficou longo (36 s) porque a Explosão custa 65 de 100 de Mana; só a metade da luta é Explosão. Se quiser uma luta de Mago mais curta, reduzir a vida do Rato e o custo juntos mantém o `explosion_spam` perdendo.
- O `kite` do Arqueiro segue 100% contra Lobo+Salteador: o recuo evita quase todo dano; só a `rotation` (persegue) fica em faixa intermediária.
- Pendências: Altar do Guarda Esquelético; desvio que veta movimento para dentro de áreas persistentes (v0.1 §4.2); os fatores da Matriarca estão agora acima do original da T-014, o que vale rever quando os bots (ou jogadores reais) tomarem mais dano direto.
- Tempo dos testes de design: 2 min 44 s (limite 5 min). `./scripts/check.sh` completo: 3 min 28 s.
