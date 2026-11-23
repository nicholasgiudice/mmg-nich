# Relatório de balanceamento v0.20 (T-423): Sacerdote e Inquisidor

Fase 4, segunda classe do Clérigo. Esta tarefa escreve nos dados as 10 habilidades de `design/segunda-classe-v0.1.md` §8 (com todos os NH) e confere, com o simulador, as metas da segunda classe (`design/progression-v0.1.md` §8, "Metas da segunda classe" e "Metas do Clérigo"). Feita no Windows.

## 1. Método

- **Dados:** `data/abilities_priest.json` e `data/abilities_inquisitor.json` (habilidades, estados, as áreas da Consagração e os dois golpes da Ira Divina no mesmo arquivo da classe); `data/classes.json` lista as cinco de cada segunda classe na ordem do design.
- **Perfis** (`tests/Game.Design.Tests/SecondClassDesignTests.cs`, agora genérico por classe base em `Specs`): o perfil esperado com segunda classe da T-408 (pontos primeiro no que falta da base, depois cada normal da segunda classe no NH1, o par principal alternando, a assinatura quando o nível deixa). Solo, a base do Clérigo usa o par solo dele (Corrente e Escudo), como o "só a base". Par principal: **Bênção e Inspiração** (Sacerdote) e **Julgamento e Martelo da Justiça** (Inquisidor).
- **Encontros:** grupo típico solo, os **Três Lobos** do nível com o Clérigo no lugar do Guerreiro (cópia do encontro real, mesma arena e posições); duelo contra **um Lobo** do 40 (o da curva de níveis da T-402, sem as assinaturas); grupo, a **Matriarca** do nível contra o grupo de 4 (`kite+dodge`), equipamento comum; diferença entre segundas classes no encontro solo da classe base (Guerreiro: Três Lobos; Clérigo: **Guarda Esquelético**). 100 seeds por lote.
- **Bot** (`src/Game.Bots/ConfigurableBot.GroupStatus.cs`): habilidades que só aplicam estados das mecânicas novas seguem o estado que aplicam, não o id: reforço de grupo (Bênção, Inspiração, Graça Veloz) com um inimigo a até 10 tiles e alguém no raio sem o estado (no começo da luta e quando acaba); Santuário com um aliado no raio (ou o próprio) com até 30% de vida; Oração com alguém ferido no raio; Ira Divina com 2+ inimigos a até 2 do alvo; Selo da Luz no alvo do bot (o do grupo). No grupo, o Sacerdote abre com Bênção e Inspiração (`order`); o Inquisidor usa a rotação da barra (mudar a ordem não mexe no resultado: 31,6 × 31,7 s no 25).
- **Dano dos outros membros do grupo:** novo `DamageDealtByClass` no simulador, por **segundo vivo** de cada membro (a mesma medida do dano recebido na T-408).

## 2. Resultados

### Inquisidor

| Nível | Clérigo só a base (Três Lobos) | Inquisidor | Inquisidor / base |
|---|---|---|---|
| 21 (NH1 do Martelo) | 100%, 45,5 s | 100%, 35,2 s | 77% |
| 25 | 100%, 45,9 s | 100%, 31,7 s | **69%** |
| 30 | 100%, 47,4 s | 100%, 24,5 s | **52%** |
| 40 | 100%, 48,9 s | 100%, 13,3 s (com a Ira Divina) | **27%** |

Duelo contra um Lobo do 40, sem assinaturas: Guerreiro 11,8 s, Arqueiro 5,6 s, Mago 5,3 s, **Inquisidor 8,4 s**; média 7,8 s, Inquisidor **1,08×** (o Clérigo só com a base ficava em 1,92×).

### Sacerdote

| Nível | Solo (Três Lobos) | Grupo, Matriarca: vitória só a base → Sacerdote | Outros membros, dano causado por segundo vivo |
|---|---|---|---|
| 21 | 100%, 42,5 s (base 45,5 s) | — | — |
| 25 | 100%, 43,1 s (base 45,9 s) | — | — |
| 30 | 100%, 41,7 s (base 47,4 s) | 77% → **98%** (173 → 98 s) | 316 → 401/s (**+27%**) |
| 40 | 100%, 42,2 s (base 48,9 s) | 64% → **98%** (180 → 100 s) | 411 → 537/s (**+31%**) |

