# Nível 40 e segunda classe — esboço v0.1

Status: **esboço em discussão com o designer**, entrada para a T-390. Pedido do designer em 2026-11-19, ao fechar o playtest v0.3: **mais níveis** e uma **segunda classe**, com **duas opções por classe**. Decisões fechadas em 2026-11-20 (seção 1).

Os números são ponto de partida para o simulador, como em `design/progression-v0.1.md`. O que ainda falta decidir está na seção 8.

## 1. Decisões do designer

| Assunto | Decisão |
|---|---|
| Nível máximo | **40** (foram considerados 30, 35 e 90) |
| Segunda classe | no **nível 20**, com **4 habilidades normais e 1 assinatura**, o mesmo formato da classe base |
| Habilidades por personagem | **8 normais e 2 assinaturas**: 8 × 5 + 2 × 3 = 46 NH |
| Pontos de habilidade | **40** no nível 40 (1 por nível, como hoje) |
| Pontos de atributo | **200** no nível 40 |
| Barra | uma tecla por habilidade: **10 teclas** (1–5 da base, 6–0 da segunda classe), mais ataque básico e poção |
| Curva de XP | a dificuldade continua crescendo como hoje, com **monstros mais fortes que dão mais XP** (seção 3) |
| Guerreiro | Guardião e Berserker |
| Arqueiro | Atirador de Elite (alvo único) e Caçador |
| Mago | Piromante (dano em área) e Arcanista (dano constante em alvo único) |
| Clérigo | Sacerdote e Inquisidor |
| Trocar de segunda classe | **não pode**: a escolha é definitiva |
| Primeira classe a entregar | **Guerreiro**, com as duas segundas classes dele, para validar o sistema |

## 2. Pontos e níveis de habilidade (NH)

### Pontos

- **Habilidade:** 1 por nível, a partir do 1, como hoje: 40 pontos para 46 NH. No 40 dá para ter 87% de tudo, e sobram 6 NH sem preencher. A escolha fica menor que hoje (65%), mas existe.
- **Atributo:** hoje são 5 por nível **a partir do 2** (70 no nível 15). No 40 isso dá **195**. Para chegar a **200**, a sugestão é dar **+5 ao virar segunda classe**, no nível 20. A outra saída é dar 5 já no nível 1, mas isso muda a calibração do nível 1 (§1 do design de progressão).

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
| 39 → 40 | ~255 | **~54 h** |

Base: os ~775 monstros até o 15 levam de 8 a 9 horas. Um nível alto leva de 2 a 3 horas.

**O que decide se "fica bom" é a XP por minuto, não a XP por monstro.** Um monstro que dá o dobro de XP mas leva o dobro do tempo para morrer não acelera nada. Para os monstros mais fortes compensarem, eles precisam dar mais XP **em relação ao tempo** que levam: é o papel dos elites, dos chefes de campo e das masmorras. O simulador mede isso: a meta a definir é a XP por hora de cada área.

**Conteúdo:** com a curva atual, o caminho do 20 ao 40 leva umas 40 horas, cerca de 3 vezes o que o primeiro mapa oferece hoje. Isso pede mais áreas, ou conteúdo que se repete bem (elites, masmorras de grupo).

**O primeiro mapa precisa ir até o 20.** Hoje ele termina no 15, com o Vigia. Como a segunda classe só entra no 20, falta conteúdo do 16 ao 20 para a classe base: uma área nova, ou os níveis do primeiro mapa redistribuídos.

### Outras regras que mudam com o nível 40

- `progression.maxLevel` e níveis de NH separados para a classe base e a segunda classe.
- **Agilidade:** com 200 pontos, a velocidade de ataque bate no máximo com AGI 100, e o crítico no máximo com AGI 140. Rever esses tetos ou o valor por ponto.
- **Monstros:** a escala por nível (`healthPerLevel 0,4`, `powerPerLevel 0,17`) é linear e nunca foi testada acima do 15.
- **Itens e defesa:** rever se a armadura chega ao teto de 60% cedo demais.

## 4. Como a segunda classe funciona

- **Quando:** no nível 20, falando com um mestre da classe na Cidade.
- **Escolha definitiva:** não dá para trocar. A tela de escolha mostra as duas opções com as habilidades e pede confirmação.
- **Pontos:** a partir do 21, os pontos podem ir para qualquer habilidade, da base ou da segunda classe.
- **Barra:** 10 teclas. As teclas 6–0 aparecem vazias até a segunda classe.

## 5. Guerreiro em detalhe (primeira classe a entregar)

**Perfil de referência no nível 21:** pontos divididos entre FOR e VIT, como o "perfil esperado" do simulador (FOR 62, VIT 60) e espada comum do nível 21 (ARMA 25). Nesse perfil, o Golpe de Espada dá 87 e o Golpe Pesado no NH5 dá 351.

### Guardião — proteger o grupo e segurar chefes

