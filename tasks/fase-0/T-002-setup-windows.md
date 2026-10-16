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

_(preencher ao concluir: versão do Godot instalada, versão do .NET, editor escolhido)_
