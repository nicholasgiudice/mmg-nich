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

Concluída em 2026-10-15 no Windows.

**Feito**

- Projeto Godot `MmoRpgClient` em `client/` (`project.godot`, `scenes/Main.tscn`, `scripts/Main.cs`), com `ProjectReference` para `../src/Game.Core/Game.Core.csproj`.
- `Game.Core.CoreInfo.Version = "0.0.1"` (`src/Game.Core/CoreInfo.cs`), exibido num `Label` centralizado: a janela mostra "Core 0.0.1".
- `.gitignore`: removida a linha `*.import`; `.godot/` continua ignorado.
- `README.md`: seção "Como abrir o cliente".

**Versões e decisões**

- Godot **4.7.2-stable mono** (instalado via `winget install GodotEngine.GodotEngine.Mono`), SDK `Godot.NET.Sdk/4.7.2`.
- Renderizador **Compatibility** (`gl_compatibility`), suficiente para 2D.
- Framework alvo final: **`net10.0`**. O Godot 4.7.2 aceita e roda .NET 10, então `SharedTargetFramework` **não** foi alterado.
- O `TargetFramework` do cliente está escrito literalmente como `net10.0`, e não como `$(SharedTargetFramework)`. Ao abrir o projeto, o editor do Godot não reconheceu a propriedade, reescreveu o `.csproj` com `net8.0` e deixou um `MmoRpgClient.csproj.old`. Com o valor literal, ele não mexe mais no arquivo. Se `SharedTargetFramework` mudar, atualize esse valor também.
- Não foi preciso um `client/Directory.Build.props`: o código gerado pelo Godot compila sem avisos com o `TreatWarningsAsErrors` da raiz.
- O `client/` **não** foi adicionado ao `MmoRpg.sln`, para que `./scripts/check.sh` na VPS não dependa do SDK do Godot. O editor não gerou `.sln` em `client/`, então `dotnet build client/` funciona sem ambiguidade.
- Os arquivos `*.uid` (Godot 4.4+) são versionados, junto com os scripts.

**Verificação**

- `dotnet build client/`: 0 avisos, 0 erros.
- `dotnet format --verify-no-changes` nos arquivos novos: ok.
- Godot: `--build-solutions` e execução da cena principal (gravação de quadros com `--write-movie`) mostrando "Core 0.0.1".
- `dotnet build MmoRpg.sln -warnaserror`: ok.

**Pendências**

- Rodar `./scripts/check.sh` na VPS após o push. No Windows, o `dotnet format` e 3 testes de `Game.Data.Tests` falharam **só** porque o clone foi feito com `core.autocrlf=true` (padrão do Git for Windows), o que deixa CRLF na cópia de trabalho: o `.editorconfig` exige LF, e os testes buscam trechos com `\n` nos JSON. O código não tem problema. Sugestão de tarefa futura: adicionar um `.gitattributes` com `* text=auto eol=lf` para que todo clone no Windows já venha em LF.
