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

**Feito.** Mapa aprovado pelo designer. Relatório: `tasks/reports/balance-v0.14.md`; imagens em `tasks/reports/mapas-t343/`.

- **Mapa** `data/maps/old_road_ruins.tmj` (180×140). Uma estrada imperial calçada sai do portão leste, que leva à Cidade, e atravessa as Ruínas até a Cripta, com mata fechada em volta.
  - **Portão Partido:** muralha com o portão quebrado, um corredor estreito e dois descampados com fundações de casas.
  - **Pátio dos Mortos:** pátio murado com piso de pedra rachada, colunas e um altar no centro.
  - **Torre Quebrada:** arena pequena e fechada ao norte do Pátio, ligada a ele por um corredor estreito.
  - **Estrada para a Cripta:** ruínas com corredores e uma praça no fim, com a escada da Cripta, um medalhão de pedra como placa e lampiões.
- **Entrada da Cripta:** área `crypt_entrance` com `name` "Cripta do Vigia", `recommended_level` 12 e `warning` "Conteúdo para grupo. Nível recomendado: 12+.". Não tem portão; ele é da T-352.
- **Zona** `zone.old_road_ruins` (field, nível 9–11) em `data/zones.json`. A distribuição dos grupos está no relatório, §2.
- **Cidade:** o portão oeste foi aberto, espelhando o leste. Ele tem a transição `west_gate` para as Ruínas e o nascimento `from_ruins`.
- **Testes:**
  - percurso: todo surgimento, portão e a entrada da Cripta alcançáveis a pé (`ZoneMapTests`);
  - dados da zona: subáreas, níveis, portões e distância de 12 tiles entre grupos (`ZoneLoaderTests`);
  - design: `RuinsZoneDesignTests`.
- **Resultados:**
  - os grupos solo vencem 100% nas 4 classes;
  - no Pátio, os pares vencem 100% e o solo fica em 62–64%;
  - o ritmo do nível 9 ao 12 é de ≈ 202 min;
  - online, 4 bots foram da Cidade à entrada da Cripta em 45–51 s, e a volta também funciona;
  - o `./scripts/check.sh` completo passa.

**Decisões:**
- Os grupos "mistos" da Estrada são Guardas de nível 10 e 11, sem Sacerdote. Com Sacerdote, o Guerreiro sozinho venceu 5%. Os Sacerdotes ficam no Pátio.
- O teste de solo do Pátio usa o limite de 240 s do encontro da T-341.
- O altar com maldição é só cenário.
- Como na T-330, o gerador do mapa (Python) ficou fora do repositório. O `.tmj` é a fonte daqui em diante.

**Mudanças em testes existentes e em ferramentas** (todas em `tests/`):
- `ZoneMapEquivalenceTests` agora aceita zonas e portões novos (verifica subconjunto); antes quebrava com qualquer zona nova.
- `FieldGroups` (em `FieldsDesignTests`) serve a qualquer zona, a grupos de jogadores e a outro limite de tempo. Monstros de apoio começam atrás.
- `Game.NetBots` ganhou `--route <zona>/<área>`: o bot anda por A* até o portão da zona e depois até a área.

**Pendências:**
- desempenho com 16 bots na VPS;
- a maldição do altar;
- fragmentos de chave da Cripta (fase futura, decisão da T-240);
- o portão para a instância (T-352).
