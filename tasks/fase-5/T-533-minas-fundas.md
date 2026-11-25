# T-533 — Mapa e instância: Minas Fundas

- **Ambiente:** Windows (Tiled)
- **Modelo:** Humano + Médio
- **Depende de:** T-501, T-505, T-512, T-532
- **Libera:** T-550
- **Áreas que pode alterar:** `data/maps/` (mapa novo `deep_mines.tmj`, `broken_ridge.tmj` para o portão da entrada), `data/zones.json`, `data/maps/tilesets/`, `client/assets/art/tiles/` (com créditos), `CREDITS.md`, `tests/`, `tasks/reports/`

## Contexto

As Minas Fundas (`design/mapas-16-40-v0.1.md` §3.4) são a masmorra instanciada do nível 24 ao 27, para 4 pessoas, e o conteúdo que se repete da faixa 21–27. É a primeira masmorra em que o papel de grupo importa: o Capataz Forjado (T-512) pede tanque e cura. As instâncias já existem (T-350); a Cripta do Vigia (T-352) é o modelo.

## Requisitos

1. **Mapa** `data/maps/deep_mines.tmj`, com cerca de **140 × 80**, nos tiles de mina da T-520, com as salas em sequência:
   - **Galeria de Entrada**: corredores e grupos pequenos;
   - **Poço dos Carrinhos**: sala grande com grupos de 6 a 10 (pede dano em área), carrinhos de mina como cobertura;
   - **Veio Partido**: duas rotas curtas, uma com o Golem de Pedra elite e mais saque, outra mais rápida;
   - **Fornalha**: arena do Capataz Forjado, com cerca de 30 × 24, sem obstáculos que atrapalhem a leitura dos telegráfos.
2. **Zona** `zone.deep_mines` do tipo `Instance`, nível 24–27, `maxPlayers` 4, os tempos da Cripta e `respawn` `zone.pass_fort` (T-501).
3. **Portão** da Boca das Minas, na Serra (`mines_entrance`), para a instância, e a saída de volta.
4. **Duração** esperada: 20 a 35 min. Registre a estimativa.
5. **XP por hora:** a meta de masmorra da T-505 (≥ 1,3 × a do campo da faixa, por pessoa, num grupo de 4).
6. O designer aprova o mapa.

## Critérios de aceite

- teste de percurso: da entrada ao chefe, alcançável a pé;
- testes de design das salas (grupo de 4 do nível 24–27 vence ≥ 80% de cada sala) e o chefe com as metas da T-512;
- online: dois clientes no mesmo grupo entram juntos na mesma cópia e chegam à Fornalha; quem morre renasce no Forte e o portão leva de volta à mesma cópia;
- `./scripts/check.sh` completo passa.

## Resultado

**Parcial: feito, falta só a aprovação do designer no jogo** (requisito 6). Feito em 2026-11-24, no Windows, na branch
`task/T-533-minas-fundas`. **Base:** `origin/task/T-532-serra-partida` (`de7143c`), que já contém a T-531, a T-530 e a cadeia
502 → 510 → 511 → 512 → 513, além da 501, 503, 505 e 520. **Integrar na `main` depois da T-532.** Relatório:
`tasks/reports/balance-v0.28.md`; mapa, imagens e ferramentas: `tasks/reports/minas-fundas/README.md`.

**Mapa** `data/maps/deep_mines.tmj` (140 × 80), no tileset da mina da T-520 (`pixellab_mina_0`, parede com face de vigas, topo
e face bloqueando), trilhos de `pixellab_sul_props` e carrinhos de minério de `pixellab_serra_props` (2 × 2, bloqueiam). Nada
gerado de novo. Salas em sequência:
- **Galeria de Entrada:** túnel da saída na borda oeste, salão da entrada e uma galeria em U; 4 grupos pequenos (4–5) nv24.
- **Poço dos Carrinhos:** salão de 38 × 53 com 13 grupos de 6 a 10 (Mineiros e Morcegos, nv24–25), carrinhos como cobertura.
- **Veio Partido**, duas rotas curtas até a **Antessala** (a guarda da Fornalha, nv26): o **Veio Rico** (3 grupos nv26, com o
  Golem de Pedra elite e a matilha; mais monstros e mais saque) e o **Veio Estreito** (túnel de 8 tiles, 5 Lobos nv26).
- **Fornalha:** arena de **40 × 28** livres, sem obstáculos, área `boss_arena`, com o `monster.forged_overseer` nv27. O
  Capataz (aggro 40) não vê a Antessala: a descida entra pelo canto noroeste.
- 23 grupos, 162 monstros, todos a ≥ 12 tiles uns dos outros.

**Zona** `zone.deep_mines` (instância, níveis 24–27, `maxPlayers` 4, `emptyTimeout` 300 e `outsiderTimeout` 30 como a Cripta,
`respawn` `zone.pass_fort`). **Portão:** na Serra, `mines_gate` na boca da mina (dentro de `mines_entrance`, que ganhou nome,
nível recomendado e aviso, como a entrada da Cripta) e a chegada `from_mines` fora dela; nas Minas, a saída `exit` leva de volta
à Serra. O portão foi posto no gerador da T-532 (`build_ridge.py`), que refaz o `broken_ridge.tmj` igual mais o portão.

**Medidas** (100 seeds; detalhes no relatório):
- salas: grupo de 4 de referência (Guardião, Caçador, Piromante, Sacerdote) no nível do grupo vence **100%** de todas (2–3 s;
  o Golem 11 s); meta ≥ 80%;
