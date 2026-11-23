# Nível 40 e segunda classe — esboço v0.1

Status: **esboço em discussão com o designer**, entrada para a T-390. Pedido do designer em 2026-11-19, ao fechar o playtest v0.3: **mais níveis** e uma **segunda classe**, com **duas opções por classe**. Decisões fechadas em 2026-11-20 (seção 1).

Os números são ponto de partida para o simulador, como em `design/progression-v0.1.md`. O que ainda falta decidir está na seção 10.

## 1. Decisões do designer

| Assunto | Decisão |
|---|---|
| Nível máximo | **40** (foram considerados 30, 35 e 90) |
| Segunda classe | no **nível 20**, com **4 habilidades normais e 1 assinatura**, o mesmo formato da classe base |
| Habilidades por personagem | **8 normais e 2 assinaturas**: 8 × 5 + 2 × 3 = 46 NH |
| Pontos de habilidade | **40** no nível 40 (1 por nível, como hoje) |
| Pontos de atributo | **200** no nível 40: 5 por nível a partir do 2 (195) e **+5 ao virar segunda classe** (decisão de 2026-11-21) |
| Barra | uma tecla por habilidade: **10 teclas** (1–5 da base, 6–0 da segunda classe), mais ataque básico e poção |
| Passivas | **algumas habilidades viram passivas**, sem que toda classe precise ter: **Sede de Sangue** (Berserker), **Mira Firme** (Atirador de Elite), esta pela distância do alvo, sem ficar parado, e **Flecha Envenenada** (Caçador), que envenena em todo acerto (seção 4.1) |
| Sacerdote | **Bênção** dá atributos e a nova **Graça Veloz** dá velocidade de movimento, as duas para todos perto do Sacerdote (seção 8). A **Inspiração** (regeneração de Mana dos aliados) **substitui o Elo da Vida** (decisão de 2026-11-22) |
| Inquisidor | **aprovado como está** na seção 8 (decisão de 2026-11-22) |
| Atirador de Elite | **aprovado** (2026-11-22), com duas mudanças: o **Tiro Certeiro sem conjuração** e o **Olho de Falcão de 15 a 20 s** (seção 6) |
| Caçador | **aprovado como está** na seção 6 (decisão de 2026-11-22) |
| Piromante | **aprovado como está** na seção 7 (decisão de 2026-11-22) |
| Arcanista | **aprovado** (2026-11-22), com uma mudança: o **Sifão Arcano aumenta a regeneração de Mana** em vez de devolver Mana na hora, para manter a regra "Mana vem só do tempo" (seção 7) |
| Curva de XP | a dificuldade continua crescendo como hoje, com **monstros mais fortes que dão mais XP** (seção 3) |
| Guerreiro | Guardião e Berserker |
| Arqueiro | Atirador de Elite (alvo único) e Caçador |
| Mago | Piromante (dano em área) e Arcanista (dano constante em alvo único) |
| Clérigo | Sacerdote e Inquisidor |
| Trocar de segunda classe | **não pode**: a escolha é definitiva |
| Primeira classe a entregar | **Guerreiro**, com as duas segundas classes dele, para validar o sistema |
| Foco agora | **só as classes** (decisão de 2026-11-21). Áreas e mapas novos, inclusive do 16 ao 20, vêm depois das classes novas |
| Teste sem conteúdo | uma **ferramenta de desenvolvimento para subir de nível**, num **botão no menu do Esc** (decisão de 2026-11-21), para o designer testar as classes sem mapa novo (seção 4.2) |

## 2. Pontos e níveis de habilidade (NH)

### Pontos

- **Habilidade:** 1 por nível, a partir do 1, como hoje: 40 pontos para 46 NH. No 40 dá para ter 87% de tudo, e sobram 6 NH sem preencher. A escolha fica menor que hoje (65%), mas existe.
- **Atributo:** hoje são 5 por nível **a partir do 2** (70 no nível 15). No 40 isso dá **195**. Para chegar a **200**, o personagem ganha **+5 ao virar segunda classe**, no nível 20 (decisão de 2026-11-21). A calibração do nível 1 (§1 do design de progressão) não muda.

### Quando cada NH libera

A classe base não muda. A segunda classe repete o desenho, espalhado do 21 ao 40:

| | NH1 | NH2 | NH3 | NH4 | NH5 |
|---|---|---|---|---|---|
| Normais da base | 1 | 3 | 5 | 7 | 9 |
| Assinatura da base | 12 | 13 | 15 | — | — |
| Normais da segunda classe | 21 | 24 | 27 | 30 | 33 |
| Assinatura da segunda classe | 35 | 37 | 40 | — | — |

No nível 20, o jogador tem 20 pontos para os 23 NH da base: chega à segunda classe com a base quase completa. Do 21 ao 40, ganha mais 20 pontos para os 23 NH da segunda classe e os 3 que sobraram da base.

### Regra dos números da segunda classe

A escada por NH continua igual (× 1,0 / 1,2 / 1,4 / 1,6 / 1,8; assinatura × 1,0 / 1,3 / 1,6). Como o NH1 só chega no nível 21, quando a base já está no NH5, o **NH1 da segunda classe vale cerca de 1,4 vez** o NH1 de uma habilidade parecida da base. Assim, a habilidade nova já é útil no dia em que chega.

## 3. Curva de XP: dá para manter a de hoje?

**Sim.** A curva atual já conta com monstros mais fortes dando mais XP: cada monstro dá `6 × nível` (vezes 5 para elite e 30 para chefe). A XP para o próximo nível (`68 × L^1,8`) cresce um pouco mais rápido que isso, então cada nível pede mais monstros que o anterior:

| Nível | Monstros normais do próprio nível para subir | Horas acumuladas (aprox.) |
|---|---|---|
| 10 | ~72 | ~4 h |
| 15 | ~99 | ~9 h |
| 20 | ~124 | ~14 h |
| 30 | ~172 | ~32 h |
| 39 → 40 | ~212 | **~54 h** |

Base: os ~775 monstros até o 15 levam de 8 a 9 horas. Um nível alto leva de 2 a 3 horas.

**O que decide se "fica bom" é a XP por minuto, não a XP por monstro.** Um monstro que dá o dobro de XP mas leva o dobro do tempo para morrer não acelera nada. Para os monstros mais fortes compensarem, eles precisam dar mais XP **em relação ao tempo** que levam: é o papel dos elites, dos chefes de campo e das masmorras. O simulador mede isso: a meta a definir é a XP por hora de cada área.

**Conteúdo:** com a curva atual, o caminho do 20 ao 40 leva umas 40 horas, cerca de 3 vezes o que o primeiro mapa oferece hoje. Isso pede mais áreas, ou conteúdo que se repete bem (elites, masmorras de grupo).

**O primeiro mapa termina no 15**, com o Vigia, e a segunda classe só entra no 20. Por decisão do designer (2026-11-21), as áreas novas, inclusive do 16 ao 20, ficam para **depois das classes**: haverá muitos mapas novos. Até lá, o teste usa a ferramenta de subir de nível (seção 4.2).

### Outras regras que mudam com o nível 40

- `progression.maxLevel` e níveis de NH separados para a classe base e a segunda classe.
- **Agilidade:** com 200 pontos, a velocidade de ataque bate no máximo com AGI 100, e o crítico no máximo com AGI 140. Rever esses tetos ou o valor por ponto.
- **Monstros:** a escala por nível (`healthPerLevel 0,4`, `powerPerLevel 0,17`) é linear e nunca foi testada acima do 15.
- **Itens e defesa:** rever se a armadura chega ao teto de 60% cedo demais.

**Medido na T-402** (`tasks/reports/balance-v0.18.md`, perfil esperado sem segunda classe):

- **Agilidade:** o Arqueiro esperado batia no teto de velocidade de ataque no nível 39 (AGI 101). O teto subiu de +50% para **+60%** (AGI 120); o crítico (AGI 140) o perfil esperado não alcança (30,7% no 40).
- **Monstros:** a escala linear acompanha o personagem. O tempo para matar um monstro normal do próprio nível fica quase igual do 15 ao 40 (de −28% a +11% por classe), e a XP por minuto cresce com o nível. O dano recebido por luta, em fração da vida, também fica constante.
- **Defesa:** ninguém chega a 60% contra o mesmo nível; a redução converge para 47% (pesada), 37% (média) e 27% (leve).
- **Perfil de referência no 21** (§5) confirmado: Guerreiro FOR 62, VIT 60, ARMA 25, Golpe de Espada 87, Golpe Pesado NH5 351.

## 4. Como a segunda classe funciona

