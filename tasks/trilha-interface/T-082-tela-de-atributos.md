# T-082 — Tela de atributos (distribuir pontos)

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** — (usa os ícones de atributo da T-080, se existirem)
- **Libera:** —
- **Áreas que pode alterar:** `client/scripts/Hud/` (`CharacterPanel`, `ProgressionPanel`, `ProgressionText`, `HudTheme`), `client/scenes/`

## Contexto

A tela de personagem (tecla C, `CharacterPanel`, T-221) lista os 5 atributos com o botão +, a prévia do próximo ponto ("Golpe Pesado 51 → 53"), o atributo sugerido em destaque e as estatísticas derivadas. Ela funciona, mas o designer quer uma tela melhor de usar. Regras dos atributos em `design/progression-v0.1.md` §3–§4. Cada ponto é uma `SpendAttributePointIntent` (um ponto por intenção), e a redistribuição usa o botão com confirmação no segundo clique (`ProgressionPanel`).

## Requisitos

1. **Pontos livres em destaque** no topo ("5 pontos para distribuir"), e um aviso discreto no HUD (perto do quadro do jogador) enquanto houver ponto sem gastar.
2. **Atributos em linhas largas:** ícone (T-080, se houver), nome, valor atual (com a divisão classe / pontos / equipamento, se o snapshot tiver; senão só o total, sem pedir campo novo ao Core), e os botões **+1** e **+5**. O +5 manda 5 intenções, ou menos se faltarem pontos. A prévia do efeito do próximo ponto fica na própria linha, sem precisar passar o mouse.
3. **Atributo sugerido da classe** marcado de forma que se entenda sem ler a dica.
4. **Estatísticas derivadas** agrupadas (Vida e Mana; ataque: crítico e velocidade de ataque; defesa: armadura e resistência), com o valor e, quando um + estiver em foco, a mudança que ele causa (verde).
5. **Redistribuir** continua com a confirmação no segundo clique, num lugar que não seja clicado sem querer.
6. Mantém as regras de hoje: as contas da prévia são as do Core (`CharacterStats`), nada é calculado com número fixo no cliente, e a tela só manda intenções (a recusa volta como `ProgressionRejected` e aparece no rodapé).

## Critérios de aceite

- distribuir 5 pontos com um clique em +5 e ver vida, Mana e prévia mudarem;
- personagem sem pontos não mostra + e não mostra o aviso no HUD;
- capturas de tela antes e depois no Resultado;
- `./scripts/check.sh` passa.

## Resultado

**Feito** (Windows, 2026-11-03). `dotnet build client/` sem avisos e `./scripts/check.sh` passa. Branch feita sobre a `task/T-085-ocultar-debug` (integre a T-085 antes).

**Tela de personagem** (`client/scripts/Hud/CharacterPanel.cs`, refeita)
- **Pontos livres** no topo, grande e em verde ("10 pontos para distribuir"); sem pontos, "Nenhum ponto para distribuir (5 a cada nível)" apagado.
- **Uma linha larga por atributo:** ícone da T-080 (`attribute.str` …), nome, valor grande, a divisão "classe 10 · pontos +5 · equip. +1" (só aparece quando há pontos ou equipamento; classe = `ClassDefinition.Attributes`, pontos = `CharacterProgress.Allocated`, equipamento = o resto do total do snapshot, sem campo novo no Core), os botões **+1** e **+5** e a prévia do próximo ponto na própria linha.
- **+5** manda 5 `SpendAttributePointIntent` (ou quantos pontos houver; o botão vira "+3" com 3 pontos). Sem pontos, a linha não tem botões.
- **Sugerido:** a linha inteira tem fundo e borda dourados, ★ no nome e o texto "sugerido para a classe" embaixo, sem depender da dica.
- **Derivadas em três grupos:** Vida e Mana (vida, Mana, recuperação/s), Ataque (dano da arma, crítico, velocidade de ataque, intervalo do ataque básico) e Defesa (armadura e resistência, com a redução). Com o mouse num +1/+5, cada valor que muda ganha "→ novo" em verde. As contas são as do Core (`StatMath`), aplicadas à cópia das estatísticas com os pontos a mais; vida e Mana partem do máximo do snapshot mais a diferença.
- **Redistribuir** foi para o rodapé, à direita, longe dos +; continua com a confirmação no segundo clique (`ProgressionPanel`).
- As recusas continuam no rodapé (`ProgressionRejected`).
- `ProgressionText`: `Get(AttributeSet, …)` e `Breakdown(...)`.

**Aviso no HUD:** já existia (T-221): o botão "Personagem (C) ● 10" fica verde enquanto houver ponto livre, logo abaixo do quadro do jogador, e volta ao normal sem pontos. Mantido; o `Hud.cs` não mudou nesta tarefa.

**Verificação** (servidor local `--world --dev-login --start-zone zone.vigil_fields --data <cópia>`, com `xpCurveBase` 1, um Rato com 12 de vida perto do nascimento; a cópia ficou fora do repositório e o `client/data` volta ao original no próximo build):
- Guerreiro mata o Rato, sobe para o nível 3 (10 pontos). Clique em +5 de VIT: cinco `attribute Vit = 11 … 15` no mesmo tick, vida 268 → 318, divisão "classe 10 · pontos +5", 5 pontos restantes.
- Mouse no +5 de AGI: crítico "6,5% → 7,75%" e velocidade "+3% → +5,5%" em verde. No +5 de INT: Mana "88 → 98" e recuperação "5,2/s → 5,45/s".
- Dois cliques em +5: 0 pontos, linhas sem botões e o botão do HUD sem o "●".

**Capturas** em `tasks/reports/interface/`: `t082-before.png` (tela antiga, 10 pontos), `t082-after.png` (depois do +5 em VIT, mouse no +5 de AGI), `t082-no-points.png` (sem pontos).

**Fora das áreas da tarefa (mínimo):** `client/scripts/Main.cs` ganhou no roteiro de demonstração os comandos de mouse `click:x;y`, `rclick:x;y`, `dclick:x;y`, `hover:x;y` e `drag:x1;y1;x2;y2` (com `;` porque a vírgula separa os itens). Eles entram como eventos de mouse da janela (`Input.ParseInputEvent`), sem mexer no cursor do sistema, para testar botões e arrastes sem uma pessoa. Também servem às T-081 e T-083.
