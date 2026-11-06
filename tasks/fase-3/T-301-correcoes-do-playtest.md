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

**Feito (branch `task/T-301-correcoes-do-playtest`):**

- **B1, recargas na troca de zona.** Novo `AbilityTimers` (`src/Game.Server/Persistence/CharacterState.cs`) com os ticks **restantes** de cada recarga (`ReadyAtTick - tick`), as cargas por recarga em uso (`CooldownChargesSpent`, T-060) e as cargas gastas (`ChargesUsed`, poção). `CharacterState` ganhou `Timers` (init, opcional). O `WorldServer.Move` captura os timers no tick da zona de origem, e o `CharacterState.Spawn` os recria no tick da zona nova (`tick + restante`), ignorando habilidades que o personagem não conhece mais. Status e ameaça continuam sendo perdidos.
- **Gravação:** `Timers` não entra no `CharacterDocument` nem vai para o banco. Login começa com tudo pronto. A poção acompanha a troca de zona até a T-320.
- **B2, corpos de invocações.** O `OpenZone` guarda os ids dos monstros de ponto (`_slotEntities`). A cada tick, todo monstro morto (facção `Monster`) fora desse conjunto ganha um tick de morte e é removido depois de `CorpseTicks`, sem renascer.
- **B3, grupo de aggro por ponto.** `OpenZone.GroupOf(i) = i + 1` (determinístico, nunca 0). Todo monstro que nasce do ponto `monsterSpawns[i]` (na criação e nos renascimentos) recebe esse `AiState.GroupId`. As invocações continuam herdando o grupo de quem invocou (`Summons.cs`, sem mudança).
- **Testes** (`tests/Game.Server.Tests/WorldTests.cs`):
  - `Cooldowns_and_spent_potion_charges_survive_a_zone_change`: poção usada nos Campos; na Cidade, a recarga restante é igual (tolerância de 1 tick) e a carga continua gasta;
  - `Dead_summons_in_an_open_zone_leave_after_the_corpse_time_and_do_not_respawn`: aranha invocada morta no Covil some depois de `CorpseTicks` e não volta;
  - `Pulling_a_monster_brings_only_its_spawn_point_group`: nos Campos reais, cada ponto tem um grupo distinto e nenhum monstro fica no grupo 0. Puxar um lobo da matilha de 3 em (54.5, 46.5) põe os 3 em combate, e a matilha de 3 em (68.5, 54.5) e o resto da zona ficam fora.

**Decisões:**

- Não existe recarga global (GCD) separada no `AbilityState`; tudo em `ReadyAtTick` é por habilidade (inclusive o ataque básico), então tudo acompanha. Nada foi descartado.
- O renascimento depois da morte (que também é uma troca de zona, via `Move`) também leva as recargas, para morrer não virar atalho para recarregar.
- O `src/Game.Core` não foi alterado.

**Requisito 5 (`AiContext.GroupMembers`):** continua correto. Ele filtra por `GroupId` e por perfil de IA, então a coordenação de matilha (foco do líder, reagrupar) agora vale só entre monstros do mesmo ponto, que é o comportamento esperado. Antes, todos os lobos da zona formavam uma matilha só. Os encontros (grupo 0) não mudaram, e os testes de design passam com o mesmo resultado.

**Pendências:** nenhuma.
