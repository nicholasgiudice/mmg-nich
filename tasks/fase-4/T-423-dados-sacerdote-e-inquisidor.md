# T-423 — Dados: habilidades do Sacerdote e do Inquisidor

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** T-421, T-422
- **Libera:** T-424
- **Áreas que pode alterar:** `data/` (habilidades, estados, áreas, `classes.json`), `tests/Game.Sim/` (só a estratégia do bot para as habilidades novas), `tests/Game.Design.Tests/`, `tests/Game.Data.Tests/`, `design/segunda-classe-v0.1.md` (só números ajustados pelo simulador), `design/progression-v0.1.md` (§8, metas do Clérigo), `design/skills-v0.1.md` (seção nova do Clérigo), `tasks/reports/`
- **Branch:** sai da `main` com a T-421 e a T-422 integradas. É integrada na `main` (seção 6g de `tasks/README.md`).

## Contexto

As 10 habilidades estão em `design/segunda-classe-v0.1.md`, seção 8, com custo, recarga e todos os NH. Aprovadas pelo designer em 2026-11-22:

- **Sacerdote:** Oração Restauradora, Bênção, Graça Veloz, Inspiração, Santuário (assinatura). A Inspiração entrou no lugar do Elo da Vida.
- **Inquisidor:** Martelo da Justiça, Consagração, Selo da Luz, Julgamento, Ira Divina (assinatura). Nenhuma das duas segundas classes tem passiva.

As mecânicas novas são da T-421 (cura por segundo num estado, atributos e regeneração de Mana vindos de um estado, deixar quem lançou de fora, vida mínima de 1, efeito ao acabar, cura pela vida máxima) e da T-422 (área que fere e cura, vida pelo dano para quem bate no alvo marcado, cura pelo dano do golpe, ataque básico trocado, cura dos aliados pelo dano). A segunda classe e as metas vêm da T-403 e da T-402. O Guerreiro serve de exemplo: `data/abilities_guardian.json`, `data/abilities_berserker.json`, `tests/Game.Data.Tests/WarriorSecondClassContentTests.cs` e `tests/Game.Design.Tests/SecondClassDesignTests.cs` (T-408, `tasks/reports/balance-v0.19.md`).

## Requisitos

1. Arquivos de dados das habilidades (sugestão: `data/abilities_priest.json` e `data/abilities_inquisitor.json`), com todos os NH, e os estados e áreas que elas usam, no mesmo arquivo da classe. Ids no padrão `priest.inspiration`, `inquisitor.seal_of_light`; textos em português.
2. `data/classes.json`: as duas segundas classes do Clérigo (`second_class.priest`, `second_class.inquisitor`) com as suas 5 habilidades, na ordem da tabela do design.
3. **Bot do simulador:** a estratégia do Clérigo usa as habilidades novas de um jeito razoável (Bênção e Inspiração no começo da luta de grupo e quando acabam; Santuário quando um aliado está para cair; Selo da Luz no alvo do grupo; Ira Divina com 2 ou mais monstros perto).
4. **Metas novas** em `design/progression-v0.1.md`, §8 ("Metas da segunda classe"), propostas abaixo. Se o simulador mostrar que uma delas não faz sentido, ajustar a meta e registrar o motivo no relatório:
   - **Inquisidor × Clérigo só com a base**, solo, grupo típico: vence ≥ 85% e em no máximo **85%** do tempo do Clérigo só com a base (a mesma meta do Berserker);
   - **Inquisidor**, solo: a diferença entre classes no 40 fica abaixo de **1,8×** também contra o Lobo (hoje o Clérigo só com a base está em 1,92×);
   - **Sacerdote × Clérigo só com a base**, grupo de 4 contra um chefe ou elite do nível: a vitória não cai e os **outros membros** causam pelo menos **10% mais dano por segundo** (Bênção e Inspiração);
   - **Sacerdote**, solo: vence ≥ 85% o grupo típico; pode ser mais lento que o Clérigo só com a base;
   - as metas gerais da segunda classe (nível 21, distribuição de pontos, diferença entre segundas classes, agora com as quatro do Guerreiro e do Clérigo).
