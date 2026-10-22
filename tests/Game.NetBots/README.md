# Game.NetBots — bots de rede para teste de carga (T-109)

Jogadores sem Godot que se conectam ao `Game.Server` por UDP, como o cliente: `Hello`, deltas da T-106 montados com o `SnapshotReconstructor`, `SnapshotAck`, `Ping`. A cada snapshot novo o estado é espelhado num `World` (`World.Mirror`) e a estratégia do `Game.Bots` decide (as mesmas do simulador); as intenções vão para o servidor, que decide tudo.

```bash
# servidor local + 4 bots até o encontro acabar
dotnet run --project src/Game.Server -c Release -- --port 7777 --bots false
dotnet run --project tests/Game.NetBots -c Release -- --server 127.0.0.1:7777 --bots 4 --until-end

# teste de carga: uma instância do servidor por sala (4 vagas), 5 minutos
tests/Game.NetBots/load.sh 16        # 4 servidores, 16 bots
tests/Game.NetBots/load.sh 40 300    # 10 servidores, 40 bots, 300 s
```

- **Opções:** `--server host:porta[,host:porta...]` (os bots enchem uma sala por vez, `--per-room` = 4), `--bots`, `--class <id|mix>` (mix = Guerreiro, Arqueiro, Mago e Clérigo em rodízio), `--strategy` (padrão `kite+dodge`), `--duration`, `--until-end`, `--data` (precisa ser igual à do servidor: o fingerprint é conferido).
- **Métricas:** a cada 10 s e no fim, por bot e no total: KB/s recebidos, intenções/s, ping (`Ping`/`Pong`), perda (ticks que faltam na sequência de snapshots), quedas e resultado (vitória/derrota com o tempo).
- **`load.sh`:** compila em Release, sobe ⌈bots/4⌉ servidores (portas a partir de 7800, logs em `$LOAD_LOGS`, padrão `/tmp/mmo-load`), roda os bots e resume o tick dos servidores (médio, p99, máximo, ticks pulados) a partir do log de 10 s de cada servidor.
- **Perigos:** os bots usam `SnapshotHazardSource`, que lê os telegráfos do estado. O `TelegraphHazardSource` do `Game.Bots` depende de eventos, que o mundo espelhado não tem.

Rodar na VPS mede o servidor onde ele vai rodar; no mesmo PC, bots e servidores disputam a CPU (o p99 do tick sobe).
