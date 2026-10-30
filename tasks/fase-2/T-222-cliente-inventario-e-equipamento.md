# T-222 — Cliente: inventário, equipamento e saque

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** T-204, T-212
- **Libera:** T-230
- **Áreas que pode alterar:** `client/`

## Contexto

Itens, equipamento e queda vêm da T-204. O cliente mostra e manda intenções; não decide nada.

## Requisitos

1. Janela de inventário (grade) e de equipamento (6 espaços) com arrastar ou clique para equipar/desequipar; tooltip com estatísticas, raridade (cor) e comparação com o equipado.
2. Item no chão visível no mundo com a cor da raridade; pegar com clique ou tecla quando em alcance.
3. Mensagens de recusa claras (classe não usa, inventário cheio).

## Critérios de aceite

- pegar, equipar e ver a estatística mudar online;
- `dotnet build client/` e `./scripts/check.sh` passam.

## Resultado

**Feito** (Windows, 2026-10-29). `dotnet build client/` e `./scripts/check.sh` completo passam.

**Branch criada a partir da T-221, não da `main`.** A T-221 (`task/T-221-personagem-e-habilidades`) ainda não estava integrada e as duas mexem nos mesmos trechos (`Hud.cs`, `Main.cs`, `project.godot`, `InputActions`, filtros do log). Esta tarefa também reaproveita o painel-base da T-221 (`ProgressionPanel`) e mostra a mudança das estatísticas na tela de personagem dela. **Integre a T-221 antes desta**; depois disso, esta branch entra sem conflito.

**Inventário e equipamento** (`client/scripts/Hud/InventoryPanel.cs`, tecla I ou botão "Inventário (I) n/30")
- Coluna com os 6 espaços equipados e grade 6×5 do inventário, lidos de `Equipment` e `Inventory` no snapshot.
- Cada quadrado tem borda e fundo na cor da raridade (comum cinza, incomum verde, raro azul, épico roxo), sigla do item e nível do item. Item que a classe não usa aparece esmaecido e com um X.
- Clique no inventário equipa (`EquipItemIntent`); clique no equipado desequipa (`UnequipItemIntent`); botão direito larga no chão (`DropItemIntent`). Ficou só o clique: o requisito aceita "arrastar ou clique".
- O tooltip (`ItemText.Tooltip`, em BBCode) mostra nome na cor da raridade, raridade, espaço, tipo de armadura, nível do item, estatísticas da base e afixos (◆ nome e valor). Também traz "Sua classe não usa (só …)" e a comparação com o equipado no mesmo espaço (diferença por estatística, verde ou vermelho). Os valores vêm de `ItemMath.StatsOf`, a mesma conta do Core.
- O efeito nas estatísticas aparece na tela de personagem da T-221 (dano da arma, armadura, atributos, vida). O botão do inventário fica laranja quando ele enche.

**Item no chão** (`GroundItemView`, `WorldView.RenderGroundItems`)
- Losango na cor da raridade; raro e épico pulsam. Item de outro jogador fica esmaecido, com "(de outro)" no nome.
- A queda deixa todos os itens no ponto do monstro (T-204); o cliente espalha os do mesmo ponto em círculo, só no desenho.
- O nome aparece com o mouse em cima e no item que a tecla F pegaria (o mais perto, do jogador e ao alcance de `rules.items.pickupRange`), com "[F]" e borda branca. Com vários itens juntos, os nomes não se sobrepõem.
- Pegar: clique no item (esse clique não vira ataque) ou F (`PickUpItemIntent`). Ao pegar, sobe "+ Nome" na cor da raridade.

**Recusas:** `ItemRejected` aparece como texto flutuante sobre o jogador e no rodapé da janela: "Sua classe não usa esse item", "Inventário cheio", "Longe demais para pegar", "Esse item não é seu", "O item não está mais lá" e "Espaço vazio".

**Log:** filtro "Itens" com cor própria (pendência da T-204).

**Depuração:** `--demo-input` aceita `equip:n`, `unequip:weapon`, `drop:n`, `pickup` e `tooltip:n` (imprime o tooltip do espaço n), além de imprimir os eventos de item.

**Testado online** (VPS 31.97.162.124:7777, protocolo v7, modo mundo, na Cidade)
- Guerreiro, Clérigo e Mago: desequipar a arma, largar, ver o item no chão, pegar com F e equipar de novo.
- A tela de personagem acompanha: sem a espada o "Dano da arma" vai a 0 e o Golpe de Espada cai para 12; sem o cajado do Mago a INT volta de 13 para 11; reequipado, tudo volta.
- Equipar um espaço vazio foi recusado com "Espaço vazio".

**Testado só localmente** (`dotnet run --project src/Game.Server -- --world --dev-login --start-zone zone.vigil_fields`)
- Quedas de verdade são raras online (8% nos Ratos). Para testar, `data/items.json` (chance 100%, 4 itens, raridades iguais) e `data/zones.json` (um Rato perto do nascimento) foram alterados **temporariamente** e revertidos antes do commit.
- Quedas comum, incomum, raro e épico apareceram no chão com as cores; F pegou os itens ao alcance.
- Arqueiro tentando equipar cetro e maça: recusado com "Sua classe não usa esse item" (X nos quadrados).
- Clérigo equipou um Elmo de Malha incomum: armadura 0 → 6 (−11% de dano físico) na tela de personagem.
- Guerreiro trocou a espada por uma Maça de Guerra épica.
- Tooltip conferido pelo `tooltip:n` (texto) e nas capturas. Passar o mouse sobre os quadrados não foi testado de forma automática; vale olhar no playtest.

**Decisões**
- Uma janela só para inventário e equipamento, no canto direito acima do log. Ela cabe ao lado da tela de personagem em 1280 px, para equipar e ver os números ao mesmo tempo. O nome do item equipado fica no tooltip.
- Largar não pede confirmação: o item fica no chão e só o dono pega de volta (T-204).
- Na queda, o item vem para quem tem direito; o cliente não decide nada além de qual item o F tenta pegar.

**Pendências**
- Arrastar e soltar entre inventário e equipamento: dá para fazer depois com `_GetDragData`/`_DropData`, se o playtest pedir.
- Abrir o Godot no editor uma vez para gerar os `.cs.uid` das classes novas (como na T-221).