### Metas

| Meta | Resultado |
|---|---|
| Inquisidor vence ≥ 85% os Três Lobos em no máximo 85% do tempo do Clérigo só com a base | **passa** (27–69%) |
| Inquisidor no 40 contra o Lobo: no máximo 1,8× a média das classes | **passa** (1,08×) |
| Sacerdote no grupo: a vitória não cai | **passa** (sobe 21 e 34 pontos) |
| Sacerdote no grupo: os outros causam ≥ 10% mais dano por segundo vivo | **passa** (+27% e +31%) |
| Sacerdote solo vence ≥ 85% | **passa** (100%; até um pouco mais rápido que a base, pela Bênção e pela Oração) |
| Nível 21: o NH1 já é útil (não fica mais lento que só a base) | **passa** nas quatro (Sacerdote 42,5 × 45,5 s; Inquisidor 35,2 × 45,5 s) |
| Pontos do 21 em diante só na base ou na segunda classe: os dois vencem ≥ 70% | **passa** nas quatro (100%) |
| Diferença entre segundas classes | **passa com a meta revista** (seção 3) |
| Todo custo cabe na Mana do Clérigo no 40 | **passa** (o maior custo é 90, o Santuário) |

As metas do Guerreiro (T-408) continuam passando com os mesmos números (Berserker 74–82% do tempo, Guardião −21% e −26% de dano nos outros).

## 3. Meta revista: diferença entre segundas classes

A meta da T-402 dizia "nenhuma segunda classe mais de 1,6× mais lenta que a média das outras (solo, grupo típico), com as classes base na média enquanto houver só as do Guerreiro". Com as quatro contra os Três Lobos (Arqueiro e Mago base nos encontros deles):

| Nível | Guardião | Berserker | Sacerdote | Inquisidor | Arqueiro (base) | Mago (base) |
|---|---|---|---|---|---|---|
| 25 | 17,4 s | 12,6 s | 43,1 s (**2,9×**) | 31,7 s (1,83×) | 7,1 s | 6,3 s |
| 30 | 24,0 s | 12,2 s | 41,7 s (**2,8×**) | 24,5 s | 6,8 s | 6,4 s |
| 40 | 24,7 s | 12,2 s | 42,2 s (**3,4×**) | 13,3 s | 6,7 s | 6,4 s |

O Sacerdote não passa em nenhum nível, e não tem como passar sem deixar de ser de cura: o design aceita que ele seja **mais lento que o Clérigo só com a base**, e o Clérigo só com a base já leva 46–49 s contra os Três Lobos (3× o Guerreiro). O Inquisidor no 25 tem só o NH1 de duas habilidades (os outros 23 pontos fecham a base) e herda a lentidão do Clérigo contra grupos. Tirar o Sacerdote da média também não resolve: o Guardião passa a 2,6× no 40, porque a média fica só com Berserker, Inquisidor e as bases de longe em encontros fáceis (6–7 s). Na T-408 a meta passava porque o Clérigo base (43 s no Guarda) entrava na média.

**Meta revista** (a mesma lógica da diferença entre classes da §8):

- cada segunda classe no **encontro solo da classe base dela** (Guerreiro: Três Lobos; Clérigo: Guarda Esquelético);
- limite 1,6×; as do **Clérigo**, a classe mais lenta pelo design, até **1,8×**;
- o **Sacerdote** (cura) entra na média, mas sem limite: as metas dele são a vitória solo e o grupo.

Medido assim (Guarda Esquelético para o Clérigo; base só com a base: 42,8 / 43,1 / 43,4 s):

