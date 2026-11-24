# Plano de Execução — v0.1

Plano para executar `tech/stack-proposal-v0.1.md` (jogo **Valdran**), dividido em tarefas que podem ser entregues a agentes diferentes (modelos diferentes), em paralelo quando as dependências permitirem.

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
| **2 — Persistência e progressão** | Contas, personagens, inventário, Cidade + Campos online; atributos, níveis, equipamento e habilidades reformuladas (`design/progression-v0.1.md`) | **Concluída** (T-230: playtest aprovado com correções para a fase 3; seção 6d) |
| **3 — Primeiro mapa completo** | Floresta, Ruínas, Cripta instanciada, Vigia Sem Rosto; grupo, lista de batalha, ouro, lojas e mapas maiores (playtest v0.2) | **Concluída** (T-380: playtest aprovado; seção 6e) |
| **4 — Segundas classes** | Nível 40, segunda classe no nível 20 com duas opções por classe, passivas, barra de 10 teclas; Guerreiro primeiro (`design/segunda-classe-v0.1.md`) | **Detalhada** nos blocos do Guerreiro (T-390; seção 6f), do Clérigo (T-420; seção 6g), do Arqueiro (T-430; seção 6h) e do Mago (T-440; seção 6i). Em andamento direto na `main` (desde 2026-11-22; antes, na branch `trilha/segundas-classes`) |
| **5 — Região do sul, 16 ao 27** | Brejo dos Afogados, Forte do Passo, Serra Partida e Minas Fundas, com renascimento no ponto seguro, água funda, chefe de campo e XP por hora de cada área (`design/mapas-16-40-v0.1.md`) | **Detalhada** (T-500, 2026-11-23; seção 6j) |
| **6 — Região do sul, 26 ao 40** | Planalto Queimado, Bastião do Juramento e o Salão do Juramento (masmorra final) | Esboço (seção 7); detalhada pela T-560, depois do playtest da fase 5 |
| **7 — Operação** | CI/CD, métricas, testes de carga, produção separada | Esboço. Era a fase 5 até 2026-11-23; pode correr em paralelo às fases 5 e 6, porque não toca nos mapas |

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
| [T-056](trilha-arte/T-056-monstros-fase-3-lpc.md) | Monstros da fase 3 com arte LPC (gerador, pacotes de criaturas, efeito de fantasma) | Windows | Humano + Médio | 053, 090 |

```text
Habilidades:  T-034 ─► T-060 ─► T-061 ─► T-062 ─┬─► T-063
                                                 └─► T-064
Arte/mapas:   T-050 ─┬─► T-053 ─► T-054 ─┐
              T-051 ─┴─► T-052 ──────────┴─► T-055
              T-053 ─► T-056 (monstros da fase 3)
```

### Trilha Valdran: nome e site (`tasks/trilha-valdran/`)

O jogo passa a se chamar **Valdran** (decisão de 2026-11-02) e ganha um site público com monstros, itens, classes e habilidades, gerado a partir de `data/*.json`, em `valdran.nich.click`. Não depende da fase 2; a T-072 precisa do DNS criado pelo humano.

| ID | Tarefa | Ambiente | Modelo | Depende de |
|---|---|---|---|---|
| [T-070](trilha-valdran/T-070-renomear-para-valdran.md) | Renomear o jogo para Valdran (nome exibido e documentação; identificadores internos ficam) | Windows | Leve | — |
| [T-071](trilha-valdran/T-071-site-gerador.md) | Site: gerador estático com monstros, itens, classes e habilidades | VPS | Médio | — |
| [T-072](trilha-valdran/T-072-site-publicacao.md) | Publicar o site em `valdran.nich.click` (Caddy) | VPS | Humano + Leve | 071, DNS |
| [T-073](trilha-valdran/T-073-logo-valdran.md) | Logo do Valdran no PixelLab (tela de login e página inicial do site) | Windows | Humano + Leve | 070, 088 |
| [T-074](trilha-valdran/T-074-selecao-e-icone.md) | Tela de seleção de personagem em cartões e ícone do executável (PixelLab) | Windows | Humano + Leve | 073, 220 |

```text
Valdran:      T-070 (independente)
              T-071 ─► T-072 ◄── DNS valdran.nich.click (humano)
              T-073 (logo; depois da T-088) ─► T-074 (seleção e ícone)
```

### Trilha de interface (`tasks/trilha-interface/`)

Refaz as telas do cliente com ícones (estilo dos RPGs clássicos, sem copiar arte de nenhum) e limpa a tela de jogo (decisão de 2026-11-02). Tudo no cliente: nenhuma mudança no Core, no protocolo ou no servidor.

| ID | Tarefa | Ambiente | Modelo | Depende de |
|---|---|---|---|---|
| [T-080](trilha-interface/T-080-icones-habilidades-e-itens.md) | Ícones de habilidades, itens e item no chão (escolha do pacote pelo humano) | Windows | Humano + Leve | — |
| [T-081](trilha-interface/T-081-habilidades-e-barra-de-atalhos.md) | Tela de habilidades com ícones e barra 1–6 configurável pelo jogador | Windows | Médio | 080 |
| [T-082](trilha-interface/T-082-tela-de-atributos.md) | Tela de atributos (distribuir pontos) | Windows | Médio | — |
| [T-083](trilha-interface/T-083-inventario-com-boneco.md) | Inventário com o boneco do personagem | Windows | Médio | 080 |
| [T-084](trilha-interface/T-084-icone-do-item-no-chao.md) | Ícone do item no chão | Windows | Leve | 080 |
| [T-085](trilha-interface/T-085-ocultar-debug-ctrl-f.md) | Esconder logs e debug; Ctrl+F mostra | Windows | Leve | — |
| [T-086](trilha-interface/T-086-hud-pedra-ouro.md) | Quadros do personagem e do alvo no tema pedra e ouro (kit PixelLab) | Windows | Leve | kit pedra e ouro |
| [T-087](trilha-interface/T-087-icones-habilidades-pixellab.md) | Ícones das 25 habilidades de classe refeitos no PixelLab | Windows | Leve | T-086 |
| [T-088](trilha-interface/T-088-imagem-tela-de-login.md) | Imagem de fundo da tela de login (cena de lado, classes olhando um castelo distante) | Windows | Humano + Leve | T-220 |
| [T-089](trilha-interface/T-089-interface-equipamentos.md) | Tela de equipamentos no tema pedra e ouro (moldura de janela, boneco e espaços) | Windows | Humano + Leve | T-083, T-086 |
| [T-097](trilha-interface/T-097-interface-habilidades.md) | Tela de habilidades no tema pedra e ouro | Windows | Humano + Leve | T-081, T-087, T-089 |
| [T-098](trilha-interface/T-098-interface-atributos.md) | Tela de atributos no tema pedra e ouro | Windows | Humano + Leve | T-082, T-089 |
| [T-099](trilha-interface/T-099-interface-inventario.md) | Inventário (mochila) no tema pedra e ouro | Windows | Humano + Leve | T-083, T-089, T-320 |

