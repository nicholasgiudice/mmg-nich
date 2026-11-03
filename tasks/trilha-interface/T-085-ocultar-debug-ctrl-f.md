# T-085 — Esconder logs e informações de debug (Ctrl+F)

- **Ambiente:** Windows
- **Modelo:** Leve
- **Depende de:** —
- **Libera:** —
- **Áreas que pode alterar:** `client/scripts/Main.cs`, `client/scripts/Hud/` (`Hud`, `CombatLogPanel`), `client/scripts/Input/` (`InputActions`, `PlayerInput`), `client/project.godot` (Input Map)

## Contexto

Hoje a tela de jogo mostra informações de desenvolvimento o tempo todo:
- o texto do canto em `Main.cs`: versão do Core, FPS, tick, servidor, ping e perda;
- o log de combate (`CombatLogPanel`);
- a linha de ajuda com as teclas (canto inferior esquerdo, `Hud`).

O designer quer **tudo isso escondido**, aparecendo só com **Ctrl+F**.

## Requisitos

1. Nova ação `toggle_debug` no Input Map: **Ctrl+F**. Ela alterna a visibilidade de:
   - a informação de debug do canto;
   - o log de combate;
   - a linha de ajuda;
   - a grade de debug (`--debug-grid`), se estiver ligada.
2. **Começa escondido.** O estado fica lembrado no `user://settings.cfg` (o mesmo arquivo da intensidade dos efeitos, `EffectSettings`).
3. **F sozinho continua pegando item** (`pickup`). Ctrl+F não pode pegar item, e F não pode alternar o debug. Confira que o Input Map distingue o modificador.
4. **Continua sempre visível** o que é jogo, não debug:
   - quadros do jogador, do alvo e do grupo;
   - barra de habilidades;
   - conjuração;
   - avisos de fase do chefe;
   - recusas;
   - números flutuantes;
   - resumo de morte.
5. Os `GD.Print` de diagnóstico (`[pred]`, `[tick ...]`) continuam no console; esta tarefa trata só do que aparece na tela.
6. Os argumentos de linha de comando de teste (`--demo-input`, `--debug-grid`) seguem funcionando. Com `--debug-grid` o debug pode começar visível.
7. Uma dica curta na tela de pausa: "Ctrl+F: informações de debug".

## Critérios de aceite

- ao entrar no jogo nada de debug aparece; Ctrl+F mostra, Ctrl+F esconde;
- fechar e abrir o jogo mantém a última escolha;
- F pega item com o debug aberto ou fechado, e Ctrl+F nunca pega item;
- `./scripts/check.sh` passa.

## Resultado

_(preencher ao concluir)_
