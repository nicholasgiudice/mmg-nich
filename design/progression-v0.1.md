# Progressão — v0.1: níveis, atributos, equipamento e habilidades

Status: **aprovado pelo designer em 2026-10-17** (T-031), com as decisões da seção 10. Decisões-base do designer:

- progressão por **nível com XP**;
- pontos de atributo **totalmente livres**;
- habilidades aprendidas com **1 ponto de habilidade por nível**, cada uma com **níveis de habilidade (NH)** — 5 nas normais, 3 na assinatura (revisão de 2026-10-19); barra com o ataque básico + **5 teclas**;
- **6 espaços de equipamento**.

Todo número abaixo é ponto de partida para o simulador, não valor final. Regras e números vão para `data/*.json` (nunca no código).

**Fase 4 (T-401, 2026-11-21):** nível máximo **40** e **segunda classe** no nível 20 (§2, §3 e §7). O detalhe das segundas classes está em `design/segunda-classe-v0.1.md`.

## 1. Princípio de calibração

**Um personagem nível 1 com o equipamento inicial e cada habilidade no NH1 produz exatamente os números do protótipo atual** (`balance-v0.5.md`): Golpe de Espada 18, Explosão Elemental 44, Cura Direta 50, vida do Guerreiro 240 etc. Assim o balanceamento já feito na fase 0 vira o ponto de partida da curva, e o simulador continua valendo.

## 2. Níveis e XP

- Nível máximo: **40** (era 15 no MVP, até a T-401). No nível **20** o personagem escolhe a segunda classe (§7 e `design/segunda-classe-v0.1.md`).
- XP para passar do nível L para L+1: `round(68 × L^1,8)` (era `50 × L^1,8` até a T-302: +36%, pedido do playtest v0.2).
- XP por monstro: `6 × nível do monstro × multiplicador de categoria` (normal 1, elite 5, chefe 30) × modificador de diferença de nível.
- Modificador de diferença: monstro acima do jogador +10% por nível (máx. +30%); abaixo, −20% por nível; 5 ou mais níveis abaixo dá 0 (evita farm de área fácil).
- Em grupo, a XP do monstro é dividida entre quem participou e recebe +10% por membro além do primeiro (incentivo a grupo sem punir solo).

| Nível | XP para o próximo | XP total | Monstros normais do próprio nível |
|---|---|---|---|
| 1 | 68 | 68 | ~11 |
| 2 | 237 | 305 | ~20 |
| 3 | 491 | 796 | ~27 |
| 4 | 825 | 1 621 | ~34 |
| 5 | 1 232 | 2 853 | ~41 |
| 6 | 1 711 | 4 564 | ~48 |
| 7 | 2 258 | 6 822 | ~54 |
| 8 | 2 871 | 9 693 | ~60 |
| 9 | 3 549 | 13 242 | ~66 |
| 10 | 4 291 | 17 533 | ~72 |
| 11 | 5 093 | 22 626 | ~77 |
| 12 | 5 957 | 28 583 | ~83 |
| 13 | 6 880 | 35 463 | ~88 |
| 14 | 7 862 | 43 325 | ~94 |
| 15 | 8 902 | 52 227 | ~99 |
| 16 | 9 998 | 62 225 | ~104 |
| 17 | 11 151 | 73 376 | ~109 |
| 18 | 12 359 | 85 735 | ~114 |
| 19 | 13 623 | 99 358 | ~120 |
| 20 | 14 940 | 114 298 | ~124 |
| 21 | 16 312 | 130 610 | ~129 |
| 22 | 17 737 | 148 347 | ~134 |
| 23 | 19 214 | 167 561 | ~139 |
| 24 | 20 744 | 188 305 | ~144 |
| 25 | 22 325 | 210 630 | ~149 |
| 26 | 23 959 | 234 589 | ~154 |
| 27 | 25 643 | 260 232 | ~158 |
| 28 | 27 377 | 287 609 | ~163 |
| 29 | 29 163 | 316 772 | ~168 |
| 30 | 30 998 | 347 770 | ~172 |
| 31 | 32 882 | 380 652 | ~177 |
| 32 | 34 816 | 415 468 | ~181 |
| 33 | 36 799 | 452 267 | ~186 |
| 34 | 38 830 | 491 097 | ~190 |
| 35 | 40 910 | 532 007 | ~195 |
| 36 | 43 038 | 575 045 | ~199 |
| 37 | 45 214 | 620 259 | ~204 |
| 38 | 47 437 | 667 696 | ~208 |
| 39 | 49 708 | 717 404 | ~212 |

