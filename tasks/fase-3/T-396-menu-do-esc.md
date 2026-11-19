# T-396 — Cliente: menu do Esc com trocar de personagem e sair do jogo

- **Ambiente:** Windows
- **Modelo:** Humano + Médio
- **Depende de:** T-395 (sai da branch dela)
- **Libera:** —
- **Áreas que pode alterar:** `client/scripts/Match/` (`MatchOverlay`, `ZoneScreen` e telas novas), `client/scripts/Main.cs`, `client/scripts/Authority/NetAuthority.cs`, `client/scripts/Account/AccountScreen.cs` (só para voltar à lista atualizada), `client/scripts/Hud/HudTheme.cs`, `client/assets/art/ui/`, `tasks/reports/interface/`

## Contexto

Pedido do designer em 2026-11-19. No mundo aberto, o Esc abre hoje um quadro "Pausado" do modo de encontro, com "Continuar" e "Menu", sem o kit pedra e ouro. O jogo online não pausa, então o título engana, e o "Menu" corta a conexão sem esperar a gravação.

## Requisitos

1. **No mundo aberto**, o Esc (quando não fecha uma janela nem limpa o alvo, como hoje) abre a janela **Menu** com o kit pedra e ouro (`HudTheme.WindowFrame`, `WindowTitlePlate`, `StoneButton`):
   - **Continuar** (ou Esc de novo) fecha;
   - **Trocar de personagem**, só para quem entrou com conta. No login de desenvolvimento, o botão vira "Voltar ao menu";
   - **Sair do jogo** fecha o cliente;
   - a opção de efeitos que já existe na pausa (`EffectSettings`).
2. Trocar de personagem e Sair mandam `LogoutRequest` (T-395):
   - com `Waiting`, a janela mostra "Saindo em N s… Mover-se cancela." e um botão **Cancelar**, que manda `LogoutCancel`;
   - com `Cancelled`, volta ao menu;
   - com `Done`, vai para a lista de personagens recarregada da API, ou fecha o jogo.
3. Se a conexão cair durante a espera, segue para a lista ou fecha, como no `Done`.
4. O jogo continua rodando por trás: com o menu aberto, o jogador não age, mas vê o mundo.
5. A sala de encontro (jogo local) continua com a pausa atual.
6. Prévia no jogo, numa captura em `tasks/reports/interface/`, aprovada pelo designer. Se faltar alguma peça de arte, gere no PixelLab com pelo menos 5 opções para o designer escolher.

## Critérios de aceite

- captura do menu e da contagem no jogo, aprovada pelo designer;
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Cuidados de integração

- Integre junto com a T-395 (protocolo 15) e publique o cliente novo na mesma rodada do `deploy-dev.sh`.

## Resultado

**Feito** (Windows, 2026-11-19) na branch `task/T-396-menu-do-esc`, que sai da `task/T-395-saida-do-mundo` (`b380430`). Falta a aprovação do designer.

**Menu do Esc** (`client/scripts/Match/GameMenu.cs`, novo):
- No mundo aberto e online, o Esc abre a janela **Menu**, quando não fecha uma janela nem limpa o alvo, como antes. A janela usa o kit pedra e ouro: `WindowFrame`, `WindowTitlePlate` e `StoneButton` em todos os botões.
- Os botões são **Continuar**, **Trocar de personagem** (com conta) ou **Voltar ao menu** (login de desenvolvimento), **Sair do jogo** e a opção de efeitos, que veio da pausa.
- Esc de novo ou Continuar fecha. O mundo continua por trás e só a entrada do jogador para (`_paused`, como a pausa online já fazia).
- A sala de encontro e o jogo local continuam com a pausa antiga (`MatchOverlay.ShowPause`).

**Saída** (`Main.cs`, `NetAuthority.cs`):
- `NetAuthority.RequestLogout()` e `CancelLogout()` mandam as mensagens da T-395. O `LogoutStatus` recebido fica em `NetAuthority.Logout`.
- Trocar e Sair mostram "Saindo...". Com `Waiting`, aparece "Em combate: saindo em N s. Mover-se ou atacar cancela." e o botão **Cancelar**. A conta usa o tick do `Waiting` menos o tick do último snapshot.
- Esc durante a espera também cancela. Com `Cancelled`, a janela volta às escolhas com "Saída cancelada.".
- Com `Done`, ou se a conexão cair durante a espera, Trocar vai para a lista de personagens (`BackToMenu`: `AccountScreen`, que recarrega a lista da API) e Sair fecha o jogo.
- Um `Welcome` novo durante a espera (troca de zona) mantém o menu aberto.

**Conferido no jogo**, com servidor local da branch da T-395 (`--world --dev-login`):
- `t396-menu-1.png`: o menu na Cidade.
- Fora de combate, "Voltar ao menu" sai na hora. O servidor registra "saiu do mundo (troca de personagem)" e o cliente volta ao menu.
- `t396-espera-combate.png`: arqueira atirando nos ratos dos Campos, depois Esc → Voltar ao menu → "saindo em 9 s". O servidor registra "saída em combate, espera de 10 s".

**Fora das áreas listadas:** o roteiro de depuração (`--demo-input`) continua rodando com o menu aberto, para dar para testar os botões por clique. Fora disso, nada muda.

**Pendências:**
- Aprovação do designer pelas capturas.
- "Trocar de personagem" com conta real não foi testado: precisa da API e do servidor da VPS com o protocolo 15, depois da integração. O caminho é o mesmo do "Voltar ao menu" e só muda a cena de destino.
- "Sair do jogo" não foi fotografado: ele faz o mesmo pedido e depois fecha o jogo.
- Integração: junto com a T-395 (protocolo 15), com o `deploy-dev.sh` e o cliente novo na mesma rodada. A T-397 também mexe em `Main.cs` e em `client/scripts/Match/`: integre uma de cada vez.
