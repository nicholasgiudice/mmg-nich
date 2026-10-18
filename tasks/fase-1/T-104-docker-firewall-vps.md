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

_(preencher ao concluir)_
