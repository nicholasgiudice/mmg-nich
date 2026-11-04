# Valdran

**Valdran** é um MMO RPG 2D medieval com foco inicial em mapa, PvE, quatro classes e habilidades.

## Estado atual

O projeto está em fase de idealização e prototipagem. O primeiro objetivo é validar um vertical slice pequeno antes de construir sistemas online complexos.

## MVP conceitual

- 1 cidade-fortaleza;
- 3 regiões externas;
- 1 dungeon;
- 1 miniboss;
- 1 boss;
- 4 classes base;
- combate em tempo real;
- PvE solo e em grupo;
- habilidades com cooldown, pagas com Mana (regenera com o tempo) e aprendidas com pontos a cada nível.

## Como compilar

Requer o .NET SDK 10. Para formatar-verificar, compilar e testar tudo:

```bash
./scripts/check.sh
```

## Como abrir o cliente

O cliente fica em `client/` e é um projeto Godot 4 **edição .NET** (testado com 4.7.2) que referencia `src/Game.Core`.

1. Instale o Godot 4 .NET (no Windows: `winget install GodotEngine.GodotEngine.Mono`) e o .NET SDK 10.
2. No Godot, use **Importar** e escolha `client/project.godot`.
3. Pressione **F5**: a janela deve mostrar `Core 0.0.1`.

Para só compilar pela linha de comando:

```bash
dotnet build client/
```

O `client/` não faz parte de `MmoRpg.sln`, então `./scripts/check.sh` não depende do Godot.

## Como gerar o executável para jogadores

O preset `Windows Desktop` está em `client/export_presets.cfg`. O executável sai num arquivo único, que já aponta para `https://api.nich.click`.

1. Uma vez só, no editor do Godot: **Editor → Gerenciar Modelos de Exportação → Baixar e Instalar** (a versão precisa ser a mesma do editor).
2. No Windows:

   ```powershell
   .\scripts\export-client.ps1
   ```

3. Envie `build\Valdran-windows.zip`. Quem recebe não precisa de Godot nem de .NET. Como o executável não é assinado, o SmartScreen pede **Mais informações → Executar assim mesmo**.

O preset inclui `*.json, *.tmj, *.tsx` como arquivos que não são recursos. Sem isso o Godot deixa de fora `data/` e os mapas, e o jogo abre vazio.

O export de C# exige uma solução: `client/sln/MmoRpgClient.sln` (apontada por `dotnet/project/solution_directory` em `project.godot`). Ela fica numa subpasta para `dotnet build client/` continuar achando um único projeto. O script usa o `Godot_*_console.exe`, porque o executável normal não espera o export terminar.

## Site do jogo

O site público do jogo (monstros, itens, classes e habilidades) está em **https://valdran.nich.click**. Ele é HTML estático **gerado** a partir de `data/*.json` por `src/Game.Site`. O gerador carrega os dados pelo `Game.Data`, com a mesma validação e a mesma herança de NH do jogo, e por isso nenhum número é copiado à mão.

```bash
./scripts/build-site.sh                   # gera em site/dist/
python3 -m http.server -d site/dist 8000  # abre em http://localhost:8000
```

- **Números, nomes e ids** vêm só de `data/`. Para mudar o site, mude o dado e gere de novo.
- **Textos que não são regra** (apresentação do jogo, descrição curta de classes e monstros, nomes das fases dos chefes) ficam em `site/content/site.json`.
- **Imagens**: o primeiro quadro de cada personagem e monstro sai das folhas em `client/assets/art/` (layout em `sprites.json`), e os ícones de habilidades, itens e atributos saem de `client/assets/art/icons/` (`icons.json`, T-080). Id sem ícone próprio fica sem ícone no site. A página de créditos é gerada de `CREDITS.md`, e as licenças das imagens exigem essa página.
- **Pontos em aberto**: o que está nos dados mas nenhuma classe nem monstro usa (ex.: Armadilha de Contenção) só aparece na página "Em aberto". O gerador avisa no console.
- `site/dist/` não é versionado. Os testes ficam em `tests/Game.Site.Tests`.

### Publicação na VPS (T-072)

