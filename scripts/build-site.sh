#!/usr/bin/env bash
# Gera o site do jogo (T-071) a partir de data/*.json em site/dist/ (ou na pasta passada como argumento).
# A publicação (T-072) usa este script.
# Uso: ./scripts/build-site.sh [pasta-de-saída]
set -euo pipefail
cd "$(dirname "$0")/.."

out="${1:-site/dist}"

echo "==> dotnet build src/Game.Site"
dotnet build src/Game.Site -c Release -warnaserror --nologo -v quiet

echo "==> gerando o site em $out"
dotnet run --project src/Game.Site -c Release --no-build -- \
  --data data \
  --out "$out" \
  --content site/content \
  --art client/assets/art \
  --credits CREDITS.md
