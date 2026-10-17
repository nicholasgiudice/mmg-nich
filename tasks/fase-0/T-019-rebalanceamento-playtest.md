# T-019 — Rebalanceamento pós-playtest: monstros mais lentos e mais dano das classes

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** T-018, primeiras impressões da T-025
- **Libera:** nova rodada de playtest (T-025)
- **Áreas que pode alterar:** `data/` (somente números), `tests/Game.Design.Tests/`, `tests/Game.Sim/`, `tests/Game.Sim.Tests/`, `tasks/reports/`

## Contexto

Primeiras impressões do playtest humano (T-025, 2026-10-16): "o combate não está muito fluido; os bichos são rápidos demais e as classes além do Guerreiro têm dificuldade; o dano das habilidades deveria ser maior".

Os números confirmam:

| | Velocidade (tiles/s) |
|---|---|
| Guerreiro, Mago, Clérigo | 4,5 |
| Arqueiro | 5,0 |
| Lobo Cinzento, Rato de Campo | 5,0 |
| Salteador da Estrada | 4,2 |

Mago e Clérigo nunca conseguem abrir distância de lobos e ratos; o Arqueiro só empata. O ataque básico leva ~17 s (Guerreiro), ~20 s (Arqueiro), ~22 s (Mago) e ~30 s (Clérigo) para matar um Lobo (220 de vida); o Clérigo leva ~55 s no Guarda Esquelético (400). O Guerreiro funciona porque tem 240 de vida e não precisa se afastar.

O lado de "controle" da fluidez (cada clique = um ataque) é tratado na T-026, no cliente.

## Requisitos

1. **Monstros comuns mais lentos que o jogador**: Lobo Cinzento e Rato de Campo em ~4,0; Salteador ≤ 4,0. Regra: todo monstro `normal` corre **menos** que a classe mais lenta (4,5), para que afastar-se (kite) seja possível. Elites e chefes podem ser exceção, desde que justificada no relatório.
2. **Dano das classes +50%** (ponto de partida pedido pelo designer): `base` dos efeitos de dano de ataques básicos e habilidades das quatro classes. Ajustes finos para cima ou para baixo por classe são permitidos se o simulador mostrar necessidade, com a razão registrada (meta: Mago, Arqueiro e Clérigo deixam de depender só do Guerreiro).
3. **Reequilibrar o restante** (vida/dano dos monstros, custos, recargas) somente se necessário para manter os encontros desafiadores — lutas solo não devem virar vitória trivial. Referência de duração: encontros solo entre ~15 s e ~60 s com `rotation`.
4. **Testes de design** (`tests/Game.Design.Tests`) continuam passando. Se algum limite da T-016/T-018 ficar incompatível com o pedido do designer (monstros mais lentos e mais dano), ajustar o limite **e justificar no relatório**; não remover testes.
5. **Novo teste de design**: com a estratégia `kite`, Mago e Arqueiro conseguem manter distância de um Lobo/Rato (ex.: tomam menos dano corpo a corpo que com `rotation` parado, ou a distância média ao inimigo fica acima do alcance do ataque dele).
6. Relatório `tasks/reports/balance-v0.3.md` com antes/depois (velocidades, dano por classe, taxa de vitória e duração por encontro/estratégia) e o que mudou.

## Critérios de aceite

- nenhum monstro `normal` com velocidade ≥ 4,5;
- dano das classes aumentado (~+50% como ponto de partida, desvios justificados);
- `./scripts/check.sh` completo passa;
- relatório criado.

## Resultado

Concluída em 2026-10-16 **no Windows** (a tarefa é de VPS; o designer pediu para fazer aqui porque a VPS estava lenta — nada nela depende de VPS: só `dotnet`, simulador e testes). Relatório completo em `tasks/reports/balance-v0.3.md`.

**Branch:** `task/T-019-rebalanceamento-playtest`, a partir de `docs/tarefas-T-019-T-026` (que traz este arquivo). Só `data/`, `tests/Game.Design.Tests/` e `tasks/`; não conflita com as branches do cliente.

**Feito:**
1. Lobo Cinzento e Rato de Campo 5,0 → 4,0; Salteador 4,2 → 4,0. Nenhum monstro `normal` corre ≥ 4,5 (a classe mais lenta).
2. Dano das classes +50% em todos os ataques básicos e habilidades de dano, **exceto** Explosão Elemental (+30%, 34 → 44): por ser em área, com +50% o `explosion_spam` vencia 96% e mais rápido que a rotação.
3. Reequilíbrio dos monstros para não trivializar: Rato vida 105 → 150, Guarda Esquelético 400 → 560, Matriarca 6000 → 9000, disparo do Salteador 11 → 20, mordida do Lobo 7 → 9.
4. Todos os 21 testes de design anteriores passam **sem afrouxar nenhum limite**.
5. Testes novos: monstros normais mais lentos que a classe mais lenta; Mago e Arqueiro com `kite` tomam claramente menos dano que com `rotation` (Mago 34 × 195; Arqueiro 152 × 203).
6. Relatório `balance-v0.3.md` com antes / só o pedido / depois para os 13 pares encontro×estratégia e a Matriarca com jogador parado por classe.

**Efeito principal:** afastar-se passou a funcionar (Mago `kite` recebe 34 de dano contra 121 antes); lutas do Guerreiro 20 → 13 s e do Arqueiro 26 → 18 s; Mago e Clérigo mantêm ~36 s, com golpes 50% mais fortes.

**Verificação:** `./scripts/check.sh` OK (23 testes de design, ~50 s).

**Pendências / para o próximo playtest:**
- Matriarca continua com ~4,5–5 min (vida 9000 para manter 60–90% com o grupo de bots). Se ficar longa demais, trocar vida por dano.
- O Arqueiro recebe mais dano do Salteador (20) para manter "precisa se mover"; se ficar punitivo para humanos, rever junto com o teste "parado não vence".
- Bots vencem os encontros solo 100%; a dificuldade humana só se confirma jogando (nova rodada da T-025, com a T-026).