- **Quando:** no nível 20, falando com um mestre da classe na Cidade. **Decisão da fase 4 (T-403, 2026-11-21):** por enquanto a escolha é feita pela tela de habilidades (T-410), em qualquer lugar fora de combate; o mestre da classe na Cidade fica para quando os mapas forem refeitos.
- **Escolha definitiva:** não dá para trocar. A tela de escolha mostra as duas opções com as habilidades e pede confirmação.
- **Pontos:** a partir do 21, os pontos podem ir para qualquer habilidade, da base ou da segunda classe.
- **Barra:** 10 teclas. As teclas 6–0 aparecem vazias até a segunda classe.
- **Passivas** (decisão do designer, 2026-11-20): algumas habilidades podem ser **passivas**, sem que toda classe precise ter uma. A passiva usa pontos e tem NH como as outras, mas não ocupa tecla, não custa Mana e vale sempre. Na tela de habilidades, aparece com uma moldura diferente. Uma classe com passiva usa menos teclas da barra. São passivas a **Sede de Sangue** do Berserker, a **Mira Firme** do Atirador de Elite e a **Flecha Envenenada** do Caçador (seção 4.1).

### 4.1 Candidatas a passiva

Bom candidato é a habilidade que só reforça o próprio personagem, sem mirar e sem momento certo de usar. Quando vira passiva, o valor cai para a média do tempo em que a versão ativa ficava ligada.

| Classe | Habilidade | Passiva? | Como fica |
|---|---|---|---|
| Berserker | **Sede de Sangue** | **sim** (decidida) | 5% a 10% de vida pelo dano, sempre (seção 5) |
| Atirador de Elite | **Mira Firme** | **sim** (decidida) | mais crítico contra alvos a 6+ tiles e +1 de alcance, sem precisar ficar parado (seção 6) |
| Caçador | **Flecha Envenenada** | **sim** (decidida) | todo acerto põe uma pilha de veneno e renova a duração; as pilhas ficam (seção 6) |
| Guardião | Bastião | não | continua ativa |
| Sacerdote | Bênção | não | continua ativa |
| Arcanista | Orbes Arcanos | não | continua ativa (como passiva ficaria forte demais) |

As outras segundas classes (Guardião, Piromante, Arcanista, Sacerdote e Inquisidor) e as classes base não têm passiva.

**No Core:** a passiva é um estado permanente, aplicado quando o jogador põe o primeiro ponto (como o Frenesi dos monstros). A Mira Firme precisa de uma condição nova: bônus de crítico pela distância até o alvo.

### 4.2 Ferramenta de teste: subir de nível

Pedido do designer (2026-11-21): enquanto não há áreas do 16 ao 40, ele precisa de um jeito de subir de nível para testar as classes. Hoje não existe: o único atalho de desenvolvimento é o `--dev-login`, que entra sem conta.

**Decisão: um botão no menu do Esc** (T-396), escolhido pelo designer em 2026-11-21. Não pede mexer no mapa da Cidade; um NPC pediria editar o mapa no Tiled e um tipo novo de NPC.

- **Só em servidor de teste.** O servidor ganha uma opção nova (por exemplo, `--dev-tools`), desligada por padrão. Sem ela, o servidor recusa o pedido e o cliente nem mostra o botão. Antes de abrir o jogo a jogadores de fora, a opção precisa estar desligada.
- **O que o botão faz:** sobe +1 nível, +5 níveis, ou vai direto ao 20 ou ao 40. O cliente só envia a intenção; quem sobe o nível é o servidor, pelo mesmo caminho da XP normal, para os pontos de atributo e de habilidade saírem iguais aos de quem subiu jogando.
- **Desfazer a segunda classe:** como a escolha é definitiva, o teste precisa de um jeito de voltar atrás. A ferramenta também volta o personagem para antes da escolha, devolvendo os pontos gastos nela.
- **Redistribuir pontos:** a redistribuição na Cidade já existe (1ª vez grátis, depois ouro). A ferramenta pode deixá-la grátis no servidor de teste.
- O personagem é gravado normalmente quando o servidor tem banco; no servidor de teste isso não atrapalha ninguém.
- **Onde testar (decisão do designer, 2026-11-22):** os testes da fase 4 são **locais, no Windows**, com o servidor rodando `--world --dev-login --dev-tools`, sem deploy na VPS. Sem banco, o personagem começa no nível 1 e não é gravado; a ferramenta resolve isso subindo de nível na hora. Comando em `tasks/README.md`, seção 6f.

## 5. Guerreiro em detalhe (primeira classe a entregar)

**Perfil de referência no nível 21:** pontos divididos entre FOR e VIT, como o "perfil esperado" do simulador (FOR 62, VIT 60) e espada comum do nível 21 (ARMA 25). Nesse perfil, o Golpe de Espada dá 87 e o Golpe Pesado no NH5 dá 351.

### Guardião — proteger o grupo e segurar chefes

Escala mais com **VIT** que o Guerreiro base: quanto mais vida, mais protege.

#### Interceptação — corre até um aliado e recebe parte do dano dele

Custo 20, recarga 12 s, alcance 8, alvo: um aliado. Avança até o aliado e, pela duração, uma parte do dano que o aliado receberia vai para o Guardião (que se defende com a própria armadura e resistência).

| NH | Dano desviado | Duração | Outros efeitos |
|---|---|---|---|
| 1 | 30% | **6 s** (era 4) | — |
| 2 | 35% | **6 s** (era 4) | — |
| 3 | 35% | **7 s** (era 5) | o aliado ganha **escudo** 20 + 1 × VIT (80), pela mesma duração |
| 4 | 40% | **7 s** (era 5) | escudo 20 + 1 × VIT |
| 5 | 40% | **8 s** (era 6) | escudo 40 + 2 × VIT (160) e recarga **9 s** |

Durações +2 s em todos os NH na T-408 (reforço do Guardião em grupo, decisão do designer de 2026-11-22). O vínculo vale com o Guardião a até 8 tiles do aliado.

#### Golpe de Escudo — atordoa e prende a atenção

Custo 25, recarga 8 s, arco de 60° e alcance 1,9. Gera **3 vezes** a ameaça do dano.

| NH | Valor | Outros efeitos | Perfil nível 21 |
|---|---|---|---|
| 1 | dano 10 + 0,8 × ARMA + 1,5 × VIT | atordoa 1 s | 120 |
| 2 | dano 12 + 0,96 × ARMA + 1,8 × VIT | atordoa 1 s | 144 |
| 3 | dano 14 + 1,12 × ARMA + 2,1 × VIT | atordoa **1,5 s** | 168 |
| 4 | dano 16 + 1,28 × ARMA + 2,4 × VIT | atordoa 1,5 s | 192 |
| 5 | dano 18 + 1,44 × ARMA + 2,7 × VIT | atordoa 1,5 s e o arco vai a **120°** (vários alvos) | 216 |

Contra elites e chefes, o atordoamento é reduzido pela regra de controle (× 0,5 e × 0,25).

#### Bastião — o grupo perto dele apanha menos

Custo **20** (era 40; T-408), recarga 20 s, raio 5 em volta do Guardião. Todos os aliados no raio (com ele) recebem menos dano pela duração.

| NH | Redução de dano | Duração | Outros efeitos |
|---|---|---|---|
| 1 | **25%** (era 15%) | 6 s | — |
| 2 | **28%** (era 18%) | 6 s | — |
| 3 | **30%** (era 20%) | 8 s | raio **7** |
| 4 | **33%** (era 23%) | 8 s | raio 7 |
| 5 | **35%** (era 25%) | 8 s | raio 7 e os aliados **não são empurrados** |

Redução +10 pontos em todos os NH na T-408 (reforço do Guardião em grupo). Vale para todo dano, inclusive de estados e áreas (veneno, poça), em quem está no raio quando o Guardião usa.

#### Inabalável — não para de pé

Sem custo, recarga 25 s, só no Guardião. Tira atordoamento, enraizamento e lentidão, e fica imune a eles pela duração.

| NH | Imunidade | Redução de dano | Outros efeitos |
|---|---|---|---|
| 1 | 3 s | 10% | — |
| 2 | 4 s | 10% | — |
| 3 | 4 s | 20% | — |
| 4 | 4 s | 20% | recarga **20 s** |
| 5 | 5 s | 20% | também **tira** esses efeitos dos aliados num raio de 4 (sem imunidade) |

#### Juramento (assinatura, nível 35) — toma para si o dano do grupo

Custo 80, recarga 90 s, raio 6. Pela duração, parte do dano que os aliados no raio receberiam vai para o Guardião, que recebe esse dano reduzido.

| NH | Dano desviado | Redução no Guardião | Duração e raio |
|---|---|---|---|
| 1 | 50% | 40% | 8 s, raio 6 |
| 2 | 60% | 45% | 8 s, raio 6 |
| 3 | 70% | 50% | **10 s**, raio **8** |

### Berserker — dano corpo a corpo alto, defesa baixa

Escala mais com **FOR**. Ganha dano se expondo: a Fúria aumenta o dano recebido, e a Sede de Sangue devolve vida pelo dano causado.

