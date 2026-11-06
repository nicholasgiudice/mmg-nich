# T-312 — Cliente: lista de batalha

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** —
- **Libera:** —
- **Áreas que pode alterar:** `client/` (inclusive o Input Map em `project.godot`)

## Contexto

O playtest da fase 2 (`tasks/reports/playtest-v0.2.md`, P1) mostrou que é difícil selecionar, clicando no mapa, o monstro que você quer atacar enquanto ele se move. O pedido é uma **lista de batalha**: um painel com todos os monstros que estão na tela, onde se clica para escolher o alvo.

O cliente já recebe do servidor as entidades dentro da área de interesse. A lista é só outra forma de escolher o alvo e usa a mesma intenção de alvo que já existe (`PlayerInput`).

## Requisitos

1. **Painel "Batalha"** (canto direito, abaixo do nome da zona), com uma linha por monstro **visível na tela** (dentro da área da câmera), vivo. Cada linha mostra:
   - nome e nível;
   - barra de vida;
   - uma marca quando o monstro está mirando o próprio jogador. Esse alvo já vem replicado, porque o cliente o mostra hoje. Se não vier, deixe a marca de fora e registre no Resultado.
2. Ordem: o alvo atual primeiro, depois por distância. A ordem não pode "pular" a cada frame: reordenar no máximo 4 vezes por segundo, ou só quando a diferença de distância for maior que 1 tile.
3. **Clique na linha** seleciona o monstro como alvo, como o clique no mapa. Passar o mouse sobre a linha destaca o monstro no mapa (anel ou contorno).
4. **Tecla para trocar de alvo:** Tab avança para o próximo da lista e Shift+Tab volta. Usar o Input Map e `InputActions` (como a T-081 e a T-085).
5. Elites e chefes têm destaque (cor do nome ou ícone). Monstros de outro grupo de aggro não precisam de diferença.
6. O painel pode ser escondido e mostrado com uma tecla (proposta: B), e o estado fica gravado em `user://settings.cfg`. Com mais de 10 monstros, o painel rola.
7. Com a lista vazia, mostrar só o título (ou esconder o corpo do painel).

## Critérios de aceite

- contra o servidor local (`--world --dev-login`), nos Campos: a lista mostra os monstros da tela, o clique numa linha seleciona e o ataque básico vai nesse alvo; Tab passa pelos monstros;
- capturas de tela com a lista ao lado de uma matilha em movimento;
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Resultado

**Feito** (Windows, 2026-11-05). `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa. Verificado contra o servidor local (`--world --dev-login --start-zone zone.vigil_fields`), nos Campos, com capturas em `tasks/reports/T-312/`.

**O que mudou**
- `Hud/BattleList.cs` (novo, sem Godot, com 7 testes em `tests/Game.Client.Tests/BattleListTests.cs`):
  - ordem: alvo atual primeiro, depois por distância;
  - a ordem por distância é refeita no máximo 4 vezes por segundo, e dois monstros só trocam de lugar quando a diferença passa de 1 tile (as duas regras do requisito 2);
  - Tab e Shift+Tab andam pela ordem por distância, sem a promoção do alvo; senão o Tab alternaria entre os dois primeiros.
- `Hud/BattleListPanel.cs` (novo): painel "Batalha (B) · n" no canto direito, abaixo do nome da zona.
  - Cada linha tem nome, nível e barra de vida.
  - Elite tem nome dourado com ◆; chefe, laranja com ★.
  - A linha do alvo fica destacada.
  - Rola com mais de 10 linhas; com a lista vazia, mostra só o título.
- `Hud.cs`: monta os candidatos e o painel. Candidatos são os monstros vivos de outra facção dentro do retângulo da câmera (`CameraRig.VisibleWorld`). A lista é calculada mesmo com o painel escondido, porque o Tab usa a ordem dela.
- Teclas novas:
  - **Shift+Tab** (`target_prev`) volta um alvo; no `PlayerInput`, ela é lida antes do Tab, porque o Shift+Tab também dispara a ação do Tab;
  - **B** (`battle_list`) mostra e esconde a lista e grava `[hud] battle_list` em `user://settings.cfg` (`BattleListSettings`).
- `PlayerInput`: `Select(id)` para o clique na linha (o mesmo caminho do clique no mapa) e `TabCycle`, que o `Main` liga à lista. Sem monstros na lista, o Tab volta ao comportamento antigo (inimigo mais perto num raio de 15 tiles).
- `WorldView` e `EntityView`: anel branco no monstro sob o mouse na lista.
- `ZoneScreen.CornerBottom`: onde termina o nome da zona, para a lista ficar abaixo dele.

**Decisões e desvios**
- **O alvo dos monstros não é replicado** (o requisito supunha que sim). A marca "!" é inferida no cliente:
  - eventos com o jogador como alvo (`AbilityStarted`, `AbilityExecuted` e `DamageDealt`) marcam o monstro por 3 s;
  - a conjuração replicada (`AbilityState.Cast.Target`) marca enquanto durar.
  - Um monstro que persegue sem ter atacado ainda não aparece marcado. Para marcar desde a perseguição, o servidor precisaria replicar o alvo da IA (mudança de Core e protocolo, fora desta tarefa).
- O clique na linha leva o monstro para o topo (regra "alvo atual primeiro"), então outra linha passa a ficar sob o mouse.

**Verificação** (capturas em `tasks/reports/T-312/`)
- `lista-tab.png`: depois de Tab, Tab, Tab e Shift+Tab, o alvo aparece no topo e no quadro do alvo; a lista tem 15 monstros e rola.
- `lista-clique.png`: depois do clique na 4ª linha, ela vira o alvo; o mouse sobre outra linha desenha o anel branco no monstro.
- `lista-escondida.png`: a tecla B esconde a lista; o `settings.cfg` guardou `false` e, depois de outro B, `true`.
- A marca "!": numa execução com log, só os 7 monstros que atacaram ficaram marcados; os 3 lobos distantes, não.
- O ataque básico automático no alvo escolhido pela lista usa o mesmo `SelectedTarget` do clique no mapa (o Guerreiro atacou o rato selecionado nas capturas).
- **O designer testou com o mouse em 2026-11-06 e aprovou.**
- Não verificado: a lista vazia em jogo (os Campos sempre têm monstros na tela; a regra está no código e no teste `EmptyListHasNoNextTarget`).

**Observação para a T-301:** nos testes, todos os monstros dos Campos vinham juntos para o jogador ao nascer. É o aggro em cadeia que a T-301 corrige; por isso quase todas as linhas aparecem com "!".
