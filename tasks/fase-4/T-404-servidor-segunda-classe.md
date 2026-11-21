# T-404 — Servidor e protocolo: segunda classe gravada e replicada

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** T-403
- **Libera:** T-405, T-410, T-411
- **Áreas que pode alterar:** `src/Game.Protocol/` (intenção e componente novos, `ProtocolVersion`), `src/Game.Server/` (persistência do personagem), `src/Game.Api/` (só se a lista de personagens precisar mostrar a segunda classe), `src/Game.Bots/` (só se a intenção nova quebrar algo), `tests/`

## Contexto

A T-403 criou no Core a escolha da segunda classe (`ChooseSecondClassIntent`), o campo da segunda classe em `CharacterBuild` e o componente replicado. Falta levar isso pela rede e gravar.

- As intenções viajam em `NetIntent` (`src/Game.Protocol/NetIntent.cs`, `NetIntentKind`), convertidas por `NetIntent.From` e `ToIntent`.
- Componentes replicados têm codec em `src/Game.Protocol/ComponentCodecs.cs`. Tipo novo = tag nova e `ProtocolVersion.Current` + 1 (hoje **15**).
- O personagem é gravado como documento JSON (`Character.Progression`, jsonb) com versão (`ProgressionVersion`), escrito e lido por `CharacterDocument` (`src/Game.Server/Persistence/CharacterState.cs`). **Não precisa de migração de banco**: só um campo novo no documento e a versão do documento + 1, lendo os documentos antigos (sem segunda classe).

## Requisitos

1. `NetIntentKind.ChooseSecondClass`, com o id da segunda classe em `AbilityId` (ou campo próprio), e conversão nos dois sentidos.
2. Codec do componente (ou dos campos novos de `CharacterProgress`) que leva a segunda classe ao cliente. `ProtocolVersion.Current` + 1.
3. `CharacterDocument` grava e lê a segunda classe; documentos da versão anterior continuam abrindo, sem segunda classe. Personagem salvo com dados inválidos (segunda classe que não existe mais) abre sem a segunda classe e com os pontos dela devolvidos, com aviso no log, como o tratamento de progressão corrompida que já existe.
4. A escolha grava o personagem logo (como uma subida de nível), para não perder uma decisão definitiva numa queda do servidor.
5. Se a API devolve a lista de personagens com nível e classe, incluir a segunda classe (id) na resposta, para a seleção de personagem poder mostrar depois. Se isso mexer em contrato público, registrar no Resultado.

## Integração

**Não integre a T-404 na `main` sozinha.** Ela sobe `ProtocolVersion`, e o próximo `deploy-dev.sh` subiria um servidor que o cliente publicado não aceita. A T-405 sai da branch desta; integre **T-404, T-405 e T-411 juntas** e, na mesma rodada, rode o `deploy-dev.sh` e publique o cliente novo (`export-client.ps1` e `deploy-download.ps1`).

## Critérios de aceite

- teste de servidor: personagem de nível 20 escolhe a segunda classe pela rede, sai, volta e continua com ela; um segundo pedido é recusado;
- teste de documento: um documento da versão anterior abre sem segunda classe; o round trip mantém a segunda classe;
- teste de protocolo: a intenção e o componente novos fazem round trip;
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
