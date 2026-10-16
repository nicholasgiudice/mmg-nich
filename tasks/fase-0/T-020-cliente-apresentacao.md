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

Concluída em 2026-10-16 no Windows (Godot 4.7.2 .NET, `net10.0`).

**Feito:**
- `src/Game.Core/Authority/IGameAuthority.cs`: `ControlledEntity`, `CurrentTick`, `InterpolationFraction`, `Latest`/`Previous` (`WorldSnapshot`), `Advance(delta)`, `Send(intent)`, `TakeEvents()`.
- `client/scripts/Authority/LocalAuthority.cs`: roda o `World` com acumulador de passo fixo (20 Hz), no máximo 5 ticks por chamada (o atraso excedente é descartado); só aceita intenções da entidade controlada; acumula eventos de cada tick até `TakeEvents()`.
- `client/scripts/Authority/FakeAuthority.cs`: autoridade roteirizada sem regras (herói andando em círculo, alvo perdendo vida até morrer). Rodar com `-- --fake-authority` mostra que a apresentação funciona sem nenhuma mudança.
- `client/scripts/Data/ContentLoader.cs`: lê `res://data/*.json` com `FileAccess` (funciona em build exportado), valida com `Game.Data` e converte em `ContentCatalog` (mesma conversão do `Game.Sim`).
- `client/scripts/Presentation/`: `Units` (`PixelsPerUnit = 32`, constante única), `ArenaView` (grade desenhada com `_Draw`), `EntityView` (círculo por facção, jogador controlado em ciano, indicador de direção, nome, barra de vida; cinza quando morto), `WorldView` (um nó por unidade, criado/removido pelo snapshot e por `EntityRemoved`; posição e direção interpoladas entre `Previous` e `Latest`; câmera segue o jogador).
- `client/scripts/Main.cs` + `scenes/Main.tscn`: raiz que escolhe a autoridade, liga a apresentação, mostra versão/FPS/tick e lê o input temporário (WASD/setas → `MoveIntent`/`StopIntent`, enviado só quando a direção muda).
- `MmoRpgClient.csproj`: referência a `Game.Data` e target `CopyGameData` que recria `client/data/` a partir de `../data/**/*.json` antes de cada build.

**Decisões:**
- A interface expõe `WorldSnapshot` do Core (componentes clonados) em vez de um DTO próprio: as próximas tarefas (HUD, status) leem o que precisam sem estender a interface. Para a rede (fase 1) o construtor de `EntitySnapshot`, hoje `internal`, terá de ser aberto ou ganhar uma fábrica.
- `Advance(delta)` faz parte da interface: o cliente de rede também precisa de um ponto por frame (ler pacotes, relógio de interpolação).
- A apresentação desenha um tick atrás (interpola entre os dois últimos), ~50 ms de atraso.
- Só entidades com `Transform` + `Health` viram `EntityView`; telegráfos e áreas ficam para a T-023.
- Como a T-014 já está em `main`, o encontro padrão é o real `encounter.three_wolves` (não fixture). Argumentos de depuração (depois de `--`): `--encounter=id`, `--seed=n`, `--fake-authority`, `--auto-move=x,y` e `--capture=arquivo.png` (salva a tela após 2 s e fecha — usado para verificar a cena sem interação).
- `GridMap` conflita com `Godot.GridMap`; nos arquivos do cliente usa-se `using GridMap = Game.Core.Spatial.GridMap;`.

**Verificação:** `dotnet build client/` sem avisos; `./scripts/check.sh` OK (294 testes) no Windows; cena executada com captura de tela: arena, Guerreiro e 3 Lobos com IA, 60 FPS; com `--auto-move` o jogador anda e a câmera segue; com `--fake-authority` a mesma apresentação roda a autoridade falsa. A suavidade com teclado (60+ FPS) foi verificada só por captura, não por playtest humano — vale conferir com F5.

**Fora das áreas listadas:** `.gitignore` (adicionado `client/data/`).

**Ambiente:** o clone do Windows ainda tinha 28 arquivos em CRLF na cópia de trabalho (problema registrado na T-002), o que quebrava 3 testes de `Game.Data.Tests`. Foram convertidos para LF no lugar (sem diferença para o Git); agora `check.sh` passa no Windows.
