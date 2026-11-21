# T-409 — Cliente: barra de atalhos com 10 teclas

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** —
- **Libera:** T-410
- **Áreas que pode alterar:** `client/scripts/Input/` (`InputActions`, `HotbarLayout`), `client/scripts/Hud/` (barra, `HotbarStore`, `AbilitySlot`), `client/project.godot` (ações de entrada), `tests/Game.Client.Tests/`, `tasks/reports/interface/`

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

**Feito** (Windows, 2026-11-21), na branch `task/T-409-barra-de-10-teclas`. **Falta a aprovação do designer** da captura.

- **10 espaços:** `HotbarLayout.Size` = 10 e `HotbarLayout.KeyLabel` ("1" a "9" e "0" no décimo). Ações `ability_7` a `ability_10` no `project.godot`, nas teclas físicas 7, 8, 9 e 0 (nenhuma outra ação usava essas teclas). O `PlayerInput` já percorria `InputActions.AbilitySlots`, então as teclas novas usam os espaços 7 a 10 sem outra mudança.
- **Barra salva antes:** o formato guardado não mudou (ids separados por `|`). Uma barra de 6 espaços abre com os 6 nos mesmos lugares e os 4 novos vazios (teste).
- **Duas linhas (req. 3):** numa linha só, a barra teria 12 quadros (M1, 1–0 e Q), 668 px. Na resolução mínima (1280 × 720), o espaço livre entre a ajuda (410 px) e o log de combate (400 px) é de 454 px, e a barra cobriria os dois. Por isso a barra ficou em **duas linhas**, centralizadas: embaixo M1, 1–5 e Q (388 px, cabe); em cima 6–0, os espaços da segunda classe. `Hud._abilityBar` virou um `VBoxContainer` com duas linhas, e o preenchimento de cada linha saiu para `FillBarRow`.
- **Remapeamento de teclas:** o cliente não tem tela de remapear teclas, então não há nada a preservar.

**Testes:** `HotbarLayoutTests` atualizados para 10 espaços, mais três novos (barra de 6 salva antes, rótulos de 1 a 0, os últimos espaços aceitam habilidade). 107 testes de cliente passando; `./scripts/check.sh --fast` passa; o projeto do cliente compila sem avisos.

**Prévia:** `tasks/reports/interface/barra-10-teclas/guerreiro-1920.png`, Guerreiro nível 1 num encontro local (`--class=class.warrior --encounter=encounter.three_wolves`). A linha de cima aparece vazia porque o Guerreiro de hoje só tem 5 habilidades.

**Desvios e pendências**
- A captura em 1280 × 720 não saiu: o `--resolution` foi ignorado e a janela abriu em 1920 × 1080. A conta de largura acima é que mostra que a linha de baixo cabe no 1280.
- Não houve captura com os 10 espaços cheios: isso pede arrastar habilidades na tela, e o Guerreiro de hoje só tem 5. Fica para a T-410, que traz as habilidades da segunda classe.
- A linha de cima aparece sempre, mesmo antes da segunda classe (o jogador pode pôr poções ou repetir a organização ali). Se o designer preferir, ela pode aparecer só depois da escolha da segunda classe.
