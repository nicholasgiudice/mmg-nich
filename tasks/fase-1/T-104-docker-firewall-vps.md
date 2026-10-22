# T-104 — VPS: Docker, firewall e porta UDP do jogo

- **Ambiente:** VPS
- **Modelo:** Humano + Leve
- **Depende de:** —
- **Libera:** T-110
- **Áreas que pode alterar:** `README.md` (seção de infraestrutura), `scripts/` (scripts de verificação), `tasks/`

## Contexto

`tech/stack-proposal-v0.1.md` (seção 10) prevê Docker Engine + Compose, `ufw` liberando só SSH, 443/tcp e a porta UDP do jogo (7777), serviços sem root. Algumas etapas pedem `sudo` e decisões do humano (porta SSH, usuário).

## Requisitos

1. Instalar **Docker Engine** e o **plugin Compose** pelos repositórios oficiais; usuário de desenvolvimento no grupo `docker`; `docker run hello-world` funcionando.
2. **ufw**: política padrão negar entrada; liberar SSH (porta atual da VPS), 443/tcp e **7777/udp**; ativar sem perder o acesso SSH (testar numa segunda sessão antes de fechar a primeira).
3. Verificar a porta UDP de fora: um `nc -u -l 7777` na VPS recebendo um pacote enviado do Windows (`scripts/udp-check.ps1` ou instrução equivalente).
4. Documentar no `README.md`: versões instaladas, portas abertas, como verificar.

## Critérios de aceite

- `docker compose version` funciona;
- `ufw status` mostra só SSH, 443/tcp e 7777/udp;
- pacote UDP do Windows chega à VPS na porta 7777.

## Resultado

Concluída em 2026-10-22 na VPS (`31.97.162.124`, Ubuntu 24.04.4).

### O que foi feito

- **Docker:** instalei pelo repositório oficial da Docker (chave em `/etc/apt/keyrings/docker.asc`) os pacotes `docker-ce` 29.9.2, `containerd.io` 2.4.2, `docker-buildx-plugin` 0.38.1 e `docker-compose-plugin` 5.6.1. O serviço está habilitado no boot (`systemctl enable --now docker`), e `docker run --rm hello-world` imprimiu "Hello from Docker!".
- **ufw:** a porta SSH em uso era a 22 (`ss -tlnp` mostrou `sshd` em `0.0.0.0:22` e `[::]:22`). Rodei `reset`, `default deny incoming`, `default allow outgoing`, liberei `22/tcp`, `443/tcp` e `7777/udp` e ativei. O humano testou uma segunda sessão SSH antes de seguirmos, e ela conectou.
- **Teste UDP:** criei `scripts/udp-check.ps1`, que envia um datagrama para `<ip>:7777`. Com `nc -u -l 7777` escutando na VPS, o pacote enviado do Windows chegou (saída abaixo).
- **README:** criei a seção "Infraestrutura (VPS)", com versões, portas, como verificar e o aviso sobre Docker × ufw.

### Saídas

`docker compose version`:

```text
Docker Compose version v5.6.1
```

`ufw status verbose`:

```text
Status: active
Logging: on (low)
Default: deny (incoming), allow (outgoing), deny (routed)
New profiles: skip

To                         Action      From
--                         ------      ----
22/tcp                     ALLOW IN    Anywhere                   # SSH
443/tcp                    ALLOW IN    Anywhere                   # HTTPS (Caddy)
7777/udp                   ALLOW IN    Anywhere                   # Game server UDP
22/tcp (v6)                ALLOW IN    Anywhere (v6)              # SSH
443/tcp (v6)               ALLOW IN    Anywhere (v6)              # HTTPS (Caddy)
7777/udp (v6)              ALLOW IN    Anywhere (v6)              # Game server UDP
```

Teste UDP: o `nc -u -l 7777` na VPS recebeu

```text
udp-check DESKTOP-6SV5H72 2026-10-22T16:34:28.7020459-03:00
```

vindo de `201.68.181.21:57096`, conforme `ss -uanp`. O pacote foi enviado com o equivalente de uma linha do `udp-check.ps1`, porque o script ainda não estava no remoto.

### Decisões e desvios

- **Grupo `docker`:** o desenvolvimento na VPS roda como `root`, então não havia usuário de desenvolvimento para incluir no grupo. Se for criado um usuário não-root, rodar `usermod -aG docker <usuário>`.
- **Docker ignora o ufw para portas publicadas.** Isso ficou registrado no README. A T-110 deve publicar só `443/tcp` e `7777/udp` e manter o Postgres sem `ports:`.
- **Primeira escuta sem pacote:** a primeira escuta do `nc` expirou (15 min) sem receber nada, porque o pacote ainda não tinha sido enviado. A segunda escuta recebeu o pacote.
- O MariaDB (`127.0.0.1:3306`), que já existia na VPS, escuta só em localhost e não é afetado.
- Nenhum código .NET foi alterado. Rodei `./scripts/check.sh` só para confirmar que a branch continua verde.

### Pendências

- Firewall do painel do provedor (se houver): o teste UDP passou, então hoje não bloqueia 7777/udp. Se 443 não responder quando o Caddy subir (T-110), conferir lá.
