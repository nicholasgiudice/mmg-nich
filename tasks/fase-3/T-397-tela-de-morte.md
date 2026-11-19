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

**Feito** (Windows, 2026-11-19) na branch `task/T-397-tela-de-morte`. O designer escolheu o emblema **B1**, a lápide com a espada.

**Tela de morte** (`client/scripts/Match/DeathScreen.cs`, nova; `ZoneScreen` hospeda a tela no lugar da faixa vermelha antiga):
- Em cerca de 1 s, a tela perde a cor (shader com a textura da tela, 85%) e escurece (vermelho bem escuro, 45%). A janela aparece em seguida.
- A janela usa o kit pedra e ouro (`WindowFrame`, `WindowTitlePlate`) e mostra:
  - "Você morreu" e o emblema em 2×;
  - a zona e "Golpe final: <monstro> — <habilidade>", tirado do `DamageDealt` com `Killed` do resumo;
  - o dano por fonte, até 4 fontes, depois "e mais N";
  - a frase de que nada é perdido e o botão **Retornar à Cidade** (`StoneButton`), que manda o `RespawnRequest` de sempre.
- O resumo vem do log de combate pelo `Hud.DeathSummaryHandler`. No mundo aberto, ele vai para a tela de morte. Na sala de encontro, continua no `DeathSummaryPanel`, como antes.
- Esc não fecha a tela. O `--auto-respawn` continua funcionando, agora com o texto "Retornar à Cidade".
- Prévia: `--death-sample` mostra a tela ao entrar na zona, com um resumo de exemplo de três lobos (`DeathSample`). O botão só fecha a tela.

**Arte:**
- Duas chamadas `create_image_pro` em 64×64, com 16 candidatas cada e o ícone do jogo (escudo B3 da T-074) como referência de estilo. A: caveira num medalhão. B: lápide com espada quebrada ou escudo rachado.
- As pranchas `tasks/reports/interface/emblema-morte/prancha-a.png` e `prancha-b.png` mostram cada candidata em 3×, 64 px e 32 px. As candidatas brutas estão em `a/` e `b/`, numeradas a partir de 0, então a B1 é o arquivo `b/0.png`.
- Arquivo: `client/assets/art/ui/death_emblem.png`, sem retoque. Crédito no `CREDITS.md`.
- Custo: 20 gerações da assinatura (2 chamadas Pro de 10).

**Conferido no jogo:** `tasks/reports/interface/t397-morte-1.png`, com um servidor local `--world --dev-login` e `--death-sample`. Na captura, a zona aparece como "Cidade-Fortaleza" porque a prévia nasce na Cidade.

**Correção no caminho:** a `ZoneScreen` fica numa `CanvasLayer` e não tem tamanho. A tela de morte cobre a janela por conta própria, com `Size` igual ao do viewport a cada quadro.

**Fora das áreas listadas:** `client/scripts/Match/DeathSample.cs` (a prévia).

**Pendências:**
- Ainda não foi vista numa morte de verdade, só com a prévia. O caminho do resumo é o mesmo do painel antigo.
- Integração: a T-396 também mexe em `Main.cs` e em `client/scripts/Match/`, então integre uma de cada vez. Não mexe em servidor nem protocolo.
