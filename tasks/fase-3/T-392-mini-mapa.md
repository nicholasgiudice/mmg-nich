# T-392 — Cliente: mini mapa

- **Ambiente:** Windows
- **Modelo:** Humano + Médio
- **Depende de:** —
- **Libera:** T-393
- **Áreas que pode alterar:** `client/scripts/Hud/` (painel novo e posição dele no `Hud.cs`), `client/scripts/Main.cs` (ligar o painel), `client/project.godot` (tecla do mapa), `client/assets/art/ui/`, `CREDITS.md`, `tests/Game.Client.Tests/`, `tasks/reports/interface/`

## Contexto

Pedido do designer durante o playtest da fase 3 (T-380), em 2026-11-17: criar um mini mapa. Os mapas novos são grandes (Floresta Baixa 200 × 160, Ruínas 180 × 140, Cripta 140 × 76) e o jogador e o grupo se perdem neles.

O designer pediu também a posição dos membros do grupo, mas o essencial é o mini mapa com a posição do jogador. Esta tarefa **não mexe no servidor nem no protocolo** e pode ser publicada sozinha. Ela mostra os membros do grupo que o cliente já recebe no snapshot (os que estão perto, na área de interesse do servidor, de uns 40 tiles). Os membros distantes ficam para a T-391 (servidor) e a T-393 (cliente).

O terreno sai do mesmo `.tmj` que o cliente já desenha (`client/scripts/Presentation/TiledVisual.cs`) e que o Core usa para colisão (`src/Game.Data/TiledMapReader.cs`).

## Requisitos

1. **Painel do mini mapa** no canto superior direito. A lista de batalha desce para baixo dele (o `Hud` já tem o `TopRightReserved`, que hoje o painel de zona usa). Moldura do kit pedra e ouro. Não cobre o painel do chefe (centro, no alto) nem o inventário.
2. **Terreno:** gerado uma vez por zona, ao entrar, a partir do `.tmj`. Chão andável e bloqueios (paredes, árvores, água) com cores diferentes, legíveis em escala pequena; os portões de troca de zona marcados. A escolha mais simples é 1 pixel por tile a partir da grade de colisão; cor média do tile do tileset é bem-vinda se ficar mais bonita.
3. **Recorte:** o mini mapa mostra a área em volta do jogador (sugestão: 64 × 64 tiles), com o jogador no centro e o recorte parando nas bordas do mapa. Mapas pequenos (Campos, 32 × 20) aparecem inteiros.
4. **Marcas:**
   - **o jogador** (obrigatório), com a direção para onde olha, se ficar legível;
   - **membros do grupo que o cliente já vê** (pelo `Entity` do `PartyMemberInfo` no snapshot), numa cor diferente da do jogador; morto com marca própria; passar o mouse mostra o nome.
   Monstros, NPCs e outros jogadores **não** aparecem.
5. **Mapa grande:** uma tecla (sugestão: `M`) abre o mapa inteiro da zona no centro da tela, com as mesmas marcas; a mesma tecla ou `Esc` fecha. A tecla não age enquanto a caixa de nome do grupo ou o campo de quantidade da loja estiver com o foco.
6. **Instâncias:** na Cripta, o mini mapa mostra o mapa da Cripta normalmente.
7. O cliente só exibe: nada do mini mapa decide gameplay.
8. A conta de recorte, de prender uma marca na borda (a T-393 vai usar) e de converter tile em pixel fica numa classe sem Godot, com testes no `Game.Client.Tests` (como `ShopQuantity` e `SkillDetailText`).
9. Deixe a fonte das marcas de membros num ponto só (uma lista de `MemberId`, nome, posição e vivo/morto), para a T-393 trocar a origem sem mexer no desenho.

## Critérios de aceite

- prévia **dentro do jogo** mostrada ao designer antes de fechar: o mini mapa nos Campos, na Floresta Baixa e na Cripta, e o mapa grande aberto; a escolha de cores e tamanho registrada;
- capturas em `tasks/reports/interface/mini-mapa/`;
- testes da conta do recorte e da borda passando;
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Cuidados de integração

- Não muda protocolo: depois de integrar, basta publicar um cliente novo (`export-client.ps1` e `deploy-download.ps1`).
- Mexe no `Hud.cs`: se outra tarefa de interface estiver aberta, integre uma de cada vez.

## Resultado

**Feito**, com a aprovação do designer pendente (cores e tamanho; ver Pendências).

**Mini mapa** (`Hud/MiniMapPanel.cs`, desenho em `Hud/MiniMapCanvas.cs`):
- **Onde fica:** canto superior direito, abaixo do nome da zona (`TopRightReserved`). A lista de batalha desce para baixo dele. Moldura fina de pedra do kit (`HudTheme.MemberFrame`), sem arte nova.
- **Tamanho:** 192 × 192 px na tela de 1280 × 720 do jogo. Nos mapas grandes, mostra 64 × 64 tiles a 3 px por tile. O recorte segue o jogador e para nas bordas do mapa.
- **Mapas menores que o recorte:** aparecem inteiros, na maior escala inteira que caiba (32 × 20 sai a 6 px por tile, centralizado). A Cidade (80 × 60) mostra 64 tiles na largura e a altura inteira.
- **Só no mundo aberto:** fora de uma zona (sala de encontro, jogo local), o mini mapa some.
- **Clique no mini mapa:** abre o mapa grande.

