# Progressão — v0.1: níveis, atributos, equipamento e habilidades

Status: **aprovado pelo designer em 2026-10-17** (T-031), com as decisões da seção 10. Decisões-base do designer:

- progressão por **nível com XP**;
- pontos de atributo **totalmente livres**;
- habilidades aprendidas com **1 ponto de habilidade por nível**, cada uma com **níveis de habilidade (NH)** — 5 nas normais, 3 na assinatura (revisão de 2026-10-19); barra com o ataque básico + **5 teclas**;
- **6 espaços de equipamento**.

Todo número abaixo é ponto de partida para o simulador, não valor final. Regras e números vão para `data/*.json` (nunca no código).

## 1. Princípio de calibração

**Um personagem nível 1 com o equipamento inicial e cada habilidade no NH1 produz exatamente os números do protótipo atual** (`balance-v0.5.md`): Golpe de Espada 18, Explosão Elemental 44, Cura Direta 50, vida do Guerreiro 240 etc. Assim o balanceamento já feito na fase 0 vira o ponto de partida da curva, e o simulador continua valendo.

## 2. Níveis e XP

- Nível máximo do MVP: **15**.
- XP para passar do nível L para L+1: `round(50 × L^1,8)`.
- XP por monstro: `6 × nível do monstro × multiplicador de categoria` (normal 1, elite 5, chefe 30) × modificador de diferença de nível.
- Modificador de diferença: monstro acima do jogador +10% por nível (máx. +30%); abaixo, −20% por nível; 5 ou mais níveis abaixo dá 0 (evita farm de área fácil).
- Em grupo, a XP do monstro é dividida entre quem participou e recebe +10% por membro além do primeiro (incentivo a grupo sem punir solo).

| Nível | XP para o próximo | XP total | Monstros normais do próprio nível |
|---|---|---|---|
| 1 | 50 | 50 | ~8 |
| 2 | 174 | 224 | ~15 |
| 3 | 361 | 585 | ~20 |
| 4 | 606 | 1 191 | ~34 |
| 5 | 906 | 2 097 | ~30 |
| 6 | 1 258 | 3 355 | ~35 |
| 7 | 1 660 | 5 015 | ~40 |
| 8 | 2 111 | 7 126 | ~50 |
| 9 | 2 610 | 9 736 | ~48 |
| 10 | 3 155 | 12 891 | ~53 |
| 11 | 3 745 | 16 636 | ~57 |
| 12 | 4 380 | 21 016 | ~61 |
| 13 | 5 059 | 26 075 | ~65 |
| 14 | 5 781 | 31 856 | ~69 |

Cerca de 600 monstros normais até o nível 15 (menos com elites, chefes e a Cripta) — na ordem de 6 a 7 horas de jogo para o MVP.

### Nível por área (`first-map-v0.1.md`)

| Área | Níveis dos monstros | Chefe |
|---|---|---|
| Campos da Vigília | 1–4 (Rato 1, Lobo 2–3, Salteador 3–4) | — |
| Floresta Baixa | 5–8 (Aranha 5, Fada 6, Ent 7) | Matriarca da Floresta, nível 8 |
| Ruínas da Estrada Antiga | 9–11 (Guarda Esquelético 9, Sacerdote Morto 10, Cavaleiro Espectral 11) | — |
| Cripta do Vigia (grupo) | 12–14 | O Vigia Sem Rosto, nível 15 |

## 3. Atributos

Cinco atributos. **Todos os pontos ganhos por nível são livres** (5 por nível, 70 até o nível 15). Cada classe começa com um perfil fixo no nível 1.

