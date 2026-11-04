# T-081 — Tela de habilidades com ícones e barra de atalhos 1–6

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** T-080 (dá para começar com ícones provisórios do `IconCatalog`)
- **Libera:** —
- **Áreas que pode alterar:** `client/scripts/Hud/` (`SkillPanel`, `AbilitySlot`, `AbilityText`, `Hud`, `HudTheme` e arquivos novos), `client/scripts/Input/` (`InputActions`, `PlayerInput`), `client/project.godot` (Input Map), `client/scenes/`

## Contexto

Hoje a barra de habilidades tem 5 espaços fixos (`InputActions.AbilitySlots`, teclas 1–5) na ordem de `classes.json`, e a poção fica na tecla Q (`consumable_1`). A tela de habilidades (tecla K, `SkillPanel`) é uma lista de texto com o botão + para subir o NH.

O designer quer:
- **teclas 1, 2, 3, 4, 5 e 6**, com o **jogador escolhendo o que vai em cada uma** (qualquer habilidade aprendida, o ataque básico ou a poção);
- uma **tela de habilidades com ícones**, no espírito de Tibia e Ragnarok.

O uso de habilidade já vai ao servidor pelo id (`UseAbilityIntent(Entity, AbilityId, ...)`). A barra é, portanto, **só do cliente**: nenhuma mudança no Core, no protocolo ou no servidor.

## Requisitos

1. **Barra de atalhos com 6 espaços** (`ability_1` … `ability_6` no Input Map, teclas 1–6). Cada espaço mostra o ícone, a tecla, a recarga (escurecendo em sentido horário, com os segundos), o custo de Mana e o estado "sem Mana", "fora de alcance" ou "não aprendida", como o `AbilitySlot` faz hoje.
2. **Atribuir:** arrastar o ícone da tela de habilidades (ou da poção) para um espaço da barra. Arrastar de um espaço para outro troca os dois. Arrastar para fora da barra (ou clicar com o botão direito) esvazia o espaço. Habilidade com NH 0 (não aprendida) não vai para a barra.
3. **Padrão:** sem configuração salva, a barra vem com as 5 habilidades na ordem de `classes.json` em 1–5 e a poção em 6. A tecla **Q continua** usando a poção (decisão simples; o designer pode tirá-la depois).
4. **Salvar:** a barra é guardada por personagem no `user://hotbar.cfg`, com o id do personagem como chave e os ids das habilidades como valores. Assim a configuração não se perde ao trocar de personagem e não depende da posição na lista de `classes.json`. Id que deixou de existir nos dados vira espaço vazio.
5. **Tela de habilidades (K):** grade de ícones da classe, com o ataque básico e a assinatura destacados. Embaixo de cada ícone ficam os pontos do NH (●●○○○). O botão + aparece no ícone quando há ponto e nível para gastar, com a mesma regra de hoje (`SkillPlan`). Passar o mouse mostra a dica: nome, NH atual, custo, recarga, alcance, efeitos com os valores do personagem e a prévia do próximo NH. O texto vem do `AbilityText`, que já calcula isso. Mostre também os pontos de habilidade livres e o nível pedido para o próximo NH.
6. **Menu de cenários (offline):** a escolha de NH da T-064 continua funcionando; a barra nova também vale lá.
7. A intensidade visual segue o `HudTheme`. Nada de valor de jogo fixo no código: custos, recargas e NH vêm dos dados ou do snapshot.

## Critérios de aceite

- com um personagem online: colocar Golpe Pesado no 6 e Provocação no 1, usar as duas pelas teclas, fechar e abrir o jogo e encontrar a barra igual;
- arrastar entre espaços troca as habilidades; tirar da barra esvazia o espaço;
- a tela K mostra os ícones e deixa subir NH como antes;
- capturas de tela da barra e da tela K, antes e depois, no Resultado;
- `./scripts/check.sh` passa.

## Resultado

**Feito** (Windows, 2026-11-03). `dotnet build client/` sem avisos e `./scripts/check.sh` passa. Branch feita sobre a `task/T-082-tela-de-atributos` (integre T-085 e T-082 antes).

