# T-390 — Detalhar a fase 4 (segundas classes) em tarefas

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-380
- **Libera:** —
- **Áreas que pode alterar:** `tasks/`, `tech/` (somente se uma decisão técnica mudar)

## Contexto

O playtest da fase 3 foi aprovado (`tasks/reports/playtest-v0.3.md`). Depois dele, o designer decidiu que a **fase 4 é a das segundas classes**, e não mais a de operação. CI/CD, observabilidade, testes de carga e produção separada passam para a fase 5 (`tasks/README.md`, seção 7).

**Fonte de verdade do design:** `design/segunda-classe-v0.1.md`. Ele traz as decisões do designer (seção 1), as regras de pontos e NH (seção 2), a curva de XP (seção 3), como a segunda classe funciona, as passivas e a ferramenta de teste (seção 4) e todas as habilidades das 8 segundas classes, NH por NH (seções 5 a 8). Resumo do que vale para esta fase:

- nível máximo **40**; segunda classe no **nível 20**, escolha **definitiva**, com 4 habilidades normais e 1 assinatura;
- **40 pontos de habilidade** (1 por nível) e **200 de atributo** (5 por nível a partir do 2, mais **+5 ao virar segunda classe**);
- **barra de 10 teclas** (1–5 da classe base, 6–0 da segunda classe);
- **passivas**: usam pontos e NH, não ocupam tecla nem custam Mana;
- curva de XP atual mantida (`68 × L^1,8`);
- **Guerreiro primeiro** (Guardião e Berserker, aprovados), para validar o sistema;
- **foco só nas classes**: áreas e mapas novos, inclusive do nível 16 ao 40, ficam para depois;
- para testar sem conteúdo novo, um **botão no menu do Esc** sobe de nível e desfaz a segunda classe, **só em servidor de teste** (seção 4.2).

**Situação das outras segundas classes:** só o Guerreiro e o Sacerdote estão aprovados por inteiro. No Atirador de Elite e no Caçador, só as passivas (Mira Firme e Flecha Envenenada) estão decididas. O Piromante, o Arcanista e o Inquisidor esperam aprovação (seção 10 do design).

## Requisitos

1. Ler `design/segunda-classe-v0.1.md`, `tasks/reports/playtest-v0.3.md`, os Resultados da fase 3 e o código atual (Core de progressão, efeitos, estados, protocolo, persistência e o menu do Esc da T-396).
2. Criar `tasks/fase-4/T-4XX-*.md` com ambiente, modelo, dependências, áreas, requisitos e critérios de aceite verificáveis, cobrindo **o bloco do Guerreiro**:
   - **Regras do nível 40:** `maxLevel`, níveis de NH separados para a classe base e a segunda classe, +5 de atributo na segunda classe, tetos de AGI; atualizar `design/progression-v0.1.md`, `design/classes-v0.1.md` e `design/skills-v0.1.md`; simulador e perfis até o 40; rever as metas dos testes de design e reativar os 19 casos marcados com `PlaytestRebalance.Pending`.
   - **Ferramenta de teste:** opção do servidor desligada por padrão; intenções de subir de nível (+1, +5, ir ao 20, ir ao 40) e de desfazer a segunda classe, pelo mesmo caminho da XP normal; botão no menu do Esc, visível só quando o servidor permite.
   - **Core da segunda classe:** escolha no nível 20, definitiva; habilidades da segunda classe e pontos de qualquer classe; passivas como estado permanente; gravação no banco (migração).
   - **Mecânicas novas do Guerreiro** (seção 5, tabela "o que já existe e o que é novo"): desviar dano de um aliado, dano extra pela vida do alvo e zerar a recarga ao matar, vida pelo dano causado, área que acompanha quem anda, imunidade a empurrão, ameaça multiplicada. Cada uma com teste no Core.
   - **Dados do Guardião e do Berserker:** as 10 habilidades com todos os NH, e testes de design.
   - **Cliente:** barra de 10 teclas, tela de escolha da segunda classe (deixa claro que é definitiva e pede confirmação), tela de habilidades com as duas classes e a moldura das passivas, ícones e efeitos das habilidades novas. A interface usa o kit de pedra e ouro e aparece primeiro como prévia no jogo para o designer aprovar.
   - **Playtest do Guerreiro** (Windows, humano), que fecha o bloco.
