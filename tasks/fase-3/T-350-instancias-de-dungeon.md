# T-350 — Servidor: instâncias de dungeon sob demanda

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-310, T-331
- **Libera:** T-352
- **Áreas que pode alterar:** `src/Game.Server/`, `src/Game.Core/Definitions/Zones.cs`, `src/Game.Data/`, `src/Game.Protocol/`, `data/zones.json`, `data/schemas/`, `tests/Game.Server.Tests/`, `tests/Game.NetBots/`, `tests/Game.Data.Tests/`

## Contexto

A Cripta do Vigia (`design/first-map-v0.1.md` §5) é o **primeiro conteúdo fechado de grupo**. Cada grupo precisa da sua cópia, com os monstros que **não renascem**, e a volta acontece só depois de concluir ou de abandonar a expedição.

Hoje o servidor no modo mundo (`--world`, T-214) só tem **zonas abertas compartilhadas** (`ZoneKind.Safe` e `Field`), com um `World` por zona, que só avança quando há jogador. A sala de encontro antiga é outro modo do processo, não uma zona. O grupo (T-310) define quem entra junto.

## Requisitos

1. **Tipo de zona `Instance`** em `ZoneDefinition`: os monstros nascem uma vez e não renascem, e existe uma **saída** (portão) de volta para a zona de origem.
2. **Criação sob demanda:** quando um personagem entra no portão de uma instância, o servidor:
   - procura uma instância **do grupo dele** (ou só dele, se estiver sem grupo) para aquela zona;
   - se não houver, cria uma nova (`World` próprio, seed derivada do id da instância).
   Todos os membros do grupo que entrarem vão para a mesma cópia. Quem não está no grupo nunca entra na cópia de outro.
3. **Limites:** no máximo `maxPlayers` por instância (4), e um número máximo de instâncias por processo (proposta: 20; quando lota, a entrada é recusada com motivo).
4. **Fim da instância:** a instância vazia é destruída depois de `emptyTimeout` (proposta: 5 min). Quem desconecta dentro dela volta para dentro se reconectar antes disso, e depois disso volta para a entrada (posição gravada = portão de origem). Quem sai do grupo dentro da instância é levado para fora ao fim de 30 s, com aviso.
5. **Morte na instância:** o personagem renasce na **entrada da instância**, e não na Cidade, enquanto houver alguém do grupo vivo lá dentro. Se todos caírem, renascem na entrada da instância, com os monstros vivos como estavam (não há "wipe" que reinicia). Registre se o designer preferir outra regra.
6. **Gravação:** a zona gravada de quem está numa instância é a **zona de origem** (entrada), nunca a instância.
7. **Protocolo:** o `Welcome` já leva arena e época. Se precisar, adicione o id da instância e o motivo de recusa (instâncias lotadas, sem grupo...).
8. **Desempenho:** as instâncias seguem a regra das zonas, e só avançam com jogadores. Medir com bots: 10 instâncias com 2 bots cada, por 60 s.

## Critérios de aceite

- testes do servidor:
  - dois do mesmo grupo entram na mesma cópia; um terceiro de fora entra numa cópia diferente;
  - os monstros não renascem;
  - a instância vazia some depois do prazo;
  - a reconexão dentro do prazo volta para dentro;
  - a gravação usa a zona de origem;
  - a morte renasce na entrada;
  - o limite de instâncias recusa;
- uma instância de teste simples em `zones.json` (pode reaproveitar `map.matriarch_nest`), usada pelos testes, até a Cripta (T-352);
- medição com bots no Resultado;
- `./scripts/check.sh` completo passa.

## Resultado

**Feito (branch `task/T-350-instancias-de-dungeon`, VPS, 2026-11-10):**

- **Dados e Core.** `ZoneKind.Instance` e, em `ZoneDefinition`, `MaxPlayers` (padrão 4), `EmptyTimeoutTicks` (`emptyTimeout`, padrão 300 s), `OutsiderTimeoutTicks` (`outsiderTimeout`, padrão 30 s) e `ExitGate()`. A saída é o primeiro portão do mapa.
  - Schema e `GameDataValidator`: os três campos novos têm que ser positivos.
  - `ZoneComposer`: instância sem `transition` é erro, e saída que leva a outra instância também.
