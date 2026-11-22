# T-411 — Cliente: botão de teste no menu do Esc

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** T-404, T-405 (sai da branch da T-405)
- **Libera:** T-413
- **Áreas que pode alterar:** `client/scripts/Match/GameMenu.cs`, `client/scripts/Main.cs` (só para ligar o menu às mensagens novas), `client/scripts/Authority/` (enviar as mensagens novas), `tests/Game.Client.Tests/`, `tasks/reports/interface/`
- **Branch:** sai da `main` e é integrada na `main` (a fase 4 entrou na `main` em 2026-11-22; seção 6f de `tasks/README.md`).

## Contexto

Design em `design/segunda-classe-v0.1.md`, seção 4.2. Decisão do designer: a ferramenta para subir de nível é um **botão no menu do Esc** (o menu da T-396, `client/scripts/Match/GameMenu.cs`). O servidor (T-405) aceita os pedidos só com `--dev-tools` e avisa o cliente se a ferramenta está ligada.

## Requisitos

1. Com a ferramenta ligada no servidor, o menu do Esc ganha uma entrada **"Teste"** (ou "Desenvolvimento"), separada das outras, que abre as opções: **+1 nível**, **+5 níveis**, **ir ao nível 20**, **ir ao nível 40**, **desfazer a segunda classe** e **redistribuir pontos grátis**.
2. Com a ferramenta desligada, a entrada **não aparece**.
3. O cliente só envia o pedido; a resposta vem pelo estado replicado (nível, pontos, segunda classe). A recusa do servidor aparece como mensagem (em combate, por exemplo).
4. "Desfazer a segunda classe" pede confirmação.
5. Prévia no jogo: captura do menu com a entrada de teste em `tasks/reports/interface/menu-teste/`.

## Integração

Sai da `main` (a T-405 já está nela, com protocolo 19). Não há deploy na VPS: teste com o servidor local (`--world --dev-login --dev-tools`, seção 6f de `tasks/README.md`).

## Critérios de aceite

- no servidor local com a ferramenta ligada: "ir ao nível 20" leva o personagem ao 20 com os pontos certos; "+1 nível" sobe um; "desfazer a segunda classe" volta para antes da escolha;
- contra um servidor sem a ferramenta, a entrada não aparece (teste de cliente ou captura);
- captura aprovada pelo designer;
- `./scripts/check.sh --fast` passa no Windows.

## Resultado

_(preencher ao concluir)_
