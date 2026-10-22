# T-107 — Cliente: `NetAuthority` e jogar online

- **Ambiente:** Windows
- **Modelo:** **Forte**
- **Depende de:** T-102, T-103, T-105
- **Libera:** T-108, T-111
- **Áreas que pode alterar:** `client/`

## Contexto

A apresentação do cliente só conhece `IGameAuthority` (T-020): trocar `LocalAuthority` por uma autoridade de rede não pode exigir mudar HUD, mundo ou input. Entidades remotas são desenhadas com ~100 ms de atraso, interpolando entre snapshots (`tech/stack-proposal-v0.1.md`, seção 4).

## Requisitos

1. `NetAuthority : IGameAuthority, ICombatLogSource` com LiteNetLib: conecta, envia `Hello` (versão, hash do conteúdo, nome, classe), trata `Welcome`/`Reject`.
2. **Buffer de snapshots** e relógio de interpolação ~100 ms atrás do servidor; `Latest`/`Previous`/`InterpolationFraction` calculados a partir do buffer (não do tick local). Monta os snapshots com a fábrica da T-101.
3. Eventos recebidos entregues por `TakeEvents()` na ordem; log de combate via T-102.
4. `Send` envia intenções com número de sequência (mesmo filtro: só a entidade controlada).
5. **Menu**: opção "Jogar online" (endereço do servidor, nome, classe); mensagem clara para recusa (versão/dados diferentes, sala cheia) e queda de conexão (voltar ao menu).
6. HUD: indicador de ping e de perda de pacotes.
7. `--server=host:porta` como atalho de depuração.

## Critérios de aceite

- dois clientes no Windows conectados ao servidor (local ou VPS) se veem andando e lutando na mesma sala;
- trocar para o modo local continua funcionando como hoje;
- `dotnet build client/` e `./scripts/check.sh` passam.

## Resultado

Feito no Windows. Verificado com o servidor da T-105/T-106 rodando local e **dois clientes Godot ao mesmo tempo**. Cada um viu o outro andar e lutar na Matriarca, junto com os 2 bots das vagas livres. Também verificados: servidor fora do ar, sala cheia e o modo local.

**`NetAuthority`** (`client/scripts/Authority/NetAuthority.cs`, sem Godot; LiteNetLib 2.1.4 no cliente):
- Conecta, manda `Hello` (versão, fingerprint do conteúdo, nome, classe) e trata `Welcome` e `Reject`. Estados: `Connecting`, `Joining`, `Playing`, `Rejected`, `Disconnected`, com mensagem em português para o jogador.
- **Snapshots:** os deltas da T-106 são montados com o `SnapshotReconstructor` e confirmados com `SnapshotAck` a cada snapshot aplicado (canal não confiável). Ficam num buffer de 32.
- **Interpolação:** um relógio de desenho anda em tempo real e mira `tick mais novo − 2` (**100 ms**). Longe demais (entrada, travada), pula; perto, corrige 10% por frame, então o movimento fica suave com chegadas irregulares e nunca extrapola. `Previous`, `Latest` e `InterpolationFraction` saem do buffer em volta desse relógio, não de um tick local.
- **Eventos:** guardados por tick e entregues por `TakeEvents()` quando o relógio de desenho chega no tick deles, então o número de dano aparece junto com o golpe. O log de combate (T-102) é gravado com `SnapshotLogLookup` do snapshot daquele tick e recriado a cada `Welcome`.
- `Send`: só a entidade controlada; `IntentMessage` com sequência no canal confiável.
- Ping por `Ping`/`Pong` a cada 1 s. Perda = ticks que faltam na sequência de snapshots recebidos, nos últimos 100.
- Novo `Welcome` (reinício da sala) zera o estado e a época, e o `Main` recarrega a arena e o acompanhamento do resultado.

**Menu e partida:**
- Seção **"Jogar online"** no menu: endereço `host:porta` (padrão `127.0.0.1:7777`), nome e a classe escolhida acima. A sala do servidor decide o encontro.
- Recusa (versão ou dados diferentes, sala cheia) e queda (servidor não responde, fechou a conexão, parou de responder): mensagem grande no centro da tela por 4 s, depois volta ao menu, que mostra o erro em vermelho.
- `--server=host:porta` (+ `--name=`, `--class=`) entra direto. O mapa só é montado quando o `Welcome` chega (vem a arena da sala).
- Online o mundo não pausa: Esc mostra a pausa, mas a rede continua e só a entrada para.
- HUD: o rodapé mostra `online host:porta · ping X ms · perda Y%`. Medido no loopback: ping 34 ms, perda 0%.
- Modo local inalterado (conferido na Matriarca).

**Fora de `client/` (mínimo):** `src/Game.Server/Transport/LiteNetTransport.cs` com `UpdateTime = 5`. O padrão do LiteNetLib envia em lotes a cada 15 ms, e com isso o ping no loopback ia a 50–70 ms. O cliente usa o mesmo valor.

**Pendências:**
- Online o servidor ainda não recebe os NH escolhidos no menu: todas as habilidades ficam no NH1. Precisa de campo no `Hello` (protocolo v3).
- Sem predição: o próprio personagem também é desenhado com 100 ms de atraso, o que é o tema da T-108.
- Fim de encontro online: a tela de resultado aparece. "Reiniciar" reconecta (se for o último na sala, o servidor reinicia o encontro); "Próximo" não existe online.
- O teste com dois clientes foi no mesmo PC. Pela VPS falta a T-104/T-110.
