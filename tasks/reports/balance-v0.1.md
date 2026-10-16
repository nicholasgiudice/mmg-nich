# Relatório de balanceamento v0.1 (T-016)

Simulador `tests/Game.Sim`, conteúdo real de `data/`, 100 seeds por linha (seed inicial 1, desvio de posição 0,5). Os testes ficam em `tests/Game.Design.Tests` (13 testes, cerca de 1 min 30 s na VPS de 2 vCPU, Debug; a variável `DESIGN_RUNS` muda o N).

## 1. Testes e limites finais

| Encontro | Verificação | Limite final | Observação |
|---|---|---|---|
| Guerreiro x 3 Lobos | `rotation` vence | >= 90% | igual à tarefa |
| | "ameaça + básico" (`taunt_and_basic`) | vence <= 50% **ou** tempo >= 1,5x o da `rotation` | adaptado: ver abaixo |
| Arqueiro x Lobo + Salteador | `kite` vence | >= 85% | igual |
| | `idle` e `rotation` vencem | <= 40% (os dois) | `idle` sozinho é fraco (os monstros não vão até um alvo fora do alcance de aggro), por isso `rotation` (persegue, não recua) entra também |
| Mago x 4 Ratos | `rotation` vence | >= 85% | igual |
| | `explosion_spam` fica sem Mana (abaixo do custo da Explosão, depois da 1a conjuração, com inimigos vivos) | em mais de 50% das execuções | loop próprio no teste (o simulador não expõe o mínimo de Mana) |
| Clérigo x Guarda | `rotation` vence | >= 85% | igual |
| | tempo médio do Clérigo / média das outras (Guerreiro `rotation`, Arqueiro `kite`, Mago `rotation`) | 1,2 a 1,6 | igual |
| | `heal_only` | 0 vitórias | igual |
| Grupo x Matriarca | grupo de 4 em `kite+dodge` | >= 70% | |
| | mesmo grupo parado (`idle`) e perseguindo sem desviar (`rotation`) | <= 20% (os dois) | |
| | grupo sem Clérigo (Guerreiro, Arqueiro, Mago), `kite+dodge` | 30% a 70% | encontro derivado em memória pelo teste |
| Bots | `TelegraphHazardSource` vê telegráfos da Matriarca e só devolve perigos não resolvidos | > 0 em 40 s | |

Adaptações:
- **`taunt_and_basic`**: o Guerreiro do protótipo não tem Provocação (a T-014 pôs ameaça em Golpe Pesado e Circular). O teste usa `--opt class.warrior:taunt=warrior.whirlwind` (Golpe Circular, a ameaça em área) + ataque básico.
- **Arqueiro parado**: `idle` + `rotation`, como descrito acima.
- **Grupo "que se move"**: todos em `kite+dodge` (inclusive o Guerreiro). Variantes com o Guerreiro e o Clérigo em `rotation+dodge` ficaram muito piores (22%; ver problemas).

## 2. Antes / depois (100 seeds)

"Antes" = números da T-014 (reproduzidos nesta tarefa; Matriarca com 60 seeds). "Depois" = números atuais de `data/`.

| Encontro / estratégia | Antes: vitória, tempo | Depois: vitória, tempo |
|---|---|---|
| 3 Lobos / `rotation` | 100%, 21,0 s | 100%, 20,2 s |
| 3 Lobos / "ameaça + básico" (Circular) | 100%, 24,8 s (1,18x), reprova | 0%, passa |
| Lobo+Salteador / `kite` | 100%, 25,5 s | 100%, 25,5 s |
| Lobo+Salteador / `rotation` | 0% | 0% |
| Lobo+Salteador / `idle` | 0% (tempo esgotado) | 0% (tempo esgotado) |
| 4 Ratos / `rotation` | 83%, 17,1 s, reprova | 99%, 17,6 s |
| 4 Ratos / `kite` | 100%, 18,1 s | 100%, 18,1 s |
| 4 Ratos / `explosion_spam` | 98%, 9,8 s | 98%, 9,8 s (passa: fica sem Mana) |
| Guarda / `rotation` | 100%, 26,3 s | 100%, 26,3 s (1,25x a média das outras classes) |
| Guarda / `heal_only` | 0% | 0% |
| Matriarca, grupo de 4 / `kite` | 91,7%, 207 s | 100%, 143,5 s |
| Matriarca, grupo de 4 / `kite+dodge` | 76,7% com áreas como perigo; 100% só com telegráfos | 100%, 140,4 s |
| Matriarca, grupo de 4 / `rotation` (persegue, sem desviar) | 0% | 0% |
| Matriarca, grupo de 4 / `idle` | 0% | 0% |
| Matriarca sem Clérigo / `kite+dodge` | 0% | 55%, 215 s |
| Matriarca sem Clérigo / `kite` | 18% | 91%, 218 s |

## 3. Mudanças de números (somente `data/`)

