# T-408 — Dados: habilidades do Guardião e do Berserker

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** T-402, T-403, T-407
- **Libera:** T-410, T-412
- **Áreas que pode alterar:** `data/` (habilidades, estados, áreas, `classes.json`), `tests/Game.Design.Tests/`, `tests/Game.Data.Tests/`, `design/segunda-classe-v0.1.md` (só números ajustados pelo simulador), `design/skills-v0.1.md` (seção nova do Guerreiro), `tasks/reports/`

## Contexto

As 10 habilidades estão em `design/segunda-classe-v0.1.md`, seção 5, com custo, recarga e todos os NH. Aprovadas pelo designer em 2026-11-20 (Sede de Sangue como passiva).

- **Guardião:** Interceptação, Golpe de Escudo, Bastião, Inabalável, Juramento (assinatura).
- **Berserker:** Investida, Fúria, Execução, Sede de Sangue (passiva), Tempestade de Aço (assinatura).

As mecânicas novas foram feitas na T-406 (desvio de dano, avançar até aliado, imunidade a empurrão, ameaça multiplicada) e na T-407 (dano pela vida do alvo, zerar recarga ao matar, vida pelo dano, área que acompanha). A segunda classe e as passivas, na T-403. As metas de segunda classe, na T-402 (§8 do design de progressão).

**Base:** a T-406 e a T-407 sobem o protocolo e só entram na `main` na rodada da seção 6f de `tasks/README.md`. Se essa rodada ainda não tiver entrado, crie a branch a partir da branch da T-407 e mescle nela a T-403. Registre a base no Resultado.

## Requisitos

1. Arquivos de dados das habilidades (sugestão: `data/abilities_guardian.json` e `data/abilities_berserker.json`), com todos os NH, e os estados e áreas que elas usam. Ids no padrão `guardian.interception`, `berserker.blood_thirst`; textos em português.
2. `data/classes.json`: as duas segundas classes do Guerreiro com as suas 5 habilidades, na ordem da tabela do design.
3. **Testes de design** com o simulador (perfis de segunda classe da T-403), cobrindo as metas da T-402 para o nível 21, 30 e 40. Se uma meta falhar, ajustar os números dentro do espírito do design e registrar o antes e depois em `design/segunda-classe-v0.1.md` e no relatório.
4. Relatório `tasks/reports/balance-v0.19.md` (ou o próximo número livre) com o resultado dos testes e os ajustes.
5. `design/skills-v0.1.md`: seção nova com as tabelas finais do Guardião e do Berserker (ou link para o design da segunda classe, se os números forem os mesmos).

## Critérios de aceite

- os dados carregam e validam;
- cada habilidade tem um teste de Core ou de design que mostra o efeito principal no NH1 e no NH5 (por exemplo, a Interceptação desvia 30% no NH1 e 40% no NH5);
- testes de design das metas da segunda classe passam;
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
