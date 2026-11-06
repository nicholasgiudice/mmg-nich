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

_(preencher ao concluir)_