No nível 40 não há próximo nível (XP para o próximo = 0). A curva é a mesma do MVP: a T-401 só subiu o teto.

Cerca de 775 monstros normais até o nível 15 (menos com elites, chefes e a Cripta) — na ordem de 8 a 9 horas de jogo para o MVP (eram ~600 e 6–7 h antes da T-302). Do 15 ao 40 são mais ~3 900 monstros normais do próprio nível; a XP por hora das áreas novas é meta do simulador (`design/segunda-classe-v0.1.md`, §3).

### Nível por área (`first-map-v0.1.md`)

| Área | Níveis dos monstros | Chefe |
|---|---|---|
| Campos da Vigília | 1–4 (Rato 1, Lobo 2–3, Salteador 3–4) | — |
| Floresta Baixa | 5–8 (Aranha 5, Fada 6, Ent 7) | Matriarca da Floresta, nível 8 |
| Ruínas da Estrada Antiga | 9–11 (Guarda Esquelético 9, Sacerdote Morto 10, Cavaleiro Espectral 11) | — |
| Cripta do Vigia (grupo) | 12–14 | O Vigia Sem Rosto, nível 15 |

## 3. Atributos

Cinco atributos. **Todos os pontos ganhos por nível são livres** (5 por nível a partir do 2: 70 no nível 15, 95 no 20, 195 no 40). Ao escolher a **segunda classe** (nível 20), o personagem ganha **+5 pontos** de uma vez: **200 no nível 40** (T-401; `secondClassAttributePoints`). Cada classe começa com um perfil fixo no nível 1.

| Atributo | Efeito |
|---|---|
| **Força (FOR)** | **dano corpo a corpo**: escala as habilidades físicas de perto (Guerreiro, ataque do Clérigo) |
| **Destreza (DES)** | **dano à distância**: escala as habilidades físicas à distância (Arqueiro) e o bônus da Marca do Caçador |
| **Agilidade (AGI)** | **velocidade de ataque e crítico, para todas as classes**: recarga do ataque básico ÷ (1 + AGI × 0,005), máx. +60% (era +50% até a T-402); crítico +0,25% por ponto (base 5%, máx. 40%). Não aumenta o dano de cada golpe. |
| **Inteligência (INT)** | **magia, cura e Mana**: escala magias arcanas e elementais (Mago), cura, escudos e dano sagrado (Clérigo); +2 de Mana máxima e +0,1 Mana/s por ponto |
| **Vitalidade (VIT)** | +10 de vida máxima por ponto |

Qual atributo escala cada habilidade está nos dados de cada efeito (`scaling`), então uma habilidade pode escalar com dois atributos (ex.: Guerreiro com FOR e arma).

Revisão do designer (2026-10-17): Agilidade virou atributo de velocidade e crítico para todas as classes; o dano à distância passou para a nova **Destreza**; Espírito foi removido e seus efeitos (cura, escudo, dano sagrado, Mana e regeneração) foram para a **Inteligência**. Mago e Clérigo compartilham a INT, mas se diferenciam pelas habilidades: no Mago ela vira dano em área e controle, no Clérigo cura e proteção.

### Perfil inicial (nível 1, antes dos pontos livres)

| Classe | FOR | DES | AGI | INT | VIT | Atributos sugeridos |
|---|---|---|---|---|---|---|
| Guerreiro | 12 | 3 | 6 | 4 | 10 | FOR, VIT |
| Arqueiro | 4 | 12 | 6 | 5 | 8 | DES, AGI |
| Mago | 3 | 3 | 5 | 11 (+2 do cajado inicial = 13) | 6 | INT |
| Clérigo | 6 | 2 | 4 | 12 | 8 | INT, VIT |

Como a distribuição é livre, a interface mostra o **atributo sugerido** da classe e a prévia do efeito de cada ponto (ex.: "+1 FOR: Golpe Pesado 51 → 53").

