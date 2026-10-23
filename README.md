# MMO RPG Medieval

Projeto de um MMO RPG 2D medieval com foco inicial em mapa, PvE, quatro classes e habilidades.

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

Por padrão, toda entrada é negada e toda saída é permitida. Só três portas aceitam entrada, em IPv4 e IPv6:

| Porta | Uso |
|---|---|
| `22/tcp` | SSH |
| `443/tcp` | HTTPS (Caddy → API) |
| `7777/udp` | servidor de jogo |

> **Atenção:** portas publicadas pelo Docker (`ports:` no Compose) **ignoram o ufw**, porque o Docker escreve as próprias regras de iptables antes das do ufw. Publique só `443` (Caddy) e `7777/udp` (servidor de jogo). Postgres e API ficam apenas na rede interna do Compose. Para expor algo só localmente, use `127.0.0.1:porta:porta`.

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

**Conectar do Windows:** no cliente, menu **Jogar online** com `31.97.162.124:7777`, ou abra o cliente com `--server=31.97.162.124:7777`. O conteúdo (`data/`) do cliente precisa ser o mesmo do servidor: o fingerprint é conferido na conexão.

**Alternativa com systemd** (se o Docker atrapalhar o UDP ou o debug): `deploy/systemd/mmo-game-server.service` é uma unidade de exemplo (publica em `/opt/mmo-rpg/game-server`, usuário `mmo-game`, `Restart=always`). Os passos de instalação estão no comentário do arquivo. Ela **não está ativada**, e as duas formas não podem rodar juntas, porque usam a mesma porta.

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
