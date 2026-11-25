# T-532 — Mapa e zona: Serra Partida

- **Ambiente:** Windows (Tiled)
- **Modelo:** Humano + Médio
- **Depende de:** T-505, T-511, T-520, T-531
- **Libera:** T-533, T-550
- **Áreas que pode alterar:** `data/maps/` (mapa novo `broken_ridge.tmj`, `pass_fort.tmj` para abrir o portão leste), `data/zones.json`, `data/maps/tilesets/`, `client/assets/art/tiles/` (com créditos), `CREDITS.md`, `tests/`, `tasks/reports/`

## Contexto

A Serra Partida (`design/mapas-16-40-v0.1.md` §3.3) é o campo do nível 21 ao 25 e a **primeira área de segunda classe**. O truque são os **desfiladeiros**, onde os monstros vêm em fila (Atirador de Elite, Berserker), e os grupos grandes, que pedem dano em área. Os monstros vêm da T-511.

## Requisitos

1. **Mapa** `data/maps/broken_ridge.tmj`, com cerca de **200 × 160**, nos tiles de serra da T-520, e as subáreas em `areas`:
   - **Desfiladeiro**: corredores de 3 a 5 tiles entre penhascos, mineiros e lobos, 21–22;
   - **Acampamento dos Mineiros**: barracas e carroças, grupos grandes de mineiros (6 a 10), 22–24;
   - **Ninho dos Morcegos**: entrada de caverna, bandos de morcegos, 23–24;
   - **Boca das Minas**: o Golem de Pedra (elite, 25, renasce em 3 min) e a entrada das Minas Fundas (`mines_entrance`, T-533).
2. Pelo menos um desfiladeiro reto e longo, onde a Rocha Rolante do Golem e as flechas do Atirador fazem sentido.
3. **Zona** `zone.broken_ridge` (campo, nível 21–25), com `respawn` `zone.pass_fort` (T-501).
4. **Portão** oeste para o Forte do Passo; abrir o portão leste do Forte.
5. **Densidade:** grupos a ≥ 12 tiles entre si; pelo menos um terço dos grupos com 5 ou mais monstros.
6. **Ritmo:** a meta de campo da T-505 (21→25), com o perfil esperado de segunda classe.
7. O designer aprova o mapa.

## Critérios de aceite

- teste de percurso: todo surgimento e portão alcançável a pé;
- testes de design da Serra passando;
- online: Forte → Serra → morrer de propósito → renascer no Forte;
- `./scripts/check.sh` completo passa.

## Resultado

**Situação:** pronto em branch, **falta só a aprovação do designer no jogo** (mapa, caverna, boca da mina, barracas e
carroças). Relatório com capturas: `tasks/reports/serra-partida/README.md`.

**Base e integração.** Branch `task/T-532-serra-partida` criada de `origin/task/T-531-forte-do-passo` (`7b88cac`), que já
tem a T-530, a cadeia 502 → 510 → 511 → 512 → 513 e a 501, 503, 505 e 520. **Entra na `main` depois da T-531.** A T-533
(Minas Fundas) sai desta branch.

**O que foi feito**
- `data/maps/broken_ridge.tmj` (200 × 160), tileset da serra da T-520 (`pixellab_serra_0`, opção B), platô e face do
  penhasco bloqueando. Subáreas Desfiladeiro, Acampamento dos Mineiros, Ninho dos Morcegos e Boca das Minas; área
  `mines_entrance` na boca da mina (T-533). 47 grupos, 174 monstros (só os da T-511), níveis 21–25, todos a ≥ 12 tiles;
  16 grupos (34%) com 5 a 10 monstros. Desfiladeiro reto de 128 tiles (5 livres) do portão oeste à Boca das Minas, com o
  Golem (elite nv25, renasce em 180 s) na linha dele. Gerado por `tasks/reports/serra-partida/tools/build_ridge.py`.
