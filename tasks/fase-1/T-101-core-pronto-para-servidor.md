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

Concluída em 2026-10-18 no Windows (a tarefa é de VPS; feita aqui a pedido do designer — só `dotnet` e testes).

**Feito:**
1. **Replicável × interno**: interface marcadora `IReplicated` (`Simulation/IComponent.cs`). Replicáveis: `Transform`, `Health`, `FactionTag`, `ResourcePool`, `Movement`, `Collider`, `UnitInfo`, `AbilityState`, `ShieldState`, `StatusEffects`, `TelegraphState`, `AreaState`, `PhaseState`. Internos (só servidor): `AiState`, `ThreatTable`, `ResourceState`, `SummonedBy` — nenhum é lido pelo cliente nem pelos bots.
2. **Snapshot por escopo**: `World.Snapshot(SnapshotScope.Full | Replicated)` (padrão `Full`, para testes); o cliente (`LocalAuthority`, `FakeAuthority`) passou a usar `Replicated`. Alocação reduzida:
   - a ordem dos componentes de cada entidade fica em cache e só é recalculada quando um componente entra ou sai (antes: `OrderBy` + lista a cada snapshot e a cada hash);
   - `EntitySnapshot` guarda um array (busca linear) em vez de um dicionário por entidade;
   - `FactionTag` (imutável) não é mais clonado.
   - **Medição** (Matriarca, 8 entidades, Debug, `ServerReadinessTests`): snapshot completo **18 167 → 9 488 B** (−48%); snapshot replicado **7 288 B** (−60% em relação ao antigo). Alocação por tick inalterada (4 892 B: vem dos sistemas, ver pendências).
   - "Copiar só o que mudou" ficou fora: os componentes são alterados por campos públicos em todo o Core, sem ponto único para marcar "sujo"; o delta por cliente fica na T-106 (comparando com o último snapshot confirmado).
3. **Fábrica pública**: `EntitySnapshot.Create(id, componentes)` (ordena pelo nome do tipo, recusa tipo repetido) e `EntitySnapshot.Components` (para a serialização da T-103). `IGameAuthority` não mudou.
4. **Impressão digital do conteúdo**: `ContentFingerprint.Compute` em `Game.Data` (SHA-256 do JSON canônico + nome do arquivo, 16 hex; independe de espaços, comentários, CRLF/LF, ordem dos arquivos e separador de pasta), exposta em `GameDatabase.Fingerprint` e copiada para o novo `ContentCatalog.Fingerprint` por quem converte (simulador, cliente, testes).
5. **Vários mundos no mesmo processo**: teste com duas Matriarcas (seeds 11 e 22) avançando intercaladas por 400 ticks dá exatamente os mesmos hashes de estado que rodando separadas — não há estado estático compartilhado que mude com o jogo.
6. **Orçamento de tick**: Matriarca com 4 jogadores bots (`kite+dodge`), Release, 1 thread, i5-8600: **0,065 ms por tick** (128 216 ticks em 8,3 s, incluindo bots e métricas do simulador) — ~15× abaixo da meta de 1 ms; deve sobrar margem mesmo numa VPS 3–4× mais lenta (medir lá na T-105).

**Testes novos:** `tests/Game.Core.Tests/ServerReadinessTests.cs` (escopo replicado, fábrica, mundos intercalados, impressão digital do conteúdo real, medição de alocação) e `tests/Game.Data.Tests/ContentFingerprintTests.cs`.

**Fora das áreas listadas:** `src/Game.Data/` (`ContentFingerprint`, `GameDatabase.Fingerprint`, cálculo no `GameDataLoader`) e `tests/Game.Data.Tests/` — a impressão digital precisa dos JSON de origem, que só o `Game.Data` vê; `tests/Game.Sim/ContentLoading.cs` e `client/` só copiam o valor e trocam o escopo do snapshot.

**Pendências:** alocação por tick dos sistemas (LINQ em IA/consultas espaciais, ~4,9 KB/tick na Matriarca) — irrelevante no orçamento atual; reavaliar com a carga da T-109.
