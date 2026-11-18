# T-393 — Cliente: membros distantes do grupo no mini mapa

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** T-391, T-392
- **Libera:** —
- **Áreas que pode alterar:** `client/scripts/Hud/` (mini mapa da T-392), `client/scripts/Authority/` (receber a mensagem da T-391), `client/scripts/Main.cs`, `tests/Game.Client.Tests/`, `tasks/reports/interface/`

## Contexto

Segunda parte do pedido do designer de 2026-11-17. A T-392 fez o mini mapa com o jogador e os membros do grupo que estão perto. A T-391 passou a mandar a posição de todos os membros na mesma zona e na mesma cópia de instância, 2 vezes por segundo. Esta tarefa liga uma coisa na outra.

## Requisitos

1. Receber a mensagem da T-391 no cliente e usá-la como fonte das marcas de membros do mini mapa e do mapa grande, no lugar do snapshot. Quando o membro está no snapshot, a posição do snapshot (mais fluida) pode continuar valendo.
2. Membro fora do recorte do mini mapa fica preso na borda, apontando a direção (a conta já existe da T-392).
3. A lista vazia da T-391 limpa as marcas de membros.
4. **Só o jogador e os membros do grupo aparecem no mini mapa e no mapa grande** (decisão do humano, 2026-11-18). O snapshot traz todos os jogadores da zona (o `InterestGrid` manda toda a facção `Player`), mas jogador de fora do grupo nunca vira marca. Sem grupo, só a marca do próprio jogador.
5. Testes no `Game.Client.Tests` para a troca de fonte (snapshot e mensagem), para a lista vazia e para jogador de fora do grupo não virar marca.

## Critérios de aceite

- prévia **dentro do jogo**: dois personagens no mesmo grupo, longe um do outro na Floresta Baixa, cada um vendo o outro no mini mapa (dentro e fora do recorte) e no mapa grande;
- capturas em `tasks/reports/interface/mini-mapa/`;
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Cuidados de integração

- **Branch:** crie a `task/T-393-...` a partir da branch da T-391, com a T-392 já em `main` (faça merge da `main` na branch se precisar).
- A T-391 sobe `ProtocolVersion` para 13: integre a T-391 e a T-393 **juntas** e, na mesma rodada, rode o `deploy-dev.sh` e publique o cliente novo.

## Resultado

**Feito** em 2026-11-17, na branch `task/T-393-mini-mapa-membros-distantes`, que sai da `task/T-391-posicao-do-grupo` (que já contém a `main` com a T-392). **As duas entram juntas na `main`**, e na mesma rodada é preciso rodar o `deploy-dev.sh` e publicar o cliente novo (protocolo 13).

**Fonte das marcas** (`Hud/PartyModel.cs`, sem Godot):
- **Mensagem:** `PartyModel.ApplyPositions(PartyPositions)` guarda a última posição de cada membro pelo `MemberId`. Cada lista substitui a anterior inteira, e a lista vazia limpa tudo.
- **Quem vira marca:** `PartyModel.MapMarks(drawn)` monta as marcas do mini mapa e do mapa grande. Só entra membro do grupo (`Others`), na mesma zona pelo `PartyState`, que esteja na última `PartyPositions`.
  - Jogador de fora do grupo nunca vira marca, mesmo estando no snapshot ou numa linha da mensagem.
  - Sem grupo, só a marca do próprio jogador.
- **Posição:** a do snapshot quando o membro está nele (`drawn`, mais fluida); senão, a da mensagem (2 por segundo).
- **Quando as posições se apagam:**
  - lista vazia;
  - `PartyState` sem grupo;
  - `Welcome` novo (troca de zona ou renascer: as posições da zona anterior não valem mais);
  - `Reset`.
- **Membro que trocou de zona:** some na hora pelo `ZoneId` do `PartyState`, sem esperar a próxima lista.

**Ligação:**
- `Authority/NetAuthority.cs`: recebe a `PartyPositions` (tag 22) e limpa as posições no `OnWelcome`.
- `Main.UpdateZoneMap` passa a usar `Party.MapMarks`. A conta do snapshot continua a mesma da T-392, inclusive o nome que precisa bater, por causa das cópias da Cripta.
- A seta na borda para membro fora do recorte é a da T-392 (`MiniMapView.PinToEdge`), sem mudança.

**Testes:** 11 novos em `tests/Game.Client.Tests/PartyModelTests.cs` (`PartyMapMarksTests`):
- posição da mensagem;
- snapshot vale mais que a mensagem;
- troca de fonte quadro a quadro;
- membro só no snapshot, antes da mensagem, não vira marca;
- lista vazia limpa;
- lista nova substitui a anterior;
- jogador de fora do grupo nunca vira marca;
- sem grupo não há marcas;
- sair do grupo limpa;
- membro que trocou de zona some;
- troca de zona limpa, e membro morto continua marcado como morto.

**Prévia no jogo**, contra o servidor local (`--world --dev-login --start-zone zone.lower_forest`), com dois clientes no mesmo grupo na Floresta Baixa. O grupo foi formado e a caminhada feita pelo roteiro `--demo-input` (`invite:Beto`, `accept`, `press:move_up`…), sem flag nova no commit.
- **O que acontece:** a Ana sobe a trilha e segue para leste até (139, 124), a 40 tiles do Beto, que fica no nascimento (100, 154). Lá as aranhas a matam.
- **Beto:** rodou com uma flag provisória (fora do commit) que ignora o snapshot, então as marcas dele vêm **só** da `PartyPositions`.
- **Ana:** rodou com o código normal (snapshot quando há).

Capturas em `tasks/reports/interface/mini-mapa/`:
- `prancha-membros-distantes.png`: o mini mapa dos dois ao longo do tempo:
  - Ana perto (ponto azul), fora do recorte (seta azul na borda), caída (seta cinza) e desconectada (a lista vazia limpou a marca);
  - do lado da Ana, o Beto perto e depois como seta na borda.
- `distantes-beto-perto.png`, `distantes-beto-fora.png`, `distantes-ana-fora.png`: tela inteira, dentro e fora do recorte.
- `distantes-ana-mapa-grande.png` e `distantes-beto-mapa-grande.png`: o mapa grande (M) de cada um, com o outro no lugar certo. No do Beto, a Ana caída aparece com o X cinza, onde caiu.
- `distantes-beto-lista-vazia.png`: depois que a Ana fecha o jogo, o servidor manda a lista vazia, e a marca dela some no mini mapa e no mapa grande do Beto.

`dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

**Decisões:**
- **A mensagem decide quem aparece; o snapshot só dá a posição.** Assim a lista vazia limpa as marcas mesmo com o membro ainda no snapshot (o `InterestGrid` manda todo jogador da zona). Quem acabou de entrar no grupo ou na zona aparece em até 10 ticks (0,5 s), na primeira `PartyPositions`.
- **Hoje a posição que se vê vem quase sempre do snapshot,** porque todo jogador da zona está nele (ver a T-391). A mensagem passa a valer sozinha se o interesse deixar de mandar jogadores distantes.

**Desvios:**
- **Branch:** sai da `task/T-391-posicao-do-grupo`, como pede a tarefa.
- **Trabalho feito numa worktree** (`.claude/worktrees/t393`), porque a pasta principal tinha alterações não commitadas da T-073.
- **Ajustes provisórios, fora do commit:** `--test-map-message`, que força a posição da mensagem, e um log `[T393]` das posições e marcas.

**Pendências:**
- integrar a T-391 e a T-393 juntas na `main`;
- `deploy-dev.sh`;
- publicar o cliente novo.
