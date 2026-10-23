# Plano de Execução — v0.1

Plano para executar `tech/stack-proposal-v0.1.md`, dividido em tarefas que podem ser entregues a agentes diferentes (modelos diferentes), em paralelo quando as dependências permitirem.

Regras gerais para agentes: `AGENTS.md` na raiz.

---

## 1. Ambientes

| Ambiente | O que roda |
|---|---|
| **VPS Linux** | .NET SDK, `Game.Core`, `Game.Data`, testes, simulador de encontros, servidor, API, PostgreSQL, Docker, builds headless do cliente |
| **Windows** | Godot 4 .NET (editor), Tiled, testes visuais, playtest |

O repositório Git remoto (GitHub privado) é o ponto de sincronização entre os dois.

## 2. Níveis de modelo

Cada tarefa sugere um nível. Use o modelo que tiver disponível naquele nível.

| Nível | Quando | Exemplos |
|---|---|---|
| **Forte** | arquitetura, código central que outros vão estender, netcode, IA | Claude Opus, GPT de topo, Gemini Pro |
| **Médio** | tarefa bem especificada sobre uma base existente | Claude Sonnet e equivalentes |
| **Leve** | setup, conteúdo, ajustes mecânicos, documentação | Claude Haiku e equivalentes |
| **Humano** | decisões de gosto, contas externas, playtest | você |

Regra prática: a **primeira** tarefa de cada subsistema (fundação do Core, pipeline de habilidades, IA, rede) usa modelo forte. Extensões dessas bases podem usar modelo médio.

## 3. Como delegar uma tarefa

Prompt padrão para o agente:

```text
Você vai executar uma tarefa no repositório mmo-rpg.
1. Leia AGENTS.md.
2. Leia tasks/fase-0/T-XXX-....md e execute somente essa tarefa.
3. Siga a definição de pronto do AGENTS.md e preencha a seção Resultado.
```

Fluxo para você:

1. Escolha uma tarefa cujas dependências estejam integradas em `main`.
2. Abra uma sessão de agente no ambiente indicado e cole o prompt.
3. Revise a branch `task/T-XXX-...`, rode `dotnet test` e faça merge em `main`.
4. Tarefas da mesma onda podem rodar em paralelo, cada uma em sua branch (ou worktree). Integre na ordem listada na onda para reduzir conflitos.

---

## 4. Fases

| Fase | Objetivo | Estado do plano |
|---|---|---|
| **0 — Protótipo de combate** | Arena local, 4 classes com 3 habilidades + ataque básico, 4 inimigos, Matriarca simplificada, simulador de encontros | **Concluída** (T-025: aprovada como base; seção 6) |
| **1 — Rede mínima** | Servidor dedicado na VPS, 4 jogadores na mesma arena, login de desenvolvimento | **Concluída** (T-111: playtest online aprovado; seção 6b) |
| **2 — Persistência e progressão** | Contas, personagens, inventário, Cidade + Campos online; atributos, níveis, equipamento e habilidades reformuladas (`design/progression-v0.1.md`) | **Detalhada** (T-120; seção 6d) |
| **3 — Primeiro mapa completo** | Floresta, Ruínas, Cripta instanciada, Vigia Sem Rosto | Esboço |
| **4 — Operação** | CI/CD, métricas, testes de carga, produção separada | Esboço |

As fases são detalhadas em arquivos de tarefa somente quando a anterior termina (T-030 detalhou a fase 1; T-120 detalha a fase 2), porque dependem do que a fase anterior ensinar.

---

## 5. Escopo do protótipo (fase 0)

Decisões de escopo, derivadas de `design/combat-encounters-v0.1.md`:

| Classe | Ataque básico | Habilidades do protótipo |
|---|---|---|
| Guerreiro | Golpe de espada | Golpe Pesado, Golpe Circular, Guarda Firme, Provocação |
| Arqueiro | Disparo | Marca do Caçador, Recuo Rápido, Armadilha de Contenção |
| Mago | Projétil Arcano | Explosão Elemental, Prisão de Gelo, Barreira Arcana |
| Clérigo | Golpe Sagrado | Cura Direta, Escudo Sagrado, Corrente Sagrada, Purificação |

Consumível comum às quatro classes (campo `consumables`, separado das habilidades): Poção de Cura Menor (`item.minor_healing_potion`, 3 cargas por encontro, recarga de 20 s).

Inimigos: Rato de Campo, Lobo Cinzento, Salteador da Estrada, Guarda Esquelético e Matriarca da Floresta (simplificada, 3 fases).

