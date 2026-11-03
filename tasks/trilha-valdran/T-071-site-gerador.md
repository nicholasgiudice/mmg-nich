# T-071 — Site do jogo: gerador com monstros, itens, classes e habilidades

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** — (T-070 só para o nome; se ainda não estiver em `main`, use "Valdran" mesmo assim)
- **Libera:** T-072
- **Áreas que pode alterar:** `src/Game.Site/` (novo), `tests/Game.Site.Tests/` (novo), `site/` (novo), `MmoRpg.sln`, `scripts/build-site.sh` (novo), `scripts/check.sh` (só para incluir o projeto novo, se precisar), `.gitignore`, `README.md`

## Contexto

O designer quer um site público com todas as informações do jogo: **monstros, itens, classes e habilidades**. Ele vai ficar em `valdran.nich.click` (publicação na T-072). O site tem que acompanhar os dados sem trabalho manual. Por isso ele é **gerado** a partir de `data/*.json` e nenhum número é copiado à mão.

Referências:
- `design/bestiario.md`, `design/habilidades.md` e `design/itens.md` (branch `docs/bestiario-habilidades-itens`, se ainda não estiver em `main`): mostram o que exibir, como ler cada campo (herança entre NH, alvos extras sem `count`, escala dos monstros por nível, arredondamento dos itens) e os pontos em aberto;
- `design/classes-v0.1.md`: função, forças e fraquezas de cada classe;
- `data/classes.json`, `abilities_*.json`, `monsters.json`, `items.json`, `statuses.json`, `areas.json`, `zones.json`, `rules.json`. **Não use `client/data/`**, que é cópia gerada pelo build.

## Requisitos

1. **Gerador**: projeto console `src/Game.Site` em .NET. Ele carrega o conteúdo pelo `Game.Data`, para ter a mesma validação e as mesmas regras de herança do jogo, e escreve HTML estático em `site/dist/` (no `.gitignore`). Uso: `dotnet run --project src/Game.Site -- --data data --out site/dist`.
2. **Páginas** (todas em português):
   - **Início**: o que é Valdran, as classes e links para as seções;
   - **Classes**: uma página por classe, com função, forças e fraquezas, atributos iniciais, vida base e crescimento, velocidade, Mana, arma inicial, armaduras que pode usar, ataque básico e as 5 habilidades;
   - **Habilidades**: uma página por habilidade (ou seção por classe), com custo de Mana, recarga, conjuração, alvo e alcance, efeitos e status, em tabela por NH. A herança de campos entre NH deve estar resolvida, não copiada crua;
   - **Monstros**: lista e uma página por monstro, com nível, categoria, vida, velocidade, habilidades, fases (chefe), onde aparece (zona, subárea, nível, tempo de renascimento) e tabela de queda;
   - **Itens**: armas, armaduras e acessórios com atributos e fórmula por nível, tipos de armadura e quem usa, raridades e afixos, poção e como se obtém cada item;
   - **Status e áreas**: referência ligada a partir das habilidades e dos monstros;
   - **Créditos**: obrigatório se o site usar qualquer sprite, porque as licenças CC-BY, CC-BY-SA e OGA-BY exigem atribuição. Gere a página a partir de `CREDITS.md`.
3. **Textos que não são regra** (descrição curta de classe e monstro, apresentação do jogo) ficam em `site/content/` (Markdown ou JSON, em português). Números, nomes exibidos e ids vêm **só** de `data/`. Nada de valor de jogo dentro do gerador.
4. **Imagens (opcional)**: se for simples, recorte o primeiro quadro de cada personagem e monstro das folhas em `client/assets/art/` (layout em `client/assets/art/sprites.json`) e mostre na página. Se não for simples, deixe sem imagem e registre no Resultado.
5. **Visual**: HTML e CSS próprios, sem framework e sem JavaScript obrigatório (uma busca simples em JS é aceitável). Precisa funcionar no celular (sem rolagem horizontal) e ter tema claro e escuro (`prefers-color-scheme`). Inclua `<title>`, descrição e links internos relativos.
6. **Pontos em aberto dos dados** (ex.: Armadilha de Contenção fora da lista do Arqueiro, Lentidão sem uso): não os mostre como se fossem do jogo. Habilidade que nenhuma classe usa fica fora das páginas de classe; o gerador avisa no console.
7. **Testes** (`tests/Game.Site.Tests`, xUnit): gerar o site com os dados reais e conferir que todo id de monstro, habilidade, item, status, afixo e classe aparece em alguma página; conferir que um número de cada tipo (vida de um monstro, custo de uma habilidade num NH herdado, atributo de um item) sai igual ao JSON; e conferir que não há link interno quebrado.
8. `scripts/build-site.sh`: compila e gera em `site/dist/`. A T-072 usa esse script no deploy.

