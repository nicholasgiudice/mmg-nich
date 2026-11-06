# Relatório de balanceamento v0.11 (T-340): monstros da Floresta Baixa

A Fada da Névoa (nv6) e o Ent Corrompido (nv7) são os primeiros monstros novos desde o protótipo. O design está em `design/combat-encounters-v0.1.md` § Floresta Baixa, e os números em `data/monsters.json`, `data/abilities_monsters.json` e `data/statuses.json`. Simulador `tests/Game.Sim`, 100 seeds por linha, mesmas opções dos testes de design. A base é o `balance-v0.7.md` (perfis por nível) e o `balance-v0.8.md` (grupos típicos dos Campos).

Testes: `tests/Game.Design.Tests/ForestDesignTests.cs` (design) e `tests/Game.Core.Tests/ForestMonsterTests.cs` (mecânicas).

## 1. Encontros e método

| Encontro | Composição | Nível do grupo | Uso |
|---|---|---|---|
| `encounter.mist_fairies` | 2 Fadas nv6 | 6 | solo, 4 classes |
| `encounter.corrupted_ent` | 1 Ent nv7 | 7 | solo, 4 classes |
| `encounter.mist_ambush` | 3 Aranhas Menores nv5 + 1 Fada nv6 (atrás) | 6 | Guerreiro + Arqueiro, contra cada um deles solo |

- **Arena:** a aberta do protótipo (`map.fields`), como nos Campos. Obstáculos e caminhos estreitos da Floresta ficam para o mapa da T-342.
- **Perfil:** o do nível do grupo (T-206). O "esperado" tem atributos sugeridos, equipamento comum do nível e NH focado; o "fraco" está 3 níveis abaixo. O Clérigo solo usa o par Corrente + Escudo, como nos outros solos.
- **Bots:** Guerreiro e Clérigo com `rotation+dodge`, Arqueiro e Mago com `kite+dodge`. A diferença em relação aos Campos é o `+dodge`, os bots desviam dos telegráfos. A Floresta ensina posicionamento, e sem desvio as Raízes acertavam sempre (§2).
- **Limite:** 120 s por luta, como nos solos do protótipo.

## 2. Como se chegou aos números (sondagens de 20–100 seeds)

1. **Proposta inicial** (Fada com vida 60, velocidade 4,2 e fuga contínua; Ent com vida 420, físico 35%). Com 60 de vida, as Fadas morriam em 4–6 s para Arqueiro e Mago, mesmo no perfil fraco. Mais rápida que o Guerreiro (4,2 contra 4,5 × 0,7 com a lentidão), a Fada fugia para sempre: **Guerreiro 0%**.
2. **A fuga ficou só na névoa.** Visível, a Fada não corre: atira e, quando alguém chega perto, conjura o véu (0,6 s) e se afasta na névoa. Vida 110, velocidade 3,4, lentidão de 30% para 20%. O Guerreiro passou a vencer 100% em 27 s.
3. **Bug de linha de visão:** sem linha de visão e já dentro da distância preferida, a Fada parava (o `MoveToward` achava que tinha chegado). O bot do Clérigo também não se mexe para recuperar a visão, então os dois ficavam parados até o tempo acabar (11% de empates do Clérigo). Agora, sem linha de visão, a Fada anda até enxergar o alvo. O `ai.ranged_kiter` do Salteador tem o mesmo comportamento; não foi mexido, para não mudar o balanço dos Campos (§5).
4. **Ent contra quem foge:** com os bots desviando, Arqueiro e Mago levavam 0–7 de dano e venciam **100% mesmo fracos**: o Ent (2,4) não alcança ninguém. Entrou a **Rajada de Espinhos** (alcance 8, 14 de natureza a cada 2,5 s), só com o alvo a 3 tiles ou mais. Com 17 de dano, o Arqueiro esperado caía para 38%; com 14, ficou em 100%, e o fraco perde.
5. **Ent sem golpear o vazio:** o `ai.melee_chaser` usa qualquer habilidade de área assim que sai da recarga, porque a validação de área nunca falha. O Pisão sairia com o alvo a 10 tiles. O perfil novo `ai.brute` só usa cada golpe com o alvo ao alcance e fica parado durante o próprio telegráfo.

## 3. Resultados (`ForestDesignTests`)

### Perfil esperado: ≥ 85% em até 40 s

