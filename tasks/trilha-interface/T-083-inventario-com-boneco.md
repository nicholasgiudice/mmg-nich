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

**Feito** (Windows, 2026-11-03). `dotnet build client/` sem avisos e `./scripts/check.sh` passa. Branch feita sobre a `task/T-081-habilidades-e-barra` (integre T-085, T-082 e T-081 antes).

**Inventário** (`client/scripts/Hud/InventoryPanel.cs`, refeito; `ItemText.cs` só no texto das dicas)
- **Boneco:** figura da classe (`sprites.json`, animação `idle`, de frente, zoom 3 inteiro e filtro `Nearest`). Em volta: cabeça em cima, peito no tronco, mãos e arma à esquerda, acessório à direita, pés embaixo.
- **Espaço vazio:** silhueta, que é o ícone de uma peça do tipo, quase transparente e sem cor.
- **Ícones** da T-080 no boneco e na mochila, com a moldura na cor da raridade (as de hoje), o nível do item no canto, e esmaecido com X quando a classe não usa.
- **Equipar:** arrastar da mochila para o espaço do boneco, ou duplo clique. O espaço só aceita (borda dourada) o item do tipo certo que a classe usa; o resto não entra. Quem decide continua sendo o servidor: no duplo clique de um item de outra classe, ele recusa (`ClassCannotUse`) e a recusa aparece no rodapé, como antes.
- **Desequipar:** arrastar do boneco para a mochila, ou duplo clique no boneco.
- **Largar no chão:** botão direito (como antes) ou arrastar da mochila para fora da janela. Arrastar e soltar dentro da janela, num lugar que não aceita, não faz nada.
- **Dica:** a de hoje (`ItemText.Tooltip`), que já comparava o item da mochila com o equipado no mesmo espaço (verde melhor, vermelho pior). Só o texto dos gestos mudou.
- **Resumo** embaixo do boneco: dano da arma, armadura e resistência totais, do `CharacterStats` do snapshot.
- Só intenções: `EquipItemIntent`, `UnequipItemIntent` e `DropItemIntent`.

**Verificação** (servidor local de teste, com os ratos derrubando 6 itens entre Anel, Elmo de Malha e Cajado): um roteiro tenta arrastar cada item da mochila para cabeça, acessório e arma, desequipa a cabeça arrastando para a mochila e larga um item arrastando para fora da janela.
- **Guerreiro, Arqueiro e Clérigo:** elmo e anel equipados arrastando, o elmo desequipado arrastando (`unequipped armor.medium_head -> slot 0`). O Cajado não entra na arma e fica esmaecido.
- **Mago:** Cajado e anel equipados arrastando. O Elmo de Malha (média) não entra.
- **Duplo clique (Mago):** anel e Cajado equipados; no elmo, o servidor recusou com `ClassCannotUse`.
- **Fora da janela:** o item largado aparece no chão (o Elmo de Malha com "[F]" em `t083-archer.png`; Cajado `dropped weapon.staff (Epic)` no log).

**Capturas** em `tasks/reports/interface/`: `t083-before.png` (lista antiga), `t083-after.png` (boneco com a dica do Elmo comparando com a cabeça vazia), `t083-warrior.png` e `t083-archer.png` (fim do roteiro de arrastar).

**Decisões:**
- Arrastar do boneco para fora da janela não larga: o Core só larga a partir do inventário (`DropItemIntent` recebe o índice da mochila). Desequipe primeiro.
- Para saber se o item foi solto fora da janela, a janela guarda a última posição dos eventos de mouse (`_Input`), em vez de perguntar ao sistema a posição do cursor.

**Anel e amuleto:** continuam no único espaço de acessório. Se o designer quiser dois espaços, a sugestão é uma tarefa própria, que mexe no `EquipSlot` do Core, em `items.json`, no protocolo e no banco.
