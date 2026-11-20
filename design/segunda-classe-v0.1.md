# Níveis até o 50 e segunda classe — esboço v0.1

Status: **esboço em discussão com o designer**, entrada para a T-390. Pedido do designer em 2026-11-19, ao fechar o playtest v0.3: **mais níveis** e uma **segunda classe**, com **duas opções por classe**.

Os números são ponto de partida para o simulador, como em `design/progression-v0.1.md`. O que ainda falta decidir está na seção 7.

## 1. Decisões do designer (2026-11-19)

| Assunto | Decisão |
|---|---|
| Nível máximo | **50** |
| Barra | **9 teclas**: 1–5 da classe base, 6–9 da segunda classe, mais ataque básico e poção |
| Guerreiro | Guardião e Berserker aprovados |
| Arqueiro | **Atirador de Elite** (alvo único) aprovado; o Patrulheiro foi **recusado**, falta escolher outra opção |
| Mago | **Piromante** (dano em área) e **Arcanista**, que passa a ser **dano constante em alvo único** |
| Clérigo | Sacerdote e Inquisidor aprovados |
| Trocar de segunda classe | **não pode**: a escolha é definitiva |
| Ordem de entrega | **uma classe base primeiro**, com as duas segundas classes dela, para validar o sistema |

## 2. Níveis de habilidade (NH) até o 50

Com a barra de 9 teclas, cada personagem tem **7 habilidades normais** (4 da base e 3 da segunda classe) e **2 assinaturas**.

### O que existe hoje não basta

Com 5 NH nas normais e 3 nas assinaturas, são 7 × 5 + 2 × 3 = **41 NH**. Com 1 ponto por nível, o personagem tem 50 pontos no nível 50. Ele completa todas as habilidades no **nível 41** e não ganha mais nada do 42 ao 50. Também acaba a escolha: todo mundo termina com a mesma build.

Hoje são 15 pontos para 23 NH, ou seja, dá para ter 65% de tudo. Para manter a escolha, o nível 50 deve ficar perto disso.

### Opções

| Opção | NH possíveis | Pontos no 50 | Quanto dá para ter | Problema |
|---|---|---|---|---|
| A. Nada muda | 41 | 50 | tudo, desde o 41 | sem escolha e sem progresso no fim |
| B. **NH6 e NH7 nas normais**, e pontos mais lentos depois do 30 | 55 | 40 | **73%** | mais números para balancear |
| C. NH continua até 5, e pontos mais lentos depois do 15 | 41 | 33 | 80% | nada novo para liberar depois do 30 |

**Recomendação: opção B.**

- **NH6 e NH7** em todas as habilidades normais, da base e da segunda classe. Multiplicador × 2,0 e × 2,2, seguindo a escada atual (× 1,0 a × 1,8). A assinatura continua com 3 NH.
- **Pontos de habilidade:** 1 por nível até o 30 e, depois, 1 a cada 2 níveis (nos pares), o que dá **40 pontos no nível 50** para 55 NH.
- Os pontos de atributo continuam 5 por nível, então todo nível ainda dá alguma coisa.

### Quando cada NH libera (opção B)

A classe base não muda até o NH5. O resto se espalha para que algo novo libere a cada 3 ou 4 níveis até o 50:

| Nível | O que libera |
|---|---|
| 1, 3, 5, 7, 9 | NH1–NH5 das normais da base (como hoje) |
| 12, 13, 15 | NH1–NH3 da assinatura da base (como hoje) |
| 15 | escolha da segunda classe |
| 16, 19, 22, 25, 28 | NH1–NH5 das normais da segunda classe |
| 30, 40, 50 | NH1–NH3 da assinatura da segunda classe |
| 34, 44 | NH6 e NH7 das normais da base |
| 37, 47 | NH6 e NH7 das normais da segunda classe |

No nível 30, o personagem tem 30 pontos para 39 NH liberados. No 50, tem 40 pontos para 55.

## 3. XP até o 50

Mantendo a fórmula atual (`68 × L^1,8`) e a XP por monstro (`6 × nível do monstro`):

| Nível | XP para o próximo | XP total | Monstros normais do próprio nível | Monstros acumulados |
|---|---|---|---|---|
| 15 | 8 902 | 52 227 | ~99 | ~870 |
| 20 | 14 940 | 114 298 | ~124 | ~1 450 |
| 30 | 30 998 | 347 770 | ~172 | ~2 950 |
| 40 | 52 026 | 769 430 | ~217 | ~4 900 |
| 49 | 74 966 | 1 349 609 | ~255 | ~7 070 |

