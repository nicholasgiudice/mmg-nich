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

**Feito** (Windows, 2026-11-03). `dotnet build client/` sem avisos e `./scripts/check.sh` passa. Branch feita sobre a `task/T-083-inventario-com-boneco` (integre T-085, T-082, T-081 e T-083 antes).

**Item no chão** (`client/scripts/Presentation/GroundItemView.cs`)
- **Ícone do próprio item** (T-080) no tamanho da folha (34×34; a folha não tem 32), sem escala e em pixel inteiro, logo sem borrão. A prévia da T-080 escalava para 26/30 px.
- **Contorno na cor da raridade:** uma silhueta branca do ícone (feita uma vez por item e guardada) tingida e desenhada 1 px para cada lado por baixo do ícone. Pintar o próprio ícone não serve, porque a borda preta do desenho continuaria preta. Por baixo vão um brilho redondo na cor da raridade e uma sombra no chão.
- **O que já existia continua:** esmaecido quando é de outro jogador; nome com o mouse em cima; "[F]" no próximo a pegar, agora com o contorno e um anel brancos; pulso de raro e épico. Sem ícone, o losango de antes.
- **Animação ao cair:** o item salta (14 px) e dá um pulinho antes de assentar, em 0,5 s. A sombra fica no chão e encolhe enquanto ele sobe. Só visual: a posição continua a do servidor.
- O ícone de saco ("item no chão") não foi preciso: o ícone do item ficou legível.

**Verificação** (servidor local de teste; os ratos derrubam 8 itens de raridades variadas e o personagem largou os itens um a um enquanto andava):
- **Campos da Vigília, grama:** botas, cajado e elmos legíveis, com o contorno da raridade (`t084-grass-zoom.png`). No alto, o anel que acabou de cair está no ar, com a sombra no chão e o contorno branco do "[F]".
- **Cidade-Fortaleza, terra e areia da praça:** amuleto e cajado legíveis na terra (`t084-dirt-zoom.png`).
- **F continua pegando:** 12 itens pegos com F na mesma rodada.
- Para ter monstros na Cidade, a cópia dos dados de teste tratou a Cidade como campo. Nada disso foi para o repositório.

**Capturas** em `tasks/reports/interface/`: `t084-fields.png` e `t084-city.png` (telas inteiras), `t084-grass-zoom.png`, `t084-dirt-zoom.png` e `t084-pile-zoom.png` (ampliadas 4×).

**Observação para o designer:** todos os itens de um monstro caem no mesmo ponto, que é a posição do servidor, e ficam empilhados (`t084-pile-zoom.png`). Com o contorno ainda dá para distinguir, mas espalhar os itens é coisa do Core/servidor (posição de cada item na queda), fora desta tarefa.

**Fora das áreas da tarefa (mínimo):** `client/scripts/Input/DebugInputScript.cs` ganhou `press:ação` e `release:ação` no roteiro de demonstração (andar enquanto larga itens, para o teste acima).
