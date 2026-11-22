# T-404 — Servidor e protocolo: segunda classe gravada e replicada

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** T-403
- **Libera:** T-405, T-410, T-411
- **Áreas que pode alterar:** `src/Game.Protocol/` (intenção e componente novos, `ProtocolVersion`), `src/Game.Server/` (persistência do personagem), `src/Game.Api/` (só se a lista de personagens precisar mostrar a segunda classe), `src/Game.Bots/` (só se a intenção nova quebrar algo), `tests/`
- **Branch:** sai de `trilha/segundas-classes` e é integrada nela, não na `main` (seção 6f de `tasks/README.md`).

## Contexto

A T-403 criou no Core a escolha da segunda classe (`ChooseSecondClassIntent`), o campo da segunda classe em `CharacterBuild` e o componente replicado. Falta levar isso pela rede e gravar.

- As intenções viajam em `NetIntent` (`src/Game.Protocol/NetIntent.cs`, `NetIntentKind`), convertidas por `NetIntent.From` e `ToIntent`.
- Componentes replicados têm codec em `src/Game.Protocol/ComponentCodecs.cs`. Tipo novo = tag nova e `ProtocolVersion.Current` + 1 (15 na `main` e na trilha; a T-406, ainda fora da trilha, já usa 16).
- O personagem é gravado como documento JSON (`Character.Progression`, jsonb) com versão (`ProgressionVersion`), escrito e lido por `CharacterDocument` (`src/Game.Server/Persistence/CharacterState.cs`). **Não precisa de migração de banco**: só um campo novo no documento e a versão do documento + 1, lendo os documentos antigos (sem segunda classe).

## Requisitos

1. `NetIntentKind.ChooseSecondClass`, com o id da segunda classe em `AbilityId` (ou campo próprio), e conversão nos dois sentidos.
2. Codec do componente (ou dos campos novos de `CharacterProgress`) que leva a segunda classe ao cliente. `ProtocolVersion.Current` + 1.
3. `CharacterDocument` grava e lê a segunda classe; documentos da versão anterior continuam abrindo, sem segunda classe. Personagem salvo com dados inválidos (segunda classe que não existe mais) abre sem a segunda classe e com os pontos dela devolvidos, com aviso no log, como o tratamento de progressão corrompida que já existe.
4. A escolha grava o personagem logo (como uma subida de nível), para não perder uma decisão definitiva numa queda do servidor.
5. Se a API devolve a lista de personagens com nível e classe, incluir a segunda classe (id) na resposta, para a seleção de personagem poder mostrar depois. Se isso mexer em contrato público, registrar no Resultado.

## Integração

**Não integre a T-404 na trilha sozinha.** Ela sobe `ProtocolVersion`. A T-405 sai da branch desta. **T-406, T-407, T-404, T-405 e T-411 entram juntas** na `trilha/segundas-classes` (a T-406 também sobe o protocolo), para a trilha nunca ter servidor e cliente de versões diferentes. Veja os cuidados de integração na seção 6f de `tasks/README.md`.

## Critérios de aceite

- teste de servidor: personagem de nível 20 escolhe a segunda classe pela rede, sai, volta e continua com ela; um segundo pedido é recusado;
- teste de documento: um documento da versão anterior abre sem segunda classe; o round trip mantém a segunda classe;
- teste de protocolo: a intenção e o componente novos fazem round trip;
- `./scripts/check.sh` completo passa.

## Resultado

Concluída em 2026-11-21, na VPS, branch `task/T-404-servidor-segunda-classe` (saída de `task/T-403-core-segunda-classe`, `5dfcf5a`). Não entra na trilha sozinha: vai na rodada do protocolo (seção 6f).

### Formato das mensagens novas (para a T-405, a T-410 e a T-411)

| O quê | Formato |
|---|---|
| Intenção | `NetIntentKind.ChooseSecondClass` (último valor do enum, depois de `Sell`), com o id da segunda classe em `NetIntent.AbilityId`: `NetIntent.From(new ChooseSecondClassIntent(eu, "second_class.guardian"))`. `ToIntent` devolve null com id vazio ou ausente. A entidade é sempre a da conexão. |
| Resposta | evento `SecondClassChosen(Entity, SecondClassId, AttributePoints)` (tag 96, da T-403) se aceitou; `ProgressionRejected(Entity, Id, Reason)` se recusou, com `Id` = segunda classe pedida e `Reason` em `SecondClassAlreadyChosen`, `NotASecondClass`, `LevelTooLow` ou `InCombat`. |
| Componente | `CharacterProgress` (tag 15, replicado como antes: quem vê a entidade recebe) ganha `SecondClass` (string opcional, gravada por último, depois de `AllocatedVit`); null = ainda não escolheu. |
| Documento do personagem | `CharacterDocument.Version` **2 → 3**, campo `"secondClass": "second_class.guardian"` (ausente = não escolheu). Versões 1 e 2 continuam abrindo, sem segunda classe. Sem migração de banco. |
| API | `GET /characters` (e o `201` de `POST /characters`) ganha `secondClass` (string ou null) em cada personagem. |
| `ProtocolVersion` | **16 → 17** |

