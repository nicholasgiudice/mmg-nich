# Mapas do nível 16 ao 40 — esboço v0.1

Status: **estrutura aprovada pelo designer** (2026-11-23), com as decisões da seção 8. O 16–27 virou a fase 5 (`tasks/fase-5/`, T-500). O 26–40 é detalhado depois do playtest dela (T-560). Os monstros listados aqui são candidatos: cada tarefa de monstro traz uma proposta que o designer ainda pode trocar.

Base: `design/first-map-v0.1.md` (o mundo de hoje), `design/segunda-classe-v0.1.md` §3 (XP até o 40), `design/map-conventions-v0.1.md` e `design/art-direction-v0.1.md`.

## Resumo

| Ponto | Proposta |
|---|---|
| Por onde se chega | pelo **portão sul da Cidade**, fechado desde o primeiro mapa como "promessa de expansão" |
| Forma | uma **região contínua para o sul**, em faixas de nível, com um **segundo ponto seguro** (Forte do Passo) no meio, para não voltar à Cidade a cada ida |
| Faixas | Brejo 16–20 · Serra 21–25 · Minas 24–27 (masmorra) · Planalto 26–32 · Bastião 33–40 (com a masmorra final) |
| Para as segundas classes | grupos de monstros maiores e em fila, chefes de campo e masmorras de 4 com papel de tanque e de curador |
| Entrega | em duas fases: **16–27 primeiro** (Brejo, Forte, Serra, Minas), playtest, depois **26–40** |
| Arte | tileset novo por bioma no PixelLab, com amostra aprovada **dentro do jogo** antes de pintar o mapa; monstros no gerador LPC, como na T-056 |

---

## 1. Quanto conteúdo o 16–40 pede

Com a curva de hoje (XP para o próximo nível `68 × L^1,8`, monstro dá `6 × nível`), os monstros normais do próprio nível para subir são:

| Faixa | Monstros normais | Horas (aprox., `segunda-classe-v0.1.md` §3) |
|---|---|---|
| 1 → 15 (o mundo de hoje) | ~775 | ~9 h |
| 15 → 20 | ~546 | ~5 h |
| 20 → 25 | ~672 | ~8 h |
| 25 → 30 | ~791 | ~10 h |
| 30 → 35 | ~907 | ~10 h |
| 35 → 40 | ~1018 | ~12 h |
| **16 → 40** | **~3930** | **~45 h** |

Ou seja: **cinco vezes** o que o primeiro mapa oferece. Como os monstros renascem, o que limita não é a XP disponível, é **quanto tempo o jogador aguenta caçar no mesmo lugar**. Hoje cada área segura de 1 a 3 horas. A proposta tenta manter cada faixa em **um campo grande mais uma coisa que se repete bem** (masmorra, elite ou chefe de campo), em vez de dobrar o número de campos.

A queda de XP para quem está muito acima (piso de 40%, branch `fix/xp-monstros-abaixo`) ajuda: quem volta para ajudar um amigo ainda ganha alguma coisa.

## 2. Estrutura geral

```text
              [Floresta Baixa 5–8]
                      |
[Ruínas 9–11] -- [Cidade-Fortaleza] -- [Campos 1–4]
     |                |
[Cripta 12–15]   portão sul (abre)
                      |
              [Brejo dos Afogados 16–20]
                      |
              [Forte do Passo]  ← ponto seguro: loja, estalagem, mestres das classes
                /           \
   [Serra Partida 21–25]     [Planalto Queimado 26–32]
          |                           |
   [Minas Fundas 24–27]       [Bastião do Juramento 33–40]
     (masmorra, 4)                    |
                              [Salão do Juramento 38–40]
                                 (masmorra final, 4)
```

A Cidade continua sendo o centro do jogo até o 20. Do 20 em diante, o **Forte do Passo** faz esse papel: quem morre na Serra, nas Minas ou mais ao sul renasce nele (regra nova, seção 6).

**Fio da história:** o Vigia Sem Rosto guardava um **Juramento** (o Selo do Juramento já existe na Cripta). Com ele derrotado, o que o juramento prendia ao sul começa a se espalhar: os afogados do Brejo, a fuga dos mineiros da Serra, a guerra no Planalto. No fim está o Bastião, a fortaleza dos cavaleiros que fizeram o juramento.

## 3. As áreas