## 4. Vida, Mana e defesa

- **Vida máxima** = base da classe + VIT × 10 + (nível − 1) × crescimento da classe.

  | Classe | Base | Crescimento por nível | Nível 1 (perfil inicial) |
  |---|---|---|---|
  | Guerreiro | 140 | 14 | 240 |
  | Arqueiro | 50 | 9 | 130 |
  | Mago | 50 | 7 | 110 |
  | Clérigo | 70 | 10 | 150 |

- **Mana máxima** = 80 + 2 × INT (nível 1: Guerreiro 88, Arqueiro 90, Clérigo 104, Mago 106; hoje é 100 para todas).
- **Regeneração de Mana** = 5 por segundo para todas as classes + INT × 0,1 (+ afixos de equipamento). **Mana vem só do tempo** (decisão de 2026-10-19, T-032): nenhuma classe ganha Mana ao atacar, apanhar, curar ou proteger.
- **Defesa**: duas estatísticas vindas do equipamento.
  - **Armadura** reduz dano físico; **Resistência** reduz dano mágico (arcano, fogo, gelo, natureza, sagrado).
  - Redução = `D / (D + 40 + 8 × nível do atacante)`, máximo 60%.
  - Exemplo: armadura 60 contra atacante nível 10 → 60 / (60 + 120) = 33% de redução.
- **Crítico**: chance 5% + AGI × 0,25% (máx. 40%), dano ou cura × 1,5. Vale para dano e cura, em todas as classes.
- **Velocidade de ataque**: recarga do ataque básico ÷ (1 + AGI × 0,005), máx. +60% (AGI 120; era +50% com AGI 100 até a T-402, `balance-v0.18.md` §3). Só o ataque básico; habilidades mantêm a recarga.

## 5. Fórmula das habilidades

```text
valor = base + Σ (coeficiente × estatística)        estatísticas: FOR, DES, INT, VIT, ARMA (AGI não entra: atua na recarga e no crítico)
dano final = valor × modificadores (status, Marca, frontal...) × (1 − redução da defesa) × crítico
```

- **ARMA** = dano da arma equipada (ver §6). Ataques básicos e habilidades físicas usam a arma; magias, não.
- Formato nos dados (extensão do que `tech/stack-proposal-v0.1.md` já previa):

```json
{ "type": "damage", "school": "physical", "base": 15, "scaling": { "weapon": 1.0, "str": 2.5 } }
```

### Exemplo — Golpe Pesado (Guerreiro)

| | FOR | Arma | Valor |
|---|---|---|---|
| Nível 1, espada inicial (dano 6) | 12 | 6 | 15 + 6 × 1,0 + 12 × 2,5 = **51** (= protótipo) |
| Nível 8, FOR 30, espada nível 7 (dano 11) | 30 | 11 | 15 + 11 + 75 = **101** |
| Nível 15, FOR 50, espada nível 14 rara (dano 18) | 50 | 18 | 15 + 18 + 125 = **158** |

## 6. Equipamento

### Espaços

| Espaço | Estatística base |
|---|---|
| Arma | dano de arma |
| Cabeça | armadura |
| Peito | armadura (a maior) |
| Mãos | armadura |
| Pés | armadura |
| Acessório | resistência |

### Armas por classe

| Classe | Armas | Dano de arma |
|---|---|---|
| Guerreiro | espada, machado, maça | 4 + 1,0 × nível do item |
| Arqueiro | arco | 4 + 1,0 × nível do item |
| Clérigo | maça (dano de arma `3 + 0,9 × nível do item`) ou cetro (dano `2 + 0,6 × nível do item` e **+INT** `1 + 0,5 × nível do item`) |
| Mago | cajado | 2 + 0,6 × nível do item, e **+INT** `1 + 0,5 × nível do item` (as magias escalam com INT) |

Equipamento inicial (nível do item 2): espada 6, arco 6, maça 5, cajado 3 com +2 INT — calibra os números do protótipo (§1).

### Tipos de armadura

