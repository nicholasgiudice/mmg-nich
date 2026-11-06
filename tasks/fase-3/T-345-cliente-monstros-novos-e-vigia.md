# T-345 — Cliente: sprites e leitura dos monstros novos e do Vigia

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** T-340, T-341, T-351
- **Libera:** T-380
- **Áreas que pode alterar:** `client/`, `CREDITS.md`

## Contexto

A T-340 e a T-341 criaram a Fada da Névoa, o Ent Corrompido, o Sacerdote Morto e o Cavaleiro Espectral. A T-351 criou o Vigia Sem Rosto, as sombras, os selos, o medo e o fim do encontro com falha. O cliente desenha os monstros por `client/assets/art/sprites.json` (T-053) e os efeitos por `effects.json` (T-054), com pacotes LPC aprovados em `design/art-direction-v0.1.md`. Sem sprite, a unidade aparece como círculo.

## Requisitos

1. **Sprites** dos cinco monstros novos e das sombras, dos pacotes já aprovados ou de pacotes LPC compatíveis (com licença, `CREDITS.txt` e `CREDITS.md`). Sem arte adequada, use uma variação de cor de um sprite existente e registre.
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