```text
Interface:    T-080 ─┬─► T-081
                     ├─► T-083
                     └─► T-084
              T-082, T-085 (independentes)
              T-086 ─► T-087
Pedra e ouro: T-088 (login, independente)
              T-086 ─► T-089 ─┬─► T-097 (habilidades)
                              ├─► T-098 (atributos)
                              └─► T-099 (inventário)
```

As T-088, T-089 e T-097 a T-099 (pedido de 2026-11-09) usam o PixelLab no modo de maior qualidade, com pelo menos 5 opções por tarefa escolhidas pelo designer e prévia dentro do jogo antes da aprovação. Os ids T-091 a T-096 ficam sem uso (eram da trilha PixelLab removida). Ordem: T-088, T-089, T-097, T-098, T-099, uma de cada vez.

Cuidados de integração: T-081, T-082, T-083 e T-085 mexem no `Hud.cs`; T-081 e T-085 mexem no Input Map (`project.godot`) e em `InputActions`. Integre uma de cada vez e rebase as outras. Ordem sugerida: T-085, T-082, T-080, T-081, T-083, T-084.

### Trilha de arte PixelLab (`tasks/trilha-arte-pixellab/`) — encerrada

Testou refazer a arte com o [PixelLab](https://www.pixellab.ai/docs) (decisão de 2026-11-04). **Encerrada em 2026-11-05:** o designer reprovou o piloto, porque as animações de andar e de ataque ficaram abaixo do LPC (`tasks/reports/arte-pixellab-piloto.md`). A pesquisa `tasks/reports/arte-alternativas.md` recomendou manter o LPC. As tarefas T-091 a T-096 foram removidas, e os assets do piloto saíram do cliente (ficam no histórico, commit `49b9720`). Do piloto ficou só o mecanismo `--art=<variante>` do `SpriteCatalog`, usado para prévias de arte (T-056). Os monstros da fase 3 ganham arte LPC na T-056.

| ID | Tarefa | Ambiente | Modelo | Depende de |
|---|---|---|---|---|
| [T-090](trilha-arte-pixellab/T-090-piloto-pixellab.md) | Piloto: Guerreiro, Lobo, um tileset e um objeto no jogo, com custo e decisão (**reprovado**) | Windows | Humano + Médio | conta e MCP do PixelLab |

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
| [T-216](fase-2/T-216-backup-fora-da-vps.md) | Backup do banco fora da VPS (destino a escolher pelo humano) | VPS | Humano + Leve | 213 |
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

A T-216 (destino do backup) fica fora das ondas: não bloqueia o playtest e roda quando o humano escolher o destino, de preferência antes de abrir o jogo a jogadores de fora.

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

## 6e. Tarefas da fase 3 (primeiro mapa completo)

Detalhadas pela T-240 a partir do esboço da seção 7, de `design/first-map-v0.1.md`, dos Resultados da fase 2 e do playtest v0.2 (`tasks/reports/playtest-v0.2.md`). A fase começa pelas correções do playtest. Depois vêm os sistemas que o designer pediu (lista de batalha, grupo, ouro, lojas, consumíveis, mapas maiores) e então o conteúdo novo (Floresta, Ruínas, Cripta, Vigia).

**Decisões da T-240**
- **Economia mínima autorizada:** ouro que cai dos monstros e NPCs com preço fixo (T-320–T-323). Fica de fora: troca entre jogadores, mercado de jogadores, crafting e armazém. As tarefas autorizam isso explicitamente, como o `AGENTS.md` exige.
- **Chave da Cripta adiada:** os fragmentos de chave exigem missões e itens de missão. Na fase 3 a entrada fica aberta, com aviso de conteúdo para grupo.
- **A Matriarca sai dos Campos** e volta para o Ninho, dentro da Floresta (T-342), como no design.
- **Fora desta fase:** missões, armazém, portas que abrem, o altar da Galeria e o aumento do nível máximo (o designer pediu "não agora").

### Índice

| ID | Tarefa | Ambiente | Modelo | Depende de |
|---|---|---|---|---|
| **Correções do playtest** | | | | |
| [T-301](fase-3/T-301-correcoes-do-playtest.md) | Recargas na troca de zona, corpos das invocações, aggro em cadeia | VPS | Médio | — |
| [T-302](fase-3/T-302-ritmo-de-xp-e-queda.md) | Dados: ritmo de XP e taxa de queda | VPS | Médio | — |
| **Sistemas pedidos no playtest** | | | | |
| [T-310](fase-3/T-310-grupo-party.md) | Grupo (party): convite, XP e saque divididos | VPS | **Forte** | 301 |
| [T-311](fase-3/T-311-cliente-grupo.md) | Cliente: grupo | Windows | Médio | 310 |
| [T-312](fase-3/T-312-cliente-lista-de-batalha.md) | Cliente: lista de batalha | Windows | Médio | — |
| [T-320](fase-3/T-320-consumiveis-e-ouro.md) | Core: consumíveis como itens e ouro | VPS | **Forte** | 310 |
| [T-321](fase-3/T-321-npcs-e-lojas.md) | NPCs vendedores e lojas | VPS | **Forte** | 320, 331 |
| [T-322](fase-3/T-322-dados-economia.md) | Dados: ouro, preços e poções | VPS | Médio | 302, 321 |
| [T-323](fase-3/T-323-cliente-ouro-loja-e-consumiveis.md) | Cliente: ouro, loja e consumíveis na barra | Windows | Médio | 321 |
| **Mundo** | | | | |
| [T-331](fase-3/T-331-zonas-lidas-do-mapa.md) | Zonas lidas do mapa (portões, surgimentos, NPCs no Tiled) | VPS | Médio | 301 |
| [T-330](fase-3/T-330-mapas-maiores-cidade-e-campos.md) | Mapas maiores: Cidade e Campos | Windows | Humano + Médio | 331 |
| [T-340](fase-3/T-340-monstros-da-floresta.md) | Monstros da Floresta: Fada e Ent | VPS | **Forte** | — |
| [T-341](fase-3/T-341-monstros-das-ruinas.md) | Monstros das Ruínas: Sacerdote e Cavaleiro | VPS | **Forte** | — (após 340) |
| [T-342](fase-3/T-342-floresta-baixa.md) | Mapa e zona: Floresta Baixa e Ninho da Matriarca | Windows | Humano + Médio | 330, 331, 340 |
| [T-343](fase-3/T-343-ruinas-da-estrada-antiga.md) | Mapa e zona: Ruínas da Estrada Antiga | Windows | Humano + Médio | 330, 331, 341 |
| [T-345](fase-3/T-345-cliente-monstros-novos-e-vigia.md) | Cliente: sprites e leitura dos monstros novos e do Vigia | Windows | Médio | 340, 341, 351, 056 |
| **Dungeon** | | | | |
| [T-350](fase-3/T-350-instancias-de-dungeon.md) | Servidor: instâncias de dungeon sob demanda | VPS | **Forte** | 310, 331 |
| [T-351](fase-3/T-351-vigia-sem-rosto.md) | Core e dados: o Vigia Sem Rosto | VPS | **Forte** | 341 |
| [T-352](fase-3/T-352-cripta-do-vigia.md) | Mapa e instância: Cripta do Vigia | Windows | Humano + Médio | 343, 350, 351 |
| **Fechamento** | | | | |
| [T-380](fase-3/T-380-playtest-fase-3.md) | Playtest da fase 3 (4 pessoas, ping de cada uma) | Windows | Humano | 302, 311, 312, 322, 323, 342, 345, 352 |
| **Correções durante o playtest** (pedido de 2026-11-16) | | | | |
| [T-381](fase-3/T-381-login-lembrar-email.md) | Login: caixa "Lembrar e-mail" | Windows | Leve | — |
| [T-382](fase-3/T-382-lista-de-batalha-compacta.md) | Lista de batalha: 5 linhas, miniatura do monstro e ícone de ataque no PixelLab | Windows | Humano + Médio | — |
| [T-383](fase-3/T-383-tirar-quadro-do-alvo.md) | Tirar a vida do alvo do topo da tela | Windows | Leve | — |
| [T-384](fase-3/T-384-loja-quantidade.md) | Loja: escolher a quantidade de poções | Windows | Médio | — |
| [T-385](fase-3/T-385-morte-na-cripta-volta-para-cidade.md) | Morte na Cripta renasce na Cidade | VPS | Médio | — |
| [T-386](fase-3/T-386-tab-e-ataque-basico.md) | Alvo do Tab às vezes não recebe o ataque básico | Windows | Médio | — |
| [T-387](fase-3/T-387-guarda-esqueletico-ataque-basico.md) | Guarda Esquelético usa o ataque básico | VPS | Médio | — |
| [T-388](fase-3/T-388-marca-do-alvo.md) | Marca melhor no monstro selecionado | Windows | Humano + Médio | — |
| **Mini mapa** (pedido de 2026-11-17) | | | | |
| [T-392](fase-3/T-392-mini-mapa.md) | Cliente: mini mapa (jogador e membros por perto) | Windows | Humano + Médio | — |
| [T-391](fase-3/T-391-posicao-do-grupo.md) | Servidor: posição dos membros distantes do grupo (pode esperar) | VPS | Médio | — |
| [T-393](fase-3/T-393-mini-mapa-membros-distantes.md) | Cliente: membros distantes do grupo no mini mapa | Windows | Médio | 391, 392 |
| **Saque** (pedido de 2026-11-18) | | | | |
| [T-394](fase-3/T-394-dono-do-saque.md) | Dono por item no saque e item largado livre para todos | Windows | Médio | — |
| **Menu do Esc e morte** (pedido de 2026-11-19) | | | | |
| [T-395](fase-3/T-395-saida-do-mundo.md) | Servidor: sair do mundo com espera em combate | VPS | Médio | — |
| [T-396](fase-3/T-396-menu-do-esc.md) | Cliente: menu do Esc (trocar de personagem, sair do jogo) | Windows | Humano + Médio | 395 |
| [T-397](fase-3/T-397-tela-de-morte.md) | Cliente: tela de morte com "Retornar à Cidade" | Windows | Humano + Médio | — |
| **Renascimento** (achado de 2026-11-19) | | | | |
| [T-398](fase-3/T-398-renascimento-em-zona-vazia.md) | Servidor: renascimento conta o tempo em que a zona ficou vazia | VPS | Médio | — |
| [T-390](fase-3/T-390-planejar-fase-4.md) | Detalhar a fase 4 (segundas classes) em tarefas | VPS | **Forte** | 380 |

### Ondas de execução

| Onda | VPS | Windows |
|---|---|---|
| 1 | T-301, T-302, T-340 | T-312 |
| 2 | T-310, T-331, T-341 | — |
| 3 | T-320, T-351 | T-311, T-330 |
| 4 | T-321, T-350 | T-342, T-345 |
| 5 | T-322 | T-323, T-343 |
| 6 | — | T-352 |
| 7 | — | T-380 |
| 8 | T-390 | — |

A T-216 (destino do backup, da fase 2) continua fora das ondas. Convém concluí-la antes de abrir o jogo a jogadores de fora.

**Correções durante o playtest (T-381 a T-388).** O designer pediu em 2026-11-16, no meio da T-380, para fazê-las já, enquanto segue testando. Não dependem umas das outras:
- **VPS, em paralelo:** T-385 e T-387.
- **Windows, uma de cada vez, nesta ordem:** T-383, T-386, T-384, T-381, T-382, T-388. As duas últimas pedem escolha do designer entre opções do PixelLab e podem dividir a mesma rodada de geração.
- Depois de integrar: subir o servidor na VPS (`scripts/deploy-dev.sh`, por causa da T-385 e da T-387) e publicar um cliente novo (`export-client.ps1` e `deploy-download.ps1`).
- O relatório `playtest-v0.3.md` registra essas correções como achados do playtest. A T-380 foi concluída em 2026-11-19: seguir para a T-390, com ajustes de classe e mudança nos níveis.

**Mini mapa (T-391 a T-393).** O designer pediu em 2026-11-17, ainda durante a T-380, um mini mapa com a posição dos membros do grupo; o essencial é o mini mapa com a posição do jogador. A área de interesse do servidor só alcança uns 40 tiles e os mapas chegam a 200 × 160, por isso o pedido foi dividido:
- **T-392 (Windows), primeiro:** o mini mapa e o mapa grande da zona, com o jogador e os membros que o cliente já vê. Não mexe em servidor nem protocolo; sai sozinha, com um cliente novo.
- **T-391 (VPS) e T-393 (Windows), depois e opcionais:** o servidor manda a posição dos membros distantes, e o cliente os mostra. A T-391 sobe `ProtocolVersion` para 13: **não integre a T-391 na `main` sozinha**, porque o próximo `deploy-dev.sh` subiria um servidor que o cliente publicado não aceita. A T-393 sai da branch da T-391; integre as duas juntas e, na mesma rodada, rode o `deploy-dev.sh` e publique o cliente novo.
- A T-391 e a T-385 mexem no `WorldServer`: integre uma de cada vez.

**Menu do Esc e morte (T-395 a T-397).** O designer pediu em 2026-11-19 o menu do Esc para trocar de personagem e uma tela de morte de verdade:
- **T-395 (VPS) e T-397 (Windows), em paralelo.** A T-397 não mexe em servidor nem protocolo e sai sozinha, com um cliente novo.
- **T-396 (Windows), depois da T-395, saindo da branch dela.** A T-395 sobe `ProtocolVersion` para 15: **não integre a T-395 na `main` sozinha**. Integre as duas juntas e, na mesma rodada, rode o `deploy-dev.sh` e publique o cliente novo.
- A T-396 e a T-397 mexem em `Main.cs` e em `client/scripts/Match/`: integre uma de cada vez.

**Cuidados de integração**
- A T-382, a T-383, a T-384 e a T-386 mexem em `Hud.cs` (e a T-386 em `PlayerInput`, que a T-382 também lê pelo Tab): integre uma de cada vez e rebase as outras.
- A T-383 e a T-388 mexem em `EntityView`: integre a 383 primeiro.
- A T-301 e a T-331 mexem no `OpenZone`: integre a 301 primeiro (a 331 depende dela).
- A T-340 e a T-341 mexem em `monsters.json` e nos perfis de IA: integre a 340 antes da 341.
- A T-310, a T-320, a T-321 e a T-350 sobem `ProtocolVersion`. A 321 e a 350 rodam na mesma onda: integre a 321 primeiro e ajuste a versão na 350.
- A T-311, a T-312 e a T-323 mexem no `Hud.cs` e no Input Map: integre uma de cada vez e rebase as outras.
- A T-330, a T-342 e a T-343 mexem em `city.tmj`: a ordem das ondas já as separa.
- **Arte dos monstros novos:** vem da T-056 (trilha de arte, LPC), que já ligou no `sprites.json` os ids `monster.mist_fairy`, `monster.dead_priest`, `monster.spectral_knight`, `monster.faceless_watcher` e `monster.shade`. A T-340, a T-341 e a T-351 usam esses ids; a T-345 só confere e ajusta. O Ent Corrompido ainda não tem arte (encomenda).

### Grafo de dependências

```text
T-301 ─┬─► T-310 ─┬─► T-311
       │          ├─► T-320 ─► T-321 ─┬─► T-322 ◄── T-302
       │          │              ▲    └─► T-323
       │          └─► T-350 ◄────┼─────────── T-331
       └─► T-331 ────────────────┘
           T-331 ─► T-330 ─┬─► T-342 ◄── T-340
                           └─► T-343 ◄── T-341 ─► T-351
T-343, T-350, T-351 ─► T-352
T-340, T-341, T-351 ─► T-345
T-312 (independente)
T-302, T-311, T-312, T-322, T-323, T-342, T-345, T-352 ─► T-380 ─► T-390
```

---

## 6f. Tarefas da fase 4 (segundas classes, bloco do Guerreiro)

Detalhadas pela T-390 a partir de `design/segunda-classe-v0.1.md` e do playtest v0.3 (`tasks/reports/playtest-v0.3.md`). A fase entrega o **sistema** de segunda classe com o **Guerreiro** (Guardião e Berserker). As outras três classes ficavam no esboço da seção 7 até o playtest do Guerreiro (T-413). Em 2026-11-22 o designer decidiu seguir sem esperar por ele: o Clérigo está na seção 6g.

**Decisões da T-390**
- **Escolha pela tela de habilidades**, fora de combate, em qualquer lugar (T-403, T-410). O mestre da classe na Cidade, do design, fica para quando os mapas forem refeitos.
- **Sem migração de banco:** a segunda classe vai no documento de progressão do personagem (jsonb, com versão), como o resto da progressão (T-404).
- **Mecânicas antes dos dados:** as peças novas do Core (desvio de dano, vida pelo dano, dano pela vida do alvo, área que acompanha) são tarefas próprias com dados de teste (T-406, T-407). Elas também servem às classes seguintes (a vida pelo dano ao Inquisidor; o desvio de dano ia servir ao Elo da Vida, que saiu em 2026-11-22, seção 6g).
- **Ferramenta de teste desligada por padrão** (`--dev-tools`), ligada só no servidor local de teste (T-405).
- **Fora desta fase:** áreas e mapas novos, monstros novos, as outras segundas classes e a operação (fase 5).

### Índice

| ID | Tarefa | Ambiente | Modelo | Depende de |
|---|---|---|---|---|
| **Base** | | | | |
| [T-401](fase-4/T-401-regras-do-nivel-40.md) | Regras do nível 40 e da segunda classe nos dados | VPS | Médio | — |
| [T-402](fase-4/T-402-simulador-ate-o-40.md) | Simulador até o 40 e metas dos testes de design (reativa os 19 casos desligados) | VPS | **Forte** | 401 |
| **Sistema de segunda classe** | | | | |
| [T-403](fase-4/T-403-core-segunda-classe.md) | Core: segunda classe e habilidades passivas | VPS | **Forte** | 401 |
| [T-404](fase-4/T-404-servidor-segunda-classe.md) | Servidor e protocolo: segunda classe gravada e replicada | VPS | Médio | 403 |
| [T-405](fase-4/T-405-ferramenta-de-teste-servidor.md) | Servidor: ferramenta de teste para subir de nível | VPS | Médio | 404 |
| **Guerreiro** | | | | |
| [T-406](fase-4/T-406-mecanicas-do-guardiao.md) | Core: mecânicas novas do Guardião (desvio de dano, imunidade a empurrão, ameaça pelo dano) | VPS | **Forte** | — |
| [T-407](fase-4/T-407-mecanicas-do-berserker.md) | Core: mecânicas novas do Berserker (dano pela vida do alvo, vida pelo dano, área que acompanha) | VPS | **Forte** | 406 |
| [T-408](fase-4/T-408-dados-guardiao-e-berserker.md) | Dados: habilidades do Guardião e do Berserker | VPS | Médio | 402, 403, 407 |
| **Cliente** | | | | |
| [T-409](fase-4/T-409-barra-de-10-teclas.md) | Barra de atalhos com 10 teclas | Windows | Médio | — |
| [T-410](fase-4/T-410-tela-da-segunda-classe.md) | Escolha da segunda classe e tela de habilidades | Windows | Humano + Médio | 404, 408, 409 |
| [T-411](fase-4/T-411-botao-de-teste-no-esc.md) | Botão de teste no menu do Esc | Windows | Médio | 404, 405 |
| [T-412](fase-4/T-412-icones-e-efeitos-do-guerreiro.md) | Ícones e efeitos das habilidades do Guardião e do Berserker | Windows | Humano + Médio | 408 |
| **Fechamento** | | | | |
| [T-413](fase-4/T-413-playtest-do-guerreiro.md) | Playtest do Guerreiro com segunda classe | Windows | Humano | 410, 411, 412 |

### Ondas de execução

| Onda | VPS | Windows |
|---|---|---|
| 1 | T-401, T-406 | T-409 |
| 2 | T-402, T-403, T-407 | — |
| 3 | T-404, T-408 | — |
| 4 | T-405 | T-412 |
| 5 | — | T-411, depois T-410 |
| 6 | — | T-413 |

**Fase 4 na `main` (decisão do designer, 2026-11-22).** A fase 4 ficou numa branch própria (`trilha/segundas-classes`) até 2026-11-22, quando o designer decidiu testar na `main`. A trilha e as tarefas prontas (T-401 a T-409 e T-412) entraram na `main` juntas, com `ProtocolVersion` **19** (16 T-403, 17 T-406, 18 T-404, 19 T-405). Daqui em diante:
- as tarefas da fase 4 saem da `main` e são integradas na `main`, como nas fases anteriores. A branch `trilha/segundas-classes` não recebe mais nada;
- a T-401, que tinha sido desfeita na `main` com um `git revert`, voltou com o merge da trilha;
- os testes da fase 4 continuam **locais, no Windows**, a partir da `main`, sem deploy na VPS:
  - servidor: `dotnet run --project src/Game.Server -c Release -- --world --dev-login --dev-tools --port 7777`;
  - cliente: o projeto do Godot, pela tela de entrada sem conta, no endereço `127.0.0.1:7777` (vários clientes no mesmo PC, ou na rede local pelo IP do PC, para testar em grupo);
  - com `--dev-login` não há banco: o personagem começa no nível 1 e não é gravado. A ferramenta do Esc (T-411) sobe de nível na hora; a gravação da segunda classe é coberta pelos testes de servidor da T-404;
  - sem a T-411, o encontro local do cliente já aceita `--level=n` e `--second-class=id` (T-412) para jogar com a segunda classe;
- o servidor de teste da VPS ainda roda a versão antiga do protocolo: um deploy da `main` exige o cliente novo junto (`export-client.ps1` e `deploy-download.ps1`), e o `--dev-tools` só se o designer pedir.

**Cuidados de integração**
- **Protocolo:** resolvido na entrada da fase 4 na `main` (versão 19, acima). A T-411 só usa as mensagens da T-405 e não precisa subir o número; se subir, é a partir de 19.
- A T-410 não sobe o protocolo; a T-404 e a T-408 já estão na `main`.
- A T-403 e a T-404 mexem em `Leveling` e no documento do personagem; a T-405 também. Uma de cada vez, na ordem.
- A T-406 e a T-407 mexem em `DamageResolver`, estados e efeitos: a T-407 sai **da branch da T-406** (que não está na `main`).
- A T-408 (dados) precisa das mecânicas da T-407 e da segunda classe da T-403: sai da branch da T-407 com a T-403 mesclada (ou da `main`, se a rodada do protocolo já tiver entrado), e entra na `main` na mesma rodada ou logo depois dela.
- A T-403 (progressão) e a T-406/T-407 (efeitos) rodam em paralelo; se as duas tocarem em `CombatServices` ou no validador de dados, integre uma e rebase a outra.
- A T-409 e a T-410 mexem na barra e no `SkillPanel`; a T-411 no `GameMenu` e no `Main.cs`: integre uma de cada vez.

### Grafo de dependências

```text
T-401 ─┬─► T-402 ─────────────► T-408
       └─► T-403 ─┬───────────► T-408
                  └─► T-404 ─┬─► T-405 ─► T-411
                             └─► T-410
T-406 ─► T-407 ─► T-408 ─┬─► T-410 ◄── T-409
                         └─► T-412
T-410, T-411, T-412 ─► T-413 ─► (outras três classes, seção 7)
```

---

## 6g. Tarefas da fase 4 (bloco do Clérigo)

Detalhadas pela T-420 a partir de `design/segunda-classe-v0.1.md`, seção 8. O bloco entrega o **Sacerdote** e o **Inquisidor** sobre o sistema de segunda classe do bloco do Guerreiro (seção 6f), que já está na `main`.

**Decisões do designer (2026-11-22)**
- **Seguir sem o playtest do Guerreiro.** A T-413 fica pendente e pode ser feita junto com o playtest do Clérigo (T-425).
- **Inquisidor aprovado como está.**
- **Inspiração no lugar do Elo da Vida** no Sacerdote. A Inspiração dá regeneração de Mana aos aliados no raio, menos ao próprio Sacerdote, e não Mana na hora ("Mana vem só do tempo").
- Arqueiro e Mago: o designer revisa as segundas classes uma por uma antes de detalhar os blocos deles. O Arqueiro está na seção 6h e o Mago na 6i.

**O que o bloco do Guerreiro já deixou pronto:** vida pelo dano (T-407) para o Selo da Luz, o Julgamento e a Ira Divina; estados em aliados num raio; áreas no chão. O desvio de dano (T-406) não é mais usado pelo Clérigo, porque o Elo da Vida saiu.

### Índice

| ID | Tarefa | Ambiente | Modelo | Depende de |
|---|---|---|---|---|
| [T-420](fase-4/T-420-planejar-clerigo.md) | Detalhar o bloco do Clérigo em tarefas | Windows | **Forte** | T-390 |
| [T-421](fase-4/T-421-mecanicas-do-sacerdote.md) | Core: mecânicas do Sacerdote (cura por segundo, atributos e Mana vindos de um estado, vida mínima de 1, efeito ao acabar) | VPS | **Forte** | — |
| [T-422](fase-4/T-422-mecanicas-do-inquisidor.md) | Core: mecânicas do Inquisidor (área que fere e cura, selo de vida pelo dano, ataque básico trocado, cura dos aliados pelo dano) | VPS | **Forte** | 421 |
| [T-423](fase-4/T-423-dados-sacerdote-e-inquisidor.md) | Dados: habilidades do Sacerdote e do Inquisidor, metas e testes de design | VPS | Médio | 421, 422 |
| [T-424](fase-4/T-424-icones-e-efeitos-do-clerigo.md) | Ícones e efeitos das habilidades do Sacerdote e do Inquisidor | Windows | Humano + Médio | 423 |
| [T-425](fase-4/T-425-playtest-do-clerigo.md) | Playtest do Clérigo com segunda classe | Windows | Humano | 424 |

### Ondas de execução

| Onda | VPS | Windows |
|---|---|---|
| 1 | T-421 | — |
| 2 | T-422 | — |
| 3 | T-423 | — |
| 4 | — | T-424 |
| 5 | — | T-425 (e a T-413, se ainda estiver aberta) |

**Cuidados de integração**
- Tudo sai da `main` e entra na `main`, como no fim do bloco do Guerreiro. Testes locais no Windows, como na seção 6f.
- A T-421 e a T-422 mexem nas definições de estado, no validador de dados e no schema: uma de cada vez, na ordem. A T-422 sai da `main` com a T-421 integrada, ou da branch da T-421.
- **Protocolo:** nenhuma tarefa do bloco precisa subir `ProtocolVersion`. Se uma subir, é a partir do número da `main` (19 em 2026-11-22), e cliente e servidor saem do mesmo commit.
- A T-423 é a primeira a pôr dados reais do Clérigo: só entra depois da T-421 e da T-422.

### Grafo de dependências

```text
T-420 ─► T-421 ─► T-422 ─► T-423 ─► T-424 ─► T-425
                                              ▲
                                  (T-413) ────┘ opcional, no mesmo playtest
```

---

## 6h. Tarefas da fase 4 (bloco do Arqueiro)

Detalhadas pela T-430 a partir de `design/segunda-classe-v0.1.md`, seção 6. O bloco entrega o **Atirador de Elite** e o **Caçador**.

**Decisões do designer (2026-11-22)**
- **Atirador de Elite aprovado**, com o **Tiro Certeiro sem conjuração** e o **Olho de Falcão de 15 / 17 / 20 s** (antes, 6 / 7 / 8 s).
- **Caçador aprovado como está.**

**Pode rodar ao lado do bloco do Clérigo.** Os dois blocos não dependem um do outro, mas as tarefas de Core (T-421, T-422, T-431, T-432) mexem nos mesmos arquivos: definições de estado, `DamageResolver`, validador de dados e schema. Rode **uma tarefa de Core de cada vez**, nesta ordem: T-421, T-422, T-431, T-432. Também dá para alternar os blocos, desde que cada tarefa saia da `main` com a anterior integrada.

### Índice

| ID | Tarefa | Ambiente | Modelo | Depende de |
|---|---|---|---|---|
| [T-430](fase-4/T-430-planejar-arqueiro.md) | Detalhar o bloco do Arqueiro em tarefas | Windows | **Forte** | T-420 |
| [T-431](fase-4/T-431-mecanicas-do-atirador.md) | Core: mecânicas do Atirador (dano menor atrás na linha, crítico pela distância, alcance de um estado, ignorar armadura, crítico garantido) | VPS | **Forte** | — |
| [T-432](fase-4/T-432-mecanicas-do-cacador.md) | Core: mecânicas do Caçador (efeito em todo acerto, veneno pelo NH da passiva, dano se o alvo andar, copiar estados, estado que pula) | VPS | **Forte** | 431 |
| [T-433](fase-4/T-433-dados-atirador-e-cacador.md) | Dados: habilidades do Atirador e do Caçador, metas e testes de design | VPS | Médio | 431, 432 |
| [T-434](fase-4/T-434-icones-e-efeitos-do-arqueiro.md) | Ícones e efeitos das habilidades do Atirador e do Caçador | Windows | Humano + Médio | 433 |
| [T-435](fase-4/T-435-playtest-do-arqueiro.md) | Playtest do Arqueiro com segunda classe | Windows | Humano | 434 |

### Ondas de execução (com o bloco do Clérigo; as três classes juntas na seção 6i)

| Onda | VPS | Windows |
|---|---|---|
| 1 | T-421 | — |
| 2 | T-422 | — |
| 3 | T-423, depois T-431 | — |
| 4 | T-432 | T-424 |
| 5 | T-433 | T-425 |
| 6 | — | T-434 |
| 7 | — | T-435 |

**Cuidados de integração**
- Tudo sai da `main` e entra na `main`. Testes locais no Windows, como na seção 6f.
- **Protocolo:** nenhuma tarefa do bloco precisa subir `ProtocolVersion`. Se a T-431 precisar mandar o alcance novo pela rede, sobe a partir do número da `main`, e cliente e servidor saem do mesmo commit.
- A T-423 e a T-433 mexem em `classes.json` e nos testes de design da segunda classe (`SecondClassDesignTests`): integrar uma e rebasear a outra.
- A T-424 e a T-434 mexem em `icons.png` e `icons.json`: uma de cada vez.

### Grafo de dependências

```text
T-430 ─► T-431 ─► T-432 ─► T-433 ─► T-434 ─► T-435
         (depois da T-422, pelos arquivos em comum)
```

---

## 6i. Tarefas da fase 4 (bloco do Mago) e ordem das três classes

Detalhadas pela T-440 a partir de `design/segunda-classe-v0.1.md`, seção 7. O bloco entrega o **Piromante** e o **Arcanista**.

**Decisões do designer (2026-11-22)**
- **Piromante aprovado como está.**
- **Arcanista aprovado**, com o **Sifão Arcano dando regeneração de Mana por 5 s** em vez de Mana na hora ("Mana vem só do tempo", como na Inspiração).

**Peças que vêm de outros blocos:** a regeneração de Mana de um estado (T-421) serve ao Sifão; a cópia de estados (T-432) serve à Combustão NH5. Por isso a T-441 depende da T-432 e a T-442 da T-421.

### Índice

| ID | Tarefa | Ambiente | Modelo | Depende de |
|---|---|---|---|---|
| [T-440](fase-4/T-440-planejar-mago.md) | Detalhar o bloco do Mago em tarefas | Windows | **Forte** | T-430 |
| [T-441](fase-4/T-441-mecanicas-do-piromante.md) | Core: mecânicas do Piromante (Queimadura por NH, dano só em quem queima, explodir no primeiro acerto, área depois do impacto) | VPS | **Forte** | 432 |
| [T-442](fase-4/T-442-mecanicas-do-arcanista.md) | Core: mecânicas do Arcanista (canalização com pulsos, dano que sobe, andar canalizando, orbes, Sifão) | VPS | **Forte** | 421, 441 |
| [T-443](fase-4/T-443-dados-piromante-e-arcanista.md) | Dados: habilidades do Piromante e do Arcanista, metas e testes de design | VPS | Médio | 441, 442 |
| [T-444](fase-4/T-444-icones-e-efeitos-do-mago.md) | Ícones e efeitos das habilidades do Piromante e do Arcanista | Windows | Humano + Médio | 443 |
| [T-445](fase-4/T-445-playtest-do-mago.md) | Playtest do Mago com segunda classe (pode ser o da fase inteira) | Windows | Humano | 444 |

### Ondas das três classes juntas

As seis tarefas de Core mexem nos mesmos arquivos (definições de estado, `DamageResolver`, conjuração, validador de dados e schema): **uma de cada vez**, cada uma saindo da `main` com a anterior integrada. Os dados e o cliente de uma classe andam enquanto o Core da seguinte roda.

| Onda | VPS | Windows |
|---|---|---|
| 1 | T-421 | — |
| 2 | T-422 | — |
| 3 | T-423, depois T-431 | — |
| 4 | T-432 | T-424 |
| 5 | T-433, depois T-441 | T-425 (opcional agora) |
| 6 | T-442 | T-434 |
| 7 | T-443 | T-435 (opcional agora) |
| 8 | — | T-444 |
| 9 | — | T-445 (e os playtests que estiverem abertos) |

**Cuidados de integração**
- Tudo sai da `main` e entra na `main`. Testes locais no Windows, como na seção 6f.
- **Protocolo:** a T-442 é a que mais pode precisar de rede (orbes, canalização em andamento). Quem subir `ProtocolVersion` sobe a partir do número da `main`, e cliente e servidor saem do mesmo commit.
- A T-423, a T-433 e a T-443 mexem em `classes.json` e nos testes de design da segunda classe: uma de cada vez.
- A T-424, a T-434 e a T-444 mexem em `icons.png` e `icons.json`: uma de cada vez.

### Grafo de dependências (as três classes)

```text
T-421 ─► T-422 ─► T-423 ─► T-424 ─► T-425
  │
  │      T-431 ─► T-432 ─► T-433 ─► T-434 ─► T-435
  │                 │
  │                 ▼
  └──────────────► T-441 ─► T-442 ─► T-443 ─► T-444 ─► T-445
       (T-421 ─► T-442)

Ordem do Core: T-421 → T-422 → T-431 → T-432 → T-441 → T-442
```

---

## 6j. Tarefas da fase 5 (região do sul, 16 ao 27)

Detalhadas pela T-500 a partir de `design/mapas-16-40-v0.1.md`, aprovado pelo designer em 2026-11-23. A fase entrega a primeira metade da região ao sul da Cidade: **Brejo dos Afogados** (16–20, campo), **Forte do Passo** (ponto seguro), **Serra Partida** (21–25, campo) e **Minas Fundas** (24–27, masmorra de 4). Mesma ordem da fase 3: peças de sistema e monstros no Core, depois os mapas, depois o playtest.

**Decisões do designer (2026-11-23; `design/mapas-16-40-v0.1.md` §8)**
- **Região contínua pelo portão sul**, com o Forte do Passo no meio. Temas aprovados.
- **Curva de XP mantida.** A T-505 mede a XP por hora de cada área; o playtest (T-550) diz se ela muda.
- **16–27 primeiro**; o 26–40 é a fase 6, detalhada depois do playtest (T-560).
- **Mestre das classes e tela de habilidades**: as duas formas valem (T-541).
- **Renascimento no Forte do Passo**, por zona: Serra e Minas renascem no Forte; o Brejo renasce na Cidade (T-501).
- **Pode começar já.** Os playtests de classe da fase 4 ainda abertos (T-413, T-425, T-435, T-445) não bloqueiam e podem ir junto com o da fase 5.
- **Fora desta fase:** missões, chave da masmorra, portas que abrem, monstro que anda por uma rota (fase 6) e o Planalto em diante.

### Índice

| ID | Tarefa | Ambiente | Modelo | Depende de |
|---|---|---|---|---|
| [T-500](fase-5/T-500-planejar-fase-5.md) | Detalhar a fase 5 em tarefas | Windows | **Forte** | esboço aprovado |
| **Sistema** | | | | |
| [T-501](fase-5/T-501-renascer-no-ponto-seguro.md) | Servidor: renascer no ponto seguro da zona (`respawn` em `zones.json`) | VPS | Médio | — |
| [T-502](fase-5/T-502-terreno-que-deixa-lento.md) | Core: terreno que deixa lento (`slow_terrain`, água funda) | VPS | **Forte** | — |
| [T-503](fase-5/T-503-anuncio-de-chefe-de-campo.md) | Servidor e protocolo: anúncio de chefe de campo | VPS | Médio | — |
| [T-505](fase-5/T-505-xp-por-hora-por-area.md) | Simulador: XP por hora de cada área e metas do 16 ao 27 | VPS | Médio | — |
| **Monstros e dados** | | | | |
| [T-510](fase-5/T-510-monstros-do-brejo.md) | Monstros do Brejo e a Bruxa do Brejo (chefe de campo) | VPS | **Forte** | — (integrar depois da 502) |
| [T-511](fase-5/T-511-monstros-da-serra.md) | Monstros da Serra Partida | VPS | **Forte** | 510 |
| [T-512](fase-5/T-512-capataz-forjado.md) | Core e dados: o Capataz Forjado e os monstros das Minas | VPS | **Forte** | 511 |
| [T-513](fase-5/T-513-dados-itens-lojas-e-quedas.md) | Dados: itens, loja do Forte, poções e quedas do 16 ao 27 | VPS | Médio | 510, 511, 512 |
| **Arte** | | | | |
| [T-520](fase-5/T-520-tilesets-do-sul.md) | Tilesets do sul no PixelLab: brejo, serra, mina e forte | Windows | Humano + Médio | — |
| [T-521](fase-5/T-521-sprites-dos-monstros-do-sul.md) | Sprites LPC dos monstros do sul | Windows | Humano + Médio | — |
| **Mapas** | | | | |
| [T-530](fase-5/T-530-brejo-dos-afogados.md) | Mapa e zona: Brejo dos Afogados (e o portão sul da Cidade) | Windows | Humano + Médio | 502, 503, 505, 510, 520 |
| [T-531](fase-5/T-531-forte-do-passo.md) | Mapa e zona: Forte do Passo | Windows | Humano + Médio | 501, 513, 520, 530 |
| [T-532](fase-5/T-532-serra-partida.md) | Mapa e zona: Serra Partida | Windows | Humano + Médio | 505, 511, 520, 531 |
| [T-533](fase-5/T-533-minas-fundas.md) | Mapa e instância: Minas Fundas | Windows | Humano + Médio | 501, 505, 512, 532 |
| **Cliente** | | | | |
| [T-540](fase-5/T-540-cliente-monstros-e-chefe-de-campo.md) | Cliente: monstros do sul, água funda e aviso de chefe de campo | Windows | Médio | 502, 503, 510, 511, 512, 521 |
| [T-541](fase-5/T-541-cliente-mestre-e-renascimento.md) | Cliente: mestre das classes e tela de morte com o destino | Windows | Médio | 501 (só a tela de morte) |
| **Fechamento** | | | | |
| [T-550](fase-5/T-550-playtest-fase-5.md) | Playtest da fase 5 (e os playtests de classe abertos) | Windows | Humano | 513, 530, 531, 532, 533, 540, 541 |
| [T-560](fase-5/T-560-planejar-fase-6.md) | Detalhar a fase 6 (26 ao 40) em tarefas | VPS ou Windows | **Forte** | 550 |

### Ondas de execução

| Onda | VPS | Windows |
|---|---|---|
| 1 | T-501, T-502, T-505 | T-520, T-521 (longas: pedem escolha do designer) |
| 2 | T-503, T-510 | T-541 (o mestre; a tela de morte espera a 501) |
| 3 | T-511 | — |
| 4 | T-512 | T-530 |
| 5 | T-513 | T-540 |
| 6 | — | T-531 |
| 7 | — | T-532 |
| 8 | — | T-533 |
| 9 | — | T-550 |
| 10 | T-560 | — |

**Cuidados de integração**
- Tudo sai da `main` e entra na `main`, como na fase 4. Testes locais no Windows, como na seção 6f, com `--dev-tools` para subir de nível.
- **Protocolo:** só a T-503 precisa subir `ProtocolVersion` (para 20, se a `main` ainda estiver no 19). **Não integre a T-503 na `main` sozinha:** a T-540 sai da branch dela, e as duas entram juntas, com um cliente novo. Quem mais subir o número sobe a partir do da `main`, e cliente e servidor saem do mesmo commit.
- **Core:** a T-502, a T-510, a T-511 e a T-512 podem tocar em estados, efeitos, IA, no validador de dados e no schema. Uma de cada vez, nesta ordem: 502, 510, 511, 512. A T-501, a T-502 e a T-503 tocam na leitura de zonas e mapas (`Zones.cs`, `TiledMapReader`), e a 501 e a 503 também no `WorldServer`: integre na ordem 501, 502, 503 e rebase as seguintes.
- **Dados:** a T-510, a T-511, a T-512 e a T-513 mexem em `monsters.json` e `abilities_monsters.json`; a T-513 também em `items.json` e `shops.json`. Uma de cada vez.
- **Mapas:** a T-530, a T-531, a T-532 e a T-533 mexem em `zones.json` e no mapa da zona anterior (para abrir o portão). A ordem das ondas já as separa. A T-530 é a única que mexe em `city.tmj`.
- **Ids fixados antes:** os ids dos monstros (`monster.drowned`, `monster.marsh_lizardman`, `monster.marsh_serpent`, `monster.marsh_witch`, `monster.possessed_miner`, `monster.mountain_wolf`, `monster.cave_bat`, `monster.stone_golem`, `monster.forged_overseer`), das zonas (`zone.drowned_marsh`, `zone.pass_fort`, `zone.broken_ridge`, `zone.deep_mines`), do estado `status.deep_water` e dos NPCs (`npc.class_master`, `shop.pass_fort`) estão nas tarefas, para a arte e os dados andarem em paralelo. Se uma tarefa trocar um id, registre no Resultado para as outras.
- A T-540 e a T-541 mexem no `Hud.cs`: uma de cada vez.

### Grafo de dependências

```text
Core e dados:  T-502 ─► T-510 ─► T-511 ─► T-512 ─► T-513
               T-501, T-503, T-505 (independentes)
Arte:          T-520, T-521 (independentes)

Mapas:         T-530 ◄── 502, 503, 505, 510, 520
                 └─► T-531 ◄── 501, 513
                       └─► T-532 ◄── 511
                             └─► T-533 ◄── 512
Cliente:       T-540 ◄── 502, 503, 510, 511, 512, 521
               T-541 ◄── 501

Fechamento:    T-513, T-530 a T-533, T-540, T-541 ─► T-550 ─► T-560
```

---

## 7. Fases seguintes (esboço)

Serão convertidas em arquivos de tarefa no fim de cada fase. As fases 1, 2 e 3 estão detalhadas nas seções 6b, 6d e 6e, os blocos do Guerreiro, do Clérigo, do Arqueiro e do Mago da fase 4 nas seções 6f a 6i, e a fase 5 na seção 6j.

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

Detalhada na seção 6e (T-240). Esboço original, mantido como histórico:

As convenções do Tiled, o carregador de mapa e a importação no Godot foram antecipados para a trilha de arte e mapas (T-051, T-052); a Cidade e os Campos também (T-055).

A arte continua no LPC (trilha de arte). Os sprites dos monstros novos (Fada da Névoa, Ent Corrompido, Sacerdote Morto, Cavaleiro Espectral, Vigia Sem Rosto, sombras) saem da T-056, que pode rodar antes ou em paralelo às tarefas de dados; os mapas novos usam os tilesets LPC da T-055.

| Tarefa | Ambiente | Modelo |
|---|---|---|
| Convenções de camadas do Tiled e carregador de mapa no Core | VPS | Forte |
| Importação Tiled → Godot | Windows | Médio |
| Múltiplas zonas no servidor e transição pelos portões | VPS | Forte |
| Instâncias de dungeon sob demanda | VPS | Forte |
| Mapas: Cidade, Campos, Floresta, Ruínas, Cripta | Windows | Humano + médio |
| Dados: monstros e encontros de cada área, Cavaleiro Espectral, Vigia Sem Rosto | VPS | Médio |
| NPCs, missões mínimas, mercado e armazém | VPS + Windows | Médio |

### Fase 4 — Segundas classes

Decidida pelo designer depois do playtest v0.3 (2026-11-19 a 2026-11-21). Design completo em `design/segunda-classe-v0.1.md`. **Foco só nas classes**: as áreas novas vêm depois. O bloco do Guerreiro está detalhado na seção 6f (T-390); o resto fica neste esboço até o playtest dele (T-413).

| Bloco | Conteúdo | Estado do design |
|---|---|---|
| Base | regras do nível 40, simulador até o 40, metas dos testes de design | decidido |
| Ferramenta de teste | botão no menu do Esc para subir de nível e desfazer a segunda classe, só em servidor de teste | decidido |
| Guerreiro | sistema de segunda classe e passivas, Guardião e Berserker, barra de 10 teclas, tela de escolha; playtest | **aprovado** |
| Clérigo | Sacerdote (Inspiração no lugar do Elo da Vida) e Inquisidor | **aprovado** (2026-11-22); detalhado na seção 6g (T-420) |
| Arqueiro | Atirador de Elite (Tiro Certeiro sem conjuração, Olho de Falcão de 15 a 20 s) e Caçador | **aprovado** (2026-11-22); detalhado na seção 6h (T-430) |
| Mago | Piromante e Arcanista (Sifão Arcano com regeneração de Mana) | **aprovado** (2026-11-22); detalhado na seção 6i (T-440) |

### Fase 6 — Região do sul, 26 ao 40

Segunda parte de `design/mapas-16-40-v0.1.md` (§3.5 e §3.6). A T-560 detalha depois do playtest da fase 5.

| Tarefa | Ambiente | Modelo |
|---|---|---|
| Core: monstro que anda por uma rota (o Chefe de Guerra do Planalto) | VPS | Forte |
| Monstros do Planalto Queimado (acampamentos orcs de 6 a 10, com xamã que cura) e o Chefe de Guerra | VPS | Forte |
| Monstros do Bastião do Juramento (cavaleiros corrompidos, elites em todo grupo) e o chefe final, nível 40, para 4 | VPS | Forte |
| Dados: itens e loja do 27 ao 40 | VPS | Médio |
| Tilesets: planalto queimado e fortaleza do Juramento; sprites dos monstros | Windows | Humano + Médio |
| Mapas: Planalto Queimado (26–32), Bastião (33–37), Salão do Juramento (38–40, instância) | Windows | Humano + Médio |
| Cliente e playtest | Windows | Médio + Humano |

### Fase 7 — Operação

Era a fase 4 até 2026-11-21 e a fase 5 até 2026-11-23.

| Tarefa | Ambiente | Modelo |
|---|---|---|
| CI: testes, build headless do cliente, imagem do servidor | VPS | Médio |
| Observabilidade: Serilog, OpenTelemetry, Prometheus, Grafana | VPS | Médio |
| Testes de carga com bots e metas de tick time | VPS | Médio |
| Separar produção de desenvolvimento (segunda VPS) | VPS | Humano + leve |
