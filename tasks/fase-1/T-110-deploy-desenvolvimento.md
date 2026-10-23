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

Concluída em 2026-10-23 na VPS (`31.97.162.124`). Instruções de uso no `README.md`, seção "Servidor de jogo na VPS (Docker)".

### O que foi feito

- `deploy/game-server/Dockerfile`: build em duas etapas. A primeira usa `mcr.microsoft.com/dotnet/sdk:10.0` e roda `dotnet publish src/Game.Server -c Release`, copiando só `global.json`, `Directory.Build.props` e `src/`. A imagem final é `mcr.microsoft.com/dotnet/runtime:10.0` com `USER $APP_UID` (uid 1654, `app`), `data/` em `/app/data` e `MMO_DATA_DIR=/app/data`. Tamanho: 302 MB em disco, 84 MB de conteúdo.
- `.dockerignore` na raiz (o contexto do build é a raiz do repositório): exclui `**/bin/`, `**/obj/`, `client/` e `.git/`.
- `deploy/compose.yml`: projeto `mmo-rpg`, serviço `game-server` e imagem `mmo-rpg/game-server:dev`. Só a porta `7777:7777/udp` é publicada. Tem `restart: unless-stopped` e `stdin_open: true` para o console. O `command` passa `--encounter ${GAME_ENCOUNTER:-encounter.forest_matriarch} --bots ${GAME_BOTS:-true}`.
- `scripts/deploy-dev.sh`: faz `git checkout main` e `git pull --ff-only origin main`, roda `./scripts/check.sh --fast`, depois `docker compose build` e `up -d`, e mostra as últimas 20 linhas do log.
- `deploy/systemd/mmo-game-server.service`: unidade de exemplo com publish em `/opt/mmo-rpg/game-server`, dados em `/opt/mmo-rpg/data`, usuário de sistema `mmo-game`, `Restart=always` e hardening básico. Os passos de instalação estão no comentário do arquivo. **Não foi ativada.**
- `README.md`: como subir, as variáveis, como ver logs, reiniciar o encontro pelo console, atualizar, parar e conectar do Windows, e a alternativa com systemd.

### Decisões

- Console: `docker compose -f deploy/compose.yml attach --sig-proxy=false game-server`. Sem `tty`, um Ctrl+C no attach normal é repassado ao contêiner e para o servidor. Com `--sig-proxy=false`, ele só desconecta o terminal. Não usei `tty: true` para o log não sair com códigos de cor ANSI.
- `name: mmo-rpg` no Compose, para o projeto não se chamar `deploy` (nome da pasta).

### Verificação

```
$ docker compose -f deploy/compose.yml ps
NAME                    IMAGE                     COMMAND                  SERVICE       CREATED          STATUS         PORTS
mmo-rpg-game-server-1   mmo-rpg/game-server:dev   "dotnet Game.Server.…"   game-server   11 minutes ago   Up 5 minutes   0.0.0.0:7777->7777/udp, [::]:7777->7777/udp

$ docker compose -f deploy/compose.yml exec game-server id
uid=1654(app) gid=1654(app) groups=1654(app)
```

Log depois do `reboot` da VPS (boot às 00:51:13, contêiner iniciado às 00:51:32, sem intervenção), com o cliente Windows conectando pela internet:

```
[00:51:33 INF] Escutando UDP na porta 7777
[00:51:33 INF] Sala: encounter.forest_matriarch (4 vagas, bots True)
[00:52:29 INF] Conexão 0 aberta
[00:52:30 INF] windows entrou na vaga 2 como class.mage (entidade #3)
[00:53:33 INF] Conexão 0 aberta
[00:53:33 INF] Nich entrou na vaga 0 como class.warrior (entidade #1)
[00:55:22 INF] Nich saiu (vaga 0); entidade removida em 30 s se não voltar
[00:55:22 INF] Sala reiniciada (todos os jogadores saíram); seed 3
```

- Antes do reboot, um bot de rede (`tests/Game.NetBots`, `--server 31.97.162.124:7777`) conectou pela própria VPS: ping de 9 a 11 ms, sem perda.
- O comando `status` enviado pelo attach funcionou e o contêiner continuou de pé.
- `./scripts/check.sh` completo: OK.

### Pendências

- **Nível de log:** o servidor não tem opção para isso (o Serilog está fixo em `Information` em `src/Game.Server/Program.cs`), então o Compose não tem essa variável. Fica para uma tarefa que possa mexer em `src/`, por exemplo com `--log-level` ou `MMO_LOG_LEVEL`.
- `docker compose down` e `stop` enviam SIGTERM. O servidor só trata Ctrl+C (`CancelKeyPress`), então sai pelo encerramento padrão do runtime, sem o log "Servidor parado". Não atrapalha o deploy.
