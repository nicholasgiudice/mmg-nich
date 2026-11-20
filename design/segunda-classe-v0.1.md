# Níveis 16–30 e segunda classe — esboço v0.1

Status: **esboço para discussão com o designer**, entrada para a T-390. Nada aqui está aprovado nem em tarefa. Pedido do designer em 2026-11-19, ao fechar o playtest v0.3: **mais níveis** e uma **segunda classe**, com **duas opções por classe**.

Os números são ponto de partida para o simulador, como em `design/progression-v0.1.md`. As perguntas para o designer estão na seção 6.

## 1. Resumo da proposta

- **Nível máximo: 30** (hoje 15).
- **Segunda classe no nível 15.** O mapa atual (Campos → Cripta, níveis 1–15) vira a jornada da classe base. Do 16 ao 30 o jogador já está na segunda classe.
- **Cada segunda classe traz 4 habilidades:** 3 normais com 5 NH e 1 assinatura com 3 NH, no mesmo formato da classe base. As 5 habilidades da classe base continuam com o jogador.
- **Pontos de habilidade:** continua 1 por nível, então são 30 no nível 30, para 23 + 18 = **41 NH possíveis**. A escolha continua: hoje são 15 pontos para 23 NH (65%), e passam a ser 30 para 41 (73%).
- **8 segundas classes**, duas por classe base (seção 4).

## 2. Níveis e XP

### Curva

Mantendo a fórmula atual (`68 × L^1,8`) e a XP por monstro (`6 × nível do monstro`):

| Nível | XP para o próximo | XP total | Monstros normais do próprio nível | Monstros acumulados |
|---|---|---|---|---|
| 15 | 8 902 | 52 227 | ~99 | ~870 |
| 18 | 12 359 | 85 735 | ~114 | ~1 200 |
| 20 | 14 940 | 114 298 | ~124 | ~1 450 |
| 22 | 17 737 | 148 347 | ~134 | ~1 710 |
| 25 | 22 325 | 210 630 | ~149 | ~2 140 |
| 29 | 29 163 | 316 772 | ~168 | ~2 780 |

Do 1 ao 15 são ~775 monstros, umas 8 a 9 horas. Do 15 ao 30 são mais ~2 000 monstros, cerca de **2,6 vezes** o caminho até o 15. Com o mesmo ritmo, dá umas **20 a 23 horas a mais**, ou seja, ~30 horas até o nível máximo, sem contar elites, chefes e masmorras, que encurtam.

Se isso for longo demais, o ajuste mais simples é baixar o expoente só a partir do 15 (uma segunda faixa na curva).

### Faixas de nível do segundo mapa

O conteúdo de 16 a 30 não existe: precisa de um **segundo mapa**. Sugestão de faixas, no mesmo formato do primeiro:

| Área | Níveis | Tipo |
|---|---|---|
| Área aberta 1 | 16–19 | campo, solo |
| Área aberta 2 | 20–23 | campo, solo e grupo, com chefe de campo |
| Área aberta 3 | 24–27 | campo, mais perigoso |
| Masmorra de grupo | 28–30 | instância, chefe nível 30 |

### Regras que mudam com mais níveis

- `progression.maxLevel`: 15 → 30.
- **Nível exigido por NH na segunda classe:** mesma regra da base (`2n − 1`), deslocada em 15. As normais liberam NH1 a NH5 nos níveis 16, 18, 20, 22 e 24, e a assinatura nos níveis 26, 28 e 30. A classe base não muda.
- **Pontos de atributo:** 5 por nível dão 145 no 30. Hoje a velocidade de ataque bate no máximo com AGI 100, e o crítico no máximo com AGI 140. Rever esses tetos ou o valor por ponto.
- **Monstros:** a escala por nível (`healthPerLevel 0,4`, `powerPerLevel 0,17`) é linear e nunca foi testada acima do 15. Precisa passar pelo simulador.
- **Itens:** as fórmulas por nível do item já vão até qualquer nível. Rever se a armadura alcança o teto de 60% cedo demais.

## 3. Como a segunda classe funciona

- **Quando:** a partir do nível 15, falando com um mestre da classe na Cidade. Não exige derrotar o Vigia: o Vigia é conteúdo de grupo, e a classe base precisa poder evoluir jogando sozinha.
- **Pontos:** a partir do 16, os pontos de habilidade podem ir para qualquer habilidade, da base ou da segunda classe.
- **Barra:** hoje é ataque básico + 5 teclas + poção. Com a segunda classe, são 9 habilidades. **Sugestão: barra de 9 teclas** (1–5 da base, 6–9 da segunda classe). É o caminho mais simples, sem tela para montar a barra. A alternativa é escolher 5 das 9 na Cidade, mais estratégica, mas com mais interface.
- **Trocar de segunda classe:** sugestão, na Cidade, com custo em ouro, como a redistribuição de pontos.
- **Regra de design (`classes-v0.1.md`):** a segunda classe deve **mudar o estilo de jogo**, não só os números. Por isso cada uma tem um papel diferente da outra da mesma base.

## 4. As 8 segundas classes

Nomes e habilidades são sugestões. As habilidades guardadas em "Depois do MVP" (`classes-v0.1.md`) foram aproveitadas e estão marcadas com ★.

### Guerreiro