Escala mais com **VIT** que o Guerreiro base: quanto mais vida, mais protege.

#### Interceptação — corre até um aliado e recebe parte do dano dele

Custo 20, recarga 12 s, alcance 8, alvo: um aliado. Avança até o aliado e, pela duração, uma parte do dano que o aliado receberia vai para o Guardião (que se defende com a própria armadura e resistência).

| NH | Dano desviado | Duração | Outros efeitos |
|---|---|---|---|
| 1 | 30% | 4 s | — |
| 2 | 35% | 4 s | — |
| 3 | 35% | 5 s | o aliado ganha **escudo** 20 + 1 × VIT (80) |
| 4 | 40% | 5 s | escudo 20 + 1 × VIT |
| 5 | 40% | 6 s | escudo 40 + 2 × VIT (160) e recarga **9 s** |

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

Custo 40, recarga 20 s, raio 5 em volta do Guardião. Todos os aliados no raio (com ele) recebem menos dano pela duração.

| NH | Redução de dano | Duração | Outros efeitos |
|---|---|---|---|
| 1 | 15% | 6 s | — |
| 2 | 18% | 6 s | — |
| 3 | 20% | 8 s | raio **7** |
| 4 | 23% | 8 s | raio 7 |
| 5 | 25% | 8 s | raio 7 e os aliados **não são empurrados** |

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

Custo 30, recarga 20 s, só no Berserker. Pela duração, causa mais dano, anda mais rápido e **recebe 20% mais dano**.

| NH | Dano causado | Velocidade | Duração | Outros efeitos |
|---|---|---|---|---|
| 1 | +20% | +10% | 8 s | — |
| 2 | +24% | +10% | 8 s | — |
| 3 | +28% | +15% | **10 s** | — |
| 4 | +32% | +15% | 10 s | — |
| 5 | +36% | +15% | 10 s | atordoamento e lentidão nele duram **metade** |

#### Execução — golpe que termina o serviço

Custo 30, recarga 8 s, arco de 90° e alcance 1,9. Causa dano extra em alvo com pouca vida.

| NH | Valor | Dano extra | Outros efeitos | Perfil nível 21 |
|---|---|---|---|---|
| 1 | dano 20 + 1,3 × ARMA + 3,2 × FOR | +50% abaixo de 30% de vida | — | 251 |
| 2 | dano 24 + 1,56 × ARMA + 3,84 × FOR | +50% abaixo de 30% | — | 301 |
| 3 | dano 28 + 1,82 × ARMA + 4,48 × FOR | **+75%** abaixo de 30% | — | 351 |
| 4 | dano 32 + 2,08 × ARMA + 5,12 × FOR | +75% abaixo de 30% | — | 401 |
| 5 | dano 36 + 2,34 × ARMA + 5,76 × FOR | +75% abaixo de **35%** | se o golpe **matar**, a recarga zera | 452 |

#### Sede de Sangue — vida pelo dano causado

Custo 25, recarga 18 s, só no Berserker. Pela duração, uma parte do dano que ele causa volta como vida.

| NH | Vida pelo dano | Duração |
|---|---|---|
| 1 | 15% | 6 s |
| 2 | 18% | 6 s |
| 3 | 20% | **8 s** |
| 4 | 23% | 8 s |
| 5 | 25% | 8 s |

Contra chefes, a vida devolvida pode precisar de redução, para o Clérigo continuar importante (`design/ideias-futuras.md`, roubo de vida).

#### Tempestade de Aço (assinatura, nível 35) — gira enquanto anda

Custo 90, recarga 75 s. Por 4 s, gira sem parar, acertando todos num raio de 3 a cada 0,5 s (8 giros), andando 20% mais devagar.

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
| Sede de Sangue | estado no próprio Berserker | **vida pelo dano causado** |
| Tempestade de Aço | vários acertos com intervalo, Sangramento | **área que acompanha quem anda** (a verificar no Core) |

O Berserker usa quase só o que já existe. O Guardião traz o **desvio de dano**, que o Sacerdote (Elo da Vida) também vai usar.

## 6. As outras segundas classes (a detalhar depois do Guerreiro)

Habilidades guardadas em "Depois do MVP" (`classes-v0.1.md`) estão marcadas com ★. A quarta habilidade normal de cada uma é nova e precisa de aprovação.

### Arqueiro

| | **Atirador de Elite** | **Caçador** |
|---|---|---|
| Papel | alvo único, de longe, rajadas e críticos | venenos e sangramento em vários alvos, dano ao longo do tempo |
| Normal 1 | ★ Flecha Perfurante: tiro forte que atravessa o alvo e acerta quem está atrás | Flecha Envenenada: veneno que acumula no alvo |
| Normal 2 | ★ Tiro Debilitante: deixa o alvo lento e com menos dano | Ferida Aberta: sangramento forte, maior se o alvo se mexer |
| Normal 3 | Mira Firme: parado, ganha alcance e crítico; andar cancela | Nuvem Tóxica: área que envenena quem fica nela |
| Normal 4 | Tiro Certeiro: preparação de 1,5 s, dano muito alto, ignora parte da armadura | Flecha Dispersa: copia os venenos do alvo para os monstros por perto |
| Assinatura | Olho de Falcão: por alguns segundos, todo ataque é crítico | Praga: por alguns segundos, o veneno que acaba num monstro pula para os vizinhos |