- **DNS**: registro `A` de `valdran.nich.click` para o IP da VPS (`31.97.162.124`, o mesmo de `api.nich.click`). Confira com `dig +short valdran.nich.click`. O Caddy emite o certificado do Let's Encrypt sozinho na primeira subida.
- **Variáveis** em `deploy/.env`: `MMO_SITE_DOMAIN=valdran.nich.click` (obrigatória) e `MMO_SITE_DIR` (opcional, padrão `/srv/valdran-site`). O Caddy monta essa pasta só para leitura em `/srv/site` e serve `current/`.
- **Publicar** (o `scripts/deploy-dev.sh` também faz isso):

  ```bash
  ./scripts/build-site.sh --publish
  ```

  O script gera uma versão nova em `/srv/valdran-site/releases/<id>/` e troca o link `current` de uma vez, sem reiniciar o Caddy e sem meia página no ar. O `<id>` é o hash do conteúdo, então publicar de novo com os mesmos dados não muda nada. Ficam as 5 versões mais recentes (`MMO_SITE_KEEP`).
- **Voltar à versão anterior**: `./scripts/build-site.sh --rollback`. Repetir volta mais uma. `--list` mostra as versões guardadas, com `*` na publicada. Uma publicação nova (`--publish`) volta a pôr no ar o que os dados atuais geram.
- **Cache**: as páginas HTML saem com `no-cache`, então a versão nova aparece na hora; CSS e JS ficam 1 h no cache e as imagens 1 dia.
- Depois de mudar o `Caddyfile` ou o `compose.yml`, recrie só o Caddy: `docker compose -f deploy/compose.yml up -d --no-deps caddy`.

## Infraestrutura (VPS)

VPS Ubuntu 24.04 LTS (`31.97.162.124`). Configurada na T-104.

### Versões instaladas

Docker vem do repositório oficial (`download.docker.com/linux/ubuntu`, `/etc/apt/sources.list.d/docker.list`):

| Pacote | Versão |
|---|---|
| `docker-ce` | 29.9.2 |
| `containerd.io` | 2.4.2 |
| `docker-compose-plugin` | 5.6.1 (`docker compose`) |
| `docker-buildx-plugin` | 0.38.1 |
| `ufw` | 0.36.2 |

O desenvolvimento na VPS roda como `root`, então não foi preciso adicionar usuário ao grupo `docker`. Os contêineres do jogo devem rodar com usuário próprio (`user:` no Compose), sem root.

### Portas abertas (ufw)

Por padrão, toda entrada é negada e toda saída é permitida. Só quatro portas aceitam entrada, em IPv4 e IPv6:

