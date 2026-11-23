# T-443 — Dados: habilidades do Piromante e do Arcanista

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** T-441, T-442
- **Libera:** T-444
- **Áreas que pode alterar:** `data/` (habilidades, estados, áreas, `classes.json`), `tests/Game.Sim/` (só a estratégia do bot para as habilidades novas), `tests/Game.Design.Tests/`, `tests/Game.Data.Tests/`, `design/segunda-classe-v0.1.md` (só números ajustados pelo simulador), `design/progression-v0.1.md` (§8, metas do Mago), `design/skills-v0.1.md` (seção nova do Mago), `tasks/reports/`
- **Branch:** sai da `main` com a T-441 e a T-442 integradas. É integrada na `main` (seção 6i de `tasks/README.md`).

## Contexto

As 10 habilidades estão em `design/segunda-classe-v0.1.md`, seção 7, com custo, recarga e todos os NH. Aprovadas pelo designer em 2026-11-22:

- **Piromante:** Combustão, Rajada de Chamas, Chão em Chamas, Bola de Fogo, Meteoro (assinatura), e o estado Queimadura.
- **Arcanista:** Raio Arcano, Marca Arcana, Orbes Arcanos, Sifão Arcano (**regeneração de Mana por 5 s**, não Mana na hora), Feixe do Vazio (assinatura). Nenhuma das duas segundas classes tem passiva.

As mecânicas novas são da T-441 (Queimadura por NH, dano só em quem queima, explodir no primeiro acerto, área depois do impacto) e da T-442 (canalização com pulsos, dano que sobe, andar devagar canalizando, efeito ao fim da canalização, orbes, Sifão). O Guerreiro serve de exemplo: `data/abilities_guardian.json`, `tests/Game.Data.Tests/WarriorSecondClassContentTests.cs` e `tests/Game.Design.Tests/SecondClassDesignTests.cs` (T-408, `tasks/reports/balance-v0.19.md`).

**Atenção à Mana do Piromante:** quatro habilidades de 35 a 50 de Mana, com recargas de 6 a 14 s, mais a Explosão Elemental da base. Se o bot ficar sem Mana a maior parte da luta, ajustar primeiro os custos, como a T-408 fez com o Guerreiro.

## Requisitos

1. Arquivos de dados das habilidades (sugestão: `data/abilities_pyromancer.json` e `data/abilities_arcanist.json`), com todos os NH, e os estados e áreas que elas usam, no mesmo arquivo da classe. Ids no padrão `pyromancer.fireball`, `arcanist.arcane_beam`; textos em português.
2. `data/classes.json`: as duas segundas classes do Mago (`second_class.pyromancer`, `second_class.arcanist`) com as suas 5 habilidades, na ordem da tabela do design.
3. **Bot do simulador:** a estratégia do Mago usa as habilidades novas de um jeito razoável (no Piromante, queimar antes da Combustão e o Meteoro com 3 ou mais monstros; no Arcanista, Marca Arcana cheia, Orbes e Raio Arcano no mesmo alvo e o Sifão quando a Mana está baixa).
4. **Metas novas** em `design/progression-v0.1.md`, §8 ("Metas da segunda classe"). Se o simulador mostrar que uma delas não faz sentido, ajustar a meta e registrar o motivo no relatório:
   - **Piromante × Mago só com a base**, solo, grupo típico (3 monstros normais do nível): vence ≥ 85% e em no máximo **85%** do tempo do Mago só com a base; contra alvo único pode ser mais lento;
   - **Arcanista × Mago só com a base**, solo, contra um alvo único forte (elite ou chefe do nível): vence ≥ 85% e em no máximo **85%** do tempo; no grupo típico pode ser mais lento;
   - **Mana:** nenhum dos dois fica sem Mana em mais da metade das lutas do grupo típico (meta proposta);
   - as metas gerais da segunda classe (nível 21, distribuição de pontos, diferença entre segundas classes, agora com as oito).
5. **Testes de design** com o simulador nos níveis 21, 25, 30 e 40. Se uma meta falhar, ajustar os números dentro do espírito do design e registrar o antes e o depois em `design/segunda-classe-v0.1.md` ("Ajustes do simulador", como na seção 5) e no relatório.
6. Relatório `tasks/reports/balance-v0.22.md` (ou o próximo número livre) com o resultado dos testes e os ajustes.
7. `design/skills-v0.1.md`: seção do Mago com link e tabela de custos e recargas, como a do Guerreiro.
8. Conferir que o site (`src/Game.Site`, `scripts/build-site.sh`) gera as páginas das habilidades novas sem erro.

## Critérios de aceite

- os dados carregam e validam;
- cada habilidade tem um teste de efeito com os dados reais no NH1 e no NH5 (assinaturas no NH3). Por exemplo: a Combustão só fere quem queima e, no NH3, não apaga a Queimadura; a Bola de Fogo explode com raio 2 e 3; o Raio Arcano sobe 15% por pulso e tem 6 e 8 pulsos; o Sifão dá +6 e +10 de Mana/s por 5 s; os Orbes são 2 e 4;
- todo custo cabe na Mana do Mago no nível 40;
- testes de design das metas passam;
- `./scripts/check.sh` completo passa.

