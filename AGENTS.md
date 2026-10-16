# Instruções para agentes

Este arquivo vale para qualquer agente (qualquer modelo/ferramenta) que trabalhe neste repositório. Leia-o inteiro antes de começar.

## O projeto

MMO RPG 2D medieval, visão de cima, combate PvE em tempo real. Documentos de referência:

- `docs/vision-mvp-v0.1.md` — visão e escopo;
- `design/*.md` — classes, mapa, encontros;
- `tech/stack-proposal-v0.1.md` — stack e arquitetura (**fonte de verdade técnica**);
- `tasks/README.md` — plano de execução e índice de tarefas.

## Como trabalhar numa tarefa

1. Você recebe **uma** tarefa (`tasks/fase-N/T-XXX-*.md`). Execute somente ela.
2. Leia a tarefa inteira, depois os documentos que ela cita.
3. Confirme que as dependências listadas estão concluídas (seção `## Resultado` preenchida e código em `main`). Se não estiverem, pare e informe.
4. Crie a branch `task/T-XXX-nome-curto` a partir de `main` atualizada.
5. Altere apenas as áreas listadas em "Áreas que pode alterar". Se precisar mexer em outra, faça o mínimo e registre no Resultado.
6. Ao terminar, preencha a seção `## Resultado` no próprio arquivo da tarefa: o que foi feito, decisões tomadas, desvios, pendências.
7. Faça commit na branch. Não faça merge em `main`, nunca faça push de `main` e nunca use push forçado — o humano revisa e integra. Se a tarefa roda no Windows, faça `git push -u origin task/T-XXX-...` ao terminar, para a integração ser feita a partir da VPS. Na VPS, não faça push (a integração é local).

Se algo for ambíguo, escolha a opção mais simples coerente com `tech/stack-proposal-v0.1.md`, siga em frente e registre a decisão no Resultado. Só pare se estiver realmente bloqueado.

## Ambientes

| Ambiente | Uso |
|---|---|
| **VPS Linux** (Ubuntu 24.04, sem interface gráfica) | tudo que não precisa de tela: `Game.Core`, `Game.Data`, testes, simulador, servidor, API, banco, builds headless |
| **Windows** (com tela) | editor do Godot, Tiled, testes visuais e playtest |

Cada tarefa indica o ambiente. Tarefas de VPS não podem depender de interface gráfica.

## Regras de arquitetura (obrigatórias)

- `src/Game.Core` **não referencia Godot** nem nenhuma biblioteca de engine, rede ou banco.
- A simulação avança por **tick fixo de 20 Hz**. Tempo de jogo é medido em ticks, nunca com `DateTime.Now`, `Stopwatch` ou tempo de frame.
- Aleatoriedade somente pelo RNG da simulação (com seed). Proibido `new Random()` sem seed ou `Random.Shared` no Core.
- Cliente e UI **não aplicam efeitos**: enviam intenções e exibem estado e eventos.
- Regras de jogo (números, habilidades, monstros) vivem em `data/*.json`, não hardcoded em código.
- Física do Godot nunca decide nada de gameplay; colisão de gameplay é do Core.
- Unidade de mundo: **1 unidade = 1 tile**. O cliente converte para pixels.

## Convenções de código

- C#, `Nullable` habilitado, avisos tratados como erro (configurado em `Directory.Build.props`).
- Identificadores de código e ids de dados em **inglês** (`warrior.heavy_strike`); textos exibidos ao jogador e documentação em **português**.
- Ids de dados: `snake_case`, com namespace por ponto (`monster.grey_wolf`, `status.poison`).
- Testes com xUnit em `tests/`. Toda regra nova do Core vem com teste.
- Formatação: `dotnet format` antes de cada commit.
- Mensagens de commit no formato `tipo(escopo): descrição` — ex.: `feat(core): add cooldown validation`.

## Definição de pronto (toda tarefa)

- `dotnet build` sem erros nem avisos;
- `dotnet test` passando;
- critérios de aceite da tarefa atendidos;
- seção `## Resultado` preenchida;
- commit feito na branch da tarefa.

## Fora de escopo para qualquer tarefa

PvP, guildas, guerras, economia, crafting, chat — salvo se a tarefa pedir explicitamente.
