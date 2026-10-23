# T-220 — Cliente: login, seleção e criação de personagem

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** T-211
- **Libera:** T-223, T-230
- **Áreas que pode alterar:** `client/`

## Contexto

Com a `Game.Api` (T-211), o cliente deixa o "Jogar online com nome" (T-107) e passa a entrar com conta.

## Requisitos

1. Telas: login/cadastro, lista de personagens (nome, classe, nível), criar (nome + classe, com a descrição da classe) e apagar (com confirmação).
2. "Jogar": pede o token de jogo e conecta no servidor com ele (`NetAuthority`).
3. Erros claros (senha errada, nome em uso, servidor fora). O token de sessão fica só em memória (não gravar senha nem token em disco).
4. O modo local (menu de cenários) continua existindo para teste.

## Critérios de aceite

- entrar, criar personagem e jogar contra a VPS de ponta a ponta;
- `dotnet build client/` e `./scripts/check.sh` passam.

## Resultado

_(preencher ao concluir)_
