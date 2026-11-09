# Itens — referência dos dados

Status: **referência gerada a partir dos dados** (2026-11-02; consumíveis, ouro e lojas em 2026-11-09, T-320–T-322). Fonte: `data/items.json` (itens base e `rules.items`), `data/abilities_items.json` (efeito dos consumíveis), `data/classes.json` (equipamento e inventário iniciais), `data/shops.json` (lojas e `rules.shops`) e `data/rules.json` (ouro, em `rules.progression`). Os números saem desses arquivos; quando os dados mudarem, este documento deve ser atualizado.

Monstros e onde cada um aparece em `design/bestiario.md`; habilidades em `design/habilidades.md`.

## Como ler

- **Valor de um atributo** do item = `base + porNível × nível do item` (esquema de `items.json`). O Core arredonda o resultado para o inteiro mais próximo (0,5 para cima), exceto chance de crítico e regeneração de Mana, que ficam com fração (`src/Game.Core/Combat/Items/ItemMath.cs`).
- **Nível do item**: o item que cai de um monstro tem o **nível do monstro**; o equipamento inicial das classes tem nível 2.
- **Atributos** que aparecem nos itens: `weapon_damage` (dano da arma, o ARMA das habilidades), `armor` (defesa contra dano físico), `resistance` (defesa contra as outras escolas), `int` (Inteligência).
- **Raridade** decide quantos afixos o item recebe (veja "Raridade e afixos").

## Regras gerais (`rules.items`)

| Regra | Valor |
|---|---|
| Espaços no inventário | 30 |
| Distância para pegar item do chão | 2 tiles |
| Tempo do item no chão | 120 s |

## Armas

Cada arma só pode ser usada pelas classes indicadas (`classes`).

| Arma | Id | Classes | Atributos (base + por nível) |
|---|---|---|---|
| Espada | `weapon.sword` | Guerreiro | dano da arma 4 + 1,0 × nível |
| Machado | `weapon.axe` | Guerreiro | dano da arma 4 + 1,0 × nível |
| Maça de Guerra | `weapon.war_mace` | Guerreiro | dano da arma 4 + 1,0 × nível |
| Arco | `weapon.bow` | Arqueiro | dano da arma 4 + 1,0 × nível |
| Maça | `weapon.mace` | Clérigo | dano da arma 3 + 0,9 × nível |
| Cetro | `weapon.scepter` | Clérigo | dano da arma 2 + 0,6 × nível; INT 1 + 0,5 × nível |
| Cajado | `weapon.staff` | Mago | dano da arma 2 + 0,6 × nível; INT 1 + 0,5 × nível |

Espada, Machado e Maça de Guerra têm os mesmos números; nos dados não há diferença entre elas além do nome.

## Armaduras

Quatro espaços (cabeça, peito, mãos, pés) em três tipos. A armadura do item é `(base + porNível × nível) × multiplicador do tipo`, arredondada. A armadura leve também dá resistência igual a `resistanceFromArmor` × essa armadura (0,5 = metade), arredondada (`ItemMath.cs`).

| Tipo | Classes que podem usar | Multiplicador de armadura | `resistanceFromArmor` |
|---|---|---|---|
| Pesada (`heavy`) | Guerreiro | × 1,5 | 0 |
| Média (`medium`) | Guerreiro, Arqueiro, Clérigo | × 1,0 | 0 |
| Leve (`light`) | todas (lista vazia nos dados = todas as classes) | × 0,6 | 0,5 |

| Item | Id | Espaço | Tipo | Armadura (base + por nível) |
|---|---|---|---|---|
| Elmo de Placas | `armor.heavy_head` | cabeça | pesada | 3 + 1 × nível |
| Peitoral de Placas | `armor.heavy_chest` | peito | pesada | 6 + 2 × nível |
| Manoplas de Placas | `armor.heavy_hands` | mãos | pesada | 3 + 1 × nível |
| Botas de Placas | `armor.heavy_feet` | pés | pesada | 3 + 1 × nível |
| Elmo de Malha | `armor.medium_head` | cabeça | média | 3 + 1 × nível |
| Cota de Malha | `armor.medium_chest` | peito | média | 6 + 2 × nível |
| Luvas de Malha | `armor.medium_hands` | mãos | média | 3 + 1 × nível |
| Botas de Malha | `armor.medium_feet` | pés | média | 3 + 1 × nível |
| Capuz de Tecido | `armor.light_head` | cabeça | leve | 3 + 1 × nível |
| Túnica de Tecido | `armor.light_chest` | peito | leve | 6 + 2 × nível |
| Luvas de Tecido | `armor.light_hands` | mãos | leve | 3 + 1 × nível |
| Sapatos de Tecido | `armor.light_feet` | pés | leve | 3 + 1 × nível |

