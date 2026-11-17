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

_(preencher ao concluir)_