O documento de encontros fala em "três tipos de inimigo", mas os quatro cenários de classe precisam de quatro tipos; o Rato é o mais simples e foi mantido.

Cenários (os cinco do design): Guerreiro × 3 Lobos; Arqueiro × Lobo + Salteador; Mago × 4 Ratos; Clérigo × Guarda Esquelético; grupo × Matriarca.

---

## 6. Tarefas da fase 0

### Índice

| ID | Tarefa | Ambiente | Modelo | Depende de |
|---|---|---|---|---|
| [T-001](fase-0/T-001-repo-e-vps.md) | Repositório remoto e setup da VPS | VPS | Humano + leve | — |
| [T-002](fase-0/T-002-setup-windows.md) | Setup do Windows | Windows | Humano + leve | — |
| [T-003](fase-0/T-003-estrutura-solucao.md) | Estrutura da solução .NET | VPS | Médio | 001 |
| [T-004](fase-0/T-004-spike-godot-dotnet.md) | Projeto Godot referenciando o Core | Windows | Médio | 002, 003 |
| [T-005](fase-0/T-005-definicoes-e-dados.md) | Definições de dados e carregador JSON | VPS | Médio | 003 |
| [T-006](fase-0/T-006-fundacao-simulacao.md) | Fundação da simulação | VPS | **Forte** | 003 |
| [T-007](fase-0/T-007-movimento-colisao.md) | Movimento, colisão, linha de visão, pathfinding | VPS | Forte | 006 |
| [T-008](fase-0/T-008-pipeline-habilidades.md) | Pipeline de habilidades e efeitos | VPS | **Forte** | 005, 006, 007 |
| [T-009](fase-0/T-009-status.md) | Efeitos de status e retorno decrescente | VPS | Médio | 008 |
| [T-010](fase-0/T-010-recursos.md) | Recursos por classe orientados a eventos | VPS | Médio | 008 |
| [T-011](fase-0/T-011-ia-ameaca.md) | IA de monstros e ameaça | VPS | **Forte** | 008 |
| [T-012](fase-0/T-012-telegrafos-boss.md) | Telegráfos e fases de boss | VPS | Forte | 009, 011 |
| [T-013](fase-0/T-013-log-combate.md) | Log de combate | VPS | Leve | 008 |
| [T-014](fase-0/T-014-conteudo-prototipo.md) | Conteúdo do protótipo em JSON | VPS | Médio | 009, 010, 011, 012 |
| [T-015](fase-0/T-015-simulador-encontros.md) | Simulador headless de encontros | VPS | **Forte** | 011 |
| [T-016](fase-0/T-016-validacao-balanceamento.md) | Testes de design e balanceamento | VPS | Médio | 014, 015 |
| [T-017](fase-0/T-017-mecanicas-faltantes.md) | Mecânicas faltantes do design (Provocação, Purificação, consumíveis…) | VPS | Forte | 016 |
| [T-018](fase-0/T-018-bots-rebalanceamento.md) | Bots com tempo de reação e rebalanceamento | VPS | Médio | 017 |
| [T-019](fase-0/T-019-rebalanceamento-playtest.md) | Rebalanceamento pós-playtest: monstros mais lentos, mais dano | VPS | Médio | 018, 025 (impressões) |
| [T-020](fase-0/T-020-cliente-apresentacao.md) | Cliente: autoridade local e sincronização de entidades | Windows | **Forte** | 004, 007 |
| [T-021](fase-0/T-021-cliente-input-camera.md) | Cliente: input e câmera | Windows | Médio | 008, 020 |
| [T-022](fase-0/T-022-cliente-hud.md) | Cliente: HUD e log de combate | Windows | Médio | 010, 013, 021 |
| [T-023](fase-0/T-023-cliente-telegrafos-status.md) | Cliente: telegráfos, efeitos e status | Windows | Médio | 012, 021 |
| [T-024](fase-0/T-024-cliente-menu-cenarios.md) | Cliente: menu de cenários | Windows | Leve | 014, 015, 021 |
| [T-025](fase-0/T-025-playtest.md) | Playtest do protótipo | Windows | Humano | 018, 022, 023, 024 |
| [T-026](fase-0/T-026-cliente-ataque-continuo.md) | Cliente: ataque contínuo segurando o botão | Windows | Leve | 021, 024 |
| [T-027](fase-0/T-027-mana-e-mago.md) | Mana para todas as classes; Mago com área à distância | VPS ou Windows | Médio | 019 |
| [T-028](fase-0/T-028-cliente-previa-area.md) | Cliente: pré-visualização de área e aviso de conjuração interrompida | Windows | Leve | 026, 027 |
| [T-030](fase-0/T-030-planejar-fase-1.md) | Detalhar a fase 1 em tarefas | VPS | Forte | 025 |
| [T-031](fase-0/T-031-design-progressao.md) | Design de progressão: atributos, equipamento e reformulação das habilidades | qualquer | Humano + Forte | 025 |
| [T-032](fase-0/T-032-ajustes-classes-mana.md) | Ajustes de classes, Mana só por tempo e níveis de habilidade | VPS ou Windows | Médio | 027, 031 |
| [T-033](fase-0/T-033-ataque-automatico.md) | Cliente: ataque básico automático no alvo selecionado | Windows | Leve | 026 |
| [T-034](fase-0/T-034-niveis-de-habilidade.md) | Design: todos os níveis de todas as habilidades; trilhas de habilidades e de arte | qualquer | Humano + Médio | 032 |