## Acessórios

Sem restrição de classe nos dados.

| Item | Id | Espaço | Atributos (base + por nível) |
|---|---|---|---|
| Anel | `accessory.ring` | acessório | resistência 4 + 1,5 × nível |
| Amuleto | `accessory.amulet` | acessório | resistência 4 + 1,5 × nível |

## Consumíveis

Itens de inventário (`type: consumable` em `items.json`): empilham, e usar gasta 1 unidade e executa a habilidade do item (`useAbility`, em `abilities_items.json`) em quem usa. A recarga é da habilidade: pilhas do mesmo item dividem a recarga, e itens diferentes têm recargas separadas.

| Item | Id | Habilidade | Efeito | Recarga | Pilha | Preço (loja) |
|---|---|---|---|---|---|---|
| Poção de Cura Menor | `consumable.minor_healing_potion` | `item.minor_healing_potion` | cura 60 | 20 s | 20 | 15 |
| Poção de Mana Menor | `consumable.minor_mana_potion` | `item.minor_mana_potion` | +40 de Mana | 20 s | 20 | 15 |

Todo personagem novo começa com **5 Poções de Cura Menor** (`startingInventory` em `classes.json`). Nenhum monstro deixa cair consumível: eles vêm do inventário inicial e do Templo.

## Raridade e afixos

| Raridade | Afixos |
|---|---|
| Comum | 0 |
| Incomum | 1 |
| Raro | 2 |
| Épico | 2 |

Os afixos são sorteados entre os dez abaixo, sem repetir no mesmo item. Valor = `base + porNível × nível do item`.

| Afixo | Id | Atributo | Base | Por nível |
|---|---|---|---|---|
| Força | `affix.str` | FOR | 1 | 0,4 |
| Destreza | `affix.dex` | DES | 1 | 0,4 |
| Agilidade | `affix.agi` | AGI | 1 | 0,4 |
| Inteligência | `affix.int` | INT | 1 | 0,4 |
| Vitalidade | `affix.vit` | VIT | 1 | 0,4 |
| Vida | `affix.health` | vida | 0 | 5 |
| Crítico | `affix.crit` | chance de crítico | 0,005 (0,5%) | 0,001 (0,1%) |
| Regeneração de Mana | `affix.mana_regen` | Mana por segundo | 0,2 | 0,03 |
| Armadura | `affix.armor` | armadura | 0 | 2 |
| Resistência | `affix.resistance` | resistência | 0 | 2 |

## Como se obtém

### Equipamento inicial (`classes.json`)

Cada classe começa com uma arma comum (sem afixos), nível 2:

| Classe | Arma inicial | Valor no nível 2 |
|---|---|---|
| Guerreiro | Espada | dano da arma 4 + 1,0 × 2 = 6 |
| Arqueiro | Arco | dano da arma 4 + 1,0 × 2 = 6 |
| Mago | Cajado | dano da arma 2 + 0,6 × 2 = 3,2 → **3**; INT 1 + 0,5 × 2 = 2 |
| Clérigo | Maça | dano da arma 3 + 0,9 × 2 = 4,8 → **5** |

### Quedas de monstros (`rules.items.drops`)

Todos os 21 itens base (as 7 armas, as 12 armaduras e os 2 acessórios) estão nas três tabelas, com o mesmo peso. A tabela é escolhida pela categoria do monstro:

