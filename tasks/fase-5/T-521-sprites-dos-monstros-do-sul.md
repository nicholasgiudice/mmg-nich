# T-521 — Sprites LPC dos monstros do sul (16 ao 27)

- **Ambiente:** Windows
- **Modelo:** Humano + Médio
- **Depende de:** —
- **Libera:** T-540
- **Áreas que pode alterar:** `client/assets/art/characters/`, `client/assets/art/sprites.json`, `CREDITS.md`, `tasks/reports/`

## Contexto

Os monstros do primeiro mapa foram montados no [gerador Universal LPC](https://liberatedpixelcup.github.io/Universal-LPC-Spritesheet-Character-Generator/), com uma amostra aprovada pelo designer (T-056, `tasks/reports/arte-monstros-fase-3.md`). O que o gerador não tinha (o Ent) foi buscado em pacotes do OpenGameArt ou virou encomenda. O PixelLab foi reprovado para personagens animados (T-090): as animações ficaram abaixo do LPC.

Os ids já estão fixados nas tarefas de monstro (T-510, T-511, T-512). Esta tarefa pode rodar antes delas.

## Requisitos

1. Montagens no gerador (link de cada montagem no relatório, como na T-056):
   - `monster.drowned`: morto-vivo encharcado (corpo de zumbi ou esqueleto, roupa rasgada, tom verde ou azul);
   - `monster.marsh_lizardman`: corpo de lagarto, lança;
   - `monster.marsh_witch`: feminino, manto e chapéu, cajado (chefe: um pouco maior pelo `scale`, como o Vigia);
   - `monster.possessed_miner`: humano com picareta, olhos ou tom alterados;
   - `monster.mountain_wolf`: o sprite do Lobo Cinzento com outra cor (`tint`), se o designer aprovar.
2. Sem corpo no gerador, buscar pacote com licença livre (4 direções, andar, atacar, morrer) ou registrar como encomenda:
   - `monster.marsh_serpent`, `monster.cave_bat`, `monster.stone_golem`, `monster.forged_overseer`.
3. Se uma criatura não tiver arte boa, **propor outra criatura** que caiba no papel, em vez de mudar o estilo (`design/mapas-16-40-v0.1.md` §5). Registre a troca para a tarefa de monstro correspondente.
4. Prancha de amostra com todos, em movimento; o designer aprova.

## Critérios de aceite

- folhas em `client/assets/art/characters/` e ids ligados no `sprites.json`;
- créditos em `CREDITS.md`;
- relatório `tasks/reports/arte-monstros-fase-5.md` com amostra, links e encomendas pendentes;
- `./scripts/check.sh` completo passa.

## Resultado

_(a preencher)_
