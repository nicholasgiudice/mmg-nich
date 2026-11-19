# T-395 — Servidor: sair do mundo (trocar de personagem) com espera em combate

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** —
- **Libera:** T-396
- **Áreas que pode alterar:** `src/Game.Server/`, `src/Game.Protocol/` (mensagens novas e `ProtocolVersion`), `src/Game.Core/Definitions/` e `src/Game.Data/` (só a regra nova e a validação dela), `data/rules.json`, `tests/`, `design/` (só para registrar a regra)

## Contexto

Pedido do designer em 2026-11-19: o Esc deve abrir um menu para **trocar de personagem** (T-396). Hoje o cliente só fecha a conexão e o servidor grava o personagem ao perceber a saída (`WorldServer.OnDisconnected` → `SaveNow`). Isso deixa dois problemas:

- **Lista desatualizada:** a gravação é assíncrona. Se o cliente volta logo para a lista de personagens, a API pode devolver o nível, a zona e a posição antigos.
- **Fuga do combate:** sair do menu no meio de uma luta tira o personagem do mundo na hora.

## Requisitos

1. Mensagem nova do cliente `LogoutRequest` e do servidor `LogoutStatus`, com o estado (`Waiting` com o tick em que a saída acontece, `Cancelled`, `Done`).
2. **Fora de combate** (`Leveling.InCombat` falso, ou personagem morto): o servidor tira o personagem do mundo, grava, espera a gravação terminar e manda `LogoutStatus(Done)`. Depois disso, fecha a conexão, como na saída normal.
3. **Em combate:** o personagem continua no mundo, vulnerável, por um prazo configurável. O servidor manda `Waiting` com o tick final e, ao fim do prazo, segue como no item 2.
4. **Cancelamento:** mover-se, usar habilidade ou ataque, ou mandar `LogoutCancel`, cancela a espera. O servidor responde `Cancelled`. Morrer durante a espera não cancela.
5. O prazo vem de `data/rules.json` (sugestão: `rules.logout.combatDelay` = **10 s**), validado (maior ou igual a 0).
6. Na gravação com falha, o servidor manda `Done` mesmo assim e registra o erro no log. O cliente não pode ficar preso.
7. Login de desenvolvimento (sem conta): mesma regra, sem gravação.
8. Instância (T-350): a saída por `LogoutRequest` segue a mesma reserva de vaga que a desconexão já faz.
9. Testes no servidor: saída fora de combate, espera em combate, cancelamento por movimento e por `LogoutCancel`, morte durante a espera, gravação concluída antes do `Done`; testes do protocolo e da validação da regra.

## Fora de escopo

- O personagem continuar no mundo quando o jogador fecha o jogo à força (Alt+F4 ou queda de conexão) em combate. Fica registrado como pendência.
- Qualquer tela do cliente (T-396).

## Critérios de aceite

- testes novos cobrindo os itens acima;
- `./scripts/check.sh` completo passa.

## Cuidados de integração

- Sobe `ProtocolVersion` para **15**. **Não integre sozinha:** integre junto com a T-396 (que sai desta branch) e, na mesma rodada, rode o `deploy-dev.sh` e publique o cliente novo.

## Resultado

Concluída em 2026-11-19 (VPS), na branch `task/T-395-saida-do-mundo`. **Protocolo 15. Não integrar sozinha:** junto com a T-396, com `deploy-dev.sh` e cliente novo na mesma rodada.

### API do protocolo 15 (para a T-396)

Arquivo novo `src/Game.Protocol/LogoutMessages.cs`; tags da união `INetMessage`: 23, 24 e 25. Só no mundo aberto (`WorldServer`, `--world`); a `ZoneRoom` (salas de encontro antigas) ignora as mensagens.

| Mensagem | Sentido | Campos | Quando |
|---|---|---|---|
| `LogoutRequest` | cliente → servidor | — | o jogador escolheu "trocar de personagem". Ignorada sem personagem no mundo. Repetida durante a espera, o servidor só reenvia o `Waiting` com o mesmo tick. |
| `LogoutCancel` | cliente → servidor | — | o jogador desistiu. Se havia saída pedida ou em espera, chega `Cancelled`; senão, nada chega. |
| `LogoutStatus` | servidor → cliente | `LogoutState State`, `long Tick` | andamento da saída (abaixo). |

`LogoutState`:

- **`Waiting`** — o personagem está em combate (`Leveling.InCombat`): continua no mundo, vulnerável, até o tick `Tick`. O tick é **da zona**, o mesmo dos snapshots e do `Welcome` (20 por segundo): faltam `Tick − tick atual` ticks. Chega no tick seguinte ao pedido. Se o personagem for levado de zona durante a espera (ex.: tirado da instância), chega um `Welcome` novo seguido de outro `Waiting` com o tick da zona nova. O servidor para o personagem (`Stop`) ao começar a espera.
- **`Cancelled`** — a espera acabou sem sair: o jogador mandou `Move` com direção não nula, `UseAbility` (habilidade ou ataque) ou `LogoutCancel`. `Stop` e `Move` com direção zero **não** cancelam; usar item, equipar etc. também não. A intenção que cancelou é aplicada normalmente. `Tick` = 0.
- **`Done`** — o personagem saiu do mundo e a gravação terminou: a lista de personagens da API já vem atualizada. `Tick` = 0. O servidor fecha a conexão 10 ticks (0,5 s) depois; o cliente pode fechar ela mesma ao receber o `Done` e voltar para a seleção de personagem. A conexão não aceita outro `Hello`: o próximo personagem entra por uma conexão nova, com um token de jogo novo da API.

