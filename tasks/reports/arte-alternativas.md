# Alternativas para a arte depois do PixelLab

Data: 2026-11-05. Contexto: o piloto PixelLab (T-090, `tasks/reports/arte-pixellab-piloto.md`) foi reprovado pelo designer. Os sprites parados ficaram bons, mas **as animações de andar e de ataque ficaram abaixo do LPC**. Esta pesquisa compara os caminhos possíveis com esse critério na frente.

## Critérios

1. **Qualidade das animações no jogo**, principalmente andar e ataque nas 4 direções. Foi o que reprovou o PixelLab.
2. **Escala e vista:** três quartos de cima, tiles de 32 px, personagens com cerca de 1,5 tile (`design/art-direction-v0.1.md`).
3. **Consistência do elenco:** 4 classes, Salteador, Guarda Esquelético, monstros (até a Matriarca) e os da fase 3, todos com o mesmo traço.
4. **Licença:** uso comercial, e **compatível com um jogo cujo código é escrito por agentes de IA**. Esse ponto elimina algumas opções (ver Mana Seed).
5. **Custo** em dinheiro e em trabalho.

## Resumo

| Caminho | Animações | Consistência | Licença | Custo | Veredito |
|---|---|---|---|---|---|
| **1. Ficar no LPC e aproveitar mais dele** | as que o designer já aprovou no jogo; o LPC Revised tem animações de combate novas | alta (base comum) | OGA-BY / CC-BY, compatível | zero em dinheiro; trabalho de integração | **recomendado como base** |
| 2. Pacotes pagos (itch.io) | variam muito; muitos com 3 a 6 quadros | baixa entre pacotes; cada um tem um estilo | varia; alguns proíbem IA | US$ 10 a 40 por pacote | só pontualmente |
| 2a. Mana Seed (Seliel the Shaper) | boas: andar e correr de 6 quadros, combate com espada e escudo, lança e arco | alta dentro da linha | **proíbe uso junto com qualquer coisa feita por IA, inclusive código** | ~US$ 20 a 38 | **eliminado pela licença** |
| 2b. Time Fantasy | andar de 3 quadros (padrão RPG Maker); o ataque vem em outro pacote, de lado | alta dentro da linha | comercial | baixo | não serve para combate de ação |
| 3. Encomendar a um artista | as melhores possíveis, sob medida | total | negociada | alto (ver abaixo) | **para lacunas**, no estilo LPC |
| 4. Outras IAs (Retro Diffusion, Ludo, God Mode) | nenhuma análise mostra animação melhor que a do PixelLab | mesma limitação | varia | baixo | pouca chance de resolver |
| 5. 3D → pixel art (modelo 3D + animações renderizadas em sprites) | consistentes em todas as direções; ilimitadas | alta | depende dos modelos | médio; requer montar um pipeline | experimento possível, outro visual |

## 1. Ficar no LPC e aproveitar mais dele (recomendado)

O LPC é a única opção que o designer já viu **em movimento no jogo** e preferiu. Além disso, ainda tem animações que o jogo não usa:

- **O gerador Universal LPC**, que é o que usamos hoje, já tem `walk`, `slash`, `thrust`, `spellcast`, `shoot` e `hurt`. A versão expandida (LPCE) acrescenta `run`, `jump`, `climb` e arco. A própria página avisa que muitas peças ainda não foram desenhadas para as animações novas.
- **O LPC Revised** (Eliza Wyatt), que já é a base do nosso terreno, tem corpos novos com estas animações: `Walk`, `Run`, `Idle`, `Jump`, `Climb`, `Sitting`, `Emotes`, `Combat 1h - Idle`, `Combat 1h - Slash`, `Combat 1h - Backslash` e `Combat 1h - Halfslash`. Isso dá o que falta hoje no combate: postura de combate parada e golpes variados (ida e volta) para encadear ataques.

**O que dá para melhorar sem trocar de arte:**
- trocar o golpe do Guerreiro e do Clérigo por golpe e contragolpe alternados (`slash` + `backslash`);
- usar a postura de combate parada (`Combat 1h - Idle`) enquanto houver alvo, e `run` para o movimento rápido;
- fazer o parado respirar, que hoje mostra só o primeiro quadro (`EntityView`).

