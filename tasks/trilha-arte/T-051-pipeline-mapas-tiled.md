# T-051 — Pipeline de mapas: Tiled → Core

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** —
- **Libera:** T-052, T-055
- **Áreas que pode alterar:** `src/Game.Core/Spatial/`, `src/Game.Data/`, `data/maps/` (novo), `tests/`, `design/` (convenções)

## Contexto

`tech/stack-proposal-v0.1.md` (Mapas): **Tiled** é a fonte de verdade, exportando JSON; o servidor e os testes leem colisão, spawns e áreas de evento do mesmo arquivo que o cliente desenha. Hoje as arenas são ASCII em `data/arenas.json`. Esta é a tarefa "convenções de camadas do Tiled e carregador de mapa no Core" da fase 3, antecipada.

## Requisitos

1. Convenções (documento em `design/map-conventions-v0.1.md`): camadas de tiles (chão, decoração, paredes), camada de **colisão**, camada de objetos para **spawns** (jogadores, monstros com id), **áreas de evento** (altares, selos, teias) e **transições de zona**; tile de 32 px = 1 unidade.
2. Carregador em `Game.Data` (JSON do Tiled → `GridMap` + spawns + áreas), validado com mensagens claras (camada faltando, monstro desconhecido).
3. Encontros podem apontar para um mapa do Tiled em vez de uma arena ASCII; as 5 arenas atuais convertidas para `data/maps/*.tmj` com o mesmo resultado no simulador (hash de estado ou taxas iguais).
4. Testes do carregador e da conversão.

## Critérios de aceite

- arenas atuais rodando a partir de mapas do Tiled, com os testes de design iguais;
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
