# Nível máximo e segunda classe — esboço v0.1

Status: **esboço em discussão com o designer**, entrada para a T-390. Pedido do designer em 2026-11-19, ao fechar o playtest v0.3: **mais níveis** e uma **segunda classe**, com **duas opções por classe**.

Os números são ponto de partida para o simulador, como em `design/progression-v0.1.md`. O que ainda falta decidir está na seção 7.

## 1. Decisões do designer (2026-11-19)

| Assunto | Decisão |
|---|---|
| Segunda classe | **4 habilidades normais e 1 assinatura**, o mesmo formato da classe base |
| Barra | uma tecla por habilidade: com 4 normais na segunda classe, passa de 9 para **10 teclas** (1–5 da base, 6–0 da segunda classe), mais ataque básico e poção |
| Nível máximo | **40**, mas o designer ainda tem dúvida e não queria aumentar muito o número de níveis (seção 2 compara 30, 35 e 40) |
| Curva de XP | **mais fácil** do que a atual |
| Guerreiro | Guardião e Berserker |
| Arqueiro | **Atirador de Elite** (alvo único) e **Caçador** |
| Mago | **Piromante** (dano em área) e **Arcanista** (dano constante em alvo único) |
| Clérigo | Sacerdote e Inquisidor |
| Trocar de segunda classe | **não pode**: a escolha é definitiva |
| Primeira classe a entregar | **Guerreiro**, com as duas segundas classes dele, para validar o sistema |

## 2. Nível máximo e níveis de habilidade (NH)

Com a segunda classe no mesmo formato da base, cada personagem tem **8 habilidades normais** com 5 NH e **2 assinaturas** com 3 NH: 8 × 5 + 2 × 3 = **46 NH**. Não é preciso criar NH6 ou NH7.

Hoje são 15 pontos para 23 NH: dá para ter **65%** de tudo, e é isso que obriga a escolher onde investir. Com 1 ponto por nível:

| Nível máximo | Pontos | Quanto dá para ter dos 46 NH | Conteúdo novo a criar |
|---|---|---|---|
| **30** | 30 | **65%**, a mesma proporção de hoje | 15 níveis: **um mapa** do tamanho do primeiro |
| 35 | 35 | 76% | 20 níveis: um mapa e meio |
| 40 | 40 | 87%: quase tudo no máximo, sobra pouca escolha | 25 níveis: **quase dois mapas** |

Se o nível 40 ficar, a escolha pode ser mantida com **1 ponto por nível até o 30 e, depois, 1 a cada 2 níveis**: 35 pontos no 40, ou 76%.

**Leitura:** como o designer não queria aumentar muito o número de níveis, o **nível 30** é o que muda menos: dobra a jornada atual, mantém a mesma proporção de escolha com a regra de pontos de hoje e pede um mapa novo, não dois.

### Quando cada NH libera

A classe base não muda: NH1–NH5 das normais nos níveis 1, 3, 5, 7 e 9, e a assinatura no 12, 13 e 15. A segunda classe é escolhida no 15 e repete o mesmo desenho nos níveis seguintes:

| Segunda classe | Nível máximo 30 | Nível máximo 40 |
|---|---|---|
| NH1–NH5 das normais | 16, 18, 20, 22, 24 | 16, 19, 22, 25, 28 |
| NH1–NH3 da assinatura | 27, 28, 30 | 30, 35, 40 |

## 3. Curva de XP mais fácil

Hoje a XP para o próximo nível é `68 × L^1,8`, e o número de monstros por nível sobe sem parar (~11 no nível 1, ~99 no 15, ~172 no 30, ~217 no 40).

**Proposta:** manter a curva atual até o 15 (a jornada que o playtest aprovou) e, do 15 em diante, **segurar o número de monstros por nível em ~100**, o mesmo do nível 15. Na fórmula: XP para o próximo = `600 × L` a partir do 15 (no 15 dá 9 000, quase igual aos 8 902 de hoje, então não há degrau).

| Nível máximo | Monstros até o máximo, curva atual | Com a proposta | Tempo aproximado com a proposta |
|---|---|---|---|
| 30 | ~2 950 | ~2 300 | ~25 h |
| 35 | ~3 900 | ~2 800 | ~30 h |
| 40 | ~4 900 | ~3 300 | ~36 h |

