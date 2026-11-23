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

_(preencher ao concluir)_
