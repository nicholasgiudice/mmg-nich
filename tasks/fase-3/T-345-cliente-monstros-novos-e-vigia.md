# T-345 — Cliente: sprites e leitura dos monstros novos e do Vigia

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** T-340, T-341, T-351, T-056 (sprites dos monstros novos)
- **Libera:** T-380
- **Áreas que pode alterar:** `client/`, `CREDITS.md`

## Contexto

A T-340 e a T-341 criaram a Fada da Névoa, o Ent Corrompido, o Sacerdote Morto e o Cavaleiro Espectral. A T-351 criou o Vigia Sem Rosto, as sombras, os selos, o medo e o fim do encontro com falha. O cliente desenha os monstros por `client/assets/art/sprites.json` (T-053) e os efeitos por `effects.json` (T-054), com pacotes LPC aprovados em `design/art-direction-v0.1.md`. Sem sprite, a unidade aparece como círculo.

**A arte já existe (T-056, `tasks/reports/arte-monstros-fase-3.md`):** o Cavaleiro, o Sacerdote, o Vigia (×2), a Sombra e a Fada foram montados no gerador LPC e estão no `sprites.json` com os ids `monster.spectral_knight`, `monster.dead_priest`, `monster.faceless_watcher`, `monster.shade` e `monster.mist_fairy`. Os fantasmas usam o campo `tint` (cor e transparência). A variante `preview_fase3` (`--art=preview_fase3`) mostrava os sprites no lugar dos monstros antigos enquanto os dados não existiam. **O Ent Corrompido não tem arte**: os candidatos LPC foram reprovados e ele vai para encomenda.

## Requisitos

1. **Sprites:**
   - confira se os ids criados pela T-340, T-341 e T-351 batem com as chaves da T-056 no `sprites.json`; se mudaram, renomeie as chaves;
   - **Ent Corrompido:** até a encomenda chegar, use um sprite provisório (uma variação de cor de um sprite existente) e registre;
   - ajuste o `tint` e o tamanho no jogo, se precisar, e **remova a variante `preview_fase3`**, que deixa de ser necessária;
   - a Fada "na névoa" (requisito 2) é um estado de jogo: a transparência dela vem desse estado, por cima do `tint` normal.
2. **Leitura das mecânicas novas:**
   - Fada "na névoa": semitransparente e sem barra de vida, com aviso antes de sumir;
   - medo: ícone sobre o personagem e a barra de controle (como os outros controles);
   - selos: barra de vida própria, sempre visível na tela do chefe (topo da tela, com os 3 selos);
   - o cast do Último Comando: barra de cast destacada, porque **precisa** ser interrompido;
   - o fim com falha: tela "Expedição fracassada" (como o resumo de morte).
3. Telegráfos e contornos de todas as habilidades novas legíveis por cima dos efeitos (regra da T-054). Conferir com capturas.
4. Os nomes dos monstros novos aparecem no log, na lista de batalha (T-312, se já integrada) e no site gerado (T-071), sem mudança manual.

## Critérios de aceite

- capturas de cada monstro novo em combate (encontros de teste locais) e da luta do Vigia nas três fases;
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
