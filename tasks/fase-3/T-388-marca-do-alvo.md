# T-388 — Marca melhor no monstro que estou atacando

- **Ambiente:** Windows
- **Modelo:** Humano + Médio
- **Depende de:** —
- **Libera:** —
- **Áreas que pode alterar:** `client/scripts/Presentation/` (`EntityView`, `WorldView`), `client/assets/art/ui/` ou `client/assets/art/fx/`, `CREDITS.md`, `tasks/reports/interface/`

## Contexto

Pedido do designer durante o playtest da fase 3 (T-380), em 2026-11-16: melhorar a marca do monstro selecionado. Hoje é um arco desenhado em código (`EntityView._Draw`, `DrawArc` de 2,5 px em volta do monstro), que some no meio do terreno e dos efeitos.

## Requisitos

1. Uma marca de alvo em pixel art, no chão sob o monstro (aos pés, atrás do sprite), que acompanha o tamanho do monstro (raio) e se lê sobre grama, terra, pedra e no escuro da Cripta. Uma animação leve (pulso ou giro lento) é bem-vinda, sem distrair.
2. **Pelo menos 5 opções**, feitas no PixelLab (modo de maior qualidade) ou desenhadas à mão com o `pixelart_workbench`, no estilo do kit pedra e ouro. Ideias: anel dourado com quatro pontas, colchetes nos cantos, seta sobre a cabeça, círculo rúnico.
3. As opções mostradas **dentro do jogo**, com o monstro selecionado em pelo menos dois terrenos (Campos e Cripta). O designer escolhe.
4. A marca de "mouse sobre a linha da lista de batalha" (T-312, anel branco) continua diferente da marca do alvo.
5. Arte em escala inteira (sem borrar). Sem o arquivo, volta o arco de hoje.
6. Créditos registrados.

## Critérios de aceite

- prancha com as opções em jogo e a escolha do designer registrada;
- capturas com a marca escolhida nos Campos e na Cripta;
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