### Ondas de execução

Tarefas da mesma onda podem rodar em paralelo.

| Onda | VPS | Windows |
|---|---|---|
| 1 | T-001 | T-002 |
| 2 | T-003 | — |
| 3 | T-005, T-006 | T-004 |
| 4 | T-007 | — |
| 5 | T-008 | T-020 |
| 6 | T-009, T-010, T-011, T-013 | T-021 |
| 7 | T-012, T-015 | T-022 |
| 8 | T-014 | T-023 |
| 9 | T-016 | T-024 |
| 10 | — | T-025 |
| 10b | T-019 | T-026 (ajustes do playtest; depois, nova rodada da T-025) |
| 10c | T-027 | T-028 (decisões do designer; depois, nova rodada da T-025) |
| 11 | T-030, T-031 (em paralelo; T-031 é documento de design) | — |

Na onda 6, as quatro tarefas da VPS mexem em pastas diferentes do Core, mas todas registram sistemas no mundo. Integre na ordem 009 → 010 → 011 → 013. A T-009 e a T-011 criam/estendem o mesmo `data/rules.json`: quem integrar depois resolve o conflito mantendo as duas seções.

### Grafo de dependências

```text
T-001 ─► T-003 ─┬─► T-005 ───────────────┐
                ├─► T-006 ─► T-007 ──────┼─► T-008 ─┬─► T-009 ─┐
                │                │       │          ├─► T-010  ├─► T-012 ─┐
T-002 ──────────┴─► T-004 ───────┴─► T-020          ├─► T-011 ─┤          │
                                                    └─► T-013  └─► T-015  │
                                                                          ▼
                    T-009, T-010, T-011, T-012 ─► T-014 ─► T-016 ─► T-025 ─► T-030
                    T-020 + T-008 ─► T-021 ─► T-022 / T-023 / T-024 (+ T-015) ─► T-025
```

---

## 6b. Tarefas da fase 1 (rede mínima)

Detalhadas pela T-030 a partir do código da fase 0. Antes do servidor, o Core é preparado para rodar fora do cliente (T-101, T-102).

### Índice

| ID | Tarefa | Ambiente | Modelo | Depende de |
|---|---|---|---|---|
| [T-101](fase-1/T-101-core-pronto-para-servidor.md) | Core pronto para servidor: estado replicável e snapshots leves | VPS | **Forte** | 025 |
| [T-102](fase-1/T-102-log-sem-world.md) | Log de combate a partir de eventos e snapshots (sem `World`) | VPS | Médio | 025 |
| [T-103](fase-1/T-103-protocolo.md) | `Game.Protocol`: mensagens, serialização e versão | VPS | **Forte** | 101 |
| [T-104](fase-1/T-104-docker-firewall-vps.md) | VPS: Docker, firewall e porta UDP do jogo | VPS | Humano + Leve | — |
| [T-105](fase-1/T-105-servidor-de-zona.md) | `Game.Server`: loop de 20 Hz, LiteNetLib e login de desenvolvimento | VPS | **Forte** | 101, 103 |
| [T-106](fase-1/T-106-interesse-e-delta.md) | Interest management por grade e snapshots delta | VPS | **Forte** | 105 |
| [T-107](fase-1/T-107-cliente-net-authority.md) | Cliente: `NetAuthority` e jogar online | Windows | **Forte** | 102, 103, 105 |
| [T-108](fase-1/T-108-predicao-reconciliacao.md) | Cliente: predição do próprio movimento e reconciliação | Windows | **Forte** | 107 |
| [T-109](fase-1/T-109-bots-de-rede.md) | Bots de rede headless para teste de carga | VPS | Médio | 103, 105 (106 para a medição final) |
| [T-110](fase-1/T-110-deploy-desenvolvimento.md) | Deploy de desenvolvimento do servidor | VPS | Leve | 104, 105 |
| [T-111](fase-1/T-111-playtest-online.md) | Playtest online com 4 jogadores | Windows | Humano | 106, 108, 109, 110 |
| [T-120](fase-1/T-120-planejar-fase-2.md) | Detalhar a fase 2 em tarefas | VPS | Forte | 111 |

