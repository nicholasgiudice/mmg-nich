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
4. Testes no `Game.Client.Tests` para a troca de fonte (snapshot e mensagem) e para a lista vazia.

## Critérios de aceite

- prévia **dentro do jogo**: dois personagens no mesmo grupo, longe um do outro na Floresta Baixa, cada um vendo o outro no mini mapa (dentro e fora do recorte) e no mapa grande;
- capturas em `tasks/reports/interface/mini-mapa/`;
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Cuidados de integração

- **Branch:** crie a `task/T-393-...` a partir da branch da T-391, com a T-392 já em `main` (faça merge da `main` na branch se precisar).
- A T-391 sobe `ProtocolVersion` para 13: integre a T-391 e a T-393 **juntas** e, na mesma rodada, rode o `deploy-dev.sh` e publique o cliente novo.

## Resultado

_(preencher ao concluir)_
