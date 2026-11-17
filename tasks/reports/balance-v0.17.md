# Relatório de balanceamento v0.17 (T-387): Guarda Esquelético com ataque básico

Relato do playtest da fase 3 (T-380), em 2026-11-16: o Guarda Esquelético não tinha ataque básico. O perfil `ai.guardian` só alternava guarda (`skeleton_guard.raise_shield`) e golpe pesado (`skeleton_guard.crushing_blow`). O Golpe de Lâmina (`skeleton_guard.slash`, recarga de 1,6 s, arco de 1,8 tile) estava na lista do monstro, mas o perfil nunca o usava. Com o golpe pesado em recarga, o guarda ficava parado em guarda até ele voltar.

## 1. Mudança

`GuardianProfile`, fase "perseguir":

- **golpe pesado em recarga:** o guarda age como o `ai.melee_chaser`, ou seja, usa o Golpe de Lâmina quando está pronto e persegue até o alcance dele;
- **golpe pesado pronto e alvo no alcance:** o ciclo não muda (guarda por 3 s, golpe pesado telegrafado, recuperação).

A guarda continua como antes. O guarda fica parado e só se vira para o alvo a cada 1 s, então **a janela para atacar pelo flanco se mantém**. O Golpe de Lâmina nunca é usado durante a guarda nem durante o golpe pesado.

O Vigia invoca o mesmo `monster.skeleton_guard`, que usa o mesmo perfil, então o guarda invocado também passa a golpear.

**Dados:** o dano do Golpe de Lâmina subiu de **9 para 11** (`data/abilities_monsters.json`). O motivo está na seção 3.4.

## 2. Método

O lado "antes" é o código de `main` (533f3fe) e o lado "depois" é a branch da tarefa, com IA nova e dano 11. Os dois rodaram com as mesmas seeds.

- **Simulador** (`tests/Game.Sim`), 200 execuções:
  - `encounter.skeleton_guard`: Clérigo sozinho, perfil esperado, níveis 9 e 10 (monstro no mesmo nível), estratégias `rotation`, `rotation+dodge` e `kite+dodge`;
  - `encounter.dead_courtyard`: Guerreiro e Clérigo, perfil nv9, estratégias `rotation` e `rotation+dodge`.
- **Testes de design** que têm o guarda, 100 seeds por lote (50 nas comparações do Vigia):
  - `RuinsZoneDesignTests`: grupos solo e Pátio em dupla;
  - `RuinsDesignTests`: Pátio;
  - `CryptDesignTests`: salas da Cripta com grupo de 4;
  - `WatcherDesignTests`: Vigia, que invoca guardas.

## 3. Resultados

### 3.1 Guarda sozinho (`encounter.skeleton_guard`, Clérigo)

| Nível | Estratégia | Vitória | Tempo (antes → depois) | Dano recebido (antes → depois) |
|---|---|---|---|---|
| 9 | rotation | 100% → 100% | 48,5 → 44,3 s | 265 → 427 |
| 9 | rotation+dodge | 100% → 100% | 45,5 → 44,4 s | **0 → 196** |
| 9 | kite+dodge | 100% → 100% | 47,7 → 41,1 s | 27 → 16 |
| 10 | rotation | 100% → 100% | 49,7 → 45,0 s | 289 → 458 |
| 10 | rotation+dodge | 100% → 100% | 46,3 → 45,2 s | **0 → 211** |
| 10 | kite+dodge | 100% → 100% | 48,6 → 41,9 s | 29 → 17 |

- **Antes**, quem desviava do golpe telegrafado não recebia dano nenhum. É o sintoma relatado no playtest.
- **Depois**, quem fica no corpo a corpo leva o Golpe de Lâmina entre os golpes pesados. Quem mantém distância (`kite`) quase não é atingido, como esperado.
- **As lutas ficaram 1 a 7 s mais curtas.** Antes, o guarda passava a maior parte do tempo em guarda, que reduz em 70% o dano frontal. Agora ele só entra em guarda quando o golpe pesado está pronto.

### 3.2 Pátio dos Mortos (`encounter.dead_courtyard`, Guerreiro + Clérigo nv9)

| Estratégia | Vitória | Tempo | Dano recebido |
|---|---|---|---|
| rotation | 100% → 100% | 59,3 → 47,7 s | 1.104 → 1.178 |
| rotation+dodge | 100% → 100% | 62,1 → 48,4 s | 505 → 804 |

### 3.3 Testes de design das Ruínas e da Cripta

Todos os testes ativos continuam passando: 52 aprovados, 0 falhas, 5 pendentes do rebalanceamento do playtest.

**Ruínas, grupos solo:** perfil esperado do nível do grupo, 4 classes. A vitória é de 100% antes e depois em todos os grupos.