### Ondas de execução

| Onda | VPS | Windows |
|---|---|---|
| 1 | T-101, T-102, T-104 | — |
| 2 | T-103 | — |
| 3 | T-105 | — |
| 4 | T-106, T-109, T-110 | T-107 |
| 5 | — | T-108 |
| 6 | — | T-111 |
| 7 | T-120 | — |

Na onda 1 as três tarefas mexem em áreas diferentes (Core, log do Core, infraestrutura). A T-102 toca `src/Game.Core/CombatLog/` e a T-101 o resto do Core: integre 101 antes de 102.

### Grafo de dependências

```text
T-025 ─┬─► T-101 ─► T-103 ─► T-105 ─┬─► T-106 ──────────────┐
       └─► T-102 ───────────────────┼─► T-107 ─► T-108 ─────┤
T-104 ──────────────────────────────┼─► T-110 ──────────────┼─► T-111 ─► T-120
                                    └─► T-109 ──────────────┘
```

---

## 6c. Trilhas paralelas

Trabalho que não depende da rede: pode rodar em paralelo à fase 1 ou depois dela, quando o designer decidir. Cada trilha tem sua pasta em `tasks/`.

### Trilha de habilidades (`tasks/trilha-habilidades/`)

Implementa os níveis de habilidade de `design/skills-v0.1.md` (hoje só existe o NH1 no jogo).

| ID | Tarefa | Ambiente | Modelo | Depende de |
|---|---|---|---|---|
| [T-060](trilha-habilidades/T-060-core-niveis-de-habilidade.md) | Core: níveis de habilidade (NH) e múltiplos acertos | VPS | **Forte** | 034 |
| [T-061](trilha-habilidades/T-061-core-efeitos-dos-niveis.md) | Core: efeitos novos usados pelos NH | VPS | **Forte** | 060 |
| [T-062](trilha-habilidades/T-062-dados-niveis-e-novas-habilidades.md) | Dados: todos os NH e as habilidades que faltam (Chuva, assinaturas) | VPS | Médio | 060, 061 |
| [T-063](trilha-habilidades/T-063-simulador-e-balanceamento-por-nivel.md) | Simulador e balanceamento por NH | VPS | Médio | 062 |
| [T-064](trilha-habilidades/T-064-cliente-niveis-de-habilidade.md) | Cliente: escolha de NH no menu e visual dos acertos extras | Windows | Médio | 062 |

Os pontos de habilidade por nível do personagem dependem de níveis e XP, que entram na fase 2 (T-120); até lá os NH são escolhidos no menu de cenários (T-064).

### Trilha de arte e mapas (`tasks/trilha-arte/`)

Arte com pacotes de assets, estilo simples como o Tibia mas não idêntico (decisão de 2026-10-19). Antecipa o pipeline de mapas que estava na fase 3.

| ID | Tarefa | Ambiente | Modelo | Depende de |
|---|---|---|---|---|
| [T-050](trilha-arte/T-050-direcao-de-arte.md) | Direção de arte e escolha dos pacotes | Windows | Humano + Leve | — |
| [T-051](trilha-arte/T-051-pipeline-mapas-tiled.md) | Pipeline de mapas: Tiled → Core | VPS | **Forte** | — |
| [T-052](trilha-arte/T-052-mapas-no-cliente.md) | Cliente: mapas do Tiled com o tileset | Windows | Médio | 050, 051 |
| [T-053](trilha-arte/T-053-sprites-e-animacoes.md) | Cliente: sprites e animações | Windows | Médio | 050 |
| [T-054](trilha-arte/T-054-efeitos-visuais.md) | Cliente: efeitos visuais de habilidades, áreas e telegráfos | Windows | Leve | 053 |
| [T-055](trilha-arte/T-055-mapas-cidade-e-campos.md) | Mapas: Cidade-Fortaleza e Campos da Vigília | Windows | Humano + Médio | 051, 052, 053 |