| Atributo | Efeito |
|---|---|
| **Força (FOR)** | **dano corpo a corpo**: escala as habilidades físicas de perto (Guerreiro, ataque do Clérigo) |
| **Destreza (DES)** | **dano à distância**: escala as habilidades físicas à distância (Arqueiro) e o bônus da Marca do Caçador |
| **Agilidade (AGI)** | **velocidade de ataque e crítico, para todas as classes**: recarga do ataque básico ÷ (1 + AGI × 0,005), máx. +50%; crítico +0,25% por ponto (base 5%, máx. 40%). Não aumenta o dano de cada golpe. |
| **Inteligência (INT)** | **magia, cura e Mana**: escala magias arcanas e elementais (Mago), cura, escudos e dano sagrado (Clérigo); +2 de Mana máxima e +0,05 Mana/s por ponto |
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
- **Regeneração de Mana** = 5 por segundo para todas as classes + INT × 0,05 (+ afixos de equipamento). **Mana vem só do tempo** (decisão de 2026-10-19, T-032): nenhuma classe ganha Mana ao atacar, apanhar, curar ou proteger.
- **Defesa**: duas estatísticas vindas do equipamento.
  - **Armadura** reduz dano físico; **Resistência** reduz dano mágico (arcano, fogo, gelo, natureza, sagrado).
  - Redução = `D / (D + 40 + 8 × nível do atacante)`, máximo 60%.
  - Exemplo: armadura 60 contra atacante nível 10 → 60 / (60 + 120) = 33% de redução.
- **Crítico**: chance 5% + AGI × 0,25% (máx. 40%), dano ou cura × 1,5. Vale para dano e cura, em todas as classes.
- **Velocidade de ataque**: recarga do ataque básico ÷ (1 + AGI × 0,005), máx. +50% (AGI 100). Só o ataque básico; habilidades mantêm a recarga.

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
| Monstro normal | 8% | 70 / 25 / 5 / 0 |
| Elite | 40% | 40 / 45 / 15 / 0 |
| Chefe | 100% (2 itens, um incomum ou melhor garantido) | 0 / 60 / 35 / 5 |

Morte não perde equipamento (visão do MVP).

## 7. Habilidades: pontos e níveis de habilidade

Revisão do designer (2026-10-19, T-032): as habilidades são **aprendidas com pontos** ganhos a cada nível e cada uma tem **níveis de habilidade (NH)**. Substitui a "liberação por nível" da primeira versão.

### Regras gerais

- **1 ponto de habilidade por nível do personagem** (15 no total; o nível 1 já dá 1 ponto).
- Cada classe tem **4 habilidades normais com 5 NH** e **1 assinatura com 3 NH**: 4 × 5 + 3 = **23 NH possíveis para 15 pontos** — não dá para ter tudo no máximo; o jogador escolhe onde investir. Redistribuir pontos de habilidade segue a mesma regra dos atributos (na cidade; 1ª vez grátis, depois moedas).
- **Requisito de nível**: NH *n* de uma habilidade normal exige personagem no nível `2n − 1` (NH1 no 1, NH2 no 3, NH3 no 5, NH4 no 7, NH5 no 9). A assinatura exige nível 12 (NH1), 13 (NH2) e 15 (NH3).
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

Perfis de teste: personagem no **nível esperado** da área, pontos no **atributo sugerido** da classe e equipamento **comum** do nível da área ("perfil esperado"); e o mesmo personagem **3 níveis abaixo** ("perfil fraco").

| Situação | Meta |
|---|---|
| Encontro solo da área, perfil esperado, estratégia correta | vitória ≥ 85%, duração 15–40 s |
| Encontro solo da área, perfil fraco | vitória ≤ 50% |
| Matriarca (nível 8), grupo de 4 no nível 7–8 com incomuns | vitória 60–90%, duração 2–4 min |
| Vigia Sem Rosto (nível 15), grupo de 4 no nível 14–15 com raros | vitória 50–80%, duração 4–6 min |
| Mesma classe, pontos todos no atributo sugerido × espalhados | o sugerido vence mais rápido, mas o espalhado ainda vence ≥ 60% no perfil esperado |
| Distribuição de pontos de habilidade (perfil esperado, mesma classe) | duas habilidades no NH5 × pontos espalhados: as duas vencem ≥ 70%; nenhuma distribuição é a única que vence |
| Diferença entre classes (solo, perfil esperado) | nenhuma classe mais de 1,6× mais lenta que a média (Clérigo pode ser a mais lenta, como hoje) |

Os testes de design atuais passam a rodar com o "perfil esperado nível 1" — que reproduz o protótipo pela regra de calibração da §1.

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
