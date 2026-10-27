#!/usr/bin/env bash
# Teste de carga (T-109): sobe uma instância do servidor por sala (4 vagas cada), roda os bots de rede
# e resume o tempo de tick dos servidores (médio e p99) e a banda dos bots.
#
# Uso: tests/Game.NetBots/load.sh <bots> [segundos=300] [porta-inicial=7800]
#   ex.: tests/Game.NetBots/load.sh 16        -> 4 servidores (7800..7803), 16 bots, 5 min
# Logs ficam em ${LOAD_LOGS:-/tmp/mmo-load}/.
set -euo pipefail

BOTS=${1:?quantos bots}
SECONDS_TO_RUN=${2:-300}
PORT0=${3:-7800}
ROOMS=$(( (BOTS + 3) / 4 ))
LOGS=${LOAD_LOGS:-/tmp/mmo-load}
ROOT=$(cd "$(dirname "$0")/../.." && pwd)

mkdir -p "$LOGS"
rm -f "$LOGS"/server-*.log "$LOGS"/bots.log

echo "==> build (Release)"
dotnet build "$ROOT/src/Game.Server" -c Release -v q >/dev/null
dotnet build "$ROOT/tests/Game.NetBots" -c Release -v q >/dev/null
SERVER="$ROOT/src/Game.Server/bin/Release/net10.0/Game.Server.dll"
NETBOTS="$ROOT/tests/Game.NetBots/bin/Release/net10.0/Game.NetBots.dll"

echo "==> $ROOMS servidor(es) a partir da porta $PORT0"
PIDS=()
ADDRS=()
for ((i = 0; i < ROOMS; i++)); do
  port=$((PORT0 + i))
  # stdin vazio: o leitor de comandos do console termina, o servidor continua rodando
  dotnet "$SERVER" --dev-login --port "$port" --bots false --seed $((i + 1)) </dev/null >"$LOGS/server-$port.log" 2>&1 &
  PIDS+=($!)
  ADDRS+=("127.0.0.1:$port")
done
trap 'kill "${PIDS[@]}" 2>/dev/null || true; wait 2>/dev/null || true' EXIT
sleep 3

SERVERS=$(IFS=,; echo "${ADDRS[*]}")
echo "==> $BOTS bots por $SECONDS_TO_RUN s"
dotnet "$NETBOTS" --server "$SERVERS" --bots "$BOTS" --duration "$SECONDS_TO_RUN" | tee "$LOGS/bots.log" | grep -E "^\[|^TOTAL"

echo
echo "==> tick dos servidores (janelas de 10 s com jogadores)"
# Linha do log: "Tick: média 0.931 ms, p99 10.492 ms, máx 26.293 ms em 200 ticks · 4 jogador(es) ..."
cat "$LOGS"/server-*.log | grep "Tick:" | grep -v " 0 jogador" | sed 's/,/./g' \
  | awk '{ for (i = 1; i <= NF; i++) { if ($i == "média") m += $(i+1); if ($i == "p99") { p += $(i+1); if ($(i+1) > pm) pm = $(i+1) } if ($i == "máx." || $i == "máx") if ($(i+1) > mx) mx = $(i+1); if ($i == "pulados") sk += $(i+1) } n++ }
         END { if (n) printf "janelas %d · tick médio %.3f ms · p99 médio %.3f ms (pior janela %.3f ms) · máx %.3f ms · ticks pulados %d\n", n, m/n, p/n, pm, mx, sk }'
