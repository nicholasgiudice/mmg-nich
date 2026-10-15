# Primeiro Mapa — Versão 0.1

## Objetivo

Definir a primeira região jogável do MMO: uma cidade medieval fortificada, três áreas externas, uma dungeon e um boss. Este mapa deve testar exploração, combate PvE, progressão das quatro classes e retorno à cidade.

## Estrutura geral

```text
                         [Floresta Baixa]
                         - Aranhas
                         - Fadas da Névoa
                         - Matriarca da Floresta
                                |
                                |
[Ruínas da Estrada Antiga] -- [Cidade-Fortaleza] -- [Campos da Vigília]
- Mortos-vivos                    - Hub seguro       - Ratos
- Sacerdote Morto                 - Mercado          - Lobos
- Cavaleiro Espectral             - Treinamento      - Salteadores
       |
       |
[Entrada da Cripta do Vigia]
       |
[Cripta do Vigia]
       |
[O Vigia Sem Rosto]
```

A estrutura é radial, mas cada direção possui identidade própria. A cidade é o único ponto totalmente seguro da primeira versão.

---

## 1. Cidade-Fortaleza

### Função

- ponto de início e retorno;
- recuperação de vida e recursos;
- compra e venda;
- treinamento das classes;
- entrega de missões;
- armazenamento de itens;
- preparação para expedições.

### Elementos

- praça central;
- mercado;
- estalagem;
- ferreiro;
- templo;
- salão de treinamento;
- armazém;
- muralha externa;
- três portões principais.

### Portões

| Portão | Destino | Perfil |
|---|---|---|
| Leste | Campos da Vigília | área inicial e segura |
| Norte | Floresta Baixa | emboscadas e veneno |
| Oeste | Ruínas da Estrada Antiga | mortos-vivos e exploração |

O portão sul fica fechado na primeira versão e pode servir como promessa de expansão futura. Não criar conteúdo atrás dele agora.

### Regra

Nenhum combate PvP. A cidade é totalmente segura.

---

## 2. Campos da Vigília

### Função

Primeira área externa. Ensina os fundamentos sem exigir grupo.

### Sensação

Campos abandonados, estradas antigas, pequenas fazendas e cercas quebradas. A cidade ainda é visível de vários pontos, reforçando que o jogador está perto da segurança.

### Subáreas

#### Estrada Leste

- caminho mais seguro;
- poucos inimigos;
- caravanas e NPCs;
- primeira missão de coleta e escolta.

#### Fazenda Abandonada

- Ratos de Campo;
- pequenos grupos de Lobos Cinzentos;
- casa com loot simples;
- primeiro encontro opcional com um Salteador.

#### Clareira dos Caçadores

- área aberta para combate;
- ponto de encontro para grupos;
- respawn um pouco maior de lobos;
- pequena fogueira usada como referência visual.

#### Acampamento dos Salteadores

- inimigos humanoides organizados;
- um arqueiro atrás de inimigos corpo a corpo;
- primeira situação que exige prioridade de alvo;
- baú protegido por grupo de inimigos.

### Inimigos

- Rato de Campo;
- Lobo Cinzento;
- Salteador da Estrada.

### Recompensas

- moeda;
- couro;
- carne;
- armas simples;
- armaduras iniciais;
- mapa parcial indicando a Floresta e as Ruínas.

### Acesso recomendado

Área inicial para todas as classes. Deve ser possível jogar sozinho.

---

## 3. Floresta Baixa

### Função

Introduzir posicionamento, controle de área, veneno e emboscadas.

### Sensação

A floresta bloqueia a visão, possui caminhos estreitos e faz a cidade desaparecer rapidamente. O jogador começa a sentir que está realmente deixando a civilização.

### Subáreas

#### Trilha dos Caçadores

- caminho principal;
- inimigos em baixa densidade;
- placas indicando perigo;
- conexão com a cidade e com o interior da floresta.

#### Bosque das Teias

- Aranhas do Bosque;
- teias no chão;
- poças de veneno;
- atalhos bloqueados por teias que podem ser removidas em missão futura.

#### Clareira da Névoa

- Fadas da Névoa;
- visibilidade reduzida;
- inimigos difíceis de localizar;
- pequenos eventos de emboscada.

#### Ninho da Matriarca

- área opcional;
- miniboss Matriarca da Floresta;
- acesso recomendado para grupos pequenos;
- recompensa melhor, mas sem bloquear a progressão principal.

### Inimigos

- Aranha do Bosque;
- Fada da Névoa;
- Ent Corrompido;
- Matriarca da Floresta.

### Recompensas

- seda;
- veneno;
- madeira corrompida;
- pó feérico;
- materiais para poções e armadilhas;
- acessórios com resistência a veneno ou controle.

### Acesso recomendado

Solo possível, mas grupos tornam a área mais segura. A Matriarca deve ser o primeiro teste real de coordenação.

---

## 4. Ruínas da Estrada Antiga

### Função

Introduzir mortos-vivos, corredores estreitos, inimigos com funções complementares e pistas da história do mundo.

### Sensação

Restos de uma estrada imperial e de uma fortificação antiga. A cidade atual foi construída muito depois, usando parte das pedras e fundações dessa estrutura.

### Subáreas

#### Portão Partido

- entrada das ruínas;
- Guardas Esqueléticos;
- combate frontal e bloqueio;
- primeira área com sensação de abandono histórico.