Sequências possíveis: fora de combate ou morto → só `Done` (no tick seguinte ao pedido, mais o tempo da gravação). Em combate → `Waiting`, depois `Done` (no tick final) ou `Cancelled`. Morrer durante a espera → `Done` na hora (não cancela). Se a conexão cair antes do `Done`, vale a saída normal por desconexão.

### O que foi feito

- **Protocolo:** `LogoutRequest`, `LogoutCancel`, `LogoutStatus` e `LogoutState`; `ProtocolVersion` 14 → 15, com a linha no histórico.
- **Regra:** `rules.logout.combatDelay` = 10 s em `data/rules.json` (`LogoutRules.CombatDelayTicks` em `Game.Core/Definitions/Rules.cs`, padrão 200 ticks), no schema `data/schemas/game-data.schema.json` e validada em `GameDataValidator` (≥ 0; 0 = sai na hora mesmo em combate).
- **Servidor (`WorldServer`):** o pedido é decidido no tick seguinte, depois das intenções, das zonas, dos portões e das instâncias. Fora de combate, morto ou com espera 0: sai na hora. Em combate: `Waiting` e saída quando o tick do servidor chega ao prazo, ou na hora se morrer. O cancelamento é verificado na chegada de cada intenção, então "pedido e depois movimento" no mesmo tick cancela. A saída tira do grupo (como a desconexão), reserva a vaga numa instância (o mesmo código da desconexão: `LeaveWorld`), grava, remove a entidade, espera a gravação, libera o personagem para entrar de novo (`_online`) e manda o `Done`.
- **Gravação confirmada:** `CharacterPersistence.SaveConfirmed` grava como `Save` e devolve uma `Task<bool>` que termina quando a primeira tentativa com esse estado (ou um mais novo) acaba: true = gravado; false = falhou (a repetição continua por trás), foi descartado (conflito ou personagem apagado) ou o servidor está parando. O loop só olha `IsCompleted` a cada tick, nunca espera.
- **Falha:** com a gravação falhando, vai o `Done` mesmo assim, com `Error` no log, e a gravação continua sendo repetida na fila. Como `CharacterPersistence` devolve o estado pendente a quem carrega o personagem, entrar de novo logo depois ainda pega o estado certo.
- **Login de desenvolvimento:** a mesma regra, sem gravação: o `Done` vai no mesmo tick da saída.
- **Testes:** `tests/Game.Server.Tests/LogoutTests.cs` (14): saída fora de combate e fechamento da conexão, morto em combate, espera até o tick final (personagem continua no mundo e é parado), pedido repetido, cancelamento por movimento (e `Stop` não cancela), por habilidade, por `LogoutCancel`, pedido e movimento no mesmo tick, morte na espera, espera 0, saída do grupo, `Done` só depois da gravação (com o personagem recusado como `AlreadyConnected` enquanto grava e voltando no lugar onde saiu), falha na gravação, prazo de segurança, saída em combate com conta. `InstanceTests.Logout_inside_keeps_the_same_reservation_as_a_disconnect`. Protocolo: ida e volta, tags 23–25 e versão 15. Dados: `RulesLoaderTests.Logout_combat_delay_loads_in_ticks_with_default_and_must_not_be_negative`.

### Decisões

- **Morrer durante a espera sai na hora** (não cancela e não espera o fim do prazo), coerente com "morto sai na hora": morto não tem do que fugir.
- **Prazo de segurança da gravação:** `WorldServer.LogoutSaveTimeoutTicks` = 10 s. Se o banco travar e a primeira tentativa não terminar nesse tempo, o `Done` vai assim mesmo, com erro no log. Ficou como constante do servidor, não em `data/`, porque não é regra de jogo.
- **Tick do `Waiting` é o tick da zona**, não o do servidor, porque é o tick que o cliente conhece pelos snapshots. As zonas com jogador avançam junto com o servidor, então a conversão é exata.
- **O servidor para o personagem ao começar a espera**, com um `StopIntent`. Sem isso, quem pedisse a saída andando continuaria andando sem mandar intenção nova, e fugiria sem cancelar.
- Não mexi em `design/`: a regra fica registrada em `data/rules.json`, no schema (descrição) e neste Resultado; nenhum documento de design trata de saída ou desconexão.

### Desvios

- `data/schemas/game-data.schema.json` (fora da lista, mas necessário: a seção `rules` não aceita propriedades desconhecidas) e `src/Game.Server/Persistence/CharacterPersistence.cs` (dentro de `src/Game.Server/`, para confirmar a gravação).

### Pendências

- **Fechar o jogo à força em combate** (Alt+F4, queda de conexão) ainda tira o personagem do mundo na hora: a desconexão continua gravando e removendo imediatamente. O personagem continuar no mundo por `combatDelay` nesse caso fica para outra tarefa (fora de escopo aqui).
- A `ZoneRoom` (modo de salas de encontro, sem `--world`) não trata `LogoutRequest`; o cliente da T-396 só deve oferecer a troca de personagem no mundo aberto (ou tratar a falta de resposta).
- Integração: só junto com a T-396, com o deploy do servidor e o cliente novo na mesma rodada.
