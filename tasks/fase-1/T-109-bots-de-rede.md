# T-109 — Bots de rede headless para teste de carga

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** T-103, T-105 (T-106 para a medição final de banda)
- **Libera:** T-111
- **Áreas que pode alterar:** `tests/Game.NetBots/` (novo), `MmoRpg.sln`

## Contexto

`Game.Bots` já joga os encontros em processo (simulador). Para medir o servidor é preciso de clientes de rede de verdade, sem Godot, rodando na VPS ou em outra máquina.

## Requisitos

1. Console `tests/Game.NetBots`: abre N conexões (`--bots=N --server=host:porta --class=...`), faz o `Hello` e joga usando as estratégias de `Game.Bots` sobre o estado recebido (montado com a fábrica de snapshots da T-101).
2. Métricas por bot e agregadas: ping, perda, bytes recebidos/s, intenções enviadas/s, desconexões.
3. Cenário de carga: 4, 16 e 40 bots em salas (o servidor pode abrir várias salas se a T-105 permitir; senão, várias instâncias de servidor) por 5 minutos; registrar no Resultado o tempo médio/p99 de tick do servidor e a banda.

## Critérios de aceite

- 4 bots completam o encontro da Matriarca contra o servidor real pelo menos uma vez;
- relatório de carga no Resultado.

## Resultado

Feito no Windows (a tarefa é de VPS). As medições abaixo são deste PC (i5-8600, servidores **e** bots na mesma máquina). Para medir o servidor sozinho, rode o `load.sh` na VPS. Como usar: `tests/Game.NetBots/README.md`.

**`tests/Game.NetBots` (console, LiteNetLib 2.1.4):**
- `--bots N --server host:porta[,...] --class <id|mix> --strategy --duration --until-end`.
- Cada bot tem sua própria conexão (o servidor distingue jogadores pelo endereço de origem). Faz o `Hello`, monta o estado com os deltas da T-106 (`SnapshotReconstructor`) e confirma com `SnapshotAck`.
- A cada snapshot novo, espelha o estado num `World` (**`World.Mirror`**, novo no Core) e roda a estratégia do `Game.Bots` como no simulador.
- **Métricas** a cada 10 s e no fim, por bot e no total: KB/s recebidos, intenções/s, ping (`Ping`/`Pong`), perda (ticks que faltam), quedas, vitória/derrota com o tempo.
- **`load.sh <bots> [s]`**: sobe ⌈bots/4⌉ instâncias do servidor (uma sala de 4 vagas cada, portas 7800+), roda os bots e resume o tick dos servidores (médio, p99, máximo, pulados).

**Achado importante:** os bots perdiam sempre pela rede, mesmo sem atraso.
- **Causa:** o `TelegraphHazardSource` do `Game.Bots` descobre os telegráfos pelos **eventos**, e um mundo espelhado não tem eventos. Os bots nunca desviavam das investidas e poças da Matriarca.
- **Correção:** `SnapshotHazardSource` (no NetBots) lê os telegráfos do **estado** (`TelegraphState`).
- **Medido fora da rede, 20 seeds:**
  - bots lendo o mundo real: 19/20 vitórias;
  - espelho com os eventos: 0/20;
  - espelho com os perigos do estado: **19/20** sem atraso, 16/20 com 2 ticks e 12/20 com 4 ticks de atraso (o atraso pesa como para um jogador com lag).

**Critério "4 bots completam a Matriarca":** atingido. Contra o servidor real (UDP no loopback) os 4 bots venceram nas duas tentativas, em **273 s e 196 s**, com ~6 KB/s por bot, ping de ~45 ms, 0% de perda e nenhuma queda. Na carga de 16 bots as 4 salas venceram.

**Relatório de carga** (Matriarca, `kite+dodge`, 5 min por cenário, 1 servidor por sala de 4):

| Cenário | Servidores | Tick médio | p99 médio (pior janela) | Máx | Pulados | Banda por bot | Banda total | Ping | Perda | Quedas | Resultado das salas |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 4 bots | 1 | 0,87 ms | 9,7 ms (32 ms) | 48 ms | 0 | 5,5 KB/s | 22 KB/s | 28 ms | 0% | 0 | sem fim em 5 min (sala lenta nesta seed) |
| 16 bots | 4 | 0,92 ms | 9,7 ms (30 ms) | 66 ms | 0 | 4,7 KB/s | 74 KB/s | 40 ms | 0% | 0 | 4/4 vitórias |
| 40 bots | 10 | 0,85 ms | 8,6 ms (45 ms) | 97 ms | 0 | 4,2 KB/s | 168 KB/s | 34 ms | 0% | 0 | 8 vitórias, 2 derrotas |

(Banda por bot cai com o tempo de cada sala: depois da vitória fica só o estado parado, ~0,7 KB/s.)

**Leitura:**
- O tick médio de ~0,9 ms por sala (orçamento de 50 ms) não muda com o número de salas.
- O p99 de ~9 ms e os picos (até 97 ms com 40 bots) acontecem com 10 servidores e 40 clientes LiteNetLib (cada um com suas threads) disputando 6 núcleos. Mesmo assim **nenhum tick foi pulado**: o `TickLoop` recuperou dentro do limite.
- A banda ficou em 4–6 KB/s por jogador, dentro da meta da T-106.
- A rodada de verdade é na VPS: o `load.sh` na VPS para o servidor, e os bots de outra máquina ou da própria VPS.

**Fora das áreas listadas (mínimo):**
- `src/Game.Core/Simulation/World.cs`: `World.Mirror(snapshot)` (+ `tests/Game.Core.Tests/WorldMirrorTests.cs`). As estratégias do `Game.Bots` leem um `World` com as mesmas ids e o mesmo tick do servidor, e o `World` não tinha como criar entidade com id dada nem ajustar o tick.
- `src/Game.Server/TickStats.cs` e `Program.cs`: o log de 10 s ganhou o **p99** do tick (a tarefa pede médio e p99).

**Pendências:**
- O cliente Godot não usa bots de rede. O `TelegraphHazardSource` continua certo para o simulador e o cliente local, que têm eventos.
- Várias salas por processo não existem: a carga usa várias instâncias. Se a fase 2 quiser várias zonas por processo, isso entra na sala.
