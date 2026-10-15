# Validação das Classes contra o Primeiro Mapa — Versão 0.1

## Objetivo

Verificar se Guerreiro, Arqueiro, Mago e Clérigo possuem funções reais contra os inimigos e encounters do primeiro mapa. O teste deve identificar habilidades redundantes, classes obrigatórias e lacunas de design antes da implementação.

---

## 1. Critérios de validação

Cada classe precisa:

- jogar sozinha nos Campos da Vigília;
- contribuir em grupo na Floresta e nas Ruínas;
- possuir pelo menos uma resposta para inimigos móveis;
- possuir pelo menos uma resposta para grupos;
- ter uma fraqueza real;
- não depender de um único tipo de inimigo para ser útil;
- não resolver todos os problemas de combate sozinha.

---

## 2. Matriz contra monstros

| Inimigo | Guerreiro | Arqueiro | Mago | Clérigo |
|---|---|---|---|---|
| Rato de Campo | Golpe circular | Disparo múltiplo/ataque básico | Explosão elemental | Golpe sagrado |
| Lobo Cinzento | Provocação e controle | Recuo rápido e marca | Prisão de gelo | Cura e ataque sagrado |
| Salteador | Investida/interrupção | Marca e tiro debilitante | Controle de grupo | Bênção e purificação |
| Aranha do Bosque | Guarda firme | Armadilha e distância | Campo de relâmpagos | Purificação |
| Fada da Névoa | Investida | Tiro debilitante | Dissipar/controle | Corrente sagrada |
| Ent Corrompido | Controle de posição | Dano constante | Exploração de fraqueza elemental | Sustentação |
| Guarda Esquelético | Ataque lateral e ameaça | Disparo em alvo exposto | Dano mágico/controle | Dano sagrado |
| Sacerdote Morto | Interrupção | Eliminação prioritária | Dissipar magia | Purificação |
| Cavaleiro Espectral | Guarda firme e interceptação | Marca e mobilidade | Controle de movimento | Escudo e cura |

A matriz mostra funções distintas sem criar um inimigo que só possa ser derrotado por uma classe específica.

---

## 3. Validação por classe

## Guerreiro

### Funções confirmadas

- protege aliados contra lobos, salteadores e mortos-vivos;
- controla grupos de criaturas pequenas;
- interrompe Sacerdotes Mortos e conjuradores;
- mantém o boss voltado para si;
- protege selos durante a fase final do boss;
- cria espaço para Arqueiro e Mago.

### Risco

O Guerreiro pode virar um personagem passivo que apenas usa Provocação e espera ser atingido.

### Ajuste necessário

A geração de Vigor deve recompensar decisões, não apenas receber dano. O Guerreiro deve ganhar mais Vigor ao:

- bloquear um ataque forte;
- interromper uma habilidade;
- interceptar dano destinado a um aliado;
- manter um inimigo perigoso afastado da retaguarda.

### Resultado esperado

O Guerreiro joga ativamente e precisa escolher entre causar dano, controlar ou proteger.

---

## Arqueiro

### Funções confirmadas

- elimina Sacerdotes Mortos e Fadas da Névoa;
- mantém distância de Lobos e Aranhas;
- usa armadilhas na Floresta;
- marca o boss para aumentar o dano do grupo;
- ataca selos ou alvos secundários sem abandonar completamente a luta;
- causa dano consistente contra alvos resistentes.

### Risco

Se o mapa tiver áreas abertas demais, o Arqueiro pode atacar todos os inimigos sem se expor. Isso transforma posicionamento em uma formalidade.

### Ajuste necessário

O mapa deve ter:

- corredores estreitos nas Ruínas;
- árvores e obstáculos na Floresta;
- inimigos que cercam ou perseguem;
- ataques que obriguem o Arqueiro a trocar de posição.

O Arqueiro precisa ser forte quando se posiciona bem, não invulnerável.

### Resultado esperado

A classe é recompensada por leitura do espaço e punida quando fica encurralada.

---

## Mago

### Funções confirmadas

- limpa grupos de Ratos, Lobos e mortos-vivos menores;
- controla Fadas e Aranhas;
- explora fraquezas do Ent Corrompido;
- dissipa cura do Sacerdote Morto;
- controla invocações do boss;
- cria zonas de segurança ou perigo.

### Risco

O Mago pode dominar o PvE se dano em área, controle e mobilidade forem todos fortes ao mesmo tempo.

### Ajustes necessários

- habilidades de área devem exigir posicionamento;
- controle deve ter duração reduzida em elites e bosses;
- dano em área deve consumir Mana de forma relevante;
- Barreira Arcana deve proteger, mas não substituir o Guerreiro;
- Cataclismo Arcano deve ter cooldown alto e preparação visível.

