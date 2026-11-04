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

**Feito** (Windows, 2026-11-03). `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

- **`toggle_debug`** no Input Map: Ctrl+F (`physical_keycode` F com `ctrl_pressed`). `InputActions.ToggleDebug`.
- **`Hud/DebugView.cs`**: estado visível/escondido, começa escondido, gravado em `user://settings.cfg` (seção `debug`, chave `visible`), no mesmo arquivo da intensidade dos efeitos.
- **O que alterna:** o `InfoLabel` do canto (versão, FPS, tick, rede **e** a linha de ajuda, que são o mesmo rótulo), o log de combate (`Hud.ShowDebug`) e, com `--debug-grid`, a grade no lugar do mapa do Tiled. Com `--debug-grid` o debug começa visível só naquela execução (não grava).
- **F × Ctrl+F:** o Godot casa ações de tecla sem olhar modificadores a mais, então Ctrl+F também disparava `pickup`. Conferido num script contra o Input Map do projeto (`InputMap.event_is_action`): F→pickup exato `true`; Ctrl+F→pickup exato `false` (inexato `true`); F→toggle_debug `false`; Ctrl+F→toggle_debug `true`. Por isso `pickup` e `toggle_debug` são lidos com `exactMatch: true` em `Main.cs`.
- **Antes do jogo** (conectando, erro ao iniciar) o texto do canto continua visível: ele é a única mensagem nesses estados.
- Pausa: "Ctrl+F: informações de debug" no corpo do quadro.
- `GD.Print` de diagnóstico e `--demo-input`/`--debug-grid` sem mudança.

**Capturas:** `tasks/reports/interface/t085-hidden.png` (padrão, nada de debug) e `t085-shown.png` (depois de Ctrl+F: canto, ajuda e log).

**Fora das áreas da tarefa (mínimo):**
- `client/scripts/Match/MatchOverlay.cs`: uma linha, a dica na pausa.
- `client/scripts/Presentation/ArenaView.cs`: `DebugGrid` virou propriedade que redesenha a arena já mostrada (guarda a arena e os portões), para a grade alternar com Ctrl+F.

**Pendência:** a linha de ajuda (quando visível) passa por trás da barra de habilidades em 1280×720; era assim antes. A T-081 muda a barra e a ajuda (teclas 1–6).
