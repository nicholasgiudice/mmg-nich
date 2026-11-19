# T-397 — Cliente: tela de morte com "Retornar à Cidade"

- **Ambiente:** Windows
- **Modelo:** Humano + Médio
- **Depende de:** —
- **Libera:** —
- **Áreas que pode alterar:** `client/scripts/Match/ZoneScreen.cs` (e telas novas em `client/scripts/Match/`), `client/scripts/Hud/DeathSummaryPanel.cs`, `client/scripts/Hud/Hud.cs` (só a ligação do resumo), `client/scripts/Hud/HudTheme.cs`, `client/scripts/Main.cs` (só a parte da morte), `client/assets/art/ui/`, `tasks/reports/interface/`, `CREDITS.md`

## Contexto

Pedido do designer em 2026-11-19. No mundo aberto, a morte mostra hoje uma faixa vermelha no alto ("Você morreu em …" e o botão "Renascer na Cidade", T-223) e, no centro, o resumo de dano num painel sem o kit (`DeathSummaryPanel`). O servidor já faz o renascimento (`RespawnRequest`): volta à zona inicial, cheio, sem perder nada.

## Requisitos

1. **Uma tela de morte só**, no lugar da faixa e do resumo soltos:
   - a tela escurece e perde cor aos poucos (cerca de 1 s);
   - no centro, uma janela do kit pedra e ouro com um emblema de morte (arte nova), o título **"Você morreu"**, a zona e quem deu o golpe final;
   - o resumo de dano por fonte, que já existe, mais compacto;
   - a frase "Você volta à Cidade com vida e Mana cheias. Nada é perdido.";
   - o botão **Retornar à Cidade** (`StoneButton`), que manda o `RespawnRequest` que já existe.
2. Esc com a tela de morte aberta não a fecha. O menu do Esc (T-396) pode abrir por cima.
3. O `--auto-respawn` continua funcionando.
4. **Arte (PixelLab):** emblema de morte no estilo do kit pedra e ouro. Gere pelo menos 5 opções (ex.: caveira, espada quebrada, lápide, escudo rachado) numa prancha em `tasks/reports/interface/` com cada opção no tamanho real. O designer escolhe, e a escolhida é mostrada no jogo antes da aprovação. Arte simples, com pouco detalhe.
5. A sala de encontro (jogo local) continua com o resultado atual. Só muda o mundo aberto.

## Critérios de aceite

- prancha das opções do emblema e captura da tela de morte no jogo, aprovadas pelo designer;
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Cuidados de integração

- Não mexe em servidor nem protocolo: sai com um cliente novo, a qualquer momento.
- A T-396 também mexe em `Main.cs` e nas telas de `client/scripts/Match/`: integre uma de cada vez.

## Resultado

_(preencher ao concluir)_
