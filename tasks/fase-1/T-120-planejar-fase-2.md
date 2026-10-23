# T-120 — Detalhar a fase 2 em tarefas

- **Ambiente:** VPS
- **Modelo:** Forte
- **Depende de:** T-111 (decisão de seguir para a fase 2)
- **Áreas que pode alterar:** `tasks/`, `tech/` (somente se uma decisão técnica mudar)

## Objetivo

Converter o esboço da fase 2 (`tasks/README.md`, seção 7) em arquivos `tasks/fase-2/T-2XX-*.md`, incluindo a implementação de `design/progression-v0.1.md` (T-031): atributos e escala no Core, níveis e XP, itens/equipamento/queda, reformulação das habilidades, perfis do simulador e telas do cliente — junto com contas, personagens, persistência e Cidade + Campos online.

## Requisitos

1. Ler `tasks/reports/`, os Resultados da fase 1 e o código atual.
2. Ordenar: o que muda o Core (atributos, escala, níveis) antes do conteúdo (habilidades reformuladas) e antes do balanceamento.
3. Criar as tarefas com ambiente, modelo, dependências, áreas, requisitos e critérios de aceite verificáveis.
4. Atualizar `tasks/README.md`: índice da fase 2, ondas e grafo.

## Critérios de aceite

- toda tarefa da fase 2 pode ser entregue a um agente sem contexto adicional além de `AGENTS.md` e o próprio arquivo;
- dependências sem ciclos.

## Resultado

Feito no Windows (a tarefa é de VPS; só documentos). A fase 2 tem **19 tarefas** em `tasks/fase-2/`. O índice, as ondas e o grafo estão na seção 6d do `tasks/README.md`. Dependências conferidas por script: sem ciclos e sem referência a tarefa inexistente.

**Blocos (na ordem do §9 do design de progressão):**
1. **Progressão no Core**, primeiro:
   - T-201 atributos, `scaling`, defesa e crítico, com a regra de calibração (perfil nível 1 = números de hoje);
   - T-203 níveis, XP e pontos (ligando os NH da trilha de habilidades a pontos ganhos);
   - T-204 itens, inventário, equipamento e queda.
2. **Conteúdo e balanceamento** depois do Core: T-202 (fórmulas de todas as habilidades), T-205 (itens e níveis dos monstros) e T-206 (perfis do simulador e testes da §8). A T-206 reativa o teste do Mago deixado ignorado na T-063.
3. **Contas e persistência**: T-210 (PostgreSQL, com o estado de progressão em `jsonb` versionado para não esperar o Core), T-211 (`Game.Api`), T-212 (servidor com token; gravação fora do loop de 20 Hz) e T-213 (Caddy e backup, com decisões do humano: domínio e destino do backup).
4. **Mundo**: T-214 (várias zonas num processo, Cidade segura, Campos com respawn, portões, morte e renascimento) e T-215 (surgimentos dos Campos).
5. **Cliente**: T-220 a T-223 (login e personagens, atributos e habilidades, inventário, zonas).
6. **Fechamento**: T-230 (playtest, incluindo as 4 pessoas pendentes da T-111) e T-240 (planejar a fase 3).

**Decisões:**
- **Redistribuir pontos é grátis na fase 2** (T-203). A §10 do design prevê custo em moedas, mas economia está fora de escopo até uma tarefa pedir.
- **Direito ao saque:** de quem participou da morte (T-204). É simples, e trocar depois não quebra nada.
- **Mapas:** "Cidade + Campos online" pede mapas, e a trilha de arte (T-050..T-055) ainda não começou. A T-214 usa mapas provisórios nos dados se a T-051/T-055 não estiverem prontas. A trilha continua independente e pode rodar em paralelo à fase 2.
- **Pendências da fase 1** (nível de log, SIGTERM, log de status) viraram a T-207, leve e sem dependências.
- **NH online:** deixam de ser escolhidos no menu e passam a vir do personagem salvo (T-212/T-221). Isso resolve a pendência da T-107.

**Fora das áreas listadas:** nada (só `tasks/`).