## Resultado

Feito em 2026-11-23, **no Windows** (a tarefa não usa tela), branch `task/T-443-dados-piromante-e-arcanista`. Relatório: `tasks/reports/balance-v0.22.md`.

### Base

Começou em `3b10ce8` (T-421, T-422, T-431, T-432, T-441, T-442), foi levada para `e471604` (com a T-423) antes de qualquer commit e rebaseada para **`8b7c9ea`** (com a T-433) antes do check completo. Conflitos da última, resolvidos mantendo os dois lados: `design/skills-v0.1.md`, `design/progression-v0.1.md` §8 (metas do Arqueiro e do Mago, uma depois da outra), `src/Game.Bots/BotOptions.cs` (opções da T-433 e da T-443) e `tests/Game.Design.Tests/SecondClassDesignTests.cs` (Atirador, Caçador, Piromante e Arcanista em `Specs`; o limite do Guardião, revisto abaixo).

### O que foi feito

- **Dados:** `data/abilities_pyromancer.json` e `data/abilities_arcanist.json`, com as 10 habilidades e todos os NH, e os estados e áreas no mesmo arquivo da classe. Ids: `pyromancer.combustion`, `pyromancer.flame_burst`, `pyromancer.burning_ground`, `pyromancer.fireball`, `pyromancer.meteor`, `arcanist.arcane_ray`, `arcanist.arcane_mark`, `arcanist.arcane_orbs`, `arcanist.arcane_siphon`, `arcanist.void_beam`. Estados: Queimadura por NH (`status.burn_1..5`, `exclusive` no grupo `pyromancer_burn`), Marca Arcana por NH (`status.arcane_mark_1..5`, pilhas 3/3/4/4/5) e o apelido `status.arcane_mark` (`byRank` pela Marca, usado pelo Sifão NH3+), Orbes por NH (`status.arcane_orbs_1..5`, `current_target` com 2/2/3/3/4 disparos), Sifão por NH (`status.arcane_siphon_1..5`, `stats.manaRegen` +6 a +10 por 5 s) e `status.void_exposed` (+15% de dano recebido, 6 s). Áreas: `area.burning_ground_1..5` e `area.meteor_flames` (o Chão em Chamas NH1 por 5 s, raio 3).
- `data/classes.json`: as cinco de cada segunda classe do Mago na ordem do design (só a entrada do Mago).
- **Testes de efeito** (`tests/Game.Data.Tests/MageSecondClassContentTests.cs`, 26 casos, dados reais): Queimadura NH1/NH5; Combustão só em quem queima (NH1 apaga, NH5 não apaga e pula para quem está a 2 tiles, que não leva o dano); Rajada 90°/120° e Queimadura de 4/6 s; Chão em Chamas 6/8 pulsos, raio 3/4; Bola de Fogo explode no primeiro com raio 2/3 e não acerta o que está na linha longe da explosão; Meteoro depois de 2 s, raio 5/6, Chão em Chamas de 5 s no NH3; Raio Arcano +15% por pulso, 6/8 pulsos, andar corta no NH1 e não no NH5; Marca 3/5 pilhas, um acerto por pilha; Orbes 2/4 no alvo atual, 6 disparos em 10 s; Sifão +6/+10 de Mana/s por 5 s, conjuração 1/0,5 s, pilha de Marca no NH5; Feixe do Vazio 8 pulsos andando e +15% no NH3. Mais: ordem das classes, Mana do nível 40 para todo custo, personagem do 40 com todos os NH.
- **Testes de design** (`tests/Game.Design.Tests/MageSecondClassDesignTests.cs`, 19 casos, e as entradas do Mago em `SecondClassDesignTests.Specs`): metas do Mago nos níveis 25, 30 e 40, uso das assinaturas no 40, e as metas gerais (nível 21, distribuição de pontos, diferença entre segundas classes) com o Piromante e o Arcanista.
- **Bot** (`src/Game.Bots/ConfigurableBot.Mage.cs`, partial nova, pelo efeito e pela mira, sem ids): Combustão só com 2+ queimando no raio; Bola de Fogo no alvo ao alcance; Chão em Chamas como área de dano; Rajada só com monstro no cone; Meteoro com 3+ no raio (opção `minAoeOf`); Marca no alvo do bot até encher, depois só renova; Orbes com o alvo ao alcance; Sifão com a Mana em até 50% (`manaRegenBelow`); canalização parada só se nenhum monstro chega antes da metade dela (`channelSafety`); canalização que deixa andar segue o movimento.
- **Simulador:** `RunResult.TicksBelowSecondClassCost` (Mana abaixo do custo da habilidade mais barata da segunda classe), para a meta de Mana.
- `design/progression-v0.1.md` §8: metas do Mago e a revisão da diferença entre segundas classes. `design/segunda-classe-v0.1.md` §7: "Ajustes do simulador (T-443)". `design/skills-v0.1.md`: seção do Mago com custos e recargas.
- **Site:** `scripts/build-site.sh` gera sem erro (94 páginas). As habilidades das segundas classes não aparecem no site (o gerador só segue a barra da classe base; vale para todas as segundas classes).

