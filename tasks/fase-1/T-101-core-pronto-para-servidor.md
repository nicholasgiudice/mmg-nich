# T-101 — Core pronto para servidor: estado replicável e snapshots leves

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** fase 0 concluída (T-025)
- **Libera:** T-103, T-105
- **Áreas que pode alterar:** `src/Game.Core/`, `tests/Game.Core.Tests/`, `tests/Game.Sim/` (só se uma API mudar), `client/` (só para acompanhar mudança de API)

## Contexto

Na fase 0 o Core roda dentro do cliente e `World.Snapshot()` clona **todos** os componentes de **todas** as entidades a cada tick. No servidor (fase 1) o estado de cada zona será enviado para vários jogadores a 20 Hz; isso exige saber o que é replicável e produzir o estado sem alocar o mundo inteiro por tick. Além disso, `EntitySnapshot` só pode ser construído dentro do Core (construtor `internal`), o que impede o cliente de rede de montar o estado recebido. Leia `tech/stack-proposal-v0.1.md` (seção 4, Rede) e o Resultado da T-020.

## Requisitos

1. **Replicável × só do servidor**: marcar cada componente (atributo ou interface) como replicável para o cliente ou interno. Hoje: replicáveis `Transform`, `Health`, `FactionTag`, `ResourcePool`, `Collider`, `UnitInfo`, `AbilityState` (recargas, conjuração, cargas), `StatusEffects`, `ShieldState`, `TelegraphState`, `AreaState`, `PhaseState`, `Movement` (direção, para a predição); internos `AiState`, `ThreatTable`, `ResourceState`, `SummonedBy` (confirmar).
2. **Snapshot replicável**: `World.Snapshot()` (ou um novo método) gera só os componentes replicáveis. Medir e reduzir alocação por tick (ex.: reaproveitar buffers, copiar só entidades que mudaram — "sujo por componente" se for simples). Registrar no Resultado as alocações por tick antes/depois no encontro da Matriarca.
3. **Construção pública de snapshots**: fábrica pública (`EntitySnapshot.Create(id, componentes)` ou `WorldSnapshotBuilder`) para o cliente de rede montar o estado recebido. `IGameAuthority` não muda.
4. **Impressão digital do conteúdo**: `ContentCatalog` expõe um hash estável dos dados carregados (mesmos JSON = mesmo hash em Windows e Linux), para o servidor recusar cliente com dados diferentes.
5. **Vários mundos no mesmo processo**: garantir (com teste) que dois `World` independentes avançando intercalados produzem o mesmo hash de estado que rodando separados (nenhum estado estático compartilhado que mude com o jogo).
6. **Orçamento de tick**: teste/medição do tempo de um tick com o encontro da Matriarca + 4 jogadores bots (meta de referência: < 1 ms em média na VPS); registrar no Resultado.

## Fora do escopo

Rede, serialização binária (T-103), delta por jogador (T-106).

## Critérios de aceite

- cliente local (LocalAuthority) continua funcionando sem mudança visível;
- testes novos para 2, 3, 4 e 5;
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