Cada área tem um **truque próprio** (uma coisa que o jogador aprende ali), como a Floresta ensinou veneno e emboscada e as Ruínas ensinaram prioridade de alvo.

### 3.1 Brejo dos Afogados — campo, 16–20

- **Função:** ponte entre o primeiro mapa e a segunda classe. Dá para jogar sozinho, com a classe base.
- **Sensação:** água rasa, juncos, palafitas abandonadas, névoa baixa. A muralha da Cidade some atrás.
- **Truque:** **água funda que deixa lento** (terreno). O jogador aprende a escolher onde lutar.
- **Subáreas:** Estrada dos Juncos (entrada, poucos monstros) · Aldeia das Palafitas (afogados em grupo) · Charco Fundo (água funda, serpentes) · Cabana da Bruxa (chefe de campo).
- **Monstros (candidatos):** Afogado (morto-vivo lento, em bando), Homem-lagarto do brejo (corpo a corpo rápido), Serpente do Charco (veneno, à distância), **Bruxa do Brejo** (chefe de campo nível 20, invoca afogados; renasce em 10 min).
- **Fim da faixa:** no 20 o jogador vira segunda classe. A Bruxa é o "teste" antes disso.

### 3.2 Forte do Passo — ponto seguro

- Posto militar pequeno no fim do Brejo, ao pé da Serra.
- Loja com itens e poções do 16 ao 30, estalagem (descanso, como a Cidade), portão para a Serra e para o Planalto.
- **Mestres das classes:** o design da segunda classe previa escolher falando com um mestre. A T-403 deixou isso "para quando os mapas forem refeitos". O Forte é o lugar natural (o jogador chega nele perto do 20). A escolha pela tela de habilidades pode continuar existindo junto (seção 8).

### 3.3 Serra Partida — campo, 21–25

- **Função:** primeira área de segunda classe. Corredores de rocha e trilhas em encosta.
- **Truque:** **monstros em fila** nos desfiladeiros (serve ao Atirador, que fura a linha, e ao Berserker) e **rolar pedras** como telegráfo grande de área.
- **Subáreas:** Desfiladeiro (corredores estreitos) · Acampamento dos Mineiros (fugitivos hostis) · Ninho das Águias/morcegos · Boca das Minas.
- **Monstros (candidatos):** Mineiro Possuído (humanoide), Lobo da Montanha (`ai.pack`, reaproveita o Lobo), Golem de Pedra (elite lento, resiste a físico), Morcego (enxame, `ai.swarm`).

### 3.4 Minas Fundas — masmorra instanciada, 24–27, 4 pessoas

- **Função:** primeira masmorra com **papel de grupo** de verdade: o chefe bate forte num só (pede Guardião) e solta dano em todos (pede Sacerdote).
- Corredores escuros, salas com grupos grandes (pede área: Piromante, Praga do Caçador), carrinhos de mina como cobertura.
- **Chefe (candidato):** o Capataz Forjado, golem de ferro nível 27, em fases como o Vigia.
- Repetível: é o "conteúdo que se repete bem" da faixa 21–27.

### 3.5 Planalto Queimado — campo grande, 26–32

- **Função:** a área mais longa. Campo aberto e queimado, acampamentos de guerra.
- **Truque:** **acampamentos em grupo grande** (6 a 10 monstros com funções: guerreiro, xamã que cura, arqueiro, lobo de montaria) e um **chefe de campo que anda** pelo mapa (o Chefe de Guerra, elite forte, renasce em 15 min).
- **Monstros (candidatos):** Guerreiro Orc, Xamã Orc (`ai.support`, como o Sacerdote Morto), Batedor Orc (`ai.ranged_kiter`), Warg (`ai.pack`).

### 3.6 Bastião do Juramento — campo, 33–40, e o Salão do Juramento (masmorra final)

- **Função:** fim do jogo. A fortaleza caída dos cavaleiros do Juramento.
- **Campo (33–37):** pátios, muralhas quebradas, capela. Cavaleiros corrompidos (elites comuns aqui), escudeiros, cultistas.
- **Salão do Juramento (38–40, 4 pessoas):** masmorra final, chefe nível 40. É o encontro mais difícil do jogo e a referência de equipamento do 40.
- **Truque:** **elites em todo grupo** e telegráfos combinados (dois monstros atacando o mesmo chão).

## 4. O que as segundas classes pedem dos mapas