#### Investida — avança até o alvo e o atordoa

Custo 20, recarga 10 s, alcance 8, alvo: um monstro.

| NH | Valor | Outros efeitos | Perfil nível 21 |
|---|---|---|---|
| 1 | dano 8 + 0,6 × ARMA + 1,4 × FOR | atordoa 0,5 s | 110 |
| 2 | dano 10 + 0,72 × ARMA + 1,68 × FOR | atordoa 0,5 s | 132 |
| 3 | dano 11 + 0,84 × ARMA + 1,96 × FOR | atordoa **1 s** | 154 |
| 4 | dano 13 + 0,96 × ARMA + 2,24 × FOR | atordoa 1 s | 176 |
| 5 | dano 14 + 1,08 × ARMA + 2,52 × FOR | atordoa 1 s, recarga **7 s** e o próximo golpe em até 3 s causa **+30%** | 198 |

#### Fúria — mais dano, mais exposto

**Sem custo** (era 30; T-408), recarga 20 s, só no Berserker. Pela duração, causa mais dano, anda mais rápido e **recebe 20% mais dano**.

| NH | Dano causado | Velocidade | Duração | Outros efeitos |
|---|---|---|---|---|
| 1 | +20% | +10% | 8 s | — |
| 2 | +24% | +10% | 8 s | — |
| 3 | +28% | +15% | **10 s** | — |
| 4 | +32% | +15% | 10 s | — |
| 5 | +36% | +15% | 10 s | atordoamento e lentidão nele duram **metade** |

#### Execução — golpe que termina o serviço

Custo **20** (era 30; T-408), recarga 8 s, arco de 90° e alcance 1,9. Causa dano extra em alvo com pouca vida.

| NH | Valor | Dano extra | Outros efeitos | Perfil nível 21 |
|---|---|---|---|---|
| 1 | dano 20 + 1,3 × ARMA + 3,2 × FOR | +50% abaixo de 30% de vida | — | 251 |
| 2 | dano 24 + 1,56 × ARMA + 3,84 × FOR | +50% abaixo de 30% | — | 301 |
| 3 | dano 28 + 1,82 × ARMA + 4,48 × FOR | **+75%** abaixo de 30% | — | 351 |
| 4 | dano 32 + 2,08 × ARMA + 5,12 × FOR | +75% abaixo de 30% | — | 401 |
| 5 | dano 36 + 2,34 × ARMA + 5,76 × FOR | +75% abaixo de **35%** | se o golpe **matar**, a recarga zera | 452 |

#### Sede de Sangue (passiva) — vida pelo dano causado

**Passiva** (decisão do designer, 2026-11-20): não ocupa tecla nem custa Mana, vale sempre. Uma parte do dano que o Berserker causa volta como vida.

| NH | Vida pelo dano | Outros efeitos |
|---|---|---|
| 1 | 5% | — |
| 2 | 6% | — |
| 3 | 7% | abaixo de 30% de vida, a parte **dobra** |
| 4 | 8% | dobra abaixo de 30% de vida |
| 5 | 10% | dobra abaixo de 30% de vida e matar um monstro cura **5% da vida máxima** |

Os valores são a média da versão ativa (15% a 25% por 6 a 8 s, a cada 18 s), que ficava ligada um terço do tempo. Contra chefes, a vida devolvida pode precisar de redução, para o Clérigo continuar importante (`design/ideias-futuras.md`, roubo de vida).

#### Tempestade de Aço (assinatura, nível 35) — gira enquanto anda

Custo **80** (era 90; T-408), recarga 75 s. Por 4 s, gira sem parar, acertando todos num raio de 3 a cada 0,5 s (8 giros), andando 20% mais devagar.

| NH | Valor por giro | Outros efeitos | Perfil nível 21, total |
|---|---|---|---|
| 1 | dano 6 + 0,5 × ARMA + 1,2 × FOR | — | 8 × 93 = 744 |
| 2 | dano 8 + 0,65 × ARMA + 1,56 × FOR | — | 8 × 121 = 968 |
| 3 | dano 10 + 0,8 × ARMA + 1,92 × FOR | cada giro aplica **Sangramento** | 8 × 149 = 1 192 |

### Mecânicas do Guerreiro: o que já existe e o que é novo

| Habilidade | Já existe no Core | Novo |
|---|---|---|
| Interceptação | avanço (`displacement`), escudo | **desviar dano de um aliado** e avançar até um aliado |
| Golpe de Escudo | dano, atordoamento, ameaça | ameaça multiplicada pelo dano |
| Bastião | estado em aliados no raio (como a Muralha Viva), redução de dano | imunidade a empurrão |
| Inabalável | tirar estados, imunidade por etiqueta (como a Purificação) | — |
| Juramento | estado em aliados no raio | **desviar dano** (o mesmo da Interceptação) |
| Investida | avanço até o alvo, atordoamento, reforço do próximo golpe (como o Recuo Rápido) | — |
| Fúria | estado com mais dano, velocidade e dano recebido (como o Frenesi dos monstros) | — |
| Execução | dano | **dano extra pela vida do alvo**, zerar a recarga ao matar |
| Sede de Sangue (passiva) | estado permanente (como o Frenesi dos monstros) | **vida pelo dano causado**; **habilidade passiva** (pontos e NH, sem tecla) |
| Tempestade de Aço | vários acertos com intervalo, Sangramento | **área que acompanha quem anda** (a verificar no Core) |

O Berserker usa quase só o que já existe. O Guardião traz o **desvio de dano**. O Elo da Vida do Sacerdote, que também ia usar o desvio, saiu em 2026-11-22 (seção 8).

### Ajustes do simulador (T-408)

Os dados estão em `data/abilities_guardian.json` e `data/abilities_berserker.json`, com os números das tabelas acima. Na primeira rodada mudaram só **custos de Mana**, porque o Guerreiro fica com **88 de Mana** até o 40 (não põe INT) e 5/s de regeneração, e a base já gasta tudo isso (Golpe Pesado e Golpe Circular acertam o grupo inteiro e rendem mais por Mana que qualquer habilidade nova de alvo único). Medidas em `tasks/reports/balance-v0.19.md`.

| Habilidade | Antes | Depois | Por quê |
|---|---|---|---|
| Tempestade de Aço | 90 | **80** | 90 não cabe em 88 de Mana (pendência da T-402); 80, como a Muralha Viva e o Juramento |
| Fúria | 30 | **sem custo** | o preço é se expor (+20% de dano recebido), como o Inabalável é sem custo; com os custos do design, o Berserker levava **118–130%** do tempo do Guerreiro só com a base (a Mana da Fúria e da Execução saía do Golpe Pesado e do Golpe Circular) |
| Execução | 30 | **20** | com a Fúria sem custo e a Execução a 30, o Berserker ficava em 94–103% do tempo da base; com 20, em **74–82%** (meta: no máximo 85%) |
| Bastião | 40 | **20** | com 40, o bot só conseguia usá-lo uma vez por luta de grupo (a Mana ia para as habilidades mais baratas); com 20, fica em ~30% do tempo nos aliados |

**Reforço do Guardião (segunda rodada, decisão do designer de 2026-11-22: manter a meta de 15% e reforçar o Guardião).** Com os números do design, os outros membros do grupo recebiam 12–16% menos dano por segundo vivo, sem margem. Foram testados, um de cada vez e juntos: raio do Bastião 7/9, vínculo da Interceptação de 12 tiles, Juramento a 40 de Mana, Bastião mais longo ou mais frequente, Interceptação mais forte ou mais longa. O menor reforço que passa nos três lotes de sementes:

| Habilidade | Antes | Depois | Medido (outros membros, nv30 / nv40) |
|---|---|---|---|
| Interceptação | duração 4/4/5/5/6 s | **6/6/7/7/8 s** | sozinha: 16–20%, sem folga no 30 |
| Bastião | redução 15/18/20/23/25% | **25/28/30/33/35%** | sozinho: 12–25% |
| As duas juntas | 12–16% | **19–26%** | vitória do grupo igual ou maior |

Raio maior, vínculo mais longo e Juramento mais barato mexem 1–3 pontos, dentro do ruído; ficaram como no design. Detalhes no relatório, §4.

## 6. Arqueiro em detalhe

**Perfil de referência no nível 21:** pontos divididos entre DES e AGI (DES 62, AGI 56) e arco comum do nível 21 (ARMA 25). Nesse perfil, o Disparo dá 82 e o Disparo Potente no NH5 dá 404 (3 flechas).

### Atirador de Elite — alvo único, de longe, rajadas e críticos

#### Flecha Perfurante — tiro forte que atravessa

Custo 30, recarga 8 s, linha de 10 tiles. O primeiro alvo recebe o dano inteiro; quem está atrás, na mesma linha, recebe uma parte.

