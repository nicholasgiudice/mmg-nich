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

_(preencher ao concluir)_
