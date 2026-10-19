# T-033 — Cliente: ataque básico automático no alvo selecionado

- **Ambiente:** Windows
- **Modelo:** Leve
- **Depende de:** T-026
- **Áreas que pode alterar:** `client/`, `tasks/`

## Contexto

Pedido do designer (2026-10-19): o ataque básico deve ser automático — sem clicar nem segurar o Espaço — sempre que houver um alvo selecionado.

## Requisitos

1. Com um **inimigo vivo selecionado** (Tab ou clique), o ataque básico sai sozinho quando está pronto e o alvo está no alcance.
2. Fora do alcance o cliente não envia o ataque (sem spam de "Fora de alcance"); ao chegar perto, começa sozinho.
3. Limpar o alvo (Esc) ou o alvo morrer para o ataque automático.
4. Clique e Espaço continuam funcionando como antes.

## Critérios de aceite

- selecionar um inimigo com Tab e ficar no alcance ataca continuamente sem apertar nada;
- `dotnet build client/` e `./scripts/check.sh` passam.

## Resultado

Concluída em 2026-10-19 no Windows (Godot 4.7.2 .NET).

**Feito** (`client/scripts/Input/PlayerInput.cs`): com inimigo vivo selecionado, o ataque básico é enviado sozinho quando a recarga permite (mesma regra anti-spam da T-026, mirando no alvo) e o alvo está no alcance do ataque (`unit` ou `melee_arc` da definição, descontando o raio do alvo). A checagem de alcance existe só para não enviar à toa — quem decide continua sendo a autoridade. Se clique ou Espaço já atacaram no frame, o automático não repete. `PlayerInput.AutoAttack` (ligado por padrão) permite desligar no futuro (ex.: opção de configuração). Textos de ajuda do jogo e do menu atualizados.

**Verificação** (roteiros só com `target_next`, sem clique nem Espaço):
- Arqueiro andando até os inimigos: 4 disparos automáticos depois de entrar no alcance e **0** rejeições "fora de alcance";
- Guerreiro parado: começa a atacar quando os lobos chegam (tick 55) e segue a cada 0,9 s;
- Tab e depois Esc (limpar alvo): 0 ataques.
- `dotnet build client/` sem avisos; `./scripts/check.sh` OK.

**Pendências:** sem opção no menu para desligar o ataque automático (a propriedade existe).
