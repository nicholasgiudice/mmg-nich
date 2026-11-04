# T-094 — Personagens: as 4 classes e os NPCs (PixelLab)

- **Ambiente:** Windows
- **Modelo:** Humano + Médio
- **Depende de:** T-091
- **Libera:** —
- **Áreas que pode alterar:** `client/assets/art/pixellab/`, `client/assets/art/characters/`, `client/assets/art/sprites.json`, `client/scripts/Presentation/`, `client/scripts/Account/SpritePreview.cs`, `client/scripts/Hud/` (só o boneco do inventário), `CREDITS.md`

## Contexto

As classes (Guerreiro, Arqueiro, Clérigo e Mago) usam hoje as folhas do gerador Universal LPC (T-053). O piloto (T-090) fez o Guerreiro no PixelLab. A direção v0.2 (T-091) fixou o estilo, os quadros, as direções, as animações e como o equipamento aparece.

## Requisitos

1. **As 4 classes** com todas as animações da v0.2: parado, andar, atacar (golpe para o Guerreiro e o Clérigo, disparo para o Arqueiro, conjuração para o Mago), conjurar e morrer. Use a mesma referência de estilo e o mesmo tamanho de corpo, para que as quatro pareçam do mesmo jogo. Refaça o Guerreiro do piloto, se a v0.2 mudou algo.
2. **Equipamento**, conforme a decisão da T-091 (visual fixo, variações ou arma separada).
3. **NPCs da Cidade** (lojas da T-321 e os outros prédios): sacerdote do Templo, ferreiro, mercador, estalajadeiro, instrutor e guarda do armazém. Basta parado e uma animação simples (respirar ou acenar), olhando para baixo.
4. **No cliente:**
   - `sprites.json` com as classes novas no layout `pixellab`;
   - o personagem na tela de login e de criação (`SpritePreview`, T-220);
   - o boneco do inventário (T-083) com o sprite novo.
   Tirar a opção `--art=pixellab` só quando a T-095 também terminar. Até lá, ela troca classes e monstros juntos.
5. Tirar do repositório as folhas LPC das classes que deixarem de ser usadas, e atualizar `CREDITS.md` e o `manifest.json`.

## Critérios de aceite

- capturas das 4 classes em cada animação e direção, e dos NPCs na Cidade, aprovadas pelo designer;
- a tela de criação de personagem e o inventário com o sprite novo;
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
