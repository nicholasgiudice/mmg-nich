# T-083 — Inventário com o boneco do personagem

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** T-080
- **Libera:** —
- **Áreas que pode alterar:** `client/scripts/Hud/` (`InventoryPanel`, `ItemText`, `HudTheme` e arquivos novos), `client/scenes/`

## Contexto

O inventário (tecla I, `InventoryPanel`, T-222) mostra os 6 espaços equipados numa lista e a grade de 30 espaços. O designer quer ver **o corpo do personagem** com cada peça no lugar, como nos RPGs clássicos.

O Core tem 6 espaços (`EquipSlot`): arma, cabeça, peito, mãos, pés e acessório. **Não há espaço separado para anel e amuleto**: os dois usam o único espaço de acessório.

## Requisitos

1. **Boneco:** a figura do personagem no centro (o sprite da classe, `client/assets/art/characters/`, quadro parado de frente e ampliado sem borrão) com os espaços em volta:
   - cabeça em cima;
   - peito no meio;
   - mãos e arma de um lado;
   - acessório do outro;
   - pés embaixo.

   Espaço vazio mostra uma silhueta clara do tipo de peça.
2. **Ícones:** cada item equipado e cada item da grade aparece pelo ícone da T-080, com a moldura na cor da raridade (as cores de hoje).
3. **Equipar e desequipar:**
   - arrastar do inventário para o espaço certo equipa; o duplo clique também equipa;
   - arrastar do boneco para a grade desequipa;
   - arrastar para fora da janela, ou usar o botão direito como hoje, larga o item no chão.

   Item que a classe não pode usar fica esmaecido e o espaço não aceita o arraste. Quem decide é o servidor; a cor é só indicação.
4. **Dica:** com o mouse em cima, a dica de hoje (`ItemText.Tooltip`) e, para item da grade, a comparação com o equipado no mesmo espaço (verde melhor, vermelho pior).
5. **Resumo** ao lado do boneco: dano da arma, armadura e resistência totais, com os valores do snapshot.
6. Só intenções (`EquipItemIntent`, `UnequipItemIntent`, `DropItemIntent`); as recusas aparecem no rodapé como hoje.
7. **Anel e amuleto:** fica como está, com um espaço de acessório. Separar em dois mexe no Core, nos dados, no protocolo e no banco; se o designer quiser, abra uma tarefa própria e registre a sugestão no Resultado.

## Critérios de aceite

- equipar e desequipar arrastando, nas quatro classes;
- item de outra classe não entra no espaço e aparece esmaecido;
- capturas de tela antes e depois no Resultado;
- `./scripts/check.sh` passa.

## Resultado

_(preencher ao concluir)_
