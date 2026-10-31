#!/usr/bin/env bash
# Backup do PostgreSQL do Compose (T-213): pg_dump compactado com data, mantém os últimos N
# e, se configurado, copia a pasta de backups para fora da VPS por rsync/SSH.
# Uso (na VPS, como root): ./scripts/backup-db.sh
# Agendado pelo timer deploy/systemd/mmo-backup-db.timer (instalação no README).
#
# Configuração (variáveis de ambiente ou linhas BACKUP_* em deploy/.env; o ambiente tem prioridade):
#   BACKUP_DIR         pasta local dos dumps (padrão /var/backups/mmo-rpg)
#   BACKUP_KEEP        quantos dumps manter na pasta local (padrão 14)
#   BACKUP_RSYNC_DEST  destino fora da VPS, ex. usuario@host:/srv/backups/mmo-rpg/ (vazio = só local)
#   BACKUP_SSH_KEY     chave SSH para o rsync (opcional)
#
# Formato: pg_dump -Fc (custom, já compactado). Restaurar com pg_restore (ver README).
set -euo pipefail
cd "$(dirname "$0")/.."

env_file=deploy/.env
# Lê só as variáveis que o script usa, sem executar o .env.
env_value() {
  [[ -f "$env_file" ]] || return 0
  { grep -E "^$1=" "$env_file" || true; } | tail -n 1 | cut -d= -f2- | sed -E 's/[[:space:]]+#.*$//; s/^"(.*)"$/\1/'
}

BACKUP_DIR="${BACKUP_DIR:-$(env_value BACKUP_DIR)}"; BACKUP_DIR="${BACKUP_DIR:-/var/backups/mmo-rpg}"
BACKUP_KEEP="${BACKUP_KEEP:-$(env_value BACKUP_KEEP)}"; BACKUP_KEEP="${BACKUP_KEEP:-14}"
BACKUP_RSYNC_DEST="${BACKUP_RSYNC_DEST:-$(env_value BACKUP_RSYNC_DEST)}"
BACKUP_SSH_KEY="${BACKUP_SSH_KEY:-$(env_value BACKUP_SSH_KEY)}"
db_user="$(env_value POSTGRES_USER)"; db_user="${db_user:-mmo}"
db_name="$(env_value POSTGRES_DB)"; db_name="${db_name:-mmo}"

[[ "$BACKUP_KEEP" =~ ^[1-9][0-9]*$ ]] || { echo "BACKUP_KEEP inválido: $BACKUP_KEEP" >&2; exit 1; }

compose=(docker compose -f deploy/compose.yml)
mkdir -p "$BACKUP_DIR"
chmod 700 "$BACKUP_DIR"

file="$BACKUP_DIR/$db_name-$(date -u +%Y%m%d-%H%M%S).dump"
tmp="$file.part"
trap 'rm -f "$tmp"' EXIT

echo "==> pg_dump $db_name -> $file"
"${compose[@]}" exec -T postgres pg_dump -U "$db_user" -d "$db_name" -Fc -Z 6 > "$tmp"
# Confere que o arquivo é um dump legível antes de aceitá-lo.
"${compose[@]}" exec -T postgres pg_restore --list < "$tmp" > /dev/null
chmod 600 "$tmp"
mv "$tmp" "$file"
echo "    $(du -h "$file" | cut -f1)"

echo "==> mantendo os últimos $BACKUP_KEEP"
ls -1t "$BACKUP_DIR"/"$db_name"-*.dump | tail -n +"$((BACKUP_KEEP + 1))" | xargs -r rm -v --

if [[ -n "$BACKUP_RSYNC_DEST" ]]; then
  ssh_cmd="ssh -o BatchMode=yes"
  [[ -n "$BACKUP_SSH_KEY" ]] && ssh_cmd+=" -i $BACKUP_SSH_KEY"
  echo "==> rsync -> $BACKUP_RSYNC_DEST"
  # Espelha a pasta local (inclui a remoção dos antigos), então o destino guarda os mesmos N dumps.
  rsync -a --delete -e "$ssh_cmd" "$BACKUP_DIR"/ "$BACKUP_RSYNC_DEST"
else
  echo "==> AVISO: BACKUP_RSYNC_DEST vazio; backup só local (não protege contra perda da VPS)" >&2
fi
echo "==> ok"