- `zone.broken_ridge` em `data/zones.json` (campo, 21–25, `respawn` `zone.pass_fort`).
- `pass_fort.tmj`: portão leste aberto (coluna 59, linhas 22–25) → `zone.broken_ridge`, chegada `from_ridge`.
- Arte nova: `pixellab_serra_props` (barraca e carroça de minério, PixelLab, 2 gerações do plano), com `CREDITS.md`.
- Testes: `ZoneMapTests` (Serra na lista de zonas; subáreas, Golem, `mines_entrance`, grupos e densidade, desfiladeiro
  reto, sem bolsões nem frestas; o teste do Forte passou a pedir o portão leste aberto). `RidgeZoneDesignTests` novo
  (grupos do mapa contra as 8 segundas classes). `ZoneGoalDesignTests.SoloHunt` tira os grupos grandes e o Golem da Serra.
  `FieldGroups` aceita até 10 monstros corpo a corpo; `ZonePace.SoloBatches` expõe os lotes por perfil.

**Medidas**
- **XP por hora da Serra (T-505, meta ligada sozinha):** 21 → 25 em **5,91 h = 84% das 7 h da curva** (meta 75–130%),
  **12.516 XP/h** (faixa aceita 8.130–14.100, alvo 10.573), vitória mínima 100%. Por nível: 11.607 / 11.948 / 12.147 /
  14.394 XP/h (nv21–24). O ritmo do Brejo não mudou (4,73 h).
- Grupos de caça solo: as 8 segundas classes vencem 100%. Grupos grandes: as 6 que não são do Clérigo vencem 87–100%;
  Sacerdote e Inquisidor só medidos (pedem grupo). `RidgeDesignTests` (T-511) continua passando.
- Online: bots nível 1 saíram do Forte pelo portão leste, morreram na Serra e **renasceram no Forte** (16 vezes). Com o
  cliente da T-541 (cópia temporária com a T-541 mesclada, nada commitado), a tela de morte na Serra mostra **"Retornar ao
  Forte do Passo"** (`serra-morte-retornar-ao-forte.png`).

**Decisões**
- Grupos de caça solo em **pares** (a XP de 6 × nível por monstro com o descanso de 50 s do modelo fazia um grupo de 4
  render ~19.500 XP/h, acima da faixa). Os grupos de 5 a 10 ficam fora do ritmo solo, como a Cabana da Bruxa do Brejo:
  rendem de 3 a 5 vezes mais para quem tem dano em área (Piromante/Arcanista ~62.500 XP/h neles).
- No máximo 4 Mineiros por grupo grande (com 5–6 Mineiros nv23–24 o Guardião, o Berserker e o Atirador sozinhos venciam
  0–80%); o resto do acampamento é Morcego.
- Traçado em blocos de 2 × 2 células: o tileset não tem degrau de 1 linha na base do penhasco.
- A boca da mina e a caverna são recortes no penhasco com o piso escuro da mina; a boca bloqueia até a T-533 pôr o portão.

**Desvios**
- Mexi em `tests/Game.Design.Tests/FieldsDesignTests.cs` (3 posições a mais em `FieldGroups`) e `ZonePace.cs`
  (`SoloBatches`): só testes, dentro de `tests/`.
- `design/map-conventions-v0.1.md` §10 não foi atualizado (fora das áreas da tarefa).

**Pendências**
- Aprovação do designer no jogo.
- T-533: portão das Minas dentro de `mines_entrance` (células 170–175, linhas 52–61).
- T-550: XP por hora dos grupos grandes com dano em área; lutas reais no corredor (os testes usam a arena aberta).
- Os monstros da Serra aparecem como círculos até a T-540/T-521 entrarem.

**`./scripts/check.sh` completo:** formatação e build sem avisos; todos os projetos de teste passam, menos as **4 falhas de
`Game.Design.Tests` que já existem na `main`** (não são desta tarefa): `CryptDesignTests.A_group_of_four_with_rares_beats_the_watcher...`,
`WatcherDesignTests.Without_the_cleric_the_win_rate_drops_clearly` e `SecondClassDesignTests.Guardian_cuts_the_damage...`
(nv30 e nv40). Game.Design.Tests: 592 aprovados, 4 com falha, 1 ignorado (23 min 43 s).
