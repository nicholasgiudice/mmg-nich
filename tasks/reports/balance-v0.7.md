# Relatório de balanceamento v0.7 (T-206): perfis por nível e monstros por nível

Primeira medição da **curva de progressão** (`design/progression-v0.1.md` §8): personagens com nível, atributos, equipamento e NH, contra monstros do nível da área. Simulador `tests/Game.Sim`, 100 seeds por linha e as mesmas opções dos testes de design. Base: `balance-v0.6.md` (NH no protótipo).

Decisões do designer (2026-10-24) na seção 4.

## 1. O que mudou

**Monstros por nível (Core, `rules.monsters`).** Até aqui o nível do monstro só entrava na XP e na defesa: um "rato nível 9" tinha a vida e o dano de um rato nível 1. Agora os números de `monsters.json` valem para o **nível 1**, e no nível L:

| | vida | potência (dano, cura e escudo, incluindo áreas e status do monstro) |
|---|---|---|
| normal e elite | × (1 + 0,40 × (L − 1)) | × (1 + 0,17 × (L − 1)) |
| chefe | × (1 + 0,30 × (L − 1)) | × (1 + 0,38 × (L − 1)) |

- O nível vem do surgimento (`enemies[].level`), do encontro (`monsterLevel`) ou, sem os dois, da definição.
- Invocações herdam o nível de quem invoca.
- Personagens nunca são escalados por essa regra.
- Os 5 encontros do protótipo ganharam `"monsterLevel": 1` e continuam idênticos. Todos os testes de design antigos passam sem mudança.

**Perfis de personagem no simulador.**
- `--level`, `--attrs sugerido|espalhado`, `--gear comum|incomum|raro`, `--gear-level`, `--skills focado|espalhado`, `--focus classe=h1,h2` e `--monster-level`.
- Com perfil, o personagem nasce com progressão (`Leveling.SpawnCharacter`) e equipamento completo do nível: a arma do tipo inicial, a armadura mais pesada que a classe usa nos 4 espaços e o anel. Incomuns levam o afixo do atributo principal; raros, ele e +vida.
- Os pontos de habilidade são os que o nível dá, com os requisitos de NH. Primeiro cada normal no NH1; depois, no focado, o par alternando; no espalhado, sempre a de menor NH. No nível 15 isso dá exatamente os perfis da T-063.
- "Perfil esperado" = nível da área, atributos sugeridos, equipamento comum do nível e NH focado. "Perfil fraco" = 3 níveis abaixo.
- O Clérigo solo usa Corrente e Escudo como par focado, porque a cura do grupo não serve sozinho.

## 2. Metas da §8

Os encontros solo do protótipo rodam com monstros do nível 5 (Floresta) e do nível 9 (Ruínas).

| Encontro (classe, estratégia) | Nível | Esperado | Fraco (−3) | Atributos espalhados | NH espalhados |
|---|---|---|---|---|---|
| Três Lobos (Guerreiro, `rotation`) | 5 | 100%, 20,8 s | 0% | 100%, 26,7 s | 100%, 20,8 s |
| Lobo + Salteador (Arqueiro, `kite`) | 5 | 100%, 13,1 s | 0% | 100%, 16,7 s | 100%, 13,1 s |
| Quatro Ratos (Mago, `rotation`) | 5 | 100%, 31,1 s | 0% | 88%, 40,6 s | 100%, 31,1 s |
| Guarda Esquelético (Clérigo, `rotation`) | 5 | 100%, 37,4 s | 100%, 61,4 s | 100%, 43,3 s | 100%, 41,8 s |
| Três Lobos | 9 | 100%, 20,0 s | 0% | 100%, 26,7 s | 100%, 21,9 s |
| Lobo + Salteador | 9 | 100%, 10,3 s | 3% | 100%, 16,3 s | 100%, 12,0 s |
| Quatro Ratos | 9 | 100%, 27,0 s | 6% | 100%, 39,2 s | 100%, 29,1 s |
| Guarda Esquelético | 9 | 100%, 38,7 s | 100%, 60,3 s | 100%, 49,6 s | 100%, 43,8 s |

| Meta | Resultado |
|---|---|
| Esperado ≥ 85%, 15–40 s | **passa** em vitória e no teto de 40 s. O Arqueiro dura 10–13 s, abaixo dos 15 s; o encontro dele já durava 7,8 s no protótipo, porque são só 2 monstros. O teste confere só o teto. |
| Fraco ≤ 50% | **passa**, com 0–6%. Exceção do Clérigo, na §3. |
| Sugerido × espalhado | **passa**: o sugerido é sempre mais rápido e o espalhado vence 88–100% (com a definição de "espalhado" da §4). |
| NH focado × espalhado | **passa**: os dois vencem 100%. Nenhuma distribuição é a única que vence. |
| Diferença entre classes (≤ 1,6× da média) | **passa**. No nível 5 o Clérigo fica em 1,46× (média 25,6 s). No nível 9, em 1,61× (média 24,0 s), dentro do limite de 1,7× que o designer aprovou para ele (§4). |
| Matriarca nível 8, grupo 7–8 com incomuns: 60–90%, 2–4 min | **passa**: grupo nível 7 com 71% em 226 s; nível 8 com 88% em 146 s. |
| Vigia Sem Rosto (nível 15) | **não medido**: o encontro não existe (fase 3). |