| Porta | Uso |
|---|---|
| `22/tcp` | SSH |
| `80/tcp` | HTTP (Caddy: desafio do Let's Encrypt e redirecionamento para HTTPS) |
| `443/tcp` | HTTPS (Caddy → API) |
| `7777/udp` | servidor de jogo |

> **Atenção:** portas publicadas pelo Docker (`ports:` no Compose) **ignoram o ufw**, porque o Docker escreve as próprias regras de iptables antes das do ufw. Publique só `80`/`443` (Caddy) e `7777/udp` (servidor de jogo). Postgres e API ficam apenas na rede interna do Compose. Para expor algo só localmente, use `127.0.0.1:porta:porta`.

### Como verificar

```bash
docker --version && docker compose version
docker run --rm hello-world
sudo ufw status verbose
```

Para testar se a porta UDP do jogo é alcançável de fora:

1. Na VPS, deixe um processo escutando: `nc -u -l 7777`. Ninguém pode estar usando a porta, então pare o servidor de jogo antes.
2. No Windows, envie um pacote:
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\scripts\udp-check.ps1 -HostName 31.97.162.124
   ```
3. A mensagem `udp-check <máquina> <horário>` precisa aparecer no terminal do `nc`. Se não aparecer, confira `ufw status` e o firewall do provedor da VPS.

Se uma mudança no ufw cortar o SSH, entre pelo console web do provedor e rode `ufw disable`.

## Servidor de jogo na VPS (Docker)

Configurado na T-110: `deploy/game-server/Dockerfile` (imagem só com o runtime .NET, usuário `app` sem root, `data/` em `/app/data`) e `deploy/compose.yml` (serviço `game-server`, só `7777/udp` publicada, `restart: unless-stopped`). O Docker sobe com a VPS, então o servidor volta sozinho depois de um reinício. Todos os comandos rodam na raiz do repositório.

**Subir** (constrói a imagem se preciso):

```bash
docker compose -f deploy/compose.yml up -d --build
docker compose -f deploy/compose.yml ps
```

O encontro e os bots vêm de variáveis (no shell ou num `.env` em `deploy/`):

```bash
GAME_ENCOUNTER=encounter.forest_matriarch GAME_BOTS=false docker compose -f deploy/compose.yml up -d
```

| Variável | Padrão | Vira |
|---|---|---|
| `GAME_ENCOUNTER` | `encounter.forest_matriarch` | `--encounter` |
| `GAME_BOTS` | `true` | `--bots` (vagas sem jogador jogam como bots) |
| `GAME_RESTART_AFTER` | `10` | `--restart-after` (segundos mostrando o fim do encontro antes de recomeçar; 0 = não recomeça) |
| `GAME_LOG_LEVEL` | `info` | `--log-level` (`verbose`, `debug`, `info`, `warning`, `error`, `fatal`) |

**Ver logs** (deve aparecer `Escutando UDP na porta 7777`):

```bash
docker compose -f deploy/compose.yml logs -f game-server
```

**Reiniciar o encontro** pelo console do servidor:

```bash
docker compose -f deploy/compose.yml attach --sig-proxy=false game-server
```

Digite `restart` (ou `status`) e Enter. Para sair do console use **Ctrl+C**: com `--sig-proxy=false` ele só desconecta o terminal. Sem essa opção, o Ctrl+C é repassado e **para o servidor**. Para reiniciar o processo inteiro: `docker compose -f deploy/compose.yml restart game-server`.

**Atualizar** (puxa a `main`, roda `check.sh --fast`, reconstrói, reinicia e mostra o log):

```bash
./scripts/deploy-dev.sh
```

**Parar:** `docker compose -f deploy/compose.yml down`.

### Banco de dados (PostgreSQL)

Configurado na T-210. O `deploy/compose.yml` sobe o serviço `postgres` (imagem `postgres:17-alpine`, volume `pgdata`, **sem porta publicada**: só os serviços do Compose o alcançam). Também sobe o `migrate`, que aplica as migrations do `src/Game.Persistence` a cada `up` e sai.

**Primeira vez:** crie `deploy/.env` (fora do Git) com a senha. Sem ele, o `docker compose` recusa subir.

```bash
cp deploy/.env.example deploy/.env
sed -i "s/troque-esta-senha/$(openssl rand -hex 24)/" deploy/.env
sed -i "s|^MMO_SESSION_KEY=.*|MMO_SESSION_KEY=$(openssl rand -base64 32)|; s|^MMO_GAME_TICKET_KEY=.*|MMO_GAME_TICKET_KEY=$(openssl rand -base64 32)|" deploy/.env
sed -i "s|^MMO_API_DOMAIN=.*|MMO_API_DOMAIN=api.nich.click|" deploy/.env   # domínio da API (Caddy, T-213)
docker compose -f deploy/compose.yml up -d --build
docker compose -f deploy/compose.yml logs migrate
```

**Abrir um console SQL:**

```bash
docker compose -f deploy/compose.yml exec postgres psql -U mmo -d mmo
```

**Nova migration** (depois de mudar as entidades; o teste `Model_has_no_changes_missing_a_migration` acusa se faltar):

```bash
dotnet tool install --global dotnet-ef --version 10.0.13
dotnet ef migrations add NomeDaMudanca --project src/Game.Persistence --output-dir Migrations
```

**Testes de integração:** os testes de `tests/Game.Persistence.Tests` que usam o banco são pulados sem a variável `MMO_TEST_DB`. Na VPS, use um PostgreSQL descartável, separado do banco do Compose:

```bash
./deploy/test-db.sh up
export MMO_TEST_DB="Host=127.0.0.1;Port=55432;Username=postgres;Password=test;Database=postgres"
dotnet test tests/Game.Persistence.Tests tests/Game.Api.Tests
./deploy/test-db.sh down
```

### API de contas e personagens (`Game.Api`)

Criada na T-211. ASP.NET Core minimal API no serviço `api` do Compose (porta 8080 **só na rede interna**; o acesso de fora vem pelo Caddy, T-213). Ela não aplica migrations: isso é do `migrate`.

| Método e caminho | Autenticação | O que faz |
|---|---|---|
| `GET /` | — | saúde (`{"status":"ok"}`) |
| `POST /accounts` | — | cadastro `{email, password}` → 201 |
| `POST /sessions` | — | login `{email, password}` → `{token, expiresAt}` (sessão de 15 min) |
| `GET /characters` | sessão | personagens da conta (`id, name, classId, level, zoneId`) |
| `POST /characters` | sessão | cria `{name, classId}`: nível 1, na `zone.city`, com o equipamento inicial da classe |
| `DELETE /characters/{id}` | sessão | apaga (só da própria conta) → 204 |
| `POST /characters/{id}/play` | sessão | token de jogo `{ticket, expiresAt, server, address}`, válido por 60 s e uma vez só |

- A sessão vai em `Authorization: Bearer <token>`. Erros seguem o ProblemDetails, com `title` em português e um `code` estável (`email_taken`, `invalid_credentials`, `name_taken`, `name_invalid`, `class_invalid`, `character_limit`, `character_not_found`…).
- Senhas: hash do ASP.NET Identity (PBKDF2). Cadastro e login: 10 pedidos por IP por minuto (`MMO_AUTH_RATE_LIMIT`). O IP vem do `X-Forwarded-For` do Caddy.
- Nomes: 3 a 16 letras latinas (com acento) ou dígitos, começando por letra, únicos sem diferenciar maiúsculas. Até 5 personagens por conta.
- O token de jogo é assinado com `MMO_GAME_TICKET_KEY`. O servidor de jogo valida com a mesma chave, sem chamar a API (`src/Game.Auth`: `GameTicketReplayGuard` faz assinatura, servidor, expiração e uso único).
- Variáveis em `deploy/.env` (modelo em `deploy/.env.example`): `MMO_SESSION_KEY` e `MMO_GAME_TICKET_KEY` (obrigatórias), `MMO_GAME_SERVER_ID`, `MMO_GAME_SERVER_ADDRESS`, `MMO_AUTH_RATE_LIMIT`.

**Testar:** `curl https://api.nich.click/` → `{"service":"mmo-api","status":"ok"}`. De dentro da rede do Compose, sem passar pelo Caddy:

```bash
docker run --rm --network mmo-rpg_default curlimages/curl:8.10.1 -s http://api:8080/
```

**Rodar fora do Docker** (desenvolvimento): defina `MMO_DB_CONNECTION`, `MMO_SESSION_KEY` e `MMO_GAME_TICKET_KEY` e rode `dotnet run --project src/Game.Api`.

**Conectar do Windows:** no cliente, menu **Jogar online** com `31.97.162.124:7777`, ou abra o cliente com `--server=31.97.162.124:7777`. O conteúdo (`data/`) do cliente precisa ser o mesmo do servidor: o fingerprint é conferido na conexão.

**Alternativa com systemd** (se o Docker atrapalhar o UDP ou o debug): `deploy/systemd/mmo-game-server.service` é uma unidade de exemplo (publica em `/opt/mmo-rpg/game-server`, usuário `mmo-game`, `Restart=always`). Os passos de instalação estão no comentário do arquivo. Ela **não está ativada**, e as duas formas não podem rodar juntas, porque usam a mesma porta.

### HTTPS (Caddy)

Configurado na T-213. O serviço `caddy` do `deploy/compose.yml` publica `80/tcp` e `443/tcp`, obtém e renova sozinho o certificado do Let's Encrypt e encaminha para a `api`. A configuração está em `deploy/caddy/Caddyfile`; os certificados ficam no volume `caddy_data` (não apague: reemitir muitas vezes esbarra no limite do Let's Encrypt).

