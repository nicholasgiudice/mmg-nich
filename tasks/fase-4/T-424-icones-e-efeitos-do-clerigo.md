# T-424 — Cliente: ícones e efeitos das habilidades do Sacerdote e do Inquisidor

- **Ambiente:** Windows
- **Modelo:** Humano + Médio
- **Depende de:** T-423
- **Libera:** T-425
- **Áreas que pode alterar:** `client/assets/art/icons/`, `client/assets/art/effects/`, `client/scripts/Presentation/` (catálogos de ícones e efeitos, `EffectCatalog`, `AreaView`), `client/scripts/` da tela de habilidades (só se a escolha da segunda classe precisar de ajuste para o Clérigo), `CREDITS.md`, `design/art-direction-v0.1.md` (só para registrar a origem das peças novas), `tasks/reports/`
- **Branch:** sai da `main` e é integrada na `main` (seção 6g de `tasks/README.md`).

## Contexto

As 10 habilidades novas do Clérigo (T-423, ids `priest.*` e `inquisitor.*`) precisam de ícone e de efeito visual. Faça como na T-412 (Guerreiro), que o designer aprovou:

- ícones gerados no PixelLab (modelo Pro) com ícones do Clérigo da T-087 como referência de estilo, em 64 px e reduzidos para 32 px; o caminho que funcionou e o que deu errado estão no Resultado da T-412;
- novas linhas em `client/assets/art/icons/icons.png` e `icons.json`, com a origem em `source`;
- efeitos em `client/assets/art/effects/` (`effects.json`, `EffectCatalog`);
- o designer prefere arte simples, com pouco detalhe, e quer ver uma **amostra aprovada no jogo antes** de fazer o resto.

A tela de escolha e a tela de habilidades da T-410 leem as segundas classes de `classes.json`: com a T-423, o Clérigo já deve mostrar o Sacerdote e o Inquisidor. Conferir.

## Requisitos

1. **Amostra primeiro:** propor o ícone e o efeito de 2 habilidades (uma do Sacerdote, uma do Inquisidor; sugestão: Inspiração e Consagração), mostrados no jogo, e esperar a aprovação do designer antes das outras 8.
2. Um **ícone** para cada uma das 10 habilidades, no mesmo tamanho e estilo dos atuais.
3. Um **efeito visual** para cada habilidade, lido de longe:
   - Oração Restauradora, Bênção, Graça Veloz e Inspiração: o pulso em volta do Sacerdote ao lançar e um sinal pequeno em cada aliado enquanto durar, diferente para cada uma (cura, atributos, velocidade, Mana);
   - Santuário: a área em volta do Sacerdote e um sinal nos aliados protegidos;
   - Consagração: a área no chão;
   - Selo da Luz: a marca no monstro enquanto durar;
   - Martelo da Justiça e Julgamento: impacto no alvo;
   - Ira Divina: um sinal no Inquisidor enquanto durar e o Golpe Sagrado em área.
4. Conferir a escolha da segunda classe no Clérigo pela ferramenta do Esc (T-411): subir ao 20, ver as duas opções com as habilidades, escolher, desfazer e escolher a outra.
5. Toda peça nova tem origem e licença registradas em `CREDITS.md`.

## Critérios de aceite

- amostra aprovada pelo designer antes do resto;
- as 10 habilidades com ícone no painel de habilidades e na barra;
- cada habilidade mostra o efeito no jogo (capturas em `tasks/reports/interface/clerigo-efeitos/`, com uma prancha final como a da T-412);
- a escolha do Sacerdote e do Inquisidor funciona na tela de habilidades;
- `./scripts/check.sh --fast` passa no Windows.

## Resultado

_(preencher ao concluir)_
