# T-084 — Ícone do item no chão

- **Ambiente:** Windows
- **Modelo:** Leve
- **Depende de:** T-080
- **Libera:** —
- **Áreas que pode alterar:** `client/scripts/Presentation/GroundItemView.cs`, `client/scripts/Presentation/` (catálogo de ícones da T-080, se precisar de ajuste)

## Contexto

O item que cai de um monstro aparece hoje como um **losango** na cor da raridade (`GroundItemView`, T-222): esmaecido quando é de outro jogador, com o nome ao passar o mouse e "[F]" no que a tecla F pegaria. Raro e épico pulsam. O designer quer um ícone melhor.

## Requisitos

1. Trocar o losango pelo **ícone do próprio item** (T-080, 32×32, sem borrão), com um brilho ou contorno na cor da raridade por baixo. Se o ícone do item ficar ilegível no chão, use o ícone de "item no chão" (saco) tingido pela raridade e registre a escolha no Resultado.
2. Manter o que já existe: esmaecido quando é de outro; o nome com o mouse em cima; "[F]" e a borda branca no próximo a pegar; o pulso de raro e épico.
3. Uma pequena animação ao cair (o item salta e assenta), só visual, sem mexer na posição que vem do servidor.
4. Legível sobre os tiles de grama e de terra da Cidade e dos Campos (conferir nos dois mapas).

## Critérios de aceite

- matar monstros até cair um item de cada raridade e conferir a leitura no chão (captura de tela no Resultado);
- pegar com F continua funcionando;
- `./scripts/check.sh` passa.

## Resultado

_(preencher ao concluir)_