Do 1 ao 15 são ~775 monstros, umas 8 a 9 horas. Até o 50 seriam **~7 000 monstros**, cerca de 9 vezes mais, algo como **75 a 80 horas**, sem contar elites, chefes e masmorras, que encurtam. A curva precisa de uma decisão própria (seção 7), depois de saber quanto conteúdo vai existir.

### Faixas de nível

Do 16 ao 50 não existe conteúdo. São 35 níveis, mais que o dobro do primeiro mapa (15 níveis com 4 áreas e 2 chefes). No mesmo ritmo, isso pede **dois ou três mapas novos**. Isso deve ser planejado à parte.

### Regras que mudam com mais níveis

- `progression.maxLevel`: 15 → 50, `skillRankLevels` e `signatureRankLevels` separados por classe base e segunda classe, e pontos de habilidade com regra por faixa de nível (hoje é um número fixo por nível).
- **Pontos de atributo:** 5 por nível dão 245 no 50. Hoje a velocidade de ataque bate no máximo com AGI 100, e o crítico no máximo com AGI 140. Rever esses tetos ou o valor por ponto.
- **Monstros:** a escala por nível (`healthPerLevel 0,4`, `powerPerLevel 0,17`) é linear e nunca foi testada acima do 15. Precisa passar pelo simulador.
- **Itens e defesa:** as fórmulas por nível do item vão até qualquer nível. Rever se a armadura chega ao teto de 60% cedo demais.

## 4. Como a segunda classe funciona

- **Quando:** a partir do nível 15, falando com um mestre da classe na Cidade, sem exigir o Vigia (a confirmar, seção 7).
- **Escolha definitiva:** não dá para trocar. A tela de escolha precisa deixar isso claro e pedir confirmação.
- **Pontos:** a partir do 16, os pontos podem ir para qualquer habilidade, da base ou da segunda classe.
- **Barra:** 9 teclas. As teclas 6–9 aparecem vazias até a segunda classe.

## 5. As segundas classes

Habilidades guardadas em "Depois do MVP" (`classes-v0.1.md`) estão marcadas com ★.

### Guerreiro (aprovado)

| | **Guardião** | **Berserker** |
|---|---|---|
| Papel | proteger o grupo, segurar chefes | dano corpo a corpo alto, defesa baixa |
| Normal 1 | ★ Interceptação: corre até um aliado e recebe parte do dano dele | ★ Investida: avança até o alvo e o atordoa |
| Normal 2 | Golpe de Escudo: atordoa e gera muita ameaça | Fúria: mais dano e mais velocidade, recebendo mais dano |
| Normal 3 | Bastião: área que reduz o dano dos aliados por perto | Execução: dano extra em alvo com pouca vida |
| Assinatura | Juramento: por alguns segundos, recebe o dano dos aliados por perto, com redução | Tempestade de Aço: gira sem parar enquanto anda |

### Arqueiro

**Atirador de Elite (aprovado):** dano alto em um alvo, de longe, em rajadas e críticos.

| | **Atirador de Elite** |
|---|---|
| Normal 1 | ★ Flecha Perfurante: tiro forte que atravessa o alvo e acerta quem está atrás |
| Normal 2 | ★ Tiro Debilitante: deixa o alvo lento e com menos dano |
| Normal 3 | Mira Firme: parado, ganha alcance e crítico; andar cancela |
| Assinatura | Olho de Falcão: por alguns segundos, todo ataque é crítico |

**Segunda opção (a escolher).** Como o Atirador de Elite é alvo único, a outra opção deve jogar de outro jeito:

| Opção | Papel | Exemplos de habilidade |
|---|---|---|
| **Batedor** | mobilidade: atira correndo, esquiva, dano enquanto se move | Disparo em Movimento, Rolamento, Flechas Rápidas; assinatura: Dança das Flechas (atira em volta enquanto corre) |
| **Caçador** | venenos e sangramento em vários alvos, dano ao longo do tempo | Flecha Envenenada, Ferida Aberta, Nuvem Tóxica; assinatura: Praga (o veneno pula de monstro em monstro) |
| **Arqueiro de Guerra** | saraivadas em área e apoio ao grupo | Saraivada, Flecha Incendiária, Estandarte (área que aumenta o dano dos aliados); assinatura: Céu de Flechas |

