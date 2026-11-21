# T-398 — Servidor: renascimento conta o tempo em que a zona ficou vazia

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** —
- **Libera:** —
- **Áreas que pode alterar:** `src/Game.Server/Zones/`, `tests/Game.Server.Tests/`

## Contexto

Achado do designer em 2026-11-19, durante a T-380: "parece que o respawn dos bichos precisa que alguém esteja no mapa".

É de propósito desde a T-214: zona sem jogador não avança (`WorldServer.Step` só chama `OpenZone.Step` nas zonas com alguém dentro), para não gastar CPU. Mas o relógio da zona para inteiro, com os prazos de corpo e de renascimento. Quem limpa os Campos, vai à Cidade e volta 10 minutos depois encontra os monstros ainda mortos, e eles só renascem depois do prazo que faltava, contado da volta.

## Requisitos

1. Zona vazia continua parada (sem gastar CPU com IA e simulação).
2. Ao voltar a ter jogador, o tempo em que ficou parada conta para os corpos e os renascimentos: corpos vencidos somem e monstros devidos nascem no primeiro tick depois da volta.
3. Vale também para os corpos de invocações (somem, não renascem).
4. Instâncias continuam sem renascimento (T-350).
5. Testes no servidor.

## Fora de escopo

- Monstros vivos, efeitos e IA continuam parados enquanto a zona está vazia (um monstro ferido continua ferido).

## Critérios de aceite

- testes novos cobrindo os itens acima;
- `./scripts/check.sh` completo passa.

## Cuidados de integração

- Só servidor: não mexe em protocolo nem pede cliente novo. Depois de integrar, rodar o `deploy-dev.sh`.

## Resultado

**Feito** em 2026-11-19 na branch `task/T-398-renascimento-em-zona-vazia`. Rodou no Windows, não na VPS: é só servidor e testes, sem tela, e o designer pediu na mesma conversa.

**Como ficou:**
- `OpenZone.Step(long serverTick)`: guarda o último tick do servidor em que a zona avançou. Se passou mais de 1 tick desde então, a zona ficou parada, e `CatchUp(ticks)` adianta pela parada os prazos de cada ponto (`DiedAt` e `RespawnAt`) e dos corpos de invocações. Em seguida vem o tick normal, que tira os corpos vencidos e faz nascer os monstros devidos.
- `WorldServer.Step` chama `zone.Step(_tick)`. O `Step()` sem argumento continua existindo para os testes que avançam a zona sozinha.
- A zona vazia continua parada: só os prazos são adiantados, de uma vez, na volta. IA, efeitos e monstros vivos ficam como estavam.
- Instância: `RespawnAt` é sempre nulo, então continua sem renascer; os corpos somem no prazo, contando a parada.

**Testes** (`tests/Game.Server.Tests/WorldTests.cs`):
- parada menor que o prazo do corpo: o corpo some no tick certo, contando a parada;
- parada maior que o renascimento: o rato nasce no primeiro tick depois da parada;
- de ponta a ponta: o jogador mata 3 ratos nos Campos, vai à Cidade, espera mais que o renascimento (os Campos ficam parados) e, ao voltar, os 3 ratos já estão de volta;
- corpo de invocação na toca da Matriarca some contando a parada.

**Integração:** só servidor, sem protocolo novo nem cliente novo. Depois de integrar, rodar o `deploy-dev.sh`.
