# Relatório de balanceamento v0.3 (T-019)

Pedido do playtest humano (T-025, 2026-10-16): "os bichos são rápidos demais, as classes além do Guerreiro têm dificuldade, o dano das habilidades deveria ser maior". Ponto de partida pedido pelo designer: monstros mais lentos e **+50% de dano das classes**.

Simulador `tests/Game.Sim`, conteúdo real de `data/`, 100 seeds por linha (seed inicial 1, desvio de posição 0,5), mesmas opções dos testes de design (`class.mage:explosion=mage.elemental_blast`). Rodado no Windows (a T-019 era de VPS; feita aqui a pedido do designer). Base de comparação: `balance-v0.2.md`.

## 1. Números alterados em `data/`

### Pedido do designer

| Id | Antes | Depois | |
|---|---|---|---|
| `monster.grey_wolf` velocidade | 5,0 | 4,0 | mais lento que a classe mais lenta (4,5) |
| `monster.field_rat` velocidade | 5,0 | 4,0 | idem |
| `monster.road_bandit` velocidade | 4,2 | 4,0 | idem (margem maior para o Arqueiro) |
| `warrior.sword_strike` | 12 | 18 | +50% |
| `warrior.heavy_strike` | 34 | 51 | +50% |
| `warrior.whirlwind` | 18 | 27 | +50% |
| `archer.shot` | 11 | 17 | +50% (arredondado) |
| `mage.arcane_missile` | 10 | 15 | +50% |
| `mage.elemental_blast` | 34 | **44** | +30% — ver §2 |
| `mage.ice_prison` | 6 | 9 | +50% |
| `cleric.holy_strike` | 8 | 12 | +50% |
| `cleric.holy_chain` | 26 | 39 | +50% |

Curas e escudos não mudaram (o pedido foi dano).

### Reequilíbrio para manter os encontros desafiadores

| Id | Antes | Depois | Motivo |
|---|---|---|---|
| `monster.field_rat` vida | 105 | 150 | com +50% o Mago vencia em 22 s e a Explosão sozinha vencia 100%, mais rápido que a rotação |
| `monster.skeleton_guard` vida | 400 | 560 | o Clérigo deixou de ser 1,2–1,6× mais lento que as outras classes (ficou 1,11×) |
| `road_bandit.shot` base | 11 | 20 | o Arqueiro que só troca golpe parado (`rotation`) passou a vencer 100%; o design pede que ele precise se afastar |
| `grey_wolf.bite` base | 7 | 9 | idem (com 3 poções o Arqueiro parado ainda vencia) |
| `monster.forest_matriarch` vida | 6000 | 9000 | o grupo `kite+dodge` foi para 99% (limite 60–90%) |

## 2. Desvios do +50%

- **Explosão Elemental +30% (44)**: dano em área multiplica o ganho por 4 contra os ratos. Com 51, `explosion_spam` vencia 96% e mais rápido que a rotação, quebrando a regra do design ("se a Explosão resolver o encontro sozinha, o Mago está forte demais") mesmo com ratos de 150 de vida.
- Nenhum limite de teste foi afrouxado: todos os 21 testes de design anteriores passam com os números novos.

## 3. Antes / depois (100 seeds)

| Encontro / estratégia | Antes (v0.2): vitória, tempo, dano recebido | Só o pedido (vel. 4,0 + dano ×1,5) | Depois (v0.3) |
|---|---|---|---|
| 3 Lobos / `rotation` | 100%, 20,4 s, 153 | 100%, 15,1 s, 116 | **100%, 13,4 s, 126** |
| 3 Lobos / `taunt_and_basic` | 0% | 100%, 25,9 s | 0% |
| Lobo+Salteador / `kite` | 100%, 25,6 s, 125 | 100%, 18,5 s, 84 | **100%, 18,5 s, 152** |
| Lobo+Salteador / `idle` | 0% (tempo) | 0% | 0% |
| Lobo+Salteador / `rotation` | 6% | 100% | 0% |
| 4 Ratos / `rotation` | 97%, 36,2 s, 202 | 100%, 22,1 s, 87 | **100%, 36,6 s, 195** |
| 4 Ratos / `kite` | 100%, 38,1 s, 121 | 100%, 23,4 s, 22 | **100%, 38,0 s, 34** |
| 4 Ratos / `explosion_spam` | 30%, 28,2 s | 100%, 19,1 s | 24%, 32,0 s |
| Guarda / `rotation` | 100%, 37,3 s, 120 | 100%, 20,7 s, 60 | **100%, 35,7 s, 120** |
| Guarda / `heal_only` | 0% | 0% | 0% |
| Matriarca / `kite+dodge` | 78%, 307,9 s | 99%, 177,0 s | **80%, 272,5 s** |
| Matriarca / `idle`, `rotation` | 0%, 0% | 0%, 0% | 0%, 0% |

Leitura:
- **Afastar-se agora funciona**: o Mago que se afasta dos ratos recebe 34 de dano contra 195 de quem fica trocando golpes (antes 121 contra 202 — com ratos mais rápidos que ele, o kite pouco ajudava).
- Lutas solo mais curtas para Guerreiro (20 → 13 s) e Arqueiro (26 → 18 s). Mago e Clérigo mantêm a duração (~36 s), mas com muito mais dano por golpe e com a opção real de manter distância.
- O Arqueiro parado ou perseguindo sem recuar continua perdendo — o design dele é se mover.

### Matriarca com 3 bots e o jogador parado (só desvia), 60 seeds

Situação do menu da T-024 quando o jogador "não faz nada além de sobreviver":

| Jogador | Antes (v0.2) | Depois (v0.3) |
|---|---|---|
| Guerreiro | 77% | 78% |
| Arqueiro | 80% | 72% |
| Mago | 43% | 43% |
| Clérigo | 15% | 13% |

Praticamente igual: a vida maior da Matriarca compensou o dano maior dos bots. Com o jogador atacando de verdade (dano +50%), o grupo deve vencer mais e mais rápido que antes.

## 4. Testes novos (`tests/Game.Design.Tests`)

- `Normal_monsters_are_slower_than_the_slowest_class`: nenhum monstro `normal` com velocidade ≥ à da classe mais lenta (hoje 4,5). Falharia com os números da v0.2 (Lobo e Rato em 5,0).
- `Kiting_mage_and_archer_take_clearly_less_damage_than_standing_and_trading`: Mago `kite` vence ≥ 85% e recebe ≤ 50% do dano da `rotation` (hoje 34 contra 195); Arqueiro `kite` recebe ≤ 85% do dano da `rotation` (152 contra 203 — margem menor porque o Salteador atira de longe).

Total: 23 testes de design passando (~50 s no Windows).

## 5. Limites e próximos passos

- **Matriarca longa**: ~4,5–5 min com o grupo de bots. O limite de 60–90% obrigou a subir a vida para 9000; se o playtest achar a luta longa demais, o caminho é trocar vida por dano (mais pressão, menos duração) e revalidar.
- **Bots não são humanos**: os bots desviam e se afastam perfeitamente; os encontros solo continuam fáceis para eles (100%). A dificuldade humana precisa ser confirmada na próxima rodada da T-025 (com a T-026, ataque segurando o botão / Espaço).
- O Arqueiro recebeu mais dano do Salteador (20) para preservar "precisa se mover": se o playtest achar o Arqueiro punido demais, reduzir o disparo e afrouxar o teste de "parado não vence" com justificativa.
