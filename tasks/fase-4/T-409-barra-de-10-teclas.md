# T-409 — Cliente: barra de atalhos com 10 teclas

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** —
- **Libera:** T-410
- **Áreas que pode alterar:** `client/scripts/Input/` (`InputActions`, `HotbarLayout`), `client/scripts/Hud/` (barra, `HotbarStore`, `AbilitySlot`), `client/project.godot` (ações de entrada), `tests/Game.Client.Tests/`, `tasks/reports/interface/`
- **Branch:** sai de `trilha/segundas-classes` e é integrada nela, não na `main` (seção 6f de `tasks/README.md`).

## Contexto

Design em `design/segunda-classe-v0.1.md`, seção 1: com a segunda classe, o personagem tem até 10 habilidades com tecla (5 da classe base e 5 da segunda classe, menos as passivas). Decisão do designer: **barra de 10 teclas** (1 a 9 e 0), mais o ataque básico e a poção, que já têm tecla própria.

Hoje a barra tem **6 espaços** (`HotbarLayout.Size = 6`, `InputActions.AbilitySlots` com `ability_1` a `ability_6`), que o jogador organiza arrastando habilidades e consumíveis (T-081). A organização fica guardada por personagem (`HotbarStore`, `user://hotbar.cfg`).

Esta tarefa não depende da segunda classe: com 10 espaços, um personagem de hoje só usa os primeiros.

## Requisitos

1. `HotbarLayout.Size` = **10**; ações `ability_1` a `ability_10`, ligadas às teclas **1 a 9 e 0**. Quem tinha remapeado teclas não perde o que já tinha.
2. Uma barra guardada com 6 espaços abre com os 6 nos mesmos lugares e os outros 4 vazios.
3. A barra continua cabendo na tela na resolução mínima do jogo, sem cobrir o mini mapa, a lista de batalha nem os quadros do grupo. Se não couber em uma linha, propor duas linhas (5 + 5) na prévia.
4. O número da tecla aparece em cada espaço (o "0" no décimo).
5. **Prévia no jogo:** capturas da barra com 6 e com 10 habilidades em `tasks/reports/interface/barra-10-teclas/`, para o designer aprovar antes de fechar a tarefa.

## Critérios de aceite

- teste de cliente: um layout de 6 espaços salvo antes abre certo no de 10;
- as teclas 7, 8, 9 e 0 usam o que está nos espaços 7 a 10 (teste ou captura);
- capturas aprovadas pelo designer;
- `./scripts/check.sh --fast` passa no Windows.

## Resultado

_(preencher ao concluir)_
