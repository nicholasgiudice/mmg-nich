# T-003 — Estrutura da solução .NET

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** T-001
- **Libera:** T-004, T-005, T-006
- **Áreas que pode alterar:** raiz do repositório (arquivos de build), `src/`, `tests/`, `scripts/`, `.gitignore`

## Contexto

Ver `tech/stack-proposal-v0.1.md`, seções 4 e 6. Esta tarefa cria apenas o esqueleto; nenhuma regra de jogo.

## Objetivo

Solução compilável com os projetos da fase 0, configuração comum e um script único de verificação usado por todos os agentes.

## Requisitos

1. `MmoRpg.sln` na raiz (formato `.sln` clássico, por compatibilidade com o Godot: `dotnet new sln --format sln`).
2. `global.json` fixando o SDK 10 com `rollForward: latestFeature`.
3. `Directory.Build.props` na raiz com:
   - `Nullable` habilitado, `ImplicitUsings` habilitado, `LangVersion` latest;
   - `TreatWarningsAsErrors` = true;
   - propriedade `SharedTargetFramework` = `net10.0` (usada pelas bibliotecas que o cliente Godot vai referenciar) e `AppTargetFramework` = `net10.0` (servidor, ferramentas, testes). A T-004 pode mudar `SharedTargetFramework` se o Godot exigir.
4. `.editorconfig` com estilo C# padrão (4 espaços, `file_scoped` namespaces, `var` quando óbvio).
5. Projetos:
   - `src/Game.Core` — classlib, `TargetFramework=$(SharedTargetFramework)`, **sem** dependências externas;
   - `src/Game.Data` — classlib, referencia `Game.Core`, `TargetFramework=$(SharedTargetFramework)`;
   - `tests/Game.Core.Tests` — xUnit, referencia `Game.Core`;
   - `tests/Game.Data.Tests` — xUnit, referencia `Game.Data`.
6. Um teste de fumaça em cada projeto de teste.
7. Pastas vazias com `.gitkeep`: `data/`, `data/schemas/`.
8. `scripts/check.sh`: roda `dotnet format --verify-no-changes`, `dotnet build -warnaserror` e `dotnet test`, retornando erro se algum falhar.
9. `.gitignore`: acrescentar `bin/`, `obj/`, `*.user`, `TestResults/`.
10. Atualizar `README.md` com a seção "Como compilar" (`./scripts/check.sh`).

## Fora do escopo

Projeto Godot (T-004), qualquer classe de jogo, servidor, Docker.

## Critérios de aceite

- `./scripts/check.sh` termina com sucesso numa cópia limpa do repositório;
- `Game.Core` não tem nenhum `PackageReference`.

## Resultado

**Feito:** `MmoRpg.sln` (formato clássico), `global.json` (SDK 10.0.100, `rollForward: latestFeature`), `Directory.Build.props`, `.editorconfig`, projetos `src/Game.Core`, `src/Game.Data`, `tests/Game.Core.Tests`, `tests/Game.Data.Tests` (um teste de fumaça cada), `data/.gitkeep`, `data/schemas/.gitkeep`, `scripts/check.sh`, `.gitignore` e seção "Como compilar" no `README.md`.

**Decisões:**
- `Game.Core` sem nenhum `PackageReference`; `TargetFramework` das bibliotecas usa `$(SharedTargetFramework)` e o dos testes `$(AppTargetFramework)`.
- `check.sh` roda `dotnet format`, `dotnet build -warnaserror` e `dotnet test --no-build` sobre `MmoRpg.sln`, com `set -euo pipefail`.
- Pacotes de teste (xunit 2.9.3, Test.Sdk 17.14.1, coverlet 6.0.4) vieram do template `dotnet new xunit`.
- `dotnet format` não reclamou de arquivos gerados; nenhuma exclusão foi necessária.

**Desvios/pendências:** nenhum.
