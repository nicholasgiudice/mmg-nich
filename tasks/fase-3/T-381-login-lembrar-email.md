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

**Feito.**

**Tela** (`client/scripts/Account/AccountScreen.cs`): caixa **"Lembrar e-mail"** logo abaixo do campo da senha, no texto claro de pedra da tela (T-088). O tooltip diz que só o e-mail é guardado.
- Ao abrir, o campo vem com o e-mail gravado e o foco vai para a senha.
- Dentro da mesma sessão, o e-mail em memória (`s_email`) continua valendo, como antes. Ex.: sair da conta e voltar.

**Gravação** (`client/scripts/Account/LoginMemory.cs`). A escolha só é gravada ao **entrar com sucesso**, pelo botão Entrar, pelo Enter na senha ou por Criar conta. Fica na seção `[account]` de `user://settings.cfg`, junto com o `EffectSettings` e o `BattleListSettings`.
- **Marcada:** grava `email` e `remember_email="true"`.
- **Desmarcada:** apaga `email` e grava `remember_email="false"`.
- **Estado da caixa ao abrir:**
  - com estado gravado, usa o gravado;
  - sem estado gravado, fica marcada só se há e-mail gravado;
  - na primeira vez, vem desmarcada.
- **A senha nunca é gravada.** Ela só passa do campo para o pedido à API, como antes.
- **`--login=email:senha`:** continua entrando sozinho e não grava nada.
- **Organização do código:** a regra fica numa classe sem Godot, sobre `IAccountSettings`. Só a gravação no arquivo (`ConfigFile`) fica sob `#if GODOT`, para os testes compilarem a classe por link.

**Teste no jogo:**
- **Por que uma API falsa:** esta máquina não tem PostgreSQL, então a `Game.Api` não sobe aqui. Usei uma API falsa, um servidor HTTP de 30 linhas fora do repositório, que aceita qualquer login (`POST /sessions`, `GET /characters`).
- **Gancho provisório:** para preencher a tela e apertar Entrar pelo mesmo caminho do botão, usei um argumento provisório no `AccountScreen`. Ele não entrou no commit.
- **`settings.cfg`:** fiz uma cópia do arquivo antes e o restaurei no fim.
- **Sequência:** cada passo é o jogo aberto e fechado de novo.
  1. Nada gravado: campo vazio, caixa desmarcada, foco no e-mail. Entrei com a caixa marcada, e o arquivo ficou com `remember_email="true"` e `email="ana@example.com"`.
  2. Reabri: e-mail preenchido, caixa marcada, foco na senha (`login-email-lembrado.png`).
  3. Desmarquei e entrei: o `email` sumiu do arquivo, e ficou `remember_email="false"`.
  4. Reabri: campo vazio, caixa desmarcada (`login-desmarcado-vazio.png`).
  5. Entrei com `--login=bia@example.com:...`: o login foi feito, e o arquivo não mudou.
- **A senha nunca apareceu no arquivo.**
- **Capturas:** em `tasks/reports/interface/login-lembrar-email/`.

**Testes:** `tests/Game.Client.Tests/LoginMemoryTests.cs` cobre:
- começar vazio e desmarcado;
- e-mail marcado lembrado na abertura seguinte;
- desmarcar apaga o e-mail;
- e-mail sem estado começa marcado;
- só o e-mail e o estado são gravados.

`dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

**Desvios:**
- **Branch:** criada a partir de `docs/correcoes-playtest-v0.3`, onde está o arquivo desta tarefa, e não de `main`.
- **Fora das áreas da tarefa:** `tests/Game.Client.Tests/Game.Client.Tests.csproj`, com o link do `LoginMemory.cs`, e `tasks/reports/interface/` para as capturas.

**Pendências:** testar com a API de verdade no próximo playtest.
