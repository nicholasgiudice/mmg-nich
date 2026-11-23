# T-434 — Cliente: ícones e efeitos das habilidades do Atirador de Elite e do Caçador

- **Ambiente:** Windows
- **Modelo:** Humano + Médio
- **Depende de:** T-433
- **Libera:** T-435
- **Áreas que pode alterar:** `client/assets/art/icons/`, `client/assets/art/effects/`, `client/scripts/Presentation/` (catálogos de ícones e efeitos, `EffectCatalog`, `AreaView`), `client/scripts/` da tela de habilidades (só se a escolha da segunda classe precisar de ajuste para o Arqueiro), `CREDITS.md`, `design/art-direction-v0.1.md` (só para registrar a origem das peças novas), `tasks/reports/`
- **Branch:** sai da `main` e é integrada na `main` (seção 6h de `tasks/README.md`).

## Contexto

As 10 habilidades novas do Arqueiro (T-433, ids `marksman.*` e `hunter.*`) precisam de ícone e de efeito visual. Faça como na T-412 (Guerreiro), que o designer aprovou:

- ícones gerados no PixelLab (modelo Pro) com ícones do Arqueiro da T-087 como referência de estilo, em 64 px e reduzidos para 32 px; o caminho que funcionou e o que deu errado estão no Resultado da T-412;
- novas linhas em `client/assets/art/icons/icons.png` e `icons.json`, com a origem em `source`;
- efeitos em `client/assets/art/effects/` (`effects.json`, `EffectCatalog`);
- o designer prefere arte simples, com pouco detalhe, e quer ver uma **amostra aprovada no jogo antes** de fazer o resto.

Se a T-424 (Clérigo) já tiver entrado, as linhas novas de `icons.png` vêm depois das dela.

## Requisitos

1. **Amostra primeiro:** propor o ícone e o efeito de 2 habilidades (uma do Atirador, uma do Caçador; sugestão: Tiro Certeiro e Nuvem Tóxica), mostrados no jogo, e esperar a aprovação do designer antes das outras 8.
2. Um **ícone** para cada uma das 10 habilidades, no mesmo tamanho e estilo dos atuais; as **passivas** (Mira Firme e Flecha Envenenada) diferentes o bastante para a moldura de passiva da T-410 funcionar.
3. Um **efeito visual** para cada habilidade ativa, lido de longe:
   - Flecha Perfurante: a flecha atravessando a linha;
   - Tiro Debilitante e Ferida Aberta: impacto e um sinal no alvo enquanto durar;
   - Tiro Certeiro: um tiro mais forte que o Disparo, com impacto maior;
   - Olho de Falcão: um sinal no Atirador enquanto durar;
   - Nuvem Tóxica: a nuvem no chão;
   - Flecha Dispersa: as linhas do alvo até os monstros que recebem a cópia;
   - Praga: a área ao lançar e o pulo do veneno de um monstro para outro;
   - o Veneno do Caçador com o número de pilhas legível no alvo.
4. Conferir a escolha da segunda classe no Arqueiro pela ferramenta do Esc (T-411): subir ao 20, ver as duas opções, escolher, desfazer e escolher a outra.
5. Toda peça nova tem origem e licença registradas em `CREDITS.md`.

## Critérios de aceite

- amostra aprovada pelo designer antes do resto;
- as 10 habilidades com ícone no painel de habilidades e na barra;
- cada habilidade ativa mostra o efeito no jogo (capturas em `tasks/reports/interface/arqueiro-efeitos/`, com uma prancha final como a da T-412);
- a escolha do Atirador de Elite e do Caçador funciona na tela de habilidades;
- `./scripts/check.sh --fast` passa no Windows.

## Resultado

_(preencher ao concluir)_
