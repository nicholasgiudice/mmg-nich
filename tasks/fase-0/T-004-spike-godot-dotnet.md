# T-004 — Projeto Godot referenciando o Core

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** T-002, T-003
- **Libera:** T-020
- **Áreas que pode alterar:** `client/`, `Directory.Build.props` (somente `SharedTargetFramework`), `.gitignore`, `README.md`

## Contexto

O cliente é Godot 4 .NET e precisa usar `src/Game.Core` diretamente (mesmo código que o servidor usará). Este é um spike para provar a integração antes de construir o cliente.

## Objetivo

Projeto Godot em `client/` que compila, referencia `Game.Core` e exibe algo vindo dele.

## Requisitos

1. Criar o projeto Godot .NET em `client/` (nome `MmoRpgClient`), renderizador **Compatibility** ou **Forward+** (registrar a escolha; Compatibility é suficiente para 2D).
2. No `.csproj` do cliente, adicionar `ProjectReference` para `../src/Game.Core/Game.Core.csproj`.
3. **Framework alvo:** tentar `net10.0` no cliente. Se o Godot instalado não aceitar, mudar `SharedTargetFramework` em `Directory.Build.props` para o framework que o Godot suporta (provavelmente `net8.0`) e confirmar que `./scripts/check.sh` continua passando. Registrar no Resultado.
4. O `Directory.Build.props` da raiz também se aplica ao `client/`. Se `TreatWarningsAsErrors` quebrar o código gerado pelo Godot, criar `client/Directory.Build.props` que importa o da raiz e desativa só o que for necessário. Registrar.
5. Adicionar em `Game.Core` uma classe mínima `CoreInfo` com `public const string Version = "0.0.1";` e exibi-la num `Label` na cena principal.
6. Corrigir `.gitignore`: no Godot 4 os arquivos `*.import` **devem** ser versionados — remover essa linha. Manter `.godot/` ignorado.
7. Verificar que o projeto também compila com `dotnet build client/` pela linha de comando.
8. Documentar no `README.md` como abrir o cliente.

## Fora do escopo

Qualquer gameplay, input, entidades.

## Critérios de aceite

- F5 no editor abre a janela mostrando "Core 0.0.1";
- `dotnet build client/` sem erros;
- `./scripts/check.sh` continua passando (rodar na VPS após o push).

## Resultado

_(preencher: versão do Godot, framework alvo final, ajustes no build)_
