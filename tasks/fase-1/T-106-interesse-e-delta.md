# T-106 — Interest management por grade e snapshots delta

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-105
- **Libera:** T-109, T-111
- **Áreas que pode alterar:** `src/Game.Server/`, `src/Game.Protocol/`, testes correspondentes

## Contexto

`tech/stack-proposal-v0.1.md`: snapshots delta por entidade e interest management por grade — o jogador recebe só entidades próximas; meta de poucos KB/s por jogador com ~50 entidades visíveis. As arenas do protótipo são pequenas, mas os mapas da fase 3 não.

## Requisitos

1. **Grade de interesse** no servidor (célula configurável, ex.: 16 tiles; raio de interesse ex.: 2 células): cada jogador recebe só as entidades das células próximas, mais as que sempre importam (o próprio grupo, chefes do encontro).
2. **Entrada e saída** do interesse: mensagens de criação/remoção de entidade no cliente (o cliente não pode "perder" entidade sem saber).
3. **Delta por cliente**: envia só componentes que mudaram desde o último snapshot **confirmado** por aquele cliente (ack de tick); snapshot completo periódico ou quando o ack fica velho demais.
4. Eventos também filtrados por interesse (eventos de entidades fora do alcance não vão).
5. Medição: bytes/s por jogador no encontro da Matriarca e num teste sintético com 100 entidades espalhadas; registrar no Resultado.

## Critérios de aceite

- teste: cliente que anda até o fim de um mapa sintético recebe e remove entidades corretamente;
- teste: aplicar a sequência de deltas reproduz o mesmo estado que o snapshot completo (com perda simulada de pacotes);
- banda por jogador medida e registrada;
- `./scripts/check.sh` completo passa.

## Resultado

Feito no Windows (a tarefa é de VPS; feita aqui a pedido do designer). **Protocolo v2.**

**Protocolo (`Game.Protocol`):**
- `SnapshotDelta(Epoch, Tick, BaseTick, Entities, Removed)`: `BaseTick` 0 = completo (o cliente descarta o que tinha). Cada `EntityDelta` é `Full` (entrou no interesse: todos os componentes) ou só os componentes que mudaram, mais as tags dos que sumiram. `Removed` = entidades que saíram do interesse ou do mundo. `EntityDelta` tem formato compacto próprio (id e contagens em varint, um byte de flags): o formato padrão do MemoryPack custava ~14 B de cabeçalho por entidade por tick.
- `SnapshotAck(Epoch, Tick)`: cliente → servidor, pode ir no canal não confiável.
- `Welcome` ganhou `Epoch`: muda a cada reinício da sala (ids e ticks recomeçam); delta de outra época é ignorado.
- `SnapshotReconstructor`: o lado do cliente (vai servir à T-107 e à T-109). Guarda os últimos 64 estados, aplica cada delta sobre a base confirmada e ignora delta repetido, fora de ordem, de outra época ou com base desconhecida.
- `ProtocolSerializer.SerializeComponent`: bytes de um componente, que o servidor usa para comparar estados. `SnapshotMessage` continua no protocolo para ferramentas e testes, mas o servidor não manda mais.

**Servidor (`Game.Server/Interest`):**
- `InterestGrid`: uma vez por tick, as entidades vão para células de 16 tiles. Cada jogador vê as células a até 2 da sua (área de 80×80 tiles) e mais as que sempre importam: o grupo (facção dos jogadores) e os chefes (`MonsterRank.Boss`). A posição usada é a da entidade do jogador; sem entidade (removida), a última conhecida. Opções: `--interest-cell`, `--interest-radius`.
- `ClientView` (por jogador): guarda o que foi mandado em cada tick recente e o último tick confirmado. O delta é sempre **contra o tick confirmado** (modelo do Quake 3). Pacote perdido não quebra nada: o próximo delta continua sobre a mesma base e traz tudo de novo. Sem confirmação, ou com a confirmada mais velha que `--max-delta-age` (40 ticks = 2 s), vai completo. A comparação é por bytes de cada componente, serializado uma vez por tick para todos.
- **`Movement` só vai para o dono** (é da predição da T-108). O cliente não usa o `Movement` das outras entidades (conferido no código), e isso tirou ~10 KB/s no teste de estresse.
- Eventos filtrados: um evento vai se cita alguma entidade que o jogador vê agora ou tinha no último envio (morreu, sumiu neste tick). Evento sem entidade vai para todos. As entidades citadas são lidas por reflexão uma vez por tipo (`EventEntities`): evento novo do Core entra sem código.

**Banda por jogador (tudo: deltas, eventos e acks):**

| Cenário | KB/s por jogador | Comparação |
|---|---|---|
| Matriarca (1 jogador + 3 bots lutando, 60 s, transporte em memória) | **4,7** (deltas 3,4) | o completo a cada tick seria 78 KB/s |
| Matriarca, 4 clientes por **UDP real** no loopback (25 s) | **~3,9** só de deltas | snapshots fragmentados: de 1342 (T-105) para 14, só os completos iniciais |
| 100 ratos espalhados em 200×200, jogador andando (30 s) — **pior caso**: os ratos perseguem e se juntam em volta dele (até 63 visíveis, ~40 correndo a cada tick) | **25** | o completo a cada tick seria 370 KB/s. Detalhe: `Transform` 13 KB/s, eventos 5 KB/s, `AbilityState` 2 KB/s |

O tick com 4 jogadores por UDP (deltas por jogador) ficou em **0,37 ms** de média.

**Testes:**
- `tests/Game.Server.Tests/InterestDeltaTests.cs`:
  - corredor de 220 tiles: o jogador anda até o fim e, em mais de 1000 ticks, o que o cliente tem é sempre exatamente o interesse do servidor. Viu os 35 bonecos, mais de 25 saíram, nunca o mapa inteiro.
  - **deltas com 30% de perda de snapshots e 50% de perda de confirmações** reproduzem o estado completo esperado, byte a byte, em 557 ticks comparados.
  - sem confirmações, tudo vai completo depois de 2 s, e com confirmações os deltas ficam 12× menores.
  - eventos de entidade fora do interesse não chegam.
  - as duas medições de banda acima.
- `tests/Game.Protocol.Tests/SnapshotDeltaTests.cs`: formato do delta, entrada e saída de entidades, perda do tick intermediário, época e base desconhecida ignoradas, histórico limitado.
- `ZoneRoomTests` passou a usar clientes que montam o estado com os deltas.

**A meta "poucos KB/s com ~50 entidades visíveis":** atingida no encontro real. No estresse sintético (40 entidades mudando a cada tick) ficou em 25 KB/s. Dá para cair mais, mas cada caminho tem custo:
1. Posição em ponto fixo: `Transform` de 17 para ~9 B, mas com perda de precisão, que mexe na reconciliação da T-108.
2. Frequência menor para entidades longe (10 Hz a partir de uma célula): o estado deixa de ser idêntico ao completo a cada tick.
3. Ids de habilidade/status como índice do conteúdo nos eventos: os dois lados têm o mesmo fingerprint.

Recomendo decidir com a carga real da T-109.

**Pendências:** chefes e o grupo sempre visíveis mesmo muito longe (de propósito: o HUD mostra o grupo).
