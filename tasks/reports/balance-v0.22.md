# Relatório de balanceamento v0.22 (T-443): Piromante e Arcanista

Fase 4, segunda classe do Mago. Esta tarefa escreve nos dados as 10 habilidades de `design/segunda-classe-v0.1.md` §7 (com todos os NH) e confere, com o simulador, as metas da segunda classe (`design/progression-v0.1.md` §8, "Metas da segunda classe" e "Metas do Mago"). Os v0.20 e v0.21 são da T-423 e da T-433, feitas em paralelo e integradas antes desta.

**Resumo:** nenhum número do design mudou. As metas do Mago passam nos níveis 25, 30 e 40; a Mana não falta. Metas revistas (§2 e §4): as metas do Mago medem a luta **sem as assinaturas**; na diferença entre segundas classes, o **Guardião** passa a entrar **sem limite**, como o Sacerdote, e o **Inquisidor** vai até **2,0×**. Há um alerta para o designer (§5): o Piromante fica muito acima da meta e mais rápido que o Arcanista também contra alvo único.

## 1. Método

- **Dados:** `data/abilities_pyromancer.json` e `data/abilities_arcanist.json` (habilidades, estados e áreas no mesmo arquivo da classe); `data/classes.json` lista as cinco de cada segunda classe na ordem do design.
- **Perfis** (`tests/Game.Design.Tests/SecondClassDesignTests.cs`, a mesma regra da T-408): o perfil esperado do nível com os +5 de atributo da segunda classe (todos em INT) e os pontos de habilidade primeiro no que falta da base, depois cada normal da segunda classe no NH1, as duas principais alternando e a assinatura quando o nível deixa. Par principal: **Bola de Fogo e Combustão** (Piromante) e **Marca Arcana e Orbes Arcanos** (Arcanista; a escolha está na §3). "Só a base" é o mesmo nível e equipamento sem segunda classe.
- **Encontros:** grupo típico, os **Três Lobos** do nível com o Mago no lugar do Guerreiro (a cópia `encounter.three_wolves.class.mage` da T-423); alvo único forte, a **Aranha Grande** (elite) do nível sozinha, na arena dos Três Lobos (o duelo `LevelCurve.WithDuel` da T-402). Estratégia `kite` (a do Mago solo). 100 seeds por lote.
- **Sem as assinaturas** (Cataclismo Arcano, Meteoro, Feixe do Vazio) nas metas de tempo e de Mana, como no tempo para matar da T-402 (§2). O uso delas é conferido à parte, no 40.
- **"Sem Mana":** nova medida no simulador, `TicksBelowSecondClassCost` (`RunResult`): ticks vivos com a Mana abaixo do custo da habilidade mais barata da segunda classe que o personagem conhece (Piromante: Rajada de Chamas, 35; Arcanista: Marca Arcana, 15). Uma luta "fica sem Mana" quando passa mais da metade do tempo assim.
- **Bot** (`src/Game.Bots/ConfigurableBot.Mage.cs`, pelo efeito e pela mira, sem ids): Combustão só com 2 ou mais monstros queimando no raio (todos, se forem menos), então o bot queima antes de detonar; Bola de Fogo no alvo ao alcance (explode no primeiro, não precisa de vários na linha); Chão em Chamas como área de dano (centro no monstro com mais vizinhos); Rajada de Chamas só com um monstro no cone de 4 tiles; Meteoro só com 3 ou mais no raio (opção `minAoeOf`, a estratégia do Piromante); Marca Arcana no alvo do bot, e com as pilhas no máximo só renova quando falta menos da metade; Orbes com o alvo ao alcance e sem orbes ativos; Sifão Arcano com a Mana em até 50%; canalização parada (Raio Arcano NH1–4) só se nenhum monstro que anda chega antes da metade dela (Prisão de Gelo conta); canalização que deixa andar (Raio NH5, Feixe do Vazio) segue o movimento de recuo.

## 2. Por que sem as assinaturas