| NH | Valor | Quem está atrás | Outros efeitos | Perfil nível 21 |
|---|---|---|---|---|
| 1 | dano 14 + 1,4 × ARMA + 2,8 × DES | 60% | — | 223 |
| 2 | dano 16,8 + 1,68 × ARMA + 3,36 × DES | 60% | — | 267 |
| 3 | dano 19,6 + 1,96 × ARMA + 3,92 × DES | **80%** | — | 312 |
| 4 | dano 22,4 + 2,24 × ARMA + 4,48 × DES | 80% | — | 356 |
| 5 | dano 25,2 + 2,52 × ARMA + 5,04 × DES | 80% | linha de **14 tiles** e recarga **6 s** | 401 |

#### Tiro Debilitante — o alvo fica lento e fraco

Custo 20, recarga 12 s, alcance 10.

| NH | Valor | Lentidão | Dano do alvo | Duração | Perfil nível 21 |
|---|---|---|---|---|---|
| 1 | dano 6 + 0,6 × ARMA + 1,2 × DES | 30% | −15% | 4 s | 95 |
| 2 | dano 7,2 + 0,72 × ARMA + 1,44 × DES | 30% | −15% | 4 s | 114 |
| 3 | dano 8,4 + 0,84 × ARMA + 1,68 × DES | **40%** | **−20%** | **5 s** | 134 |
| 4 | dano 9,6 + 0,96 × ARMA + 1,92 × DES | 40% | −20% | 5 s | 153 |
| 5 | dano 10,8 + 1,08 × ARMA + 2,16 × DES | **50%** | **−25%** | **6 s** | 172 |

#### Mira Firme (passiva) — quanto mais longe o alvo, mais crítico

**Passiva** (decisão do designer, 2026-11-20). Não precisa ficar parado: o bônus de crítico vale contra alvos **a 6 tiles ou mais**, o que premia manter distância sem tirar a mobilidade do Arqueiro. O alcance extra vale sempre.

| NH | Crítico contra alvos a 6+ tiles | Alcance | Outros efeitos |
|---|---|---|---|
| 1 | +6% | — | — |
| 2 | +8% | — | — |
| 3 | +10% | **+1** em todos os disparos | — |
| 4 | +12% | +1 | — |
| 5 | +12% | +1 | o crítico contra esses alvos causa **× 1,65** (em vez de × 1,5) |

Os valores são menores que os da versão ativa (+10% a +20% por 6 a 8 s, a cada 15 s), porque agora valem o tempo todo. A versão anterior, que exigia ficar parado, foi descartada: com o Tiro Certeiro pedindo 1,5 s parado (na época), o Atirador passaria tempo demais sem andar.

#### Tiro Certeiro — dano muito alto que ignora armadura

Custo **20** (era 35; T-433), recarga 10 s, alcance 12, **sem conjuração** (decisão do designer, 2026-11-22; antes, 1,5 s parado). Ignora parte da armadura do alvo.

| NH | Valor | Armadura ignorada | Outros efeitos | Perfil nível 21 |
|---|---|---|---|---|
| 1 | dano 25 + 2 × ARMA + 4 × DES | 30% | — | 323 |
| 2 | dano 30 + 2,4 × ARMA + 4,8 × DES | 30% | — | 388 |
| 3 | dano 35 + 2,8 × ARMA + 5,6 × DES | **40%** | — | 452 |
| 4 | dano 40 + 3,2 × ARMA + 6,4 × DES | 40% | — | 517 |
| 5 | dano 45 + 3,6 × ARMA + 7,2 × DES | **50%** | recarga **8 s** | 581 |

Sem a conjuração, o NH5 troca "conjuração de 1 s" por recarga de 8 s. O dano foi feito para um tiro que pedia 1,5 s parado: sem esse preço, o simulador deve dizer se ele precisa cair (tarefa de dados do Arqueiro). **Resposta da T-433:** o dano não precisa cair; o custo caiu de 35 para 20 (ver "Ajustes do simulador (T-433)" no fim desta seção).

#### Olho de Falcão (assinatura, nível 35) — todo ataque é crítico

Custo 70, recarga 90 s, só no Atirador. **Dura perto de 20 s** (decisão do designer, 2026-11-22: 6 a 8 s era pouco para 70 de Mana).

| NH | Duração | Outros efeitos |
|---|---|---|
| 1 | 15 s | todo ataque e habilidade de dano é crítico |
| 2 | 17 s | todos críticos |
| 3 | **20 s** | todos críticos e **+2 de alcance** |

Com 15 a 20 s a cada 90 s, o Atirador fica com todos os golpes críticos de 17% a 22% do tempo. O simulador confere se isso passa das metas (antes, 6 a 8 s).

### Caçador — venenos e sangramento em vários alvos

Usa um veneno próprio, o **Veneno do Caçador**, que **acumula** em pilhas. O dano por pilha, o máximo de pilhas e quanto tempo o veneno dura vêm do NH da Flecha Envenenada.

#### Flecha Envenenada (passiva) — todo golpe envenena

**Passiva** (sugestão do designer, 2026-11-21, que manteve as **pilhas**: o veneno acumula em vez de ser um só). Não ocupa tecla nem custa Mana. **Todo acerto** do Caçador (o Disparo e qualquer habilidade de dano) põe 1 pilha do Veneno do Caçador no alvo e **renova a duração** de todas as pilhas. Quem continua atirando mantém o alvo envenenado; se parar, o veneno acaba depois da duração.

| NH | Veneno por pilha | Pilhas no máximo | Duração depois do último acerto | Perfil nível 21 |
|---|---|---|---|---|
| 1 | 1 + 0,12 × DES por s | 3 | 4 s | 8/s por pilha, 25/s no máximo |
| 2 | 1,2 + 0,144 × DES por s | 3 | 4 s | 10/s, 30/s no máximo |
| 3 | 1,4 + 0,168 × DES por s | **4** | **5 s** | 12/s, 47/s no máximo |
| 4 | 1,6 + 0,192 × DES por s | 4 | 5 s | 14/s, 54/s no máximo |
| 5 | 1,8 + 0,216 × DES por s | **5** | **6 s** | 15/s, 76/s no máximo |

**Por que o dano por pilha caiu:** na versão ativa, a pilha dava 29 a 52 por segundo, mas a recarga de 6 s deixava manter só uma pilha por alvo. Como passiva, o veneno chega ao máximo de pilhas em poucos disparos e pega em **todos** os alvos que o Caçador acerta, inclusive com a Chuva de Flechas. No máximo, o NH1 soma uns 30% ao dano do Disparo (82 a cada 1 s).

#### Ferida Aberta — sangramento forte, pior se o alvo correr

Custo 25, recarga 10 s, alcance 9. Sangramento por segundo; se o alvo se move, sangra mais.

| NH | Sangramento por s | Duração | Se o alvo andar | Perfil nível 21 |
|---|---|---|---|---|
| 1 | 6 + 0,6 × DES | 6 s | +50% | 43/s, 259 no total |
| 2 | 7,2 + 0,72 × DES | 6 s | +50% | 52/s, 311 |
| 3 | 8,4 + 0,84 × DES | **8 s** | +50% | 60/s, 484 |
| 4 | 9,6 + 0,96 × DES | 8 s | +50% | 69/s, 553 |
| 5 | 10,8 + 1,08 × DES | 8 s | **+100%** | 78/s, 622 |

#### Nuvem Tóxica — área que envenena

Custo 45, recarga 14 s, alcance 9. Nuvem no chão; quem fica nela sofre dano a cada segundo e ganha 1 pilha do Veneno do Caçador a cada 2 s.

| NH | Dano por s | Raio | Duração | Outros efeitos | Perfil nível 21 |
|---|---|---|---|---|---|
| 1 | 4 + 0,7 × DES | 3 | 6 s | — | 47/s, 284 no total |
| 2 | 4,8 + 0,84 × DES | 3 | 6 s | — | 57/s, 341 |
| 3 | 5,6 + 0,98 × DES | **4** | 6 s | — | 66/s, 398 |
| 4 | 6,4 + 1,12 × DES | 4 | 6 s | — | 76/s, 455 |
| 5 | 7,2 + 1,26 × DES | 4 | **8 s** | lentidão de 20% na nuvem | 85/s, 683 |

#### Flecha Dispersa — espalha os venenos do alvo

Custo 30, recarga 12 s, alcance 9. Dano no alvo e **copia** o Veneno do Caçador e o sangramento dele para monstros a até 4 tiles.

| NH | Valor | Monstros que recebem a cópia | Perfil nível 21 |
|---|---|---|---|
| 1 | dano 8 + 0,6 × ARMA + 1,2 × DES | 2 | 97 |
| 2 | dano 9,6 + 0,72 × ARMA + 1,44 × DES | 2 | 117 |
| 3 | dano 11,2 + 0,84 × ARMA + 1,68 × DES | **3** | 136 |
| 4 | dano 12,8 + 0,96 × ARMA + 1,92 × DES | 3 | 156 |
| 5 | dano 14,4 + 1,08 × ARMA + 2,16 × DES | **4** | 175 |