| Tipo | Quem usa | Armadura | Bônus |
|---|---|---|---|
| Pesada | Guerreiro | × 1,5 | — |
| Média | Guerreiro, Arqueiro, Clérigo | × 1,0 | — |
| Leve | todas | × 0,6 | + resistência igual a 50% da armadura |

Armadura base por peça: peito `6 + 2 × nível do item`; cabeça, mãos e pés `3 + 1 × nível do item`. Acessório: resistência `4 + 1,5 × nível do item`.

### Raridade e afixos

| Raridade | Cor | Afixos | Onde cai |
|---|---|---|---|
| Comum | branco | só a estatística base | monstros normais, vendedores |
| Incomum | verde | +1 | monstros normais e elites |
| Raro | azul | +2 | elites, chefes |
| Épico | roxo | +2 e um efeito próprio | só Matriarca e Vigia (lista própria) |

Afixos possíveis: +FOR, +DES, +AGI, +INT, +VIT (valor `1 + 0,4 × nível do item`), +vida (`5 × nível do item`), +crítico (`0,5% + 0,1% × nível do item`), +regeneração de Mana (`0,2 + 0,03 × nível do item`/s), +armadura ou +resistência (`2 × nível do item`).

O nível do item é o nível da área onde caiu.

### Queda (loot)

| Fonte | Chance de item | Raridade (comum / incomum / raro / épico) |
|---|---|---|
| Monstro normal | 4% | 70 / 25 / 5 / 0 |
| Elite | 25% | 40 / 45 / 15 / 0 |
| Chefe | 100% (2 itens, um incomum ou melhor garantido) | 0 / 60 / 35 / 5 |

Morte não perde equipamento (visão do MVP).

## 7. Habilidades: pontos e níveis de habilidade

Revisão do designer (2026-10-19, T-032): as habilidades são **aprendidas com pontos** ganhos a cada nível e cada uma tem **níveis de habilidade (NH)**. Substitui a "liberação por nível" da primeira versão.

### Regras gerais

- **1 ponto de habilidade por nível do personagem** (40 no total, 15 no MVP; o nível 1 já dá 1 ponto).
- Cada classe tem **4 habilidades normais com 5 NH** e **1 assinatura com 3 NH**: 4 × 5 + 3 = **23 NH possíveis**. Com a **segunda classe** (nível 20, escolha definitiva) vêm mais 4 normais e 1 assinatura, no mesmo formato: **46 NH para 40 pontos** — não dá para ter tudo no máximo; o jogador escolhe onde investir. Redistribuir pontos de habilidade segue a mesma regra dos atributos (na cidade; 1ª vez grátis, depois moedas).
- **Requisito de nível** (`skillRankLevels`, `signatureRankLevels`): NH *n* de uma habilidade normal da classe base exige personagem no nível `2n − 1` (NH1 no 1, NH2 no 3, NH3 no 5, NH4 no 7, NH5 no 9). A assinatura da base exige nível 12 (NH1), 13 (NH2) e 15 (NH3).
- **Segunda classe** (T-401; `secondClassLevel`, `secondClassRankLevels`, `secondClassSignatureRankLevels`): escolhida no nível **20**. As normais exigem nível 21 (NH1), 24, 27, 30 e 33 (NH5); a assinatura, 35, 37 e 40. Detalhe em `design/segunda-classe-v0.1.md`, §2.

| | NH1 | NH2 | NH3 | NH4 | NH5 |
|---|---|---|---|---|---|
| Normais da base | 1 | 3 | 5 | 7 | 9 |
| Assinatura da base | 12 | 13 | 15 | — | — |
| Normais da segunda classe | 21 | 24 | 27 | 30 | 33 |
| Assinatura da segunda classe | 35 | 37 | 40 | — | — |

- Habilidade com NH 0 não aparece na barra. Ataque básico não tem NH (escala com atributos e arma).
- Barra: **ataque básico** (clique / Espaço) + **5 teclas** (4 normais + assinatura) + poção (Q).
- **Valor por NH**: cada NH multiplica o valor da habilidade (base + escalas da §5): NH1 × 1,0; NH2 × 1,2; NH3 × 1,4; NH4 × 1,6; NH5 × 1,8 (assinatura: × 1,0 / 1,3 / 1,6). **NH3 e NH5 também mudam o comportamento** (tabelas abaixo). Quando o NH acrescenta acertos (mais flechas, explosões, giros), cada acerto vale a porcentagem indicada **do valor do NH1**: 3 flechas de 85% = 2,55 × NH1, contra 1,8 × nas habilidades sem acertos extras — o ganho maior compensa não ter efeito extra (atordoar, lentidão...).
- Custo de Mana e recarga não mudam com o NH, salvo quando a tabela diz.
- Ataques básicos não custam Mana (Projétil Arcano sem custo desde a T-032).