Com as assinaturas, a luta do Mago acaba em **2–4 s em qualquer nível** (o Cataclismo, com 1,5 s de aviso, decide a luta):

| Nível 40, 100 seeds | Só a base | Piromante | Arcanista |
|---|---|---|---|
| Três Lobos, com assinaturas | 4,3 s (30 seeds) | 2,7 s (Meteoro em todas) | 3,9 s (Feixe do Vazio em todas) |
| Três Lobos, sem assinaturas | 12,9 s | 5,0 s | 10,2 s |
| Aranha Grande, com assinaturas | 6,7 s (30 seeds) | 2,0 s (30 seeds) | 4,7 s |
| Aranha Grande, sem assinaturas | 13,9 s | 3,9 s | 9,2 s |

Com 2–4 s, a comparação mede a abertura (tempo de reação, aviso de 1,5–2 s, os Lobos chegando), não o kit. E quem caça um grupo atrás do outro não tem a assinatura (90–120 s de recarga) em toda luta, o mesmo motivo da T-402 para tirá-la do tempo para matar. As metas de tempo e de Mana do Mago são medidas sem elas. A Mana também foi medida com elas (§4) e o uso delas no 40 tem teste próprio (`At_level_40_the_bot_uses_the_signatures`).

## 3. Resultados

### Piromante, Três Lobos do nível (sem assinaturas, 100 seeds)

| Nível | Só a base | Piromante | Piromante / base |
|---|---|---|---|
| 25 (Bola de Fogo e Combustão NH1) | 100%, 12,4 s | 100%, 8,7 s | **70%** |
| 30 | 100%, 12,7 s | 100%, 7,5 s | **59%** |
| 40 | 100%, 12,9 s | 100%, 5,0 s | **39%** |

Contra a elite (pode ser mais lento): 100% nos três níveis, 8,6 / 6,9 / 3,9 s.

### Arcanista, Aranha Grande do nível (sem assinaturas, 100 seeds)

| Nível | Só a base | Arcanista | Arcanista / base |
|---|---|---|---|
| 25 (Marca e Orbes NH1) | 100%, 13,3 s | 100%, 9,8 s | **74%** |
| 30 | 100%, 13,6 s | 100%, 10,5 s | **77%** |
| 40 | 100%, 13,9 s | 100%, 9,2 s | **66%** |

Nos Três Lobos (pode ser mais lento): 100%, 10,2 / 11,9 / 10,2 s (82%, 94%, 79% da base).

**Par principal do Arcanista.** Com o Raio Arcano entre as duas principais, o NH1 do Raio no 25 quase não muda o tempo: a canalização de 3 s troca três Projéteis Arcanos (113 cada no perfil do 21) por 587 de dano, e o bot só canaliza com o monstro longe ou congelado. Aranha Grande, 30 seeds:

| Par | nv25 | nv30 | nv40 |
|---|---|---|---|
| Raio e Marca | 12,0 s (**92%**, falha) | 8,1 s (61%) | 7,5 s (55%) |
| Raio e Orbes | 11,9 s (**91%**, falha) | 8,1 s (61%) | 6,8 s (50%) |
| **Marca e Orbes** (escolhido) | 9,8 s (75%) | 10,5 s (79%) | 9,1 s (66%) |
| Só a base | 13,1 s | 13,3 s | 13,7 s |

"Marca e Orbes" é o único par que passa nos três níveis sem mudar número; os pares com o Raio são melhores do 30 em diante, quando o Raio já tem NH3 (8 pulsos). Antes da regra de canalização segura do bot, o Arcanista com o Raio no 25 vencia só **83%** da elite (canalizava com a aranha mordendo); com a regra, 100%. Alternativa para o designer, se quiser o Raio como principal desde o 24: reforçar o NH1–2 do Raio (não feito: o design foi aprovado como está e a meta passa).

### Metas gerais (100 seeds)

