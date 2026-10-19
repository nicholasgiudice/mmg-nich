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

## Matriz de decisões

| Encontro | Posicionamento | Prioridade | Gestão de recurso | Controle | Cura/suporte |
|---|---:|---:|---:|---:|---:|
| 3 Lobos | Médio | Médio | Médio | Alto | Baixo |
| Lobo + Salteador | Alto | Alto | Médio | Médio | Baixo |
| 4 Ratos | Alto | Médio | Alto | Alto | Baixo |
| Guarda Esquelético | Médio | Médio | Médio | Médio | Médio |
| Matriarca | Alto | Alto | Alto | Alto | Alto |

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