Base: do 1 ao 15 são ~775 monstros em 8 a 9 horas. Elites, chefes, masmorras e grupo encurtam o tempo. Se ainda ficar longo, o próximo ajuste é baixar os ~100 monstros por nível (por exemplo, `480 × L` dá ~80).

### Outras regras que mudam com mais níveis

- `progression.maxLevel`, e `skillRankLevels` e `signatureRankLevels` separados para a classe base e a segunda classe.
- **Pontos de atributo:** 5 por nível dão 145 no 30 e 195 no 40. Hoje a velocidade de ataque bate no máximo com AGI 100, e o crítico no máximo com AGI 140. Rever esses tetos ou o valor por ponto.
- **Monstros:** a escala por nível (`healthPerLevel 0,4`, `powerPerLevel 0,17`) é linear e nunca foi testada acima do 15. Precisa passar pelo simulador.
- **Itens e defesa:** as fórmulas por nível do item vão até qualquer nível. Rever se a armadura chega ao teto de 60% cedo demais.

## 4. Como a segunda classe funciona

- **Quando:** a partir do nível 15, falando com um mestre da classe na Cidade, sem exigir o Vigia (a confirmar, seção 7).
- **Escolha definitiva:** não dá para trocar. A tela de escolha precisa deixar isso claro e pedir confirmação.
- **Pontos:** a partir do 16, os pontos podem ir para qualquer habilidade, da base ou da segunda classe.
- **Barra:** 10 teclas. As teclas 6–0 aparecem vazias até a segunda classe.

## 5. As segundas classes

Habilidades guardadas em "Depois do MVP" (`classes-v0.1.md`) estão marcadas com ★. A **quarta habilidade normal** de cada classe é nova neste esboço e precisa de aprovação.

### Guerreiro (primeira a entregar)

| | **Guardião** | **Berserker** |
|---|---|---|
| Papel | proteger o grupo, segurar chefes | dano corpo a corpo alto, defesa baixa |
| Normal 1 | ★ Interceptação: corre até um aliado e recebe parte do dano dele | ★ Investida: avança até o alvo e o atordoa |
| Normal 2 | Golpe de Escudo: atordoa e gera muita ameaça | Fúria: mais dano e mais velocidade, recebendo mais dano |
| Normal 3 | Bastião: área que reduz o dano dos aliados por perto | Execução: dano extra em alvo com pouca vida |
| Normal 4 (nova) | Escudo Refletor: bloqueia por pouco tempo e devolve parte do dano | Sede de Sangue: por alguns segundos, os golpes curam parte do dano causado |
| Assinatura | Juramento: por alguns segundos, recebe o dano dos aliados por perto, com redução | Tempestade de Aço: gira sem parar enquanto anda |

### Arqueiro

| | **Atirador de Elite** | **Caçador** |
|---|---|---|
| Papel | alvo único, de longe, rajadas e críticos | venenos e sangramento em vários alvos, dano ao longo do tempo |
| Normal 1 | ★ Flecha Perfurante: tiro forte que atravessa o alvo e acerta quem está atrás | Flecha Envenenada: veneno que acumula no alvo |
| Normal 2 | ★ Tiro Debilitante: deixa o alvo lento e com menos dano | Ferida Aberta: sangramento forte, maior se o alvo se mexer |
| Normal 3 | Mira Firme: parado, ganha alcance e crítico; andar cancela | Nuvem Tóxica: área que envenena quem fica nela |
| Normal 4 (nova) | Tiro Certeiro: preparação de 1,5 s, dano muito alto, ignora parte da armadura | Flecha Dispersa: copia os venenos do alvo para os monstros por perto |
| Assinatura | Olho de Falcão: por alguns segundos, todo ataque é crítico | Praga: por alguns segundos, o veneno que acaba num monstro pula para os vizinhos |

### Mago

| | **Piromante** | **Arcanista** |
|---|---|---|
| Papel | dano em área, queimadura | dano constante em alvo único: o dano cresce enquanto ele mantém o mesmo alvo |
| Normal 1 | ★ Combustão: detona as queimaduras dos monstros por perto | Raio Arcano: canalizado, o dano sobe a cada segundo no mesmo alvo |
| Normal 2 | Rajada de Chamas: cone curto de fogo que queima | Marca Arcana: dano ao longo do tempo que acumula até 5 vezes |
| Normal 3 | Chão em Chamas: área que queima quem fica nela | Orbes Arcanos: 3 orbes que disparam sozinhos no alvo atual |
| Normal 4 (nova) | Bola de Fogo: projétil que explode numa área pequena e queima | Sifão Arcano: dano no alvo que devolve Mana |
| Assinatura | Meteoro: grande área com aviso no chão e dano muito alto | Feixe do Vazio: canalização longa com dano muito alto num alvo; anda devagar enquanto canaliza |

