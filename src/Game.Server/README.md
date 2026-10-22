# Game.Server — servidor de zona (T-105)

Servidor autoritativo: roda o mesmo `Game.Core` do cliente a 20 Hz, uma sala com um encontro do protótipo, login de desenvolvimento (só um nome).

```bash
dotnet run --project src/Game.Server
dotnet run --project src/Game.Server -- --encounter encounter.four_rats --port 7777 --bots false
dotnet run --project src/Game.Server -- --help
```

- **Opções:** `--data`, `--port` (UDP, padrão 7777), `--encounter` (padrão Matriarca), `--seed`, `--bots` (vagas sem jogador jogam como bots; padrão `true`), `--disconnect-grace` (s até remover quem caiu; padrão 30), `--max-catch-up`, `--max-players`, `--interest-cell` (16 tiles), `--interest-radius` (2 células), `--max-delta-age` (40 ticks). Variáveis de ambiente: `MMO_DATA_DIR`, `MMO_PORT`.
- **Console:** `restart` (reinicia o encontro; quem está conectado recebe um `Welcome` novo), `status`, `quit`. Ctrl+C também para.
- **Entrada:** o cliente manda `Hello` com a versão do protocolo e o fingerprint de `data/`; diferente = `Reject`. Cada jogador ocupa uma vaga de jogador do encontro com a classe pedida (vaga da mesma classe; senão a primeira livre, que troca de classe). Sem vaga = `Reject` (`Full`).
- **A cada tick (T-106):** para cada jogador, `SnapshotDelta` só com as entidades da área de interesse dele (grade de células; o grupo e os chefes sempre), como diferença em relação ao último tick que ele confirmou com `SnapshotAck` (canal 1, não confiável). Sem confirmação recente, vai completo. `Movement` só vai para o dono. Eventos só das entidades que ele vê, mais `IntentAck` (canal 0, confiável e ordenado). O cliente monta o estado com `Game.Protocol.SnapshotReconstructor`.
- **Quem cai:** a entidade fica parada (ou vira bot) e é removida depois do prazo; voltando com o mesmo nome antes disso, retoma a mesma entidade. Sem jogadores, a sala reinicia.
- **Log (Serilog):** conexões, recusas, entradas e saídas, e o tempo de tick a cada 10 s.

Estrutura: `ZoneRoom` (sala e regras), `TickLoop` (relógio de 20 Hz sem deriva, recupera até N ticks e pula o resto com aviso), `Interest/` (`InterestGrid`, `ClientView` com o delta por cliente, `EventEntities`), `Transport/` (`LiteNetTransport` em produção, `InMemoryTransport` nos testes, com perda simulada).
