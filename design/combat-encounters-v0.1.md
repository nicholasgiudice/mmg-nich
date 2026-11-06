# Encontros de Combate — Primeiro Teste de Classes v0.1

## Objetivo

Validar se as quatro classes e suas habilidades criam decisões reais em combate. Cada cenário possui uma função de teste clara, condição de sucesso e falhas a evitar.

## Premissas

- combate em tempo real;
- visão 2D de cima;
- habilidades com cooldown;
- movimentação livre;
- ataque básico disponível;
- nenhum encontro exige uma classe específica;
- o jogador deve entender por que tomou dano ou perdeu o combate.

---

## Encontro 1 — Três Lobos

### Classe principal

Guerreiro.

### Local

Campos da Vigília, próximo à Clareira dos Caçadores.

### Composição

- 3 Lobos Cinzentos;
- distância inicial média;
- os lobos atacam o mesmo alvo por alguns segundos;
- se um lobo morrer, os demais ficam mais agressivos.

### O que testa

- gestão de Mana (regenera só com o tempo);
- controle de ameaça;
- uso de Golpe Circular;
- escolha entre dano e defesa;
- controle de grupo pequeno;
- capacidade de lutar sem curador.

### Decisões do jogador

1. Agrupar os lobos para usar Golpe Circular ou mantê-los separados.
2. Usar Guarda Firme antes de receber dano ou depois.
3. Gastar Mana em Golpe Pesado ou guardar para Golpe Circular e Guarda Firme.
4. Escolher o alvo do Golpe Pesado: o lobo mais ferido ou o que está mordendo.

### Resultado esperado

O Guerreiro vence sem ficar parado. Precisa alternar ataque, defesa e posicionamento.

### Falha de design

Se o jogador apenas usar Provocação e ataque básico até os lobos morrerem, o combate está raso.

---

## Encontro 2 — Lobo e Salteador

### Classe principal

Arqueiro.

### Local

Campos da Vigília, perto do Acampamento dos Salteadores.

### Composição

- 1 Salteador à distância;
- 1 Lobo Cinzento corpo a corpo;
- o Salteador tenta recuar e manter distância;
- o Lobo persegue o jogador.

### O que testa

- prioridade de alvo;
- manutenção de distância;
- Marca do Caçador;
- Recuo Rápido;
- Disparo Potente;
- diferença entre alvo perigoso e alvo resistente.

### Decisões do jogador

1. Eliminar o Salteador primeiro ou controlar o Lobo.
2. Usar Recuo Rápido para criar distância do Lobo ou guardar para escapar quando ele alcançar.
3. Gastar Mana em Disparo Potente no Salteador ou guardar para a Marca do Caçador no Lobo.
4. Lutar em campo aberto ou usar obstáculos.

### Resultado esperado

O Arqueiro vence se controlar o espaço e priorizar o alvo correto. Se for cercado, deve ficar em desvantagem clara.

### Falha de design

Se o Arqueiro conseguir atacar continuamente sem se mover, o cenário não está pressionando seu posicionamento.

---

## Encontro 3 — Quatro Ratos de Campo

### Classe principal

Mago.

### Local

Campos da Vigília, perto de uma fazenda abandonada.

### Composição

- 4 Ratos de Campo;
- aproximação rápida;
- os ratos tentam cercar o jogador;
- um rato muda de alvo ocasionalmente.

### O que testa

- dano em área;
- custo de Mana;
- movimentação;
- controle de aproximação;
- diferença entre usar Explosão Elemental cedo ou esperar agrupamento.

### Decisões do jogador

1. Esperar os ratos se agruparem ou atacar imediatamente.
2. Usar Prisão de Gelo no alvo mais próximo ou guardar para emergência.
3. Usar Campo de Relâmpagos para controlar passagem.
4. Ativar Barreira Arcana antes ou depois de ser cercado.

### Resultado esperado

O Mago vence com bom posicionamento e uso eficiente de Mana. Se for cercado, precisa gastar recursos defensivos ou fugir.

### Falha de design

Se Explosão Elemental resolver o encontro sozinha sem exigir movimento ou gestão de recurso, o Mago está forte demais.

---

## Encontro 4 — Guarda Esquelético

### Classe principal

Clérigo.

### Local

Ruínas da Estrada Antiga, no Portão Partido.

### Composição

- 1 Guarda Esquelético;
- bloqueia ataques frontais;
- desfere golpe pesado após manter a guarda;
- pode receber uma pequena bênção de um altar próximo.

