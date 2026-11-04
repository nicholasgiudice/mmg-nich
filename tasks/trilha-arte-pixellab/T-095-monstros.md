# T-095 — Monstros: atuais e da fase 3 (PixelLab)

- **Ambiente:** Windows
- **Modelo:** Humano + Médio
- **Depende de:** T-091
- **Libera:** —
- **Áreas que pode alterar:** `client/assets/art/pixellab/`, `client/assets/art/monsters/`, `client/assets/art/characters/` (o Salteador e o Esqueleto), `client/assets/art/sprites.json`, `client/scripts/Presentation/`, `CREDITS.md`

## Contexto

Os monstros usam hoje folhas LPC (T-053): rato gigante, lobo recolorido, aranhas e o gerador para o Salteador e o Guarda Esquelético. A Matriarca é a aranha vermelha em escala ×2, porque nenhum pacote tinha arte própria para ela (lacuna em `design/art-direction-v0.1.md`). A fase 3 traz a Fada da Névoa, o Ent Corrompido, o Sacerdote Morto, o Cavaleiro Espectral, o Vigia Sem Rosto e as sombras (T-340, T-341, T-351). A T-345 (fase 3) liga esses monstros no cliente.

## Requisitos

1. **Monstros atuais**, com as animações da v0.2 (parado, andar, atacar, morrer) e no tamanho de cada raio de colisão:
   - Rato de Campo, Lobo Cinzento (refazer o do piloto, se a v0.2 mudou algo), Salteador da Estrada (**com besta**, porque ele usa "Disparo de Besta"), Guarda Esquelético, Aranha pequena (o tamanho deve bater com o raio de 0,35; pendência da T-053) e Aranha grande;
   - **Matriarca da Floresta com sprite próprio**, tamanho de chefe e sem escala ×2.
2. **Monstros da fase 3**, com os ids de definição que a T-340, a T-341 e a T-351 criaram (Fada, Ent, Sacerdote, Cavaleiro, Vigia e sombras). Se alguma dessas tarefas ainda não terminou, use o design dela para o visual e deixe para ligar no `sprites.json` quando o id existir. A Fada precisa de uma versão "na névoa" (semitransparente ou com efeito), e o Vigia precisa de uma leitura clara de cada fase, se o design pedir.
3. **No cliente:** `sprites.json` com todos os monstros no layout `pixellab`. Ao terminar esta tarefa e a T-094, o PixelLab vira o padrão e a opção `--art=pixellab` sai (ou vira `--art=lpc`, enquanto houver asset LPC).
4. Coordene com a T-345: se ela ainda não começou, ela usa estes sprites; se já terminou, esta tarefa troca os sprites provisórios dela.
5. Tirar as folhas LPC que deixarem de ser usadas, e atualizar `CREDITS.md` e o `manifest.json`.

## Critérios de aceite

- capturas de cada monstro em combate (encontros de teste locais), aprovadas pelo designer;
- a Matriarca e o Vigia legíveis com os telegráfos por cima (regra da T-054);
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