| Classe | Pede do mapa |
|---|---|
| Guardião | chefes e elites que batem forte num só; masmorras de 4 |
| Berserker, Piromante | grupos grandes de monstros que se juntam |
| Atirador de Elite | corredores onde os monstros andam em fila |
| Caçador | grupos que ficam perto (a Praga pula de um para outro); monstros que andam (Sangramento) |
| Arcanista | lutas longas contra alvo único (elites, chefes) |
| Sacerdote, Inquisidor | dano em todo o grupo, para a cura importar |

Os campos de hoje têm grupos de 2 a 4 monstros. Do 21 em diante a proposta é ter **grupos de 5 a 10** em parte dos pontos, com espaço para lutar.

## 5. Arte

- **Chão e paredes:** um tileset novo por bioma (brejo, serra, mina, planalto, fortaleza) no PixelLab, como os de hoje (`data/maps/tilesets/pixellab_*`). Uma amostra de cada bioma é aprovada **dentro do jogo**, antes de pintar o mapa inteiro.
- **Monstros:** gerador LPC, como na T-056 (afogado, homem-lagarto, orcs, mineiros e cavaleiros cabem nos corpos do gerador). O que o gerador não tem (serpente, golem, morcego) vira encomenda ou pacote do OpenGameArt, como o Ent. **Verificar no gerador antes de fechar a lista de monstros**: se uma criatura não tiver arte boa, troca-se a criatura, não o estilo.
- Estilo simples, como pede a direção de arte: primeiro uma amostra, depois a animação.

## 6. Peças novas de sistema

| Peça | Para quê | Onde |
|---|---|---|
| Terreno que deixa lento | água funda do Brejo | Core (camada ou área no Tiled) |
| Renascer no ponto seguro mais próximo | morrer no 21+ volta ao Forte, não à Cidade | servidor (zona tem um "ponto de renascimento") |
| Chefe de campo com tempo de renascimento longo e anúncio | Bruxa, Chefe de Guerra | servidor e dados |
| NPC mestre da classe | escolher a segunda classe no Forte | servidor, cliente e mapa |
| Monstro que anda por uma rota | Chefe de Guerra no Planalto | Core (IA) |
| Meta de XP por hora por área | o simulador mede, como já faz para o ritmo do 5 ao 8 | simulador e testes de design |

As mecânicas de cada monstro novo saem quando a lista de monstros for aprovada, como na T-340/T-341.

## 7. Ordem de entrega

**Fase A — 16 a 27** (Brejo, Forte do Passo, Serra Partida, Minas Fundas). Mesma ordem da fase 3: peças de sistema e monstros no Core antes, depois os mapas, depois o playtest. Cobre a chegada na segunda classe e a primeira masmorra de grupo, que é o que os playtests das classes ainda não testaram.

**Fase B — 26 a 40** (Planalto, Bastião, Salão), detalhada depois do playtest da fase A.

Hoje a "Fase 5" do `tasks/README.md` é a Operação. A proposta é que as fases A e B sejam a 5 e a 6, e a Operação passe a 7 (como já aconteceu quando a segunda classe entrou na frente dela). A Operação também pode correr em paralelo, porque não toca nos mapas.

## 8. Decisões do designer

Em 2026-11-23 o designer aprovou o esboço como estava ("Aprovo, detalhe"). Nos pontos em que o esboço não tinha uma proposta só, a T-500 ficou com a opção mais simples:

| # | Pergunta | Decisão |
|---|---|---|
| 1 | Forma | **região contínua pelo portão sul**, com o Forte do Passo no meio |
| 2 | Temas | **aprovados**: brejo, serra e minas, planalto de guerra, fortaleza do Juramento |
| 3 | Curva de XP | **mantida** (~45 h do 16 ao 40), como na T-390. A T-505 mede a XP por hora de cada área. O playtest da fase 5 (T-550) diz se ela precisa mudar |
| 4 | Entrega | **16–27 primeiro** (fase 5), playtest, depois 26–40 (fase 6) |
| 5 | Mestres das classes | **as duas formas**: o mestre (no Forte e no Salão de Treinamento da Cidade) abre a escolha, e a tela de habilidades continua valendo (T-541) |
| 6 | Renascimento | **no Forte do Passo**, com a regra **por zona** em vez de por nível (T-501): Serra e Minas renascem no Forte, e o Brejo, que fica entre a Cidade e o Forte, renasce na Cidade |
