# Relatório de balanceamento v0.12 (T-341): Sacerdote Morto e Cavaleiro Espectral

Monstros novos das Ruínas da Estrada Antiga (níveis 9–11): o **Sacerdote Morto** (nv10), suporte inimigo atrás dos Guardas Esqueléticos, e o **Cavaleiro Espectral** (nv11, elite), duelo opcional da Torre Quebrada. Design: `design/combat-encounters-v0.1.md` § Ruínas. Testes: `tests/Game.Design.Tests/RuinsDesignTests.cs` (100 seeds por lote) e `tests/Game.Core.Tests/RuinsMonsterTests.cs`.

Perfil de personagem da T-206: "esperado" = nível do grupo, atributos sugeridos, equipamento comum (incomum no Cavaleiro, como pede a tarefa). Estratégias dos bots como na Floresta (balance-v0.11): Guerreiro e Clérigo `rotation+dodge`, Arqueiro e Mago `kite+dodge`. Arena: `map.broken_gate` (corredor de 4 tiles que abre para um pátio).

## 1. Números finais (nível 1; entre parênteses, no nível do monstro)

**Sacerdote Morto** (`monster.dead_priest`, nv10, `ai.support`): vida 180 (828), velocidade 3,0, raio 0,45.

| Habilidade | Números |
|---|---|
| Remendar os Mortos | conjuração 2 s, cura 105 (266) no aliado mais ferido com até 70% da vida, alcance 9, recarga 6 s; não cura a si mesmo |
| Maldição Definhante | arcano 6 (15) + cura recebida −50% por 8 s, alcance 9, recarga 12 s |
| Dardo Sepulcral | arcano 10 (25), alcance 9, recarga 2,2 s |

**Cavaleiro Espectral** (`monster.spectral_knight`, nv11, elite, `ai.brute`): vida 4500 (22 500), velocidade 3,4, raio 0,5.

| Habilidade | Números |
|---|---|
| Escudo Espectral | 6 s, recarga 15 s: devolve 30% do dano direto; quebra com 8% da vida máxima em dano (1800) ou interrupção |
| Golpe Giratório | círculo 2,4, telegráfo 1 s, físico 130 (351), recarga 9 s |
| Lâmina Espectral | arco 100°, alcance 1,9, físico 55 (149), recarga 1,6 s |
| Investida Espectral | linha 8 × 1,4, telegráfo 1 s, avança, físico 140 (378), recarga 10 s; só com o alvo a 3+ tiles |

## 2. Pátio dos Mortos (3 Guardas nv9 + 1 Sacerdote nv10), perfil esperado nv10

**Par** (foco no inimigo mais próximo, o padrão dos bots). Meta: ≥ 80%.

| Par | Vitória | Tempo |
|---|---|---|
| Guerreiro + Clérigo (o do encontro) | 100% | 55,5 s |
| Guerreiro + Arqueiro | 100% | 31,3 s |
| Guerreiro + Mago | 100% | 31,4 s |
| Arqueiro + Clérigo | 100% | 51,5 s |
| Mago + Clérigo | 100% | 45,3 s |

**Solo**, com foco no mais próximo e depois com foco no Sacerdote (`focusFirst`). Meta: média do solo de 40% a 70%.

| Classe | Mais próximo | Foco no Sacerdote |
|---|---|---|
| Guerreiro | 38%, 77,6 s | **100%, 60,3 s** |
| Arqueiro | 3%, 75,4 s | **100%, 78,0 s** |
| Mago | 89%, 82,2 s | **100%, 64,6 s** |
| Clérigo | 100%, 166,7 s | **100%, 101,0 s** |
| **Média** | **57%** | 100% |

**Prioridade de alvo.** No par Guerreiro + Clérigo, focar o Sacerdote leva de 55,5 s a **43,4 s**, e o dano recebido cai de 471 para 70. Sozinho, o foco nunca piora a vitória. Nos outros pares o ganho varia: Guerreiro + Mago vai de 31,4 s a 27,7 s; Guerreiro + Arqueiro fica mais lento (31,3 s → 36,7 s), porque o Arqueiro, recuando, demora a alcançar o Sacerdote atrás dos Guardas. O teste usa o par do encontro.

**Leitura.**
- Sem foco, o Sacerdote cura os Guardas e amaldiçoa o jogador, e quem bate no Guarda mais próximo perde a corrida.
- O Arqueiro é quem mais sofre: o Escudo Erguido do Guarda corta 70% do dano que vem da frente, e quem recua está sempre na frente.
- Com o Sacerdote morto primeiro, os 3 Guardas sozinhos são vencíveis por qualquer classe. Sem o Sacerdote, Guerreiro, Arqueiro e Mago sozinhos vencem os 3 Guardas em 100%, em 51–65 s (sondagem).