**Barra 1–6** (`Input/HotbarLayout.cs`, `Hud/HotbarStore.cs`, `Hud/AbilitySlot.cs`, `Hud/Hud.cs`)
- `ability_6` (tecla 6) no Input Map; `InputActions.AbilitySlots` com 6 ações.
- `HotbarLayout` (sem Godot): o id em cada tecla. Um id fica em um espaço só; pôr num espaço tira dos outros. Padrão: as 5 habilidades de `classes.json` em 1–5 e o primeiro consumível em 6. Id que sumiu dos dados vira espaço vazio.
- **Barra no HUD:** M1 (ataque básico, fixo), 1–6 do jogador e Q (poção) só quando a poção não está em 1–6. Espaço vazio mostra a tecla. Habilidade não aprendida fica escura, com "não aprendida", e a tecla não manda nada. Recarga, custo, sem Mana e fora de alcance como antes.
- **Arrastar:** do ícone da tela K para um espaço põe a habilidade nele. De um espaço para outro, troca os dois. Para fora da barra, ou com o botão direito, esvazia o espaço. O destino fica com a borda dourada durante o arraste. M1 e Q não aceitam.
- **Uso:** `PlayerInput.Hotbar` lê a barra (a mesma instância do HUD). Habilidade de área continua com a prévia ao segurar. A poção numa tecla só sai no aperto. **Q continua** usando a poção.
- **Salvar:** `user://hotbar.cfg`, seção `hotbar`, por personagem: `char-<id da conta>` online com conta, `dev-<host>-<porta>-<nome>` no login de desenvolvimento e `local-<classe>` no menu de cenários. O valor são os ids separados por `|`.

**Tela K** (`Hud/SkillPanel.cs`, `Hud/SkillIcon.cs`)
- Grade de ícones (4 colunas): ataque básico (borda azul, "ataque básico"), as 5 habilidades (assinatura com borda dourada) e a poção (borda verde, cargas).
- Embaixo de cada ícone, os pontos do NH (●○○○○) e "NH II: nível 3" (laranja se o nível ainda não basta). Não aprendida fica esmaecida e não arrasta.
- **+** no canto do ícone quando há ponto e nível (mesma regra de antes, `SkillPlan.RaiseBlocker`).
- **Dica:** nome, NH atual de máximo, alcance (`AbilityText.Reach`, novo), custo, recarga e efeitos com os valores do personagem, a prévia do próximo NH e o motivo quando não dá para subir.
- Topo: pontos de habilidade livres e "Usados 2 de 3 (nível 3)". Redistribuir no rodapé, à direita.

**Menu de cenários (offline):** a barra vale lá também (chave por classe), e a escolha de NH da T-064 não mudou. Testado com `--ranks=focado`: teclas 1, 2 e 6 usaram Golpe Pesado, Golpe Circular e a poção.

**Verificação online** (servidor local de teste, como na T-082):
- Guerreiro sobe para o nível 3 e aprende Golpe Pesado e Provocação. Golpe Pesado foi arrastado da tela K para o 6 e Provocação para o 1. Tecla 6: `started warrior.heavy_strike`; tecla 1: `started warrior.taunt`.
- Jogo fechado e aberto com o mesmo personagem: a barra voltou igual (`t081-bar-reopen.png`).
- Arrastar 1→2 trocou os dois; botão direito no 6 esvaziou; arrastar o 2 para o meio da tela esvaziou. `hotbar.cfg` ficou `warrior.whirlwind||warrior.firm_guard||warrior.living_wall|`.

**Testes:** `tests/Game.Client.Tests/HotbarLayoutTests.cs` (padrão, carregar com id que sumiu e repetido, mover, trocar, esvaziar, guardar e ler). O `HotbarLayout.cs` entra no projeto de teste por link.

**Capturas** em `tasks/reports/interface/`: `t081-before.png` (tela K e barra antigas), `t081-after.png` (grade nova com a dica do Golpe Pesado), `t081-bar-after.png` (Provocação no 1, Golpe Pesado no 6, poção no Q), `t081-bar-reopen.png` (depois de reabrir) e `t081-offline.png` (menu de cenários).

**Fora das áreas da tarefa (mínimo):**
- `Match/MatchSettings.cs`: `OnlineSettings.CharacterId`. `Account/AccountScreen.cs`: preenche o id do personagem ao jogar (chave da barra).
- `Main.cs`: liga a barra ao `PlayerInput` e ao HUD, monta a chave do personagem e atualiza a linha de ajuda ("1-6 atalhos"). O roteiro de mouse da T-082 passou a mandar `Relative` no movimento, porque o Godot começa o arraste pela soma dele.
- `tests/Game.Client.Tests`: o link e os testes.

**Decisões:**
- A poção aparece no Q só quando não está em 1–6, para não repetir o mesmo ícone.
- Habilidade não aprendida pode ficar na barra (o padrão de um personagem novo é assim), mas escura e sem efeito na tecla. Da tela K só dá para arrastar as aprendidas.