### O que testa

- dano sagrado;
- posicionamento lateral;
- Corrente Sagrada;
- uso de Escudo Sagrado;
- capacidade de jogar sozinho;
- diferença entre atacar e guardar Fé.

### Decisões do jogador

1. Atacar enquanto o escudo está levantado ou reposicionar.
2. Usar Golpe Sagrado para gerar dano ou guardar Fé.
3. Destruir o altar antes ou enfrentar o Guarda diretamente.
4. Usar Escudo Sagrado antes do golpe pesado ou reagir depois.

### Resultado esperado

O Clérigo vence sozinho, mas mais devagar que Guerreiro, Arqueiro ou Mago. O dano sagrado oferece vantagem contra mortos-vivos sem tornar a classe dominante contra todos os inimigos.

### Falha de design

Se o Clérigo só conseguir vencer usando cura e consumíveis, a progressão solo está quebrada.

---

## Encontro 5 — Matriarca da Floresta

### Grupo recomendado

- Guerreiro;
- Arqueiro;
- Mago;
- Clérigo.

### Local

Ninho da Matriarca, na Floresta Baixa.

### Estrutura

A arena possui:

- área aberta central;
- três trechos de teia no chão;
- duas bordas livres para reposicionamento;
- pontos de surgimento das aranhas menores;
- nenhuma parede que impeça a leitura do combate.

### Fase 1 — Caçada

A Matriarca usa:

- ataque corpo a corpo;
- investida curta;
- poça de veneno;
- invocação de duas aranhas menores.

#### Funções

- Guerreiro posiciona a Matriarca longe do grupo;
- Arqueiro mantém distância e marca o boss;
- Mago controla as aranhas menores;
- Clérigo remove veneno e sustenta o grupo.

### Fase 2 — Teia Sufocante

A Matriarca:

- cria áreas de teia;
- reduz o espaço seguro;
- invoca uma aranha maior;
- aumenta a frequência das poças de veneno.

#### Decisões

- matar as aranhas menores ou continuar no boss;
- mover o boss para uma área limpa;
- guardar Cataclismo Arcano para a fase final;
- usar Milagre para salvar o grupo ou esperar outra ameaça.

### Fase 3 — Frenesi

A Matriarca:

- fica mais rápida;
- recebe mais dano;
- aplica sangramento;
- reduz a duração de controles;
- deixa de invocar novas aranhas.

#### Objetivo

Finalizar a luta antes que o espaço da arena fique completamente contaminado.

### Resultado esperado

O grupo vence por coordenação e leitura de prioridade, não apenas por dano acumulado.

### Falhas de design

- o boss morrer sem o grupo precisar se mover;
- Clérigo conseguir limpar todo veneno sem custo;
- Mago controlar todas as aranhas sozinho;
- Guerreiro manter a Matriarca permanentemente neutralizada;
- Arqueiro causar dano sem nunca correr risco;
- a fase final ser apenas uma corrida de dano.

---

## Floresta Baixa — Fada da Névoa e Ent Corrompido (T-340)

Monstros comuns da Floresta Baixa (`design/first-map-v0.1.md` §3, níveis 5–8 pelo `design/progression-v0.1.md` §2), ao lado das Aranhas do Bosque (5) e antes da Matriarca (8). A Floresta ensina **posicionamento, controle de área, veneno e emboscadas**: a Fada pune quem fica parado e perde o alvo, o Ent pune quem não lê o chão. Solo é possível; em grupo é mais rápido e mais seguro.

Os números abaixo estão nos dados: `data/monsters.json`, `data/abilities_monsters.json`, `data/statuses.json`. Valem no **nível 1**. Entre parênteses, o valor no nível do monstro, pela escala da T-206 (vida × (1 + 0,4 × (nível − 1)), potência × (1 + 0,17 × (nível − 1))), antes da defesa do jogador.

### Fada da Névoa (`monster.mist_fairy`, nível 6)

**Papel:** atiradora frágil e difícil de prender. Na Clareira da Névoa aparece em pares, ou atrás de aranhas (emboscada).

| | |
|---|---|
| Vida | 110 (330) |
| Velocidade / raio | 3,4 (mais lenta que qualquer classe) / 0,38 (sprite humanoide LPC da T-056) |
| Tags | `fey` |
| IA | `ai.mist_kiter` (novo, `Combat/Ai/Profiles/MistKiterProfile.cs`) |