5. **Testes de design** com o simulador nos níveis 21, 25, 30 e 40. Se uma meta falhar, ajustar os números dentro do espírito do design e registrar o antes e o depois em `design/segunda-classe-v0.1.md` ("Ajustes do simulador", como na seção 5) e no relatório.
6. Relatório `tasks/reports/balance-v0.20.md` (ou o próximo número livre) com o resultado dos testes e os ajustes.
7. `design/skills-v0.1.md`: seção do Clérigo com link e tabela de custos e recargas, como a do Guerreiro.
8. Conferir que o site (`src/Game.Site`, `scripts/build-site.sh`, gerado de `data/*.json`) gera as páginas das habilidades novas sem erro.

## Critérios de aceite

- os dados carregam e validam;
- cada habilidade tem um teste de efeito com os dados reais no NH1 e no NH5 (assinaturas no NH3). Por exemplo: a Inspiração dá +4,0 de Mana/s no NH1 e +7,2 no NH5 com INT 62 e não pega no Sacerdote; o Santuário deixa o aliado com 1 de vida e, no NH3, cura 20% ao acabar; o Selo da Luz cura 10% e 20%; a Ira Divina acerta em área e cura os aliados perto;
- todo custo cabe na Mana do Clérigo no nível 40;
- testes de design das metas passam;
- `./scripts/check.sh` completo passa.

## Resultado

Feito em 2026-11-22/23, **no Windows** (a tarefa não usa tela), branch `task/T-423-dados-sacerdote-e-inquisidor`. Relatório: `tasks/reports/balance-v0.20.md`.

### Base

O worktree nasceu em `8f5c228` (só a T-421). A branch foi criada da `main` local em **`c6fda57`** (T-421, T-422, T-431 e T-442 integradas; `git merge-base --is-ancestor b9e91ca HEAD` dá ok). A `main` andou depois para `9d3a372` (T-432, Core do Caçador), que não mexe nos dados do Clérigo; não rebaseei. Na `main` em `c6fda57`, a seção `## Resultado` da T-422 está vazia (o texto existe no commit `b9e91ca`, mas o merge da T-421 por cima o perdeu): pendência do integrador.

### O que foi feito