- O domínio vem de `MMO_API_DOMAIN` em `deploy/.env` (hoje `api.nich.click`, registro A para `31.97.162.124`). O DNS precisa apontar para a VPS **antes** de subir o Caddy, e a porta 80 precisa estar aberta para o desafio.
- Outro site (ex.: o site do jogo em `nich.click`): acrescente um bloco de domínio no `Caddyfile` (há um modelo no comentário do arquivo), passe a variável do domínio no serviço `caddy` e rode `docker compose -f deploy/compose.yml up -d caddy`.
- Depois de editar só o `Caddyfile`: `docker compose -f deploy/compose.yml exec caddy caddy reload --config /etc/caddy/Caddyfile`.
- Logs: `docker compose -f deploy/compose.yml logs -f caddy`.

### Backup do banco

Configurado na T-213. `scripts/backup-db.sh` roda `pg_dump -Fc` (formato custom, compactado) no contêiner `postgres`, confere o arquivo com `pg_restore --list` e grava em `/var/backups/mmo-rpg/mmo-AAAAMMDD-HHMMSS.dump` (UTC), mantendo os últimos 14. Se `BACKUP_RSYNC_DEST` estiver definido, espelha a pasta para fora da VPS por `rsync` sobre SSH. Variáveis (em `deploy/.env` ou no ambiente): `BACKUP_DIR`, `BACKUP_KEEP`, `BACKUP_RSYNC_DEST` (ex.: `usuario@host:/srv/backups/mmo-rpg/`), `BACKUP_SSH_KEY`.

