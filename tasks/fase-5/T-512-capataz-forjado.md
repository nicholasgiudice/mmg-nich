# T-512 — Core e dados: o Capataz Forjado e os monstros das Minas Fundas

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** 511
- **Libera:** T-513, T-533, T-540
- **Áreas que pode alterar:** as mesmas da T-510

## Contexto

As Minas Fundas (`design/mapas-16-40-v0.1.md` §3.4) são a masmorra instanciada do nível 24 ao 27, para 4 pessoas, e a **primeira com papel de grupo de verdade**: o chefe bate forte num só (pede Guardião) e solta dano em todos (pede Sacerdote). As salas usam os monstros da Serra (T-511) nos níveis 24–26.

Mesmo formato da T-351 (o Vigia Sem Rosto): fases, telegráfos e metas de grupo no simulador.

## Requisitos

1. **Design** do chefe (seção nova em `design/combat-encounters-v0.1.md`). Proposta (o designer pode trocar):
   - **Capataz Forjado** (`monster.forged_overseer`, 27, **chefe**, tag `construct`), golem de ferro, três fases:
     1. *Martelo*: golpe em cone curto no alvo da ameaça, muito forte, com telegráfo. Quem não é tanque cai em 2 golpes;
     2. *Fornalha*: pulsos de dano em toda a sala a cada poucos segundos, que pedem cura em grupo; continua o Martelo;
     3. *Chamar a Equipe*: invoca mineiros possuídos (`summon`) e acelera.
2. **Monstros das salas:** os da Serra nos níveis 24–26, mais um Golem de Pedra elite numa rota opcional. Se uma sala pedir um monstro novo, registre.
3. **Core:** só o que faltar, com teste.
4. **Testes de design** (perfis com segunda classe, nível 27):
   - grupo de 4 com Guardião e Sacerdote vence ≥ 75%;
   - grupo de 4 **sem** Guardião e **sem** Sacerdote vence ≤ 40% (o papel importa);
   - duração da luta com o chefe entre 3 e 6 min.
5. **Arte:** só o id. O Capataz não tem corpo no gerador LPC: a T-521 resolve (encomenda ou pacote).

## Critérios de aceite

- seção de design do chefe;
- testes de design passando, com relatório em `tasks/reports/`;
- `design/bestiario.md` atualizado;
- `./scripts/check.sh` completo passa.

## Resultado

Feito em 2026-11-24, no Windows (sem tela), na branch `task/T-512-capataz-forjado`. **A branch está empilhada sobre a da T-511** (`task/T-511-monstros-da-serra`, `52bb459`), que está sobre a T-510 (`5caadf9`) e a T-502 (`6ee2b2e`). **Integrar na `main` depois da T-502, da T-510 e da T-511.**

**Design.** Seção nova "Minas Fundas — O Capataz Forjado" em `design/combat-encounters-v0.1.md`: as salas (monstros da Serra no 24–26, o Golem de Pedra elite na rota opcional; nenhum monstro novo), o chefe em três fases, resposta de cada segunda classe, encontro de teste, falhas de design e uma linha na matriz. `design/bestiario.md` com o Capataz, o encontro e as Minas nos monstros da Serra. A proposta da tarefa vale como decidida (designer ausente); os acréscimos estão abaixo e no relatório.

**Dados** (nível 1; id `monster.forged_overseer` mantido; ids dos monstros da Serra reaproveitados):
- **Capataz Forjado** (nv27, **chefe**, `construct`, `ai.boss`): vida 70.000 (616.000 no 27), velocidade 3,0, raio 0,9. Punho de Ferro (`iron_fist`, físico 62, contínuo no tanque), **Escória Ardente** (`slag_throw`, fogo 25 em quem está a 3 tiles ou mais), **Martelo** (`hammer`, cone 100° × 3,5 com telegráfo de 1 s, físico 90: Arqueiro e Mago caem em 2), **Sopro da Fornalha** (`furnace_blast`, fogo 22 na sala toda a cada 5 s), **Chamar a Equipe** (`call_crew`, conjuração de 2 s, Mineiros Possuídos, no máximo 3) e Martelo Acelerado (`hammer_fast`).
- Fases: `phase.hammer` (100%), `phase.furnace` (60%: entra a **Fornalha Esquentando**, `status.overseer_heat`, 190 s; ao acabar, **Fornalha Transbordando**, `status.overseer_meltdown`, dano × 4), `phase.call_the_crew` (30%: 2 Mineiros e a **Fornalha Acesa**, `status.overseer_overdrive`, velocidade × 1,3 e dano × 1,15).
- Encontro `encounter.forged_overseer` (no fim de `data/encounters.json`), na `map.watcher_hall`, provisória até a Fundição da T-533.