| Arquivo / id | Antes | Depois | Motivo |
|---|---|---|---|
| `abilities_monsters.json` `grey_wolf.bite` base | 6 | 7 | o Guerreiro com só Circular+básico vencia sempre (a `rotation` recebia 130 de dano, a rasa 220, de 240 de vida); com 7 a rasa perde e a `rotation` ainda vence 100% |
| `field_rat.bite` base | 5 | 4 | Mago `rotation` vencia 83% (< 85%). O dano é arredondado: 4,5 não mudou nada, 4 levou a 99% |
| `matriarch.bite`, `matriarch.rending_bite` base | 18 | 9 | grupo sem Clérigo nunca vencia (ver abaixo) |
| `matriarch.charge` base | 38 | 19 | idem |
| `spider_small.bite` base | 4 | 2 | idem |
| `spider_big.bite` base | 11 | 5,5 | idem |
| `status.poison` dano por tick | 3 | 1 | idem (veneno de aranhas e poças era a maior fonte de dano contínuo) |
| `area.venom_pool` dano por tick | 4 | 1 | idem |
| `monster.forest_matriarch` vida | 4500 | 3825 (-15%) | complementa a redução de dano para o grupo sem Clérigo cair em 30-70% |

Por que a Matriarca foi reduzida tanto: o grupo de 3 (480 de vida somados, sem cura nem escudo de outro jogador) precisa sobreviver cerca de 200 s sem nenhuma fonte de sustentação. Com o dano original, o grupo de 4 tomava cerca de 913 de dano (curas 384) e o grupo sem Clérigo morria na fase 2 em 100% das execuções. Reduzir o dano direto em 30% (e metade do veneno) ainda deu 0%; dano x0,5 com veneno e poças em 1 deu 23-27% sem vida alterada; só junto com a vida -15% o resultado ficou em 52-55%. Isso é problema de regra, não de número (ver §4): a Matriarca ficou fácil para o grupo completo (cerca de 0,1 morte por luta), embora ainda exija movimento (parado e perseguindo sem desviar: 0%).

## 4. Problemas de regra para tarefas futuras

1. **Sem sustentação fora o Clérigo.** Nenhuma classe cura a si mesma, não há consumíveis nem regeneração em combate; só o Mago (Barreira) e o Guerreiro (Guarda Firme) mitigam. O design (seção 6, item 7) pede que consumíveis deixem grupos sem Clérigo progredirem. Quando existirem consumíveis (ou cura própria), reverter os fatores x0,5 e a vida -15% da Matriarca.
2. **Bot `+dodge` oscila nas áreas persistentes.** Tratar poças e teias como perigo faz o bot sair da borda, o movimento do bot interno o devolve para dentro e ele perde dano (`kite+dodge` 76,7% contra 100% só com telegráfos; grupo com Guerreiro e Clérigo em `rotation+dodge`, 22%). Por isso `TelegraphHazardSource` só inclui áreas com `includeAreas: true` (padrão `false`). Pendente: um desvio que veta o movimento para dentro de áreas, em vez de só empurrar para fora. Bots corpo a corpo com `rotation+dodge` ficam muito atrás de `kite` na Matriarca.
3. **Sem Provocação no Guerreiro** (a T-014 usou ameaça em Golpe Pesado e Circular). O "botão automático de ameaça" do design (seção 6, item 1) não existe; a falha de design foi testada com "Circular + básico".
4. **Resultados quase binários.** Com bots determinísticos uma luta costuma dar 0% ou 100%; os limites 30-70% são difíceis de acertar. A variação por seed vem só do desvio inicial de posição (0,5 tile) e do RNG de combate. Convém dar variação de reação aos bots (T-015) antes de recalibrar.
5. **Lacunas do Core apontadas na T-014 e impacto**
   - Corrente Sagrada não cura o conjurador: o Clérigo não tem habilidade de dano mais cura; a razão 1,25x do tempo vem do dano base e do ponto fraco sagrado do Guarda.
   - Dano extra contra mortos-vivos só via `resistances.holy` do monstro; a regra de recurso aceita uma tag. Qualquer monstro com `holy` negativo muda o tempo do Clérigo sem relação com a classe.
   - Armadilha sem tempo de armar e áreas só criadas por habilidades: a teia fixa de arena do design (3 trechos) não existe, só a Teia Sufocante da fase 2; essa leitura de arena não é testável.
   - Frenesi não reduz duração de controle e não há Purificação: o veneno só expira. Isso pesa no sustento do grupo sem Clérigo (item 1).
   - Altar/bênção do Guarda não modelado.
6. **Mago rápido demais:** `rotation` em 17,6 s (meta da T-014: 20 s) e `explosion_spam` vence 98% em 9,8 s, mais rápido que a `rotation`, apesar de ficar sem Mana (o teste só exige o esgotamento). Se a falha "Explosão resolve sozinha" for levada a sério, exigir que `explosion_spam` perca mais ou leve mais tempo que a `rotation`.
7. **`idle` do Arqueiro é teste fraco:** os monstros não avançam sobre um alvo fora do alcance de aggro, então `idle` dá tempo esgotado em 100%. Por isso o teste também exige que `rotation` não vença.

## 5. Desempenho dos testes

13 testes de design, N = 100 seeds, Debug, 2 vCPU: cerca de 1 min 30 s (`dotnet test tests/Game.Design.Tests`). O custo é quase todo da Matriarca (cerca de 30 s por lote de 100). Limite da tarefa: 5 min.
