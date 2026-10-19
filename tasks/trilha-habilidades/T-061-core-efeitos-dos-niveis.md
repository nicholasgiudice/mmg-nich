# T-061 — Core: efeitos novos usados pelos níveis de habilidade

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-060
- **Libera:** T-062
- **Áreas que pode alterar:** `src/Game.Core/`, `src/Game.Data/`, `data/schemas/`, `tests/Game.Core.Tests/`

## Contexto

Vários NH de `design/skills-v0.1.md` pedem comportamentos que o Core ainda não tem. Esta tarefa implementa os efeitos genéricos (configuráveis por dados), sem mexer nos números das habilidades.

## Requisitos

Efeitos/mecânicas novos, cada um com teste:

1. **Atordoar** junto do dano (Golpe Pesado NH3/NH5) — usar o status de controle com retorno decrescente já existente (T-009).
2. **Saltos** para inimigos próximos com fração do dano (Corrente Sagrada NH3/NH5).
3. **Segundo alvo automático** ("também o inimigo mais próximo do alvo" — Marca NH3; "também o aliado mais ferido" — Cura Direta NH5, Escudo Sagrado NH5; "aliado mais próximo" — Barreira NH5).
4. **Área em volta do alvo** para status (Prisão de Gelo NH3+).
5. **Devolver Mana** quando um escudo acaba, proporcional ao que sobrou (Barreira NH3+).
6. **Refletir** parte do dano corpo a corpo bloqueado (Guarda Firme NH5).
7. **Imunidade temporária** a tags de status (Purificação NH5).
8. **Bônus no próximo ataque** depois de um deslocamento (Recuo Rápido NH3+).
9. **Levantar aliado caído** com fração da vida (Milagre NH3) — exige estado "caído" em vez de remoção imediata do jogador morto em grupo; registrar a decisão.
10. **Remover controle** dos aliados (Muralha Viva NH3).
11. **Área persistente deixada pela habilidade** reaproveitando `spawn_area` (chão em chamas do Cataclismo NH3, lentidão da Chuva NH5).

## Critérios de aceite

- cada efeito novo coberto por teste no Core;
- nenhum número de habilidade alterado (isso é a T-062);
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
