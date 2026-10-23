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

_(preencher ao concluir)_
