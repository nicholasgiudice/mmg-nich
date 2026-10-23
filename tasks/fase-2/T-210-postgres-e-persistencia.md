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

_(preencher ao concluir)_