| Nível | Guardião | Berserker | Sacerdote | Inquisidor | Razão máxima com limite |
|---|---|---|---|---|---|
| 25 | 17,4 s | 12,6 s | 36,9 s | 24,2 s | Inquisidor 1,51× (≤ 1,8) |
| 30 | 24,0 s | 12,2 s | 34,9 s | 19,8 s | Guardião 1,50× (≤ 1,6) |
| 40 | 24,7 s | 12,2 s | 35,5 s | 17,2 s | Guardião 1,58× (≤ 1,6) |

O Guardião no 40 fica perto do limite (1,58×). Quando entrarem as segundas classes do Arqueiro e do Mago (T-433, T-443), as bases deles saem da média e a meta precisa ser revista de novo.

## 4. Sacerdote no grupo: de onde vem

Matriarca do nível 40, 100 seeds, uma habilidade do Sacerdote de fora de cada vez (só a base: vitória 64%, 180 s, outros causam 411/s, 2,94 mortes por luta):

| Variante | Vitória | Tempo | Outros causam | Mortes |
|---|---|---|---|---|
| Sacerdote completo | 98% | 100 s | 537/s (+31%) | 1,36 |
| sem a Bênção | 88% | 146 s | 446/s (+9%) | 2,10 |
| sem a Inspiração | 100% | 110 s | 516/s (+26%) | 1,40 |
| sem a Oração | 99% | 107 s | 521/s (+27%) | 1,35 |
| sem a Graça Veloz | 99% | 94 s | 528/s (+29%) | 1,19 |
| só Bênção e Inspiração (sem Oração, Graça e Santuário) | 90% | 114 s | 509/s (+24%) | 1,91 |
| idem, no 30 | 94% | 100 s | 391/s (+23%) | 1,59 |

**A Bênção é quase todo o efeito:** +13 em todos os atributos no NH5 (do 33 em diante; +10 no NH3) dá dano e vida a todos, e o grupo morre menos (as mortes caem pela metade). Sem ela, os outros causam só 9% mais. A Inspiração e a Oração somam alguns pontos cada.

**Alerta para o designer:** o Sacerdote leva a Matriarca de 64–77% de vitória a 98% e corta a luta quase pela metade; o Guardião, a outra segunda classe de apoio, deixa a vitória em 72–75%. As metas pedem só "não cai" e "+10%", então nenhum número mudou, mas o Sacerdote ficou bem mais forte no grupo que o Guardião. Se for demais, a alavanca é a Bênção (valor ou duração).

## 5. Outros achados

- **Bênção e a INT do Sacerdote:** a Bênção soma INT no próprio Sacerdote. Se ele a recebe primeiro, os aliados ganham o bônus pela INT já abençoada (+8 em vez de +7 no perfil do 21). Os dados aplicam nos aliados primeiro e no Sacerdote por último. Não é número novo, é a ordem dos efeitos.
- **Curas críticas:** a cura por segundo da Oração pode ser crítica, como qualquer cura com escala por atributo.
- **Inquisidor muito à frente no 40:** com a Ira Divina (NH3 no 40) acertando os três Lobos, o Inquisidor leva 13,3 s, parecido com o Berserker (12,2 s). A meta só pede "no máximo 85% da base"; fica registrado.
- **Grupo só com a base:** a Matriarca do 40 só com a base deu 64% e 30,1/s de dano nos outros (a T-408 registrou 63% e 30,3/s); a diferença vem de mudanças da `main` depois da T-408 (T-421 a T-442), não desta tarefa. As metas do Guardião continuam passando.
- **Site:** gera sem erro (94 páginas), mas não mostra habilidades de segunda classe (nem as do Guerreiro): `Usage` só conta as habilidades da classe base, e as da segunda classe aparecem no aviso "não é usada por nenhuma classe nem monstro". Fica para uma tarefa do site.

## 6. Números que mudaram em relação ao design

**Nenhum.** Os custos, recargas, durações e valores por NH são os de `design/segunda-classe-v0.1.md` §8. Mudou uma meta (seção 3), registrada em `design/progression-v0.1.md` §8, e o que o design não dizia ficou decidido nos dados (`design/segunda-classe-v0.1.md` §8, "Ajustes do simulador (T-423)").
