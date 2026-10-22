# T-105 — `Game.Server`: loop de 20 Hz, LiteNetLib e login de desenvolvimento

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-101, T-103
- **Libera:** T-106, T-107, T-109, T-110
- **Áreas que pode alterar:** `src/Game.Server/` (novo), `tests/Game.Server.Tests/` (novo), `MmoRpg.sln`

## Contexto

O servidor autoritativo roda o mesmo `Game.Core` do cliente. Primeira versão: **uma sala** com um encontro do protótipo (padrão: Matriarca, grupo de 4), jogadores entram por login de desenvolvimento (só um nome, sem senha nem banco). Leia `tech/stack-proposal-v0.1.md` (seção 4, Rede e Topologia) e `AGENTS.md`.

## Requisitos

1. Aplicação console `src/Game.Server` (`$(AppTargetFramework)`), carregando `data/` com `Game.Data` (pasta por argumento ou variável de ambiente).
2. **Loop de tick fixo de 20 Hz** com relógio do servidor (`Stopwatch` é permitido aqui, não no Core), sem deriva acumulada; se atrasar, recupera até N ticks e registra aviso.
3. **LiteNetLib** na porta UDP configurável (padrão 7777); `Hello` → valida versão do protocolo e hash do conteúdo (T-101) → `Welcome` ou `Reject`.
4. **Sala**: encontro configurável por argumento; cada jogador ocupa uma vaga de jogador do encontro com a classe pedida (mesma regra do `MatchSetup` do cliente); vagas livres podem ser preenchidas por bots de `Game.Bots` (opção); quando todos saem, a sala reinicia o encontro. Comando de console para reiniciar.
5. **Intenções**: recebidas no canal confiável, aplicadas no próximo tick **somente** para a entidade do próprio jogador (mesma regra da `LocalAuthority`); `IntentAck` com a última sequência aplicada.
6. **Envio**: a cada tick, `Snapshot` completo (não confiável) e `Events` do tick (confiável e ordenado) para todos os jogadores da sala. (Delta e interesse: T-106.)
7. Desconexão: entidade do jogador fica parada (ou vira bot, se a opção estiver ligada) e é removida após X segundos.
8. Logs com Serilog (console): conexão, recusa, tempo médio de tick a cada 10 s.
9. Teste de integração **sem rede real** (transporte em memória ou loopback): dois clientes falsos entram, enviam intenções e recebem snapshots coerentes.

## Critérios de aceite

- `dotnet run --project src/Game.Server` sobe a sala e aceita conexões;
- teste de integração passando;
- tempo médio de tick registrado no Resultado (Matriarca, 4 jogadores);
- `./scripts/check.sh` completo passa.

## Resultado

Feito no Windows (a tarefa é de VPS; feita aqui a pedido do designer). Pacotes: **LiteNetLib 2.1.4**, **Serilog 4.4.0** + Serilog.Sinks.Console 6.1.1. Como rodar: `src/Game.Server/README.md`.

**O que foi feito (`src/Game.Server`):**
- `Program.cs` + `ServerOptions`: `--data`, `--port` (7777), `--encounter` (Matriarca), `--seed`, `--bots`, `--disconnect-grace` (30 s), `--max-catch-up` (5), `--max-players`; `MMO_DATA_DIR` e `MMO_PORT` por ambiente. Sem `--data`, procura o `data/` do repositório subindo até o `MmoRpg.sln` (senão `./data`, para o contêiner da T-110). Console: `restart`, `status`, `quit` (lidos noutra thread, executados na thread do loop).
- `TickLoop`: o tick *n* vence em `início + n × 50 ms`, sempre calculado do início (sem deriva). Atrasou: recupera até 5 ticks; além disso pula os perdidos e loga aviso. Relógio injetável (testado com relógio falso).
- `Transport/`: `IServerTransport`. `LiteNetTransport`: canal 0 confiável e ordenado; canal 1 para snapshots não confiáveis. `InMemoryTransport` para os testes.
- `ZoneRoom`: `Hello` → valida versão e fingerprint → `Welcome` ou `Reject` (`ProtocolVersion`, `Content`, `Full`, `BadRequest` para classe desconhecida), fecha a conexão recusada 10 ticks depois (para a recusa chegar). Vaga com a regra do `MatchSetup` (mesma classe; senão a primeira livre, que **troca de classe**: a entidade é recriada onde estava). Vagas sem jogador jogam como bots (`kite+dodge`, opção `--bots`). Intenções: só para a entidade da conexão (a mensagem nem carrega entidade), aplicadas no próximo tick, até 16 por tick por jogador (o resto espera; fila limitada a 64); `IntentAck` com a última aplicada. Por tick: snapshot completo (serializado uma vez para todos), eventos do tick e acks. `Ping` → `Pong` com o tick.
- Desconexão: a entidade para (ou vira bot) e some depois do prazo; **voltar com o mesmo nome dentro do prazo retoma a mesma entidade**. Sem jogadores, a sala reinicia (seed + 1). `restart` dá um `Welcome` novo a quem está conectado, na mesma classe.
- Logs (Serilog, console): conexões, recusas, entradas/saídas, remoções, reinícios e, a cada 10 s, tempo médio e máximo de tick, jogadores, ticks pulados e snapshots fragmentados.

**Medição (Matriarca, 4 jogadores reais por UDP no loopback, Release, i5-8600):** tick médio **0,18 ms** (máx. 3,6 ms; a primeira janela tem 1,0 ms de média por causa do JIT), incluindo simulação, serialização do snapshot e envio para os 4. Exatamente 200 ticks a cada 10 s, nenhum pulado. Os 4 clientes de teste (programa de rascunho com LiteNetLib) receberam `Welcome` com a classe pedida, ~20 snapshots/s e os eventos; a saída dos 4 reiniciou a sala.

**Snapshots acima de um pacote:** o snapshot completo da Matriarca tem ~3,2 KB e passa do MTU, então vai **confiável sem ordem** (o LiteNetLib só fragmenta no confiável). Funciona no loopback, mas é o motivo da T-106 (delta + interesse): o contador "snapshots fragmentados" no log mostra quando isso acontece.

**Testes** (`tests/Game.Server.Tests`, 12, sem rede real): dois jogadores entram com suas classes e os outros viram bots; intenção move só a própria entidade (mesmo pedindo outra), com `IntentAck`; os dois recebem os mesmos snapshots em ordem, snapshot no modo não confiável e eventos no confiável; eventos do tick chegam com o tick certo; recusas por versão, conteúdo e sala cheia (com fechamento); classe fora do encontro troca a vaga; desconexão com prazo, retorno pelo nome e remoção; reinício por saída de todos e pelo console; Ping/Pong; enxurrada de intenções limitada e lixo ignorado; relógio sem deriva em 60 s com acordadas atrasadas, recuperação de 5 e pulo do resto; opções e ambiente.

**Decisões:** bots ligados por padrão (com menos de 4 pessoas a Matriarca fica jogável); o fim do encontro (vitória/derrota) não reinicia sozinho — use `restart` (o cliente da T-107 precisa tratar `Welcome` novo); só uma sala por processo.

**Pendências:** fim de encontro automático (reiniciar depois de X s) se o playtest pedir; o `EntityRemoved` da entidade trocada de classe chega como evento, mas o cliente da T-107 deve usar o `Welcome`/snapshot como verdade.
