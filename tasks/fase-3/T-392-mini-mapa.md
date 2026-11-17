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

_(preencher ao concluir)_