- **Instância de teste.** `zone.test_instance` ("Covil de Teste (instância)") em `data/zones.json`, com os valores da tarefa (4 / 300 s / 30 s). Reaproveita o mapa `map.matriarch_lair`: a Matriarca nível 8 e a saída oeste para os Campos, na chegada `from_lair`. Nenhum mapa leva a ela até a Cripta (T-352).
- **Protocolo v12.**
  - `Welcome` ganhou `InstanceId` (opcional no fim, padrão 0 = zona aberta ou sala).
  - Mensagens novas em `src/Game.Protocol/InstanceMessages.cs`:
    - `InstanceRejected(ZoneId, Reason, Detail)`, com os motivos `TooManyInstances` e `InstanceFull`;
    - `InstanceNotice(Kind, Ticks)`, com os tipos `LeavingSoon`, `StayCancelled` e `Removed`.
- **Servidor.** `Zones/DungeonInstance` e mudanças no `WorldServer` e no `OpenZone`:
  - As zonas de instância não são criadas na partida (`WorldServer.Zones` continua só com as abertas). O portão para uma delas procura a cópia do dono `party:<id>` (ou `solo:<personagem>`, sem grupo) e, se não houver, cria uma: `OpenZone` próprio com seed `DungeonInstance.SeedFor(seed do servidor, id)` (SplitMix64). O `OpenZone` de instância não faz renascer os monstros: o corpo some e acabou.
  - Limites: `--max-instances` (padrão 20) e `maxPlayers` da zona. A recusa manda `InstanceRejected` uma vez e o personagem fica no portão, sem nova tentativa até sair da área.
  - Fim: a cópia vazia conta o prazo pelo tick do servidor e é destruída. Como as zonas abertas, ela só avança com jogador dentro.
  - Reconexão: quem desconecta dentro guarda uma reserva (chave do personagem → cópia e posição). Se reconectar enquanto a cópia existe e tem vaga, volta para lá, na posição em que estava. Depois disso, entra pela posição gravada.
  - Saída do grupo: quem está dentro de uma cópia que não é do seu grupo recebe `LeavingSoon` com o prazo. Se voltar ao grupo, recebe `StayCancelled`; senão, no fim do prazo recebe `Removed` e é levado pela saída para a zona de origem.
  - Morte: renasce na entrada (o `spawn`) da mesma cópia, sempre, com os monstros como estavam.
  - Gravação: dentro de uma instância, vivo ou morto, grava a zona e a posição da chegada da saída (`zone.vigil_fields`, `from_lair`), nunca a instância.
  - `Status()` do console lista as cópias (id, dono, tick, jogadores e monstros).
- **Bots de rede.**
  - Opções novas: `--instance <zona>` (com o grupo completo, anda por A* até o portão da instância ou da zona vizinha que tem esse portão e luta lá dentro) e `--party-size <n>`.
  - No fim sai a linha `INSTÂNCIA`. README atualizado.
- **Testes:**
  - Servidor (`tests/Game.Server.Tests/InstanceTests.cs`, 13 testes): conteúdo real mais um portão de teste na Cidade e prazos curtos. Cobrem:
    - entrada sob demanda e saída para a zona de origem;
    - os dois do grupo na mesma cópia e o de fora noutra, sem se verem;
    - monstros que não renascem;
    - cópia vazia destruída no prazo, parada enquanto vazia, e cópia nova com a Matriarca viva;
    - reconexão dentro do prazo volta para dentro, e depois do prazo não volta (dev e conta);
    - gravação na zona de origem (vivo, morto e na desconexão);
    - morte renasce na entrada, inclusive com todos caídos, e a Matriarca continua ferida;
    - limite de instâncias recusa (uma vez por tentativa) e cópia cheia recusa;
    - quem sai do grupo é levado para fora no prazo, com aviso;
    - grupo de 2 desfeito: a cópia fica com quem entrou primeiro, e quem é convidado de novo fica;
    - ida e volta das mensagens novas.
    - A `WorldRig` aceita conteúdo e opções, e `Where` acha também as cópias.
  - Dados (`ZoneLoaderTests`): carga da instância, padrões, instância sem saída ou com saída para outra instância, campos inválidos e a instância de teste dos dados reais. O `ZoneMapEquivalenceTests` (T-331) passou a comparar só as zonas abertas.
  - Conferi que os testes pegam o erro: com `Respawns => true`, o teste de "não renascem" falha.
