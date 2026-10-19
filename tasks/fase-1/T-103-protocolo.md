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

_(preencher ao concluir)_