- **Dados:** `data/abilities_priest.json` e `data/abilities_inquisitor.json`, com as 10 habilidades e todos os NH, e os estados (Oração, Bênção, Graça Veloz, Passo Livre, Inspiração, Santuário, Selo da Luz, Ira Divina), as áreas da Consagração (`area.consecration_harm_N` e `area.consecration_mend_N`) e os golpes da Ira (`inquisitor.divine_wrath_strike_1` e `_3`) no mesmo arquivo da classe. Ids: `priest.restoring_prayer`, `priest.blessing`, `priest.swift_grace`, `priest.inspiration`, `priest.sanctuary`, `inquisitor.hammer_of_justice`, `inquisitor.consecration`, `inquisitor.seal_of_light`, `inquisitor.judgment`, `inquisitor.divine_wrath`.
- `data/classes.json`: `second_class.priest` e `second_class.inquisitor` com as cinco na ordem do design.
- **Bot** (`src/Game.Bots/ConfigurableBot.GroupStatus.cs`; `ConfigurableBot` virou `partial`, com uma chamada em `Plan`): as habilidades que só aplicam estados das mecânicas novas seguem o estado, não o id. Bênção, Inspiração e Graça Veloz no começo da luta (inimigo a até `buffRange` = 10) e quando o estado acaba; Santuário com um aliado no raio (ou o próprio) com até `emergencyBelow` = 30% de vida; Oração com alguém ferido no raio (`healBelow`); Ira Divina com `minAoe` (2) inimigos a até 2 do alvo; Selo da Luz no alvo do bot (o do grupo). As habilidades antigas não mudam de comportamento (as metas da T-408 dão os mesmos números).
- **Simulador:** `DamageDealtByClass` em `RunResult` (dano causado por classe do jogador), para o dano dos outros membros do grupo.
- **Testes de efeito** (`tests/Game.Data.Tests/ClericSecondClassContentTests.cs`, 24 casos, dados reais): cada habilidade no NH1 e no NH5 (assinaturas no NH1 e NH3). Oração 29/s e 52/s com INT 62, 8 e 10 s, raio 6/8; Bênção +7 e +13 em todos os atributos, 15 e 20 s, raio 6/8; Graça Veloz +15%/+25%, 30 s, tira a lentidão e dá imunidade de 10 s no NH5; Inspiração +4,0 e +7,2 de Mana/s com INT 62, não pega no Sacerdote, 12 e 15 s; Santuário deixa o aliado com 1 de vida, 4 e 6 s, e no NH3 cura 20% ao acabar; Martelo 10/18 sagrado, atordoa 1/1,5 s, arco de 120° no NH5; Consagração 6 pulsos de 4 de dano e 3 de cura (8 de 7 e 5 no NH5, raio 4), dano só nos monstros e cura só nos aliados; Selo cura 10% e 20% do dano de quem bate, 8 e 10 s; Julgamento 20/36, alcance 3 (5 do NH3), cura 20% do dano no NH5; Ira Divina: o ataque básico acerta o alvo e quem está a até 2, é o Golpe Sagrado × 1,5/× 1,8 e cura os aliados perto em 10%/15% de cada acerto, 10 e 15 s. Mais: ordem das classes, Mana do 40 para todo custo, personagem do 40 com todos os NH.
- **Testes de design:** `SecondClassDesignTests.cs` ficou genérico por classe base (`SecondClassSpec` em `Specs`: classe base, par principal, opções solo e de grupo, limite da diferença); `SoloOf(classe)` cria os Três Lobos com o jogador da classe. Metas do Clérigo em `tests/Game.Design.Tests/ClericSecondClassDesignTests.cs` (11 casos); as gerais (nível 21, distribuição de pontos, diferença entre segundas classes) rodam nas quatro. Passam todas (35 casos).
- `design/progression-v0.1.md` §8: metas do Clérigo e a meta revista da diferença entre segundas classes. `design/segunda-classe-v0.1.md` §8: "Ajustes do simulador (T-423)". `design/skills-v0.1.md`: seção do Clérigo com link e tabela de custos e recargas. `tests/Game.Sim/README.md` e `BotOptions`: opções `emergencyBelow` e `buffRange`.
- **Site:** o gerador (o comando de `scripts/build-site.sh`, numa pasta temporária) roda sem erro, 94 páginas. As habilidades de segunda classe (as do Guerreiro também) ficam fora do site: o `Usage` do site só conta a classe base.

### Resultado das metas (detalhes no relatório, §2)

| Meta | Resultado |
|---|---|
| Inquisidor × Clérigo só com a base, Três Lobos: ≥ 85% e ≤ 85% do tempo | passa: 69% (25), 52% (30), 27% (40) |
| Inquisidor no 40 contra o Lobo: ≤ 1,8× a média | passa: 1,08× (8,4 s; média 7,8 s) |
| Sacerdote no grupo (Matriarca): vitória não cai | passa: 77% → 98% (30), 64% → 98% (40) |
| Sacerdote no grupo: outros causam ≥ 10% mais por segundo vivo | passa: +27% (30), +31% (40) |
| Sacerdote solo ≥ 85% | passa: 100% (41,7–43,1 s; base 45,9–48,9 s) |
| Nível 21 e distribuição de pontos | passam nas quatro segundas classes |
| Diferença entre segundas classes | passa com a meta revista (abaixo) |

### Números que mudaram em relação ao design

**Nenhum.** Custos, recargas, durações e valores por NH são os de `design/segunda-classe-v0.1.md` §8.

### Meta ajustada

