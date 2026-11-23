# Relatório de balanceamento v0.21 (T-433): Atirador de Elite e Caçador

Fase 4, segunda classe do Arqueiro. Esta tarefa escreve nos dados as 10 habilidades de `design/segunda-classe-v0.1.md` §6 (com todos os NH; Tiro Certeiro sem conjuração e Olho de Falcão de 15/17/20 s, decisões do designer de 2026-11-22) e confere, com o simulador, as metas da segunda classe (`design/progression-v0.1.md` §8, "Metas da segunda classe" e "Metas do Arqueiro"). Feita no Windows.

## 1. Método

- **Dados:** `data/abilities_marksman.json` e `data/abilities_hunter.json` (habilidades, estados e as áreas da Nuvem Tóxica no mesmo arquivo da classe); `data/classes.json` lista as cinco de cada segunda classe na ordem do design.
- **Perfis:** os da T-408/T-423 (`SecondClassDesignTests.Specs`, genérico por classe base): pontos primeiro no que falta da base, depois cada normal da segunda classe no NH1, o par principal alternando, a assinatura quando o nível deixa. Par principal: **Tiro Certeiro e Mira Firme** (Atirador) e **Flecha Envenenada e Ferida Aberta** (Caçador). No 25 isso dá 2 NH da segunda classe; no 30, 7; no 40, 17 (com a assinatura no NH3).
- **Encontros** (`tests/Game.Design.Tests/ArcherSecondClassDesignTests.cs`), estratégia `kite` (a do Arqueiro solo), 100 seeds por lote:
  - grupo típico: os **Três Lobos** do nível com o Arqueiro no lugar do Guerreiro (`SoloOf`, T-423);
  - alvo único forte: a **Aranha Grande** (`monster.spider_big`, elite) do nível, sozinha, na arena dos Três Lobos. É a única elite que se enfrenta sozinho: o Cavaleiro Espectral (elite de grupo de 3) mata o Arqueiro solo em todos os lotes (0% no 30 e no 40), e o Ent Corrompido é normal;
  - Olho de Falcão: um alvo que não morre (a Aranha Grande com 1 milhão de vida) a 9 tiles, luta de 20 s; dano causado com o Olho de Falcão e sem ele (`skip`);
  - diferença entre segundas classes: as duas no encontro solo do Arqueiro (**Lobo e Salteador**), como manda a meta revista na T-423.
- **Bot** (`src/Game.Bots/ConfigurableBot.cs`, duas opções novas, desligadas por padrão):
  - `groupOnly`: habilidades usadas só com 2 ou mais inimigos (Nuvem Tóxica e Flecha Dispersa do Caçador, requisito 3);
  - `selfBuffRange`: até que distância do inimigo mais próximo um reforço próprio sem dano é usado (padrão 6, o de antes). O Olho de Falcão é um reforço próprio; com 6, o Atirador, que fica longe, quase nunca o usava. Nome próprio para não misturar com o `buffRange` dos reforços de grupo do Clérigo (T-423).
- **Estratégia correta:**
  - Atirador: `order=marksman.hawk_eye,marksman.true_shot` (abre com o Olho de Falcão e o Tiro Certeiro logo depois), `selfBuffRange=12`, `kiteDistance=8` (para de se aproximar a ~9,5 tiles, para a Mira Firme valer) e `skip=marksman.crippling_shot` (seção 3);
  - Caçador: `groupOnly=hunter.toxic_cloud,hunter.scatter_arrow`, o resto na ordem da barra (a base primeiro).

## 2. Resultados

O Arqueiro tem **90 de Mana** do nível 1 ao 40 (não põe INT) e 5,5/s de regeneração. Toda habilidade cabe (a mais cara é a Praga, 80).

### Atirador de Elite (números finais: Tiro Certeiro a 20 de Mana)

