#!/usr/bin/env bash
# Site do jogo (T-071, T-072): gera o HTML estático a partir de data/*.json e, com --publish, publica na VPS.
#
# Uso:
#   ./scripts/build-site.sh                 gera em site/dist/
#   ./scripts/build-site.sh <pasta>         gera em <pasta>
#   ./scripts/build-site.sh --publish       gera e publica em MMO_SITE_DIR (troca atômica, sem reiniciar o Caddy)
#   ./scripts/build-site.sh --rollback      volta à versão publicada anterior (repetir volta mais uma)
#   ./scripts/build-site.sh --list          lista as versões guardadas e a publicada
#
# Publicação: MMO_SITE_DIR (ambiente, senão deploy/.env, senão /srv/valdran-site) guarda
#   releases/<id>/   uma pasta por versão; <id> = hash do conteúdo, então os mesmos dados dão a mesma versão
#   current          link simbólico relativo para releases/<id>; é o que o Caddy serve (/srv/site/current)
#   history          ids publicados, em ordem (o último é o atual)
# A troca é "ln -s" num nome temporário + "mv -T" por cima de current (rename atômico): nunca há meia página no ar.
# Publicar de novo com os mesmos dados não muda nada. Ficam as KEEP (padrão 5) versões mais recentes do histórico.
set -euo pipefail
cd "$(dirname "$0")/.."

keep="${MMO_SITE_KEEP:-5}"

site_dir() {
  if [ -n "${MMO_SITE_DIR:-}" ]; then
    echo "$MMO_SITE_DIR"
  elif [ -f deploy/.env ] && grep -q '^MMO_SITE_DIR=' deploy/.env; then
    grep '^MMO_SITE_DIR=' deploy/.env | tail -1 | cut -d= -f2- | sed 's/[[:space:]]*#.*//; s/^"//; s/"$//'
  else
    echo /srv/valdran-site
  fi
}

build() {
  echo "==> dotnet build src/Game.Site"
  dotnet build src/Game.Site -c Release -warnaserror --nologo -v quiet

  echo "==> gerando o site em $1"
  dotnet run --project src/Game.Site -c Release --no-build -- \
    --data data \
    --out "$1" \
    --content site/content \
    --art client/assets/art \
    --credits CREDITS.md
}

# Troca current para releases/$2 de forma atômica.
switch_to() {
  local dir="$1" id="$2"
  ln -sfn "releases/$id" "$dir/.current.new"
  mv -Tf "$dir/.current.new" "$dir/current"
}

current_id() {
  local link
  link="$(readlink "$1/current" 2>/dev/null || true)"
  echo "${link#releases/}"
}

publish() {
  local dir tmp id
  dir="$(site_dir)"
  mkdir -p "$dir/releases"
  tmp="$dir/releases/.tmp-$$"
  trap 'rm -rf "$tmp"' EXIT
  umask 022
  build "$tmp"

  # Mesmo conteúdo = mesmo id (só caminhos e bytes entram no hash, não datas).
  id="$(cd "$tmp" && find . -type f -print0 | LC_ALL=C sort -z | xargs -0 sha256sum | sha256sum | cut -c1-12)"
  if [ -d "$dir/releases/$id" ]; then
    rm -rf "$tmp"
  else
    mv "$tmp" "$dir/releases/$id"
  fi
  trap - EXIT

  if [ "$(current_id "$dir")" = "$id" ]; then
    echo "==> nada mudou: a versão $id já está publicada em $dir"
    return
  fi

  switch_to "$dir" "$id"
  echo "$id" >>"$dir/history"
  echo "==> publicado: versão $id em $dir/current"
  prune "$dir"
}

# Apaga as versões fora das KEEP mais recentes do histórico (a publicada nunca é apagada).
prune() {
  local dir="$1" keepers r name
  keepers="$( (tac "$dir/history" | awk '!seen[$0]++' | head -n "$keep"; current_id "$dir") | sort -u)"
  for r in "$dir"/releases/*/; do
    [ -d "$r" ] || continue
    name="$(basename "$r")"
    if ! grep -qx "$name" <<<"$keepers"; then
      rm -rf "$r"
      echo "    versão antiga removida: $name"
    fi
  done
}

rollback() {
  local dir cur prev
  dir="$(site_dir)"
  cur="$(current_id "$dir")"
  [ -f "$dir/history" ] || { echo "sem histórico em $dir" >&2; exit 1; }
  # Tira o atual do fim do histórico; o anterior diferente dele passa a ser o publicado.
  prev="$(awk -v cur="$cur" '$0 != cur' "$dir/history" | tail -1)"
  if [ -z "$prev" ] || [ ! -d "$dir/releases/$prev" ]; then
    echo "não há versão anterior guardada em $dir" >&2
    exit 1
  fi

  switch_to "$dir" "$prev"
  # Histórico até a versão restaurada (inclusive): um novo --rollback volta mais uma.
  awk -v prev="$prev" '{ lines[NR] = $0; if ($0 == prev) last = NR } END { for (i = 1; i <= last; i++) print lines[i] }' \
    "$dir/history" >"$dir/history.new"
  mv -f "$dir/history.new" "$dir/history"
  echo "==> voltou para a versão $prev (era $cur)"
}

list() {
  local dir cur r name
  dir="$(site_dir)"
  cur="$(current_id "$dir")"
  echo "pasta: $dir"
  for r in "$dir"/releases/*/; do
    [ -d "$r" ] || continue
    name="$(basename "$r")"
    printf '%s %s  %s\n' "$([ "$name" = "$cur" ] && echo '*' || echo ' ')" "$name" "$(date -r "$r" '+%Y-%m-%d %H:%M')"
  done
}

case "${1:-}" in
  --publish) publish ;;
  --rollback) rollback ;;
  --list) list ;;
  -h | --help) sed -n '2,17p' "$0" | sed 's/^# \{0,1\}//' ;;
  -*) echo "opção desconhecida: $1 (veja --help)" >&2; exit 2 ;;
  *) build "${1:-site/dist}" ;;
esac
