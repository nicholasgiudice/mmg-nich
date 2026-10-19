# T-063 — Simulador e balanceamento por nível de habilidade

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** T-062
- **Libera:** nova rodada de playtest
- **Áreas que pode alterar:** `tests/Game.Sim/`, `tests/Game.Sim.Tests/`, `tests/Game.Design.Tests/`, `src/Game.Bots/` (usar NH e efeitos novos), `data/` (números), `tasks/reports/`

## Contexto

Os testes de design validam o protótipo com todas as habilidades no NH1. Com os NH implementados, é preciso medir distribuições de pontos e garantir que nenhum NH ou combinação quebra os encontros.

## Requisitos

1. Simulador aceita NH por classe/habilidade (`--rank class.archer:archer.power_shot=5` ou perfis prontos: "NH1", "focado", "espalhado").
2. Bots usam os comportamentos novos (cargas, áreas, saltos) sem estratégia nova obrigatória.
3. Testes de design por perfil (metas de `design/progression-v0.1.md` §8): distribuições "duas habilidades no NH5" e "pontos espalhados" vencem ≥ 70% nos encontros solo; nenhuma habilidade no NH5 sozinha resolve um encontro sem movimento (mesma regra da Explosão).
4. Ajustar números em `data/` se necessário e atualizar `design/skills-v0.1.md` com o que mudou.
5. Relatório `tasks/reports/balance-v0.6.md`.

## Critérios de aceite

- testes de design passando para NH1 e para os perfis novos;
- relatório criado.

## Resultado

_(preencher ao concluir)_