| | **Guardião** | **Berserker** |
|---|---|---|
| Papel | proteger o grupo, segurar chefes | dano corpo a corpo alto, defesa baixa |
| Normal 1 | ★ Interceptação: corre até um aliado e recebe parte do dano dele | ★ Investida: avança até o alvo e o atordoa |
| Normal 2 | Golpe de Escudo: atordoa e gera muita ameaça | Fúria: mais dano e mais velocidade, recebendo mais dano |
| Normal 3 | Bastião: área que reduz o dano dos aliados por perto | Execução: dano extra em alvo com pouca vida |
| Assinatura | Juramento: por alguns segundos, recebe o dano dos aliados por perto, com redução | Tempestade de Aço: gira sem parar enquanto anda |

### Arqueiro

| | **Atirador** | **Patrulheiro** |
|---|---|---|
| Papel | alvo único, longa distância, precisão | controle de área, armadilhas e um companheiro |
| Normal 1 | ★ Flecha Perfurante: atravessa todos os monstros em linha | ★ Armadilha de Contenção: prende quem pisa (já está nos dados) |
| Normal 2 | ★ Tiro Debilitante: deixa o alvo lento e com menos dano | Armadilha Explosiva: dano em área quando ativada |
| Normal 3 | Mira Firme: parado, ganha alcance e crítico; andar cancela | Companheiro Lobo: invoca um lobo que segue e ataca |
| Assinatura | Olho de Falcão: por alguns segundos, todo ataque é crítico | Matilha: invoca vários lobos por pouco tempo |

### Mago

| | **Piromante** | **Arcanista** |
|---|---|---|
| Papel | dano em área, queimadura | controle e suporte arcano |
| Normal 1 | ★ Combustão: detona as queimaduras dos monstros por perto | ★ Dissipar Magia: tira escudos e reforços dos monstros |
| Normal 2 | Rajada de Chamas: cone curto de fogo que queima | Piscar: teleporte curto |
| Normal 3 | Chão em Chamas: área que queima quem fica nela | Lentidão Temporal: área que deixa os monstros lentos |
| Assinatura | Meteoro: grande área com aviso no chão e dano muito alto | Parar o Tempo: congela os monstros normais na área; elites e chefes só ficam lentos |

### Clérigo

| | **Sacerdote** | **Inquisidor** |
|---|---|---|
| Papel | cura do grupo | dano sagrado corpo a corpo, com cura menor |
| Normal 1 | ★ Oração Restauradora: cura em área ao longo do tempo | Martelo da Justiça: dano sagrado e atordoamento |
| Normal 2 | ★ Bênção: reforço nos atributos de um aliado | Consagração: área no chão que fere mortos-vivos e cura aliados |
| Normal 3 | Elo da Vida: liga dois aliados, que dividem o dano | Selo da Luz: quem ataca o alvo marcado se cura |
| Assinatura | Ressurreição: revive um aliado caído | Ira Divina: por alguns segundos, os ataques básicos viram área sagrada que cura |

### Papéis no grupo

- **Proteção:** Guardião. O Inquisidor e o Berserker aguentam um pouco, mas não seguram chefe.
- **Cura:** Sacerdote em primeiro lugar. O Inquisidor cura pouco, e as poções cobrem quem joga sem cura.
- **Controle:** Arcanista e Patrulheiro.
- **Dano:** Berserker, Atirador e Piromante.

Nenhuma segunda classe é obrigatória, e cada uma ainda joga sozinha, como manda `classes-v0.1.md`.

## 5. Custo e mecânicas novas no Core

São **32 habilidades novas**, das quais 9 já tinham ideia guardada e 1 já está nos dados (Armadilha de Contenção). Cada uma pede dados, regras no Core, teste, ícone e efeito visual.

Mecânicas que **não existem hoje** e pesam mais:

| Mecânica | Quem usa | Observação |
|---|---|---|
| Invocação controlada pelo jogador | Patrulheiro | o Core já invoca monstros (Matriarca), mas um companheiro do jogador precisa de IA que siga e ajude |
| Reviver aliado em combate | Sacerdote | hoje a morte leva à tela de "Retornar à Cidade" (T-397) |
| Redirecionar dano de um aliado | Guardião, Sacerdote (Elo da Vida) | evento de dano com troca de alvo |
| Teleporte | Arcanista | colisão e validação no Core |
| Barra com mais de 5 teclas | todas | cliente e protocolo |

Se alguma pesar demais, troca-se por uma habilidade do mesmo papel que use mecânicas existentes.

## 6. Decisões para o designer

1. Nível máximo **30**? Curva igual até lá (~30 h no total) ou mais rápida depois do 15?
2. Segunda classe no **nível 15**, sem exigir o Vigia?
3. Barra de **9 teclas** ou escolher 5 de 9?
4. Os 8 papéis e nomes da seção 4: algum troca?
5. Pode trocar de segunda classe? Com que custo?
6. Ordem de entrega: as 8 segundas classes juntas, ou **uma classe base primeiro** (as duas segundas classes dela) para validar o sistema e depois as outras?

## 7. Para a T-390

- A fase 4 hoje é infraestrutura (CI/CD, observabilidade, testes de carga, produção separada). Mais níveis, segunda classe e o segundo mapa são **conteúdo grande**: cabem melhor numa fase própria, ou como trilha paralela, do que espremidos na fase 4.
- Ordem natural: (1) regras de nível até 30 e simulador; (2) sistema de segunda classe no Core, com uma classe base de exemplo; (3) as demais segundas classes; (4) o segundo mapa, com monstros e masmorra; (5) cliente: barra, tela de escolha e efeitos.
- Junto com isso, rever as metas dos testes de design e reativar os 19 casos marcados com `PlaytestRebalance.Pending` (`tasks/reports/playtest-v0.3.md`).
