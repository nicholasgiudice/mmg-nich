# T-211 — `Game.Api`: cadastro, login, token e personagens

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-210
- **Libera:** T-212, T-213, T-220
- **Áreas que pode alterar:** `src/Game.Api/` (novo), `tests/Game.Api.Tests/` (novo), `deploy/`, `MmoRpg.sln`, `README.md`

## Contexto

`tech/stack-proposal-v0.1.md`: ASP.NET Core minimal API para login, lista de personagens e emissão de token de sessão; o jogo (UDP) valida o token. Contas de verdade substituem o "login de desenvolvimento" da T-105.

## Requisitos

1. Endpoints: `POST /accounts` (cadastro), `POST /sessions` (login → token de sessão curto, ex.: 15 min, assinado), `GET /characters`, `POST /characters` (nome + classe, nasce no nível 1 com o perfil e o equipamento inicial), `DELETE /characters/{id}`, `POST /characters/{id}/play` (token de jogo de uso único para aquele personagem e servidor).
2. Senha com hash forte (ex.: `PasswordHasher` do ASP.NET Identity); limite de tentativas por IP; validação de nome (tamanho, caracteres, único).
3. Token de jogo verificável pelo servidor de jogo **sem chamada de rede** (assinatura com chave compartilhada por variável de ambiente) e com expiração.
4. Serviço `api` no Compose, só na rede interna (o acesso de fora vem pelo Caddy da T-213).
5. Testes com `WebApplicationFactory` e banco de teste.

## Critérios de aceite

- fluxo cadastro → login → criar personagem → token de jogo coberto por testes;
- nenhuma senha ou segredo no repositório;
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