```text
Habilidades:  T-034 ─► T-060 ─► T-061 ─► T-062 ─┬─► T-063
                                                 └─► T-064
Arte/mapas:   T-050 ─┬─► T-053 ─► T-054 ─┐
              T-051 ─┴─► T-052 ──────────┴─► T-055
```

---

## 6d. Tarefas da fase 2 (persistência e progressão)

Detalhadas pela T-120 a partir de `design/progression-v0.1.md` (§9: Core antes do conteúdo, conteúdo antes do balanceamento), do esboço da seção 7 e dos Resultados da fase 1. A trilha de habilidades (NH) já está pronta; a de arte e mapas não — a T-214 usa mapas provisórios se a T-051/T-055 ainda não existirem.

### Índice

| ID | Tarefa | Ambiente | Modelo | Depende de |
|---|---|---|---|---|
| **Progressão no Core** | | | | |
| [T-201](fase-2/T-201-core-atributos-e-escala.md) | Core: atributos, escala das habilidades, defesa e crítico | VPS | **Forte** | — |
| [T-202](fase-2/T-202-dados-escala-das-habilidades.md) | Dados: escala de todas as habilidades e NH | VPS | Médio | 201 |
| [T-203](fase-2/T-203-core-niveis-xp-e-pontos.md) | Core: níveis, XP, pontos de atributo e de habilidade | VPS | **Forte** | 201 |
| [T-204](fase-2/T-204-core-itens-equipamento-e-queda.md) | Core: itens, inventário, equipamento e queda | VPS | **Forte** | 201 |
| [T-205](fase-2/T-205-dados-itens-e-niveis-dos-monstros.md) | Dados: itens, tabelas de queda e níveis dos monstros | VPS | Médio | 203, 204 |
| [T-206](fase-2/T-206-simulador-perfis-por-nivel.md) | Simulador: perfis por nível e equipamento, testes de design da progressão | VPS | Médio | 202, 203, 205 |
| [T-207](fase-2/T-207-pendencias-da-fase-1.md) | Servidor e log: pendências da fase 1 (nível de log, SIGTERM, log de status) | VPS | Leve | — |
| **Contas e persistência** | | | | |
| [T-210](fase-2/T-210-postgres-e-persistencia.md) | PostgreSQL no Compose e camada de persistência | VPS | Médio | — |
| [T-211](fase-2/T-211-api-contas-e-personagens.md) | `Game.Api`: cadastro, login, token e personagens | VPS | **Forte** | 210 |
| [T-212](fase-2/T-212-servidor-personagem-persistente.md) | Servidor: entra com token, carrega e salva o personagem | VPS | **Forte** | 203, 204, 210, 211 |
| [T-213](fase-2/T-213-caddy-https-e-backup.md) | Caddy com HTTPS para a API e backup do banco fora da VPS | VPS | Humano + Leve | 211 |
| **Mundo** | | | | |
| [T-214](fase-2/T-214-servidor-zonas-cidade-e-campos.md) | Servidor: zonas abertas, Cidade e Campos, troca pelo portão | VPS | **Forte** | 212 |
| [T-215](fase-2/T-215-dados-campos-da-vigilia.md) | Dados: Campos da Vigília (surgimentos e níveis) | VPS | Médio | 205, 214 |
| **Cliente** | | | | |
| [T-220](fase-2/T-220-cliente-login-e-personagens.md) | Cliente: login, seleção e criação de personagem | Windows | Médio | 211 |
| [T-221](fase-2/T-221-cliente-personagem-e-habilidades.md) | Cliente: tela de personagem (atributos) e de habilidades (pontos e NH) | Windows | Médio | 203, 212 |
| [T-222](fase-2/T-222-cliente-inventario-e-equipamento.md) | Cliente: inventário, equipamento e saque | Windows | Médio | 204, 212 |
| [T-223](fase-2/T-223-cliente-zonas.md) | Cliente: troca de zona, Cidade e Campos | Windows | Médio | 214, 220 |
| **Fechamento** | | | | |
| [T-230](fase-2/T-230-playtest-fase-2.md) | Playtest da fase 2 (inclui as 4 pessoas pendentes da T-111) | Windows | Humano | 206, 207, 213, 215, 221, 222, 223 |
| [T-240](fase-2/T-240-planejar-fase-3.md) | Detalhar a fase 3 em tarefas | VPS | **Forte** | 230 |

