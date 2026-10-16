#!/usr/bin/env bash
# Verificação única usada por todos os agentes: formatação, build e testes.
# Uso: ./scripts/check.sh          (completo, inclui tests/Game.Design.Tests, ~1-2 min a mais)
#      ./scripts/check.sh --fast   (pula os testes de design; use durante o trabalho)
set -euo pipefail
cd "$(dirname "$0")/.."

fast=0
case "${1:-}" in
  "") ;;
  --fast) fast=1 ;;
  *) echo "uso: $0 [--fast]" >&2; exit 2 ;;
esac

echo "==> dotnet format --verify-no-changes"
dotnet format MmoRpg.sln --verify-no-changes

echo "==> dotnet build -warnaserror"
dotnet build MmoRpg.sln -warnaserror

if [ "$fast" -eq 1 ]; then
  echo "==> dotnet test (modo rápido: sem tests/Game.Design.Tests)"
  for proj in tests/*/*.csproj; do
    case "$proj" in
      tests/Game.Design.Tests/*) continue ;;
    esac
    dotnet test "$proj" --no-build
  done
  echo "==> check.sh --fast: OK (rode ./scripts/check.sh completo antes do commit)"
  exit 0
fi

echo "==> dotnet test"
dotnet test MmoRpg.sln --no-build

echo "==> check.sh: OK"
