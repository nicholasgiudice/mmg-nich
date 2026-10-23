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

_(preencher ao concluir)_