**Limitações:**
- as roupas e armas do LPC Revised ainda não cobrem todas as animações novas; é preciso conferir peça por peça para as 4 classes;
- continua faltando sprite próprio para a Matriarca e para os monstros da fase 3 (Fadas da Névoa, Cavaleiro Espectral, Vigia Sem Rosto). Isso já estava nas lacunas de `design/art-direction-v0.1.md`.

## 2. Pacotes pagos

Há muitos pacotes de personagens de cima com 4 direções e andar, ataque e morte no itch.io, alguns a partir de US$ 10. Os problemas para o Valdran:
- **Escala:** muitos usam personagens de 32×32 (1 tile de altura), menores que o nosso 1,5 tile. Misturá-los com o LPC fica desigual.
- **Consistência:** cada pacote tem o seu traço e a sua paleta. Montar o elenco inteiro com pacotes diferentes repete o problema que o LPC já resolve.
- **Licença:** é preciso ler a de cada um. Alguns usam CC-BY-ND, que proíbe modificar a arte (nem recolorir); outros proíbem IA.

**Mana Seed** é o pacote de mais qualidade nesse estilo: personagem de 32 px em quadros de 64, sistema de camadas para roupas e armas, andar e correr de 6 quadros e combate com espada e escudo, lança e arco. **Mas a autora afirma que usar a arte junto com qualquer conteúdo gerado por IA, "inclusive código", viola a licença.** O código do Valdran é escrito por agentes de IA, então ele está fora. A licença completa fica em `selieltheshaper.weebly.com/user-license.html`; vale lê-la se o designer quiser confirmar.

**Time Fantasy** (finalbossblues) é comercial e consistente, mas segue o padrão RPG Maker: andar de 3 quadros, e o combate vem em pacotes de batalha vistos de lado. Não serve para combate de ação visto de cima.

## 3. Encomendar a um artista

É a única forma de ter **animações de qualidade feitas sob medida** e com identidade própria.

**Preços encontrados** (páginas de comissão individuais, sem data, só para ter ideia):
- personagem parado: US$ 20 a 50;
- uma animação de 4 a 8 quadros: US$ 40 a 75;
- personagem com andar: US$ 30 a 45.

Um conjunto completo (parado, andar, ataque, conjurar e morrer, nas 4 direções) é orçado projeto a projeto. Pelas tabelas, deve passar de algumas centenas de dólares por personagem. Refazer o elenco inteiro (~25 personagens) ficaria na casa dos milhares de dólares. Só um orçamento real confirma.

**Uso recomendado:** encomendar **no estilo LPC** apenas o que falta, como a Matriarca e os monstros da fase 3. Muitos artistas trabalham sobre a base LPC, e o resultado fica consistente com o resto do elenco. Ao encomendar, a licença deve permitir uso comercial e o artista precisa estar de acordo com um jogo cujo código é feito por IA.

## 4. Outras ferramentas de IA

As comparações de 2026 que encontrei apontam:
- **Retro Diffusion:** a mais especializada em pixel art, com animação de sprites, rotações em 8 direções e estilos a partir de referência. Cobra por uso (cerca de US$ 0,14 a 0,25 por animação via API).
- **Ludo.ai:** generalista; gera os quadros a partir do texto, sem esqueleto.
- **God Mode AI:** anima arte própria, com exportação para Spine. Não achei avaliação de qualidade.

O ponto central: **as próprias comparações colocam o PixelLab como o melhor em animação de personagem**, por causa do esqueleto e das rotações. A maioria dessas comparações vem de blogs ligados às ferramentas, então são opinião. Mesmo assim, não há indício de que outra IA resolva o que reprovou o PixelLab: andar e golpe consistentes nas 4 direções. Um teste custaria poucos dólares, mas a chance de mudar o veredito é baixa.

## 5. 3D renderizado em pixel art

O personagem é modelado em 3D, recebe animações prontas (por exemplo, do Mixamo) e é renderizado de cima, em projeção ortográfica, quadro a quadro, nas 4 ou 8 direções, com redução para pixel art. Já existem ferramentas para isso: o script `3d-to-2d` para Blender, o Pixelize3D_FBX e o SPRITE.EX, este pago, com câmera de cima.

