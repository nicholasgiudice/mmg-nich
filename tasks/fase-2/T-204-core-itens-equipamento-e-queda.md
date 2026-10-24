# T-204 — Core: itens, inventário, equipamento e queda

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-201
- **Libera:** T-205, T-212, T-222
- **Áreas que pode alterar:** `src/Game.Core/`, `src/Game.Data/`, `src/Game.Protocol/`, `src/Game.Server/Interest/` (inventário só para o dono), `data/schemas/`, testes correspondentes

## Contexto

`design/progression-v0.1.md` §6: 6 espaços (arma, cabeça, peito, mãos, pés, acessório), armas por classe, tipos de armadura, raridade com afixos, queda por categoria de monstro. Morte não perde equipamento.

## Requisitos

1. **Definições de item** nos dados (`data/items.json`): base (espaço, classes que usam, tipo de armadura, fórmula da estatística base por nível do item) e tabelas de afixos/raridade. Os itens concretos (instâncias) guardam base, nível do item, raridade e afixos sorteados.
2. **Inventário** (capacidade fixa, ex.: 30) e **equipamento** como componentes do Core. Inventário é visto **só pelo dono** (incluir a tag no `OwnerOnly` do `ClientView`, T-106); equipamento é visível para todos.
3. **Estatísticas do equipamento** somadas ao `CharacterStats` da T-201 (arma → ARMA, armaduras → armadura/resistência, afixos → atributos, vida, crítico, Mana/s). A arma inicial da T-201 vira um item de verdade.
4. **Queda**: ao morrer, monstro sorteia itens pela tabela da categoria (RNG da simulação), com nível do item = nível do monstro; o item fica no chão (entidade com dono e prazo) e quem tem direito pega com uma intenção `PickUp`. Na fase 2 o direito é de quem participou da morte (simples; registrar).
5. Intenções `Equip`, `Unequip`, `PickUp`, `Drop` validadas no Core (classe pode usar, espaço certo, inventário cheio).
6. Protocolo: componentes, entidades e intenções novos → `ProtocolVersion` + 1.

## Critérios de aceite

- testes do Core: equipar/desequipar muda as estatísticas, restrições por classe, sorteio de queda determinístico por seed, inventário cheio, só quem tem direito pega;
- personagem com o equipamento inicial produz os números de hoje;
- `./scripts/check.sh` completo passa.

## Resultado

**Feito** (VPS, 2026-10-24). `./scripts/check.sh` completo passa (inclui `Game.Design.Tests`: o equipamento inicial reproduz os números de hoje).

**Dados**
- `data/items.json` (nova seção `items` no carregador): 20 bases com as fórmulas da §6. Armas por classe: espada, machado, maça de guerra, arco, maça, cetro e cajado. Armaduras pesada, média e leve para cabeça, peito, mãos e pés, e um anel. Ali também fica `rules.items`: inventário de 30 espaços, alcance para pegar de 2 tiles, item no chão por 120 s, tipos de armadura (×1,5 / ×1,0 / ×0,6, e na leve +50% da armadura como resistência), afixos por raridade (0/1/2/2), os 10 afixos da §6 e as tabelas de queda (normal 8%, elite 40%, chefe 100% com 2 itens e o primeiro pelo menos incomum).
- `classes.json`: `startingWeapon` virou `startingEquipment` (itens de verdade, nível 2, comuns): espada 6, arco 6, cajado 3 com +2 INT, maça 5. Valores inteiros (dano de arma, armadura, resistência, atributos, vida) são arredondados por item (maça 4,8 → 5). Crítico e Mana/s ficam fracionários.
- Schema e validação: base (classes existem, `armorType` obrigatório só em armadura, estatística sem repetir e sem valor negativo), regras (tabelas apontam para itens existentes, pesos e chances válidos) e equipamento inicial (item existe, a classe usa, um por espaço).

