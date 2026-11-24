# T-531 — Mapa e zona: Forte do Passo

- **Ambiente:** Windows (Tiled)
- **Modelo:** Humano + Médio
- **Depende de:** T-501, T-513, T-520, T-530
- **Libera:** T-532, T-550
- **Áreas que pode alterar:** `data/maps/` (mapa novo `pass_fort.tmj`, `drowned_marsh.tmj` para o portão sul), `data/zones.json`, `data/maps/tilesets/`, `client/assets/art/tiles/` (com créditos), `CREDITS.md`, `tests/`, `tasks/reports/`

## Contexto

O Forte do Passo (`design/mapas-16-40-v0.1.md` §3.2) é o segundo ponto seguro do jogo: um posto militar pequeno no fim do Brejo, ao pé da Serra. Do 21 em diante ele faz o papel da Cidade, e quem morre na Serra ou nas Minas renasce nele (T-501).

## Requisitos

1. **Mapa** `data/maps/pass_fort.tmj`, com cerca de **60 × 45**, nos tiles de forte da T-520: paliçada ou muro, pátio, alguns prédios.
2. **Zona** `zone.pass_fort` do tipo `safe`, com descanso (`restPerSecond`) igual ao da Cidade.
3. **NPCs** (áreas `npc`):
   - o Intendente, com a loja `shop.pass_fort` (T-513);
   - um sacerdote com o Templo (`shop.temple`), para as poções;
   - o **mestre das classes** (`npc.class_master`), que abre a escolha da segunda classe no cliente (T-541);
   - um estalajadeiro, só decorativo (como na Cidade).
4. **Portões:**
   - norte, para o Brejo (`from_fort` no Brejo, `from_marsh` no Forte);
   - leste, para a Serra Partida (a T-532 põe o outro lado). Até a Serra existir, o portão fica **fechado** (muro inteiro), como os portões reservados da Cidade antes da T-342;
   - um portão sul reservado para o Planalto (fase 6), também fechado.
5. O designer aprova o mapa.

## Critérios de aceite

- teste de percurso: nascimento, NPCs e portões abertos alcançáveis a pé;
- teste de zona: `zone.pass_fort` é `safe` e não tem `monster_spawn`;
- online: Cidade → Brejo → Forte → comprar na loja do Intendente → voltar;
- `./scripts/check.sh` completo passa.

## Resultado

_(a preencher)_
