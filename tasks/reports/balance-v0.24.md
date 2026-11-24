# Relatório de balanceamento v0.24 (T-510): monstros do Brejo dos Afogados e a Bruxa do Brejo

Monstros do campo do nível 16 ao 20 (`design/mapas-16-40-v0.1.md` §3.1): **Afogado** (nv16), **Homem-lagarto do Brejo** (nv17), **Serpente do Charco** (nv18) e a **Bruxa do Brejo** (nv20, chefe de campo). Design: `design/combat-encounters-v0.1.md` § Brejo dos Afogados. Testes: `tests/Game.Design.Tests/MarshDesignTests.cs` (100 seeds por lote) e `tests/Game.Core.Tests/MarshMonsterTests.cs`. O número v0.23 é da T-505 (XP por hora), que corre em paralelo.

Perfil de personagem da T-206: "esperado" = nível do grupo (o maior nível dos monstros do encontro), atributos sugeridos, equipamento comum; "fraco" = 3 níveis abaixo. Bots como na Floresta (balance-v0.11): Guerreiro e Clérigo `rotation+dodge`, Arqueiro e Mago `kite+dodge`, todos desviando de telegráfos; o Clérigo solo com Corrente e Escudo como par focado. Arenas: `map.fields` (grupos) e `map.matriarch_nest` (Bruxa); sem água do mapa (ela entra no Brejo real, T-530).

## 1. Números finais (nível 1; entre parênteses, no nível do monstro)

**Afogado** (`monster.drowned`, nv16, `ai.brute`, `undead`, sagrado −40%): vida 300 (2.100), velocidade 2,6, raio 0,4.

| Habilidade | Números |
|---|---|
| Agarrão | cone 90° × 1,6, telegráfo 0,6 s, físico 26 (92) + Preso 1,5 s, recarga 6 s |
| Garra do Afogado | arco 90°, alcance 1,4, físico 22 (78), recarga 1,8 s |

**Homem-lagarto do Brejo** (`monster.marsh_lizardman`, nv17, `ai.brute`, `humanoid` + `amphibious`): vida 440 (3.256), velocidade 4,0, raio 0,4.

| Habilidade | Números |
|---|---|
| Bote | linha 4 × 1,2, telegráfo 0,6 s, avança, físico 40 (149), recarga 6 s |
| Garra do Lagarto | arco 90°, alcance 1,5, físico 16 (60), recarga 1,4 s |

**Serpente do Charco** (`monster.marsh_serpent`, nv18, `ai.ranged_kiter`, `beast`): vida 200 (1.560), velocidade 3,0, raio 0,45; distância preferida 6, recua abaixo de 3.

| Habilidade | Números |
|---|---|
| Cuspe Venenoso | alcance 9, natureza 5 (19) + Veneno 1,5 s (27 por segundo), recarga 2,8 s |
| Picada da Serpente | arco 90°, alcance 1,4, físico 30 (117), recarga 1,6 s |

**Bruxa do Brejo** (`monster.marsh_witch`, nv20, chefe, `ai.boss`): vida 5.500 (36.850), velocidade 3,2, raio 0,45. Fases a 100%, 66% e 33%.

| Habilidade | Fases | Números |
|---|---|---|
| Golpe de Cajado | 1–3 | arco 100°, alcance 2, físico 8 (66), recarga 1,6 s |
| Dardo do Brejo | 1–2 | alcance 10, natureza 10 (82), recarga 2,2 s |
| Poça Venenosa | 1–2 | círculo 2 no alvo, telegráfo 1 s; área de 8 s, natureza 3,5 (29) por segundo; recarga 9 s |
| Chamar os Afogados | 2 | conjuração 1,5 s, 1 Afogado (máx. 2 vivos), recarga 20 s; ao entrar na fase, 2 Afogados (máx. 3) |
| Lodo que Puxa | 3 | conjuração 1 s; área presa a ela, raio 4,5, 6 s: Água Funda (−40% de velocidade) em quem está dentro; recarga 14 s |
| Poça Venenosa Rápida | 3 | a mesma poça, recarga 7 s |
| Dardo do Lodo | 3 | alcance 12, natureza 10 (82), recarga 1,6 s |

