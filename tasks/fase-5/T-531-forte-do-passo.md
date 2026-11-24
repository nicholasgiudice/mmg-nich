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

**Estado: completo, falta a aprovação do designer no jogo (requisito 5).** O mapa do Brejo (T-530) também ainda espera a
aprovação dele. Relatório com as capturas: `tasks/reports/forte-do-passo/README.md`.

### Base e ordem de integração

Branch `task/T-531-forte-do-passo` criada de `origin/task/T-530-brejo-dos-afogados` (`c7badd4`, que já traz 502, 510, 501,
503, 505 e 520), com merge de `origin/task/T-513-dados-itens-lojas-e-quedas` (`b32474c`, que traz 511, 512 e 513) no commit
`9dd28e0`. O merge não teve conflito. Integrar na `main` depois da T-530 e da T-513 (e das que elas trazem). O check rápido
na base combinada passou em todos os projetos (as falhas de `Game.Sim.Tests` naquela rodada vieram do gerador desta tarefa,
que escreveu o portão para `zone.pass_fort` no Brejo enquanto os testes rodavam; sem isso, passa).

### O que foi feito

- **Mapa** `data/maps/pass_fort.tmj` (60 × 45), gerado por `tasks/reports/forte-do-passo/tools/build_fort.py` (o `.tmj` é a
  fonte daqui em diante). Tileset do forte da T-520 (opção C, `pixellab_forte_0`), autotile por `pattern_4x4` como nas
  amostras: muro de 2 células com face no norte e de 1 nos outros lados; face e topo bloqueiam. Pátio de terra batida com
  seis prédios copiados da Cidade (Capela, Salão dos Mestres, Estalagem, Intendência e dois quartéis sem NPC).
- **Zona** `zone.pass_fort` (`safe`, `restPerSecond` 0,05 como a Cidade), sem `respawn` (zona segura não leva; quem renasce
  no Forte são as zonas que apontam para ele, T-501/T-532). Nascimento no meio do pátio.
- **NPCs:** Intendente (`npc.quartermaster`, `shop.pass_fort`), Sacerdote (`npc.priest`, `shop.temple`), Mestre das Classes
  (`npc.class_master`, que a T-541 já trata como mestre) e Estalajadeiro (`npc.innkeeper`, decorativo).
- **Portões:** norte aberto ↔ portão sul do Brejo (`north_gate` → `zone.drowned_marsh`/`from_fort`; no Brejo `south_gate`,
  células 99–102, linhas 158–159, → `zone.pass_fort`/`from_marsh`, chegada `from_fort` em (101; 154,5)). Leste (Serra) e
  sul (Planalto) fechados, muro inteiro. No Brejo só entraram os dois objetos; terreno e colisão não mudaram.
- **Testes** (`tests/Game.Sim.Tests/ZoneMapTests.cs`): o Forte entra nos testes de todas as zonas (mapa do Tiled; todo
  surgimento e portão alcançável a pé; portões batem com as transições); novos: zona segura sem monstros, descanso igual ao
  da Cidade, só o portão norte aberto (muros leste, oeste e sul inteiros) e ligado ao portão sul do Brejo na borda de baixo;
  os quatro NPCs com as lojas certas, alcançáveis a pé; sem bolsão fechado nem fresta sem saída. Os testes do Brejo agora
  separam os dois portões, e a estrada seca vai do portão norte **ao portão sul** sem água funda.
  `ShopLoaderTests`: a lista de NPCs com loja inclui `shop.pass_fort` e o segundo `shop.temple`.
- **Online** (`tests/Game.NetBots`, servidor local `--world --dev-login --dev-tools`): 2 de 2 bots fizeram Cidade → Brejo →
  Forte, compraram uma Poção de Cura Menor com o Intendente e voltaram à praça da Cidade. Opções novas dos bots para isso:
  rota por várias zonas, `--shop-id`, `--shop-item`, `--return`, `--dev-level`, `--earn`.
- **No jogo** (cliente Godot com o servidor local): capturas em `tasks/reports/forte-do-passo/` (nascimento, os quatro NPCs,
  portão norte, chegada no Brejo pelo portão sul).

### Desvios e decisões

- **Fora das áreas listadas:** `client/assets/art/sprites.json` (2 linhas: folhas provisórias do Intendente, a do Guardião,
  e do Mestre das Classes, a do Arcanista; sem elas os NPCs novos apareciam como círculos) e `tests/Game.NetBots` (as opções
  dos bots para o teste online).
- **Prédios copiados da Cidade** em vez de arte nova (como as palafitas do Brejo); o designer prefere arte simples e decide
  no jogo. O topo claro do muro do forte C parece pavimento quando grosso: por isso 1 célula nos lados e no sul.
- Ids novos de NPC: `npc.quartermaster` (Intendente). O Sacerdote reusa `npc.priest`, o estalajadeiro `npc.innkeeper`.
- **Bot de loja:** o `--shop` antigo (Templo da Cidade) já não comprava nesta base: o personagem de `--dev-login` nasce sem
  ouro e as 5 poções iniciais vendem por 5, menos que os 15 da poção. O bot agora, sem nada para vender, pede a compra e o
  servidor decide (falta de ouro vira recusa, com o motivo na linha LOJA); `--earn` faz ele caçar antes no Brejo.
- `client/scripts/Account/OnlineCounterText.cs.uid` apareceu ao importar o cliente no Godot; não entrou no commit.

### Pendências

- Requisito 5: aprovação do designer no jogo (do Forte e do Brejo da T-530).
- Pátio liso, só terra e prédios; caixas, barris e estandartes pedem peças que os tilesets ainda não têm.
- Câmera e barra de atalhos (da T-530): o portão sul do Brejo fica atrás da barra de atalhos quando se chega perto.
- T-532: o portão leste do Forte (hoje muro) e o `respawn` da Serra e das Minas para `zone.pass_fort`.
- `design/map-conventions-v0.1.md` §10 (tabela de mapas) não foi atualizado: fora das áreas desta tarefa.

### Verificação

`dotnet format` limpo, `dotnet build -warnaserror` sem avisos e `./scripts/check.sh` completo: todos os projetos passam
(Sim 91, Data 548, Core 623, Server 123, Client 131...), menos `Game.Design.Tests` com **4 falhas em 514** (20 min), as mesmas
que já existem na `main`: Guardião 30 e 40 (`Guardian_cuts_the_damage...`), Vigia sem o Clérigo e Cripta com raros.
