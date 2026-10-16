# T-021 — Cliente: input e câmera

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** T-008, T-020
- **Libera:** T-022, T-023, T-024
- **Áreas que pode alterar:** `client/`

## Requisitos

1. **Input Map** do Godot com ações nomeadas (remapeáveis depois):
   - WASD: movimento em 8 direções → `MoveIntent`/`StopIntent` só quando a direção muda;
   - mouse: mira (ponto no mundo);
   - botão esquerdo: ataque básico no alvo sob o cursor ou no alvo selecionado;
   - 1, 2, 3: habilidades da classe, usando alvo selecionado ou ponto de mira conforme o tipo de alvo;
   - Tab: alterna alvo entre inimigos próximos; clique seleciona; Esc limpa seleção.
2. Conversão tela ↔ mundo usando a constante de 32 px/unidade.
3. Feedback de rejeição: texto curto acima do personagem para `AbilityRejected` ("Em recarga", "Sem Vigor", "Fora de alcance", "Sem linha de visão").
4. Câmera seguindo o jogador com suavização leve e pequeno deslocamento na direção da mira; limitada às bordas da arena.
5. Indicador do alvo selecionado.

## Critérios de aceite

- Todas as habilidades das quatro classes podem ser usadas com teclado e mouse;
- mensagens de rejeição aparecem corretamente;
- sem envio de intenção repetida a cada frame quando nada mudou.

## Resultado

_(preencher ao concluir)_
