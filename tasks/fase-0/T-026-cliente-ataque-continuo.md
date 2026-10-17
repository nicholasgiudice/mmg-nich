# T-026 — Cliente: ataque contínuo segurando o botão

- **Ambiente:** Windows
- **Modelo:** Leve
- **Depende de:** T-021, T-024
- **Libera:** nova rodada de playtest (T-025)
- **Áreas que pode alterar:** `client/`

## Contexto

Playtest (T-025, 2026-10-16): "o combate não está muito fluido". Parte disso é controle: hoje **cada clique é um ataque básico** (recarga de ~1 s), então o jogador clica sem parar durante toda a luta (pendência registrada na T-021). O lado numérico (velocidade dos monstros, dano) é a T-019.

## Requisitos

1. **Segurar o botão de ataque** (`attack`) repete o ataque básico automaticamente no alvo sob o cursor ou no selecionado (mesmas regras de alvo da T-021), assim que a recarga permitir.
2. **Sem spam de intenções**: enquanto o ataque está em recarga, conjurando ou impossível (sem alvo, morto), o cliente não envia uma intenção por frame. Usar a recarga do snapshot (`AbilityState.ReadyAtTick`) para enviar perto da hora de ficar pronto; rejeições repetidas não podem gerar um texto flutuante por frame.
3. **Segurar teclas de habilidade** (1–4) também repete a habilidade quando ficar pronta (opcional, mas com a mesma regra anti-spam). Um toque continua funcionando como hoje.
4. O clique sobre o HUD continua não virando ataque.
5. Registrar no Resultado o que mais foi percebido como "pouco fluido" no controle e não coube nesta tarefa.

## Critérios de aceite

- segurar o botão esquerdo sobre um inimigo ataca continuamente no ritmo da recarga;
- nenhuma intenção repetida por frame enquanto nada pode acontecer (verificável no log de intenções/rejeições);
- `dotnet build client/` e `./scripts/check.sh` passam.

## Resultado

Concluída em 2026-10-16 no Windows (Godot 4.7.2 .NET).

**Branch:** `task/T-026-cliente-ataque-continuo` saiu de `task/T-024-cliente-menu-cenarios` com `docs/tarefas-T-019-T-026` mesclada (para ter este arquivo). Integrar nesta ordem: `docs/tarefas-T-019-T-026`, T-024, T-026.

**Feito** (`client/scripts/Input/PlayerInput.cs`):
1. **Botão esquerdo mantido** repete o ataque básico (mesmas regras de alvo da T-021: unidade sob o cursor, senão a selecionada; mira sempre junto). O primeiro aperto continua igual (envia na hora e seleciona o alvo clicado).
2. **Anti-spam** (`CanRepeat`): com a tecla mantida, só envia quando a recarga do snapshot (`ReadyAtTick`) diz que a habilidade fica pronta no próximo tick, sem conjuração em andamento e com ao menos 2 ticks desde o último envio. Se a recarga não mudou desde o envio anterior (a autoridade recusou: fora de alcance, sem alvo, sem recurso), espera 8 ticks (0,4 s) para tentar de novo.
3. **Teclas 1–4 mantidas** repetem a habilidade com a mesma regra; toque funciona como antes. A poção (Q) **não** repete (segurar não gasta todas).
4. Clique sobre o HUD continua sem virar ataque.
5. **Ataque pelo Espaço** (pedido no playtest): ação `attack_key` (Espaço) faz o ataque básico no **alvo selecionado** (Tab ou clique) e mira nele — o cursor só conta quando não há seleção. Assim o jogador seleciona com Tab e ataca com Espaço sem precisar do mouse. Segurar o Espaço repete com a mesma regra anti-spam. O clique esquerdo continua como antes (alvo sob o cursor primeiro).
- `WorldView`: a mesma mensagem de rejeição não se repete antes de 1 s (botão mantido fora de alcance não enche a tela de "Fora de alcance").
- Depuração: `--hold=attack,ability_1,...` mantém ações apertadas (usado nos testes abaixo); `--capture-after` passou a valer também com `--demo-input`.

**Verificação** (`--hold`, log de `started`/`rejected` do jogador):
- Guerreiro segurando o ataque 8 s: 9 golpes nos ticks 1, 19, 37, … 145 — exatamente a recarga de 0,9 s (18 ticks), 0 rejeições.
- Mago segurando o ataque longe dos ratos 6 s: 0 envios por frame; 1 tentativa a cada 8 ticks (15 rejeições em 6 s, texto mostrado no máximo 1×/s).
- Espaço mantido com o cursor sobre o inimigo mais próximo: 1 Tab → ataques no #3 (o mais próximo); 2 Tabs → ataques no #2 (Salteador), a cada recarga — segue a seleção, não o cursor.
- Arqueiro andando até os inimigos segurando ataque + Marca: tenta até entrar no alcance, depois dispara a cada recarga; a Marca (recarga 12 s) saiu 1 vez e não foi reenviada.
- `dotnet build client/` sem avisos; `./scripts/check.sh` OK (332 testes).

**Ainda pouco fluido no controle (fora desta tarefa, para o próximo playtest):**
- Não há "fila" de habilidade: apertar uma habilidade 0,1 s antes de ficar pronta (sem segurar) é recusado com "Em recarga"; segurar resolve.
- Habilidades com conjuração (Golpe Pesado, Explosão Elemental, Cura Direta) são canceladas ao andar, sem aviso visual próprio além do log.
- O lado numérico (monstros mais rápidos que o jogador, dano baixo) é a T-019.
