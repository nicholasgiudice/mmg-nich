# T-386 — Alvo do Tab às vezes não recebe o ataque básico

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** —
- **Libera:** —
- **Áreas que pode alterar:** `client/scripts/Input/`, `client/scripts/Hud/BattleList.cs`, `client/scripts/Main.cs`, `tests/Game.Client.Tests/`

## Contexto

Relato do designer no playtest da fase 3 (T-380), em 2026-11-16: às vezes, depois de escolher um monstro com Tab, o ataque básico não sai.

O caminho está em `PlayerInput.Update`. Hipóteses a conferir (nenhuma confirmada):
- **Clique do mouse:** o clique esquerdo usa o monstro **sob o cursor**, não o alvo do Tab (`UseAbility` sem `preferSelected`). Com o cursor fora do monstro, o golpe vai para onde o mouse aponta e erra. Só o Espaço e o ataque automático preferem o alvo selecionado.
- **Ataque automático (T-033):** só dispara com o alvo **no alcance** (`InReach`). Se o Tab escolhe um monstro longe, nada acontece até o jogador andar, e o jogador pode achar que travou.
- **Alvo some:** o `SelectedTarget` é limpo quando o monstro sai do snapshot (área de interesse) ou morre.
- **Lista e Tab fora de sincronia:** o `TabCycle` usa a ordem da lista de batalha; se a lista ainda não foi recalculada, pode escolher um id que já não está na tela.

## Requisitos

1. Reproduzir contra o servidor local (`--world --dev-login --start-zone zone.vigil_fields`), com `--demo-input` e com o mouse, e registrar no Resultado qual hipótese (ou outra) é a causa.
2. Corrigir a causa. Regra desejada: **com um alvo escolhido pelo Tab, o ataque básico (clique, Espaço ou automático) vai nele**, enquanto ele estiver vivo e visível. O clique em outro monstro troca o alvo, como hoje.
3. Fora do alcance, o jogo precisa deixar claro por que o ataque não saiu (por exemplo, o aviso "fora de alcance" que já existe para habilidades) em vez de ficar mudo.
4. Teste no `Game.Client.Tests` para a regra do item 2.

## Critérios de aceite

- roteiro que reproduzia o problema agora ataca o alvo do Tab;
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