## Critérios de aceite

- `scripts/build-site.sh` gera o site sem erro na VPS;
- o site aberto localmente (`python3 -m http.server -d site/dist` ou equivalente) mostra todas as seções, e os links funcionam;
- mudar um número em `data/*.json` e gerar de novo muda o site, sem tocar no gerador;
- testes do item 7 passando; `./scripts/check.sh` passa;
- README com a seção "Site do jogo" (como gerar e onde fica).

## Resultado

Concluída em 2026-11-03 (VPS).

**Base da branch.** A tarefa e as referências ainda não estavam em `main`. A branch `task/T-071-site-gerador` saiu de `main` e recebeu o merge de `origin/docs/tarefas-valdran` (esta tarefa) e de `origin/docs/bestiario-habilidades-itens` (`design/bestiario.md`, `habilidades.md` e `itens.md`). Os dois merges não tiveram conflito. Ao integrar, os merges entram junto (ou integre essas duas branches antes).

**O que foi feito**

- `src/Game.Site` (console .NET, no `MmoRpg.sln`): carrega `data/` por `GameDataLoader.LoadDirectory`, com a mesma validação do jogo, e escreve HTML estático. Uso: `dotnet run --project src/Game.Site -- --data data --out site/dist`. Os padrões de `--content site/content`, `--art client/assets/art` e `--credits CREDITS.md` são opcionais. Sai com código 1 se os dados forem inválidos e lista os erros do loader.
- As regras são as do Core, sem cópia: herança dos NH por `AbilityRanks.Resolve`; vida, Mana e crítico do nível 1 por `StatMath`; valores dos itens por `ItemMath.StatsOf` (arredondamento, multiplicador do tipo de armadura, resistência da armadura leve); escala dos monstros por `MonsterScalingRules.HealthFactor/PowerFactor`; XP por `ProgressionRules.MonsterXp`; nível exigido por NH por `RequiredLevel`. No gerador ficam só rótulos em português (nome das escolas, espaços, raridades) e a forma de mostrar.
- Páginas (63): início; classes (índice com as regras comuns de progressão e uma página por classe); habilidades (índice por classe, consumíveis e monstros, e uma página por habilidade); monstros (índice com a escala por categoria, zonas, subáreas, encontros de teste e uma página por monstro com IA, habilidades, fases, onde aparece com vida e potência no nível de nascimento, invocações e tabela de queda); itens (regras, armas, tipos de armadura, armaduras, acessórios, consumíveis, raridade, afixos, equipamento inicial e quedas, com fórmula e valores nos níveis 1, 5, 10 e 15); referência de status e de áreas (com "aplicado por" e "criada por" por NH); créditos; busca; "Em aberto".
- Tabela por NH: Custo, Recarga e Efeitos sempre aparecem por NH. Os outros campos (conjuração, alvo, aviso, acertos) vão para "Em todos os NH" quando não mudam. O que mudou em relação ao NH anterior fica destacado.
- `site/content/site.json`: apresentação do jogo, função/forças/fraquezas e resumo das classes (de `design/classes-v0.1.md`), descrição curta dos monstros e dos perfis de IA, e nomes das fases da Matriarca, que os dados não têm. Nenhum número fica ali. O gerador avisa se faltar texto para uma classe ou monstro.
- Imagens: o quadro parado de frente (direção `down` da animação `idle` de `sprites.json`) de cada classe e monstro. Não há recorte de imagem: a folha PNG é copiada para `assets/sprites/` e a página mostra só o quadro com `background-position` e `image-rendering: pixelated`. O tamanho da folha vem do cabeçalho do PNG, então não precisa de biblioteca de imagem. Com imagens, o gerador exige `CREDITS.md`, e a página de créditos é gerada dele por um conversor de Markdown mínimo (títulos, listas, tabelas, links, código, negrito).
- Visual: CSS próprio (`src/Game.Site/Assets/style.css`, embutido no assembly), tema claro e escuro por `prefers-color-scheme`. Tabelas largas rolam dentro do próprio contêiner, sem rolagem horizontal da página. Toda página tem `<title>`, descrição e viewport, e todos os links internos são relativos. O único JS é o filtro da busca (`assets/search.js`). Sem ele a lista inteira continua visível.
- Pontos em aberto: o gerador calcula o que está em uso (ataque básico, habilidades e consumíveis das classes; habilidades e fases dos monstros; e, a partir delas, status, áreas e invocações, NH a NH). O que sobra só aparece na página "Em aberto" e na página própria, com aviso. Não entra nas páginas de classe nem no índice de habilidades, e o console avisa. Hoje sobram: `archer.containment_trap`, `area.containment_trap`, `status.slow` e `status.snared` (Preso só é aplicado pela armadilha).
- `scripts/build-site.sh`: compila em Release com `-warnaserror` e gera em `site/dist/`, ou na pasta passada como argumento, para a T-072. `site/dist/` entrou no `.gitignore`. README ganhou a seção "Site do jogo".
- `tests/Game.Site.Tests` (10 testes, rodam no `check.sh`; o `check.sh` não precisou mudar): todo id de classe, habilidade, monstro, item, status, área e afixo aparece em alguma página; a vida do Lobo, o custo de NH2 herdado da primeira habilidade do Guerreiro sem `cost` no NH2 e a base e o por-nível da Espada saem iguais ao JSON, lido direto do arquivo e não pelo loader; nenhum link interno quebrado, âncoras incluídas, com mais de 500 links conferidos (hoje cerca de 1200); mudar a vida do Lobo numa cópia de `data/` muda a página gerada; a habilidade sem uso fica fora da página do Arqueiro e aparece em "Em aberto" e nos avisos; a página de créditos vem de `CREDITS.md`; toda página tem título, descrição e viewport.

