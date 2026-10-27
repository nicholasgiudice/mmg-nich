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

**Feito** (VPS, 2026-10-27). `./scripts/check.sh` completo passa com o PostgreSQL de teste (36 testes da API, 6 deles de integração; sem `MMO_TEST_DB` esses 6 são pulados).

**`src/Game.Api`** (ASP.NET Core minimal API, sem pacotes além do framework):
- Endpoints do requisito 1, mais `GET /` (saúde, para o Caddy da T-213). Tabela no `README.md`, seção "API de contas e personagens".
- **Sessão:** token próprio assinado (HMAC-SHA256, chave `MMO_SESSION_KEY`), 15 min, em `Authorization: Bearer`. Validado por um `AuthenticationHandler` (esquema `Session`); `/characters` exige autenticação.
- **Senha:** `PasswordHasher` do ASP.NET Identity (PBKDF2-HMAC-SHA512, sal aleatório), 8 a 128 caracteres. O login com e-mail inexistente verifica contra um hash fictício, para demorar o mesmo que senha errada; os dois respondem `invalid_credentials`.
- **Limite por IP:** `POST /accounts` e `POST /sessions` com janela fixa de 10 pedidos por minuto (`MMO_AUTH_RATE_LIMIT`), 429 ao passar. O IP vem do `X-Forwarded-For` (a API só é alcançável pela rede interna, então todo proxy é confiável).
- **Nome:** 3 a 16 letras latinas (com acento) ou dígitos, começando por letra, normalizado em NFC; único sem diferenciar maiúsculas (índice da T-210). Classe: as de `data/classes.json` com atributos.
- **Erros:** ProblemDetails com `title` em português e `code` estável para o cliente (T-220).
- **Token de jogo** (`POST /characters/{id}/play`): conta, personagem, servidor (`MMO_GAME_SERVER_ID`, padrão `game.dev`), expiração de 60 s e um id único. A resposta traz também `address` (`MMO_GAME_SERVER_ADDRESS`), para o cliente saber onde conectar.

**`src/Game.Auth`** (novo, sem dependências; fora das áreas listadas): `SignedToken` (assinar/ler com HMAC-SHA256, comparação em tempo constante), `GameTicket`, `GameTickets.Validate` e `GameTicketReplayGuard`. Fica separado da API para o servidor de jogo (T-212) referenciar sem levar ASP.NET nem EF. Validar não faz chamada de rede: assinatura, tipo, servidor e expiração. O uso único é do servidor: o `GameTicketReplayGuard` lembra os ids aceitos até expirarem. Cada token leva um tipo (`session`/`game`), então um nunca vale como o outro, nem com a mesma chave.

**Deploy:** serviço `api` no `deploy/compose.yml` (`deploy/api/Dockerfile`, imagem `aspnet:10.0`, sem root), sem `ports:`, depois do `migrate`. As duas chaves são obrigatórias em `deploy/.env` (modelo e comando para gerar em `deploy/.env.example`). Na VPS: chaves geradas no `deploy/.env`, `docker compose up -d --build api` subiu, e o fluxo cadastro → login → criar → jogar → apagar respondeu 201/200/201/200/204 de dentro da rede do Compose. A porta 8080 não responde no host. A conta do teste foi apagada.

**Testes** (`tests/Game.Api.Tests`, `WebApplicationFactory` + banco `mmo_test_*` descartável, relógio controlado):
- fluxo completo, com o token de jogo validado como o servidor fará e recusado na segunda vez;
- senha errada e e-mail inexistente iguais, e-mail sem diferenciar maiúsculas, e-mail repetido, hash sem a senha, último login gravado;
- nome inválido, repetido em outra conta (maiúsculas diferentes) e classe inexistente; limite de personagens;
- uma conta não vê, não apaga e não joga com personagem de outra;
- sessão expira em 15 min; sem token, com chave errada ou expirado → 401;
- limite por IP (429; outro IP no `X-Forwarded-For` continua passando);
- tokens: adulterado, chave errada, outro servidor, expirado, tipos trocados, uso único, chaves curtas ou fora de base64.

**Decisões**
- **Progressão inicial `"{}"`**: o personagem novo é gravado com nível 1, XP 0 e documento de progressão vazio. "Vazio" quer dizer "nada distribuído e o equipamento inicial da classe": é o que `Leveling.SpawnCharacter` sem build já faz (`EntityFactory.SpawnPlayer` equipa `startingEquipment`). O formato do documento continua com a T-212, que é quem grava.
- **Zona inicial `zone.city`, posição (0, 0)**: a Cidade ainda não existe; (0, 0) quer dizer "ponto de nascimento da zona" para a T-214.
- **Token próprio em vez de JWT**: um formato só para sessão e jogo, que o servidor valida com 30 linhas e sem pacote. Não é criptografado (o cliente pode ler o conteúdo, sem segredo dentro).
- **Até 5 personagens por conta** (não estava no requisito; protege o banco). A checagem não é atômica: dois pedidos simultâneos podem passar de 5 (aceitável agora).
- Sem renovação de sessão: depois de 15 min o cliente faz login de novo.
- Rehash de senha com parâmetros antigos (`SuccessRehashNeeded`) não é feito: o repositório não tem "trocar hash". Só importa quando o .NET mudar o padrão.

**Fora das áreas da tarefa:** `src/Game.Auth/` (novo, explicado acima) e `deploy/.env` local na VPS (fora do Git).

**Pendências**
- **T-212:** referenciar `Game.Auth`, ler `MMO_GAME_TICKET_KEY` e `MMO_GAME_SERVER_ID` (o mesmo valor da API) no servidor, e usar um `GameTicketReplayGuard` por processo. Passar as duas variáveis ao `game-server` no Compose. Tratar progressão `"{}"` como personagem novo.
- **T-213:** Caddy → `api:8080`. O `GET /` já responde.
- **T-220:** usar os `code` de erro; `address` vem vazio enquanto `MMO_GAME_SERVER_ADDRESS` não estiver no `.env`.
