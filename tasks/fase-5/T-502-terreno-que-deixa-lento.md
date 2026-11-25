# T-502 — Core: terreno que deixa lento

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** —
- **Libera:** T-510 (ordem de integração), T-530, T-540
- **Áreas que pode alterar:** `src/Game.Core/` (movimento, estados, leitura das áreas do mapa), `src/Game.Data/TiledMapReader.cs`, `data/statuses.json`, `data/schemas/`, `data/maps/` (arena de teste nova), `data/encounters.json`, `design/map-conventions-v0.1.md` (§6), `tests/`

## Contexto

O Brejo dos Afogados (`design/mapas-16-40-v0.1.md` §3.1) tem **água funda que deixa lento**. É o truque da área: o jogador aprende a escolher onde lutar. Os homens-lagarto do brejo não sentem a água.

Hoje o mapa aceita áreas de evento (`areas`, `design/map-conventions-v0.1.md` §6) e o Core guarda as propriedades como texto, mas nenhuma área muda o movimento. A classe `web` está no documento como exemplo e não tem mecânica. Lentidão por estado já existe (`status.slow`, `status.mist_chill`), e a predição do cliente (T-108) já lida com velocidade vinda de estado.

## Requisitos

1. Classe de área nova **`slow_terrain`** (retângulo na camada `areas`), com as propriedades:
   - `status` (obrigatória): id do estado aplicado enquanto a unidade está dentro;
   - `ignoreTags` (opcional): tags de monstro que não sentem o terreno, separadas por vírgula (ex.: `amphibious`).
2. Estado novo `status.deep_water`: −40% de velocidade, sem dano. Entra ao pisar na área e sai **no mesmo tick** em que a unidade sai dela (não fica um resto de duração). Vale para jogadores e monstros.
3. Não conta como controle: não sofre retorno decrescente, não é limpo pela Purificação e não é bloqueado pelas imunidades a controle dos elites e chefes. É terreno, não efeito de habilidade. Registre a escolha se o Core pedir outra forma.
4. A IA **não** desvia da água (é a forma mais simples). O jogador usa isso a seu favor. Registre se mudar.
5. Erros de carga: `slow_terrain` sem `status`, ou com um estado que não existe.
6. Documentar a classe em `design/map-conventions-v0.1.md` §6.

## Critérios de aceite

- testes do Core: velocidade cai ao entrar e volta ao sair, no tick certo; monstro com tag em `ignoreTags` não é afetado; duas áreas sobrepostas não acumulam;
- arena de teste com um trecho de água: um encontro do simulador mostra que lutar fora da água rende mais que dentro, para uma classe corpo a corpo;
- o estado aparece no snapshot como os outros (o cliente mostra o ícone sem mudança no protocolo). Se precisar subir `ProtocolVersion`, a partir do número da `main`;
- `./scripts/check.sh` completo passa.

## Resultado

Feito em 2026-11-24, branch `task/T-502-terreno-que-deixa-lento`.

**O que foi feito**

- **Classe de área `slow_terrain`** (camada `areas` do Tiled). Propriedades: `status` (obrigatória) e `ignoreTags` (opcional,
  tags de monstro separadas por vírgula, espaços e itens vazios ignorados). No Core vira `Spatial.TerrainArea`
  (`src/Game.Core/Spatial/Terrain.cs`), lida das áreas da arena por `EntityFactory.BuildMap(arena)` e guardada no próprio mapa
  (`GridMap.Terrain`, imutável; `GridMap.WithTerrain`). Assim o servidor (`OpenZone`), os encontros, o simulador e o cliente
  recebem o terreno sem mudar quem cria o mundo.
- **Estado `status.deep_water`** ("Água Funda", `data/statuses.json`): tags `debuff` e `terrain`, `speedMultiplier` 0,6
  (−40%), sem dano. `defaultDuration` 1 s só serve para manter o estado vivo (ver abaixo).
- **`TerrainSystem`** (`src/Game.Core/Combat/Areas/TerrainSystem.cs`, ordem nova `SystemOrder.Terrain = 2800`, depois de
  tudo que move e antes de Status). A cada tick, para cada unidade com `Movement`: se está viva, com o centro dentro de uma
  área e sem tag de `ignoreTags`, tem o estado; senão, não tem. Entrar aplica pelo `StatusManager.Apply` sem fonte (evento
  `StatusApplied`); ficar dentro empurra o fim para "agora + duração padrão" em silêncio (sem `StatusRefreshed` a cada
  tick), então nunca acaba lá dentro; sair remove no mesmo tick (`StatusRemoved`). O estado do tick vale para o movimento do
  tick seguinte. Áreas sobrepostas com o mesmo estado dão uma instância só. Se algo tira o estado com a unidade ainda na
  água (ex.: levantar um caído), ele volta no tick seguinte. Unidade morta perde o estado.
- **Erros de carga:** `slow_terrain` sem `status` ou com id fora do padrão (`TiledMapReader`); estado que não existe
  (`GameDataValidator.ValidateMapReferences`, que é quem conhece os estados).
