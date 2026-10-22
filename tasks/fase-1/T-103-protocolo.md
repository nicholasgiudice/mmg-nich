# T-103 — `Game.Protocol`: mensagens, serialização e versão

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-101
- **Libera:** T-105, T-107, T-109
- **Áreas que pode alterar:** `src/Game.Protocol/` (novo), `tests/Game.Protocol.Tests/` (novo), `MmoRpg.sln`, `Directory.Packages.props` ou `.csproj` (pacotes)

## Contexto

Cliente e servidor trocam **intenções** (cliente → servidor) e **estado + eventos** (servidor → cliente), como previsto em `tech/stack-proposal-v0.1.md` (seção 4, Rede): canal não confiável para snapshots de posição, confiável e ordenado para eventos. Serialização binária gerada em tempo de compilação (MemoryPack ou MessagePack-CSharp). `Game.Protocol` referencia `Game.Core`, nunca o contrário.

## Requisitos

1. Projeto `src/Game.Protocol` (biblioteca, `$(SharedTargetFramework)`), com MemoryPack (ou MessagePack se MemoryPack não funcionar com o Godot .NET — testar no cliente e registrar).
2. Mensagens mínimas:
   - cliente → servidor: `Hello` (versão do protocolo, hash do conteúdo da T-101, nome de desenvolvimento, classe desejada), `IntentMessage` (sequência, tick do cliente, `MoveIntent`/`StopIntent`/`UseAbilityIntent`), `Ping`;
   - servidor → cliente: `Welcome` (id da entidade controlada, tick atual, encontro/arena), `Reject` (motivo: versão, conteúdo, sala cheia), `Snapshot` (tick, entidades replicáveis), `Events` (tick, lista de `GameEvent` replicáveis), `Pong`, `IntentAck` (última sequência aplicada, para a predição da T-108).
3. Serialização de todos os componentes replicáveis e de todos os `GameEvent` publicados hoje (incluir um teste que falha se surgir um tipo de evento novo sem serializador).
4. **Versão do protocolo** (inteiro) e regra de compatibilidade documentada no próprio projeto.
5. Testes de ida e volta (serializar → desserializar → igual) para cada mensagem e para um snapshot real da Matriarca; tamanho em bytes do snapshot completo registrado no Resultado.

## Critérios de aceite

- `Game.Core` não referencia `Game.Protocol` nem pacote de rede;
- o cliente Godot compila referenciando `Game.Protocol`;
- `./scripts/check.sh` completo passa.

## Resultado

Feito no Windows (a tarefa é de VPS; feita aqui a pedido do designer).

**Projeto `src/Game.Protocol`** (`$(SharedTargetFramework)`, referencia só o `Game.Core`), com **MemoryPack 1.21.4**.
- `Messages.cs`: união `INetMessage` com tags fixas (0–8). Cliente → servidor: `Hello` (versão, fingerprint do conteúdo, nome, classe), `IntentMessage` (sequência, tick do cliente, `NetIntent`), `Ping`. Servidor → cliente: `Welcome` (entidade, tick, encontro, arena), `Reject` (`ProtocolVersion` | `Content` | `Full` | `BadRequest` + texto), `SnapshotMessage` (tick + entidades), `EventsMessage` (tick + eventos), `Pong` (tempo do cliente + tick do servidor), `IntentAck` (última sequência aplicada + tick).
- `NetIntent`: `Move` / `Stop` / `UseAbility` **sem a entidade** — o servidor usa sempre a entidade da conexão (um cliente não manda intenção por outro). Direção não finita e habilidade vazia viram intenção inválida (`null`).
- `ProtocolSerializer`: ponto único de (des)serialização; `TryDeserialize` para bytes vindos da rede (lixo não lança).
- **Tipos do Core sem atributos do MemoryPack** (o Core não conhece a rede): `ComponentCodecs` (13 componentes replicáveis) e `EventCodecs` (33 eventos) escrevem campo a campo, com **tag fixa por tipo**, registrados como formatadores do MemoryPack (`EntitySnapshot`, `GameEvent`). Inteiros e ids em varint; listas limitadas a 4096 itens na leitura; enum fora do intervalo é erro.
- **Versão:** `ProtocolVersion.Current = 1`. Regra no próprio arquivo: cliente e servidor precisam da mesma versão **e** do mesmo fingerprint de `data/`; qualquer mudança de bytes (mensagem, campo, tag) sobe a versão; tags nunca são reaproveitadas; mudar números de `data/` não sobe (o fingerprint pega).

**Tamanhos (Matriarca, 4 jogadores):** snapshot completo no início **1 594 B** (5 entidades); o maior em 60 s de luta **4 693 B** (com invocações, áreas, telegráfos, escudos e status). Eventos: 2 737 em 60 s = 114 756 B (~1,9 KB/s, ~42 B por evento). No Godot (Mago, ~1 s de partida): snapshot 2 425 B, 9 entidades.

**MemoryPack no Godot:** testado — o cliente referencia o `Game.Protocol`, compila, e com `--protocol-check` serializa e lê de volta um snapshot real dentro do runtime .NET do Godot (`[protocol] v1: snapshot 2425 B, 9/9 entidades de volta`). Não foi preciso o MessagePack.

**Testes** (`tests/Game.Protocol.Tests`, 40): ida e volta de cada mensagem (byte a byte); intenções e "sempre a entidade da conexão"; lixo recusado sem exceção; **todo componente `IReplicated` e todo `GameEvent` do Core têm formato** (falha com o nome do tipo que faltar); cada evento com valores diferentes em cada campo (pega campo trocado ou esquecido); snapshot real da Matriarca (início e o mais variado da luta, com os 13 tipos de componente) comparando o hash de estado de cada componente; todos os eventos de 60 s de luta; o Core não referencia `Game.Protocol` nem MemoryPack/LiteNetLib.

**Fora das áreas listadas (mínimo):**
- `src/Game.Core/Game.Core.csproj`: `InternalsVisibleTo("Game.Protocol")` — `GameEvent.Tick` e `Health.IsAlive` são `internal set` e o protocolo precisa reconstruí-los. O Core continua sem referenciar o protocolo.
- `src/Game.Core/Combat/Components.cs`: `AbilityState.HasRanks` (internal), para não criar o dicionário de NH a cada snapshot serializado.
- `client/MmoRpgClient.csproj` (referência ao `Game.Protocol`, critério de aceite) e `client/scripts/Main.cs` (argumento de depuração `--protocol-check`).

**Pendências / para as próximas:** o tamanho do evento é dominado pelas strings (ids de habilidade/status); se a banda apertar, a T-106 pode trocar ids por índices do conteúdo (os dois lados têm o mesmo fingerprint). Delta de snapshot fica na T-106.