**Terreno** (`Hud/MiniMapTerrain.cs`): gerado uma vez por zona, ao entrar, com 1 pixel por tile.
- **Cor de cada tile:** a média dos pixels visíveis do tile, camada por camada como o jogo desenha, menos a de colisão. A média sai de `TiledVisual.AverageColor` e fica guardada por gid.
- **Bloqueios:** tiles bloqueados no `GridMap` do Core escurecem 45%, então a trilha se destaca da mata e o chão das paredes.
- **Portões:** retângulo dourado.
- **Arena ASCII:** sem mapa do Tiled, chão e parede saem em duas cores.

**Marcas:**
- **Você:** ponto amarelo claro.
- **Membro do grupo:** ponto azul claro; morto, cinza com um X.
- **Membro fora do recorte:** seta presa na borda, na direção dele (azul vivo, cinza morto).
- **Mouse sobre a marca:** mostra o nome ("Você", o nome do membro, "(morto)").
- **Direção do olhar:** não foi desenhada, porque o snapshot não traz para onde a unidade olha.
- **De onde vêm os membros:** os do `PartyState` na mesma zona cuja entidade está no snapshot e tem o mesmo nome. Na Cripta, outra cópia pode ter uma entidade com o mesmo id; por isso o nome também precisa bater.
- **Uma fonte só:** as marcas vêm da lista de `MapMark` que o `Main` monta a cada frame (`UpdateZoneMap`). A T-393 só troca a origem dessa lista.

**Mapa grande** (`Hud/ZoneMapWindow.cs`):
- **Tecla:** M (ação `zone_map`). M ou Esc fecha. M não age enquanto se digita no grupo ou na loja.
- **Janela:** a zona inteira no centro, numa janela de pedra e ouro com a placa "Mapa: <zona>", na maior escala inteira que caiba em 80% da tela (teto de 10 px por tile). Abre por cima do painel do chefe.

**Contas sem Godot** (`Hud/MiniMapView.cs`): recorte, escala inteira, centralização e marca presa na borda. 10 testes em `tests/Game.Client.Tests/MiniMapViewTests.cs`.

**Prévia no jogo**, contra o servidor local (`--world --dev-login --start-zone <zona>`), em `tasks/reports/interface/mini-mapa/`:
- `prancha-mini-mapa.png`: o mini mapa nos Campos da Vigília, na Cidade, na Floresta (grupo perto, membro fora do recorte e membro caído) e na Cripta;
- `floresta-grupo.png` e `floresta-mapa-grande.png`: dois clientes no mesmo grupo na Floresta Baixa, com a Ana subindo a trilha e o Beto no nascimento;
- `floresta-membro-fora.png` e `floresta-membro-caido.png`: cada um subiu por um lado da trilha. O Beto ficou a mais de 32 tiles e virou seta na borda esquerda; depois os monstros o mataram e a seta ficou cinza;
- `campos.png`, `cidade.png`, `cripta.png` e `cripta-mapa-grande.png`: um cliente sozinho.

**Ajustes provisórios só para as capturas, fora do commit:**
- `--test-invite`, `--test-accept` e `--test-map` no `Main`, para formar o grupo e abrir o mapa grande sem clicar.
- **Cripta:** o `kind` da `zone.watcher_crypt` em `data/zones.json` virou `field`, porque o servidor só aceita nascer em zona aberta (como na T-388). Por isso o painel do Vigia aparece nas capturas da Cripta.
- **Correção que essas capturas renderam:** na Cripta, o painel do chefe cobria o título do mapa grande. O mapa grande agora vai para a frente ao abrir (`MoveToFront`).

`dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

**Desvios:**
- **Branch:** criada a partir de `docs/tarefa-mini-mapa`, onde está o arquivo desta tarefa, e não de `main`.
- **Fora das áreas da tarefa, o mínimo:**
  - `Presentation/TiledVisual.cs` ganhou `AverageColor`, a cor média de um tile;
  - `Presentation/ArenaView.cs` ganhou `Visual`, para o mini mapa reaproveitar o mapa do Tiled já carregado;
  - `Input/InputActions.cs` ganhou a ação `zone_map`.
- **`.uid`:** o Godot gerou os `.uid` dos scripts novos e também os de `LoginMemory.cs` e `ShopQuantity.cs`, que faltavam na `main`. Foram todos para o commit.
- **"Campos" de 32 × 20:** o mapa citado na tarefa (`fields.tmj`) não é usado por nenhuma zona aberta. Os Campos do jogo são os da Vigília (200 × 140). O caso de mapa pequeno inteiro ficou coberto pelos testes, sem captura.

**Pendências:**
- **Aprovação do designer:** cores (você amarelo, grupo azul, morto cinza), tamanho (192 px, 64 tiles) e o escurecimento dos bloqueios.
- **Contraste na Cripta:** chão e parede são cinzas próximos. Se o designer achar fraco, dá para clarear o chão nos mapas escuros.
- **Não exercitados no jogo, só no código:** a dica com o nome ao passar o mouse, o clique no mini mapa e a tecla M. Nas capturas, o mapa grande abriu pelo mesmo método que a tecla e o clique chamam.
- **Membros distantes:** só aparecem os que o cliente já recebe (uns 40 tiles). Os distantes ficam para a T-391 e a T-393.
