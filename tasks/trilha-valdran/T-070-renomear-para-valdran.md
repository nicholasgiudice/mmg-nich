# T-070 — Renomear o jogo para Valdran

- **Ambiente:** Windows (export e conferência visual do cliente)
- **Modelo:** Leve
- **Depende de:** —
- **Libera:** T-071 (o site já usa o nome novo)
- **Áreas que pode alterar:** `client/project.godot`, `client/export_presets.cfg`, `client/scripts/` (só textos exibidos), `scripts/export-client.ps1`, `README.md`, `AGENTS.md`, `docs/`, `design/`, `tech/`, `tasks/README.md`, `deploy/systemd/*.service` e `*.timer` (só o campo `Description`)

## Contexto

O jogo passa a se chamar **Valdran** (decisão do designer, 2026-11-02). Hoje o nome que aparece é "MMO RPG Medieval" ou "MmoRpg":

- `client/project.godot`: `config/name="MmoRpgClient"` (título da janela);
- `client/export_presets.cfg`: `application/product_name` e `application/file_description` = "MMO RPG Medieval";
- `scripts/export-client.ps1`: gera `build/windows/MmoRpg.exe` e `build/MmoRpg-windows.zip`;
- `client/scripts/Match/MenuScreen.cs`: título "Protótipo de combate"; a tela de login (`client/scripts/Account/AccountScreen.cs`) não mostra o nome do jogo;
- `README.md` ("# MMO RPG Medieval"), `AGENTS.md`, `docs/vision-mvp-v0.1.md`, `tasks/README.md` e as descrições das unidades systemd ("MMO RPG - ...").

## Requisitos

1. **Nome exibido ao jogador** passa a ser **Valdran**: título da janela (`config/name`), nome do produto e descrição no export do Windows, executável e zip (`Valdran.exe`, `Valdran-windows.zip`) e título na tela de login e no menu.
2. **Documentação**: README, AGENTS, visão e plano de tarefas chamam o jogo de Valdran ("Valdran, MMO RPG 2D medieval..."). Não reescreva os `## Resultado` de tarefas já concluídas; eles registram o que era verdade na época.
3. **Descrições** das unidades systemd (`Description=Valdran - ...`). Não troque o nome dos arquivos `.service`/`.timer`, porque eles já estão instalados na VPS.
4. **Identificadores internos ficam como estão.** Trocar qualquer um deles quebra o deploy, o banco ou os backups e não muda nada para o jogador:
   - namespace e assembly `MmoRpgClient`, `MmoRpg.sln`, `client/sln/`;
   - variáveis `MMO_*`, projeto do Compose `mmo-rpg`, imagens `mmo-rpg/*`, usuário e banco `mmo`;
   - pastas `/root/mmo-rpg`, `/opt/mmo-rpg`, `/var/backups/mmo-rpg`, nomes das unidades systemd;
   - o `"service": "mmo-api"` do health check da API, o nome do repositório no GitHub e a pasta local.

   Liste no Resultado os que encontrar e recomende se vale uma tarefa de operação para renomeá-los depois.
5. **`user://` do Godot**: o diretório de dados do usuário vem de `config/name`, então trocar o nome faz o cliente perder o `user://settings.cfg` (intensidade dos efeitos, T-054). Escolha a opção mais simples e registre a decisão: aceitar a perda, que é só uma preferência, ou fixar o diretório antigo com `application/config/use_custom_user_dir`.

## Critérios de aceite

- a janela do cliente, o `.exe` exportado e as propriedades do arquivo no Windows mostram "Valdran";
- login e menu mostram "Valdran";
- `git grep -i "mmo rpg medieval"` não encontra nada fora de `tasks/**/## Resultado` e `tasks/reports/`;
- `./scripts/check.sh` passa; o export pelo `scripts/export-client.ps1` gera `Valdran.exe` e o cliente conecta ao servidor como antes.

## Resultado

Concluída em 2026-11-02 no Windows. A pendência (teste de conexão com o executável novo) foi fechada pelo designer em 2026-11-05.

**O que foi feito**

