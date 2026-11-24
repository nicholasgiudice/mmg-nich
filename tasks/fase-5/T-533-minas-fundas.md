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

_(a preencher)_
