# Relatório de balanceamento v0.4 (T-027)

Decisões do designer (2026-10-17): **todas as classes usam Mana**; o **Mago ganha área à distância** (nova habilidade) e a **Explosão Elemental fica instantânea**. Simulador `tests/Game.Sim`, 100 seeds por linha, mesmas opções dos testes de design. Base de comparação: `balance-v0.3.md`.

## 1. O que mudou em `data/`

### Recursos

| Classe | Antes | Depois |
|---|---|---|
| Guerreiro | Vigor, começa 0, −3/s fora de combate | **Mana**, começa 100, +4/s |
| Arqueiro | Foco, começa 50, +3/s | **Mana**, começa 100, +4/s |
| Mago | Mana, começa 100, +5/s | igual |
| Clérigo | Fé, começa 40, sem regeneração | **Mana**, começa 100, +3/s |

- Custos das habilidades: mesmos valores, agora em `mana` (Golpe Pesado 30, Golpe Circular 25, Marca 10, Armadilha 25, Cura 20, Escudo 15, Corrente 25).
- As regras de ganho por evento de cada classe (`resourceRules`) foram **mantidas**, agora gerando Mana: o Guerreiro ganha ao bater e apanhar, o Arqueiro ao acertar de longe ou alvo marcado, o Clérigo ao curar e proteger. Assim cada classe continua tendo um jeito próprio de se reabastecer, mesmo com um recurso único.
- O Clérigo regenera menos (+3/s) porque também ganha Mana com curas e escudos.

### Mago

| Id | Antes | Depois |
|---|---|---|
| `mage.elemental_blast` | conjuração 0,6 s, recarga 4 s | **instantânea**, recarga 6 s (custo 65) |
| `mage.lightning_field` (novo, tecla 4) | — | Campo de Relâmpagos: área à distância (alcance 9, raio 2,5), 12 de dano no impacto + zona `area.lightning_field` por 5 s com 8 de dano por segundo; custo 40, recarga 12 s |

Campo de Relâmpagos está na lista de habilidades do Mago em `design/classes-v0.1.md` ("zona de dano"). O dano no impacto faz os bots o reconhecerem como habilidade de dano em área (nenhuma mudança em `Game.Bots`).

### Outros

| Id | Antes | Depois | Motivo |
|---|---|---|---|
| `monster.forest_matriarch` vida | 9000 | 10000 | com Mana para Guerreiro e Clérigo (habilidades desde o início) e o Campo, o grupo `kite+dodge` foi para 91% (limite 60–90%); 10000 deixa margem |

## 2. Antes / depois (100 seeds)

| Encontro / estratégia | v0.3 | v0.4 |
|---|---|---|
| 3 Lobos / `rotation` | 100%, 13,4 s | 100%, 13,7 s |
| 3 Lobos / `taunt_and_basic` | 0% | 0% |
| Lobo+Salteador / `kite` | 100%, 18,5 s | 100%, 18,5 s |
| Lobo+Salteador / `idle`, `rotation` | 0%, 0% | 0%, 0% |
| 4 Ratos / `rotation` | 100%, 36,6 s | 100%, 37,8 s |
| 4 Ratos / `kite` | 100%, 38,0 s (34 recebido) | 100%, 38,8 s (35 recebido) |
| 4 Ratos / `explosion_spam` | 24% | 16% |
| Guarda / `rotation` | 100%, 35,7 s | 100%, 35,7 s |
| Guarda / `heal_only` | 0% (derrota) | 0% (tempo esgotado: com Mana o Clérigo não morre, mas não vence) |
| Matriarca / `kite+dodge` | 80%, 272,5 s | 78%, 306,7 s |
| Matriarca / `idle`, `rotation` | 0%, 0% | 0%, 0% |

Matriarca com 3 bots e o jogador parado (só desvia), 60 seeds: Guerreiro 80%, Arqueiro 55%, Mago 32%, Clérigo 5% (v0.3: 78 / 72 / 43 / 13).

Todos os 23 testes de design passam sem afrouxar limites.

## 3. Observações

- **Bots e área**: o bot só usa habilidade em área com 2+ inimigos no raio (`minAoe=2`); contra os ratos espalhados o Mago bot usa Explosão ~1 vez e Campo quase nunca por luta. Na Matriarca usa Campo ~2 vezes por luta. O jogador humano pode usar bem mais — os números de vitória dos bots subestimam o Mago com área.
- **Matriarca longa (~5 min)**: a vida subiu de novo (10000) para manter 60–90% com o grupo de bots, e a vitória dos bots com o jogador parado caiu. Recomendação para a próxima rodada: trocar vida por dano/pressão (mais curta e mais perigosa), em vez de seguir subindo a vida.
- **Pré-visualização da área e aviso de conjuração interrompida**: T-028 (cliente).
