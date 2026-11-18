# T-391 — Servidor: posição dos membros do grupo para o mini mapa

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** —
- **Libera:** T-393
- **Áreas que pode alterar:** `src/Game.Protocol/`, `src/Game.Server/`, `tests/Game.Protocol.Tests/`, `tests/Game.Server.Tests/`, `src/Game.Server/README.md`

## Contexto

Pedido do designer durante o playtest da fase 3 (T-380), em 2026-11-17: um mini mapa que mostre onde estão os membros do grupo. A T-392 faz o mini mapa no cliente com o jogador e os membros que estão perto; esta tarefa dá ao cliente a posição dos membros distantes, e a T-393 os mostra no mini mapa. É a parte que pode esperar: o mini mapa sai antes, sem ela.

Hoje o cliente só conhece a posição de quem está na sua área de interesse (`src/Game.Server/Interest/`, células de 16 tiles e raio de 2 células). Os mapas chegam a 200 × 160 tiles (`lower_forest.tmj`, `vigil_fields.tmj`), então um membro do grupo do outro lado da Floresta não aparece no snapshot. O `PartyState` (`src/Game.Protocol/PartyMessages.cs`) traz zona, vida e Mana de cada membro, mas não a posição, e só é mandado quando algo muda.

## Requisitos

1. **Mensagem nova** do servidor para o cliente com a posição dos outros membros do grupo **na mesma zona e na mesma cópia de instância** de quem recebe. Cada linha tem o `MemberId` (o mesmo do `PartyState`) e a posição em tiles (`x`, `y`). Membros em outra zona ou em outra cópia ficam de fora (o cliente já sabe a zona deles pelo `PartyState`).
2. **Ritmo:** a cada 10 ticks (2 por segundo), só para quem está num grupo com pelo menos um outro membro na mesma zona. Sem grupo, ou sozinho na zona, não manda nada. Quando o último membro sai da zona, manda uma lista vazia uma vez, para o cliente limpar o mini mapa.
3. **Membro morto** continua na lista, na posição em que caiu, até renascer (o cliente pode mostrar diferente pelo `Alive` do `PartyState`).
4. A mensagem vale também para a sala de encontro? **Não:** o grupo só existe no mundo aberto (T-310), e esta tarefa segue a mesma regra.
5. `ProtocolVersion.Current` sobe de 12 para 13 (tag nova na união de mensagens, sem reaproveitar tag).
6. A posição vem do estado da simulação no servidor, nunca do cliente. Nada muda no Core.

## Critérios de aceite

- testes no `Game.Server.Tests`:
  - dois membros na mesma zona, longe um do outro (fora da área de interesse), recebem a posição um do outro;
  - membro em outra zona não aparece;
  - dois grupos em cópias diferentes da Cripta não veem as posições um do outro;
  - sem grupo não chega mensagem; ao sair da zona chega a lista vazia uma vez;
- teste de ida e volta da mensagem no `Game.Protocol.Tests`;
- `src/Game.Server/README.md` cita a mensagem nova e o ritmo;
- `./scripts/check.sh` completo passa.

## Cuidados de integração

- A T-385 também mexe no `WorldServer` (renascer fora da Cripta): integre uma de cada vez.
- Com `ProtocolVersion` novo, o cliente publicado deixa de entrar no servidor até sair um cliente com a T-393. **Não integre esta tarefa na `main` sozinha**, porque o próximo `deploy-dev.sh` (o da T-385 ou da T-387, por exemplo) subiria o protocolo 13 sem cliente compatível. Ao terminar, faça o commit e deixe a branch para a T-393, que sai dela; as duas entram juntas na `main`.

## Resultado

Concluída em 2026-11-18 na branch `task/T-391-posicao-do-grupo` (com push para o origin, a pedido do humano, porque a T-393 sai dela). **Não está na `main`** e não houve deploy: protocolo 13 sem cliente compatível. Entra junto com a T-393.

### Para a T-393 (contrato da mensagem)

