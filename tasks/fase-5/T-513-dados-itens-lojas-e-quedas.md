# T-513 — Dados: itens, loja do Forte, poções e quedas do 16 ao 27

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** 510, 511, 512
- **Libera:** T-531, T-550
- **Áreas que pode alterar:** `data/items.json`, `data/shops.json`, `data/monsters.json` (só tabelas de queda e ouro), `data/rules.json` (só itens e economia), `design/itens.md`, `tests/Game.Design.Tests/EconomyDesignTests.cs` e testes de design de itens, `tasks/reports/`

## Contexto

Os itens escalam pelo nível (a loja oferece o item com um `level`, e a queda sai no nível do monstro). Hoje a loja do Ferreiro e o Templo, na Cidade, vendem para o primeiro mapa. O Forte do Passo (`design/mapas-16-40-v0.1.md` §3.2) precisa de uma loja para o 16–30, e as poções de hoje podem curar pouco para a vida de um personagem do 25.

## Requisitos

1. **Loja do Forte** `shop.pass_fort` (o Intendente): armas e armaduras das 4 classes no nível 16 e no 21; poções.
2. **Poções:** conferir se as de hoje (`consumable.greater_healing_potion`, `consumable.greater_mana_potion`) ainda fazem diferença no nível 25 (fração da vida e da Mana que devolvem). Se não, criar a próxima (superior), com preço e recarga, e registrar.
3. **Quedas e ouro** dos monstros novos (T-510 a T-512), no padrão de hoje para normal, elite e chefe. A Bruxa e o Capataz com tabela de chefe.
4. **Economia:** o teste de economia de hoje (`EconomyDesignTests`) estendido até o 27, para o ouro de uma hora de caça pagar as poções que essa hora gasta, como no primeiro mapa.
5. Atualizar `design/itens.md`.

## Critérios de aceite

- testes de economia e de itens passando, com relatório em `tasks/reports/`;
- `./scripts/check.sh` completo passa.

## Resultado

_(a preencher)_
