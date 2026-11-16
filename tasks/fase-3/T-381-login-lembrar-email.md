# T-381 — Login: lembrar o e-mail

- **Ambiente:** Windows
- **Modelo:** Leve
- **Depende de:** —
- **Libera:** —
- **Áreas que pode alterar:** `client/scripts/Account/`, `tests/Game.Client.Tests/`

## Contexto

Pedido do designer durante o playtest da fase 3 (T-380), em 2026-11-16: hoje o e-mail só fica lembrado enquanto o jogo está aberto (`AccountScreen.s_email`, em memória). Ao abrir o jogo de novo, é preciso digitar tudo.

## Requisitos

1. Caixa de seleção **"Lembrar e-mail"** na tela de login, logo abaixo dos campos, no mesmo estilo da tela (T-088).
2. Marcada: ao entrar com sucesso, o e-mail é gravado em `user://settings.cfg` (seção `[account]`, como o `EffectSettings` e o `BattleListSettings` fazem). Ao abrir o jogo, o campo vem preenchido e o foco vai para a senha.
3. Desmarcada: o e-mail gravado é apagado ao entrar.
4. O estado da caixa também fica gravado (começa marcada se havia e-mail gravado).
5. **A senha nunca é gravada**, nem em memória entre sessões.
6. O argumento `--login=email:senha` continua funcionando e não grava nada.

## Critérios de aceite

- abrir o jogo, entrar com a caixa marcada, fechar e abrir: o e-mail aparece preenchido; desmarcar, entrar, fechar e abrir: o campo vem vazio;
- captura da tela de login com a caixa;
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
