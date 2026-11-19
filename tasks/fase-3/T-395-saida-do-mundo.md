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

_(preencher ao concluir)_
