#!/usr/bin/env bash
# Verificação única usada por todos os agentes: formatação, build e testes.
set -euo pipefail
cd "$(dirname "$0")/.."

echo "==> dotnet format --verify-no-changes"
dotnet format MmoRpg.sln --verify-no-changes

echo "==> dotnet build -warnaserror"
dotnet build MmoRpg.sln -warnaserror

echo "==> dotnet test"
dotnet test MmoRpg.sln --no-build

echo "==> check.sh: OK"
