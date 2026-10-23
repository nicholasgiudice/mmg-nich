#!/usr/bin/env bash
# PostgreSQL descartável para os testes de integração (T-210), só em 127.0.0.1. Independente do banco do Compose.
# Uso, na raiz do repositório:
#   ./deploy/test-db.sh up      # sobe o contêiner e espera ficar pronto
#   export MMO_TEST_DB="Host=127.0.0.1;Port=55432;Username=postgres;Password=test;Database=postgres"
#   dotnet test tests/Game.Persistence.Tests
#   ./deploy/test-db.sh down    # remove o contêiner (e os dados)
set -euo pipefail

name=mmo-test-db
port=55432

case "${1:-up}" in
  up)
    if ! docker ps --format '{{.Names}}' | grep -qx "$name"; then
      docker run --rm -d --name "$name" -e POSTGRES_PASSWORD=test -p "127.0.0.1:$port:5432" postgres:17-alpine >/dev/null
    fi
    for _ in $(seq 1 60); do
      if docker exec "$name" pg_isready -U postgres >/dev/null 2>&1; then
        echo "pronto. export MMO_TEST_DB=\"Host=127.0.0.1;Port=$port;Username=postgres;Password=test;Database=postgres\""
        exit 0
      fi
      sleep 1
    done
    echo "PostgreSQL de teste não ficou pronto" >&2
    exit 1
    ;;
  down)
    docker rm -f "$name" >/dev/null 2>&1 || true
    echo "removido"
    ;;
  *)
    echo "uso: $0 [up|down]" >&2
    exit 2
    ;;
esac