- `dotnet format` aplicado; `./scripts/check.sh` completo passa.

**Medição com bots (requisito 8).** Servidor Release `--world --dev-login --max-players 32` na VPS (2 vCPU, bots na mesma máquina), 20 bots `--party --party-size 2 --instance zone.test_instance`, 60 s. Usei uma cópia de `data/` no scratchpad com um `transition` 1 × 1 na Cidade para a instância, porque nenhum mapa do repositório leva a ela.

- 20/20 em grupo de 2, **10 cópias, 2 bots em cada**, 28 entradas, 0 recusas, 0 quedas, 0 erros no log.
- Os bots (nível 1) morrem para a Matriarca e renascem na entrada (34 renascimentos). Alguns saem pela saída ao fugir e voltam pela Cidade (75 trocas de zona); no fim, 18/20 estavam dentro.
- Tick do servidor, janelas de 10 s: a primeira teve aquecimento (JIT e criação das 10 cópias), com média de 12,5 ms, p99 de 214 ms e 7 ticks pulados. Depois, em regime, média de **3,4 a 4,1 ms**, p99 de **19 a 25 ms**, máximo de 39 ms e nenhum tick pulado.
- Banda: 5,3 KB/s por bot.

**Decisões:**

- A instância de teste usa `map.matriarch_lair`, e não `map.matriarch_nest`. O ninho é uma arena de encontro só com colisão, sem `spawn` nem `transition`, e a zona não carregaria. Para isso seria preciso mexer em `data/maps/`, que está fora das áreas da tarefa. O Covil já tem nascimento, saída e a Matriarca.
- O dono da cópia é o grupo (`party:<id>`) ou o personagem (`solo:<id da conta>`, ou `solo:dev:<nome>` no `--dev-login`). Como a T-310 tira do grupo quem desconecta e desfaz grupos de 1, adotei uma regra simples: se ninguém do dono está dentro, a cópia passa para o dono de quem entrou primeiro, desde que ele não tenha outra cópia da mesma zona. Assim, quem fica sozinho depois que o grupo se desfaz não é expulso, e quem volta a um grupo junto com ele continua lá dentro.
- Reconexão: quem volta a uma cópia que já não é do seu grupo (a desconexão tirou ele do grupo) recebe o aviso de 30 s, e o líder pode convidar de novo. A reserva vale enquanto a cópia existir, e não por um prazo à parte.
- Morte: sempre na entrada da cópia, também quando todos caem. Não há reinício da instância (requisito 5, sem mudança do designer).
- Quem é levado para fora morto renasce cheio na zona de origem (sai como saída normal, sem vida e Mana guardadas).
- `maxPlayers` conta só quem está dentro. Reservas de quem caiu não ocupam vaga, mas a volta respeita o limite: com a cópia cheia, entra pela posição gravada.
- O limite de instâncias é do processo (`ServerOptions.MaxInstances`, `--max-instances`), não de dados. Os outros prazos são regras de jogo e ficam em `zones.json`.
- A zona inicial (`--start-zone`) não pode ser uma instância: ela não está em `Zones`, e o servidor recusa na partida como "zona inicial desconhecida".

**Fora das áreas (mínimo):** nenhum arquivo de fora das áreas listadas. `ZoneLoaderTests`, `ZoneMapEquivalenceTests` e `WorldTests` estão nas pastas permitidas.

**Pendências:**

- T-352: o portão real para a Cripta num mapa, e trocar `zone.test_instance` pela Cripta (ou deixá-la só para testes).
- Cliente (Windows): mostrar `InstanceRejected` e `InstanceNotice` (aviso de saída com contagem), e talvez o id da cópia no `Welcome`. Ele já compila com o protocolo v12, porque o campo novo é opcional.
- O designer pode querer outra regra para a morte de todos (ex.: expulsar todos ou reiniciar). Hoje ninguém sai e nada reinicia.

