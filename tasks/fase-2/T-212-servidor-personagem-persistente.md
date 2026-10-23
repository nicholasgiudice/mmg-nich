# T-212 — Servidor: entra com token, carrega e salva o personagem

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-203, T-204, T-210, T-211
- **Libera:** T-214, T-221, T-222, T-230
- **Áreas que pode alterar:** `src/Game.Server/`, `src/Game.Protocol/`, `src/Game.Persistence/`, `tests/Game.Server.Tests/`, `tests/Game.NetBots/` (entrar com token de teste)

## Contexto

Hoje o servidor aceita um nome (`Hello.DevName`) e cria um personagem nível 1 a cada entrada. Com contas e progressão, ele passa a carregar o personagem salvo e gravar o que mudou (XP, nível, pontos, NH, inventário, equipamento, posição).

## Requisitos

1. `Hello` leva o **token de jogo** (T-211) em vez do nome; o servidor valida assinatura, expiração e uso único. Modo de desenvolvimento por opção (`--dev-login`) mantém o login só com nome para testes locais e bots.
2. Ao entrar: carrega o personagem do banco e cria a entidade com nível, atributos, NH, equipamento e inventário. Um personagem só pode estar conectado uma vez.
3. **Gravação assíncrona e transacional fora do loop de 20 Hz**: ao sair, a cada N minutos e em eventos importantes (subir de nível, item raro). O loop nunca espera o banco; falha de gravação é registrada e repetida.
4. Ao parar o servidor (SIGTERM, T-207), grava todos os conectados antes de sair.
5. Protocolo v+1; bots de rede da T-109 continuam funcionando com `--dev-login`.

## Critérios de aceite

- teste: entra, ganha XP e item, sai, entra de novo e está tudo lá;
- teste: o tick não espera o banco (gravação lenta simulada não aumenta o tempo de tick);
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