#### Praga (assinatura, nível 35) — o veneno pula de monstro em monstro

Custo 80, recarga 90 s, alcance 9, raio 6. Põe pilhas do Veneno do Caçador em todos no raio. Pela duração, quando o veneno acaba num monstro, ele pula com todas as pilhas para o monstro mais próximo (até 4 tiles).

| NH | Pilhas | Duração |
|---|---|---|
| 1 | 3 | 8 s |
| 2 | 4 | 8 s |
| 3 | 5 | **10 s** |

### Mecânicas do Arqueiro

| Habilidade | Já existe no Core | Novo |
|---|---|---|
| Flecha Perfurante | linha (como a Flecha Devastadora NH2) | dano menor em quem está atrás |
| Tiro Debilitante | dano, lentidão, menos dano causado | — |
| Mira Firme (passiva) | estado permanente | **crítico pela distância até o alvo**, alcance como reforço |
| Tiro Certeiro | dano | **ignorar armadura** |
| Olho de Falcão | reforço próprio | **crítico garantido** (passa do teto de 40%) |
| Flecha Envenenada (passiva) | veneno, pilhas (como o Sangramento), estado permanente | **efeito em todo acerto** (hoje um efeito só vale dentro da própria habilidade) |
| Ferida Aberta | sangramento | **dano maior se o alvo andar** |
| Nuvem Tóxica | área no chão (como o Campo de Relâmpagos) | — |
| Flecha Dispersa | dano | **copiar estados para outros monstros** |
| Praga | estado em área | **estado que pula ao acabar** |

### Ajustes do simulador (T-433)

Os dados estão em `data/abilities_marksman.json` e `data/abilities_hunter.json`, com os números das tabelas acima. Mudou **um número**: o custo do Tiro Certeiro. Medidas em `tasks/reports/balance-v0.21.md`.

| Habilidade | Antes | Depois | Por quê |
|---|---|---|---|
| Tiro Certeiro | custo 35 | **20** | o Arqueiro tem **90 de Mana** do 1 ao 40 (não põe INT) e 5,5/s; o Olho de Falcão (70) e o Tiro Certeiro (35) juntos passavam disso, e o bot gastava a Mana que voltava nas habilidades baratas: com o Olho de Falcão ligado, o Tiro Certeiro não saía. Com 20, os dois cabem nos 90 e saem juntos. Contra a elite, o Atirador levava **84–98%** do tempo da base (meta: no máximo 85%); com 20, **72–78%** |

O dano do Tiro Certeiro (feito para um tiro que pedia 1,5 s parado) **não precisou cair**: a rajada com o Olho de Falcão fica longe da meta (abaixo). A duração do Olho de Falcão (15/17/20 s) é a do designer.

**Olho de Falcão (pendente com o designer):** com 70 de Mana, ele quase não acrescenta dano. Contra um alvo que não morre, o Atirador causa nos 20 s **0,98–0,99×** o que causa sem ele (13,0 mil × 13,2 mil no 37; 14,2 mil × 14,3 mil no 40), e mata a elite do 40 em 8,9 s com ele e 9,1 s sem. Motivo: o perfil esperado já tem 40% de crítico a 6+ tiles (o teto) com × 1,65, então todo golpe crítico rende só +31%, e os 70 de Mana pagam 3 Tiros Certeiros. Com o custo a 40, a elite cai em 6,9 s; a 30, em 6,2 s (1,18× de dano nos 20 s). Ficou em 70: nenhuma meta falha e a tarefa pede não mexer no que o designer pediu sem isso; a decisão está no relatório, §5.

O que os dados decidem onde o design não diz:

- **Flecha Perfurante:** linha de 1,2 de largura (a da Flecha Devastadora NH2).
- **Tiro Debilitante:** o estado (Debilitado) tem as etiquetas `slow` e `weaken`, sem `control` (elites e chefes não o encurtam).
- **Olho de Falcão:** o +2 de alcance do NH3 vale para os disparos com a etiqueta `ranged`, como o +1 da Mira Firme.
- **Flecha Envenenada:** o mesmo estado em todos os NH; o Veneno do Caçador do NH sai do apelido `status.hunter_venom` (T-432). Sem pontos nela (a Nuvem e a Praga de quem não a tem), vale o veneno do NH1.
- **Ferida Aberta:** sem dano na hora (só o sangramento); o estado renova a duração.
- **Nuvem Tóxica:** duas áreas juntas, uma com o dano a cada 1 s e outra com 1 pilha a cada 2 s (a primeira aos 2 s); a lentidão do NH5 é um estado de 1,2 s renovado a cada segundo na nuvem.
- **Flecha Dispersa:** copia os estados com `hunter_venom` ou `bleed` (o Sangramento comum e a Ferida Aberta) para os 2/2/3/3/4 monstros mais perto a até 4 tiles.
- **Praga:** a Praga NH3 põe 5 pilhas, mas o máximo do veneno vem da Flecha Envenenada (3 no NH1 e NH2, 4 no NH3 e NH4, 5 no NH5).

Metas revistas (`design/progression-v0.1.md` §8): no nível 25 a meta de tempo das segundas classes do Arqueiro é não ficar mais lento que a base (85% do 30 em diante); a rajada do Olho de Falcão no máximo 1,5× (era 2,5×); o Guardião até 2,0× na diferença entre segundas classes.

## 7. Mago em detalhe

**Perfil de referência no nível 21:** todos os pontos em INT (111) e cajado comum do nível 21 (+11 INT): **INT 122**. Nesse perfil, o Projétil Arcano dá 113 e a Explosão Elemental dá 271 no NH1 e 922 no NH5 (4 explosões).

### Piromante — dano em área e queimadura

Usa um estado novo, a **Queimadura** (dano de fogo por segundo, 4 s). Cada habilidade do Piromante que queima aplica a Queimadura do seu NH; uma nova renova a antiga.

| NH da habilidade | Queimadura por s | Perfil nível 21 |
|---|---|---|
| 1 | 3 + 0,3 × INT | 40/s, 158 no total |
| 2 | 3,6 + 0,36 × INT | 48/s, 190 |
| 3 | 4,2 + 0,42 × INT | 55/s, 222 |
| 4 | 4,8 + 0,48 × INT | 63/s, 253 |
| 5 | 5,4 + 0,54 × INT | 71/s, 285 |

#### Combustão — detona as queimaduras

Custo 40, recarga 10 s, raio 8 em volta do Piromante. Cada monstro **queimando** no raio sofre o dano, e a Queimadura acaba.

| NH | Valor por monstro queimando | Outros efeitos | Perfil nível 21 |
|---|---|---|---|
| 1 | dano 20 + 1,5 × INT | — | 203 |
| 2 | dano 24 + 1,8 × INT | — | 244 |
| 3 | dano 28 + 2,1 × INT | a Queimadura **não acaba** | 284 |
| 4 | dano 32 + 2,4 × INT | a Queimadura não acaba | 325 |
| 5 | dano 36 + 2,7 × INT | a Queimadura não acaba e **pula** para monstros a até 2 tiles | 365 |

#### Rajada de Chamas — cone curto de fogo

Custo 35, recarga 6 s, cone de 90° e 4 tiles à frente. Aplica Queimadura.

| NH | Valor | Outros efeitos | Perfil nível 21 |
|---|---|---|---|
| 1 | dano 14 + 1,4 × INT | — | 185 |
| 2 | dano 16,8 + 1,68 × INT | — | 222 |
| 3 | dano 19,6 + 1,96 × INT | cone de **120°** | 259 |
| 4 | dano 22,4 + 2,24 × INT | cone de 120° | 296 |
| 5 | dano 25,2 + 2,52 × INT | cone de 120° e a Queimadura dura **6 s** | 333 |

#### Chão em Chamas — área que queima

Custo 50, recarga 14 s, alcance 9. Um pulso por segundo em quem está na área; cada pulso aplica Queimadura.

| NH | Valor por pulso | Raio | Duração | Perfil nível 21 |
|---|---|---|---|---|
| 1 | dano 5 + 0,6 × INT | 3 | 6 s | 6 × 78 = 469 |
| 2 | dano 6 + 0,72 × INT | 3 | 6 s | 6 × 94 = 563 |
| 3 | dano 7 + 0,84 × INT | **4** | 6 s | 6 × 109 = 657 |
| 4 | dano 8 + 0,96 × INT | 4 | 6 s | 6 × 125 = 751 |
| 5 | dano 9 + 1,08 × INT | 4 | **8 s** | 8 × 141 = 1 126 |

#### Bola de Fogo — projétil que explode

Custo 45, recarga 7 s, alcance 10. Explode no primeiro monstro que acerta e aplica Queimadura em todos na explosão.