| Categoria | Monstros | Chance | Itens por queda | Peso de raridade (comum / incomum / raro / épico) | Garantia |
|---|---|---|---|---|---|
| Normal | Rato de Campo, Lobo Cinzento, Salteador da Estrada, Aranha Menor, Guarda Esquelético | 4% | 1 | 70 / 25 / 5 / 0 | — |
| Elite | Aranha Grande | 25% | 1 | 40 / 45 / 15 / 0 | — |
| Chefe | Matriarca da Floresta | 100% | 2 | 0 / 60 / 35 / 5 | o primeiro sai pelo menos incomum |

Item épico só cai da Matriarca. Os itens caem para quem participou da morte e ficam 120 s no chão.

## Economia (T-320–T-322)

Economia mínima da fase 3: **só ouro de monstros e NPCs com preço fixo** (sem troca entre jogadores). Metas e medição em `tasks/reports/balance-v0.10.md`; testes em `tests/Game.Design.Tests/EconomyDesignTests.cs`.

### Ouro dos monstros (`rules.progression`)

Ouro de uma morte = `goldPerMonsterLevel` (**1**) × nível do monstro × categoria (`goldByRank`: normal **1**, elite **4**, chefe **20**), com variação de **±20%** (`goldVariance`) sorteada pela simulação. O ouro é dividido igualmente entre quem recebe a XP daquela morte (participantes e grupo por perto), arredondando para baixo. Vai direto para a carteira; não há ouro no chão.

| Monstro | Ouro (média) |
|---|---|
| normal nível L | L (ex.: Lobo nv2 = 2) |
| elite nível L | 4 × L |
| Matriarca (chefe nv8) | 160, dividido pelo grupo |

### Lojas (`data/shops.json`)

| NPC | Loja | Vende |
|---|---|---|
| Sacerdote (Templo) | `shop.temple` | Poção de Cura Menor e Poção de Mana Menor (15 cada) |
| Ferreiro | `shop.smith` | as 7 armas e as 12 armaduras, **comuns**, nos níveis 2 a 6 |

O Mercado, o Armazém, a Estalagem e o Salão de Treinamento ainda não têm loja. Para comprar ou vender é preciso estar a até **3 tiles** do NPC (`interactRange`). Estoque ilimitado; até 100 unidades por pedido.

**Preço de compra:** o `price` da oferta ou, sem ele (o caso de todas hoje), o **valor do item**:

- consumível: o `price` do item (15);
- equipamento: `(equipmentValueBase + equipmentValuePerLevel × nível) × raridade` = **45 × nível** × (comum **1**, incomum **2**, raro **4**, épico **8**).

| Nível do item | 2 | 3 | 4 | 5 | 6 |
|---|---|---|---|---|---|
| Comum no Ferreiro | 90 | 135 | 180 | 225 | 270 |

**Venda:** qualquer NPC com loja compra qualquer item do inventário por **10%** do valor (`sellRatio`), arredondado para baixo, por unidade. Comum nv L = 4,5 × L (nv4 = 18); poção = 1. Item equipado não se vende (desequipe antes). Não há recompra.

### Metas (caça solo nos Campos, modelo da T-215/T-302)

| Meta | Resultado |
|---|---|
| 30 min de caça pagam 8–12 poções de vida | **10,4** (~156 de ouro) |
| Equipamento comum do próprio nível custa 20–40 min de caça na área desse nível | nv2 **30**, nv3 **24**, nv4 **30** min |
| Vender tudo o que cai rende < 30% do ouro direto | **25%** |
| Poção de Mana não carrega o perfil fraco (vitória ≤ 50%) | Mago fraco: 0% (nv5) e 6% (nv9), igual sem poção |

Ouro por hora nos Campos: grupos nv1 ≈ 92, nv2 ≈ 178, nv3 ≈ 335, nv4 ≈ 362 (limitado pelo renascimento).

## Pontos em aberto nos dados

- **Arredondamento** e **`resistanceFromArmor`** não estão descritos nos JSON nem no esquema; as regras acima vêm do código (`ItemMath.cs`).
- **Armas iguais**: Espada, Machado e Maça de Guerra têm atributos idênticos; qualquer diferença entre elas não está nos dados.
- **Cargas da poção**: resolvido na T-320 (a poção virou item de inventário, sem cargas).
- **Fontes e destinos de ouro**: hoje o ouro só sai pelas lojas; redistribuir pontos (progression §10) ainda não cobra.