| Tipo de habilidade | Custo (Mana) | Recarga |
|---|---|---|
| Utilidade / reposicionamento | 0–20 | 6–12 s |
| Dano principal (alvo único) | 20–35 | 5–8 s |
| Controle | 25–40 | 10–15 s |
| Área | 35–80 | 8–14 s |
| Defensiva / cura | 0–30 | 4–15 s |
| Assinatura | 60–100 | 60–120 s |

**Calibração**: o NH1 de cada habilidade, no nível 1 com equipamento inicial, dá os números do protótipo (`balance-v0.5.md`).

### Habilidades de cada classe

**Todos os níveis de todas as habilidades** (valor de cada NH, número de acertos, durações, recargas e efeitos extras) estão em **`design/skills-v0.1.md`**. Resumo do conjunto do MVP:

| Classe | Ataque básico | Habilidades normais (5 NH) | Assinatura (3 NH, nível 12) | Fora do MVP |
|---|---|---|---|---|
| Guerreiro (FOR, arma) | Golpe de Espada | Golpe Pesado, Provocação, Golpe Circular, Guarda Firme | Muralha Viva | Investida, Interceptação |
| Arqueiro (DES, arma) | Disparo | Disparo Potente, Marca do Caçador, Recuo Rápido, Chuva de Flechas | Flecha Devastadora | Armadilha de Contenção, Flecha Perfurante, Tiro Debilitante |
| Mago (INT) | Projétil Arcano (sem custo) | Explosão Elemental, Prisão de Gelo, Barreira Arcana, Campo de Relâmpagos | Cataclismo Arcano | Combustão, Dissipar Magia |
| Clérigo (INT, arma) | Golpe Sagrado | Cura Direta, Corrente Sagrada, Escudo Sagrado, Purificação | Milagre | Bênção, Oração Restauradora |

### Exemplos de distribuição (nível 15, 15 pontos)

| Classe | Foco | Distribuição |
|---|---|---|
| Arqueiro | alvo único | Disparo Potente 5, Marca 5, Recuo 1, Chuva 1, Flecha Devastadora 3 |
| Arqueiro | área e controle de distância | Chuva 5, Recuo 3, Disparo Potente 3, Marca 1, Flecha Devastadora 3 |
| Mago | área | Explosão 5, Campo 5, Prisão 1, Barreira 1, Cataclismo 3 |
| Clérigo | cura do grupo | Cura 5, Escudo 5, Purificação 1, Corrente 1, Milagre 3 |

Com 23 NH possíveis e 15 pontos, sempre sobram 8 NH sem preencher: duas habilidades no máximo, a assinatura e o resto no mínimo, ou várias pela metade.

### Comparação Guerreiro × Arqueiro no nível 15

Arma rara de dano 18, 50 pontos no atributo de dano e 20 em AGI (FOR/DES 62, AGI 26), habilidades principais no NH5:

| | Guerreiro | Arqueiro |
|---|---|---|
| Ataque básico | Golpe de Espada 80, a cada 0,79 s | Disparo 80, a cada 0,88 s |
| Dano principal (NH5) | Golpe Pesado 27 + 1,8 × 18 + 4,5 × 62 = 338 e atordoa 1 s | Disparo Potente 3 flechas × (8 + 0,85 × 18 + 1,7 × 62) = 3 × 129 = 386 |
| Bônus | — | Marca NH5: +60% em todo dano no alvo e lentidão |
| Crítico | 11,5% | 11,5% |

## 8. Metas para o simulador e testes de design

Perfis de teste: personagem no **nível esperado** da área, pontos no **atributo sugerido** da classe e equipamento **comum** do nível da área ("perfil esperado"); e o mesmo personagem **3 níveis abaixo** ("perfil fraco"). Desde a T-402 os perfis vão do nível 1 ao 40 (`tests/Game.Sim`, `--level-curve`).