### Mago

| | **Piromante** (aprovado) | **Arcanista** (redesenhado, a aprovar) |
|---|---|---|
| Papel | dano em área, queimadura | dano constante em alvo único: o dano cresce enquanto ele mantém o mesmo alvo |
| Normal 1 | ★ Combustão: detona as queimaduras dos monstros por perto | Raio Arcano: canalizado, o dano sobe a cada segundo no mesmo alvo |
| Normal 2 | Rajada de Chamas: cone curto de fogo que queima | Marca Arcana: dano ao longo do tempo que acumula até 5 vezes |
| Normal 3 | Chão em Chamas: área que queima quem fica nela | Orbes Arcanos: 3 orbes que disparam sozinhos no alvo atual |
| Assinatura | Meteoro: grande área com aviso no chão e dano muito alto | Feixe do Vazio: canalização longa com dano muito alto num alvo; anda devagar enquanto canaliza |

Diferença entre o Arcanista e o Atirador de Elite: os dois são de alvo único, mas o Atirador é **rajada** (dano alto de uma vez, críticos) e o Arcanista é **constante** (dano que cresce com o tempo, perde força se trocar de alvo). Prisão de Gelo, da base, segue como o controle do Mago. Dissipar Magia (★) fica guardada.

### Clérigo (aprovado)

| | **Sacerdote** | **Inquisidor** |
|---|---|---|
| Papel | cura do grupo | dano sagrado corpo a corpo, com cura menor |
| Normal 1 | ★ Oração Restauradora: cura em área ao longo do tempo | Martelo da Justiça: dano sagrado e atordoamento |
| Normal 2 | ★ Bênção: reforço nos atributos de um aliado | Consagração: área no chão que fere mortos-vivos e cura aliados |
| Normal 3 | Elo da Vida: liga dois aliados, que dividem o dano | Selo da Luz: quem ataca o alvo marcado se cura |
| Assinatura | Ressurreição: revive um aliado caído | Ira Divina: por alguns segundos, os ataques básicos viram área sagrada que cura |

## 6. Custo e mecânicas novas no Core

São **32 habilidades novas**, cada uma com dados, regras no Core, teste, ícone e efeito visual.

Mecânicas que **não existem hoje**:

| Mecânica | Quem usa |
|---|---|
| Redirecionar dano de um aliado | Guardião (Interceptação, Juramento), Sacerdote (Elo da Vida) |
| Reviver aliado em combate | Sacerdote (Ressurreição) |
| Dano que cresce enquanto mantém o alvo | Arcanista |
| Ataque básico que vira área | Inquisidor (Ira Divina) |
| Barra com 9 teclas, tela de escolha da segunda classe | todas |

### Qual classe primeiro

**Recomendação: Guerreiro.** As duas opções já estão aprovadas, cobrem papéis opostos (proteção e dano) e testam o sistema inteiro: escolha definitiva, NH da segunda classe, barra de 9 teclas. O Berserker usa quase só mecânicas que já existem. O Guardião traz a mecânica nova mais útil para as outras classes (redirecionar dano, que o Sacerdote também usa).

## 7. Ainda falta decidir

1. **NH até o 50:** opção B (NH6 e NH7, pontos mais lentos depois do 30)?
2. **Segunda opção do Arqueiro:** Batedor, Caçador, Arqueiro de Guerra, ou outra ideia?
3. **Arcanista** como descrito na seção 5?
4. **Segunda classe no nível 15**, sem exigir o Vigia?
5. **Curva de XP até o 50:** fica igual (~75–80 h) ou fica mais rápida? Depende de quantos mapas novos vão existir.
6. **Primeira classe a entregar:** Guerreiro?

## 8. Para a T-390

- A fase 4 hoje é infraestrutura (CI/CD, observabilidade, testes de carga, produção separada). Níveis até o 50, segunda classe e os mapas novos são **conteúdo grande**: cabem melhor numa fase própria, ou numa trilha paralela, do que dentro da fase 4.
- Ordem natural: (1) regras de nível até o 50 e simulador; (2) sistema de segunda classe no Core e no cliente (escolha, barra de 9 teclas) com a primeira classe base; (3) as outras três classes; (4) os mapas novos, com monstros e masmorras.
- Junto com isso, rever as metas dos testes de design e reativar os 19 casos marcados com `PlaytestRebalance.Pending` (`tasks/reports/playtest-v0.3.md`).
