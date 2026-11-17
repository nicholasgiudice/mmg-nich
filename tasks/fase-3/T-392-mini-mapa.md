# T-392 — Cliente: mini mapa com a posição do grupo

- **Ambiente:** Windows
- **Modelo:** Humano + Médio
- **Depende de:** T-391
- **Libera:** —
- **Áreas que pode alterar:** `client/scripts/Hud/` (painel novo e posição dele no `Hud.cs`), `client/scripts/Authority/` (receber a mensagem da T-391), `client/scripts/Main.cs` (ligar o painel), `client/project.godot` (tecla do mapa), `client/assets/art/ui/`, `CREDITS.md`, `tests/Game.Client.Tests/`, `tasks/reports/interface/`

## Contexto

Pedido do designer durante o playtest da fase 3 (T-380), em 2026-11-17: criar um mini mapa que inclua a posição dos membros do grupo. Os mapas novos são grandes (Floresta Baixa 200 × 160, Ruínas 180 × 140, Cripta 140 × 76) e o grupo se perde neles.

A T-391 manda as posições dos membros do grupo na mesma zona. O terreno sai do mesmo `.tmj` que o cliente já desenha (`client/scripts/Presentation/TiledVisual.cs`) e que o Core usa para colisão (`src/Game.Data/TiledMapReader.cs`).

## Requisitos

1. **Painel do mini mapa** no canto superior direito. A lista de batalha desce para baixo dele (o `Hud` já tem o `TopRightReserved`, que hoje o painel de zona usa). Moldura do kit pedra e ouro. Não cobre o painel do chefe (centro, no alto) nem o inventário.
2. **Terreno:** gerado uma vez por zona, ao entrar, a partir do `.tmj`. Chão andável e bloqueios (paredes, árvores, água) com cores diferentes, legíveis em escala pequena; os portões de troca de zona marcados. A escolha mais simples é 1 pixel por tile a partir da grade de colisão; cor média do tile do tileset é bem-vinda se ficar mais bonita.
3. **Recorte:** o mini mapa mostra a área em volta do jogador (sugestão: 64 × 64 tiles), com o jogador no centro e o recorte parando nas bordas do mapa. Mapas pequenos (Campos, 32 × 20) aparecem inteiros.
4. **Marcas:**
   - o jogador, com a direção para onde olha, se ficar legível;
   - cada membro do grupo na mesma zona, numa cor diferente da do jogador; morto com marca própria;
   - membro fora do recorte fica preso na borda do mini mapa, apontando a direção;
   - passar o mouse sobre a marca mostra o nome do membro.
   Monstros, NPCs e outros jogadores **não** aparecem (fora desta tarefa).
5. **Mapa grande:** uma tecla (sugestão: `M`) abre o mapa inteiro da zona no centro da tela, com as mesmas marcas; a mesma tecla ou `Esc` fecha. A tecla não age enquanto a caixa de nome do grupo ou o campo de quantidade da loja estiver com o foco.
6. **Instâncias:** na Cripta, o mini mapa mostra a cópia em que o jogador está (as posições da T-391 já vêm só da mesma cópia).
7. O cliente só exibe: nada do mini mapa decide gameplay.
8. A conta de recorte, de prender na borda e de converter tile em pixel fica numa classe sem Godot, com testes no `Game.Client.Tests` (como `ShopQuantity` e `SkillDetailText`).

## Critérios de aceite

- prévia **dentro do jogo** mostrada ao designer antes de fechar: o mini mapa nos Campos, na Floresta Baixa e na Cripta, com um membro do grupo dentro e outro fora do recorte; a escolha de cores e tamanho registrada;
- capturas em `tasks/reports/interface/mini-mapa/`;
- testes da conta do recorte e da borda passando;
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Cuidados de integração

- **Branch:** crie a `task/T-392-mini-mapa` a partir da branch da T-391 (`task/T-391-...`), e não da `main`. A T-391 não entra na `main` sozinha.
- Depende do `ProtocolVersion` 13 da T-391: integre e publique as duas juntas (servidor com `deploy-dev.sh` e cliente novo com `export-client.ps1` e `deploy-download.ps1`).
- Mexe no `Hud.cs`: se outra tarefa de interface estiver aberta, integre uma de cada vez.

## Resultado

_(preencher ao concluir)_