### Verificação

`./scripts/check.sh` **completo** passou no Windows, na base `8b7c9ea`: formatação, build sem avisos e todos os testes (Protocol 74, Core 590, Data 533, Client 128, Persistence 2 + 8 ignorados, Server 117 + 1 ignorado, Site 15, Sim 75, Api 39 + 7 ignorados, **Design 304 em 10 min 58 s**; os ignorados são os que pedem banco, como antes). `dotnet format MmoRpg.sln` rodado antes do commit.

### Números que mudaram em relação ao design

**Nenhum.** A Mana do Piromante não falta: o Mago chega ao 21 com ~340 de Mana e ~18/s, e nenhuma luta do grupo típico ficou "sem Mana" (0% nos três níveis, com e sem assinaturas).

### Metas (100 seeds)

| Meta | nv25 | nv30 | nv40 |
|---|---|---|---|
| Piromante / base, Três Lobos, sem assinaturas (≤ 85%) | 70% | 59% | 39% |
| Arcanista / base, Aranha Grande (elite), sem assinaturas (≤ 85%) | 74% | 77% | 66% |
| Vitória (todas as lutas do Mago medidas) | 100% | 100% | 100% |
| Sem Mana (≤ 50% das lutas) | 0% | 0% | 0% |

Nível 21: só a base 3,7 s, Piromante 3,6 s, Arcanista 3,5 s. Pontos na base ou na segunda classe (30): 100% nos quatro casos. No 40, o bot usa o Meteoro e o Feixe do Vazio em todas as lutas.

### Decisões e metas revistas

- **Metas do Mago sem as assinaturas** (Cataclismo, Meteoro, Feixe do Vazio), como no tempo para matar da T-402: com elas, toda luta do Mago acaba em 2–4 s (a assinatura decide) e a comparação vira ruído. O uso delas tem teste próprio.
- **Alvo único forte = a Aranha Grande** (elite) do nível, sozinha: o Cavaleiro Espectral e a Matriarca vencem o Mago sozinho em todos os níveis (a T-433 escolheu a mesma).
- **"Sem Mana"** = mais da metade da luta com a Mana abaixo do custo da habilidade mais barata da segunda classe (Rajada 35, Marca 15).
- **Par principal:** Bola de Fogo e Combustão (Piromante); **Marca Arcana e Orbes** (Arcanista). Com o Raio Arcano no par, o Arcanista fica em 91–92% no 25 (o Raio NH1 troca Projéteis pela canalização); do 30 em diante o Raio seria melhor (relatório §3).
- **Diferença entre segundas classes, revista de novo:** com o Mago, as oito segundas classes estão na média e nenhuma base; o Inquisidor falhava no 25 (1,89×, limite 1,8×) e o Guardião ficava em 1,97× no 40 (limite 2,0× da T-433). O **Guardião entra sem limite**, como o Sacerdote (suporte), e o **Inquisidor vai até 2,0×**. Sugestão de trocar a meta por uma que não dependa do encontro (relatório §4).

### Desvios (fora das áreas listadas, o mínimo)

- `src/Game.Bots/` (a estratégia do bot está no projeto `Game.Bots`, não em `tests/Game.Sim`): partial nova `ConfigurableBot.Mage.cs`; em `ConfigurableBot.cs`, uma chamada em `Plan` e o caso da canalização que deixa andar em `Think`; em `AbilityInfo.cs`, `NeedsStanding` também para canalização que andar corta; `BotOptions.cs`, documentação das opções. Nenhuma habilidade antiga usa canalização, `if_status`, `explodeRadius`, `current_target` ou cone de alcance ≥ 3, então os testes de antes não mudam.
- `tests/Game.Sim/EncounterRunner.cs`: `TicksBelowSecondClassCost`.
- Nada em `src/Game.Core` nem no schema.

### Pendências

- **Alerta para o designer:** o Piromante passa com muita folga (39–70% do tempo da base; o Berserker ficou em 74–82%) e é mais rápido que o Arcanista também contra alvo único (3,9 × 9,2 s na elite no 40), ao contrário do papel das duas (relatório §5). Nada mudou porque nenhuma meta falha.
- O Raio Arcano NH1–2 rende pouco (a canalização troca o ataque básico); se o designer quiser o Raio como principal desde o 24, reforçar o NH1–2.
- A meta "diferença entre segundas classes" deveria ser trocada por uma que não dependa da dificuldade do encontro solo de cada classe.
- Site: as segundas classes (todas) não aparecem; as etiquetas novas (`burn`, `arcane_mark`, `arcane_orbs`, `mana_regen`, `void_exposed`) não têm rótulo em `site/content/site.json`. O site também não descreve `if_status`, `explodeRadius`, canalização e `current_target` (pendência da T-441/T-442).
- T-444: ícones, efeitos e a previsão de movimento canalizando no cliente.