## 3. Observações

- **Clérigo fraco ainda vence o Guarda.** Contra um único monstro lento, a Corrente, a cura e o escudo seguram a luta indefinidamente. O Clérigo 3 níveis abaixo vence, mas demora 1,6× mais (61 s contra 37 s), e o teste exige pelo menos 1,3×. Para ele perder seria preciso um encontro com pressão de verdade (vários atacantes, dano à distância), o que é conteúdo da área, não da curva.
- **A curva é um degrau.** O perfil esperado vence 100% e o fraco perde quase sempre. A diferença de 3 níveis é grande: 15 pontos de atributo, 3 de habilidade e 3 níveis de item. Isso cumpre a §8, mas o jogador nota bem quando está fora do nível da área.
- **Sensibilidade.** A potência dos monstros normais é o número mais delicado. Com 0,20 em vez de 0,17, o Mago espalhado vai de 100% a 7%. Com 0,25, o Guerreiro espalhado vai de 90% a 0%. O chefe também é sensível: potência de chefe 0,28 dá 97% no nível 8, e 0,35 dá 43% no nível 7.
- **Níveis 12+ ficam fora.** No nível 13, com a mesma regra, o Arqueiro esperado cai para 67% e o Mago fraco vence 97%, porque a assinatura entra no nível 12. Solo vai só até o nível 11 no mapa (Ruínas). Do 12 em diante é a Cripta, de grupo, que é fase 3 e deve ser medida com o Vigia.

## 4. Decisões do designer (2026-10-24)

1. **Monstros escalam por regra no Core** (`rules.monsters`), e não só no simulador nem com dados reescritos. Os números de hoje viram o nível 1.
2. **Chefes têm crescimento próprio** (`rules.monsters.boss`). Com o crescimento geral, a Matriarca nível 8 ficava fácil demais (94–98%). Elites podem ganhar o seu (`elite`), mas hoje usam o geral.
3. **"Espalhado" = metade dos pontos no atributo sugerido e metade dividida entre os demais.** Com os pontos divididos igualmente pelos 5 atributos, o Mago (que depende só de INT) vencia 7% dos ratos nível 9. A meta mede quem diversifica, não quem joga pontos fora em FOR e DES.
4. **Explosão/Campo do Mago no NH5 (balance-v0.6 §4), opção 3:** nos NH altos o spam pode vencer, mas a rotação não pode ser mais que 1,5× mais lenta que ele.
   - Com o Mago nível 9 contra ratos nível 9, o spam da Explosão NH5 vence 100% em 17,6 s, e a rotação vence 100% em 25,3 s (1,44×).
   - O spam do Campo NH5 vence só 13%.
   - O teste `Mage_rank_5_area_skill_does_not_solve_the_rats_alone` foi reativado com essa regra.
5. **Clérigo pode ser até 1,7× mais lento que a média** (as outras classes, 1,6×), por ser a classe de sustentação.

## 5. Testes

- `tests/Game.Design.Tests/ProgressionDesignTests.cs`: as metas da §2 (esperado, fraco, atributos, NH, diferença entre classes, Matriarca).
- `RankDesignTests.Mage_rank_5_area_skill_does_not_solve_the_rats_alone` reativado.
- A suíte de design passou de cerca de 2,6 min para cerca de 4 min na VPS (2 vCPU, 100 seeds), ainda abaixo do limite de 5 min.
- `tests/Game.Core.Tests/MonsterScalingTests.cs`, `tests/Game.Data.Tests/MonsterScalingLoaderTests.cs` e `tests/Game.Sim.Tests/CharacterProfileTests.cs`: a regra nova, a validação e os perfis.

## 6. Pendências

- O Vigia Sem Rosto (nível 15, grupo 14–15 com raros, 50–80%) entra quando o encontro existir (fase 3).
- O servidor (T-214/T-215) deve criar os monstros com o nível da área: `level`/`monsterLevel`, ou o nível da definição. Hoje ele usa os encontros do protótipo, que são nível 1.
- Rever a duração do encontro solo do Arqueiro (2 monstros, 10–13 s) quando os Campos tiverem grupos de surgimento reais (T-215).