Habilidades:

- **Dardo de Névoa** (`mist_fairy.mist_bolt`): projétil instantâneo, alcance 9, a cada 2 s. Dano arcano 14 (26) e **Névoa Gelada** (`status.mist_chill`), lentidão de 20% por 2,5 s. Não é controle: não tem retorno decrescente.
- **Véu de Névoa** (`mist_fairy.mist_veil`): **conjuração de 0,6 s**, que é o aviso visual. Ela para, e o cliente mostra a barra de conjuração; o efeito de névoa em volta dela fica com a T-345. Depois ela fica **Na Névoa** (`status.in_mist`) por 2 s: **não pode ser alvo**, anda 20% mais rápido e não ataca. Recarga de 8 s.

Comportamento: fica a ~7 tiles e atira. Usa o véu quando alguém chega a 3,5 tiles, ou assim que puder depois de perder 25% da vida. Na névoa, afasta-se do alvo até a distância preferida e reaparece longe. **Visível, ela não corre**: foge só pela névoa. Com o véu em recarga, quem chegou perto tem cerca de 6 s para matá-la. Sem linha de visão, ela anda até enxergar o alvo.

**Decidido (troca em relação à proposta):** a proposta tinha a Fada fugindo de quem chega perto o tempo todo, como o Salteador. Nas sondagens, com a lentidão do dardo, o Guerreiro nunca alcançava uma Fada que corria e vencia 0% (balance-v0.11 §2). A fuga ficou só na névoa.

"Não pode ser alvo" (`flags.untargetable`, mecânica nova no Core): ninguém além dela mesma a escolhe como alvo de unidade. Ela também não é atingida por arcos, linhas, círculos, telegráfos, áreas persistentes, correntes ou alvos extras. Status que ela já tinha (veneno, marca) continuam correndo. Uma habilidade de alvo único lançada nela é rejeitada (`InvalidTarget`), e o jogador troca de alvo ou espera.

Resposta de cada classe:

- **Guerreiro:** avança e força o véu cedo. Na janela da recarga, mata a Fada com Golpe Pesado. Contra o par, Provocação puxa as duas para perto.
- **Arqueiro:** mata a Fada de longe antes de ela se sentir ameaçada, porque o véu só sai por proximidade ou depois de perder vida. Marca do Caçador antes de ela sumir faz o dano voltar quando ela reaparece.
- **Mago:** dano de explosão a distância (Explosão Elemental, Campo de Relâmpagos) a mata antes do véu. Prisão de Gelo a prende no lugar: mesmo que ela entre na névoa, não sai de perto.
- **Clérigo:** troca dano com ela e se sustenta com Escudo Sagrado e Corrente Sagrada. Na névoa, aproveita para se curar.

### Ent Corrompido (`monster.corrupted_ent`, nível 7)

**Papel:** bruto lento e resistente, com controle de área. É o "muro" da Floresta e pune quem foge em linha reta.

| | |
|---|---|
| Vida | 430 (1.462) |
| Velocidade / raio | 2,4 (lento) / 0,7 |
| Tags | `plant`, **`corrupted`** (as habilidades sagradas do Clérigo causam +25%, `bonusVsTags`) |
| Resistências | **físico 25%**; **fogo −30%** (fraqueza: recebe 130%) |
| IA | `ai.brute` (novo, `Combat/Ai/Profiles/BruteProfile.cs`) |

Habilidades:

- **Pisão Corrompido** (`corrupted_ent.trampling_slam`): círculo de raio 2,6 em volta dele, com **telegráfo de 1,2 s**. Dano físico 44 (89). Recarga de 7 s. Só sai com o alvo dentro do círculo, e ele fica parado até o golpe cair.
- **Raízes Famintas** (`corrupted_ent.grasping_roots`): círculo de raio 1,6 no ponto do alvo, alcance 9, com **telegráfo de 1,0 s**. Quem continua na área marcada sofre dano de natureza 16 (32) e fica **Preso** (`status.snared`, já existia) por 2 s. Recarga de 8 s. Só sai com o alvo a 3 tiles ou mais.
- **Rajada de Espinhos** (`corrupted_ent.thorn_volley`): instantânea, alvo único, alcance 8, a cada 2,5 s. Dano de natureza 14 (28). Só sai com o alvo a 3 tiles ou mais.
- **Açoite de Galhos** (`corrupted_ent.branch_lash`): golpe corpo a corpo, alcance 1,8, a cada 1,8 s. Dano físico 19 (38).

