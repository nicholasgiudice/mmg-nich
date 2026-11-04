# T-343 — Mapa e zona: Ruínas da Estrada Antiga

- **Ambiente:** Windows (Tiled)
- **Modelo:** Humano + Médio
- **Depende de:** T-330, T-331, T-341
- **Libera:** T-352
- **Áreas que pode alterar:** `data/maps/` (mapa novo `old_road_ruins.tmj`, `city.tmj` para abrir o portão oeste), `data/zones.json`, `data/maps/tilesets/`, `client/assets/art/tiles/` (com créditos), `CREDITS.md`, `tests/` (testes de percurso e de design da área), `tasks/reports/`

## Contexto

As Ruínas da Estrada Antiga (`design/first-map-v0.1.md` §4) são a área de nível 9–11, saindo pelo **portão oeste** da Cidade. Subáreas: Portão Partido, Pátio dos Mortos, Torre Quebrada (Cavaleiro Espectral, elite opcional) e Estrada para a Cripta (a entrada da dungeon fica no fim).

Restos de uma estrada imperial e de uma fortificação antiga: corredores estreitos, combate frontal. Os monstros vêm da T-341.

**Decisão da T-240 sobre a chave da Cripta:** o design fala em juntar fragmentos de chave. Isso exige um sistema de missões e de itens de missão, que não entra nesta fase. **Na fase 3 a entrada da Cripta fica aberta**, com um aviso de conteúdo para grupo (nível recomendado 12+). Os fragmentos ficam para uma fase futura.

## Requisitos

1. **Mapa** `data/maps/old_road_ruins.tmj`, com cerca de **180×140**: corredores estreitos e pátios, com a Torre Quebrada como uma arena pequena e fechada.
2. **Zona** `zone.old_road_ruins` (tipo `field`), com a faixa de nível 9–11:
   - Portão Partido: guardas 9;
   - Pátio dos Mortos: guardas 9–10 com sacerdotes 10 atrás (o grupo típico da T-341);
   - Torre Quebrada: um Cavaleiro Espectral 11, com renascimento longo (proposta: 5 min);
   - Estrada para a Cripta: monstros mistos, níveis 10–11.
3. **Entrada da Cripta:** marcada no mapa como uma área `crypt_entrance` (o portão para a instância é da T-352). Nesta tarefa ela pode ser só a área marcada, com uma placa.
4. **Portão oeste da Cidade** aberto, ligando às Ruínas.
5. Grupos a ≥ 12 tiles entre si. Teste de design da área como na T-342. O Pátio dos Mortos fica marcado como "grupo recomendado" e não precisa passar na meta de solo de 85%: use a meta da T-341.
6. O designer aprova o mapa.

## Critérios de aceite

- teste de percurso: todo surgimento, portão e a entrada da Cripta alcançáveis a pé;
- testes de design das Ruínas passando;
- online (servidor local): entrar pela Cidade e chegar à entrada da Cripta;
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
