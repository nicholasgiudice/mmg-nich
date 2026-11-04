# T-096 — Efeitos, ícones e limpeza do LPC (PixelLab)

- **Ambiente:** Windows
- **Modelo:** Humano + Leve
- **Depende de:** T-092, T-093, T-094, T-095
- **Libera:** —
- **Áreas que pode alterar:** `client/assets/art/`, `client/scripts/Presentation/`, `client/scripts/Hud/` (só ícones), `CREDITS.md`, `design/art-direction-v0.2.md` (seção de licença), `src/Game.Site/` (só se os ícones mudarem de lugar)

## Contexto

Depois das tarefas T-092 a T-095, sobram dois conjuntos de arte de outra origem:
- os efeitos (T-054, pacotes LPC em CC-BY-SA 3.0);
- os ícones de habilidades, itens e atributos (T-080, usados no HUD, no inventário, no chão e no site da T-071).

Esta tarefa decide, com o designer, o que trocar, e fecha a trilha removendo o que sobrou do LPC.

## Requisitos

1. **Decisão do designer** sobre trocar os efeitos e os ícones. Os ícones atuais podem ficar, se combinarem com a arte nova. Registre a escolha.
2. Se trocar os **efeitos**: impactos por escola de dano, projéteis (flecha, arcano), áreas (veneno, relâmpagos, flechas, fogo), escudo e a **teia** (que hoje é desenhada por código, porque nenhum pacote tinha teia). O `effects.json` usa o mesmo formato.
3. Se trocar os **ícones**: todas as habilidades, itens e atributos, em 32 px e com a mesma referência de estilo. O site gerado (T-071) continua mostrando os ícones.
4. **Limpeza:** apagar do repositório todo asset LPC que não é mais usado. Atualizar `CREDITS.md` e a seção de licença de `design/art-direction-v0.2.md`, e dizer o que ainda exige crédito, CC-BY-SA ou a tela de créditos. Se não sobrar nada do LPC, registre que a exigência da tela de créditos e a restrição de DRM deixam de valer.
5. A opção `--art` sai do cliente, se não houver mais asset LPC.

## Critérios de aceite

- busca no repositório sem referência a arquivos apagados (`sprites.json`, `effects.json`, `icons.json`, `.tsx`);
- capturas da Matriarca com os efeitos (se trocados), aprovadas pelo designer;
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