**Core** (`src/Game.Core/Combat/Items/`)
- `ItemInstance` (base, nível, raridade, afixos), salvo e mandado como está. Os valores sempre vêm das definições.
- `Inventory` (só o dono recebe) e `Equipment` (todos recebem) usam cópia sob demanda: o snapshot de cada tick compartilha o vetor de espaços. Sem isso o teste de alocação do snapshot (T-101) estourava.
- `GroundItem`: entidade com `Transform`, donos e prazo. Não tem facção nem vida, então não vira alvo de habilidade nem de IA.
- `CharacterStats` ganhou `HealthBonus`, `CritBonus` e `ManaRegen`, que entram em vida máxima, chance de crítico (antes do teto) e Mana/s. `ItemMath.Recompute` refaz as estatísticas a partir do perfil, dos pontos distribuídos e do equipamento. Ao equipar, a vida acompanha o máximo, como no ganho de VIT.
- `Gear`: equipar (troca com o que estava, no mesmo espaço do inventário, então nunca dá "inventário cheio"), desequipar, pegar, largar e `DropLoot`/`Roll` (RNG da simulação, afixos distintos). `ItemSystem` (ordem 9600) trata as intenções e o prazo. Recusas viram `ItemRejected` com o motivo: classe não usa, inventário cheio, não é seu, longe demais, espaço vazio, não está mais lá.
- Eventos de item com formatador de log (nova categoria `Itens`). O `SourceLabel` do log agora reconhece nomes de itens.

**Decisões**
- **Direito ao saque:** quem participou da morte, com a mesma regra da XP da T-203 (dano nos últimos 30 s, ou cura/escudo em quem causou dano). O primeiro dono que pedir leva o item. Item largado pelo jogador só ele pega de volta (não há troca entre jogadores).
- **A queda só acontece se algum participante tem progressão** (`Leveling.SpawnCharacter`). Os encontros de teste, o simulador e o servidor atual não sorteiam nada nem consomem o RNG por isso. A chamada da queda está no `ProgressionSystem`, logo depois da XP, para usar os mesmos participantes.
- Maça com dois números na §6: virou duas bases, `weapon.war_mace` (Guerreiro, 4 + 1,0) e `weapon.mace` (Clérigo, 3 + 0,9).
- Ids de item por tipo (`weapon.*`, `armor.*`, `accessory.*`, `affix.*`), para não confundir com as habilidades `item.*` (poções).
- Todos os personagens com atributos ganham `Equipment` e `Inventory`, inclusive os dos encontros de teste. Equipar é permitido a qualquer momento, até em combate.
- Itens no chão de uma queda aparecem todos na posição do monstro. Espalhar na tela fica com o cliente (T-222).
- Protocolo **v5**: componentes 16–18, eventos 100–105, campo `Slot` no `NetIntent`. `Inventory` entrou no `ClientView.OwnerOnly`.

**Fora das áreas da tarefa (mínimo):** `data/classes.json` e `data/items.json` (exigidos pelos requisitos), `src/Game.Server/ServerOptions.cs`, `tests/Game.Sim/ContentLoading.cs`, `tests/Game.NetBots/Program.cs` e `client/scripts/Data/ContentLoader.cs`. Esses lugares só passaram a entregar os itens ao `ContentCatalog`; quem monta um catálogo sem eles recebe um erro claro ao criar um personagem. O cliente não foi compilado na VPS.

**Pendências**
- **T-205:** revisar nomes e listas das tabelas de queda (hoje toda base cai em toda categoria) e a lista própria de épicos. O "efeito próprio" do épico não existe: hoje ele é um raro com outra cor.
- **T-212:** carregar e salvar `Inventory` e `Equipment` (`ItemInstance` já é o formato de gravação) e passar a criar o personagem com progressão, que é o que liga a queda no servidor.
- **T-222:** mostrar o item no chão (`GroundItem`), as janelas e as recusas (`ItemRejected`), e o filtro de log `Itens`, que ainda não existe no painel (falta também o `Progressao`, da T-203).