- **Mensagem:** `PartyPositions(PartyMemberPosition[] Members)`, servidor → cliente, em `src/Game.Protocol/PartyMessages.cs`.
- **Tag na união `INetMessage`:** `22` (`[MemoryPackUnion(22, typeof(PartyPositions))]` em `Messages.cs`). O teste de protocolo fixa o primeiro byte em 22.
- **Linha:** `PartyMemberPosition(int MemberId, float X, float Y)`: `MemberId` é o mesmo de `PartyMemberInfo.MemberId` do `PartyState`; `X`/`Y` em tiles (1 unidade = 1 tile; o cliente converte para pixels). Não é mensagem (não está na união), só o tipo da linha.
- **Quem aparece:** só os **outros** membros (quem recebe não vem na lista), na **mesma zona e na mesma cópia** de instância de quem recebe, em ordem de entrada no grupo. Membro em outra zona ou cópia fica de fora (a zona dele está no `PartyState.ZoneId`).
- **Ritmo:** a cada **10 ticks** (`WorldServer.PartyPositionIntervalTicks`, 2 por segundo, quando `tick % 10 == 0`), canal confiável. Só vai para quem está em grupo com pelo menos um outro membro na mesma zona.
- **Lista vazia:** quando quem recebe fica sem ninguém do grupo na zona (o último membro saiu da zona, quem recebe trocou de zona, saiu/foi expulso ou o grupo acabou), chega **uma vez** `PartyPositions` com `Members` vazio; o cliente deve limpar os marcadores do mini mapa. Depois disso não chega nada até haver alguém de novo. Ao trocar de zona, o cliente também pode limpar pelo `Welcome` novo (a primeira mensagem da nova zona chega em até 10 ticks).
- **Morto:** continua na lista, na posição em que caiu, até renascer (renasce na Cidade, então sai da lista de quem não está na Cidade). Use `PartyMemberInfo.Alive` para desenhar diferente.
- **Sala de encontro (`ZoneRoom`):** não manda; grupo só existe no mundo aberto.
- `ProtocolVersion.Current` = **13**.

### O que foi feito

- `src/Game.Protocol`: `PartyMemberPosition`, `PartyPositions` (tag 22), versão 12 → 13 com a nota no histórico.
- `src/Game.Server/Zones/WorldServer.cs`: `SendPartyPositions()` no fim do `Step()` (depois do `SendPartyStates`) e `PartyPositionsFor(member)` (público, para teste). "Mesma zona" é a mesma `OpenZone` (referência), o que já separa as cópias de instância. A posição vem do `Transform` da entidade no mundo do servidor. `Player.SentPartyPositions` guarda se a última mensagem tinha alguém, para a lista vazia ir uma vez só.
- `tests/Game.Server.Tests/PartyPositionTests.cs`: longe na mesma zona (> 3 células, nos Campos) e ritmo de 1 a cada 10 ticks; sem grupo nada chega; outra zona fica de fora e ao sair chega a lista vazia uma vez (para quem ficou e para quem foi); sair do grupo manda a lista vazia uma vez; morto fica onde caiu até renascer; dois grupos em cópias diferentes da Cripta só veem o próprio par.
- `tests/Game.Protocol.Tests/ProtocolTests.cs`: ida e volta (com lista vazia) e tag 22.
- `src/Game.Server/README.md`: item "Posição do grupo" com a mensagem e o ritmo.

### Decisões e desvios

- **A premissa do contexto não vale hoje:** o `InterestGrid` (T-106) trata toda entidade da facção `Player` como sempre relevante, então todo jogador da zona já vai no snapshot, inclusive membros distantes (e jogadores de fora do grupo). A mensagem foi feita como pedido e não depende disso; o teste confere a distância (> 3 células) em vez de "não está no snapshot". Não mexi no interesse (fora das áreas e mudaria o que o cliente recebe). Se um dia o interesse deixar de mandar jogadores distantes, a `PartyPositions` já cobre o mini mapa. **Decisão do humano (2026-11-18): só jogadores do grupo aparecem no mapa** (mini mapa e mapa grande). Isso já vale: a `PartyPositions` só leva membros e o mini mapa da T-392 só desenha membros (`Main.UpdateZoneMap` parte de `net.Party.Others`). O interesse fica como está; a regra foi escrita nos requisitos da T-393.
- Canal confiável (como `PartyState`), para a lista vazia não se perder. São no máximo 3 linhas de 12 bytes, 2 vezes por segundo.
- O envio segue o tick global do `WorldServer` (`_tick`), não o tick da zona, como o `PartyState`.

### Pendências

- T-393 (cliente, Windows): ler `PartyPositions` e desenhar os membros distantes no mini mapa; integrar as duas juntas na `main` e só então rodar o `deploy-dev.sh`.