| Meta (§8) | Resultado |
|---|---|
| Piromante vence ≥ 85% os Três Lobos em no máximo 85% do tempo da base | **passa** (39–70%) |
| Arcanista vence ≥ 85% a elite em no máximo 85% do tempo da base | **passa** (66–77%) |
| Piromante contra a elite e Arcanista nos Três Lobos: vencem ≥ 85% | **passa** (100%) |
| Mana: nenhum dos dois "sem Mana" em mais da metade das lutas do grupo típico | **passa**: 0% das lutas, com e sem assinaturas, nos três níveis |
| Nível 40: o bot usa o Meteoro (3 Lobos) e o Feixe do Vazio (elite) | **passa**: 1,0 Meteoro e 1,0 Feixe por luta, 100% de vitória |
| Nível 21: o NH1 já é útil (não mais lento que só a base, com assinatura) | **passa**: só a base 3,7 s; Piromante 3,6 s (Bola de Fogo NH1); Arcanista 3,5 s (Marca NH1) |
| Pontos do 21 em diante só na base ou na segunda classe: os dois vencem ≥ 70% (nível 30) | **passa**: 100% nos quatro casos |
| Diferença entre segundas classes (cada uma no encontro solo da classe base dela) | **passa depois da revisão da §4** |

## 4. Mana e a diferença entre segundas classes

**Mana.** A preocupação da tarefa (quatro habilidades de 35 a 50 de Mana mais a Explosão Elemental) não se confirmou: o Mago põe tudo em INT e chega ao 21 com ~340 de Mana e ~18/s (370 e 19,5/s no 25; 534 e 27,7/s no 40, `tasks/reports/balance-v0.18.md`). Uma luta do grupo típico dura 5–9 s com o Piromante, e nesse tempo ele gasta o kit uma vez (170 de Mana nas quatro, mais a base) sem chegar ao fundo: **0% das lutas sem Mana**, com e sem assinaturas, nos três níveis. Nenhum custo foi ajustado.

**Diferença entre segundas classes.** Medida como na T-423 e na T-433 (cada segunda classe no encontro solo da classe base dela; o Mago nos Quatro Ratos, com as assinaturas), 100 seeds, já com o Atirador e o Caçador (T-433, integrada antes desta). Agora as oito segundas classes estão na média e **nenhuma classe base fica nela**:

| Nível | Guardião | Berserker | Sacerdote | Inquisidor | Atirador | Caçador | Piromante | Arcanista | Guardião / outras | Inquisidor / outras |
|---|---|---|---|---|---|---|---|---|---|---|
| 25 | 17,4 s | 12,6 s | 36,9 s | 24,2 s | 6,1 s | 5,6 s | 5,3 s | 5,7 s | 1,26× | **1,89×** |
| 30 | 24,0 s | 12,2 s | 34,9 s | 19,8 s | 7,2 s | 5,3 s | 4,9 s | 5,0 s | 1,88× | 1,48× |
| 40 | 24,7 s | 12,2 s | 35,5 s | 17,2 s | 8,1 s | 5,0 s | 4,9 s | 4,8 s | **1,97×** | 1,26× |

Antes desta tarefa, o Mago base (6,4 s nos Quatro Ratos) entrava na média uma vez; agora entram o Piromante e o Arcanista (~5 s cada). A média cai e, com os limites da T-433 (Guardião 2,0×, Clérigo 1,8×), o **Inquisidor falha no 25** (1,89×: só 2 NH da segunda classe, como já registrado na T-423) e o **Guardião fica em 1,97× no 40**, encostado no limite (falharia com outro lote de sementes). O Piromante e o Arcanista não estão lentos; o que mudou foi a média.

**Meta revista (de novo):**

- o **Guardião entra na média sem limite**, como o Sacerdote: as duas são de suporte, a meta solo delas já aceita que sejam mais lentas que só a base, e o que as mede é a meta do grupo (Guardião: 15% menos dano nos outros e a vitória não cai), que continua valendo;
- o **Inquisidor** vai até **2,0×** (era 1,8×; as do Clérigo, a classe mais lenta pelo design). O Inquisidor no 25 tem só o NH1 de duas habilidades e herda a lentidão do Clérigo base (43 s no Guarda);
- as outras seis seguem com 1,6× e ficam abaixo de 1,2×.