| Encontro | Classe | Vitória | Tempo médio (s) | p90 (s) | Dano recebido |
|---|---|---|---|---|---|
| 2 Fadas nv6 | Guerreiro | 100% | 26,7 | 26,9 | 344 |
| | Arqueiro | 100% | 9,6 | 10,5 | 155 |
| | Mago | 100% | 9,7 | 9,8 | 134 |
| | Clérigo | 100% | 24,1 | 26,0 | 340 |
| Ent nv7 | Guerreiro | 100% | 35,7 | 36,2 | 304 |
| | Arqueiro | 100% | 24,1 | 25,6 | 211 |
| | Mago | 100% | 23,1 | 24,1 | 191 |
| | Clérigo | 100% | 30,3 | 31,7 | 314 |

### Perfil fraco (3 níveis abaixo): ≤ 50%

| Encontro | Classe | Vitória | Tempo médio (s) |
|---|---|---|---|
| 2 Fadas, perfil nv3 | Guerreiro | 8% | 33,6 |
| | Arqueiro | 0% | — |
| | Mago | 0% | — |
| | Clérigo | **100%** | 33,7 (1,40× o esperado) |
| Ent, perfil nv4 | Guerreiro | 9% | 50,9 |
| | Arqueiro | 0% | — |
| | Mago | 39% | 40,3 |
| | Clérigo | **100%** | 44,5 (1,47× o esperado) |

**Exceção do Clérigo.** É a mesma do Guarda Esquelético (`balance-v0.7.md` §3): sozinho, o Clérigo se cura sem parar e vence mesmo fraco, só que mais devagar. O teste aceita que ele demore pelo menos 1,3× o esperado. Fazê-lo perder exigiria mais dano dos monstros, o que derrubaria o Guerreiro esperado contra o Ent, hoje com 35,7 s para um limite de 40.

### Vantagem de grupo: Emboscada na Névoa (perfil esperado nv6)

| Quem | Vitória | Tempo médio (s) | Dano recebido (por jogador) |
|---|---|---|---|
| Guerreiro + Arqueiro | 100% | 7,3 | 47 (93 no total) |
| Guerreiro solo | 100% | 15,3 | 219 |
| Arqueiro solo | 100% | 13,5 | 114 |
| Mago solo (referência) | 100% | 9,8 | 77 |
| Clérigo solo (referência) | 100% | 27,9 | 645 |

O par leva **51%** do tempo médio dos dois solos (meta ≤ 70%). Cada um recebe **28%** do dano que receberia sozinho (meta ≤ 60%). Solo continua possível: todas as classes esperadas vencem 100%. Perfil fraco solo: 0–10%.

## 4. Mecânicas novas no Core

- **`flags.untargetable`** (`StatusFlags.Untargetable`, `StatusManager.IsUntargetable`): ninguém além da própria entidade a escolhe como alvo de unidade (`InvalidTarget`). Ela também fica fora de arcos, linhas, círculos, telegráfos, áreas persistentes, correntes, alvos extras e da troca de alvo dos acertos extras. Status que ela já tem continuam valendo.
- **Fraqueza** = resistência negativa. A fórmula já aceitava (`1 − resistência`), e agora há teste.
- **Perfis de IA:** `ai.mist_kiter` (`MistKiterProfile`) e `ai.brute` (`BruteProfile`).

## 5. Riscos e pendências

- **Arena aberta.** Na Floresta real, com árvores, a Fada perde a linha de visão com frequência. O Clérigo e o Salteador ficam parados atrás de obstáculos pelo motivo do §2 (bot e `ai.ranged_kiter`). Vale revisar quando a T-342 tiver o mapa.
- **Ent contra Guerreiro**, 35,7 s para um limite de 40: é a classe mais justa. Mais vida ou mais resistência física para o Ent passam pelo Guerreiro primeiro.
- **Ritmo de XP na Floresta** não foi medido aqui (não há zona da Floresta ainda). Fica para a T-342, com o modelo de `FieldGroups.Pace`.
- **Cliente (T-345):** mostrar a conjuração do véu e o estado "na névoa" (o status `status.in_mist` chega no snapshot). O cliente também deve impedir a seleção de alvo na névoa; o Core já rejeita.
- O relatório é v0.11 porque a T-340 pediu esse número. Os v0.9 e v0.10 são de outras tarefas da fase 3.
