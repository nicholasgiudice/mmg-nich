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

_(preencher ao concluir)_
