# T-056 — Monstros da fase 3 com arte LPC

- **Ambiente:** Windows
- **Modelo:** Humano + Médio
- **Depende de:** T-053; o mecanismo `--art=<variante>` do `SpriteCatalog` (T-090), que entra em `main` com a branch `docs/encerrar-trilha-pixellab`
- **Libera:** a tarefa de cliente da fase 3 que liga os monstros no jogo (criada pela T-240)
- **Áreas que pode alterar:** `client/assets/art/monsters/`, `client/assets/art/characters/`, `client/assets/art/sprites.json`, `client/scripts/Presentation/` (só para o efeito visual de fantasma/névoa), `design/art-direction-v0.1.md` (tabela de lacunas e de pacotes), `CREDITS.md`, `tasks/reports/`

## Contexto

O piloto PixelLab (T-090) foi reprovado: as animações de andar e de ataque ficaram abaixo do LPC. A pesquisa `tasks/reports/arte-alternativas.md` recomenda manter o LPC. Faltam sprites para os monstros da fase 3, que estão marcados como lacuna em `design/art-direction-v0.1.md`.

Com os recursos LPC que existem hoje, dá para montar quase todos:

- **O gerador Universal LPC** (o mesmo das classes, do Salteador e do Guarda Esquelético) tem corpos não humanos: esqueleto, zumbi, orc, goblin, minotauro, homem-lobo, lagarto e outros. Todos vêm com o conjunto completo de animações (andar, golpe, estocada, conjurar, atirar, cair).
- **Pacotes LPC de criaturas** no OpenGameArt:
  - [[LPC] Monsters](https://opengameart.org/content/lpc-monsters): morcego, slime, cobra, minhoca e fantasma; os ataques ficam em [outro pacote](https://opengameart.org/node/133276) e as mortes em [mais um](https://opengameart.org/node/120992);
  - [LPC Fairy](https://opengameart.org/content/lpc-fairy) e [LPC Fairy Wings Pack 2](https://opengameart.org/content/lpc-fairy-wings-pack-2);
  - fantasmas: [ghost sprite](https://opengameart.org/content/ghost-sprite) e [animated filthy ectoplasm](https://opengameart.org/content/animated-filthy-ectoplasm);
  - [LPC Zombie](https://opengameart.org/content/lpc-zombie) e o [Undead LPC pack](https://sorthen.itch.io/undead-lpc-pack).
- **Ferramentas:**
  - [lpctools](https://www.github.com/bluecarrot16/lpctools): recolorir com paleta e montar folhas;
  - efeitos no Godot (transparência, tom, brilho) para fantasmas e névoa, sem editar a arte.

Os ids e os números dos monstros da fase 3 ainda não existem em `data/monsters.json`; eles saem do planejamento da fase 3 (T-240) e das tarefas de dados dela. Esta tarefa produz a arte antes e deixa tudo pronto para a ligação.

## Monstros e caminho sugerido

| Monstro | Onde | Caminho sugerido |
|---|---|---|
| Fada da Névoa | Floresta Baixa | LPC Fairy com asas do Fairy Wings Pack 2, **ou** corpo pequeno do gerador com asas do LPC Revised; versão "na névoa" semitransparente no Godot |
| Ent Corrompido | Floresta Baixa | **sem fonte LPC conhecida.** Procurar (árvore animada, golem de madeira); se não houver, registrar como encomenda |
| Sacerdote Morto | Ruínas da Estrada Antiga | gerador: corpo de esqueleto ou zumbi, manto e capuz de sacerdote, cajado; usa `spellcast` para conjurar |
| Cavaleiro Espectral | Ruínas da Estrada Antiga | gerador: corpo de esqueleto ou zumbi, armadura de placas, elmo, espada e escudo; no Godot, semitransparente e azulado |
| Vigia Sem Rosto (chefe) | Cripta do Vigia | gerador: manto longo com capuz e máscara (ou cabeça sem rosto), recolorido escuro, em escala ×2 se o raio de chefe pedir; precisa ficar legível com os telegráfos por cima (regra da T-054) |
| Sombras menores | Cripta do Vigia | gerador ou o fantasma LPC, escurecido e semitransparente no Godot |

Se algum caminho não funcionar no teste, procure outro dentro do LPC antes de desistir. O que não tiver solução entra no relatório como **encomenda a artista no estilo LPC**, junto com a Matriarca (que continua sendo a aranha `spider04` em ×2).

## Requisitos

1. **Montagem (humano + agente).** O agente prepara, para cada monstro do gerador, a lista de peças: corpo, roupa, arma, cores e o link do gerador já configurado, se possível. O designer exporta as folhas, porque o navegador do agente não grava downloads (decisão da T-053). Pacotes do OpenGameArt são baixados com a confirmação do designer, como na T-050.
2. **Animações mínimas** (`design/art-direction-v0.1.md` §5): parado, andar, atacar e morrer nas 4 direções, e conjurar para quem conjura (Sacerdote, Vigia, Fada, se o design pedir). Monstro de pacote sem alguma direção ou animação vai para o relatório, com uma proposta.
3. **Tamanho:** use os raios que o design da fase 3 indicar. Se ainda não houver raio, use referências do jogo: Fada ~0,35; Sacerdote e Cavaleiro ~0,45 a 0,50; sombras ~0,40; Vigia de chefe ~0,90. O pivô (pés) fica no centro do círculo de colisão (§3).
4. **Efeito de fantasma/névoa:** transparência e tom definidos nos **dados do cliente** (`sprites.json`, por exemplo `"tint"` e `"alpha"`), não por id em código. Se o `EntityView` precisar de um campo novo, ele é genérico e vale para qualquer sprite.
5. **Ligação no `sprites.json`** com ids provisórios em inglês, seguindo a convenção: `monster.mist_fairy`, `monster.corrupted_ent`, `monster.dead_priest`, `monster.spectral_knight`, `monster.faceless_watcher`, `monster.shade`. Registre os ids no Resultado; a tarefa de dados da fase 3 usa os mesmos ou renomeia aqui.
6. **Teste em movimento no jogo, antes de dar por pronto** (lição da T-090: o designer avalia a arte pelo andar e pelo ataque). Como os monstros ainda não existem no Core, crie uma variante `variants.preview_fase3` no `sprites.json` que troca monstros atuais pelos novos: por exemplo, o Salteador vira o Cavaleiro, o Guarda Esquelético vira o Sacerdote, o Lobo vira a Fada. Rode com `--art=preview_fase3` e grave GIFs ou capturas andando e atacando nas 4 direções, ao lado do Guerreiro LPC.
7. **Créditos e licença:** cada arquivo em `CREDITS.md` e no `CREDITS.txt` da pasta. Prefira peças OGA-BY/CC-BY; arte derivada de CC-BY-SA fica em CC-BY-SA (§ licenças da direção de arte). GPL só com decisão do designer.
8. **Direção de arte:** atualize a tabela de lacunas e a de pacotes em `design/art-direction-v0.1.md`.
9. **Relatório** `tasks/reports/arte-monstros-fase-3.md`: capturas e GIFs de cada monstro, a fonte e a licença de cada um, o que funcionou e o que ficou como encomenda.

## Critérios de aceite

- cada monstro da fase 3 com sprite LPC animado, visto em movimento com `--art=preview_fase3` e **aprovado pelo designer**; ou registrado como encomenda, com o motivo;
- o modo normal (sem `--art`) inalterado;
- créditos completos e licenças conferidas;
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Resultado

**Em andamento (2026-11-05), aguardando o designer.**
- Montagens propostas no gerador para Cavaleiro, Sacerdote, Vigia, Sombra e Fada, com links e amostra em `tasks/reports/arte-monstros-fase-3.md`.
- Dois candidatos a Ent (OpenGameArt, CC-BY) listados; o download espera autorização.
- Cliente: campo genérico `tint` no `sprites.json` (`SpriteDefinition.Tint`, multiplicado no `EntityView`).
- A branch saiu de `docs/encerrar-trilha-pixellab`, que traz o `--art=<variante>` e ainda não está em `main`.