**Diferença entre segundas classes.** Com as quatro contra os Três Lobos, o Sacerdote fica a 2,8–3,4× da média das outras e o Inquisidor no 25 a 1,83×: o Clérigo só com a base já leva 46–49 s contra três Lobos (3× o Guerreiro), e o design aceita que o Sacerdote seja mais lento que ele. Tirar o Sacerdote da média derruba o Guardião (2,6× no 40). Revista com a lógica da diferença entre classes: cada segunda classe no **encontro solo da classe base** (Guerreiro: Três Lobos; Clérigo: Guarda Esquelético), limite 1,6×, as do **Clérigo até 1,8×**, e o **Sacerdote** na média, sem limite (as metas dele são a vitória solo e o grupo). Medido: no máximo 1,51× (Inquisidor, 25) e 1,58× (Guardião, 40). Relatório §3.

### Decisões

- **Bênção:** aplicada nos aliados primeiro e no Sacerdote por último; na ordem inversa, os aliados ganhavam o bônus pela INT já abençoada (+8 em vez de +7 no perfil do 21).
- Santuário, Oração, Bênção e Graça Veloz valem também para o Sacerdote; a Inspiração não (design).
- Graça Veloz NH3+: tira os estados com a tag `slow` de todos no raio; NH5: estado à parte "Passo Livre" (10 s, imune a `slow`).
- Contra mortos-vivos e corrompidos: +25% nos dois como regra do Inquisidor; Consagração +50% e Julgamento +50% (NH5 +75%) contra mortos-vivos, com +25% contra corrompidos.
- Ira Divina: golpe = Golpe Sagrado da base (1 × ARMA + 0,6 × INT, mesmos bônus) × 1,5 no NH1 e NH2 e × 1,8 no NH3, alcance 6, recarga 1,1 s, no alvo e em todos a até 2 dele; um estado para o NH1 e o NH2 (só a duração muda, no efeito) e outro para o NH3.
- Selo da Luz com as tags `debuff` e `seal`.
- Medidas do grupo por **segundo vivo** dos outros membros, como a do Guardião.
- No grupo, o Sacerdote abre com Bênção e Inspiração (`order`); o Inquisidor usa a rotação da barra (mudar a ordem não muda o resultado).

### Desvios (fora das áreas listadas, o mínimo)

- `src/Game.Bots/` (`ConfigurableBot.GroupStatus.cs` novo; `partial` e uma chamada em `ConfigurableBot.cs`; comentário em `BotOptions.cs`): a estratégia do bot mora em `src/Game.Bots`, não em `tests/Game.Sim` (a T-408 fez o mesmo). Arquivo próprio para não conflitar com as tarefas do Arqueiro e do Mago.
- `tests/Game.Sim/EncounterRunner.cs`: `DamageDealtByClass`.
- Nada em `src/Game.Core` nem no schema.

### Pendências

- **Sacerdote forte no grupo:** leva a Matriarca de 64–77% a 98% de vitória e corta a luta quase pela metade (o Guardião: 72–75%). Quase tudo vem da Bênção (relatório §4). As metas passam; se o designer achar demais, a alavanca é a Bênção.
- **Diferença entre segundas classes:** o Guardião fica a 1,58× no 40 (limite 1,6×), e com as segundas classes do Arqueiro e do Mago (T-433, T-443) as bases deles saem da média: rever a meta nessas tarefas. Para entrar, basta pôr a classe em `SecondClassDesignTests.Specs`.
- **Site:** não mostra habilidades de segunda classe (nem as do Guerreiro); tags novas sem rótulo em `site/content/site.json` (`sanctuary`, `seal`, `blessing`, ...); faltam textos para `stats`, `undying`, `onExpire`, `attackerLifesteal`, `basicAttack`, `selfHeal` e `allyHeal`.
- T-424: ícones e efeitos; o cliente não mostra a troca do ataque básico e desenha a Consagração como dois círculos.
- Resultado da T-422 perdido na `main` (acima).

### Verificação

`./scripts/check.sh` **completo** passou no Windows (2026-11-23): formatação, build sem avisos e todos os testes: 555 + 457 + 128 + 117 + 75 + 74 + 39 + 15 + 2 nos projetos rápidos (os ignorados são os que pedem banco, como antes) e os **263** de `Game.Design.Tests` em 11 min 21 s (o PC estava dividido com outros agentes). `dotnet format MmoRpg.sln` rodado antes do commit.