## 2. Resultados (100 seeds)

**Solo, perfil esperado** (meta: vitória ≥ 85%) e **perfil fraco** (meta: ≤ 50% ou ≥ 1,25× o tempo; Clérigo ≥ 1,10×, §3). Vitória, tempo médio das vitórias.

| Grupo (nível) | Classe | Esperado | Fraco | Fraco ÷ esperado |
|---|---|---|---|---|
| Bando de Afogados (16) | Guerreiro | 100%, 19,4 s | **20%**, 20,0 s | — |
| | Arqueiro | 100%, 32,1 s | 100%, 46,6 s | **1,45×** |
| | Mago | 100%, 8,6 s | 100%, 19,1 s | **2,22×** |
| | Clérigo | 100%, 34,7 s | **0%** | — |
| Par de Homens-lagarto (17) | Guerreiro | 97%, 22,5 s | **24%**, 25,8 s | — |
| | Arqueiro | 98%, 32,6 s | 71%, 42,5 s | **1,30×** |
| | Mago | 100%, 14,1 s | 96%, 26,2 s | **1,86×** |
| | Clérigo | 92%, 69,1 s | **0%** | — |
| Serpentes do Charco (18) | Guerreiro | 100%, 38,8 s | 88%, 49,8 s | **1,28×** |
| | Arqueiro | 100%, 16,1 s | **15%**, 19,3 s | — |
| | Mago | 100%, 4,0 s | 100%, 5,6 s | **1,40×** |
| | Clérigo | 100%, 38,9 s | 100%, 44,0 s | **1,13×** (exceção) |
| Palafitas Assombradas (17) | Guerreiro | 100%, 29,9 s | **20%**, 33,9 s | — |
| | Arqueiro | 100%, 24,3 s | 91%, 32,1 s | **1,32×** |
| | Mago | 100%, 11,8 s | 100%, 19,2 s | **1,63×** |
| | Clérigo | 100%, 51,3 s | 100%, 59,1 s | **1,15×** (exceção) |

**Bruxa do Brejo.** Grupo de 3 (Guerreiro + Arqueiro + Clérigo), perfil esperado, o Clérigo segue os feridos (`followInjured`). Meta: ≥ 80% nos níveis 19 e 20; solo no 20, ≤ 30% em cada classe.

| Lote | Vitória | Tempo | Mortes por luta |
|---|---|---|---|
| Grupo nv19 | **93%** | 82,7 s | 0,33 |
| Grupo nv20 | **100%** | 80,5 s | 0,01 |
| Solo nv20: Guerreiro, Arqueiro, Mago, Clérigo | **0%** nos quatro | — | 1,00 |

Todos os 38 casos de `MarshDesignTests` passam. Lotes com 30–60 seeds variam ±8 pontos; os números acima são os de 100.

## 3. Decisões e o que mudou desde a proposta