3. As outras três classes (Arqueiro, Mago, Clérigo) entram em `tasks/README.md`, seção 7, como **esboço** da continuação da fase 4, a detalhar depois do playtest do Guerreiro e da aprovação das segundas classes que faltam. Indicar quais mecânicas novas delas já saem prontas do bloco do Guerreiro (por exemplo, o desvio de dano serve ao Elo da Vida do Sacerdote).
4. Atualizar `tasks/README.md`: estado das fases (seção 4), índice da fase 4, ondas, cuidados de integração (protocolo, migrações, arquivos que várias tarefas tocam) e grafo.

## Fora do escopo

Áreas e mapas novos, monstros novos, CI/CD e observabilidade (fase 5), e as segundas classes ainda não aprovadas.

## Critérios de aceite

- toda tarefa da fase 4 pode ser entregue a um agente sem contexto além de `AGENTS.md`, o próprio arquivo e `design/segunda-classe-v0.1.md`;
- dependências sem ciclos;
- toda tarefa que sobe `ProtocolVersion` diz com quais outras precisa ser integrada, e quando rodar o `deploy-dev.sh` e publicar o cliente;
- a ferramenta de teste não pode ser ligada por engano em produção: o critério de aceite da tarefa dela verifica que, sem a opção, o servidor recusa o pedido.

## Resultado

Concluída em 2026-11-21, feita no Windows (só documentos), a pedido do designer. **13 tarefas** em `tasks/fase-4/` (T-401 a T-413) e índice, decisões, ondas, cuidados de integração e grafo na seção 6f de `tasks/README.md`.

**Blocos**
- **Base:** regras do nível 40 nos dados (T-401); simulador até o 40, revisão das metas e reativação dos 19 casos de `PlaytestRebalance.Pending` (T-402).
- **Sistema de segunda classe:** Core com escolha definitiva e passivas (T-403); servidor, protocolo e gravação (T-404); ferramenta de teste no servidor (T-405).
- **Guerreiro:** mecânicas do Guardião (T-406) e do Berserker (T-407); dados das 10 habilidades (T-408).
- **Cliente:** barra de 10 teclas (T-409), escolha e tela de habilidades (T-410), botão de teste no Esc (T-411), ícones e efeitos (T-412).
- **Fechamento:** playtest do Guerreiro (T-413).

**Decisões**
- A escolha da segunda classe é feita pela tela de habilidades, fora de combate. O "mestre da classe na Cidade" do design fica para quando os mapas forem refeitos (a T-403 registra no design).
- A segunda classe vai no documento de progressão do personagem (jsonb, com versão): **sem migração de banco**.
- As mecânicas novas são tarefas próprias, antes dos dados, porque também servem às classes seguintes.
- A ferramenta de teste fica desligada por padrão (`--dev-tools`, `GAME_DEV_TOOLS`), e o critério de aceite da T-405 verifica a recusa sem ela.
- **Protocolo:** T-404 e T-405 sobem `ProtocolVersion`; integrar T-404, T-405 e T-411 juntas, com `deploy-dev.sh` e cliente novo na mesma rodada.

**Desvios**
- Feita no Windows, e não na VPS, por pedido do designer. Não mexeu em código.
- As outras três classes (Arqueiro, Mago, Clérigo) ficaram como esboço na seção 7, como o requisito 3 pede: dependem do playtest do Guerreiro e da aprovação de cinco segundas classes.

**Pendências**
- Aprovação do designer do Atirador de Elite (além da Mira Firme), do Caçador (além da Flecha Envenenada), do Piromante, do Arcanista e do Inquisidor, antes de detalhar as outras classes.
