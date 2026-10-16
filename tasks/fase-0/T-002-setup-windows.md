# T-002 — Setup do Windows

- **Ambiente:** Windows
- **Modelo:** Humano (com ajuda de agente leve, se quiser)
- **Depende de:** — (o clone depende do push da T-001)
- **Libera:** T-004
- **Áreas que pode alterar:** apenas a máquina Windows

## Objetivo

Máquina Windows pronta para abrir o projeto Godot com C# e rodar agentes.

## Passos

1. Instalar **Git for Windows** e configurar `user.name`/`user.email` (mesmos da VPS).
2. Cadastrar chave SSH do Windows no GitHub.
3. Instalar o **.NET SDK 10** (mesma versão maior da VPS).
4. Baixar o **Godot 4 — edição .NET** (versão estável mais recente 4.x) do site oficial. Atenção: é o download marcado ".NET", não o padrão.
5. Instalar um editor C#: VS Code com extensão C# Dev Kit, ou JetBrains Rider.
6. Clonar o repositório (depois da T-001).
7. Opcional: instalar o agente de código de sua preferência (ex.: Claude Code) no Windows para executar as tarefas de cliente.
8. Tiled **não** é necessário na fase 0 (a arena é definida em JSON); instalar só na fase 3.

## Critérios de aceite

- `dotnet --list-sdks` mostra SDK 10.x;
- Godot .NET abre e cria um projeto C# vazio que roda (F5) sem erro;
- `git pull` funciona no clone.

## Resultado

Concluída em 2026-10-15.

- **Git for Windows** com `user.name`/`user.email` configurados; chave SSH cadastrada no GitHub (`ssh -T git@github.com` autentica) e `git pull` funcionando.
- **.NET SDK 10.0.402** instalado via `winget install Microsoft.DotNet.SDK.10` (o SDK 8.0.100 continua instalado; o `global.json` escolhe o 10).
- **Godot 4.7.2-stable, edição .NET (mono)** instalado via `winget install GodotEngine.GodotEngine.Mono`. Aliases `godot`/`godot_console` no PATH (novo shell). Executável em `%LOCALAPPDATA%\Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_*\`.
- Editor C#: **VS Code** (1.128) já instalado.
- Claude Code instalado (app desktop).
- Projeto C# rodando no Godot: confirmado com o projeto da T-004 (`client/`), que compila e mostra "Core 0.0.1".

**Desvio / atenção:** o Git for Windows vem com `core.autocrlf=true` (configuração do sistema), e por isso o clone ficou com CRLF na cópia de trabalho. Com isso, `./scripts/check.sh` falha no Windows (`dotnet format` reclama de ENDOFLINE e 3 testes de `Game.Data.Tests` que buscam `\n` nos JSON quebram). Neste clone foi aplicado `git config core.autocrlf false`. Para os arquivos que já existiam passarem a LF, rode uma vez no clone, com a árvore limpa:

```bash
git rm --cached -r -q . && git reset --hard
```

Solução definitiva sugerida: `.gitattributes` com `* text=auto eol=lf`.