**Decisões**

- Áreas não têm nome nos dados. O site mostra "Área: <habilidade que cria>", com o NH quando a área muda por NH (ex.: "Área: Campo de Relâmpagos NH3").
- Status com o mesmo nome exibido (as três "Marca do Caçador", "Guarda Firme", "Congelado") ganham o sufixo do id entre parênteses para não se confundirem.
- Itens e afixos ficam numa página só, com âncora por id (`itens/index.html#weapon.sword`). Status e áreas também têm uma página cada, com âncora por id. Classes, habilidades e monstros têm uma página por id.
- Os encontros de teste (`encounters.json`) aparecem nos monstros, marcados como arenas do protótipo, fora do mundo aberto.
- A página de créditos reproduz `CREDITS.md` inteiro, inclusive "Como adicionar créditos". Se o designer preferir, dá para cortar essa seção no arquivo ou no gerador.

**Diferenças encontradas em relação aos documentos de design**

- Mana: `design/habilidades.md` diz "100 de Mana máxima" (o `resource.max` de `classes.json`). Com atributos, o Core usa `ManaBase + ManaPerInt × INT` (`EntityFactory`/`StatMath.MaxMana`), então o site mostra o que o jogo usa: Guerreiro 88, Arqueiro 90, Mago 102, Clérigo 104, e regeneração de 5 + 0,05 × INT. Vale corrigir o documento ou o `resource.max`.

**Pendências**

- Conferência visual (celular, tema escuro, quadros dos sprites) não foi feita: a VPS não tem tela. Na VPS o site respondeu 200 em todas as seções por `python3 -m http.server -d site/dist`. Vale abrir no Windows ou no celular antes da T-072.
- As folhas de sprite inteiras somam cerca de 3 MB em `site/dist`. Se pesar, a T-072 pode recortar os quadros com uma ferramenta de imagem no deploy.

