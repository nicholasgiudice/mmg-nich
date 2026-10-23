#!/usr/bin/env bash
# Atualiza a main, roda os testes rápidos e reconstrói/reinicia o servidor de jogo no Docker (T-110).
# Uso (na VPS, na raiz do repositório ou de qualquer lugar): ./scripts/deploy-dev.sh
set -euo pipefail
cd "$(dirname "$0")/.."

compose=(docker compose -f deploy/compose.yml)

echo "==> git pull --ff-only (main)"
git checkout main
git pull --ff-only origin main

echo "==> check.sh --fast"
./scripts/check.sh --fast

echo "==> docker compose build"
"${compose[@]}" build

echo "==> docker compose up -d"
"${compose[@]}" up -d

sleep 3
echo "==> últimas linhas do log"
"${compose[@]}" logs --tail=20 game-server