- `client/project.godot`: `config/name="Valdran"` (título da janela). O `project/assembly_name` continua `MmoRpgClient`.
- `client/export_presets.cfg`: `product_name` e `file_description` = "Valdran"; `export_path` = `../build/windows/Valdran.exe`.
- `scripts/export-client.ps1`: gera `build\windows\Valdran.exe` e `build\Valdran-windows.zip`.
- Login (`client/scripts/Account/AccountScreen.cs`): "Valdran" pequeno acima do título de cada tela (Entrar, Personagens, Criar personagem).
- Menu de cenários (`client/scripts/Match/MenuScreen.cs`): o título passa a ser "Valdran", e "Protótipo de combate" vai para a linha de baixo, junto com a versão do Core.
- Documentação: `README.md` (título e zip), `AGENTS.md`, `docs/vision-mvp-v0.1.md` e `tasks/README.md` chamam o jogo de Valdran. Os `## Resultado` antigos e `tasks/reports/` não foram mexidos.
- `deploy/systemd/*.service` e `*.timer`: `Description=Valdran - ...`. Os nomes dos arquivos não mudaram. Na VPS, as descrições novas só valem depois de copiar as unidades e rodar `systemctl daemon-reload`; até lá, as instaladas continuam com o texto antigo, o que é inofensivo.

**Base da branch**

A branch saiu de `origin/main` e recebeu, por merge, `docs/tarefas-valdran`. Esse merge trouxe o arquivo desta tarefa e o commit `9441db8` (preset de export e `export-client.ps1`), que estava em `feat/ux-online-covil-matriarca` e ainda não tinha entrado em `main`.

**Decisões**

- **`user://`**: aceitar a perda. Com o nome novo, o Godot passa a usar a pasta `Godot/app_userdata/Valdran`. A pasta antiga (`MmoRpgClient`) só guardava o `settings.cfg` com a intensidade dos efeitos (T-054), que volta ao padrão uma vez. Não vale fixar o diretório antigo com `use_custom_user_dir` e manter o nome velho no disco para sempre.
- **Identificadores internos que ficaram** (requisito 4):
  - namespace e assembly `MmoRpgClient`, `client/MmoRpgClient.csproj`, `client/sln/MmoRpgClient.sln`, `MmoRpg.sln`;
  - variáveis `MMO_*` (API, servidor, auth, persistência), projeto `mmo-rpg` no Compose, imagens `mmo-rpg/*`, usuário e banco `mmo`;
  - `/root/mmo-rpg`, `/opt/mmo-rpg`, `/var/backups/mmo-rpg`; unidades `mmo-backup-db.*` e `mmo-game-server.service`;
  - `"service": "mmo-api"` no `/` da API; repositório `mmg-nich` no GitHub; o prompt "repositório mmo-rpg" em `tasks/README.md`.

  **Recomendação:** não renomear agora. Nenhum deles aparece para o jogador, e trocar os do deploy (Compose, banco, pastas, systemd, variáveis) exige migração cuidadosa na VPS: volume do Postgres, backups e unidades instaladas. Se o designer quiser, faça isso numa tarefa de operação na fase 4. Só o namespace do cliente poderia mudar sem risco, mas mexe em todos os arquivos de `client/scripts/` e conflitaria com qualquer tarefa de cliente em andamento.

**Verificação**

- `./scripts/check.sh` completo: OK (format, build sem avisos e todos os testes; os 6 testes da API ignorados já eram ignorados antes).
- `scripts/export-client.ps1` (Godot 4.7.2 .NET): gerou `build\windows\Valdran.exe` (190 MB) e `build\Valdran-windows.zip` (73 MB). Nas propriedades do arquivo, `ProductName` e `FileDescription` = "Valdran".
- O `Valdran.exe` abriu com a janela "Valdran" e foi fechado depois de alguns segundos.
- `git grep -i "mmo rpg medieval"` só encontra o texto desta tarefa, que descreve o estado anterior.

**Pendências**

- ~~Humano: entrar com uma conta no `Valdran.exe` e confirmar que conecta ao servidor como antes.~~ **Feito em 2026-11-05:** o designer testou o executável gerado pelo `export-client.ps1`.
