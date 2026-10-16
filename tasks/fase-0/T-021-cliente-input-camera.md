# T-021 — Cliente: input e câmera

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** T-008, T-020
- **Libera:** T-022, T-023, T-024
- **Áreas que pode alterar:** `client/`

## Requisitos

1. **Input Map** do Godot com ações nomeadas (remapeáveis depois):
   - WASD: movimento em 8 direções → `MoveIntent`/`StopIntent` só quando a direção muda;
   - mouse: mira (ponto no mundo);
   - botão esquerdo: ataque básico no alvo sob o cursor ou no alvo selecionado;
   - 1, 2, 3: habilidades da classe, usando alvo selecionado ou ponto de mira conforme o tipo de alvo;
   - Tab: alterna alvo entre inimigos próximos; clique seleciona; Esc limpa seleção.
2. Conversão tela ↔ mundo usando a constante de 32 px/unidade.
3. Feedback de rejeição: texto curto acima do personagem para `AbilityRejected` ("Em recarga", "Sem Vigor", "Fora de alcance", "Sem linha de visão").
4. Câmera seguindo o jogador com suavização leve e pequeno deslocamento na direção da mira; limitada às bordas da arena.
5. Indicador do alvo selecionado.

## Critérios de aceite

- Todas as habilidades das quatro classes podem ser usadas com teclado e mouse;
- mensagens de rejeição aparecem corretamente;
- sem envio de intenção repetida a cada frame quando nada mudou.

## Resultado

Concluída em 2026-10-16 no Windows (Godot 4.7.2 .NET).

**Feito:**
- **Input Map** em `project.godot`: `move_up/down/left/right` (WASD + setas), `attack` (botão esquerdo), `ability_1..4` (teclas 1–4), `consumable_1` (Q), `target_next` (Tab), `target_clear` (Esc). Nomes em `client/scripts/Input/InputActions.cs`; o `Main` avisa no log se alguma ação faltar no Input Map.
- `client/scripts/Input/PlayerInput.cs`: lê as ações e envia intenções. Movimento em 8 direções só quando a direção muda; habilidades só no aperto da tecla (nada repetido por frame). Slots 1–4 = `AbilityState.Known` sem o ataque básico, na ordem dos dados.
  - Mira (ponto do mouse no mundo) vai em toda `UseAbilityIntent` (dá a direção de arco, linha, área e deslocamento).
  - Alvo único (`unit`): unidade sob o cursor, senão a selecionada, se for da facção certa; habilidade de aliado sem aliado mirado vai no próprio jogador (cura, escudo e purificação do Clérigo funcionam sozinho).
  - Clique esquerdo numa unidade a seleciona e ataca; Tab alterna entre inimigos vivos a até 15 tiles, do mais próximo ao mais distante; Esc limpa; o alvo é limpo ao morrer ou sumir.
- `Units.ToWorld` (pixels → tiles), mesma constante de 32 px.
- **Rejeição**: `AbilityRejected` do jogador vira texto flutuante amarelo acima do personagem, que o acompanha (`RejectionText`: "Em recarga", "Sem Vigor/Foco/Mana/Fé", "Fora de alcance", "Sem linha de visão", "Alvo inválido", "Já conjurando", "Atordoado" etc.).
- **Câmera** (`CameraRig`): segue a posição desenhada do jogador com suavização exponencial, avança até 20% da distância até a mira (máximo de 80 px) e é limitada às bordas da arena (arena menor que a tela fica centralizada). Saiu do `WorldView`.
- **Alvo selecionado**: anel amarelo em volta da unidade.
- `DebugInputScript` + `--demo-input=acao,acao,...`: aperta ações do Input Map em sequência (pelo mesmo caminho do teclado), mirando o inimigo mais próximo, e imprime `started`/`executed`/`rejected` do jogador. `--auto-move=x,y` agora mantém as ações de movimento. Com `--capture`, a tela é salva ao fim do roteiro.

**Verificação** (roteiros com `--demo-input` nos quatro encontros de classe): todas as habilidades das quatro classes foram **executadas** pelo menos uma vez — Guerreiro (sword_strike, heavy_strike, whirlwind, firm_guard, taunt), Arqueiro (shot, hunters_mark, quick_retreat, containment_trap), Mago (arcane_missile, elemental_blast*, ice_prison, arcane_barrier), Clérigo (holy_strike, direct_heal, sacred_shield, holy_chain, purify) e a poção. Rejeições observadas com o motivo e o texto certos: `NotEnoughResource`, `OutOfRange`, `NoLineOfSight`, `OnCooldown`, `AlreadyCasting`. `--fake-authority` continua rodando. `dotnet build client/` sem avisos; `./scripts/check.sh` OK (332 testes).

\* A Explosão Elemental tem tempo de conjuração; nos roteiros com movimento ela começou e foi cancelada ao andar (regra do Core). Sem movimento ela conclui.

**Decisões:**
- Teclas 1–4 em vez de 1–3: desde a T-017 Guerreiro e Clérigo têm 4 habilidades. Poção na tecla Q (não pedido, mas sem ela o consumível da T-017 ficaria inacessível).
- O cliente não pré-valida nada: envia a intenção e mostra a resposta da autoridade (a mesma coisa vai acontecer com o servidor).
- O tipo de alvo vem das definições carregadas pelo cliente (`ContentCatalog.Abilities`); com a autoridade falsa (sem conteúdo) só mira e alvo são enviados.

**Pendências / para as próximas tarefas:**
- Conjuração cancelada por movimento (`CastInterrupted`) ainda não tem retorno visual; barra de conjuração e log ficam para a T-022/T-023.
- Não há repetição automática do ataque básico com o botão mantido (cada clique = um ataque).
- Teste com mouse e teclado reais (sensação da câmera e da mira) ainda depende do playtest humano.
