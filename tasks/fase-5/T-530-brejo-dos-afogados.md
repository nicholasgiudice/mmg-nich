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

**Parcial: falta só a aprovação do designer no jogo** (requisito 7). Relatório com imagens: `tasks/reports/mapa-brejo/README.md`.

**Base e ordem de integração.** As dependências estavam prontas em branches, fora da `main`. A branch
`task/T-530-brejo-dos-afogados` saiu de `origin/task/T-510-monstros-do-brejo` (`5caadf9`, que já contém a T-502 `6ee2b2e`)
com merge, nesta ordem, de `origin/task/T-501-renascer-no-ponto-seguro`, `origin/task/T-503-anuncio-de-chefe-de-campo`,
`origin/task/T-505-xp-por-hora-por-area` e `origin/task/T-520-tilesets-do-sul` (`a41c0f3`). Integrar na `main` nesta ordem:
**502, 510, 501, 503, 505, 520, depois esta**. Único conflito: `tests/Game.Data.Tests/TiledMapLoaderTests.cs` (o teste
de `slow_terrain` da T-502 e o de `announce` da T-503 no mesmo lugar; ficaram os dois). `Zones.cs`, `TiledMapReader` e
`WorldServer` juntaram sozinhos as três mudanças. **Atenção:** a T-503 sobe o protocolo para 20 e só entra na `main`
junto com a T-540; esta tarefa depende dela (`announce` da Bruxa), então também espera a T-540.

**O que foi feito.**
- **Mapa** `data/maps/drowned_marsh.tmj` (200 × 160) com os tilesets do brejo C (`pixellab_brejo_0`, `pixellab_brejo_1`)
  e as quatro subáreas: Estrada dos Juncos (aterro de lama seca com água rasa dos lados, Afogados nv16 e mistos nv17),
  Aldeia das Palafitas (lagoa rasa com seis casas em ilhas, bandos de 3 Afogados nv17–18, mistos, Homens-lagarto e
  Serpentes nv18), Charco Fundo (poços de água funda, Serpentes e Homens-lagarto nv18–19) e Cabana da Bruxa (ilha cercada
  de água funda, Bruxa nv20 e bandos de 4 a 6 Afogados nv19). 42 grupos, 106 monstros, a ≥ 12 tiles entre si.
- **Água funda:** 157 retângulos `slow_terrain` (`status.deep_water`, `ignoreTags` `amphibious`). A estrada seca vai do
  portão norte à borda sul sem pisar nela.
- **Bruxa:** `monster_spawn` com `announce` true, `respawn` 600, nível 20.
- **Zona** `zone.drowned_marsh` (campo, 16–20, corpo 10 s), sem `respawn`: quem morre volta à Cidade (T-501).
- **Cidade:** portão sul `south_gate` aberto nas células 46–49 da muralha (entre o Armazém e o Mercado), com os pilares
  do portão norte, e a chegada `from_marsh`. No Brejo, `north_gate` de volta, com `from_city` e o `spawn` da zona.
- **Arte nova:** `pixellab_brejo_2` (lama → juncal fechado, que bloqueia), para a borda e as moitas. 15 gerações no
  PixelLab (3 do plano + US$ 0,12). Créditos em `CREDITS.md`.
- **Testes:** `ZoneMapTests` (percurso, portões, estrada seca até a borda sul, água funda, distância entre grupos, Bruxa,
  sem bolsões nem frestas); `MarshZoneDesignTests` (novo: grupos solo ≥ 85% nas 4 classes, grupos da Cabana em dupla
  ≥ 80%); os testes de meta do Brejo da T-505 (`ZoneGoalDesignTests`) ligaram sozinhos com a zona e passam.

**Medidas.** Grupos solo: perfil esperado vence ≥ 99% em todos. Cabana em dupla: 99–100%. **Ritmo 16 → 20: 4,73 h**
(95% das 5 h da curva; meta 75%–130%), 9.967 XP/h. Bruxa (grupo de 4): 7.020 XP/h por pessoa, vitória 100%. Online
(servidor local `--world --dev-login --dev-tools`, `Game.NetBots`): os bots saíram da Cidade pelo portão sul, chegaram à
água funda e voltaram (pelo portão no `--roam`; renascendo na Cidade quando morreram). No cliente, o personagem dentro
do poço do Charco mostra "Água Funda".

**Decisões.**
- Bandos de 4 a 6 Afogados só na Cabana ("grupo recomendado", fora do ritmo solo): na Aldeia, 4 Afogados nv17–18 davam
  41–46% ao Guerreiro e 0–6% ao Clérigo. Os bandos solo têm 3.
- Pares de Homens-lagarto nv17 viraram grupos mistos (Afogado + Homem-lagarto): o Clérigo vencia o par nv17 em 81%.
- O portão sul saiu do eixo da avenida (o Armazém fica no caminho; um bot em linha reta não chegava ao portão nas
  células 38–41).
- O gerador do mapa ficou no relatório (`tasks/reports/mapa-brejo/tools/build_marsh.py`); o `.tmj` é a fonte daqui em
  diante. As casas das palafitas e a cabana são cópias de prédios da Cidade.

**Check completo:** `./scripts/check.sh` passa, menos as 4 falhas de `Game.Design.Tests` que já existem na `main`
(`CryptDesignTests.A_group_of_four_with_rares_beats_the_watcher...`, `WatcherDesignTests.Without_the_cleric...` e
`SecondClassDesignTests.Guardian_cuts_the_damage...` nos níveis 30 e 40). Os testes do Brejo passam todos.

**Fora das áreas listadas (mínimo):** `tests/Game.Design.Tests/ZoneGoalDesignTests.cs` (o filtro de caça solo tira a
Cabana) e `client/assets/art/tiles/pixellab_brejo_2.png.import` (gerado pelo Godot). Não atualizei a tabela de mapas de
`design/map-conventions-v0.1.md` §10 (fora das áreas).

**Pendências.**
- Aprovação do designer no jogo (como ver: relatório).
- Cliente: a câmera para na borda do mapa, e o portão sul da Cidade (e o futuro portão sul do Brejo) fica atrás da
  barra de atalhos quando o jogador chega perto. Dá para passar, mas quase não se vê.
- T-531: portão sul do Brejo para `zone.pass_fort`, no fim do aterro (borda sul, células ~98–103).
- T-550: os testes de design rodam sem água; conferir no playtest o Charco e a Cabana com a água funda.
