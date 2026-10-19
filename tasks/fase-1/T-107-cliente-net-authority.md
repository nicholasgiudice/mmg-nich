# T-107 — Cliente: `NetAuthority` e jogar online

- **Ambiente:** Windows
- **Modelo:** **Forte**
- **Depende de:** T-102, T-103, T-105
- **Libera:** T-108, T-111
- **Áreas que pode alterar:** `client/`

## Contexto

A apresentação do cliente só conhece `IGameAuthority` (T-020): trocar `LocalAuthority` por uma autoridade de rede não pode exigir mudar HUD, mundo ou input. Entidades remotas são desenhadas com ~100 ms de atraso, interpolando entre snapshots (`tech/stack-proposal-v0.1.md`, seção 4).

## Requisitos

1. `NetAuthority : IGameAuthority, ICombatLogSource` com LiteNetLib: conecta, envia `Hello` (versão, hash do conteúdo, nome, classe), trata `Welcome`/`Reject`.
2. **Buffer de snapshots** e relógio de interpolação ~100 ms atrás do servidor; `Latest`/`Previous`/`InterpolationFraction` calculados a partir do buffer (não do tick local). Monta os snapshots com a fábrica da T-101.
3. Eventos recebidos entregues por `TakeEvents()` na ordem; log de combate via T-102.
4. `Send` envia intenções com número de sequência (mesmo filtro: só a entidade controlada).
5. **Menu**: opção "Jogar online" (endereço do servidor, nome, classe); mensagem clara para recusa (versão/dados diferentes, sala cheia) e queda de conexão (voltar ao menu).
6. HUD: indicador de ping e de perda de pacotes.
7. `--server=host:porta` como atalho de depuração.

## Critérios de aceite

- dois clientes no Windows conectados ao servidor (local ou VPS) se veem andando e lutando na mesma sala;
- trocar para o modo local continua funcionando como hoje;
- `dotnet build client/` e `./scripts/check.sh` passam.

## Resultado

_(preencher ao concluir)_