| NH | Valor | Raio da explosão | Perfil nível 21 |
|---|---|---|---|
| 1 | dano 38 + 2,8 × INT | 2 | 380 |
| 2 | dano 45,6 + 3,36 × INT | 2 | 456 |
| 3 | dano 53,2 + 3,92 × INT | **2,5** | 531 |
| 4 | dano 60,8 + 4,48 × INT | 2,5 | 607 |
| 5 | dano 68,4 + 5,04 × INT | **3** | 683 |

#### Meteoro (assinatura, nível 35) — grande área com aviso

Custo 100, recarga 120 s, alcance 10. Aviso aliado de 2 s no chão; depois o impacto, que aplica Queimadura.

| NH | Valor | Outros efeitos | Perfil nível 21 |
|---|---|---|---|
| 1 | dano 90 + 9 × INT | raio 5 | 1 188 |
| 2 | dano 117 + 11,7 × INT | raio **6** | 1 544 |
| 3 | dano 144 + 14,4 × INT | raio 6 e deixa **Chão em Chamas** (NH1) por 5 s | 1 901 |

### Arcanista — dano constante em alvo único

O Arcanista rende mais quanto mais tempo fica no mesmo alvo: o Raio Arcano sobe a cada pulso, a Marca Arcana acumula e os Orbes atiram sozinhos. Trocar de alvo perde o que foi acumulado.

#### Raio Arcano — canalizado, o dano sobe a cada pulso

Custo 40, recarga 8 s, alcance 9. Canaliza parado; um pulso a cada 0,5 s, e **cada pulso causa 15% mais** que o anterior. Andar cancela.

| NH | Primeiro pulso | Canalização | Outros efeitos | Perfil nível 21 |
|---|---|---|---|---|
| 1 | dano 6 + 0,5 × INT | 3 s (6 pulsos) | — | 67 → 135, total 587 |
| 2 | dano 7,2 + 0,6 × INT | 3 s | — | 80 → 162, total 704 |
| 3 | dano 8,4 + 0,7 × INT | **4 s (8 pulsos)** | — | 94 → 250, total 1 288 |
| 4 | dano 9,6 + 0,8 × INT | 4 s | — | 107 → 285, total 1 472 |
| 5 | dano 10,8 + 0,9 × INT | 4 s | pode **andar devagar** (−50%) sem cancelar | 121 → 321, total 1 655 |

#### Marca Arcana — dano ao longo do tempo que acumula

Custo 15, recarga 3 s, alcance 9. Cada uso põe 1 pilha (dura 10 s; um uso novo renova todas). Cada pilha causa o dano por segundo.

| NH | Dano por s por pilha | Pilhas no máximo | Perfil nível 21 |
|---|---|---|---|
| 1 | 1 + 0,12 × INT | 3 | 16/s por pilha, 47/s no máximo |
| 2 | 1,2 + 0,144 × INT | 3 | 19/s, 56/s no máximo |
| 3 | 1,4 + 0,168 × INT | **4** | 22/s, 88/s no máximo |
| 4 | 1,6 + 0,192 × INT | 4 | 25/s, 100/s no máximo |
| 5 | 1,8 + 0,216 × INT | **5** | 28/s, 141/s no máximo |

#### Orbes Arcanos — atiram sozinhos no alvo atual

Custo 45, recarga 16 s, duração 10 s. Os orbes giram em volta do Arcanista e cada um atira no **alvo atual** a cada 1,5 s.

| NH | Orbes | Valor por disparo | Perfil nível 21 |
|---|---|---|---|
| 1 | 2 | dano 2 + 0,2 × INT | 26 cada, 35/s |
| 2 | 2 | dano 2,4 + 0,24 × INT | 32 cada, 42/s |
| 3 | **3** | dano 2,8 + 0,28 × INT | 37 cada, 74/s |
| 4 | 3 | dano 3,2 + 0,32 × INT | 42 cada, 84/s |
| 5 | **4** | dano 3,6 + 0,36 × INT | 48 cada, 127/s |

#### Sifão Arcano — dano que acelera a Mana

Sem custo, recarga 10 s, alcance 9, conjuração 1 s. **Decisão do designer (2026-11-22):** em vez de devolver Mana na hora, o Sifão dá ao Arcanista **regeneração de Mana a mais por 5 s**, para manter a regra "Mana vem só do tempo" (`design/progression-v0.1.md`, seção 4). O total é o mesmo da versão anterior (30 a 50), só que espalhado em 5 s. Usa a mesma peça da Inspiração do Sacerdote (T-421).

| NH | Valor | Mana por s a mais (5 s) | Outros efeitos | Perfil nível 21 |
|---|---|---|---|---|
| 1 | dano 20 + 1,5 × INT | +6 (30 no total) | — | 203 |
| 2 | dano 24 + 1,8 × INT | +7 (35) | — | 244 |
| 3 | dano 28 + 2,1 × INT | +8 (40) | põe **1 pilha** de Marca Arcana | 284 |
| 4 | dano 32 + 2,4 × INT | +9 (45) | põe 1 pilha | 325 |
| 5 | dano 36 + 2,7 × INT | +10 (50) | põe 1 pilha e conjuração **0,5 s** | 365 |

#### Feixe do Vazio (assinatura, nível 35) — canalização longa num alvo

Custo 90, recarga 90 s, alcance 9. Canaliza 4 s, um pulso a cada 0,5 s (8 pulsos). Pode andar 50% mais devagar enquanto canaliza.

| NH | Valor por pulso | Outros efeitos | Perfil nível 21 |
|---|---|---|---|
| 1 | dano 20 + 2 × INT | — | 8 × 264 = 2 112 |
| 2 | dano 26 + 2,6 × INT | — | 8 × 343 = 2 746 |
| 3 | dano 32 + 3,2 × INT | ao acabar, o alvo recebe **+15% de dano** de todos por 6 s | 8 × 422 = 3 379 |

### Mecânicas do Mago

| Habilidade | Já existe no Core | Novo |
|---|---|---|
| Queimadura | estado com dano por segundo (como o veneno) | — |
| Combustão | dano em área | **dano só em quem tem um estado**, e tirar o estado |
| Rajada de Chamas | arco à frente (como o Golpe Pesado), com alcance maior | — |
| Chão em Chamas | área no chão (como o Campo de Relâmpagos) | — |
| Bola de Fogo | projétil, área | explodir no primeiro que acerta (a verificar no Core) |
| Meteoro | área com aviso (como o Cataclismo Arcano) | — |
| Raio Arcano | canalização (como a Flecha Devastadora) | **dano que sobe a cada pulso**; andar canalizando (NH5) |
| Marca Arcana | pilhas (como o Sangramento) | — |
| Orbes Arcanos | — | **orbes que atiram sozinhos no alvo atual** |
| Sifão Arcano | dano, pilha de Marca Arcana | **regeneração de Mana vinda de um estado** (a mesma peça da Inspiração, T-421) |
| Feixe do Vazio | canalização, mais dano recebido (como a Marca do Caçador) | andar canalizando |

### Ajustes do simulador (T-443)

Os dados estão em `data/abilities_pyromancer.json` e `data/abilities_arcanist.json`, com os números das tabelas acima. **Nenhum número mudou:** com os custos, recargas e valores do design, as metas do Mago passam (`tasks/reports/balance-v0.22.md`). A Mana do Piromante, que preocupava, não falta: o Mago chega ao 21 com ~340 de Mana e ~18/s (534 e 27,7/s no 40), e as lutas do grupo típico acabam antes de ela acabar. Antes e depois:

| Habilidade | Antes (design) | Depois (dados) |
|---|---|---|
| Piromante (as 5) | tabelas acima | **iguais** |
| Arcanista (as 5) | tabelas acima | **iguais** |

O que os dados decidem onde o design não diz:

- **Rajada de Chamas:** cone de 4 tiles (arco de 90°, 120° do NH3), na direção do alvo.
- **Chão em Chamas:** só a área (sem dano na hora); um pulso por segundo, o primeiro 1 s depois de lançar (6 pulsos em 6 s, 8 no NH5), cada um com a Queimadura do NH.
- **Bola de Fogo:** linha de 10 tiles e 1 de largura; sem monstro na linha, não explode (T-441).
- **Meteoro:** a Queimadura do NH do Meteoro (1 a 3); o Chão em Chamas do NH3 é uma área própria de 5 s, raio 3.
- **Marca Arcana:** as pilhas no máximo só renovam a duração. A pilha do Sifão (NH3+) é a do NH da Marca Arcana de quem lança (NH1 se ela não foi aprendida).
- **Orbes Arcanos:** atiram no alvo atual, alcance 9 (o do Raio e da Marca), a partir de 1,5 s depois de lançar: 6 disparos de cada orbe em 10 s.

Metas revistas (`design/progression-v0.1.md` §8): as metas do Mago são medidas sem as assinaturas; na diferença entre segundas classes, o Guardião entra sem limite e o Inquisidor vai até 2,0×.