- **Arena de teste** `data/maps/terrain_test.tmj` (`map.terrain_test`, 40×20, borda de parede, metade oeste x < 20 é
  `slow_terrain` com `status.deep_water` e `ignoreTags = amphibious`). **Encontros** `encounter.terrain_water` e
  `encounter.terrain_dry`: Guerreiro nível 1 contra dois Salteadores (atiradores que recuam) nível 1, a mesma posição
  espelhada, um dentro da água e outro fora.
- **Documentação:** `design/map-conventions-v0.1.md` §6 (linha da tabela e parágrafo da regra).
- **Testes:** `tests/Game.Core.Tests/TerrainTests.cs` (12: velocidade cai no tick seguinte ao de entrar e volta no seguinte
  ao de sair, com os eventos no tick certo; sem eventos de renovação parado na água; `ignoreTags`; áreas sobrepostas não
  acumulam; chefe entrando 5 vezes sem retorno decrescente nem redução de rank; Purificação e Inabalável não tiram; volta
  depois de ser tirado; morto perde; está no snapshot replicado; mapa sem terreno; arena → terreno),
  `tests/Game.Data.Tests/TiledMapLoaderTests.cs` (erros de carga), `tests/Game.Sim.Tests/TerrainSimTests.cs` (os dois
  encontros começam um na água e outro fora; seco rende mais que água com `basic_only` e `rotation`).

**Simulador** (`dotnet run --project tests/Game.Sim -- --encounter encounter.terrain_water,encounter.terrain_dry --strategy rotation,basic_only --runs 50`):

| Encontro | Estratégia | Vitórias | Tempo até vencer | Dano recebido |
|---|---|---|---|---|
| água | `rotation` | 52% | 31,2 s | 341 |
| seco | `rotation` | 84% | 21,8 s | 280 |
| água | `basic_only` | 100% | 21,8 s | 314 |
| seco | `basic_only` | 100% | 15,7 s | 234 |

Na água os dois lados ficam lentos, mas quem corre levando tiro é o Guerreiro: lutar fora da água rende mais.

**Decisões**

- **Não é controle** (requisito 3), sem precisar de regra nova no Core: o estado não tem a tag `control` (as regras de rank e
  o retorno decrescente só olham essa tag) nem `slow`/`poison`/`curse` (as tags que a Purificação, o Inabalável, o Passo
  Livre e as limpezas de lentidão olham). A tag `terrain` não é usada por nada hoje; serve para filtros futuros.
- **Por que não "permanente"** (`StatusManager.ApplyPermanent`, sem contagem no HUD): o `Leveling` tira todo estado
  permanente que não é de passiva ao sincronizar as passivas. Ficou estado normal, mantido vivo pelo sistema; o HUD mostra
  "Água Funda 1.0s" parado enquanto a unidade está na água.
- **IA não desvia da água** (requisito 4, sem mudança).
- **Pelo centro da unidade**, como o resto das áreas (`RectDefinition.Contains`).
- O estado de terreno é exclusivo do terreno: fora de toda área que o aplica, o sistema o tira, mesmo que outra coisa o
  tenha posto (hoje nada põe).
- **Protocolo:** sem mudança (o estado vai no `StatusEffects` replicado, como os outros); `ProtocolVersion` igual.

**Verificação** (`./scripts/check.sh` completo, Windows): formatação e build sem avisos; todos os projetos de teste passam,
menos o `Game.Design.Tests`, com **4 falhas em 304** (46 min). São as mesmas 4 que a T-500 registrou na `main` antes desta
tarefa, com os mesmos números: Vigia sem o Clérigo ainda vence 99%; grupo de quatro com raros vence o Vigia da Cripta 71%
(meta 80–99%); o Guardião corta só 10% e 6% do dano nos níveis 30 e 40. Nenhum mapa real tem `slow_terrain`, e sem
terreno o `TerrainSystem` volta na primeira linha, então o conteúdo existente roda igual.

**Desvios (fora de "Áreas que pode alterar")**

- `src/Game.Data/GameDataValidator.cs`: conferir que o estado existe precisa da lista de estados, que o `TiledMapReader` não
  tem. Mudança mínima: parâmetro `statuses` em `ValidateMapReferences` e um laço.
- Nenhuma mudança em `Zones.cs`, `OpenZone`, cliente nem protocolo.

**Pendências**

- A arena de teste usa só grama no chão (é para o simulador); a água não aparece no visual. Os tiles de água do Brejo são da
  tarefa do mapa.
- O gerador do site avisa que `status.deep_water` "não é aplicado por nada usado no jogo" (o `Usage` do site não olha as
  áreas dos mapas); é só aviso. Quando o Brejo usar a água, vale ensinar o `Usage` a contar `slow_terrain`.
- A predição do cliente (T-108) só vê o estado quando o snapshot chega: ao entrar ou sair da água há uma pequena correção
  de posição, como com qualquer lentidão vinda do servidor.