**Revisão de 2026-11-21 (T-402, `tasks/reports/balance-v0.18.md` §4):** o balanceamento do playtest de 2026-11-16 (mais Mana por INT, Explosão Elemental +50%, poções mais rápidas e curando com a VIT) deixou o jogo mais fácil, e o designer aprovou o jogo assim. As metas abaixo foram revistas para esse jogo; entre parênteses, a meta anterior.

| Situação | Meta |
|---|---|
| Encontro solo da área, perfil esperado, estratégia correta | vitória ≥ 85%, duração 15–40 s |
| Encontro solo da área, perfil fraco | vitória ≤ 50% **ou**, se vence, leva pelo menos **1,25×** o tempo do perfil esperado (era: vitória ≤ 50%; a regra do tempo valia só para o Clérigo, com 1,3×) |
| Matriarca (nível 8), grupo de 4 no nível 7–8 com incomuns | vitória **60–97%** (60–90%), duração 2–4 min |
| Cavaleiro Espectral (nível 11), grupo de 3 no nível 10–11 com incomuns | vitória média **85–99%** (70–90%), 60–120 s |
| Vigia Sem Rosto (nível 15), grupo de 4 no nível 14–15 com raros | vitória média **80–99%**, cada nível ≥ 70% (50–80%, cada nível 40–90%), duração 4–6 min |
| Ida completa à Cripta (grupos da rota + Vigia) | **20–40 min** (25–40 min) |
| Mesma classe, pontos todos no atributo sugerido × espalhados | o sugerido vence mais rápido, mas o espalhado ainda vence ≥ 60% no perfil esperado |
| Distribuição de pontos de habilidade (perfil esperado, mesma classe) | duas habilidades no NH5 × pontos espalhados: as duas vencem ≥ 70%; nenhuma distribuição é a única que vence |
| Diferença entre classes (solo, perfil esperado) | nenhuma classe mais de 1,6× mais lenta que a média; o Clérigo, a mais lenta pelo design, até **1,8×** (1,7×) |
| Mago só com a Explosão Elemental (4 Ratos) | vence no máximo metade **ou não é mais de 10% mais rápido** que a rotação (ou é mais lento que a rotação); e fica sem Mana na maioria das lutas |
| Poção de Mana no perfil fraco (Mago e Clérigo solo) | o fraco com 1 poção por luta vence no máximo 50% **ou a poção acrescenta no máximo 15 pontos** à vitória dele sem poção (vitória ≤ 50%) |
| Do nível 15 ao 40 (perfil esperado, solo, monstro normal do próprio nível, sem assinatura) | tempo para matar **não cresce mais de 20%** do nível 15 ao 40 em nenhuma classe (a escala dos monstros acompanha a do personagem; medido: de −28% a +11%); nenhuma classe mais de 1,6× mais lenta que a média, Clérigo até 1,8× (medido no 40: Clérigo 1,64× contra o Guarda e **1,92× contra o Lobo**, a rever com as segundas classes do Clérigo) |

Saiu na T-402: "pelo menos três encontros do protótipo com uma estratégia relevante entre 5% e 95%" (T-018). Os encontros do protótipo ficaram em 96–100%; resultados não binários continuam cobertos pelo Pátio dos Mortos solo (média 40–70%), pela Matriarca sem Clérigo (5–60%) e pelo perfil fraco.

Os testes de design atuais passam a rodar com o "perfil esperado nível 1" — que reproduz o protótipo pela regra de calibração da §1.

### Metas da segunda classe (T-402, para a T-408)

Perfil esperado com segunda classe: o do nível, com os +5 pontos de atributo da segunda classe nos atributos sugeridos e os pontos de habilidade do 21 em diante divididos como no perfil focado — primeiro o que falta da base, depois cada normal da segunda classe no NH1 e as duas principais dela alternando, e a assinatura quando o nível deixa. Comparação "só a base": o mesmo nível e equipamento, sem segunda classe (pontos que sobram ficam sem uso). Testes nos níveis **25, 30 e 40**, monstros normais do próprio nível.