### O que foi feito

- **Protocolo:** intenção e campo do componente acima; `ProtocolVersion.Current = 17` com o comentário da versão. Testes em `tests/Game.Protocol.Tests/ProtocolTests.cs`: a intenção entra no round trip das intenções (e o id vazio é recusado) e `Character_progress_round_trips_the_second_class` faz o round trip do componente com segunda classe (o teste antigo confere que sem escolha vem null).
- **Gravação:** `CharacterDocument` escreve e lê `secondClass` (v3). `SecondClassChosen` grava o personagem logo, como `LevelUp`, no mundo aberto (`WorldServer`) e na sala de encontro (`ZoneRoom`), motivo "segunda classe".
- **Segunda classe que não existe mais:** ao carregar, antes do `Leveling.Validate`, `LoadedCharacter.WithoutMissingSecondClass` (que usa `CharacterState.WithoutMissingSecondClass`) tira a segunda classe que não é mais da classe base, com aviso no log (`Warning`): saem os NH das habilidades que a base não aprende (pontos de habilidade de volta) e, se os atributos distribuídos passam do que o nível dá sem os +5, **todos** os pontos de atributo voltam (não há como saber quais eram os da segunda classe). O personagem entra; o próximo salvamento grava o documento corrigido. Vale nos dois servidores.
- **API:** `CharacterSummary.SecondClass`, lido do jsonb (`CharacterSummary.SecondClassOf`) só em documentos v3 em diante; documento antigo ou ilegível dá null, sem quebrar a lista.
- **Testes:** `tests/Game.Server.Tests/SecondClassServerTests.cs` — nível 20 escolhe pela rede, grava na hora (ainda conectado), o cliente vê `CharacterProgress.SecondClass`, um segundo pedido é recusado (`SecondClassAlreadyChosen`), sai, outro processo carrega do banco com a segunda classe e recusa de novo; nível 19 é recusado e não grava; a sala de encontro também grava na hora; segunda classe inexistente entra sem ela e com os pontos de volta (mundo e sala) e o documento é regravado corrigido; documento v3 faz round trip, v1 e v2 abrem sem segunda classe, v4 é recusado. `tests/Game.Api.Tests/CharacterSummaryTests.cs` para a lista. `FakeStore.Add` ganhou `level` e `progressionVersion`; os testes que esperavam `ProgressionVersion == 2` passaram a usar `CharacterDocument.Version`.

### Desvios e decisões

- **Contrato público da API:** campo novo `secondClass` na resposta de personagem. É aditivo: o cliente atual (`client/scripts/Account/ApiClient.cs`) ignora campos que não conhece; a T-410 só precisa acrescentar `string? SecondClass` ao record do cliente.
- O campo do componente foi acrescentado ao `CharacterProgress` existente (mesma tag 15), em vez de um componente novo: o cliente já recebe esse componente e é o que a T-403 deixou replicável.
- O saneamento da segunda classe fica no servidor, na carga (não no `CharacterDocument.Read`, que não conhece os dados). Só trata a segunda classe que não existe mais; outros builds inválidos continuam recusados como antes (`Unavailable`).
- `Game.Bots` não precisou mudar.
- Os testes com PostgreSQL real (`PostgresAccountTests`, parte de `Game.Api.Tests`) foram pulados, como sempre nesta VPS sem banco de teste; `PostgresAccountTests` já confere `CharacterDocument.Version`.

### Pendências

- Rodada do protocolo (T-403, T-406, T-407, T-404, T-405, T-411): a T-406 também usa 16; o número final sai na integração.
- T-405: desfazer a segunda classe pela ferramenta de teste (a escolha grava na hora; desfazer também deveria gravar).
- T-410: mostrar a segunda classe na seleção de personagem (`secondClass`) e mandar `ChooseSecondClass` pela tela de habilidades.