| Nível | Elite: só a base | Elite: Atirador | Atirador / base | Três Lobos: base → Atirador |
|---|---|---|---|---|
| 21 (NH1 do Tiro Certeiro) | — | — | — | 18,3 s → 18,2 s |
| 25 | 100%, 13,5 s | 100%, 10,4 s | **77%** | 15,7 → 17,7 s (113%) |
| 30 | 100%, 12,9 s | 100%, 10,1 s | **78%** | 15,5 → 17,2 s (111%) |
| 40 (com o Olho de Falcão) | 100%, 12,2 s | 100%, 8,9 s | **73%** | 15,3 → 16,9 s (110%) |

Olho de Falcão contra o alvo que não morre, 20 s: nível 37 (NH2), **13 005 com × 13 223 sem (0,98×)**; nível 40 (NH3), **14 165 × 14 279 (0,99×)**. Arqueiro só com a base no 40: 11 732.

### Caçador

| Nível | Três Lobos: só a base | Três Lobos: Caçador | Caçador / base | Elite: Caçador (base) |
|---|---|---|---|---|
| 21 (NH1 da Flecha Envenenada) | 100%, 18,3 s | 100%, 17,1 s | 93% | — |
| 25 | 100%, 15,7 s | 100%, 14,2 s | **90%** | 12,0 s (13,5 s), 89% |
| 30 | 100%, 15,5 s | 100%, 12,2 s | **79%** | 10,7 s (12,9 s), 83% |
| 40 | 100%, 15,3 s | 100%, 9,7 s | **63%** | 9,5 s (12,2 s), 78% |

### Metas

| Meta (§8) | Resultado |
|---|---|
| Atirador × base, elite, vence ≥ 85% em no máximo 85% do tempo | **passa**: 73–78%, vitória 100% |
| Atirador no grupo típico: pode ser mais lento, vence ≥ 85% | **passa**: 110–113% do tempo, 100% |
| Caçador × base, Três Lobos, no máximo 85% do tempo | **passa no 30 e no 40** (79%, 63%); **no 25, 90%**: meta revista (seção 3) |
| Caçador contra a elite: pode ser mais lento, vence ≥ 85% | **passa**: 100% (e mais rápido que a base: 78–89%) |
| Olho de Falcão: 20 s com ele ≤ 2,5× sem ele | **passa** com folga (0,98–0,99×); meta revista para 1,5× (seção 5) |
| Nível 21: NH1 da segunda classe não deixa mais lento (margem de 2%) | **passa**: Atirador 18,2 × 18,3 s; Caçador 17,1 × 18,3 s |
| Pontos do 21 em diante só na base ou na segunda classe, nível 30: os dois vencem ≥ 70% | **passa**: 100% nos quatro casos (Atirador 14,8 / 18,5 s; Caçador 15,2 / 13,9 s) |
| Diferença entre segundas classes ≤ 1,6× | **o Guardião passa de 1,6×** no 30 e no 40 com o Arqueiro na média: meta revista (seção 4) |

## 3. Atirador: a Mana do Arqueiro e o Tiro Certeiro

Como no Guerreiro (balance-v0.19 §3), a base do Arqueiro já gasta toda a Mana de uma luta: uns 90 + 5,5/s × 10–15 s ≈ 150–170. A Mana que vai para uma habilidade nova sai do Disparo Potente, da Chuva de Flechas e da Flecha Devastadora. Dano por Mana no 40 (perfil esperado, sem crítico nem defesa): Disparo Potente NH5 697 / 25 = 28; Chuva de Flechas NH5 1181 por alvo / 45 = 26; Tiro Certeiro NH5 995 / 35 = 28 (e ignora metade da armadura); Flecha Perfurante NH2 460 / 30 = 15 (mais nos outros da linha); Tiro Debilitante NH2 197 / 20 = **10**.

Tempo do Atirador / tempo da base contra a elite (lotes de 60 seeds, com o kite de 8/6 testado antes; o resultado final está na seção 2):

| Variante | nv25 | nv30 | nv40 |
|---|---|---|---|
| Números do design, rotação da barra (com o Tiro Debilitante) | 79% | 95% | 98% |
| Números do design, sem o Tiro Debilitante | — | 84% | 84% |
| Tiro Certeiro 20, sem o Tiro Debilitante | 80% | 78% | 72% |

