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

_(preencher ao concluir)_
