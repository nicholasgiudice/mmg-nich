# T-110 — Deploy de desenvolvimento do servidor

- **Ambiente:** VPS
- **Modelo:** Leve
- **Depende de:** T-104, T-105
- **Libera:** T-111
- **Áreas que pode alterar:** `deploy/` (novo), `scripts/`, `README.md`

## Contexto

`tech/stack-proposal-v0.1.md` (seção 10, Implantação): Docker Compose, servidor de jogo expondo 7777/udp diretamente, contêineres sem root; systemd como alternativa se o Docker atrapalhar o UDP ou o debug.

## Requisitos

1. `deploy/game-server/Dockerfile` (build em várias etapas; imagem final só com o runtime, usuário não root, `data/` copiado).
2. `deploy/compose.yml` com o serviço `game-server` (7777/udp, reinício automático, variáveis: encontro, bots nas vagas livres, nível de log).
3. `scripts/deploy-dev.sh`: atualiza do Git, roda testes rápidos, reconstrói e reinicia o serviço.
4. Alternativa documentada com **systemd** (unidade de exemplo).
5. `README.md`: como subir, ver logs, reiniciar e conectar do cliente Windows (`--server=IP:7777` ou menu).

## Critérios de aceite

- o servidor sobe na VPS por `docker compose up -d` e o cliente Windows conecta pela internet;
- reiniciar a VPS sobe o servidor de novo sozinho.

## Resultado

_(preencher ao concluir)_
