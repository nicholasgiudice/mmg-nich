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

**Feito** (VPS, 2026-10-24). `./scripts/check.sh` completo passa.

A maior parte do conteúdo já tinha entrado nas tarefas anteriores. Os níveis e categorias dos monstros vieram com a T-203. Itens base, afixos, raridades, tabelas de queda e equipamento inicial vieram com a T-204, que precisava deles para ter queda e equipamento inicial de verdade. Esta tarefa conferiu esse conteúdo contra o design, completou o que faltava e fixou tudo em testes.

**Dados**
- `data/items.json`: novo acessório `accessory.amulet` ("Amuleto", mesma fórmula do anel), porque o requisito fala em "acessórios". Ele entrou nas três tabelas de queda. São 21 itens base.
- Monstros (sem mudança): Rato 1, Lobo 2, Salteador 3, Aranha pequena 5, Aranha grande 6 (elite), Matriarca 8 (chefe), Guarda Esquelético 9. Onde o design dá uma faixa (Lobo 2–3, Salteador 3–4), a definição fica no menor nível. Variar o nível por surgimento é da T-215.
- `data/encounters.json` não precisou de mudança.

**Testes** (`tests/Game.Data.Tests/ProgressionContentTests.cs`)
- O equipamento inicial reproduz o perfil nível 1 das 4 classes: dano de arma, atributos, vida e Mana, sem armadura.
- Toda classe tem item para os 6 espaços, e cada espaço de armadura tem um item de cada tipo.
- As fórmulas das bases e dos afixos e a quantidade de afixos por raridade seguem a §6.
- As tabelas de queda têm as chances e pesos do design, só apontam para itens que existem, não repetem item, e todo item cai de alguma tabela.
- Nenhum monstro está sem `level` explícito no JSON. Os níveis e categorias batem com a tabela "Nível por área" (§2), e todo monstro está nessa tabela.

**Pendências**
- Itens épicos: o design pede uma lista própria, só da Matriarca e do Vigia, e um efeito próprio. As tabelas são por categoria, então qualquer chefe pode soltar épico, e o épico é só um raro com outra cor. Uma lista por monstro precisa de mudança no Core (`DropTableDefinition`), fora das áreas desta tarefa.
- Monstros da Floresta e das Ruínas que ainda não existem (Fada, Ent, Sacerdote Morto, Cavaleiro Espectral) ficam para a fase 3.
