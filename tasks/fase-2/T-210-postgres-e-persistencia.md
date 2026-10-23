# T-210 — PostgreSQL no Compose e camada de persistência

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** —
- **Libera:** T-211, T-212, T-213
- **Áreas que pode alterar:** `src/Game.Persistence/` (novo), `tests/Game.Persistence.Tests/` (novo), `deploy/`, `MmoRpg.sln`, `README.md` (infraestrutura)

## Contexto

`tech/stack-proposal-v0.1.md` §4 (Persistência) e §10: PostgreSQL só na rede interna do Compose, EF Core + Npgsql, migrations. A fase 2 grava contas e personagens; o estado de progressão (atributos, NH, inventário, equipamento) é desenhado nas T-203/T-204, então esta tarefa guarda esse estado como **JSON versionado** (`jsonb`) para não depender delas.

## Requisitos

1. Serviço `postgres` no `deploy/compose.yml` (volume persistente, **sem** `ports:`; senha por arquivo `.env` fora do Git, com exemplo `deploy/.env.example`).
2. Projeto `Game.Persistence`: `DbContext` com **Account** (id, e-mail único, hash de senha, criado em, último login) e **Character** (id, conta, nome único, classe, nível, XP, zona, posição, `progression` jsonb com versão do formato, criado/atualizado em, versão para concorrência otimista).
3. Migrations do EF Core; comando para aplicar (no deploy e nos testes).
4. Repositório com operações assíncronas: criar/listar/apagar personagem, carregar, salvar com checagem de versão.
5. Testes de integração contra PostgreSQL real (contêiner de teste ou o do Compose) marcados para rodar na VPS; o `check.sh` não pode falhar onde não houver PostgreSQL (pular com mensagem).

## Critérios de aceite

- `docker compose up -d` sobe o PostgreSQL sem porta publicada; migrations aplicadas;
- testes de integração passando na VPS;
- `./scripts/check.sh` completo passa.

## Resultado

**Feito (no Windows, sem Docker nem PostgreSQL)**

- **`src/Game.Persistence`** (EF Core 10.0.13 + Npgsql 10.0.3):
  - `GameDbContext` com as tabelas `accounts` e `characters`, colunas em snake_case.
  - **Account:** id, e-mail único (guardado em minúsculas), hash da senha, criado em, último login.
  - **Character:** id, conta (cascata ao apagar a conta), nome e `normalized_name`, classe, nível, XP, zona, posição x/y, `progression_version`, `progression` (jsonb), criado/atualizado em.
    - O nome é único sem diferenciar maiúsculas, por índice único em `normalized_name`.
    - A versão de concorrência é a coluna de sistema `xmin` (uint + `IsRowVersion`): não precisa de coluna extra e muda a cada UPDATE.
  - Migration inicial em `Migrations/` (`20261023020840_InitialCreate`).
  - `GameDatabase.MigrateAsync` aplica as migrations; `DesignTimeGameDbContextFactory` lê `MMO_DB_CONNECTION` para o `dotnet ef` e para o bundle.
  - `GameRepository`, assíncrono, com um `DbContext` por chamada e `TimeProvider` injetável:
    - criar conta (null se o e-mail existe), achar por e-mail, registrar login;
    - criar personagem (`Created` / `NameTaken` / `AccountNotFound`), listar por conta, carregar;
    - apagar (só da própria conta);
    - salvar com checagem de versão (`Saved` / `Conflict` / `NotFound`): grava tudo menos id, conta e criado em, e devolve a versão nova na entidade.
- **Deploy:**
  - `deploy/compose.yml` ganhou `postgres` (`postgres:17-alpine`, volume `pgdata`, healthcheck, **sem `ports:`**) e `migrate`. O `migrate` usa `deploy/migrate/Dockerfile`, um bundle autocontido do EF Core em `runtime-deps`, sem root. Ele roda a cada `up` depois do banco ficar saudável e sai.
  - A senha vem de `deploy/.env` (modelo em `deploy/.env.example`; `.env` já era ignorado pelo Git). Sem ela o Compose para com mensagem.
- **Testes** (`tests/Game.Persistence.Tests`):
  - 7 testes de integração com `[PostgresFact]`: migrations, e-mail, criação/lista/carga com jsonb, nome único e conta inexistente, apagar só da conta, conflito de versão e NotFound, cascata.
  - Eles são **pulados com mensagem** sem `MMO_TEST_DB`. Cada execução cria um banco `mmo_test_*` e o apaga no fim.
  - 2 testes sem banco: o modelo não tem mudança sem migration; nomes snake_case, jsonb e xmin.
  - `deploy/test-db.sh up|down` sobe um PostgreSQL descartável em `127.0.0.1:55432` para esses testes.
- O `README.md` ganhou a seção "Banco de dados (PostgreSQL)", com a primeira configuração, o psql, nova migration e testes de integração.

**Verificado aqui**

- `check.sh` completo passou, com os 7 testes de integração pulados.
- O `dotnet ef migrations bundle` gera o executável a partir do projeto biblioteca.
- O script SQL não cria a coluna `xmin`.

**Pendente (VPS)**

- `docker compose up -d --build` com o PostgreSQL sem porta publicada e as migrations aplicadas.
- Testes de integração rodando contra o banco real.
- Atenção: na VPS, `./scripts/deploy-dev.sh` e qualquer `docker compose` passam a exigir `deploy/.env`.

**Decisões**

- O estado de progressão é `string` jsonb com versão do formato; o dono do formato são as T-203/T-204.
- O hash da senha é só uma coluna: o algoritmo fica para a autenticação (T-211).

**Validação na VPS (2026-10-23)**

- **Correção 1 — build do `migrate` falhava** com `NETSDK1004` (`project.assets.json` não encontrado): o `dotnet ef migrations bundle --runtime linux-x64` não restaura para o runtime pedido. `deploy/migrate/Dockerfile` ganhou `RUN dotnet restore src/Game.Persistence --runtime linux-x64` antes do bundle.
- **Correção 2 — `Save_checks_the_version_and_reports_conflicts` falhava** (`CreatedAt` diferente em 0,5 µs): o relógio do Linux tem resolução de 100 ns e o `timestamptz` guarda microssegundos, então a entidade devolvida por `CreateCharacterAsync` não batia com a lida do banco (no Windows o relógio é mais grosseiro e passava). `GameRepository.Now()` agora trunca para microssegundos.
- `docker compose up -d --build`: `migrate` aplicou `20261023020840_InitialCreate` e saiu com 0; tabelas `accounts`, `characters` e `__EFMigrationsHistory` criadas; `ps` mostra o postgres só com `5432/tcp` (sem porta no host); game-server escutando `0.0.0.0:7777/udp`.
- Testes de integração contra o banco de `test-db.sh`: 9 passaram, 0 pulados (3 execuções seguidas).
- `./scripts/check.sh` completo passou.
- T-207: `docker compose stop game-server` termina o log em "Servidor parado"; subiu de novo normalmente.
- Ruído conhecido, sem efeito: o log do `migrate` mostra `Cannot load library libgssapi_krb5.so.2` (a imagem `runtime-deps` não tem Kerberos; o Npgsql tenta carregá-lo e segue sem) e, na primeira execução, um `Failed executing DbCommand` ao ler `__EFMigrationsHistory` antes de ela existir (comportamento normal do EF Core).
