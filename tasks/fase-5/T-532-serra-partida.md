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

_(a preencher)_
