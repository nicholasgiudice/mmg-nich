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

**Feito.**

- `GuardianProfile` (`src/Game.Core/Combat/Ai/Profiles/GuardianProfile.cs`), fase "perseguir": com o golpe pesado em recarga, o guarda age como o `ai.melee_chaser`, isto é, usa o Golpe de Lâmina quando está pronto e persegue até o alcance dele. Ele só entra em guarda com o golpe pesado pronto. Guarda, golpe pesado e recuperação não mudaram, e a janela para atacar pelo flanco durante a guarda continua (req. 1 e 2).
- **Vigia (req. 3):** o Vigia invoca o mesmo `monster.skeleton_guard`, com o mesmo perfil. O comportamento vale lá sem código extra.
- **Teste:** `Guardian_uses_its_basic_strike_between_two_heavy_strikes` (`tests/Game.Core.Tests/AiThreatTests.cs`). Ele verifica que há golpe comum entre dois golpes pesados e que cada golpe pesado continua precedido da guarda, sem golpe comum no meio. O teste falha no código de `main` e passa com a mudança. O teste antigo (`Guardian_alternates_guard_and_heavy_strike`) continua passando.
- **Equilíbrio (req. 4):** `tasks/reports/balance-v0.17.md`, com antes (`main`) e depois, nas mesmas seeds. Foram medidos (inclui o Vigia):
  - o simulador em `encounter.skeleton_guard` (nv9 e nv10) e em `encounter.dead_courtyard`;
  - os testes de design das Ruínas e da Cripta.

**Decisões:**

- **Dano do Golpe de Lâmina: 9 → 11** (`data/abilities_monsters.json`). Escolha do designer em 2026-11-17, entre as opções medidas.
  - Com a IA nova e dano 9, o teste `WatcherDesignTests.Without_the_cleric_the_win_rate_drops_clearly` falhava: sem Clérigo, o grupo vencia o Vigia 34% das vezes, e a meta é no máximo 30%. Em `main` dava 28%. O guarda invocado passa menos tempo em guarda e morre mais rápido.
  - Com 11, o teste fica em 30%, exatamente no limite.
  - 13 dava 28% e pesava mais no resto.
  - Detalhes em `balance-v0.17.md` §3.4.
- **Efeito no resto:**
  - nenhuma taxa de vitória ativa caiu;
  - as lutas ficaram 1 a 15 s mais curtas, porque o guarda passa menos tempo em guarda;
  - o dano recebido por quem fica no corpo a corpo subiu (com desvio, de 0 para cerca de 200 por luta), que era o objetivo;
  - no Pátio sozinho, atacando o mais próximo, a média das classes foi de 81% para 59%, dentro da meta pendente de 40–70%.
- **Regra de entrada em guarda:** a guarda só começa com o golpe pesado pronto, não apenas com o alvo no alcance dele. Antes, com o golpe pesado em recarga, o guarda ficava parado em guarda até ele voltar.

**Desvio:** `data/abilities_monsters.json` está nas áreas permitidas. Nenhuma área fora da lista foi alterada.

**Pendências para o playtest:**

- **Guerreiro sozinho no Pátio:** atacando o mais próximo, vence só 3% (antes, 92%). Focando o Sacerdote primeiro, vence 100%. Confirmar se esse é o recado que o Pátio deve passar.
- **Clérigo sozinho contra o guarda:** conferir se está confortável.
- **Teste do Vigia sem Clérigo:** está no limite (15 vitórias em 50). Um ajuste futuro no Vigia ou nos guardas pode derrubá-lo.
