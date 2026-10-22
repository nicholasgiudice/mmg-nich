# Relatório de balanceamento v0.6 (T-063): níveis de habilidade

Primeira medição dos **níveis de habilidade (NH)** da trilha T-060..T-062. Simulador `tests/Game.Sim`, 100 seeds por linha, as mesmas opções dos testes de design (`class.mage:explosion=mage.elemental_blast`). Base: `balance-v0.5.md` (NH1).

**Nenhum número de `data/` mudou nesta tarefa**, por isso `design/skills-v0.1.md` continua igual. Há uma decisão de design pendente (seção 4).

## 1. O que o simulador e os bots ganharam

- `--ranks <perfil>`: `nh1` (padrão, o protótipo), `focado` e `espalhado`. `--rank classe:habilidade=NH` muda uma habilidade (repetível, por cima do perfil). O rótulo do lote ganha `NH:<perfil>`.
- Os perfis gastam os **15 pontos do nível 15** (§7 de `design/progression-v0.1.md`):

| Perfil | Normais | Assinatura | Pares no NH5 (focado) |
|---|---|---|---|
| `focado` | duas no NH5, duas no NH1 | NH3 | Guerreiro: Golpe Pesado + Golpe Circular · Arqueiro: Disparo Potente + Marca · Mago: Explosão + Campo · Clérigo: Cura Direta + Escudo Sagrado |
| `espalhado` | as quatro no NH3 | NH3 | — |

- Os bots passaram a ler a definição **do NH que o personagem tem** (antes classificavam sempre pelo NH1). Cargas por recarga, acertos extras, saltos e alvos extras já vinham do Core: nenhuma estratégia nova foi preciso. A assinatura entra na rotação quando tem NH (no perfil `nh1` ela fica fora da barra).

**Limite importante:** os encontros do simulador são os do protótipo (monstros do nível 1), e os valores dos NH em `data/` são o "perfil nível 1". Então os perfis acima são **distribuições de nível 15 com os atributos e os monstros do nível 1**: servem para comparar distribuições entre si, não para medir a curva de dificuldade. Para isso faltam monstros e atributos por nível (fase 2).

## 2. Distribuições de pontos (meta: as duas vencem ≥ 70%; nenhuma é a única)

| Encontro (estratégia) | NH1 | focado | espalhado |
|---|---|---|---|
| Três Lobos — Guerreiro (`rotation`) | 100%, 14,1 s | 100%, 7,6 s | 100%, 12,4 s |
| Lobo + Salteador — Arqueiro (`kite`) | 100%, 8,3 s | 100%, 3,1 s | 100%, 2,7 s |
| Quatro Ratos — Mago (`rotation`) | 100%, 29,6 s | 100%, 6,6 s | 100%, 6,4 s |
| Guarda Esquelético — Clérigo (`rotation`) | 100%, 25,2 s | 100%, 25,2 s | 100%, 19,1 s |
| Matriarca — grupo (`kite+dodge`) | 89%, 196 s | 100%, 94 s | 97%, 114 s |

- **Meta atendida**: as duas distribuições vencem tudo; nenhuma é a única que vence.
- Os tempos curtos (Arqueiro em 3 s, Mago em 6 s) vêm da **assinatura no NH3**: a Flecha Devastadora (170) e o Cataclismo (221 em área) matam monstros do nível 1 de uma vez. Esperado com monstros fracos; não é sinal de assinatura forte demais.
- O Clérigo `focado` (Cura + Escudo no NH5) é tão lento quanto o NH1 no encontro solo: as habilidades que ele subiu não causam dano. Coerente com o papel de suporte; o perfil "dano" do Clérigo seria Corrente no NH5.

## 3. "Nenhuma habilidade no NH5 sozinha resolve o encontro" (mesma regra da Explosão)

Regra do NH1: o `explosion_spam` da habilidade vence no máximo 50% **ou** demora mais que a rotação com os mesmos NH. Aqui só a habilidade testada está no NH5; o resto no NH1, sem assinatura.

