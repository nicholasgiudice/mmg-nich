# T-020 — Cliente: autoridade local e sincronização de entidades

- **Ambiente:** Windows
- **Modelo:** **Forte** (base de todo o cliente)
- **Depende de:** T-004, T-007
- **Libera:** T-021
- **Áreas que pode alterar:** `client/`, `src/Game.Core/Authority/` (somente a interface)

## Contexto

Leia `tech/stack-proposal-v0.1.md` (seção 4, `IGameAuthority`) e as regras de `AGENTS.md`. No protótipo o Core roda dentro do Godot; na fase 1 o mesmo cliente vai conversar com o servidor. A apresentação **não pode** saber qual dos dois está do outro lado.

## Requisitos

1. **`IGameAuthority`** em `src/Game.Core/Authority/` (sem dependência do Godot): enviar intenção; ler entidades do último tick e do anterior; ler eventos novos; id da entidade controlada pelo jogador; tick atual e fração de interpolação.
2. **`LocalAuthority`** no cliente: cria o `World`, avança em passo fixo de 20 Hz com acumulador no `_Process` (sem depender de `_PhysicsProcess`), limita passos por frame para não travar.
3. **Dados**: target MSBuild no `.csproj` do cliente que copia `../data` para `client/data/` antes do build (pasta ignorada no Git); o cliente carrega de `res://data` via `Game.Data`. Até a T-014, usar uma arena e um encontro de fixture (ex.: 1 Guerreiro × 3 Lobos parados).
4. **Arena**: desenhar a grade (chão/parede) com `TileMapLayer` ou desenho simples; **32 px por unidade** (constante única).
5. **Visualização de entidades**: um nó por entidade criado/removido conforme eventos e snapshot; placeholder (círculo colorido por facção, indicador de direção, nome, barra de vida pequena). Posição **interpolada** entre os dois últimos ticks.
6. Câmera seguindo o jogador (detalhes na T-021).
7. Nenhum nó do Godot altera estado da simulação.

## Critérios de aceite

- Arena e entidades aparecem; mover o jogador com intenção de teste (tecla temporária) é suave a 60+ FPS mesmo com simulação a 20 Hz;
- trocar `LocalAuthority` por uma implementação falsa em teste não exige mudar a apresentação;
- `dotnet build client/` e `./scripts/check.sh` passam.

## Resultado

_(preencher ao concluir)_