#### Pátio dos Mortos

- grupos de Guardas Esqueléticos;
- Sacerdotes Mortos protegidos atrás;
- exige eliminar alvos prioritários;
- altar com maldição temporária.

#### Torre Quebrada

- Cavaleiro Espectral;
- arena pequena;
- elite opcional;
- recompensa superior e fragmento de chave.

#### Estrada para a Cripta

- caminho mais perigoso;
- inimigos mistos;
- necessidade de juntar dois fragmentos de chave;
- entrada da dungeon ao final.

### Inimigos

- Guarda Esquelético;
- Sacerdote Morto;
- Cavaleiro Espectral.

### Recompensas

- metal antigo;
- ossos;
- símbolos profanos;
- fragmentos de chave;
- equipamentos melhores que os encontrados nos Campos.

### Acesso recomendado

A área pode ser explorada solo por jogadores preparados, mas a entrada da dungeon deve sinalizar claramente que o conteúdo foi desenhado para grupo.

---

## 5. Dungeon — Cripta do Vigia

### Função

Primeiro conteúdo fechado de grupo e preparação para o boss.

### Entrada

A entrada exige os fragmentos obtidos nas Ruínas. A chave deve ser coletiva ou consumível? Decisão futura. Para o MVP, recomenda-se que a chave seja permanente após descoberta para não transformar a entrada em grind artificial.

### Estrutura

```text
Entrada
  ↓
Salão dos Escudos
  ↓
Galeria das Cinzas
  ↓
Câmara dos Juramentos
  ↓
Sala do Vigia Sem Rosto
```

### Salão dos Escudos

- Guardas Esqueléticos;
- portas bloqueadas por grupos de inimigos;
- ensina controle de ameaça e posicionamento.

### Galeria das Cinzas

- Sacerdotes Mortos;
- áreas de maldição;
- inimigos que retornam uma vez se o altar não for desativado.

### Câmara dos Juramentos

- escolha entre duas rotas curtas;
- uma com mais inimigos e melhor loot;
- outra com menos combate e caminho mais seguro;
- primeira decisão de risco e recompensa.

### Sala do Boss

- arena ampla o suficiente para movimentação;
- três selos visíveis nas bordas;
- espaço para ataques em área;
- sem obstáculos aleatórios que quebrem a leitura do combate.

---

## 6. Boss — O Vigia Sem Rosto

### Conceito

Antigo comandante da fortificação original. Continua protegendo a cripta mesmo depois da morte e considera qualquer invasor uma ameaça à cidade.

### Fase 1 — Juramento de Ferro

- golpe frontal em cone;
- investida em linha reta;
- invocação de Guardas Esqueléticos;
- necessidade de manter o boss voltado para o Guerreiro.

### Fase 2 — O Rosto Vazio

- dano espectral;
- áreas de medo;
- vulnerabilidade maior a dano sagrado;
- sombras menores;
- necessidade de reposicionamento constante.

### Fase 3 — Último Comando

- três selos começam a ruir;
- o grupo deve interromper o boss e proteger os selos;
- a luta termina se o boss for derrotado ou se todos os selos forem destruídos;
- a segunda opção representa falha da expedição.

### Recompensas

- equipamento raro inicial;
- fragmento do antigo juramento;
- material de fabricação;
- moeda;
- item cosmético de baixa chance.

O boss não deve entregar um item tão forte que torne todo o equipamento anterior inútil.

---

## 7. Rotas e retorno

O mapa precisa incentivar o jogador a decidir quando voltar.

### Rotas

- Campos: retorno rápido pela estrada principal;
- Floresta: atalhos perigosos pela névoa;
- Ruínas: caminho mais lento e com maior risco;
- Dungeon: retorno apenas após concluir ou abandonar a expedição.

### Postos de referência

Para o MVP, não criar teletransportes. Usar referências visuais:

- fogueiras;
- torres quebradas;
- árvores grandes;
- estátuas;
- placas e portões.

O jogador deve conseguir se orientar sem depender de uma interface lotada.

---

## 8. Progressão do primeiro mapa

```text
Cidade
  ↓
Campos da Vigília
  ↓
Floresta Baixa ou Ruínas da Estrada Antiga
  ↓
Matriarca ou Cavaleiro Espectral
  ↓
Entrada da Cripta
  ↓
Cripta do Vigia
  ↓
O Vigia Sem Rosto
```

A Floresta e as Ruínas podem ser acessadas em ordem flexível. A dungeon exige preparação e não deve ser o primeiro destino de um personagem novo.

---

## 9. Critérios de validação

O mapa será considerado funcional quando:

- cada saída da cidade tiver identidade própria;
- as quatro classes tiverem pelo menos uma situação em que sejam úteis;
- jogadores solo consigam progredir nos Campos;
- grupos tenham vantagem real na Floresta e nas Ruínas;
- a Matriarca testar movimento e controle;
- o Vigia exigir posicionamento, cura, interrupção e prioridades;
- o jogador entender visualmente quando está se afastando da segurança;
- o retorno à cidade for uma decisão, não apenas uma obrigação arbitrária;
- nenhum mapa depender de PvP ou guerras para ser divertido.

## Fora do escopo

- guerras territoriais;
- PvP;
- evolução das classes;
- crafting complexo;
- múltiplas cidades;
- mundo dinâmico;
- dezenas de bosses;
- árvore extensa de talentos;
- economia entre guildas.
