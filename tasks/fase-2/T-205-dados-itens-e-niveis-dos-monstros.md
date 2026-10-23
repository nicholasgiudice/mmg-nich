# T-205 — Dados: itens, tabelas de queda e níveis dos monstros

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** T-203, T-204
- **Libera:** T-206, T-215
- **Áreas que pode alterar:** `data/items.json`, `data/monsters.json`, `data/encounters.json`, `tests/Game.Data.Tests/`

## Contexto

Conteúdo da §2 e §6 de `design/progression-v0.1.md` sobre a mecânica das T-203/T-204.

## Requisitos

1. Itens base: armas de cada classe (espada, machado, maça, arco, cajado, cetro), armaduras pesada/média/leve para cabeça, peito, mãos e pés, acessórios — com as fórmulas da §6.
2. Afixos e raridades com os valores da §6; tabelas de queda por categoria (normal 8%, elite 40%, chefe 100% com 2 itens).
3. Níveis e categorias dos monstros conforme a tabela "Nível por área" (§2): Rato 1, Lobo 2–3, Salteador 3–4, Aranha 5, Matriarca 8 (chefe), Guarda Esquelético 9 etc.
4. Equipamento inicial de cada classe (nível do item 2) igual à calibração.

## Critérios de aceite

- teste de conteúdo: equipamento inicial reproduz o perfil nível 1; toda tabela de queda referencia itens existentes; nenhum monstro sem nível;
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