| Habilidade NH5 | Encontro | Rotação | Spam | Regra |
|---|---|---|---|---|
| Golpe Pesado | Três Lobos | 100%, 11,4 s | 100%, 13,7 s | ok (mais lento) |
| Golpe Circular | Três Lobos | 100%, 9,5 s | 100%, 9,7 s | ok, por pouco |
| Disparo Potente | Lobo + Salteador | 100%, 3,7 s | 100%, 8,9 s | ok |
| Chuva de Flechas | Lobo + Salteador | 100%, 5,3 s | 100%, 10,4 s | ok |
| Corrente Sagrada | Guarda Esquelético | 100%, 18,4 s | 100%, 18,4 s | não se aplica: a rotação solo do Clérigo já é só a Corrente |
| **Explosão Elemental** | Quatro Ratos | 100%, 25,9 s | **100%, 16,6 s** | **falha** |
| **Campo de Relâmpagos** | Quatro Ratos | 100%, 24,1 s | **100%, 20,2 s** | **falha** |

## 4. Decisão pendente: Explosão e Campo do Mago

No NH1 o spam da Explosão perde (18%) porque a Mana acaba antes dos ratos. Do **NH3** em diante (2 explosões = 1,7× o dano pelos mesmos 80 de Mana) a Explosão sozinha já mata os 4 ratos antes de a Mana acabar:

| Explosão | Rotação | Spam |
|---|---|---|
| NH1 (44) | 100%, 29,6 s | 18%, 29,6 s |
| NH3 (2 × 37) | 100%, 27,9 s | **100%, 22,5 s** |
| NH5 (3 × 37) | 100%, 25,9 s | **100%, 16,6 s** |

Tentativas numa cópia dos dados (nenhuma foi aplicada):

| Ajuste | NH3: rotação × spam | NH5: rotação × spam |
|---|---|---|
| Custo 90 (NH3) / 100 (NH5) | 28,8 s × 25,6 s (87%) | 26,6 s × 23,1 s |
| Por explosão 28 (NH3) / 26 (NH5) | 28,6 s × 26,9 s | 27,3 s × 20,5 s |
| Por explosão 25 (NH3) / 22 (NH5) — NH5 = 1,5× o NH1 | 28,8 s × 27,5 s | 28,0 s × 23,9 s |

Nem custo (o máximo de Mana é 100) nem um corte grande no dano fazem o spam ficar mais lento que a rotação. A causa é a comparação, não o número: contra 4 ratos do nível 1, qualquer Explosão mais forte que a do NH1 resolve antes de a Mana acabar; e a rotação gasta Mana em Prisão de Gelo e Barreira, que não ajudam contra ratos. O Campo tem o mesmo motivo (recarga, não Mana, limita o spam).

Opções para o designer:

1. **Aceitar no protótipo e medir no nível certo** (recomendado): a regra vale para o "perfil esperado" (§8), isto é, NH5 contra monstros do nível 9. Fica como teste ignorado (`RankDesignTests.Mage_rank_5_area_skill_does_not_solve_the_rats_alone`) até existirem encontros por nível.
2. **Cortar os acertos extras** da Explosão (ex.: 70% do NH1 por explosão em vez de 85%): ainda não basta pelos números acima, e muda a regra geral dos NH.
3. **Mudar a regra**: exigir só "o spam vence no máximo 50% **no NH1**" (como hoje) e, nos NH altos, que a rotação não seja mais lenta que **1,5×** o spam.

## 5. Testes

- `tests/Game.Design.Tests/RankDesignTests.cs`: perfis `focado` e `espalhado` vencem ≥ 70% nos 4 encontros solo; na Matriarca nenhum perfil vence menos que o NH1; regra da habilidade NH5 sozinha para Guerreiro e Arqueiro; a do Mago está ignorada (seção 4).
- `tests/Game.Sim.Tests/RankProfileTests.cs`: perfis, `--rank`, NH aplicados aos jogadores do encontro e erros da linha de comando.
- Os 23 testes de design do NH1 continuam iguais e passando.