- **A favor:** as animações ficam **iguais em todas as direções e em todos os personagens**, há animações prontas de sobra e armas e roupas são peças separadas.
- **Contra:** o visual é de "3D pixelado", não de pixel art desenhada à mão, e fica diferente do LPC. Exige modelos 3D (comprar pacotes low-poly ou encomendar) e montar o pipeline. Mudaria a direção de arte inteira, então precisa de um piloto próprio.

## Recomendação

1. **Manter o LPC como arte do jogo** e abrir uma tarefa para melhorar as animações com o que ele já oferece: golpe e contragolpe, postura de combate, correr e parado animado. É o caminho com risco zero de licença e custo zero em dinheiro, e o designer já aprova o resultado em movimento.
2. **Para as lacunas** (Matriarca e fase 3), pedir orçamento a artistas para sprites no estilo LPC.
3. **Só se o designer quiser outro visual no futuro:** um piloto curto de 3D → pixel art com um personagem e um monstro, avaliado em movimento no jogo, como fizemos com o PixelLab.
4. **Assinatura do PixelLab:** cancelar antes de 2026-12-05, salvo se ela for usada para ícones ou objetos parados, onde foi bem.

## Fontes

- [Universal LPC Spritesheet Character Generator (repositório oficial)](https://github.com/LiberatedPixelCup/Universal-LPC-Spritesheet-Character-Generator)
- [Expanded Universal LPC Spritesheet: Idle, Run, Jump, LPC Revised Combat (OpenGameArt)](https://opengameart.org/comment/107223)
- [LPC Revised, Eliza Wyatt (repositório)](https://github.com/ElizaWy/LPC). A lista de animações dos corpos foi lida da árvore de arquivos do repositório.
- [Mana Seed Character Base (itch.io)](https://seliel-the-shaper.itch.io/character-base) e comentários da autora sobre IA: [1](https://itch.io/post/14276406), [2](https://itch.io/post/12516802)
- [Time Fantasy RPG Sprites (itch.io)](https://finalbossblues.itch.io/tf-rpg-charactersprites-1)
- Pacotes de personagens de cima: [32x32 top down character animation pack](https://itch.io/s/198836/launch-sale-32x32-top-down-character-animation-pack), [Ultimate Pixel Top-Down Character Pack](https://ohmsart.itch.io/ultimate-pixel-top-down-character-pack), [Top-down 4-direction Character Asset Pack](https://pixpp.itch.io/top-down), [Dungeon Hero](https://pixel-poem.itch.io/dungeon-hero)
- Preços de comissão: [Pixel Art Commissions (jamesfoxbr)](https://sites.google.com/view/jamesfoxbr/prices/pixel-art), [Tigerhorse's Pixel Commissions](https://tigerhorse-pixel-commissions.carrd.co), [A simple pricing sanity-check (Aseprite)](https://community.aseprite.org/t/a-simple-pricing-sanity-check-for-pixel-art-commissions/28697)
- Comparações de IAs: [Ludo: 8 Best AI Pixel Art Generators 2026](https://ludo.ai/compare/best-ai-pixel-art-generators), [Retro Diffusion vs PixelLab](https://gamedevaihub.com/retro-diffusion-vs-pixellab/), [Best AI Sprite Sheet Generators 2026](https://gamedevaihub.com/best-ai-sprite-sheet-generators/), [Cinevva: AI Pixel Art and Sprite Generators 2026](https://app.cinevva.com/guides/ai-pixel-art-generators)
- 3D → pixel art: [3d-to-2d (Blender + Mixamo)](https://awesome.ecosyste.ms/projects/github.com%2Falexferl%2F3d-to-2d), [Pixelize3D_FBX](https://la-llama-studio.itch.io/fbx3d-to-sprite-sheets-2d), [SPRITE.EX](https://xionworld.itch.io/spriteex), [dissertação UFPE sobre 3D em pixel art de cima](https://repositorio.ufpe.br/handle/123456789/52665)