- chefe na Fornalha real, metas da T-512: com Guardião e Sacerdote **100% em 270 s** (≥ 75% em 3–6 min); sem os dois **9%**
  (≤ 40%);
- duração estimada: **22,2 min pelo Veio Rico** e **20,4 min pelo Veio Estreito** (meta 20–35);
- **XP por hora** (meta da T-505 ligada, `ZoneGoalDesignTests`): nv24 28.330 (Veio Rico) e 27.810 (Veio Estreito) contra
  14.394 na Serra; nv25 24.962 e 24.391 contra 11.495. **Masmorra ÷ campo: 2,06× e 2,02×** (meta ≥ 1,3×, conferida nas duas
  rotas).

**Testes novos ou mudados** (`tests/`):
- `ZoneMapTests`: as Minas na lista de zonas (tudo alcançável a pé, portões batem); instância de 4 com saída para a Serra,
  `respawn` no Forte, salas em sequência, níveis 24–26, Golem só no Veio Rico, Capataz nv27 numa Fornalha ≥ 30 × 24 sem
  obstáculo; grupos grandes no Poço, pequenos na Galeria, o Veio Rico com mais monstros e o Estreito mais curto; sem bolsão nem
  fresta. O teste da Serra agora espera dois portões e o das Minas dentro de `mines_entrance`.
- `MinesInstanceTests` (servidor): da Cidade até a mesma cópia em grupo; morrer nas Minas leva ao Forte e o portão da Serra
  volta à mesma cópia; o Capataz recomeça só quando o grupo inteiro cai.
- `MinesDesignTests` (salas, chefe na Fornalha real, duração por rota) e a meta de XP por hora das Minas em
  `ZoneGoalDesignTests` (por rota, com o chefe na Fornalha real). `OverseerDesignTests.Group` aceita outro encontro.
- `Game.NetBots`: opção `--route-again` (depois de renascer, refaz a rota; a linha ROTA mostra as chegadas e as cópias).

**Online** (servidor local, 2 bots em grupo, cópia de `data/` sem os monstros das salas e da Serra, como na T-352): entraram
juntos na mesma cópia e chegaram à Fornalha em 69 s; morreram para o Capataz, renasceram no Forte e voltaram pela boca da mina
à **mesma cópia** (4 entradas na instância 2 cada um, 3 chegadas à Fornalha em 300 s). Capturas no jogo em
`tasks/reports/minas-fundas/` (boca da mina, entrada, Galeria, Poço, Veio Rico, Antessala, Fornalha).

**Decisões e desvios:**
- **Fornalha com 40 × 28, não 30 × 24.** Com 30 × 25 o grupo sem Guardião e sem Sacerdote vencia 83% (meta da T-512 ≤ 40%):
  o Capataz anda pouco na sala pequena e a luta sem os papéis acaba antes da Fornalha Transbordando. Com o tamanho do salão em
  que a T-512 balanceou o chefe, cai para 9%. A Cripta fez o mesmo (T-352).
- **Mais grupos que a proposta da T-512** (que tinha 4 salas e 5 grupos): com lutas de sala de 2 s, a primeira versão (10
  grupos) levava 11–13 min. A Galeria virou um U com 4 grupos, o Poço tem 13, e entrou a guarda da Antessala.
- **"Veio Partido"** são as duas subáreas `Veio Rico` e `Veio Estreito`; a Antessala é uma subárea própria (a sala onde as
  rotas se juntam).
- **Reinício do chefe (pendência da T-512).** Fora das áreas da tarefa, o mínimo em `src/Game.Server/Zones/OpenZone.cs`: numa
  instância, o chefe que entrou em luta e ficou sem ninguém na tabela de ameaça (o grupo inteiro morreu ou saiu) some com as
  invocações dele e nasce de novo no ponto (vida cheia, fase 1, sem estados). Vale também para o Vigia na Cripta; os chefes de
  campo não mudam. Sem isso, quem voltasse pelo portão acharia o Capataz pela metade e com a Fornalha Transbordando (dano × 4)
  para sempre.

**`./scripts/check.sh` completo:** formatação e build sem avisos; todos os projetos passam, menos os **4 testes de design que
já falham na `main`** e nas T-502 a T-532, com os mesmos números: `CryptDesignTests.A_group_of_four_with_rares_beats_the_watcher_in_the_real_room...`
(71%), `WatcherDesignTests.Without_the_cleric_the_win_rate_drops_clearly` e
`SecondClassDesignTests.Guardian_cuts_the_damage_on_the_rest_of_the_group_by_at_least_15_percent` (nv30 94%, nv40 90%). Os
testes de design levaram 42,5 min (618 testes); os das Minas, ~1,5 min.

**Pendências:**
- **Designer:** aprovar o mapa no jogo (Galeria em U, Poço com carrinhos, as duas rotas, Antessala, Fornalha de 40 × 28 e a
  boca da mina na Serra).
- **T-550/designer:** as salas são fáceis para 4 com dano em área (2 s por grupo), e a XP por hora fica no dobro do campo
  (meta ≥ 1,3×), puxada pelas pausas do modelo e pelo chefe. Endurecer as salas pede monstros "de masmorra" (`data/monsters.json`).
  No Poço os grupos ficam a 12–14 tiles e os monstros puxam a 14: dá para puxar dois de uma vez.
- **Cliente (T-540 ou depois):** a barra do Capataz aparece no alto em toda a instância, desde a entrada.
- `design/combat-encounters-v0.1.md` § Minas Fundas ainda fala da "Fundição" e das salas propostas na T-512; `design/map-conventions-v0.1.md`
  §10 não lista a Serra nem as Minas (fora das áreas desta tarefa).
