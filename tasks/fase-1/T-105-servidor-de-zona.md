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

_(preencher ao concluir)_