| Situação | Meta |
|---|---|
| Berserker × Guerreiro só com a base, grupo típico (3 monstros normais do nível), solo | o Berserker vence ≥ 85% e em no máximo **85%** do tempo do Guerreiro só com a base |
| Berserker, mesmo encontro | recebe no máximo **1,5×** o dano do Guerreiro só com a base (a Fúria expõe, a Sede de Sangue devolve), e vence ≥ 85% |
| Guardião × Guerreiro só com a base, encontro de grupo (grupo de 4 com Clérigo contra um chefe ou elite do nível) | os **outros membros** do grupo recebem pelo menos **15% menos dano** com o Guardião, e a vitória não cai |
| Guardião, solo | vence ≥ 85% o grupo típico; pode ser mais lento que o Guerreiro só com a base |
| Diferença entre segundas classes (solo, perfil esperado, grupo típico) | nenhuma segunda classe mais de **1,6×** mais lenta que a média das outras (as classes base no mesmo nível entram na média enquanto houver só as do Guerreiro) |
| Nível 21 (NH1 da segunda classe) | escolher a segunda classe não deixa o personagem mais lento que só a base: o NH1 já é útil (`design/segunda-classe-v0.1.md` §2) |
| Distribuição de pontos (base × segunda classe) | pôr os pontos do 21 em diante só na base (as 3 NH que faltam) ou na segunda classe: as duas vencem ≥ 70%; nenhuma é a única que vence |

## 9. Implementação (fase 2, ordem sugerida)

1. Core: componente de atributos/estatísticas, `scaling` nos efeitos (dano, cura, escudo), defesa (armadura/resistência), crítico com o RNG da simulação; dados de perfil por classe. Com perfil nível 1 + equipamento inicial, todos os testes atuais continuam passando.
2. Core: níveis, XP, pontos de atributo livres, **pontos de habilidade e NH** (multiplicador por NH; variações de NH3/NH5 descritas nos dados de cada habilidade, ex.: `"ranks": [{ "rank": 3, "hits": 2, "hitScale": 0.85 }]`), **múltiplos acertos** (flechas, explosões, giros) e requisito de nível por NH.
3. Core: itens, equipamento e queda (já no esboço da fase 2).
4. Dados: habilidades com os NH da §7 e as novas assinaturas.
5. Simulador: perfis por nível/equipamento; testes de design da §8.
6. Cliente: tela de personagem (atributos com prévia), tela de habilidades (pontos e NH, com prévia do próximo NH), inventário/equipamento.

## 10. Decisões (aprovadas em 2026-10-17)

O designer aprovou a proposta e as recomendações abaixo.

1. **Redistribuir pontos**: permitido na cidade; a primeira vez é grátis, depois custa moedas, com custo crescente pelo nível.
2. **Habilidades fora do MVP** (ficam para especializações): Guerreiro — Investida, Interceptação; Arqueiro — Flecha Perfurante, Tiro Debilitante; Mago — Combustão, Dissipar Magia; Clérigo — Bênção, Oração Restauradora.
3. **Assinaturas do nível 12**: Muralha Viva, Flecha Devastadora, Cataclismo Arcano e Milagre.
4. **Nível máximo 15**, ~6–7 h até o máximo no MVP.
5. **Cajados e cetros dão INT** como estatística base, além de um dano de arma menor (§6). Para manter a calibração, o perfil inicial do Mago tem INT 11 e o cajado inicial dá +2.
6. **Crítico vale para cura** (e escudo).

### Revisão de 2026-10-19 (T-032)

7. **Habilidades por pontos**: 1 ponto de habilidade por nível; 5 NH nas habilidades normais, 3 na assinatura; não dá para maximizar tudo (§7).
8. **Barra com 5 teclas**: para o Arqueiro ganhar o **Disparo Potente**, a **Armadilha de Contenção** saiu do MVP.
9. **Mana só por tempo** (5/s para todas as classes + INT); sem ganho por ataque, dano recebido, cura ou escudo.
10. **Projétil Arcano sem custo**; **Campo de Relâmpagos com raio 3,5**.
11. Ideias para depois do MVP ficam em `design/ideias-futuras.md`.
