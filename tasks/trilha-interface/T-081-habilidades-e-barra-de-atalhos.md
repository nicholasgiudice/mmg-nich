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

_(preencher ao concluir)_
