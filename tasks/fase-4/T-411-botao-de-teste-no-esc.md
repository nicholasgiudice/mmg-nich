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

**Feito** (Windows, 2026-11-22), na branch `task/T-411-botao-de-teste-no-esc`, saída de `integra/fase-4-main` (a fase 4 inteira na `main`, com a T-405 e o protocolo 19). Não subiu `ProtocolVersion`.

- **Entrada no menu do Esc:** "Teste (desenvolvimento)", por último e separada das outras por uma linha. Só aparece quando o `Welcome` do servidor diz que a ferramenta está ligada (`Welcome.DevTools`, T-405); sem ela, a entrada nem existe. Abre a lista: +1 nível, +5 níveis, ir ao nível 20, ir ao nível 40, desfazer a segunda classe, redistribuir pontos (grátis) e Voltar. O título da janela muda para "Teste".
- **Desfazer a segunda classe** pede confirmação ("As habilidades dela saem e os pontos voltam. Depois dá para escolher outra.", Desfazer / Cancelar). Os outros pedidos saem direto.
- **Só envia o pedido:** `NetAuthority.RequestDevTool` manda `DevToolRequest`; o nível, os pontos e a segunda classe mudam pelo estado replicado de sempre. O `DevToolResult` aparece no aviso de cima da janela: verde quando feito ("Agora no nível 20.", "Segunda classe desfeita. Os pontos voltaram."), laranja quando recusado, com o motivo (em combate, morto, ferramenta desligada, sem segunda classe, já no nível, sem progressão). Com o menu fechado, a resposta aparece sobre o personagem.
- **Código:** `GameMenu` (entrada, lista e confirmação; evento `DevToolRequested`; `Open(withAccount, devTools)`; `ShowNote`), `NetAuthority` (`DevTools`, `RequestDevTool`, `TakeDevToolResults`), `Main` (liga o menu ao `NetAuthority` e mostra as respostas) e `client/scripts/Match/DevToolText.cs` (novo: rótulos, confirmação e textos das respostas, sem Godot).
- **Testes:** `tests/Game.Client.Tests/DevToolTextTests.cs` (a entrada só com `Welcome.DevTools`; um botão com rótulo para cada pedido do servidor; só o desfazer pede confirmação; cada resposta tem a sua frase). 111 testes de cliente passando.
- **Prévia no jogo** (servidor local `--world --dev-login --dev-tools`, cliente em `127.0.0.1:7777`), em `tasks/reports/interface/menu-teste/`: `1-menu-com-teste.png` (menu com a entrada), `2-pedidos.png` (lista), `3-nivel-20.png` ("Ir ao nível 20": nível 20, 95 pontos de atributo e 20 de habilidade, aviso "Agora no nível 20."), `4-confirmar-desfazer.png` (confirmação) e `5-recusa.png` (desfazer sem segunda classe: "Ainda não há segunda classe para desfazer.").

**Desvios**
- `client/scripts/Match/DevToolText.cs` é arquivo novo fora das áreas listadas (o mínimo, para os textos serem testáveis sem Godot); entra no projeto de testes do cliente por link, como os outros.

**Pendências**
- Captura aprovada pelo designer.
- Os critérios "+1 sobe um" e "desfazer volta para antes da escolha" precisam da T-410 para escolher a segunda classe pela tela; a ida ao 20 e a recusa foram conferidas no jogo, e o servidor tem os testes da T-405 para os outros pedidos.