### Mago

| | **Piromante** | **Arcanista** |
|---|---|---|
| Papel | dano em área, queimadura | dano constante em alvo único: o dano cresce enquanto ele mantém o mesmo alvo |
| Normal 1 | ★ Combustão: detona as queimaduras dos monstros por perto | Raio Arcano: canalizado, o dano sobe a cada segundo no mesmo alvo |
| Normal 2 | Rajada de Chamas: cone curto de fogo que queima | Marca Arcana: dano ao longo do tempo que acumula até 5 vezes |
| Normal 3 | Chão em Chamas: área que queima quem fica nela | Orbes Arcanos: 3 orbes que disparam sozinhos no alvo atual |
| Normal 4 | Bola de Fogo: projétil que explode numa área pequena e queima | Sifão Arcano: dano no alvo que devolve Mana |
| Assinatura | Meteoro: grande área com aviso no chão e dano muito alto | Feixe do Vazio: canalização longa com dano muito alto num alvo; anda devagar enquanto canaliza |

O Arcanista e o Atirador de Elite são de alvo único, mas o Atirador é **rajada** (dano alto de uma vez) e o Arcanista é **constante** (dano que cresce com o tempo e perde força se trocar de alvo).

### Clérigo

| | **Sacerdote** | **Inquisidor** |
|---|---|---|
| Papel | cura do grupo | dano sagrado corpo a corpo, com cura menor |
| Normal 1 | ★ Oração Restauradora: cura em área ao longo do tempo | Martelo da Justiça: dano sagrado e atordoamento |
| Normal 2 | ★ Bênção: reforço nos atributos de um aliado | Consagração: área no chão que fere mortos-vivos e cura aliados |
| Normal 3 | Elo da Vida: liga dois aliados, que dividem o dano | Selo da Luz: quem ataca o alvo marcado se cura |
| Normal 4 | Renovar: cura ao longo do tempo num aliado | Julgamento: dano sagrado de perto, maior contra mortos-vivos |
| Assinatura | **Santuário**: área onde os aliados não caem abaixo de 1 de vida por alguns segundos | Ira Divina: por alguns segundos, os ataques básicos viram área sagrada que cura |

**Por que não há Ressurreição:** o jogo já ressuscita. O **Milagre** do Clérigo base, no NH3 (nível 15), levanta os aliados caídos num raio de 8 tiles com 30% da vida (efeito `revive`, T-061), enquanto eles ainda estão na tela de morte. O Santuário evita a morte em vez de desfazê-la.

## 7. Custo

São **40 habilidades novas** (5 por segunda classe), cada uma com dados, regras no Core, teste, ícone e efeito visual. Mecânicas novas das outras classes, além das do Guerreiro (seção 5):

| Mecânica | Quem usa |
|---|---|
| Vida mínima de 1 | Sacerdote (Santuário) |
| Dano que cresce enquanto mantém o alvo | Arcanista |
| Veneno que se espalha | Caçador (Flecha Dispersa, Praga) |
| Ataque básico que vira área | Inquisidor (Ira Divina) |
| Curar com o dano causado | Inquisidor (Selo da Luz), o mesmo da Sede de Sangue |
| Barra com 10 teclas, tela de escolha da segunda classe | todas |

## 8. Ainda falta decidir

1. **Guardião e Berserker** como detalhados na seção 5.
2. **+5 de atributo ao virar segunda classe**, para fechar 200 no nível 40?
3. **Conteúdo do 16 ao 20:** área nova no primeiro mapa, ou redistribuir os níveis do mapa atual?
4. **XP por hora** como meta de cada área (o simulador mede).
5. A quarta habilidade normal e as ideias das outras seis segundas classes (seção 6).

## 9. Para a T-390

- A fase 4 hoje é infraestrutura (CI/CD, observabilidade, testes de carga, produção separada). Nível 40, segunda classe e mapas novos são **conteúdo grande**: cabem melhor numa fase própria, ou numa trilha paralela, do que dentro da fase 4.
- Ordem natural: (1) regras do nível 40 (pontos, NH por classe) e simulador até o 40; (2) sistema de segunda classe no Core e no cliente (escolha definitiva, barra de 10 teclas) com o **Guerreiro**; (3) conteúdo do 16 ao 20; (4) as outras três classes; (5) mapas novos para o 21 ao 40, com monstros, elites e masmorras.
- Junto com isso, rever as metas dos testes de design e reativar os 19 casos marcados com `PlaytestRebalance.Pending` (`tasks/reports/playtest-v0.3.md`).
