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

_(preencher ao concluir)_
