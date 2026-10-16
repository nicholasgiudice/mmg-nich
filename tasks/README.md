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
| **0 — Protótipo de combate** | Arena local, 4 classes com 3 habilidades + ataque básico, 4 inimigos, Matriarca simplificada, simulador de encontros | **Detalhada** (tarefas abaixo) |
| **1 — Rede mínima** | Servidor dedicado na VPS, 4 jogadores na mesma arena, login de desenvolvimento | Esboço (seção 7) |
| **2 — Persistência** | Contas, personagens, inventário, Cidade + Campos online | Esboço |
| **3 — Primeiro mapa completo** | Floresta, Ruínas, Cripta instanciada, Vigia Sem Rosto | Esboço |
| **4 — Operação** | CI/CD, métricas, testes de carga, produção separada | Esboço |

As fases 1 a 4 são detalhadas em arquivos de tarefa somente quando a fase anterior terminar (tarefa T-030 para a fase 1), porque dependem do que o protótipo ensinar.

---

## 5. Escopo do protótipo (fase 0)

Decisões de escopo, derivadas de `design/combat-encounters-v0.1.md`:

| Classe | Ataque básico | Habilidades do protótipo |
|---|---|---|
| Guerreiro | Golpe de espada | Golpe Pesado, Golpe Circular, Guarda Firme |
| Arqueiro | Disparo | Marca do Caçador, Recuo Rápido, Armadilha de Contenção |
| Mago | Projétil Arcano | Explosão Elemental, Prisão de Gelo, Barreira Arcana |
| Clérigo | Golpe Sagrado | Cura Direta, Escudo Sagrado, Corrente Sagrada |

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
| [T-020](fase-0/T-020-cliente-apresentacao.md) | Cliente: autoridade local e sincronização de entidades | Windows | **Forte** | 004, 007 |
| [T-021](fase-0/T-021-cliente-input-camera.md) | Cliente: input e câmera | Windows | Médio | 008, 020 |
| [T-022](fase-0/T-022-cliente-hud.md) | Cliente: HUD e log de combate | Windows | Médio | 010, 013, 021 |
| [T-023](fase-0/T-023-cliente-telegrafos-status.md) | Cliente: telegráfos, efeitos e status | Windows | Médio | 012, 021 |
| [T-024](fase-0/T-024-cliente-menu-cenarios.md) | Cliente: menu de cenários | Windows | Leve | 014, 015, 021 |
| [T-025](fase-0/T-025-playtest.md) | Playtest do protótipo | Windows | Humano | 016, 022, 023, 024 |
| [T-030](fase-0/T-030-planejar-fase-1.md) | Detalhar a fase 1 em tarefas | VPS | Forte | 025 |

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
| 11 | T-030 | — |

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

## 7. Fases seguintes (esboço)

Serão convertidas em arquivos de tarefa pela T-030 e pelas tarefas equivalentes no fim de cada fase.

### Fase 1 — Rede mínima

| Tarefa | Ambiente | Modelo |
|---|---|---|
| `Game.Protocol`: mensagens, serialização (MemoryPack), versionamento | VPS | Forte |
| `Game.Server`: loop de 20 Hz, LiteNetLib, login de desenvolvimento, intenções → Core, snapshots | VPS | Forte |
| Interest management por grade e snapshots delta | VPS | Forte |
| Cliente: `NetAuthority`, interpolação, predição de movimento e reconciliação | Windows | Forte |
| Bots de rede headless para teste de carga | VPS | Médio |
| Deploy de desenvolvimento: Dockerfile, Compose, ufw, porta UDP | VPS | Leve |
| Playtest online com 4 jogadores | Windows | Humano |

### Fase 2 — Persistência

| Tarefa | Ambiente | Modelo |
|---|---|---|
| PostgreSQL no Compose, migrations (EF Core + Npgsql) | VPS | Médio |
| `Game.Api`: cadastro, login, token de sessão, CRUD de personagem | VPS | Forte |
| Itens, inventário, equipamento e loot no Core | VPS | Forte |
| Servidor: valida token, carrega/salva personagem, gravação assíncrona e transacional | VPS | Forte |
| Cliente: login, seleção e criação de personagem | Windows | Médio |
| Caddy com HTTPS e backup `pg_dump` fora da VPS | VPS | Leve |

### Fase 3 — Primeiro mapa completo

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
