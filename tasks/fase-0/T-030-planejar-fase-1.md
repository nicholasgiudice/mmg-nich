# T-030 — Detalhar a fase 1 em tarefas

- **Ambiente:** VPS
- **Modelo:** Forte
- **Depende de:** T-025 (decisão de seguir para a fase 1)
- **Áreas que pode alterar:** `tasks/`, `tech/` (somente se uma decisão técnica mudar)

## Objetivo

Converter o esboço da fase 1 (`tasks/README.md`, seção 7) em arquivos `tasks/fase-1/T-1XX-*.md` no mesmo formato da fase 0, considerando o código real e os relatórios da fase 0.

## Requisitos

1. Ler `tasks/reports/`, as seções Resultado da fase 0 e o código atual de `Game.Core`, `Game.Bots` e do cliente.
2. Revisar se algo do Core precisa mudar para rodar em servidor (alocação por tick, serialização de estado, ids estáveis) e criar tarefas para isso primeiro.
3. Criar as tarefas com ambiente, modelo, dependências, áreas, requisitos e critérios de aceite verificáveis.
4. Atualizar `tasks/README.md`: índice da fase 1, ondas de execução e grafo.
5. Incluir a instalação de Docker na VPS e a abertura da porta UDP como tarefa própria.

## Critérios de aceite

- Toda tarefa da fase 1 pode ser entregue a um agente sem contexto adicional além de `AGENTS.md` e o próprio arquivo;
- dependências não têm ciclos.

## Resultado

_(preencher ao concluir)_
