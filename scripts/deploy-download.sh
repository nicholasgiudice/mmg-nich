#!/usr/bin/env bash
# Publica o cliente para Windows na área de download do site (https://<MMO_SITE_DOMAIN>/download.html).
#
# Uso (na VPS):
#   ./scripts/deploy-download.sh <Valdran-windows.zip> [--version <versão>]   publica o .zip
#   ./scripts/deploy-download.sh --list                                       lista as versões guardadas
#   ./scripts/deploy-download.sh --rollback                                   volta à versão anterior
#
# O .zip vem do Windows (scripts/export-client.ps1 gera build\Valdran-windows.zip). Para mandar e publicar de uma vez,
# rode no Windows scripts/deploy-download.ps1, ou copie e publique à mão:
#   scp build\Valdran-windows.zip root@<vps>:/tmp/   e na VPS:   ./scripts/deploy-download.sh /tmp/Valdran-windows.zip
#
# Pasta: MMO_SITE_DIR/downloads (MMO_SITE_DIR do ambiente, senão de deploy/.env, senão /srv/valdran-site), que o Caddy
# serve em /downloads/, fora das versões do site (publicar o site não mexe no download):
#   Valdran-windows-<versão>.zip   um arquivo por versão
#   Valdran-windows.zip            link simbólico para a versão atual (endereço fixo, para quem não tem JavaScript)
#   latest.json                    versão, arquivo, tamanho, data e SHA-256 da atual; a página de download lê este arquivo
#   history                        versões publicadas, em ordem (a última é a atual)
# Os três últimos são trocados com rename atômico: quem baixa no meio da publicação recebe um arquivo inteiro.
# Versão padrão: data e hora UTC (AAAA.MM.DD-HHMM). Ficam as KEEP (MMO_DOWNLOAD_KEEP, padrão 3) versões mais recentes.
set -euo pipefail
cd "$(dirname "$0")/.."

keep="${MMO_DOWNLOAD_KEEP:-3}"
name="Valdran-windows"

site_dir() {
  if [ -n "${MMO_SITE_DIR:-}" ]; then
    echo "$MMO_SITE_DIR"
  elif [ -f deploy/.env ] && grep -q '^MMO_SITE_DIR=' deploy/.env; then
    grep '^MMO_SITE_DIR=' deploy/.env | tail -1 | cut -d= -f2- | sed 's/[[:space:]]*#.*//; s/^"//; s/"$//'
  else
    echo /srv/valdran-site
  fi
}

dir="$(site_dir)/downloads"

die() { echo "erro: $*" >&2; exit 1; }

current_version() {
  [ -f "$dir/history" ] && tail -1 "$dir/history" || true
}

# Aponta o link e o latest.json para a versão $1 (o arquivo já tem de existir).
switch_to() {
  local version="$1" file size sha date
  file="$name-$version.zip"
  [ -f "$dir/$file" ] || die "versão $version não encontrada em $dir"
  size="$(stat -c %s "$dir/$file")"
  sha="$(sha256sum "$dir/$file" | cut -d' ' -f1)"
  date="$(date -u -r "$dir/$file" +%Y-%m-%dT%H:%M:%SZ)"
  printf '{\n  "version": "%s",\n  "file": "%s",\n  "size": %s,\n  "date": "%s",\n  "sha256": "%s"\n}\n' \
    "$version" "$file" "$size" "$date" "$sha" >"$dir/.latest.json.new"
  ln -sfn "$file" "$dir/.$name.zip.new"
  mv -Tf "$dir/.$name.zip.new" "$dir/$name.zip"
  mv -f "$dir/.latest.json.new" "$dir/latest.json"
}

publish() {
  local zip="$1" version="$2" target
  [ -f "$zip" ] || die "arquivo não encontrado: $zip"
  [ "$(head -c 2 "$zip")" = "PK" ] || die "$zip não é um .zip"
  if command -v unzip >/dev/null; then
    unzip -tq "$zip" >/dev/null || die "$zip está corrompido (unzip -t falhou)"
    unzip -l "$zip" | grep -q 'Valdran\.exe$' || die "$zip não tem Valdran.exe (é o .zip do scripts/export-client.ps1?)"
  fi

  [[ "$version" =~ ^[A-Za-z0-9._-]+$ ]] || die "versão inválida: '$version' (use letras, números, ponto, hífen e sublinhado)"
  target="$dir/$name-$version.zip"
  [ -e "$target" ] && die "a versão $version já existe em $dir (use outra --version)"

  umask 022
  mkdir -p "$dir"
  cp "$zip" "$dir/.upload-$$"
  trap 'rm -f "$dir/.upload-$$"' EXIT
  mv "$dir/.upload-$$" "$target"
  trap - EXIT

  switch_to "$version"
  echo "$version" >>"$dir/history"
  echo "==> publicado: $name-$version.zip ($(numfmt --to=iec --suffix=B "$(stat -c %s "$target")")) em $dir"
  prune
}

# Apaga as versões fora das KEEP mais recentes do histórico (a atual nunca é apagada).
prune() {
  local keepers f v
  keepers="$( (tac "$dir/history" | awk '!seen[$0]++' | head -n "$keep"; current_version) | sort -u)"
  for f in "$dir/$name"-*.zip; do
    [ -f "$f" ] || continue
    v="${f##*/$name-}"
    v="${v%.zip}"
    if ! grep -qxF "$v" <<<"$keepers"; then
      rm -f "$f"
      echo "    versão antiga removida: $v"
    fi
  done
}

rollback() {
  local cur prev
  cur="$(current_version)"
  [ -n "$cur" ] || die "nada publicado em $dir"
  prev="$(awk -v cur="$cur" '$0 != cur' "$dir/history" | tail -1)"
  if [ -z "$prev" ] || [ ! -f "$dir/$name-$prev.zip" ]; then
    die "não há versão anterior guardada em $dir"
  fi

  switch_to "$prev"
  # Histórico até a versão restaurada (inclusive): um novo --rollback volta mais uma.
  awk -v prev="$prev" '{ lines[NR] = $0; if ($0 == prev) last = NR } END { for (i = 1; i <= last; i++) print lines[i] }' \
    "$dir/history" >"$dir/history.new"
  mv -f "$dir/history.new" "$dir/history"
  echo "==> voltou para a versão $prev (era $cur)"
}

list() {
  local cur f v
  cur="$(current_version)"
  echo "pasta: $dir"
  for f in "$dir/$name"-*.zip; do
    [ -f "$f" ] || continue
    v="${f##*/$name-}"
    v="${v%.zip}"
    printf '%s %-20s %8s  %s\n' "$([ "$v" = "$cur" ] && echo '*' || echo ' ')" "$v" \
      "$(numfmt --to=iec --suffix=B "$(stat -c %s "$f")")" "$(date -r "$f" '+%Y-%m-%d %H:%M')"
  done
}

case "${1:-}" in
  --list) list ;;
  --rollback) rollback ;;
  -h | --help | "") sed -n '2,22p' "$0" | sed 's/^# \{0,1\}//' ;;
  -*) die "opção desconhecida: $1 (veja --help)" ;;
  *)
    zip="$1"
    shift
    version="$(date -u +%Y.%m.%d-%H%M)"
    while [ $# -gt 0 ]; do
      case "$1" in
        --version) [ $# -ge 2 ] || die "--version precisa de um valor"; version="$2"; shift 2 ;;
        *) die "argumento desconhecido: $1" ;;
      esac
    done
    publish "$zip" "$version"
    ;;
esac