- **Tiro Debilitante fora da rotação contra a elite:** rende ~10 de dano por Mana, e a lentidão não faz falta contra um monstro corpo a corpo que o Arqueiro já deixa para trás. É a estratégia correta, como a Investida do Berserker contra a matilha. Custo mantido (20).
- **Tiro Certeiro 35 → 20:** sem ele, o Atirador fica em 84% do tempo da base (na borda da meta) e, no 40, **o Tiro Certeiro não sai junto com o Olho de Falcão**: 70 + 35 = 105 > 90 de Mana, e a Mana que volta vai para a Marca do Caçador e o Disparo Potente antes de juntar 35. Com 20, os dois cabem nos 90 e saem juntos (requisito 3). O dano não mudou: a pergunta do design ("o dano feito para um tiro de 1,5 s parado precisa cair?") tem resposta **não**; a rajada fica longe de qualquer meta (seção 5).
- **Recuar a 6 tiles quebra o bot:** a primeira estratégia mantinha 6+ tiles também recuando a 6 (`retreatDistance=6`). No Lobo e Salteador do 40, com o Olho de Falcão, o Atirador passou a fugir do Lobo sem parar: 19,4 s e 89% de vitória (p90 46 s), contra 8,1 s com o recuo padrão (3,85). Ficou só `kiteDistance=8`.

**Caçador no 25.** Com 2 NH da segunda classe (Flecha Envenenada NH1 e Ferida Aberta NH1; a Mana não sobra para a Nuvem) o Caçador fica em 90–94% do tempo da base. Para chegar a 85% no 25 seria preciso reforçar o NH1, e o Caçador do 40 já está em 63%. O Berserker passava no 25 porque a Fúria não tem custo. **Meta revista:** no 25 a segunda classe do Arqueiro não pode ser mais lenta que a base; os 85% valem do 30 em diante (o Atirador também passaria nos 85% no 25, com 77%).

**Par do Caçador:** com a Nuvem Tóxica no par (em vez da Ferida Aberta), 94% / 80% / 67%. A Ferida Aberta rende mais contra quem corre atrás do Arqueiro (+50%, +100% no NH5, se o alvo anda).

**Nuvem Tóxica e Flecha Dispersa:** o bot as usa só com 2+ monstros, como pede a tarefa, mas quase nunca sobra Mana (a base vem antes na barra). Pôr a Nuvem na frente deixa o Caçador **mais lento** (17,5 s no 25, 18,9 s no 30): os Lobos correm atrás do Arqueiro e saem da nuvem em 1–2 s. A nuvem pede monstros parados ou que venham em direção a ela; um bot que mire entre o Arqueiro e o grupo fica como pendência.

## 4. Meta revista: diferença entre segundas classes

A meta da T-423 compara cada segunda classe no encontro solo da classe base dela, e as bases sem segunda classe entram na média. Com o Arqueiro, o Arqueiro base (6,7 s no Lobo e Salteador) deu lugar a duas segundas classes no mesmo encontro, e a média caiu:

| Nível | Guardião | Berserker | Sacerdote | Inquisidor | Atirador | Caçador | Mago (base) | Guardião / média das outras |
|---|---|---|---|---|---|---|---|---|
| 25 | 17,4 s | 12,6 s | 36,9 s | 24,2 s | 6,1 s | 5,6 s | 6,3 s | 1,14× |
| 30 | 24,0 s | 12,2 s | 34,9 s | 19,8 s | 7,2 s | 5,3 s | 6,4 s | **1,68×** |
| 40 | 24,7 s | 12,2 s | 35,5 s | 17,2 s | 8,1 s | 5,0 s | 6,4 s | **1,76×** |

O Guardião estava em 1,58× no 40 (balance-v0.20 §3) e passa do limite só porque a média ganhou mais um encontro fácil. O Inquisidor fica em 1,71× no 25 (limite 1,8×); o resto, abaixo de 1,2×.

