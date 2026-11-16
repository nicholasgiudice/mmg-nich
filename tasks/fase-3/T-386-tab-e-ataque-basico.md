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

**Feito.**

**Causa: a hipótese do clique do mouse, mais um detalhe.**
- O clique e o **clique mantido** usavam o monstro sob o cursor e miravam no cursor. Isso acontecia mesmo com um alvo escolhido pelo Tab.
- O ataque básico do guerreiro é um arco corpo a corpo (`melee_arc`), que sai na direção da mira. Com o cursor longe do alvo, o golpe ia para o lado errado e não acertava ninguém.
- **O detalhe:** quem segura o botão manda o golpe assim que ele fica pronto, sempre antes do ataque automático (o `basicSent` desliga o automático naquele frame). Por isso o automático, que mira certo, nunca chegava a sair.
- **Por que "às vezes":** só acontecia com o cursor longe do alvo. Arqueiro, mago e clérigo também mandavam o clique para o cursor, mas como o ataque deles é de alvo único, o servidor ainda acertava o alvo selecionado. O guerreiro errava.
- **Ataque automático:** só sai com o alvo no alcance, e antes isso era mudo. Ganhou o aviso, descrito mais abaixo.
- **As outras duas hipóteses** (o alvo sumir e a lista fora de sincronia) não apareceram nos roteiros.

**Reprodução** (o log do roteiro imprime cada golpe):
- **Cenário:** `--encounter=encounter.three_wolves --class=class.warrior --demo-aim=1;18 --demo-input=wait:3.5,target_next,press:attack,wait:5`, com o lobo do Tab colado no guerreiro.
- **Antes:** a mira ia para `<1, 18>`, o ponto do cursor, e cada golpe atingia 0 alvos (`executed warrior.sword_strike targets=0`).
- **Depois:** a mira vai para o lobo, `<6,8, 9,8>`, e cada golpe atinge 2 alvos.
- **Contra o servidor local** (`--world --dev-login --start-zone zone.vigil_fields`): Tab no rato, andar até ele, cursor parado em `<1, 1>` e botão mantido. Todo golpe acerta o rato (`targets=1`).

**Correção** (`client/scripts/Input/PlayerInput.cs`):
- O clique, o clique mantido, o Espaço e o automático passam pelo mesmo caminho (`preferSelected`): com alvo selecionado, vivo e visível, o golpe vai nele e mira nele. Sem alvo, o clique mira no cursor, como antes.
- Clicar em outra unidade troca o alvo, como antes.
- **Aviso de alcance:** o "Fora de alcance", o mesmo texto da recusa de habilidades, aparece sobre o jogador ao escolher com Tab ou Shift+Tab um inimigo longe demais para o ataque básico. Aparece também ao apertar o ataque (clique ou Espaço) com o alvo longe.
  - Ele não se repete a cada frame enquanto o jogador anda até o alvo.
  - Usa a mesma trava de repetição das recusas do servidor, então o arqueiro não vê o texto duas vezes.
  - O aperto continua mandando a intenção: quem decide é o servidor.

**Testes:** são 7, em `tests/Game.Client.Tests/PlayerInputTests.cs`. Cobrem o clique, o clique mantido, o Espaço e o automático no alvo do Tab com o cursor longe; a troca de alvo pelo clique; o clique sem alvo mirando no cursor; e o aviso de alcance, que não aparece com o alvo perto e não se repete a cada frame.
- Para isso, o `PlayerInput.cs` e o `InputActions.cs` são compilados no projeto de testes por link, como outros arquivos do cliente.
- No `PlayerInput`, as duas partes que usam Godot ficaram sob `#if GODOT`: as teclas padrão (`Godot.Input`) e o filtro de poção de cura da tecla Q, que depende do `ItemText`.
- O Godot.NET.Sdk define `GODOT`. Conferido rodando o jogo depois da mudança: o roteiro acima ataca normalmente.

**Desvios:**
- **Branch:** criada a partir de `docs/correcoes-playtest-v0.3`, onde está o arquivo desta tarefa, e não de `main`.
- **Fora das áreas da tarefa, o mínimo necessário:**
  - `client/scripts/Presentation/WorldView.cs`: o aviso de recusa virou o método público `ShowRejection`, para o cliente usar a mesma trava de repetição;
  - `tests/Game.Client.Tests/Game.Client.Tests.csproj`: os dois links novos.
- **`Main.cs`:** ganhou o argumento de depuração `--demo-aim=x;y` (tiles). Ele deixa a mira do roteiro parada num ponto do mapa, em vez de mirar sozinha no inimigo mais próximo. Sem ele não dava para reproduzir o cursor longe, porque o `click:x;y` do roteiro não move a mira no mundo.

**Pendências:**
- **Barra de atalhos:** a casa M1 só fica vermelha de "fora de alcance" para ataques de alvo único (`Hud.IsOutOfRange`). Não muda para o arco do guerreiro. Seria uma mudança no `Hud.cs`, que a T-383, a T-382 e a T-384 também alteram; ficou de fora.