Alerta para o designer (relatório, §5): o Piromante fica muito acima da meta (37–70% do tempo do Mago só com a base contra os Três Lobos, quando a meta é no máximo 85%) e também mais rápido que o Arcanista contra um alvo único forte, ao contrário do papel das duas.

## 8. Clérigo em detalhe

**Perfil de referência no nível 21:** pontos divididos entre INT e VIT (INT 62, VIT 58) e maça comum do nível 21 (ARMA 22). Nesse perfil, a Cura Direta dá 150 no NH1 e 270 no NH5, e o Golpe Sagrado dá 59.

### Sacerdote — cura do grupo

#### Oração Restauradora — cura em área ao longo do tempo

Custo 45, recarga 12 s, raio 6 em volta do Sacerdote. Cura por segundo todos os aliados que estavam no raio ao lançar.

| NH | Cura por s | Duração | Outros efeitos | Perfil nível 21 |
|---|---|---|---|---|
| 1 | 4 + 0,4 × INT | 8 s | — | 29/s, 230 em cada aliado |
| 2 | 4,8 + 0,48 × INT | 8 s | — | 35/s, 276 |
| 3 | 5,6 + 0,56 × INT | **10 s** | — | 40/s, 403 |
| 4 | 6,4 + 0,64 × INT | 10 s | — | 46/s, 461 |
| 5 | 7,2 + 0,72 × INT | 10 s | raio **8** | 52/s, 518 |

#### Bênção — atributos para todos por perto

Pedido do designer (2026-11-20). Custo 35, recarga 30 s, raio 6 em volta do Sacerdote. Todos os aliados no raio ao lançar (com ele) ganham **pontos em todos os atributos** (FOR, DES, AGI, INT e VIT) pela duração. O bônus escala com a INT do Sacerdote.

| NH | Atributos (cada um) | Duração | Outros efeitos | Perfil nível 21 |
|---|---|---|---|---|
| 1 | +(2 + 0,08 × INT) | 15 s | — | +7 em cada atributo |
| 2 | +(2,4 + 0,096 × INT) | 15 s | — | +8 |
| 3 | +(2,8 + 0,112 × INT) | **20 s** | — | +10 |
| 4 | +(3,2 + 0,128 × INT) | 20 s | — | +11 |
| 5 | +(3,6 + 0,144 × INT) | 20 s | raio **8** | +13 |

Para ter ideia do tamanho: +7 em cada atributo são 35 pontos, o que um personagem ganha em 7 níveis. Num Guerreiro de nível 21, +7 de FOR e de VIT dão +31 no Golpe Pesado (NH5) e +70 de vida. A Bênção fica ligada metade do tempo no NH1 e dois terços a partir do NH3.

#### Graça Veloz — velocidade para todos por perto

Pedido do designer (2026-11-20). Custo 20, recarga 20 s, raio 6 em volta do Sacerdote. Todos os aliados no raio ao lançar (com ele) andam mais rápido pela duração. Ajuda o grupo a fugir, a reposicionar numa luta e a atravessar o mapa.

**Dura 30 s em todos os NH** (pedido do designer, 2026-11-21). Com a recarga de 20 s, dá para manter o efeito ligado o tempo todo, e isso é **de propósito** (decisão do designer, 2026-11-21): a Graça Veloz é o reforço de viagem e de posicionamento do grupo.

| NH | Velocidade de movimento | Duração | Outros efeitos |
|---|---|---|---|
| 1 | +15% | 30 s | — |
| 2 | **+17%** | 30 s | — |
| 3 | **+20%** | 30 s | **tira a lentidão** dos aliados no raio |
| 4 | **+22%** | 30 s | tira a lentidão |
| 5 | **+25%** | 30 s | tira a lentidão, raio **8** e imunidade a lentidão por 10 s |

#### Inspiração — mais Mana por segundo para os aliados

Decisão do designer (2026-11-22): **substitui o Elo da Vida**, que repetia o desvio de dano da Interceptação do Guardião. Custo 30, recarga 30 s, raio 6 em volta do Sacerdote. Os aliados que estavam no raio ao lançar, **menos o próprio Sacerdote**, ganham regeneração de Mana a mais pela duração. O bônus escala com a INT do Sacerdote.

Segue a regra **"Mana vem só do tempo"** (`design/progression-v0.1.md`, seção 4): a Inspiração não dá Mana na hora, ela aumenta a regeneração. O Sacerdote fica de fora para a habilidade não pagar o próprio custo.

| NH | Mana por s a mais | Duração | Outros efeitos | Perfil nível 21 |
|---|---|---|---|---|
| 1 | +(1,5 + 0,04 × INT) | 12 s | — | +4,0/s, 48 em cada aliado |
| 2 | +(1,8 + 0,048 × INT) | 12 s | — | +4,8/s, 57 |
| 3 | +(2,1 + 0,056 × INT) | **15 s** | — | +5,6/s, 84 |
| 4 | +(2,4 + 0,064 × INT) | 15 s | — | +6,4/s, 96 |
| 5 | +(2,7 + 0,072 × INT) | 15 s | raio **8** | +7,2/s, 107 |

Para ter ideia do tamanho: todas as classes regeneram 5 de Mana por segundo, mais a INT. No NH1, a Inspiração quase dobra a regeneração do Guerreiro enquanto dura, e os 48 de Mana são meia barra dele (88 no nível 40). Fica ligada 40% do tempo no NH1 e metade a partir do NH3.

**Saiu o Renovar** (cura ao longo do tempo num aliado; aprovado pelo designer em 2026-11-20) para a Graça Veloz caber nas 4 habilidades normais: ele repetia a Oração Restauradora, que já cura ao longo do tempo, e a Cura Direta da base.

#### Santuário (assinatura, nível 35) — ninguém cai

Custo 90, recarga 120 s, raio 6 em volta do Sacerdote. Pela duração, os aliados no raio **não caem abaixo de 1 de vida**.

| NH | Duração | Outros efeitos |
|---|---|---|
| 1 | 4 s | — |
| 2 | 5 s | — |
| 3 | **6 s** | ao acabar, cura **20% da vida máxima** de cada aliado no raio |

**Por que não há Ressurreição:** o jogo já ressuscita. O **Milagre** do Clérigo base, no NH3 (nível 15), levanta os aliados caídos num raio de 8 tiles com 30% da vida (efeito `revive`, T-061), enquanto eles ainda estão na tela de morte. O Santuário evita a morte em vez de desfazê-la.

### Inquisidor — dano sagrado corpo a corpo, com cura menor

As habilidades sagradas do Inquisidor causam **+25% contra mortos-vivos e corrompidos**, como o Golpe Sagrado, salvo onde a tabela diz outro valor.

#### Martelo da Justiça — dano sagrado e atordoamento

Custo 25, recarga 8 s, arco de 60° e alcance 1,9.

| NH | Valor | Outros efeitos | Perfil nível 21 |
|---|---|---|---|
| 1 | dano 10 + 1 × ARMA + 1,8 × INT | atordoa 1 s | 144 |
| 2 | dano 12 + 1,2 × ARMA + 2,16 × INT | atordoa 1 s | 172 |
| 3 | dano 14 + 1,4 × ARMA + 2,52 × INT | atordoa **1,5 s** | 201 |
| 4 | dano 16 + 1,6 × ARMA + 2,88 × INT | atordoa 1,5 s | 230 |
| 5 | dano 18 + 1,8 × ARMA + 3,24 × INT | atordoa 1,5 s e o arco vai a **120°** | 258 |

#### Consagração — área no chão que fere e cura

Custo 45, recarga 14 s, no chão onde o Inquisidor está. Um pulso por segundo: dano nos monstros (**+50%** contra mortos-vivos) e cura nos aliados.

| NH | Dano por s | Cura por s | Raio | Duração | Perfil nível 21 |
|---|---|---|---|---|---|
| 1 | 4 + 0,5 × INT | 3 + 0,3 × INT | 3 | 6 s | 35/s e 22/s |
| 2 | 4,8 + 0,6 × INT | 3,6 + 0,36 × INT | 3 | 6 s | 42/s e 26/s |
| 3 | 5,6 + 0,7 × INT | 4,2 + 0,42 × INT | 3 | **8 s** | 49/s e 30/s |
| 4 | 6,4 + 0,8 × INT | 4,8 + 0,48 × INT | 3 | 8 s | 56/s e 35/s |
| 5 | 7,2 + 0,9 × INT | 5,4 + 0,54 × INT | **4** | 8 s | 63/s e 39/s |

#### Selo da Luz — quem bate no alvo se cura

Custo 20, recarga 12 s, alcance 6, um monstro. Pela duração, todo aliado que causa dano no alvo marcado se cura de uma parte do dano.

