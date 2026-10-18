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

Concluída em 2026-10-17 no Windows (a tarefa é de VPS; feita aqui a pedido do designer — só documentos). Fase 1 detalhada em `tasks/fase-1/` e na seção 6b de `tasks/README.md` (índice, ondas e grafo, sem ciclos).

**Revisão do Core para rodar em servidor** (requisito 2) — vira as primeiras tarefas:
- `World.Snapshot()` clona todos os componentes de todas as entidades a cada tick e mistura estado interno (IA, ameaça, recurso) com o que o cliente precisa → **T-101** separa replicável × interno, reduz alocação e mede o tick.
- `EntitySnapshot` só pode ser construído dentro do Core → **T-101** cria uma fábrica pública para o cliente de rede.
- O log de combate depende do `World` → **T-102** passa a formatar a partir de eventos + snapshots + conteúdo.
- Conteúdo igual em cliente e servidor → hash do `ContentCatalog` (**T-101**) verificado no `Hello` (**T-103/T-105**).
- Pontos fortes já existentes: nenhum relógio nem aleatoriedade sem seed no Core; ids de entidade nunca reutilizados; hash de estado (`StateHasher`) útil para achar dessincronização.

**Tarefas criadas:** T-101 (Core para servidor), T-102 (log sem `World`), T-103 (`Game.Protocol`), T-104 (Docker, ufw e porta UDP — tarefa própria, como pedido), T-105 (`Game.Server`), T-106 (interesse e delta), T-107 (`NetAuthority`), T-108 (predição e reconciliação), T-109 (bots de rede), T-110 (deploy de desenvolvimento), T-111 (playtest online) e T-120 (planejar a fase 2).

**Decisões:**
- Primeira versão do servidor = **uma sala** com um encontro do protótipo (padrão Matriarca), vagas livres opcionalmente preenchidas por bots de `Game.Bots`; zonas e mapa completo ficam para a fase 3.
- A progressão aprovada na T-031 (atributos, níveis, equipamento, habilidades reformuladas) entrou no esboço da fase 2 ("Persistência e progressão"), que a T-120 detalha.