### Resultado esperado

O Mago causa grande impacto quando antecipa o combate, mas sofre quando é pressionado.

---

## Clérigo

### Funções confirmadas

- remove veneno das Aranhas;
- remove maldições do Sacerdote Morto;
- protege o grupo contra o Cavaleiro Espectral;
- sustenta o grupo durante a Matriarca;
- cura durante as fases do Vigia;
- causa dano relevante contra mortos-vivos e criaturas corrompidas.

### Risco

O Clérigo pode ficar inútil quando joga sozinho ou quando o grupo não recebe dano.

### Ajuste necessário

A geração de Fé deve incluir ações ofensivas e preventivas:

- atacar criaturas corrompidas;
- remover uma condição perigosa;
- proteger um aliado antes do impacto;
- curar uma quantidade relevante de dano.

O Clérigo também precisa de dano solo suficiente para concluir os Campos e enfrentar inimigos comuns sem depender de outro jogador.

### Resultado esperado

O Clérigo é melhor em grupo, mas não é um personagem incapaz de jogar sozinho.

---

## 4. Validação dos encontros

## Matriarca da Floresta

### O que o encontro exige

- movimentação constante;
- limpeza de aranhas menores;
- controle de veneno;
- proteção contra dano corpo a corpo;
- ataque à distância durante áreas perigosas.

### Participação das classes

- Guerreiro segura a Matriarca e posiciona o corpo dela;
- Arqueiro mantém dano e usa armadilhas;
- Mago controla as aranhas menores e zonas de veneno;
- Clérigo purifica veneno e mantém o grupo vivo.

### Falha a evitar

O boss não pode ser derrotado ficando parado em um ponto enquanto o Clérigo cura tudo.

---

## O Vigia Sem Rosto

### O que o encontro exige

- controle de ameaça;
- evitar ataques frontais;
- sair de investidas;
- interromper invocações;
- controlar Guardas Esqueléticos;
- proteger os três selos;
- responder a medo e dano espectral.

### Participação das classes

- Guerreiro mantém o boss longe do grupo e intercepta ataques;
- Arqueiro marca o boss e elimina alvos secundários;
- Mago controla invocações e cria zonas de contenção;
- Clérigo cura, purifica e protege os selos ou jogadores.

### Falha a evitar

O combate não pode ser decidido apenas pelo dano por segundo. A destruição dos selos deve obrigar o grupo a dividir atenção e movimentar-se.

---

## 5. Composições de grupo

### Grupo equilibrado

```text
Guerreiro + Arqueiro + Mago + Clérigo
```

Maior segurança e cobertura completa de funções.

### Grupo ofensivo

```text
Guerreiro + Arqueiro + Mago + Mago
```

Mais dano e controle, mas menor sustentação. Deve funcionar com execução melhor.

### Grupo resistente

```text
Guerreiro + Guerreiro + Clérigo + Arqueiro
```

Mais segurança e menor dano em área. Deve ser bom contra elites e combates longos.

### Grupo sem Clérigo

```text
Guerreiro + Arqueiro + Mago + Arqueiro
```

Deve conseguir completar conteúdo básico usando consumíveis e bom posicionamento, mas sofrer mais contra veneno, maldição e lutas prolongadas.

### Grupo sem Guerreiro

```text
Arqueiro + Mago + Clérigo + Clérigo
```

Deve ser possível em áreas básicas, mas com dificuldade maior para controlar inimigos e proteger a retaguarda.

---

## 6. Ajustes de design confirmados

1. Controle de ameaça do Guerreiro não pode funcionar como botão automático em todo inimigo.
2. Controle do Mago deve sofrer redução contra elites e bosses.
3. Arqueiro precisa de mapas com obstáculos e inimigos que pressionem sua posição.
4. Clérigo precisa de dano solo suficiente para não depender de grupo desde o nível inicial.
5. Nenhum inimigo pode exigir uma classe específica.
6. Bosses devem exigir movimentação, prioridade e divisão de atenção.
7. Consumíveis devem permitir grupos sem Clérigo progredirem no conteúdo básico.
8. O primeiro mapa deve oferecer pelo menos duas rotas de progressão depois dos Campos.

## Próximo teste

Construir a especificação de combate de um encontro simples com:

- Guerreiro contra 3 Lobos;
- Arqueiro contra 1 Lobo e 1 Salteador;
- Mago contra 4 Ratos;
- Clérigo contra 1 Guarda Esquelético;
- grupo completo contra a Matriarca.

Esses cinco cenários serão o teste mínimo para verificar se as habilidades criam decisões reais.