**Decidido:** a proposta foi mantida (golpe em área com telegráfo de 1,2 s, raízes que prendem quem fica na marca, resistência física e fraqueza a fogo). Entrou a **Rajada de Espinhos**. Sem ela, Arqueiro e Mago desviavam de tudo, levavam ~0 de dano e venciam mesmo 3 níveis abaixo (balance-v0.11 §2). Com ela, fugir do Ent custa vida, e o perfil fraco perde a corrida de dano.

Resposta de cada classe:

- **Guerreiro:** fica colado nele, sai do círculo no telegráfo do Pisão e volta. É a classe mais lenta contra ele, por causa da resistência física, mas ainda vence em ~36 s.
- **Arqueiro:** mantém distância, sai da marca das raízes e aceita os espinhos. Recuo Rápido tira da marca quando o telegráfo pega no meio de um disparo.
- **Mago:** fogo (Explosão Elemental) entra com +30%. Barreira Arcana absorve os espinhos e Prisão de Gelo segura o Ent longe.
- **Clérigo:** Golpe e Corrente Sagrados entram com +25% (`corrupted`). Ele se cura dos espinhos, sai do Pisão e vence pela sustentação.

### Grupos típicos e encontros de teste (`data/encounters.json`)

| Encontro | Composição | Nível do grupo |
|---|---|---|
| `encounter.mist_fairies` | 2 Fadas nv6 | 6 |
| `encounter.corrupted_ent` | 1 Ent nv7 | 7 |
| `encounter.mist_ambush` (Emboscada na Névoa) | 3 Aranhas Menores nv5 na frente + 1 Fada nv6 atrás; Guerreiro + Arqueiro | 6 |

Rodam na arena aberta do protótipo (`map.fields`). Os testes de design (`ForestDesignTests`) trocam o jogador por cada classe. Metas e resultados em `tasks/reports/balance-v0.11.md`:

- o perfil esperado do nível do grupo vence ≥ 85% em até 40 s, nas 4 classes;
- o perfil fraco (3 níveis abaixo) vence no máximo 50%. O Clérigo tem a mesma exceção do Guarda Esquelético: se cura indefinidamente, então a meta dele é demorar ≥ 1,3× o esperado;
- na Emboscada, o par vence em no máximo 70% do tempo dos solos, e cada um recebe no máximo 60% do dano que receberia sozinho.

### Falhas de design

- a Fada ficar na névoa mais tempo do que visível;
- o jogador perder o alvo sem ter visto o aviso da conjuração;
- o Ent ser vencido sem ninguém nunca sair de uma marca;
- o Guerreiro não conseguir lutar com o Ent (resistência física alta demais);
- quem ataca de longe vencer o Ent sem tomar dano.

---

## Matriz de decisões

| Encontro | Posicionamento | Prioridade | Gestão de recurso | Controle | Cura/suporte |
|---|---:|---:|---:|---:|---:|
| 3 Lobos | Médio | Médio | Médio | Alto | Baixo |
| Lobo + Salteador | Alto | Alto | Médio | Médio | Baixo |
| 4 Ratos | Alto | Médio | Alto | Alto | Baixo |
| Guarda Esquelético | Médio | Médio | Médio | Médio | Médio |
| Matriarca | Alto | Alto | Alto | Alto | Alto |
| Fadas da Névoa | Alto | Alto | Baixo | Médio | Baixo |
| Ent Corrompido | Alto | Médio | Médio | Alto | Médio |

## Critérios gerais de sucesso

O combate está aprovado quando:

- o jogador sabe por que tomou dano;
- cada classe possui pelo menos duas respostas válidas por encontro;
- não existe uma habilidade claramente obrigatória em toda luta;
- movimentação importa;
- recursos importam;
- grupos sem composição perfeita ainda conseguem jogar;
- o boss exige mudança de comportamento entre fases;
- o encontro continua compreensível sem excesso de efeitos visuais.

## Próximo passo de implementação

Antes de criar o jogo completo, implementar apenas um protótipo de combate com:

1. uma arena simples;
2. quatro classes;
3. três tipos de inimigo;
4. uma barra de vida;
5. uma barra de recurso;
6. três habilidades por classe inicialmente;
7. um encontro de boss simplificado.

As habilidades restantes entram depois que o combate básico provar que é divertido.