### Ondas de execução

| Onda | VPS | Windows |
|---|---|---|
| 1 | T-201, T-207, T-210 | — |
| 2 | T-202, T-203, T-204, T-211 | — |
| 3 | T-205, T-212, T-213 | T-220 |
| 4 | T-206, T-214 | T-221, T-222 |
| 5 | T-215 | T-223 |
| 6 | — | T-230 |
| 7 | T-240 | — |

Cuidados de integração: T-203 e T-204 mexem no Core e no protocolo ao mesmo tempo (ambas sobem `ProtocolVersion` e criam tags de componente): integre a 203 antes da 204 e ajuste a versão e as tags na segunda. A T-207 toca o servidor e o log, sem conflito com a onda 1. A T-210 só adiciona projeto e serviço novos.

### Grafo de dependências

```text
T-201 ─┬─► T-202 ───────────────────────────────┐
       ├─► T-203 ─┬─► T-205 ─┬──────────────────┼─► T-206 ─────────┐
       └─► T-204 ─┘          └─► T-215 ◄─┐      │                  │
T-210 ─► T-211 ─┬─► T-212 ◄── (203, 204) │      │                  │
                │     ├─► T-214 ─────────┘      │                  │
                │     ├─► T-221 ◄── 203         │                  ├─► T-230 ─► T-240
                │     └─► T-222 ◄── 204         │                  │
                ├─► T-213 ──────────────────────┼──────────────────┤
                └─► T-220 ─► T-223 ◄── T-214    │                  │
T-207 ──────────────────────────────────────────┴──────────────────┘
```

---

## 7. Fases seguintes (esboço)

Serão convertidas em arquivos de tarefa no fim de cada fase (T-240 para a fase 3). As fases 1 e 2 já estão detalhadas nas seções 6b e 6d.

### Fase 2 — Persistência e progressão

Detalhada na seção 6d (T-120). Esboço original, mantido como histórico:

| Tarefa | Ambiente | Modelo |
|---|---|---|
| PostgreSQL no Compose, migrations (EF Core + Npgsql) | VPS | Médio |
| `Game.Api`: cadastro, login, token de sessão, CRUD de personagem | VPS | Forte |
| Itens, inventário, equipamento e loot no Core | VPS | Forte |
| Servidor: valida token, carrega/salva personagem, gravação assíncrona e transacional | VPS | Forte |
| Cliente: login, seleção e criação de personagem | Windows | Médio |
| Caddy com HTTPS e backup `pg_dump` fora da VPS | VPS | Leve |
| Core: atributos (FOR, DES, AGI, INT, VIT), `scaling` nos efeitos, defesa e crítico (`design/progression-v0.1.md`) | VPS | Forte |
| Core: níveis, XP, pontos livres e liberação de habilidades por nível | VPS | Forte |
| Dados: reformulação das habilidades das quatro classes e assinaturas do nível 12 | VPS | Médio |
| Simulador: perfis por nível/equipamento e testes de design da progressão | VPS | Médio |
| Cliente: tela de personagem (atributos), inventário/equipamento, barra com liberação por nível | Windows | Médio |

### Fase 3 — Primeiro mapa completo

As convenções do Tiled, o carregador de mapa e a importação no Godot foram antecipados para a trilha de arte e mapas (T-051, T-052); a Cidade e os Campos também (T-055).

| Tarefa | Ambiente | Modelo |
|---|---|---|
| Convenções de camadas do Tiled e carregador de mapa no Core | VPS | Forte |
| Importação Tiled → Godot | Windows | Médio |
| Múltiplas zonas no servidor e transição pelos portões | VPS | Forte |
| Instâncias de dungeon sob demanda | VPS | Forte |
| Mapas: Cidade, Campos, Floresta, Ruínas, Cripta | Windows | Humano + médio |
| Dados: monstros e encontros de cada área, Cavaleiro Espectral, Vigia Sem Rosto | VPS | Médio |
| NPCs, missões mínimas, mercado e armazém | VPS + Windows | Médio |

### Fase 4 — Operação

| Tarefa | Ambiente | Modelo |
|---|---|---|
| CI: testes, build headless do cliente, imagem do servidor | VPS | Médio |
| Observabilidade: Serilog, OpenTelemetry, Prometheus, Grafana | VPS | Médio |
| Testes de carga com bots e metas de tick time | VPS | Médio |
| Separar produção de desenvolvimento (segunda VPS) | VPS | Humano + leve |
