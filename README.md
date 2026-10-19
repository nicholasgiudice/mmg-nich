# MMO RPG Medieval

Projeto de um MMO RPG 2D medieval com foco inicial em mapa, PvE, quatro classes e habilidades.

## Estado atual

O projeto está em fase de idealização e prototipagem. O primeiro objetivo é validar um vertical slice pequeno antes de construir sistemas online complexos.

## MVP conceitual

- 1 cidade-fortaleza;
- 3 regiões externas;
- 1 dungeon;
- 1 miniboss;
- 1 boss;
- 4 classes base;
- combate em tempo real;
- PvE solo e em grupo;
- habilidades com cooldown, pagas com Mana (regenera com o tempo) e aprendidas com pontos a cada nível.

## Como compilar

Requer o .NET SDK 10. Para formatar-verificar, compilar e testar tudo:

```bash
./scripts/check.sh
```

## Como abrir o cliente

O cliente fica em `client/` e é um projeto Godot 4 **edição .NET** (testado com 4.7.2) que referencia `src/Game.Core`.

1. Instale o Godot 4 .NET (no Windows: `winget install GodotEngine.GodotEngine.Mono`) e o .NET SDK 10.
2. No Godot, use **Importar** e escolha `client/project.godot`.
3. Pressione **F5**: a janela deve mostrar `Core 0.0.1`.

Para só compilar pela linha de comando:

```bash
dotnet build client/
```

O `client/` não faz parte de `MmoRpg.sln`, então `./scripts/check.sh` não depende do Godot.

## Documentação

- `docs/vision-mvp-v0.1.md` — visão, escopo e decisões;
- `design/classes-v0.1.md` — quatro classes base;
- `design/first-map-v0.1.md` — primeiro mapa;
- `design/class-map-validation-v0.1.md` — validação das classes contra o mapa;
- `design/combat-encounters-v0.1.md` — encontros de combate;
- `tech/architecture-v0.1.md` — direção tecnológica;
- `tech/stack-proposal-v0.1.md` — proposta de stack (linguagem, servidor, rede, dados, persistência);
- `tasks/README.md` — plano de execução e tarefas para agentes;
- `AGENTS.md` — regras para agentes que trabalham no repositório.

## Fora do MVP

PvP, guerras territoriais, guildas, economia complexa, evoluções de classe, crafting lendário, múltiplas cidades e mundo dinâmico ficam para depois.
