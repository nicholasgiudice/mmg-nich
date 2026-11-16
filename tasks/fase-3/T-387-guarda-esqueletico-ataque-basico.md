# T-387 — Guarda Esquelético usa o ataque básico

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** —
- **Libera:** —
- **Áreas que pode alterar:** `src/Game.Core/Combat/Ai/`, `data/monsters.json`, `data/abilities_monsters.json`, `tests/`, `tasks/reports/`

## Contexto

Relato do designer no playtest da fase 3 (T-380), em 2026-11-16: o Guarda Esquelético não tem ataque básico.

A causa está no perfil `ai.guardian` (`GuardianProfile`): o ciclo é perseguir → guarda (`skeleton_guard.raise_shield`) → golpe pesado (`skeleton_guard.crushing_blow`) → recuperar. O **Golpe de Lâmina** (`skeleton_guard.slash`, recarga de 1,6 s) está na lista de habilidades do monstro, mas o perfil nunca o usa. Na prática o guarda só bate uma vez a cada ciclo, com o golpe telegrafado.

## Requisitos

1. O guarda usa o Golpe de Lâmina quando o alvo está no alcance e o golpe está pronto, **fora** da guarda e do golpe pesado. Proposta: na fase "perseguir", com o alvo no alcance do golpe de lâmina e o golpe pesado em recarga, ele golpeia em vez de entrar em guarda; a guarda e o golpe pesado continuam quando o golpe pesado está pronto.
2. A janela para atacar pelo flanco durante a guarda continua existindo.
3. O Guarda também aparece invocado pelo Vigia (`summon`); o comportamento vale lá também.
4. Conferir o equilíbrio: rodar o simulador nos encontros com o guarda (`encounter.skeleton_guard` e os da Cripta) antes e depois, e ajustar o dano do golpe de lâmina em `data/` se a dificuldade subir demais. Relatório em `tasks/reports/` (próximo `balance-v0.N.md`).
5. Teste de Core: o guarda usa o golpe de lâmina entre dois golpes pesados.

## Critérios de aceite

- teste novo passando;
- relatório do simulador com antes e depois;
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