**Meta revista:** o **Guardião** (tanque) passa a ter limite de **2,0×**, como o Sacerdote (cura) entrou sem limite na T-423: as metas dele são as do grupo (os outros membros recebem 15% menos dano) e a vitória solo, e o design já aceita que ele seja mais lento que o Guerreiro só com a base. Foi considerado pôr as segundas classes do Arqueiro nos Três Lobos (o grupo típico delas): a média subiria, mas cada classe base deixaria de rodar no mesmo encontro, o que a T-423 escolheu. Com o Mago (T-443), as bases de longe saem da média e a meta deve ser vista de novo.

## 5. Olho de Falcão: a meta de 2,5× e o custo de 70

- **A meta de 2,5× nunca é alcançada.** O Olho de Falcão só troca crítico sorteado por crítico garantido. No 40 o perfil esperado tem 30,7% de crítico; a Mira Firme NH5 soma 12% contra alvos a 6+ tiles (40%, o teto) e faz o crítico valer × 1,65. Sem o Olho de Falcão, cada golpe vale em média 1 + 0,4 × 0,65 = 1,26; com ele, 1,65: **+31%** nos golpes de habilidade (venenos e áreas não critam). Mesmo com 0% de crítico, o teto seria 1,65×. **Meta revista: no máximo 1,5×**, que ainda pega um erro (por exemplo, crítico sobre crítico).
- **O Olho de Falcão quase não rende com 70 de Mana:** 0,98–0,99× do dano em 20 s, e 8,9 s × 9,1 s para matar a elite do 40. Os 70 de Mana são 3,5 Tiros Certeiros (a 20) que deixam de sair, e eles valem os +31%. Medido com outros custos (Tiro Certeiro a 20, kite de 8/6): **40 de Mana**, elite do 40 em 6,9 s e 1,02× nos 20 s; **30 de Mana**, 6,2 s e 1,18×. A resposta não é linear (a Mana de uma luta é pouca, e 10 a mais ou a menos decidem se o Disparo Potente sai a tempo).
- **Não mudei o custo** (nenhuma meta falha e a tarefa pede não mexer no que o designer pediu sem uma meta que falhe), mas a assinatura do Atirador, como está, é quase neutra no simulador. **Decisão para o designer:** manter 70 (o Olho de Falcão vale pelo alcance e pela previsibilidade do crítico, não pelo dano), baixar para 40 ou 30, ou dar a ele algo além do crítico (por exemplo, +dano crítico).

## 6. Outros achados

- **O Caçador também é rápido contra alvo único** (78–89% do tempo da base contra a elite, contra 73–78% do Atirador): a Flecha Envenenada e a Ferida Aberta não dependem de haver vários monstros. A especialidade do Atirador (alvo único) fica com pouca margem sobre o Caçador. Não há meta que peça diferença; fica para o designer.
- **Atirador no grupo típico:** 110–113% do tempo da base (o design aceita). Ele gasta a Mana em Tiro Certeiro e Flecha Perfurante (alvo único, e a linha raramente pega dois Lobos) em vez da Chuva de Flechas.
- **O Atirador não usa a Flecha Devastadora** quando tem o Tiro Certeiro e o Olho de Falcão na frente: a Mana não chega a 70 de novo na luta.
- **Site:** `scripts/build-site.sh` gera o site sem erro (94 páginas), mas o gerador não tem páginas de habilidades de segunda classe (nenhuma, nem as do Guerreiro e do Clérigo) e avisa da etiqueta nova `weaken` sem rótulo. Fica para a tarefa do site.

## 7. Números que mudaram em relação ao design

| Habilidade | Antes | Depois |
|---|---|---|
| Tiro Certeiro (Atirador) | custo 35 | **20** |

Metas revistas: Atirador e Caçador no 25 (no máximo o tempo da base; 85% do 30 em diante), Olho de Falcão (1,5×, era 2,5×), Guardião na diferença entre segundas classes (2,0×, era 1,6×). Registrado em `design/segunda-classe-v0.1.md` §6 ("Ajustes do simulador (T-433)"), `design/progression-v0.1.md` §8 e `design/skills-v0.1.md`.
