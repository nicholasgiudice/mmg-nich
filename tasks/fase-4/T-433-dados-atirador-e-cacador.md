# T-433 — Dados: habilidades do Atirador de Elite e do Caçador

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** T-431, T-432
- **Libera:** T-434
- **Áreas que pode alterar:** `data/` (habilidades, estados, áreas, `classes.json`), `tests/Game.Sim/` (só a estratégia do bot para as habilidades novas), `tests/Game.Design.Tests/`, `tests/Game.Data.Tests/`, `design/segunda-classe-v0.1.md` (só números ajustados pelo simulador), `design/progression-v0.1.md` (§8, metas do Arqueiro), `design/skills-v0.1.md` (seção nova do Arqueiro), `tasks/reports/`
- **Branch:** sai da `main` com a T-431 e a T-432 integradas. É integrada na `main` (seção 6h de `tasks/README.md`).

## Contexto

As 10 habilidades estão em `design/segunda-classe-v0.1.md`, seção 6, com custo, recarga e todos os NH. Aprovadas pelo designer em 2026-11-22:

- **Atirador de Elite:** Flecha Perfurante, Tiro Debilitante, Mira Firme (passiva), Tiro Certeiro (**sem conjuração**), Olho de Falcão (assinatura, **15/17/20 s**).
- **Caçador:** Flecha Envenenada (passiva), Ferida Aberta, Nuvem Tóxica, Flecha Dispersa, Praga (assinatura).

As mecânicas novas são da T-431 (dano menor atrás na linha, crítico pela distância, alcance de um estado, ignorar armadura, crítico garantido) e da T-432 (efeito em todo acerto, veneno pelo NH da passiva, dano maior se o alvo andar, copiar estados, estado que pula). O Guerreiro serve de exemplo: `data/abilities_guardian.json`, `tests/Game.Data.Tests/WarriorSecondClassContentTests.cs` e `tests/Game.Design.Tests/SecondClassDesignTests.cs` (T-408, `tasks/reports/balance-v0.19.md`).

**Atenção aos números que o designer mudou:** o dano do Tiro Certeiro foi feito para um tiro que pedia 1,5 s parado, e o Olho de Falcão passou de 6–8 s para 15–20 s. Os dois juntos são a rajada do Atirador. Se passarem das metas, ajustar primeiro o dano do Tiro Certeiro ou o custo, e não a duração que o designer pediu. Registrar o antes e o depois.

## Requisitos

1. Arquivos de dados das habilidades (sugestão: `data/abilities_marksman.json` e `data/abilities_hunter.json`), com todos os NH, e os estados e áreas que elas usam, no mesmo arquivo da classe. Ids no padrão `marksman.piercing_arrow`, `hunter.poison_arrow`; textos em português.
2. `data/classes.json`: as duas segundas classes do Arqueiro (`second_class.marksman`, `second_class.hunter`) com as suas 5 habilidades, na ordem da tabela do design.
3. **Bot do simulador:** a estratégia do Arqueiro usa as habilidades novas de um jeito razoável (manter distância de 6+ tiles com o Atirador; Tiro Certeiro e Olho de Falcão juntos; Nuvem Tóxica e Flecha Dispersa no Caçador quando há 2 ou mais monstros).
4. **Metas novas** em `design/progression-v0.1.md`, §8 ("Metas da segunda classe"). Se o simulador mostrar que uma delas não faz sentido, ajustar a meta e registrar o motivo no relatório:
   - **Atirador × Arqueiro só com a base**, solo, contra um alvo único forte (elite ou chefe do nível): vence ≥ 85% e em no máximo **85%** do tempo do Arqueiro só com a base; no grupo típico pode ser mais lento;
   - **Caçador × Arqueiro só com a base**, solo, grupo típico (3 monstros normais do nível): vence ≥ 85% e em no máximo **85%** do tempo; contra alvo único pode ser mais lento;
   - **Olho de Falcão:** o dano do Atirador nos 20 s dela não passa de **2,5×** o dano dele nos mesmos 20 s sem ela (meta proposta; ajustar se o simulador mostrar outra coisa);
   - as metas gerais da segunda classe (nível 21, distribuição de pontos, diferença entre segundas classes, com as do Guerreiro e as do Clérigo que já estiverem na `main`).
5. **Testes de design** com o simulador nos níveis 21, 25, 30 e 40. Se uma meta falhar, ajustar os números dentro do espírito do design e registrar o antes e o depois em `design/segunda-classe-v0.1.md` ("Ajustes do simulador", como na seção 5) e no relatório.
6. Relatório `tasks/reports/balance-v0.21.md` (ou o próximo número livre) com o resultado dos testes e os ajustes.
7. `design/skills-v0.1.md`: seção do Arqueiro com link e tabela de custos e recargas, como a do Guerreiro.
8. Conferir que o site (`src/Game.Site`, `scripts/build-site.sh`) gera as páginas das habilidades novas sem erro.

## Critérios de aceite

- os dados carregam e validam;
- cada habilidade tem um teste de efeito com os dados reais no NH1 e no NH5 (assinaturas no NH3). Por exemplo: a Flecha Perfurante dá 60% e 80% atrás; a Mira Firme dá +6% e +12% de crítico a 6+ tiles; o Tiro Certeiro sai sem conjuração; o Olho de Falcão dura 15 s e 20 s; a Flecha Envenenada chega a 3 e a 5 pilhas;
- todo custo cabe na Mana do Arqueiro no nível 40;
- testes de design das metas passam;
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
