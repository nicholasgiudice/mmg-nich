# T-111 — Playtest online com 4 jogadores

- **Ambiente:** Windows
- **Modelo:** Humano (um agente leve pode organizar as anotações depois)
- **Depende de:** T-106, T-108, T-109, T-110
- **Libera:** T-120

## Objetivo

Responder: **o combate continua bom pela internet, com o servidor na VPS e quatro pessoas?**

## Roteiro

1. Sozinho contra a VPS: cada classe num encontro solo e na Matriarca com 3 bots (vagas livres).
2. Com 1–3 pessoas convidadas: a Matriarca em grupo, pelo menos 3 vezes, com classes diferentes.
3. Anotar: ping médio de cada jogador, "pulos" de posição, ações que pareceram atrasadas, desconexões.

## Perguntas

- Movimento e ataque básico parecem tão responsivos quanto no modo local?
- Telegráfos chegam a tempo de desviar?
- Algo apareceu diferente para jogadores diferentes (dessincronização)?
- O que mais irritou?

## Entrega

`tasks/reports/playtest-online-v0.1.md` com as respostas, ping por jogador, problemas encontrados e a decisão: seguir para a fase 2 (T-120) ou corrigir antes.

## Resultado

**Decisão: seguir para a fase 2 (T-120).** Relatório: `tasks/reports/playtest-online-v0.1.md`.

- O designer jogou sozinho contra a VPS e achou o combate online "bem fluido": ping de 14–22 ms, 0% de perda, nenhuma correção visível da predição e nenhuma queda.
- O que incomodou foi chegar com a luta em andamento. Isso foi corrigido no servidor: sala vazia parada, luta do zero para quem entra e reinício automático no fim.
- **Não feito:** o teste em grupo com convidados (passo 2), por falta de pessoas. Ficou como primeira pendência do próximo playtest, junto com ping alto real e NH online.