> **Pendente:** o destino fora da VPS ainda não foi escolhido; por enquanto o backup é só local e o script avisa isso a cada execução. Para ativar: crie uma chave (`ssh-keygen -t ed25519 -N '' -f /root/.ssh/mmo-backup`), autorize-a no destino, aceite a chave do host uma vez (`ssh -i /root/.ssh/mmo-backup usuario@host true`) e defina `BACKUP_RSYNC_DEST` e `BACKUP_SSH_KEY` em `deploy/.env`.

**Agendamento:** timer do systemd, todo dia às 06:30 UTC (03:30 em Brasília). Instalado na VPS com:

```bash
cp deploy/systemd/mmo-backup-db.service deploy/systemd/mmo-backup-db.timer /etc/systemd/system/
systemctl daemon-reload && systemctl enable --now mmo-backup-db.timer
systemctl list-timers mmo-backup-db.timer   # próxima execução
systemctl start mmo-backup-db.service       # rodar agora
journalctl -u mmo-backup-db                 # logs
```

**Teste de restauração** (num PostgreSQL descartável e vazio, sem tocar no banco do jogo):

```bash
docker run --rm -d --name mmo-restore-test -e POSTGRES_PASSWORD=test postgres:17-alpine
until docker exec mmo-restore-test pg_isready -U postgres; do sleep 1; done
docker exec mmo-restore-test createdb -U postgres mmo_restore
docker exec -i mmo-restore-test pg_restore -U postgres -d mmo_restore --no-owner --exit-on-error \
  < "$(ls -1t /var/backups/mmo-rpg/*.dump | head -1)"
docker exec mmo-restore-test psql -U postgres -d mmo_restore -c 'select count(*) from accounts' -c 'select count(*) from characters'
docker rm -f mmo-restore-test
```

**Restaurar de verdade** (perda do banco): pare quem escreve (`docker compose -f deploy/compose.yml stop api game-server`), recrie o banco e restaure o dump:

```bash
docker compose -f deploy/compose.yml exec -T postgres dropdb -U mmo --force mmo
docker compose -f deploy/compose.yml exec -T postgres createdb -U mmo mmo
docker compose -f deploy/compose.yml exec -T postgres pg_restore -U mmo -d mmo --exit-on-error < /var/backups/mmo-rpg/<arquivo>.dump
docker compose -f deploy/compose.yml up -d
```

## Documentação

- `docs/vision-mvp-v0.1.md` — visão, escopo e decisões;
- `design/classes-v0.1.md` — quatro classes base;
- `design/first-map-v0.1.md` — primeiro mapa;
- `design/class-map-validation-v0.1.md` — validação das classes contra o mapa;
- `design/combat-encounters-v0.1.md` — encontros de combate;
- `tech/architecture-v0.1.md` — direção tecnológica;
- `tech/stack-proposal-v0.1.md` — proposta de stack (linguagem, servidor, rede, dados, persistência);
- `tasks/README.md` — plano de execução e tarefas para agentes;
- `AGENTS.md` — regras para agentes que trabalham no repositório.

## Fora do MVP

PvP, guerras territoriais, guildas, economia complexa, evoluções de classe, crafting lendário, múltiplas cidades e mundo dinâmico ficam para depois.
