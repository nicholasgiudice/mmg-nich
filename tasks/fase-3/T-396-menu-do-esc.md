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

_(preencher ao concluir)_
