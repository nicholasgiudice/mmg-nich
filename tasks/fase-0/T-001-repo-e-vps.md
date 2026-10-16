# T-001 — Repositório remoto e setup da VPS

- **Ambiente:** VPS
- **Modelo:** Humano (passos 1–3) + leve (passos 4–6)
- **Depende de:** —
- **Libera:** T-003
- **Áreas que pode alterar:** configuração do sistema, `.gitignore`

## Contexto

O código será editado em dois lugares: VPS Linux (sem tela) e Windows (com tela). Hoje o repositório só existe na VPS e não tem remoto. A VPS também não tem .NET instalado.

## Objetivo

Repositório sincronizável entre VPS e Windows e VPS pronta para compilar e testar C#.

## Passos

### Humano

1. Criar um repositório **privado** no GitHub (ex.: `mmo-rpg`), vazio, sem README.
2. Na VPS, gerar chave SSH (`ssh-keygen -t ed25519 -C "vps-mmo"`) e cadastrá-la no GitHub (Settings → SSH keys, ou deploy key com escrita).
3. Configurar `git config --global user.name` e `user.email`.

### Agente

4. Adicionar o remoto `origin`, fazer push de `main` e confirmar com `git ls-remote origin`.
5. Instalar o **.NET SDK 10 (LTS)** pelos pacotes do Ubuntu 24.04 (se não estiver no repositório padrão, usar `ppa:dotnet/backports`). Confirmar com `dotnet --info`.
6. Instalar utilitários: `unzip`, `jq`, `build-essential` se ausentes.

Docker, PostgreSQL e Godot headless **não** entram agora — são da fase 1 em diante.

## Critérios de aceite

- `git push` e `git pull` funcionam a partir da VPS;
- `dotnet --list-sdks` mostra um SDK 10.x;
- `dotnet new console -o /tmp/x && dotnet run --project /tmp/x` imprime "Hello, World!" (apagar depois).

## Resultado

Concluída em 2026-10-16.

- Remoto `origin` = `git@github.com:nicholasgiudice/mmg-nich.git` (SSH), `main` enviado e rastreando `origin/main`.
- Identidade Git configurada **no repositório** (não global): Nicholas Giudice / nicholas.giudice@outlook.com.
- .NET SDK **10.0.112** instalado pelo pacote `dotnet-sdk-10.0` do Ubuntu 24.04 (não precisou do PPA backports).
- `unzip`, `jq`, `build-essential` instalados.
- Hello World com `dotnet run` verificado.
