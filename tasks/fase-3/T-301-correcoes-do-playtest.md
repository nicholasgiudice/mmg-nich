# T-301 — Correções do playtest: recargas na troca de zona, corpos das invocações e aggro em cadeia

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** —
- **Libera:** T-310, T-331
- **Áreas que pode alterar:** `src/Game.Server/Zones/`, `src/Game.Server/Persistence/CharacterState.cs`, `src/Game.Core/Combat/` (só o necessário), testes correspondentes

## Contexto

O playtest da fase 2 (`tasks/reports/playtest-v0.2.md`, bugs B1–B3) achou três bugs no mundo aberto. A causa provável de cada um já foi encontrada no código:

- **B1: recargas zeram ao trocar de zona.** Foi uma decisão registrada na T-214 ("ao trocar de zona perdem-se status, recargas e ameaça; a entidade é recriada"). Na prática, isso deixa recarregar tudo, e também as cargas da poção, só passando pelo portão. As recargas ficam em `AbilityState` (`src/Game.Core/Combat/Components.cs`) como **tick absoluto**, e cada zona tem o seu próprio tick. Por isso não dá para copiar os valores direto: é preciso levar o tempo **restante**.
- **B2: os corpos das aranhas ficam no chão para sempre** no Covil da Matriarca (`zone.matriarch_lair`). O `OpenZone` só remove os corpos dos monstros que nasceram de um ponto de surgimento (`slot.DiedAt`). As invocações da Matriarca (`Summons.cs`) não têm ponto e nunca são removidas.
- **B3: um monstro puxa todos os outros da zona.** `ThreatService.PullGroup` puxa todo monstro com o mesmo `AiState.GroupId`. O `OpenZone` nunca define esse valor, então todos os monstros da zona ficam no grupo 0. Quando um entra em combate, a zona inteira vem atrás do jogador, inclusive os monstros fora da tela.

## Requisitos

1. **Recargas e cargas acompanham o personagem na troca de zona.** Ao sair de uma zona, capturar, para cada habilidade, os ticks restantes de recarga (inclusive as recargas por carga da T-060) e as cargas gastas. Na zona nova, recriar o estado com base no tick dela. A recarga global (GCD) pode ser descartada. Status e ameaça continuam sendo perdidos, como hoje.
2. **Gravação:** a recarga **não** precisa ser gravada no banco. Sair do jogo e voltar pode zerar as recargas. As cargas da poção seguem a regra que a T-320 vai definir (consumíveis viram itens). Até lá, elas acompanham a troca de zona como as recargas.
3. **Corpos de invocações:** numa zona aberta, todo monstro morto que não vem de um ponto de surgimento é removido depois de `ZoneDefinition.CorpseTicks`, como os outros. Não renasce.
4. **Grupo de aggro por ponto de surgimento:** cada ponto (`monsterSpawns[i]`) ganha um `GroupId` próprio, e todos os monstros que nascem dele recebem esse id. A invocação herda o grupo de quem invocou, como já acontece hoje. O id deve ser determinístico (por exemplo, o índice do ponto + 1) e não pode colidir com o grupo 0 usado nos encontros.
5. Conferir se o `AiContext` (que filtra aliados por `GroupId`, na linha ~86) continua correto com grupos por ponto.

## Critérios de aceite

- teste do servidor (`tests/Game.Server.Tests/WorldTests.cs`): usar uma habilidade nos Campos, passar pelo portão e confirmar, na Cidade, que a recarga restante é a mesma (com tolerância de 1 tick) e que as cargas gastas da poção continuam gastas;
- teste: invocação morta numa zona aberta some depois do prazo do corpo;
- teste: num mapa com dois pontos de surgimento distantes, puxar um grupo não coloca o outro em combate; puxar um monstro de um grupo de 3 coloca os 3 em combate;
- os testes de design (`Game.Design.Tests`) não mudam de resultado;
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