- **Afogado com fraqueza a sagrado (−40%).** Sem ela, com dano suficiente para o Guerreiro fraco perder, o Clérigo esperado vencia só 50% do bando (mata devagar e leva dano o tempo todo). Com ela, o Clérigo vence 100% e o Guerreiro fraco cai a 20%.
- **Homem-lagarto resistente, não frágil.** Foi a busca mais longa. O Clérigo esperado (luta de ~70 s, se cura por segundo) e o Guerreiro fraco (luta de ~25 s, depende de 3 poções) puxam para lados opostos com dano físico em pares: subir o dano derruba o Clérigo antes de derrubar o Guerreiro fraco. O que separou os dois foi **mais vida e menos dano por golpe** (lutas mais longas pesam mais nas poças de poção do que na cura do Clérigo): vida 200 → 440, Garra 25 → 16. Um Bote forte (120) matava o Arqueiro de um golpe (446 contra ~290 de vida) e foi a 40, com linha de 4 em vez de 5.
- **Serpente: dano na Picada, não no Cuspe.** O Arqueiro (~290 de vida no nv18, sem como evitar projéteis) é o limite do dano à distância; o Guerreiro e o Clérigo, com 3× a vida, mal sentiam esse dano. A força foi para a Picada (30), que só pega quem a alcança. Velocidade 3,4 → 3,0 e recuo 4 → 3: com os valores da proposta, o Guerreiro vencia 0%. As duas começam a 8 tiles uma da outra, senão a Explosão do Mago matava as duas juntas mesmo no perfil fraco (1,0×).
- **Palafitas Assombradas = 1 Afogado + 1 Homem-lagarto.** Com 2 Afogados + 1 Homem-lagarto, o Guerreiro esperado vencia 47–73%; com 1 Afogado + 1 Homem-lagarto + 1 Serpente, o Arqueiro esperado vencia 7–13%.
- **Exceção do Clérigo no perfil fraco (≥ 1,10×).** Contra as Serpentes e as Palafitas, o Clérigo fraco vence 100% e demora só 1,13× e 1,15×. Ele alcança as Serpentes com a Corrente Sagrada (a Picada não o pega) e se cura do resto, então a diferença de nível aparece só no tempo. No nível 15–18, 3 níveis valem ~1,1–1,2× de dano, menos que no 5–9 (onde a meta de 1,25× foi medida). É a mesma exceção que o Clérigo teve na Floresta e no Guarda (balance-v0.7 §3, v0.11 §3), agora com 1,10×. **Para o designer:** se a meta deve valer igual para o Clérigo, o caminho é um monstro do Brejo que ele não alcance de longe, ou que tire cura.
- **Bruxa: as poças ficaram fracas e sem Veneno.** A primeira versão (vida 12.000, cajado 34, poça 10 + Veneno) matava o grupo de 3 em 25 s. Os bots desviam de telegráfos, mas não saem de uma área já no chão, e a poça cai em cima do tanque: no simulador ela era o maior dano. Com a poça a 6 por segundo, o grupo nv19 vencia 43–60%; a 3,5, vence 93%. Quem sai das poças leva bem menos do que o simulador mostra. A conferir no playtest (T-550), com jogadores que saem delas.
- **Lodo que Puxa e a fase 3.** Com o lodo e a poça rápida a cada 5 s, o grupo nv19 caía a 18%: dentro do lodo (−40%) não dá tempo de sair da poça. A poça rápida passou a 7 s e o Dardo do Lodo a 1,6 s.
- **Degrau entre 19 e 20.** Como no Cavaleiro Espectral, o grupo um nível abaixo do chefe sofre bem mais (93% contra 100%; antes dos ajustes, 43% contra 87%). A meta ≥ 80% vale nos dois níveis, e o nv20 fica perto de 100%.

## 4. Core

- **Terreno e Água Funda de habilidade.** O `TerrainSystem` (T-502) tirava `status.deep_water` de quem estivesse fora da água do mapa, no mesmo tick, viesse o estado de onde viesse. Num mapa com água (o Brejo real), o Lodo da Bruxa sumiria no tick seguinte. Agora ele tira só o estado que ele mesmo pôs (sem fonte); o da Bruxa acaba pela duração. Testes: `MarshMonsterTests.Terrain_does_not_remove_deep_water_that_came_from_a_monster` e os 12 da T-502 (`TerrainTests`), que continuam passando.
- Nenhum perfil de IA novo: `ai.brute` (Afogado, Homem-lagarto), `ai.ranged_kiter` (Serpente), `ai.boss` (Bruxa). Área presa ao dono (`followOwner`, T-407), invocação e fases já existiam.

## 5. Pendências

- **T-530:** o Brejo real com água funda. Medir de novo os grupos lá: o Afogado e a Serpente ficam mais fortes contra quem luta na água, e o Homem-lagarto (`amphibious`) mais ainda. Pôr `ignoreTags = amphibious` nas áreas de água.
- **T-540:** mostrar o cone curto do Agarrão (0,6 s), a linha do Bote, a conjuração do Lodo e o Lodo em volta da Bruxa.
- **T-550:** conferir se as poças da Bruxa, fracas por causa dos bots, não ficaram fáceis demais para quem sai delas; e se o Clérigo fraco vencendo no Charco incomoda.
- `design/habilidades.md` § Monstros não foi atualizado (fora das áreas da tarefa, como na T-340 e na T-341).
