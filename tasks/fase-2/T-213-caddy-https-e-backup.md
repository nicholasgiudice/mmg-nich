# T-213 — Caddy com HTTPS para a API e backup do banco fora da VPS

- **Ambiente:** VPS
- **Modelo:** Humano + Leve
- **Depende de:** T-211
- **Libera:** T-230
- **Áreas que pode alterar:** `deploy/`, `scripts/`, `README.md`

## Contexto

`tech/stack-proposal-v0.1.md` §10: Caddy na 443/tcp com HTTPS automático na frente da API; `pg_dump` agendado copiado para fora da VPS. Exige um domínio apontando para a VPS e um destino de backup — decisões do humano.

## Requisitos

1. Serviço `caddy` no Compose (443/tcp, certificados persistidos em volume) encaminhando para a `api`.
2. Domínio configurável por variável (o humano registra/aponta o DNS).
3. `scripts/backup-db.sh`: `pg_dump` compactado com data, mantendo os últimos N; cópia para fora da VPS (destino escolhido pelo humano: outro servidor por `rsync`/SSH ou armazenamento de objetos). Agendamento por cron ou timer do systemd.
4. Teste de restauração documentado (restaurar o dump num banco vazio).

## Critérios de aceite

- `https://<domínio>/` responde pela API com certificado válido;
- backup diário chegando fora da VPS e restauração testada uma vez;
- README atualizado.

## Resultado

Concluída em 2026-10-31 na VPS, com uma pendência: a cópia do backup para fora da VPS (destino ainda não decidido pelo humano).

**O que foi feito**

- `deploy/caddy/Caddyfile`: bloco `{$MMO_API_DOMAIN}` com `reverse_proxy api:8080`, mais um snippet `(common)` (compressão e log) e um modelo comentado de segundo bloco de domínio, para acrescentar depois o site do jogo em `nich.click`.
- `deploy/compose.yml`: serviço `caddy` (`caddy:2.10-alpine`), portas `80` e `443`, volumes `caddy_data` (certificados) e `caddy_config`. O domínio vem de `MMO_API_DOMAIN`, obrigatório no `deploy/.env`. Também adicionado ao `deploy/.env.example`, junto com as variáveis `BACKUP_*`.
- `scripts/backup-db.sh`: `pg_dump -Fc -Z 6` no contêiner `postgres`, conferido com `pg_restore --list` antes de aceitar o arquivo. Grava em `/var/backups/mmo-rpg/mmo-AAAAMMDD-HHMMSS.dump` (UTC, pasta 700, arquivos 600) e mantém os últimos `BACKUP_KEEP` (padrão 14). Se `BACKUP_RSYNC_DEST` estiver definido, espelha a pasta com `rsync -a --delete` sobre SSH (`BACKUP_SSH_KEY` opcional, `BatchMode`). Lê só as linhas `BACKUP_*`/`POSTGRES_*` do `deploy/.env`, sem executá-lo; o ambiente tem prioridade.
- `deploy/systemd/mmo-backup-db.{service,timer}`: backup diário às 06:30 UTC (03:30 em Brasília), `Persistent=true`. **Instalado e ativo na VPS.**
- README: porta 80 no ufw, seções "HTTPS (Caddy)" e "Backup do banco" (agendamento, teste de restauração, restauração de verdade, como ativar a cópia remota) e `MMO_API_DOMAIN` na primeira configuração.

**Aplicado na VPS (fora do Git)**

- `ufw allow 80/tcp` (v4 e v6). Agora as portas abertas são 22/tcp, 80/tcp, 443/tcp e 7777/udp.
- `deploy/.env`: `MMO_API_DOMAIN=api.nich.click`. O `MMO_GAME_SERVER_ADDRESS=31.97.162.124:7777` já estava lá; o serviço `api` foi recriado e o contêiner confirma a variável.
- `GAME_DEV_LOGIN` não foi mexido.

**Verificação**

- `curl -v https://api.nich.click/` → `HTTP/2 200` `{"service":"mmo-api","status":"ok"}`. Certificado Let's Encrypt (`CN=api.nich.click`, emissor YE2, vence em 29/01/2027), `SSL certificate verify ok`. `http://` responde 308 para `https://`.
- Backup: 3 execuções seguidas com `BACKUP_KEEP=2` removeram o mais antigo. O caminho do rsync foi testado com destino local (os mesmos arquivos espelhados). O backup também rodou pelo `systemctl start mmo-backup-db.service`.
- Restauração: o dump mais recente foi restaurado num `postgres:17-alpine` descartável e vazio (`pg_restore --exit-on-error`). A contagem de linhas de todas as tabelas (`__EFMigrationsHistory`, `accounts`, `characters`) bateu com o banco do jogo.

**Decisões**

- Timer do systemd em vez de cron: os logs vão para o `journalctl`, e o `Persistent=true` roda o backup perdido se a VPS estava desligada no horário.
- Formato custom do `pg_dump` (já compactado, restaura seletivamente) em vez de `.sql.gz`.
- Sem e-mail ACME no Caddyfile (é opcional no Let's Encrypt). Só 443/tcp, sem HTTP/3 (443/udp), para não abrir outra porta.
- O contêiner do Caddy roda como root (padrão da imagem). Para rodar sem root seria preciso ajustar o dono dos volumes; fica como melhoria futura.
- A API continua confiando em qualquer proxy no `X-Forwarded-For` (T-211). Isso é seguro porque ela não tem porta publicada, e o Caddy substitui o cabeçalho pelo IP real do cliente.

**Pendências**

- **Cópia fora da VPS:** escolher o destino, criar e autorizar a chave SSH e definir `BACKUP_RSYNC_DEST`/`BACKUP_SSH_KEY` no `deploy/.env` (passo a passo no README). Até lá, o critério "backup diário chegando fora da VPS" não está atendido: o backup diário é só local, e o script avisa isso a cada execução.
- Site do jogo em `nich.click`: futuro, com um novo bloco no Caddyfile.
