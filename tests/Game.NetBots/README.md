# Game.NetBots — bots de rede para teste de carga (T-109)

Jogadores sem Godot que se conectam ao `Game.Server` por UDP, como o cliente: `Hello`, deltas da T-106 montados com o `SnapshotReconstructor`, `SnapshotAck`, `Ping`. A cada snapshot novo o estado é espelhado num `World` (`World.Mirror`) e a estratégia do `Game.Bots` decide (as mesmas do simulador); as intenções vão para o servidor, que decide tudo.

```bash
# servidor local + 4 bots até o encontro acabar
dotnet run --project src/Game.Server -c Release -- --dev-login --port 7777 --bots false
dotnet run --project tests/Game.NetBots -c Release -- --server 127.0.0.1:7777 --bots 4 --until-end

# teste de carga: uma instância do servidor por sala (4 vagas), 5 minutos
tests/Game.NetBots/load.sh 16        # 4 servidores, 16 bots
tests/Game.NetBots/load.sh 40 300    # 10 servidores, 40 bots, 300 s
```

- **Opções:** `--server host:porta[,host:porta...]` (os bots enchem uma sala por vez, `--per-room` = 4), `--bots`, `--class <id|mix>` (mix = Guerreiro, Arqueiro, Mago e Clérigo em rodízio), `--strategy` (padrão `kite+dodge`), `--duration`, `--until-end`, `--data` (precisa ser igual à do servidor: o fingerprint é conferido).
- **Grupo (T-310, mundo aberto):** `--party` forma grupos de até `rules.party.maxMembers` bots em ordem (o primeiro de cada bloco convida os outros pelo nome e repete a cada 5 s; os outros aceitam). No fim sai a linha `GRUPO` com quantos bots estão em grupo, o tamanho do grupo de cada um, estados recebidos e recusas. Ex.: `dotnet run --project src/Game.Server -c Release -- --world --dev-login --port 7777` e `dotnet run --project tests/Game.NetBots -c Release -- --bots 8 --party --duration 60`.
- **Loja (T-321, mundo aberto):** `--shop` faz cada bot andar (caminho A*) até o NPC do Templo e comprar uma poção de vida. O bot de `--dev-login` nasce sem ouro, então antes vende as poções iniciais. No fim sai a linha `LOJA` com quantos bots chegaram ao NPC e quantos compraram, mais o total de compras, vendas e recusas. Ex.: `dotnet run --project tests/Game.NetBots -c Release -- --bots 4 --shop --duration 60`.
- **Instância (T-350, mundo aberto):** `--instance <zona>` faz cada bot esperar o grupo completo (`--party`, tamanho `--party-size`), andar (A*) até o portão da zona atual que leva à instância (ou à zona vizinha que tem esse portão) e lutar lá dentro. No fim sai a linha `INSTÂNCIA` com bots dentro, cópias distintas, entradas e recusas. Nenhum mapa leva à `zone.test_instance` até a Cripta (T-352): para medir, use uma cópia de `data/` com um `transition` para ela na Cidade, passada com `--data` ao servidor e aos bots. Ex.: `dotnet run --project tests/Game.NetBots -c Release -- --bots 20 --per-room 20 --party --party-size 2 --instance zone.test_instance --duration 60 --data <cópia>` (servidor com `--world --dev-login --max-players 32 --data <cópia>`).
- **Métricas:** a cada 10 s e no fim, por bot e no total: KB/s recebidos, intenções/s, ping (`Ping`/`Pong`), perda (ticks que faltam na sequência de snapshots), quedas e resultado (vitória/derrota com o tempo).
- **`load.sh`:** compila em Release, sobe ⌈bots/4⌉ servidores (portas a partir de 7800, logs em `$LOAD_LOGS`, padrão `/tmp/mmo-load`), roda os bots e resume o tick dos servidores (médio, p99, máximo, ticks pulados) a partir do log de 10 s de cada servidor.
- **Perigos:** os bots usam `SnapshotHazardSource`, que lê os telegráfos do estado. O `TelegraphHazardSource` do `Game.Bots` depende de eventos, que o mundo espelhado não tem.

Rodar na VPS mede o servidor onde ele vai rodar; no mesmo PC, bots e servidores disputam a CPU (o p99 do tick sobe).