**Core (com teste em `tests/Game.Core.Tests/OverseerMonsterTests.cs`)**, só no `ai.boss` (`BossProfile`), sem mudar os outros chefes:
- parâmetro **`meleeTargetRange`**: o chefe bate no de maior ameaça entre quem está a até N tiles dele; sem ninguém perto, segue a tabela. Sem ele, o Capataz passava a luta atrás do Piromante, que gera muito mais ameaça que o Guardião no 27;
- papel **`ai.ranged`** no `ai.boss`: a habilidade só sai com o alvo a `farDistance` (3) ou mais, como no `ai.brute` (a Escória Ardente; sem ela, o Mago fugia do chefe pela sala de graça quando o tanque caía).
- O resto usa o que já existia: cone com telegráfo, círculo sem telegráfo, invocação e estado ao entrar na fase, `onExpire` de estado (T-421) para o contador. Os testes conferem a Fornalha (todos na sala de 40 × 28, nenhum monstro, só da fase 2), o cone curto do Martelo, a fase 3 (2 Mineiros, mais velocidade), a escolha de alvo nova e o Vigia sem ela.

**Testes de design** (`tests/Game.Design.Tests/OverseerDesignTests.cs`, 100 seeds; relatório **`tasks/reports/balance-v0.26.md`**: v0.23 é da T-505, v0.24 da T-510 e v0.25 da T-511). Perfil de grupo do nível 27 com segunda classe e raros, como o Vigia:
- **Guardião + Atirador + Piromante + Sacerdote: 97%, 273 s** (meta ≥ 75% em 3–6 min);
- **sem Guardião e sem Sacerdote** (Berserker + Atirador + Piromante + Inquisidor): **7%** (meta ≤ 40%);
- medidos: só o Guardião 92% (305 s); só o Sacerdote 100% (283 s); Guardião + Caçador + Arcanista + Sacerdote 100% (275 s).
- Simulador: medida nova `DamageTakenBySource` (dano recebido por habilidade de origem) em `tests/Game.Sim`.

**Decisões e desvios.**
- **O limite de tempo (Fornalha Transbordando) é o que separa os grupos.** Sem ele, subir o dano levava os dois grupos juntos de ~90% a ~10% num degrau: o Inquisidor cura quase tanto quanto o Sacerdote e a Sede de Sangue do Berserker compensa a defesa do Guardião. A diferença estável é o tempo: com o chefe parado na frente do Guardião o Piromante rende ~990 de dano por segundo (contra ~600 com o Berserker), e a Bênção e a Inspiração somam 10–20%; a luta leva ~273 s com os papéis e ~360 s sem eles. O contador começa na fase 2 porque a fase 1 é entrada quando o chefe nasce.
- "Acelera" (fase 3) = Fornalha Acesa (velocidade e +15% de dano) e Martelo com recarga de 6 s.
- O Martelo quase nunca acerta nos testes (os bots saem do telegráfo); fica pela proposta e para quem não lê o chão.
- **Fora das "Áreas que pode alterar"** (mínimo): `tests/Game.Data.Tests/ProgressionContentTests.cs` (tabela por área e lista de monstros), `site/content/site.json` (descrição do Capataz e nomes das três fases) e `tests/Game.Sim` (a medida nova).

**`./scripts/check.sh` completo:** formatação e build sem avisos; todos os projetos passam, menos os **4 testes de design que já falham na `main`** e nas T-502/T-510/T-511, com os mesmos números: `SecondClassDesignTests.Guardian_cuts_the_damage_on_the_rest_of_the_group_by_at_least_15_percent` (nv30 94%, nv40 90%; meta ≤ 85%), `WatcherDesignTests.Without_the_cleric_the_win_rate_drops_clearly` (32%; meta ≤ 30%) e `CryptDesignTests.A_group_of_four_with_rares_beats_the_watcher_in_the_real_room_80_to_99_percent_in_4_to_6_minutes` (71%; meta 80–99%). Os testes de design levam ~18 min no Windows; os do Capataz, ~1,5 min.

**Pendências.**
- **T-533:** a Fundição e as salas propostas; medir de novo com as paredes. Uma derrota do grupo precisa reiniciar o chefe (vida, fase, contador): o Core não tem reset de chefe.
- **T-540:** o cone do Martelo, a fornalha acendendo a cada 5 s, o contador da Fornalha Esquentando (é o limite da luta) e a Fornalha Transbordando.
- **T-521:** o corpo do Capataz (não há no gerador LPC).
- **T-550/designer:** no 27 o tanque não segura chefe pela ameaça contra o Piromante; o `meleeTargetRange` resolve só o Capataz (provável causa também da falha antiga do Guardião no 30/40). O Inquisidor cura em grupo quase como o Sacerdote. A meta "sem papéis ≤ 40%" depende do tempo da luta.
- `design/habilidades.md` § Monstros não foi atualizado (fora das áreas, como na T-510 e na T-511).
