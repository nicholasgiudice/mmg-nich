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

_(preencher ao concluir)_
