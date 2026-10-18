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

_(preencher ao concluir)_
