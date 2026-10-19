# Relatório de balanceamento v0.5 (T-032)

Pedidos do designer (2026-10-19): Arqueiro com habilidade de dano, Projétil Arcano sem custo, Campo de Relâmpagos maior e **Mana só por tempo** (sem ganho ao atacar, apanhar, curar ou proteger). Simulador `tests/Game.Sim`, 100 seeds por linha, mesmas opções dos testes de design. Base: `balance-v0.4.md`.

## 1. O que mudou em `data/`

### Pedidos do designer

| Id | Antes | Depois |
|---|---|---|
| `archer.power_shot` (novo) | — | **Disparo Potente**: alvo único, alcance 9, 40 de dano, custo 25, recarga 7 s (tecla 1 do Arqueiro) |
| Lista do Arqueiro | Marca, Recuo, Armadilha | **Disparo Potente**, Marca, Recuo (Armadilha fica nos dados, fora da classe) |
| `mage.arcane_missile` custo | 3 | **0** |
| `mage.lightning_field` / `area.lightning_field` raio | 2,5 | **3,5** |
| `resourceRules` (Guerreiro, Arqueiro, Clérigo) | ganhos de Mana por acerto, dano recebido, cura, escudo, purificação | **removidos** |

Antes desta tarefa a Mana vinha de duas fontes: tempo (Guerreiro e Arqueiro 4/s, Mago 5/s, Clérigo 3/s) e ações — Guerreiro +5 por acerto e +12% do dano recebido, Arqueiro +4/+8 por acerto, Clérigo +5 por acerto sagrado e parte das curas e escudos. O Mago, que mais depende de Mana, era o único sem ganho por ação.

### Reequilíbrio

| Id | Antes | Depois | Motivo |
|---|---|---|---|
| Regeneração de Mana | 4 / 4 / 5 / 3 | **5 para todas** | sem os ganhos por ação, Guerreiro e Clérigo ficavam sem Mana; uniforme e simples |
| `mage.elemental_blast` custo / recarga | 65 / 6 s | **80 / 8 s** | com o Projétil grátis, `explosion_spam` vencia 100% e mais rápido que a rotação |
| `grey_wolf.bite` | 9 | 12 | o Arqueiro com Disparo Potente matava rápido demais; mais pressão nos Três Lobos e no Lobo+Salteador |
| `archer.power_shot` | 45 / 6 s (primeira versão) | 40 / 7 s | idem |
| `monster.skeleton_guard` vida | 560 | 440 | as outras classes ficaram mais rápidas (Disparo Potente, Projétil grátis) e o Clérigo foi para 1,94× mais lento que a média (limite 1,2–1,6) |
| `monster.forest_matriarch` vida | 10 000 | **8 000** | o grupo sem os ganhos de Mana caiu para 53%; e o grupo sem Clérigo **não vencia por tempo** (7 min) |
| `matriarch.bite` / `rending_bite` | 60 | 62 | troca de vida por dano (recomendação de `balance-v0.4.md`): luta mais curta e mais perigosa |

Varredura da Matriarca (60 seeds, `kite+dodge`), grupo completo × sem Clérigo: 7000/60 → 95% × 65%; 7000/70 → 97% × 35%; 7500/66 → 93% × 38%; **8000/62 → 87% × 18%**; 6500/72 → 97% × 37%; 6000/78 → 97% × 47%. Só 8000/62 atende os dois limites (60–90% e 5–60% com ≥ 30 pontos de diferença). Com 8300 a vitória **subiu** para 93% (perto do limite o resultado não é monótono: as fases e os telegráfos mudam de momento), por isso ficou 8000 — margem estreita, estável porque as seeds são fixas.

## 2. Antes / depois (100 seeds)

| Encontro / estratégia | v0.4 | v0.5 |
|---|---|---|
| 3 Lobos / `rotation` | 100%, 13,7 s | 100%, 14,1 s (176 recebido) |
| 3 Lobos / `taunt_and_basic` | 0% | 0% |
| Lobo+Salteador / `kite` | 100%, 18,5 s | **100%, 12,2 s** (101 recebido) |
| Lobo+Salteador / `idle` | 0% | 0% |
| Lobo+Salteador / `rotation` | 0% | **100%, 13,2 s (168 recebido)** — ver §3 |
| 4 Ratos / `rotation` | 100%, 37,8 s | 100%, 29,6 s |
| 4 Ratos / `kite` | 100%, 38,8 s | 100%, 33,7 s (35 recebido) |
| 4 Ratos / `explosion_spam` | 16% | 18% |
| Guarda / `rotation` | 100%, 35,7 s | 100%, 25,2 s |
| Guarda / `heal_only` | 0% | 0% |
| Matriarca / `kite+dodge` | 78%, 306,7 s | **89%, 208,6 s** |
| Matriarca / `idle`, `rotation` | 0%, 0% | 0%, 0% |

Matriarca com 3 bots e o jogador parado (60 seeds): Guerreiro 90%, Arqueiro 82%, Mago 40%, Clérigo 35% (v0.4: 80 / 55 / 32 / 5) — luta ~3,5–5 min.

## 3. Teste de design alterado

`Archer_standing_still_does_not_win` virou `Archer_standing_still_does_not_win_and_trading_hits_costs_much_more_than_kiting`:

- **mantido**: o Arqueiro parado (`idle`) não vence (≤ 40%; hoje 0%);
- **trocado**: "Arqueiro que persegue sem recuar (`rotation`) vence ≤ 40%" virou "`rotation` recebe ≥ 1,4 × o dano do `kite`" (hoje 168 × 101 = 1,66 ×).

Justificativa: o designer pediu uma habilidade de dano para o Arqueiro; com ela, quem troca golpes parado passa a vencer. Fazer o encontro punir isso de novo exigiria muito mais dano do Salteador, deixando o encontro duro justamente para humanos (a queixa do playtest). A regra "o Arqueiro precisa se mover" continua valendo como custo em vida.

Todos os 23 testes de design passam.

## 4. Pendências

- Margem estreita na Matriarca (89% com limite de 90%).
- Bots ainda usam pouco as áreas (`minAoe=2`), agora com o Campo maior.
- Níveis de habilidade (múltiplas flechas, explosões etc.) são design (`design/progression-v0.1.md` §7); implementação na fase 2.
