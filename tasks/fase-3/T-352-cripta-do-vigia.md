# T-352 — Mapa e instância: Cripta do Vigia

- **Ambiente:** Windows (Tiled)
- **Modelo:** Humano + Médio
- **Depende de:** T-343, T-350, T-351
- **Libera:** T-380
- **Áreas que pode alterar:** `data/maps/` (mapa novo `watcher_crypt.tmj`, `old_road_ruins.tmj` para o portão da entrada), `data/zones.json`, `data/maps/tilesets/`, `client/assets/art/tiles/` (com créditos), `CREDITS.md`, `tests/` (percurso e design da Cripta), `tasks/reports/`

## Contexto

A Cripta do Vigia (`design/first-map-v0.1.md` §5) é a dungeon de grupo, níveis 12–14, com o Vigia Sem Rosto (nível 15) no fim. A sequência é: Entrada → **Salão dos Escudos** (guardas; portas bloqueadas por grupos) → **Galeria das Cinzas** (sacerdotes; áreas de maldição) → **Câmara dos Juramentos** (duas rotas curtas: mais monstros e mais saque, ou menos combate) → **Sala do Vigia** (arena ampla, três selos visíveis nas bordas, sem obstáculos que atrapalhem a leitura).

A instância por grupo vem da T-350. O chefe e os selos vêm da T-351. A entrada fica nas Ruínas (`crypt_entrance`, T-343) e, nesta fase, fica **aberta**, sem chave (decisão da T-240).

## Requisitos

1. **Mapa** `data/maps/watcher_crypt.tmj` com as cinco salas em sequência. Arena do chefe com cerca de 30×24 e os selos em posições marcadas (`spawns` ou objetos próprios, conforme a T-351 definiu).
2. **Zona** `zone.watcher_crypt` do tipo `Instance` (T-350), com os monstros por sala nos níveis 12–14 (guardas, sacerdotes, e o Cavaleiro Espectral como elite numa das rotas da Câmara) e o Vigia nível 15.
3. **Portas bloqueadas por grupos** (Salão dos Escudos): se a T-350 não trouxer portas, use a forma mais simples, que são salas ligadas por corredores, sem portas que abrem. Registre a escolha. As portas que abrem ficam para depois.
4. **Galeria:** "inimigos que retornam uma vez se o altar não for desativado" **fica fora** desta fase, a não ser que já exista mecânica. Registre a decisão.
5. **Portão** das Ruínas (`crypt_entrance`) para a instância, e a saída da instância de volta para a entrada.
6. **Teste de design da Cripta inteira** (opcional, se o simulador permitir encadear salas): no mínimo, cada sala como um encontro com um grupo de 4 no nível 12–14 vencendo ≥ 80%, e o chefe com a meta da T-351.
7. Duração total esperada: 25–40 min. Registre a estimativa.
8. O designer aprova o mapa.

## Critérios de aceite

- teste de percurso: da entrada ao chefe, alcançável a pé;
- testes de design das salas passando;
- online (servidor local): dois clientes no mesmo grupo entram juntos na mesma cópia e chegam à sala do chefe;
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