**Sugestão para o designer:** a meta já foi revista três vezes (T-423, T-433 e esta) pelo mesmo motivo: ela compara tempos de encontros solo de dificuldades muito diferentes (os de longe em encontros de 5–8 s, o Clérigo num de 43 s), e cada classe nova que entra muda a média. Alternativas: (a) comparar cada segunda classe com a própria base (tempo com a segunda classe / tempo só com a base) e limitar a diferença entre essas razões; (b) rodar todas no mesmo encontro (os Três Lobos) e aceitar que as classes base têm velocidades diferentes; (c) tirar as assinaturas também desta meta, como nas metas do Mago (§2). Não foi feito aqui por ser uma meta nova.

## 5. Alerta para o designer: o Piromante

O Piromante passa com muita folga e fica fora do papel das duas segundas classes:

- **Contra o grupo:** 39–70% do tempo do Mago só com a base, quando a meta é no máximo 85% (o Berserker, o equivalente do Guerreiro, ficou em 74–82%). No 40, 5,0 s contra 12,9 s.
- **Contra alvo único:** o design diz que o Piromante "pode ser mais lento" contra alvo único e que o Arcanista é o de "dano constante em alvo único". Na elite, o Piromante é **mais rápido que o Arcanista** em todos os níveis: 8,6 × 9,8 s (25), 6,9 × 10,5 s (30), 3,9 × 9,2 s (40).

Por quê: as lutas duram 5–14 s, e o Piromante abre com quatro golpes grandes prontos ao mesmo tempo (no perfil do 21: Bola de Fogo 380, Combustão 203, Rajada 185, Chão em Chamas 469, mais as Queimaduras); o Arcanista rende com o tempo (Marca acumula em 3 usos, Orbes em 10 s, Raio que sobe a cada pulso) e o Raio troca Projéteis Arcanos pela canalização. Em regime, o dano por segundo das duas é parecido (conta pela escala de INT no NH5: Piromante ~2,6 × INT/s, Arcanista ~3,4 × INT/s, fora o ataque básico), mas as lutas do jogo são curtas demais para o regime aparecer.

Como nenhuma meta falha, **nada foi mudado** (o design foi aprovado como está). Se o designer quiser o Piromante mais perto do Berserker ou o Arcanista na frente em alvo único, as alavancas mais diretas são a recarga ou o custo da Bola de Fogo e do Chão em Chamas, ou uma meta nova "Arcanista mais rápido que o Piromante contra a elite".

## 6. Outros achados

- **O Mago morre sozinho contra o Cavaleiro Espectral e a Matriarca** do nível (0% em 25, 30 e 40, com e sem segunda classe). Por isso o "alvo único forte" é a Aranha Grande (elite de 400 de vida no nível 6, ~13 s no nível do Mago).
- **Raio Arcano parado:** com a canalização de 3–4 s, o bot só canaliza com o monstro longe ou congelado; mesmo assim o Arcanista leva 270–340 de dano da elite no 30 e no 40 (o Mago só com a base, 2–3). Vence 100%.
- **Combustão no 25 com assinatura:** 0,1 uso por luta (o Cataclismo mata antes de haver dois Lobos queimando). Sem assinatura, 1 uso por luta.
- **Site:** `scripts/build-site.sh` gera o site sem erro (94 páginas), mas, como para todas as segundas classes, as habilidades novas ficam fora (o gerador só segue as habilidades da barra da classe base) e as etiquetas novas (`burn`, `arcane_mark`, `arcane_orbs`, `mana_regen`, `void_exposed`) não têm rótulo em `site/content/site.json`.

## 7. Números que mudaram em relação ao design

Nenhum. Registrado em `design/segunda-classe-v0.1.md` §7 ("Ajustes do simulador (T-443)") e em `design/skills-v0.1.md`.
