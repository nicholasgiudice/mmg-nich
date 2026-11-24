# T-530 — Mapa e zona: Brejo dos Afogados (e o portão sul da Cidade)

- **Ambiente:** Windows (Tiled)
- **Modelo:** Humano + Médio
- **Depende de:** T-502, T-503, T-505, T-510, T-520
- **Libera:** T-531, T-550
- **Áreas que pode alterar:** `data/maps/` (mapa novo `drowned_marsh.tmj`, `city.tmj` para abrir o portão sul), `data/zones.json`, `data/maps/tilesets/`, `client/assets/art/tiles/` (com créditos), `CREDITS.md`, `tests/` (percurso e design da área), `tasks/reports/`

## Contexto

O Brejo dos Afogados (`design/mapas-16-40-v0.1.md` §3.1) é o campo do nível 16 ao 20 e a primeira área depois do primeiro mapa. Sai pelo **portão sul da Cidade**, que está fechado desde o primeiro mapa como promessa de expansão (`design/first-map-v0.1.md` §1). Na outra ponta, ao sul, fica o Forte do Passo (T-531).

Na Cidade (`city.tmj`, 80 × 60), a rua do sul já existe, mas o Armazém (NPC em ~39,52) fica perto do eixo do portão. A avenida pode desviar do prédio.

## Requisitos

1. **Mapa** `data/maps/drowned_marsh.tmj`, com cerca de **200 × 160**, nos tiles da T-520, e as quatro subáreas em `areas`:
   - **Estrada dos Juncos** (norte, saindo da Cidade): estrada elevada e seca, poucos monstros, nível 16–17;
   - **Aldeia das Palafitas**: afogados em bandos, 17–18;
   - **Charco Fundo**: muita água funda (`slow_terrain` com `status.deep_water` e `ignoreTags` `amphibious`, T-502), lagartos e serpentes, 18–19;
   - **Cabana da Bruxa**: a Bruxa do Brejo (nível 20, `respawn` 600, `announce` true, T-503) e afogados 19 em volta.
2. A estrada seca atravessa o mapa até a borda sul: quem só quer passar para o Forte consegue, evitando a água. A água funda fica onde se luta.
3. **Zona** `zone.drowned_marsh` (campo, nível 16–20). Sem `respawn`: quem morre aqui volta à Cidade (T-501).
4. **Portão sul da Cidade** aberto (`south_gate`), com a chegada `from_marsh`, e o portão do Brejo para a Cidade. O portão sul do Brejo, para o Forte (`zone.pass_fort`), entra na T-531.
5. **Densidade:** grupos a ≥ 12 tiles entre si; parte dos grupos com 4 a 6 monstros.
6. **Ritmo:** a meta de campo da T-505 (16→20 entre 75% e 130% do tempo da curva). Registrar o tempo no relatório.
7. O designer aprova o mapa, com imagens no relatório.

## Critérios de aceite

- teste de percurso: todo surgimento e portão alcançável a pé, e a estrada seca liga o portão norte à borda sul sem passar por `slow_terrain`;
- testes de design do Brejo passando (vitória dos grupos e ritmo);
- online (servidor local `--world --dev-login --dev-tools`): sair da Cidade pelo portão sul, sentir a água funda e voltar;
- `./scripts/check.sh` completo passa.

## Resultado

_(a preencher)_