| Grupo | Guerreiro | Arqueiro | Mago | Clérigo |
|---|---|---|---|---|
| Portão Partido: 1 Guarda nv9 | 35,1 → 31,6 s | 24,1 s (=) | 23,1 s (=) | 38,6 → 36,5 s |
| Portão Partido: 2 Guardas nv9 | 37,2 → 34,0 s | 47,5 s (=) | 39,2 s (=) | 64,9 → 64,2 s |
| Estrada: 2 Guardas nv10 | 36,0 → 32,6 s | 45,8 s (=) | 39,1 s (=) | 66,0 → 65,2 s |
| Estrada: Guarda nv11 + nv10 | 36,1 → 32,2 s | 37,4 s (=) | 25,8 s (=) | 59,9 → 61,3 s |
| Estrada: 2 Guardas nv11 | 36,2 → 32,8 s | 39,7 s (=) | 27,6 s (=) | 63,3 → 63,9 s |

O Arqueiro e o Mago mantêm distância, então os números deles não mudam.

**Pátio dos Mortos (dupla, perfil nv10):** a vitória é de 100% antes e depois em todas as 5 duplas e nos dois grupos.

- Guerreiro + Clérigo: 60 → 45 s;
- nas outras duplas, o tempo muda menos de 2 s.

`RuinsDesignTests` mede o Pátio (3 Guardas nv9 + Sacerdote nv10) com o perfil esperado nv10.

Focando o Sacerdote primeiro (teste `Focusing_the_priest_first_finishes_faster`, que segue passando):

| Composição | Vitória | Tempo | Dano recebido |
|---|---|---|---|
| Guerreiro + Clérigo | 100% → 100% | 43,1 → 36,6 s | 69 → 415 |
| Guerreiro sozinho | 100% → 100% | 62,7 → 51,5 s | 110 → 603 |
| Clérigo sozinho | 100% → 100% | 101,0 → 102,3 s | 154 → 1.167 |
| Arqueiro / Mago sozinhos | 100% → 100% | sem mudança relevante | sem mudança relevante |

Atacando o alvo mais próximo, sozinho, que é o cenário do teste pendente `Solo_against_the_dead_courtyard_is_possible_but_risky` (meta: média das 4 classes entre 40% e 70%, e uma classe com pelo menos 50%):

| Classe | Antes | Depois |
|---|---|---|
| Guerreiro | 92% | **3%** |
| Arqueiro | 31% | 33% |
| Mago | 100% | 100% |
| Clérigo | 100% | 100% |
| **Média** | **81%** (fora da meta) | **59%** (dentro da meta) |

**Cripta do Vigia (grupo de 4):** nenhuma mudança relevante. A vitória é de 98–100% e o tempo é o mesmo, porque o grupo derruba os guardas em 7–16 s.

A única variação foi nas mortes por luta em Salão dos Escudos com Cavaleiro + 3 Guardas: 0,23 → 0,26.

### 3.4 Vigia Sem Rosto e a escolha do dano

O Vigia invoca 2 Guardas. O teste `Without_the_cleric_the_win_rate_drops_clearly` exige que um grupo sem Clérigo (Guerreiro + Arqueiro + 2 Magos, nv15) vença no máximo 30%. Em `main` o teste já ficava perto do limite.

| Variante | Sem Clérigo, 50 execuções (meta ≤ 30%) |
|---|---|
| `main` (dano 9, sem a IA nova) | 28% |
| IA nova, dano 9 | 34% ❌ |
| IA nova, dano 11 | **30%** ✅ |
| IA nova, dano 13 | 28% ✅ |

O grupo completo segue com 99% de vitória em todas as variantes.

**Por que o teste piorou só com a IA nova:** o guarda invocado passa menos tempo em guarda, morre mais rápido e alivia o grupo sem cura. Com dano 9, a mudança deixava o Vigia **mais fácil** nesse cenário.

**Por que 11:**

- é o menor dano que mantém a meta;
- 13 dá o mesmo resultado no Pátio solo (Guerreiro 2%, média 59%) e pesa mais no resto;
- a decisão foi do designer, em 2026-11-17.

O teste do Vigia ficou **exatamente no limite** (15 vitórias em 50). Qualquer ajuste futuro no Vigia ou nos guardas pode derrubá-lo.

## 4. Conclusão

- O guarda agora causa dano constante no corpo a corpo, além do golpe pesado telegrafado. Isso pune ficar parado na frente dele.
- Taxas de vitória:
  - nos testes ativos, nenhuma caiu;
  - as lutas ficaram mais curtas;
  - o solo arriscado do Pátio entrou na meta de design.
- **Golpe de Lâmina: dano 9 → 11.**
- Para o próximo playtest:
  - **Guerreiro sozinho no Pátio:** atacando o mais próximo, vence só 3% (antes, 92%). Focando o Sacerdote primeiro, vence 100%. Confirmar se esse é o recado que o Pátio deve passar;
  - **Clérigo sozinho contra o guarda:** dano recebido de 0–265 para ~200–430 por luta. Conferir se está confortável;
  - **teste do Vigia sem Clérigo:** ficou em 30%, no limite da meta.