| NH | Vida pelo dano | Duração |
|---|---|---|
| 1 | 10% | 8 s |
| 2 | 12% | 8 s |
| 3 | 15% | **10 s** |
| 4 | 17% | 10 s |
| 5 | 20% | 10 s |

#### Julgamento — dano sagrado forte de perto

Custo 30, recarga 7 s, alcance 3.

| NH | Valor | Contra mortos-vivos | Outros efeitos | Perfil nível 21 |
|---|---|---|---|---|
| 1 | dano 20 + 1,2 × ARMA + 2,5 × INT | +50% | — | 201 |
| 2 | dano 24 + 1,44 × ARMA + 3 × INT | +50% | — | 242 |
| 3 | dano 28 + 1,68 × ARMA + 3,5 × INT | +50% | alcance **5** | 282 |
| 4 | dano 32 + 1,92 × ARMA + 4 × INT | +50% | alcance 5 | 322 |
| 5 | dano 36 + 2,16 × ARMA + 4,5 × INT | **+75%** | alcance 5 e cura o Inquisidor em **20% do dano** | 363 |

#### Ira Divina (assinatura, nível 35) — o ataque básico vira área que cura

Custo 80, recarga 90 s, só no Inquisidor. Pela duração, o Golpe Sagrado acerta **todos num raio de 2** com mais dano, e cada acerto cura os aliados a até 4 tiles em uma parte do dano.

| NH | Dano do Golpe Sagrado | Cura nos aliados | Duração | Perfil nível 21 |
|---|---|---|---|---|
| 1 | +50% | 10% do dano | 10 s | 89 por alvo |
| 2 | +50% | 10% | **12 s** | 89 |
| 3 | **+80%** | **15%** | **15 s** | 107 |

### Mecânicas do Clérigo

| Habilidade | Já existe no Core | Novo |
|---|---|---|
| Oração Restauradora | estado em aliados no raio | **estado que cura por segundo** (hoje o periódico só causa dano; a verificar) |
| Bênção | estado em aliados no raio | **atributos vindos de um estado**, com o valor pela INT de quem lançou (hoje só o equipamento soma atributos, e os números de um estado são fixos) |
| Graça Veloz | estado em aliados no raio, velocidade, tirar estados, imunidade por etiqueta | — |
| Inspiração | estado em aliados no raio | **regeneração de Mana vinda de um estado** (a mesma peça da Bênção) e **deixar quem lançou de fora** |
| Santuário | estado em aliados no raio | **vida mínima de 1**; no NH3, **efeito ao acabar o estado** e **cura pela vida máxima do alvo** |
| Martelo da Justiça | dano, atordoamento | — |
| Consagração | área no chão | área que **também cura** aliados |
| Selo da Luz | estado no monstro | **vida pelo dano** (o mesmo da Sede de Sangue), para quem bate no alvo |
| Julgamento | dano sagrado, bônus contra mortos-vivos | — |
| Ira Divina | reforço próprio | **ataque básico em área** e cura pelo dano |

### Ajustes do simulador (T-423)

Os dados estão em `data/abilities_priest.json` e `data/abilities_inquisitor.json`, com os números das tabelas acima. **Nenhum número mudou:** com os custos, recargas e valores do design, as metas do Clérigo passam (`tasks/reports/balance-v0.20.md`). Antes e depois:

| Habilidade | Antes (design) | Depois (dados) |
|---|---|---|
| Sacerdote (as 5) | tabelas acima | **iguais** |
| Inquisidor (as 5) | tabelas acima | **iguais** |

O que os dados decidem onde o design não diz:

- **Bênção:** o Sacerdote recebe a Bênção **depois** dos aliados, para todos ganharem o bônus pela INT dele sem a Bênção (com a ordem inversa, os aliados ganhavam +8 em vez de +7 no perfil do 21).
- **Santuário** vale também para o Sacerdote ("ninguém cai"); **Oração Restauradora**, **Graça Veloz** e **Bênção** também; a **Inspiração** não.
- **Graça Veloz NH5:** a imunidade a lentidão é um estado à parte (Passo Livre, 10 s), ao lado da velocidade (30 s).
- **Contra mortos-vivos e corrompidos:** Martelo da Justiça e Ira Divina +25% nos dois; Consagração +50% contra mortos-vivos e +25% contra corrompidos; Julgamento +50% (NH5 +75%) contra mortos-vivos e +25% contra corrompidos.
- **Ira Divina:** o golpe trocado é o Golpe Sagrado da base (1 × ARMA + 0,6 × INT) × 1,5 no NH1 e no NH2 e × 1,8 no NH3, no alvo e em todos a até 2 dele, com a mesma recarga (1,1 s) e alcance (6).

Meta revista: a diferença entre segundas classes passou a comparar cada uma no encontro solo da classe base dela, com o limite do Clérigo (1,8×) para as do Clérigo e o Sacerdote sem limite (`design/progression-v0.1.md` §8).

## 9. Custo

São **40 habilidades novas** (5 por segunda classe), cada uma com dados, regras no Core, teste, ícone e efeito visual. Muitas mecânicas novas servem a mais de uma classe:

| Mecânica nova | Quem usa |
|---|---|
| Desviar dano de um aliado | Guardião (Interceptação, Juramento) |
| Vida pelo dano causado | Berserker (Sede de Sangue), Inquisidor (Selo da Luz, Julgamento, Ira Divina) |
| Estado que cura por segundo | Sacerdote (Oração Restauradora) |
| Atributos e regeneração de Mana vindos de um estado | Sacerdote (Bênção, Inspiração), Arcanista (Sifão Arcano) |
| Dano que depende de um estado ou da vida do alvo | Berserker (Execução), Piromante (Combustão), Caçador (Ferida Aberta) |
| Copiar ou espalhar estados | Caçador (Flecha Dispersa, Praga), Piromante (Combustão NH5) |
| Crítico, alcance e armadura como números de reforço | Atirador de Elite (Mira Firme, Tiro Certeiro, Olho de Falcão) |
| Canalizar andando, dano que sobe a cada pulso | Arcanista (Raio Arcano, Feixe do Vazio) |
| Unidades que atiram sozinhas | Arcanista (Orbes Arcanos) |
| Área que acompanha quem anda | Berserker (Tempestade de Aço) |
| Vida mínima de 1 | Sacerdote (Santuário) |
| Efeito em todo acerto (passiva que age a cada golpe) | Caçador (Flecha Envenenada) |
| Ataque básico em área | Inquisidor (Ira Divina) |
| Barra com 10 teclas, tela de escolha da segunda classe | todas |

## 10. Ainda falta decidir

O designer aprovou o Guardião e o Berserker em 2026-11-20 (seção 5).

1. ~~Piromante e Arcanista~~: **aprovados em 2026-11-22**. O Piromante ficou como está; no Arcanista, o Sifão Arcano passou a aumentar a regeneração de Mana (seção 7). Tarefas na seção 6i de `tasks/README.md` (T-440).
2. ~~Atirador de Elite e Caçador~~: **aprovados em 2026-11-22**. O Caçador ficou como está; no Atirador, o Tiro Certeiro ficou sem conjuração e o Olho de Falcão dura de 15 a 20 s (seção 6). Tarefas na seção 6h de `tasks/README.md` (T-430).
3. ~~Sacerdote e Inquisidor~~: **aprovados em 2026-11-22**. O Inquisidor ficou como está; no Sacerdote, a **Inspiração** entrou no lugar do Elo da Vida (seção 8). Tarefas na seção 6g de `tasks/README.md` (T-420).
4. **XP por hora** como meta de cada área (o simulador mede), quando as áreas novas forem planejadas.

Decididos em 2026-11-21: **+5 de atributo ao virar segunda classe**; foco só nas classes, com as áreas novas depois; uma **ferramenta para subir de nível** no teste (seção 4.2).

Todos os números das seções 5 a 8 seguem a regra da seção 2 e ainda **não passaram pelo simulador**: devem mudar depois dos testes de design.

## 11. Para a T-390

- A fase 4 hoje é infraestrutura (CI/CD, observabilidade, testes de carga, produção separada). Nível 40, segunda classe e mapas novos são **conteúdo grande**: cabem melhor numa fase própria, ou numa trilha paralela, do que dentro da fase 4.
- **Foco: só as classes.** Ordem: (1) regras do nível 40 (pontos, NH por classe, +5 de atributo na segunda classe) e simulador até o 40; (2) **ferramenta para subir de nível** no servidor de teste e no menu do Esc (seção 4.2); (3) sistema de segunda classe e passivas no Core e no cliente (escolha definitiva, barra de 10 teclas) com o **Guerreiro**; (4) as outras três classes.
- **Depois das classes:** as áreas novas (do 16 ao 40), com monstros, elites e masmorras, numa fase própria.
- Junto com isso, rever as metas dos testes de design e reativar os 19 casos marcados com `PlaytestRebalance.Pending` (`tasks/reports/playtest-v0.3.md`).
