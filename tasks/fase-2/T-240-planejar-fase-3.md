# T-240 — Detalhar a fase 3 em tarefas

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-230
- **Libera:** —
- **Áreas que pode alterar:** `tasks/`, `tech/` (somente se uma decisão técnica mudar)

## Contexto

Converter o esboço da fase 3 (`tasks/README.md`, seção 7: Floresta, Ruínas, Cripta instanciada, Vigia Sem Rosto, NPCs e missões) em tarefas, aproveitando o que a fase 2 e a trilha de arte já entregaram.

**Arte (decisão de 2026-11-05):** a arte continua no LPC. A trilha PixelLab foi encerrada (`tasks/reports/arte-pixellab-piloto.md`, `tasks/reports/arte-alternativas.md`). Os sprites dos monstros da fase 3 vêm da T-056 (`tasks/trilha-arte/T-056-monstros-fase-3-lpc.md`), que propõe ids provisórios (`monster.mist_fairy`, `monster.corrupted_ent`, `monster.dead_priest`, `monster.spectral_knight`, `monster.faceless_watcher`, `monster.shade`). As tarefas de dados da fase 3 usam esses ids ou registram a troca, e a tarefa de cliente que liga os monstros se coordena com a T-056. Lacunas sem fonte LPC (Ent Corrompido, sprite próprio da Matriarca) viram encomenda a artista no estilo LPC; não bloqueiam a fase (use um sprite provisório).

## Requisitos

1. Ler `tasks/reports/`, os Resultados da fase 2 e o código atual.
2. Criar `tasks/fase-3/T-3XX-*.md` com ambiente, modelo, dependências, áreas, requisitos e critérios de aceite verificáveis.
3. Atualizar `tasks/README.md`: índice da fase 3, ondas e grafo.

## Critérios de aceite

- toda tarefa da fase 3 pode ser entregue a um agente sem contexto além de `AGENTS.md` e o próprio arquivo;
- dependências sem ciclos.

## Resultado

_(preencher ao concluir)_
