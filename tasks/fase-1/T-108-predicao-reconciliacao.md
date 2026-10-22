# T-108 — Cliente: predição do próprio movimento e reconciliação

- **Ambiente:** Windows
- **Modelo:** **Forte**
- **Depende de:** T-107
- **Libera:** T-111
- **Áreas que pode alterar:** `client/`, `src/Game.Core/` (só se o movimento precisar ser chamado isoladamente — registrar)

## Contexto

`tech/stack-proposal-v0.1.md`: predição **apenas do movimento do próprio jogador**, com reconciliação quando o servidor corrige a posição; habilidades tocam animação imediatamente, mas dano, cura e status só aparecem quando o servidor confirma. O movimento e a colisão são do Core (T-007), então o cliente pode rodar a mesma regra localmente.

## Requisitos

1. O jogador local é desenhado na posição **prevista**: o cliente aplica as próprias intenções de movimento com a mesma regra do Core (movimento + colisão contra o mapa) a 20 Hz.
2. **Reconciliação**: ao receber o snapshot com `IntentAck`, volta à posição do servidor e reaplica as intenções ainda não confirmadas; correção pequena é suavizada, grande é aplicada direto.
3. Entidades remotas continuam interpoladas (T-107).
4. Feedback imediato de habilidade (ex.: barra de conjuração local começa no aperto), sem aplicar efeito; se o servidor recusar, o feedback é desfeito e a rejeição aparece (T-021).
5. Ferramenta de depuração: latência e perda artificiais no cliente (`--net-lag=ms`, `--net-loss=%`).

## Critérios de aceite

- com 150 ms de latência artificial, andar com WASD fica tão responsivo quanto no modo local;
- com 5% de perda, a posição do jogador não "salta" mais que alguns pixels em situação normal;
- `dotnet build client/` e `./scripts/check.sh` passam.

## Resultado

Feito no Windows. Verificado no Godot contra o servidor local, com latência e perda artificiais, e com testes automáticos do preditor contra o mundo do Core.

**Predição** (`client/scripts/Authority/MovementPredictor.cs`, sem Godot):
- Roda a mesma regra do `MovementSystem` do Core: velocidade × multiplicador de status × dt, e colisão deslizante com `Collision.MoveCircle` contra o mapa da arena (montado com o `Welcome`). Anda a 20 Hz no relógio do cliente e guarda a direção usada em cada tick.
- O multiplicador vem dos status do próprio jogador no snapshot, com a mesma conta do Core: atordoado ou enraizado = 0.
- A direção é normalizada como o `World` faz. Não usei nada novo do Core: `Collision.MoveCircle` já era público, então o Core não mudou.
- **Reconciliação:**
  - O servidor diz, no `IntentAck`, em que tick dele a última intenção passou a valer (sequência A no tick Ta). O snapshot do tick T corresponde ao tick do cliente `kA + (T − Ta)`.
  - O preditor volta à posição do servidor em T e reaplica as direções dos ticks seguintes.
  - Correção até 2 tiles vira um deslocamento de desenho que some em ~0,2 s. Acima disso (investida, empurrão grande) é aplicada direto. Histórico de 6,4 s.
- **Separação entre corpos** (empurrões) não é prevista: o servidor corrige e a correção é suavizada (medido: 0–3 px).

**No `NetAuthority`:**
- O próprio jogador é desenhado na posição **prevista**: ela substitui o `Transform` dele no `Previous`/`Latest`, então HUD, mundo, câmera e entrada não mudaram. O facing segue a direção apertada.
- As outras entidades continuam interpoladas 100 ms atrás (T-107). Morto: sem predição, vale o servidor.
- **Feedback de habilidade:** habilidade com conjuração mostra a barra local no aperto (o servidor ainda não confirmou). Ela some quando o servidor passa a mostrar a conjuração dele, quando chega `AbilityRejected` ou `CastInterrupted` (na chegada, sem esperar o relógio de desenho) ou quando o jogador anda. Nenhum efeito é aplicado no cliente.
- `IntentMessage.ClientTick` agora leva o tick previsto.

**Depuração:**
- `--net-lag=ms` (latência de ida e volta, metade em cada sentido) e `--net-loss=%` (perda dos pacotes não confiáveis nos dois sentidos: snapshots e confirmações), feitos no próprio `NetAuthority`. A simulação do LiteNetLib só existe em build de depuração da biblioteca.
- `--net-stats` imprime a cada segundo o maior passo do jogador desenhado entre frames e a última correção. O rodapé mostra a simulação ligada.

**Medido no Godot:**
- **150 ms + 5% de perda** (ping ~185–200 ms, perda medida 3–5%), andando: maior passo entre frames **2,4 px**, que é a própria velocidade do Guerreiro a 60 FPS (4,5 tiles/s × 32 px ÷ 60), e correções de 0 a 3 px. O jogador anda no frame seguinte ao aperto, como no modo local.
- **300 ms:** a barra "Cura Direta" aparece no aperto, antes de qualquer resposta do servidor.

**Testes** (`tests/Game.Client.Tests`, 6; compila o `MovementPredictor.cs` do cliente por link, porque o projeto do cliente usa o SDK do Godot). O "servidor" é um `World` de verdade com atraso de rede nas duas direções:
- com 150 ms o preditor anda já no próximo tick, antes de o servidor receber;
- trocas de direção (inclusive diagonal sem normalizar) batem com o servidor, com correção menor que 0,001 tile e posição final igual;
- escorregar na parede é previsto igual ao servidor;
- com **5% de perda** e frames irregulares, o desenho nunca anda mais de 0,25 tile entre frames;
- correção pequena do servidor é suavizada e grande é aplicada direto;
- enraizado não anda.

**Fora de `client/`:** o projeto `tests/Game.Client.Tests` (novo, no `MmoRpg.sln`), para testar o preditor sem o Godot.

**Pendências:** investida e recuo (deslocamento de habilidade) não são previstos: o jogador chega lá quando o servidor confirma, com a correção direta. Prever deslocamentos de habilidade fica para depois do playtest, se incomodar.
