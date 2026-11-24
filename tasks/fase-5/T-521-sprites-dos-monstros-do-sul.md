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

**PARCIAL: feito até a aprovação do designer (2026-11-24).** Relatório completo, links, licenças e amostra: [`tasks/reports/arte-monstros-fase-5.md`](../../tasks/reports/arte-monstros-fase-5.md).

**O que foi feito**
- Cinco folhas montadas no gerador Universal LPC (links no relatório): `drowned`, `marsh_lizardman`, `marsh_witch` (`scale` 2), `possessed_miner` e `forged_overseer`, em `client/assets/art/characters/`, com `-credits.txt`.
- Quatro do OpenGameArt/gerador: `stone_golem` ([LPC] Golem, Redshrike, CC-BY 3.0), `cave_bat` ([LPC] Bat extended, OGA-BY 3.0) e `marsh_serpent` (LPC Base Assets, CC-BY-SA 3.0, com linha de morte feita por nós), em `client/assets/art/monsters/`, com `CREDITS.txt`; `forged_overseer` virou troca de criatura (ver abaixo).
- `monster.mountain_wolf` reusa a folha do Lobo Cinzento com `tint` `[0.62, 0.72, 0.9]`.
- `sprites.json`: layouts `lpc_snake`, `lpc_bat`, `lpc_golem`, os nove ids ligados (ids da T-510/511/512 sem mudança) e a variante `preview_fase5`. `CREDITS.md` atualizado.
- Prancha estática e GIF em movimento, mais duas capturas dentro do jogo (`--art=preview_fase5`), em `tasks/reports/arte-monstros-fase-5/`; os scripts `board.py` e `build_sheets.py` ficam ao lado.

**Decisões e desvios**
- Capataz Forjado: nenhum pacote livre servia, então propus **troca de criatura**: humano em armadura de ferro negro com elmo de chifres e martelo, montado no gerador (mesmo id e papel). Registrar na T-512.
- A Serpente do OpenGameArt só tem andar em 4 direções; o golpe repete o andar e a morte é provisória (4 quadros gerados por achatar e esmaecer o quadro de frente). Morcego e Serpente são pequenos (~30 px).
- Mineiro: as camisas testadas não apareceram na folha; ele ficou sem camisa, com pele lavanda.
- Fora das áreas listadas só mexi em: `tasks/` (esta tarefa) e nada de código. Os arquivos `.png.import` das folhas novas foram gerados pelo Godot e entram no commit, como os dos outros sprites.

**O designer precisa aprovar** (em movimento): o `tint` do Lobo da Serra; o Afogado (zumbi + `tint` verde-azulado); o Homem-Lagarto (verde vivo); a Bruxa (pele verde, manto roxo, ×2); o Mineiro (pele lavanda); **a troca do Capataz Forjado**; o Golem, o Morcego e a Serpente (e a morte provisória dela).

**Verificação:** `./scripts/check.sh` completo: formatação, build e todos os testes passam, exceto 4 falhas em `Game.Design.Tests` que já existem na `main` e não tocam em arte (Vigia: `WatcherDesignTests.Without_the_cleric_the_win_rate_drops_clearly`, `CryptDesignTests.A_group_of_four_with_rares_beats_the_watcher_in_the_real_room_`; Guardião: `SecondClassDesignTests.Guardian_cuts_the_damage_on_the_rest_of_the_group_by_at_least_...`).

**Falta para fechar a tarefa:** aprovação ou pedidos de troca do designer (refazer a montagem exportando por cima, com o mesmo nome); depois marcar como concluída. Encomenda: nenhuma obrigatória (opcionais no relatório). Para a T-540: conferir a direção das linhas do Golem e do Morcego em jogo, `headPx` e raios.
