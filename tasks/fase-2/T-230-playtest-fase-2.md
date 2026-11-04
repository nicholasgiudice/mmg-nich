# T-230 — Playtest da fase 2

- **Ambiente:** Windows
- **Modelo:** Humano
- **Depende de:** T-206, T-207, T-213, T-215, T-221, T-222, T-223
- **Libera:** T-240
- **Áreas que pode alterar:** `tasks/reports/`

## Contexto

Fecha a fase 2 e as pendências do playtest online (`tasks/reports/playtest-online-v0.1.md`): teste com quatro pessoas reais e ping alto.

## Requisitos

1. Criar conta e personagem, subir do nível 1 ao 4–5 nos Campos, equipar o que cair, gastar pontos.
2. Com 2–4 pessoas: jogar junto nos Campos e fazer a Matriarca (instância de encontro) no nível 7–8.
3. Anotar: tempo para subir de nível, se a queda de itens empolga, se a distribuição de pontos é clara, ping de cada pessoa, problemas.

## Critérios de aceite

- `tasks/reports/playtest-v0.2.md` com respostas, problemas e a decisão: seguir para a fase 3 (T-240) ou corrigir antes.

## Resultado

Concluída em 2026-11-04. Relatório em `tasks/reports/playtest-v0.2.md`.

**Decisão: seguir para a fase 3 (T-240)**, que deve começar por um bloco de correções antes do conteúdo novo.

- **Testado:** conta, personagem, progressão nos Campos, equipamento, pontos, grupo e Matriarca, com 2 pessoas. O ping ficou estável.
- **Bugs:** cooldown zera ao trocar de zona; corpos das aranhas da Matriarca não somem; um monstro que pega o alvo puxa monstros distantes, fora da tela.
- **Balanceamento:** cai item demais; a progressão está rápida (aumentar o XP por nível, sem aumentar o nível máximo agora).
- **Pedidos para a fase 3:** lista de batalha para escolher o alvo, grupo com divisão de XP, ouro que cai dos monstros, NPCs vendedores de consumíveis (a poção deixa de ser habilidade), consumíveis na barra de atalhos, mapas bem maiores. Moeda e lojas são economia, que o `AGENTS.md` deixa fora de escopo salvo pedido explícito: a T-240 precisa autorizá-las de forma explícita nas tarefas.

**Desvios:** 2 pessoas em vez de 2–4; o teste com 4 pessoas (pendente desde a T-111) continua em aberto. Ping sem números por pessoa.

**Pendências:** quatro pessoas reais, ping alto de verdade, confirmar as correções no próximo playtest.