O Arcanista e o Atirador de Elite são de alvo único, mas o Atirador é **rajada** (dano alto de uma vez) e o Arcanista é **constante** (dano que cresce com o tempo e perde força se trocar de alvo).

### Clérigo

| | **Sacerdote** | **Inquisidor** |
|---|---|---|
| Papel | cura do grupo | dano sagrado corpo a corpo, com cura menor |
| Normal 1 | ★ Oração Restauradora: cura em área ao longo do tempo | Martelo da Justiça: dano sagrado e atordoamento |
| Normal 2 | ★ Bênção: reforço nos atributos de um aliado | Consagração: área no chão que fere mortos-vivos e cura aliados |
| Normal 3 | Elo da Vida: liga dois aliados, que dividem o dano | Selo da Luz: quem ataca o alvo marcado se cura |
| Normal 4 (nova) | Renovar: cura ao longo do tempo num aliado | Julgamento: dano sagrado de perto, maior contra mortos-vivos |
| Assinatura | **Santuário** (no lugar da Ressurreição): área onde os aliados não caem abaixo de 1 de vida por alguns segundos | Ira Divina: por alguns segundos, os ataques básicos viram área sagrada que cura |

**Por que saiu a Ressurreição:** o jogo já ressuscita. O **Milagre** do Clérigo base, no NH3 (nível 15), levanta os aliados caídos num raio de 8 tiles com 30% da vida (efeito `revive`, T-061). Funciona enquanto o aliado ainda está na tela de morte, antes de clicar em "Retornar à Cidade". Uma segunda habilidade de reviver repetiria o Milagre. O Santuário evita a morte em vez de desfazê-la.

## 6. Custo e mecânicas novas no Core

São **40 habilidades novas** (5 por segunda classe), cada uma com dados, regras no Core, teste, ícone e efeito visual.

Mecânicas que **não existem hoje**:

| Mecânica | Quem usa |
|---|---|
| Redirecionar dano de um aliado | Guardião (Interceptação, Juramento), Sacerdote (Elo da Vida) |
| Devolver dano | Guardião (Escudo Refletor) |
| Curar com o dano causado | Berserker (Sede de Sangue), Inquisidor (Selo da Luz) |
| Vida mínima de 1 | Sacerdote (Santuário) |
| Dano que cresce enquanto mantém o alvo | Arcanista |
| Veneno que se espalha | Caçador (Flecha Dispersa, Praga) |
| Ataque básico que vira área | Inquisidor (Ira Divina) |
| Barra com 10 teclas, tela de escolha da segunda classe | todas |

**Guerreiro primeiro:** o Berserker usa quase só mecânicas que já existem, com a exceção de curar com o dano. O Guardião traz as mecânicas de redirecionar e devolver dano, que o Sacerdote também vai usar.

## 7. Ainda falta decidir

1. **Nível máximo:** 30, 35 ou 40? Se for 40, os pontos ficam mais lentos depois do 30?
2. **Curva depois do 15:** ~100 monstros por nível, ou menos?
3. **Segunda classe no nível 15**, sem exigir o Vigia?
4. **Quarta habilidade normal** de cada segunda classe (seção 5) e o **Santuário** no lugar da Ressurreição.
5. **Caçador e Arcanista** como descritos na seção 5.

## 8. Para a T-390

- A fase 4 hoje é infraestrutura (CI/CD, observabilidade, testes de carga, produção separada). Mais níveis, segunda classe e mapas novos são **conteúdo grande**: cabem melhor numa fase própria, ou numa trilha paralela, do que dentro da fase 4.
- Ordem natural: (1) regras de nível e curva nova, com o simulador; (2) sistema de segunda classe no Core e no cliente (escolha definitiva, barra de 10 teclas) com o **Guerreiro**; (3) as outras três classes; (4) o mapa novo (ou os mapas), com monstros e masmorra.
- Junto com isso, rever as metas dos testes de design e reativar os 19 casos marcados com `PlaytestRebalance.Pending` (`tasks/reports/playtest-v0.3.md`).
