# T-031 — Design de progressão: atributos, equipamento e reformulação das habilidades

- **Ambiente:** qualquer (documento)
- **Modelo:** Humano + **Forte**
- **Depende de:** T-025
- **Libera:** tarefas de atributos/escala no Core e de reformulação das habilidades (fase 2)
- **Áreas que pode alterar:** `design/`, `tasks/`

## Contexto

O playtest (`tasks/reports/playtest-v0.1.md`) aprovou o combate como base, mas o designer decidiu que **todas as habilidades serão reformuladas** e que elas devem ficar mais fortes com **pontos de atributos e equipamentos**. Hoje cada efeito tem só um valor `base` fixo; o Core não tem atributos nem escala. `tech/stack-proposal-v0.1.md` já prevê o formato `"scaling": { "attack": 1.2 }` nos efeitos. Itens e equipamento estão no esboço da fase 2.

## Objetivo

Um documento de design (`design/progression-v0.1.md`) que permita a outras tarefas implementar atributos, equipamento e as novas habilidades sem decisões de design em aberto.

## Requisitos

1. **Atributos**: lista (ex.: Força, Agilidade, Inteligência, Espírito, Vitalidade), o que cada um afeta (dano físico/mágico, cura, vida, Mana, regeneração, velocidade de ataque...), valores iniciais por classe, como o jogador ganha pontos (nível? marcos?) e se a distribuição é livre ou guiada.
2. **Equipamento**: espaços (arma, armadura, acessórios...), que atributos/efeitos os itens dão, raridade, e o que cai de monstros e bosses do primeiro mapa (`design/first-map-v0.1.md`).
3. **Fórmulas**: dano, cura, escudo e defesa/resistência com escala por atributo (ex.: `base + escala × atributo`), crítico (se houver), e como o nível/equipamento esperado em cada área do mapa se traduz em números.
4. **Diretrizes para reformular as habilidades**: papel de cada classe, quantas habilidades no início e como novas se desbloqueiam, faixas de custo de Mana e recarga por tipo de habilidade, o que escala com o quê. Partir das listas de `design/classes-v0.1.md`.
5. **Metas para o simulador**: duração e taxa de vitória esperadas por encontro em cada faixa de progressão, para os testes de design futuros.
6. Lista de decisões pendentes (se houver) para o humano.

## Critérios de aceite

- `design/progression-v0.1.md` criado, revisado e aprovado pelo designer;
- cada habilidade das quatro classes tem papel, custo/recarga alvo e atributo de escala definidos;
- fórmulas com exemplos numéricos.

## Resultado

_(preencher ao concluir)_
