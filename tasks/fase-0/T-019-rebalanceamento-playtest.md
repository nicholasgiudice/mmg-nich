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

_(preencher ao concluir)_
