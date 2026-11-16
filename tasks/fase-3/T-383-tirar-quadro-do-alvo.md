# T-383 — Tirar a vida do alvo do topo da tela

- **Ambiente:** Windows
- **Modelo:** Leve
- **Depende de:** —
- **Libera:** —
- **Áreas que pode alterar:** `client/scripts/Hud/` (`Hud`, `UnitFrame`), `client/scripts/Presentation/EntityView.cs` (só se for preciso mostrar a conjuração no monstro), `tasks/reports/interface/`

## Contexto

Pedido do designer durante o playtest da fase 3 (T-380), em 2026-11-16. Ao atacar um monstro, o quadro do alvo (`Hud._target`, um `UnitFrame` com vida, estados e conjuração) aparece no topo da tela. Num mapa com chefe, ficam no topo a vida do chefe (`BossPanel`) **e** a do monstro atacado, o que polui a tela. O designer pediu: tirar a vida do monstro que você está atacando.

## Requisitos

1. O quadro do alvo **sai do topo da tela**. A vida do monstro continua visível na barra sobre a cabeça dele (`EntityView`), e o alvo continua marcado no mapa e na lista de batalha.
2. O painel do chefe (`BossPanel`) fica como está.
3. O quadro do alvo também mostrava a **conjuração** do monstro (útil para interromper). Confira se ela já aparece sobre o monstro. Se não aparecer, mostre só uma barra fina de conjuração sobre o monstro selecionado, enquanto ele conjura.
4. Se, ao ver a tela, o designer preferir esconder o quadro só quando o painel do chefe estiver aberto, faça assim e registre.

## Critérios de aceite

- capturas no jogo: atacando um monstro nos Campos e atacando um monstro na Cripta com o Vigia ativo; nenhuma das duas mostra a vida do alvo no topo;
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