**Calibração.**
- Com a cura em si mesmo, o Sacerdote se mantinha vivo contra quem bate de longe: o solo com foco ficava em 10–33% para Arqueiro e Mago. A cura passou a ir só nos aliados.
- A cura começou em 90, subiu para 200 (assim o par ficava preso) e ficou em 105.
- O dardo começou em 12 e ficou em 10; a vida começou em 140 e ficou em 180.

## 3. Cavaleiro Espectral, grupo de 3 com incomuns

Meta: vitória média de 70% a 90%, em 60–120 s.

| Grupo | nv10 | nv11 |
|---|---|---|
| Guerreiro + Arqueiro + Clérigo (o do encontro) | 48%, 94,4 s | 100%, 91,2 s |
| Guerreiro + Mago + Clérigo | 80%, 80,0 s | 100%, 75,8 s |

**Média: 82%** (meta 70–90%), com todos os tempos de vitória entre 75 e 95 s. O grupo recebe entre 4.300 e 5.700 de dano por luta.

**Degrau entre os níveis 10 e 11.** O grupo vive ou morre pela conta "cura do Clérigo contra dano no Guerreiro".
- No nível 11, com um pouco mais de vida, armadura e cura, o grupo fica sempre acima da linha.
- No nível 10, fica perto dela.
- Com dano maior (Lâmina 75, Giro 160, Investida 160), o nível 11 cai para 78–85%, mas o nível 10 vai a 0–25%.

Optei pela média: o Cavaleiro é **difícil para quem chega um nível abaixo** e **seguro para quem está no nível dele**. Ele é opcional e dá a tabela de queda elite.

**Calibração.**
- Com vida 1600, o grupo matava em ~20 s.
- Com 6000, levava 100–120 s e às vezes estourava o tempo.
- O dano subiu em passos até o grupo de nível 10 começar a perder: 22/40/45 → 30/60/70 → 45/100/110 → 55/130/140.

## 4. Solo do Cavaleiro (não é viável)

A proposta pedia que um solo bem equipado vencesse "com dificuldade". Com os números acima, isso não acontece:

| Perfil solo (as 4 classes) | Vitória |
|---|---|
| nv11, incomum | 0% |
| nv11, raro | 0% |
| nv14, raro | Mago 5%, outras 0% |

**Motivo.**
- Um solo tem ~1/3 do dano de um grupo de 3. Ele precisaria de ~3× o tempo (~240 s) contra o mesmo dano por segundo que o tanque do grupo recebe, e sem o Clérigo curando.
- A meta de grupo de 3 (60–120 s, 70–90%) fixa a vida e o dano do Cavaleiro, e com eles o solo não fecha.
- Priorizei a meta testada do grupo.

**Saídas possíveis** (fora desta tarefa):
- versão "solo" mais fraca do Cavaleiro;
- dano dele mais concentrado em telegráfos, que um solo que desvia evita.

Registrado como pendência.

## 5. Mecânicas novas no Core

- **`modifiers.healingTakenMultiplier`** (status): multiplica toda cura recebida pelo efeito `heal`: curas, Corrente Sagrada, poções e efeitos periódicos. É a Maldição Definhante (0,5).
- **`reflect.fraction`** (status): devolve essa fração do dano direto recebido a quem atacou, na mesma escola e sem multiplicadores.
  - Dano vindo de status não é devolvido (veneno, sangramento, outros reflexos), então dois reflexos não se rebatem.
- **`breaks`** (status): o status acaba antes do tempo de duas formas.
  - `damageFraction`: ao receber essa fração da vida máxima em dano desde a aplicação (renovar zera a conta).
  - `interrupt`: ao receber um efeito `interrupt`, mesmo sem estar conjurando.
- **`ai.support`**: perfil do Sacerdote (cura o aliado mais ferido, fica atrás dos protetores).
- **`ai.brute` + `ai.buff`**: habilidade em si mesmo, usada quando o alvo chega a `buffDistance`.
- **Interrupção de monstro:** confirmado que o Golpe Pesado do Guerreiro cancela a cura do Sacerdote, e a cura não acontece.

## 6. Conferências

- Testes de Core: `RuinsMonsterTests` (13), que cobrem cura reduzida, reflexo, rebote, quebra por dano, renovação, quebra por interrupção, dados reais, cura no mais ferido sem se curar, limiar, interrupção pelo Guerreiro, posição atrás dos Guardas, escudo e investida do Cavaleiro.
- Os outros testes de design não mudaram: nenhum monstro antigo foi alterado, e a opção nova dos bots (`focusFirst`) vem desligada.
- `./scripts/check.sh` completo passa.
