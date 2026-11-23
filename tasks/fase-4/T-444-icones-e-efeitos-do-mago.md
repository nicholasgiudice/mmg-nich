# T-444 — Cliente: ícones e efeitos das habilidades do Piromante e do Arcanista

- **Ambiente:** Windows
- **Modelo:** Humano + Médio
- **Depende de:** T-443
- **Libera:** T-445
- **Áreas que pode alterar:** `client/assets/art/icons/`, `client/assets/art/effects/`, `client/scripts/Presentation/` (catálogos de ícones e efeitos, `EffectCatalog`, `AreaView`, desenho dos orbes), `client/scripts/` da tela de habilidades (só se a escolha da segunda classe precisar de ajuste para o Mago), `CREDITS.md`, `design/art-direction-v0.1.md` (só para registrar a origem das peças novas), `tasks/reports/`
- **Branch:** sai da `main` e é integrada na `main` (seção 6i de `tasks/README.md`).

## Contexto

As 10 habilidades novas do Mago (T-443, ids `pyromancer.*` e `arcanist.*`) precisam de ícone e de efeito visual. Faça como na T-412 (Guerreiro), que o designer aprovou:

- ícones gerados no PixelLab (modelo Pro) com ícones do Mago da T-087 como referência de estilo, em 64 px e reduzidos para 32 px; o caminho que funcionou e o que deu errado estão no Resultado da T-412;
- novas linhas em `client/assets/art/icons/icons.png` e `icons.json`, com a origem em `source`;
- efeitos em `client/assets/art/effects/` (`effects.json`, `EffectCatalog`);
- o designer prefere arte simples, com pouco detalhe, e quer ver uma **amostra aprovada no jogo antes** de fazer o resto.

Se a T-424 ou a T-434 já tiverem entrado, as linhas novas de `icons.png` vêm depois das delas.

## Requisitos

1. **Amostra primeiro:** propor o ícone e o efeito de 2 habilidades (uma do Piromante, uma do Arcanista; sugestão: Bola de Fogo e Orbes Arcanos), mostrados no jogo, e esperar a aprovação do designer antes das outras 8.
2. Um **ícone** para cada uma das 10 habilidades, no mesmo tamanho e estilo dos atuais.
3. Um **efeito visual** para cada habilidade, lido de longe:
   - Queimadura: um sinal de fogo no monstro enquanto durar;
   - Combustão: a explosão em cada monstro que queimava;
   - Rajada de Chamas: o cone de fogo;
   - Chão em Chamas: a área no chão;
   - Bola de Fogo: o projétil e a explosão;
   - Meteoro: o aviso no chão, o impacto e o chão em chamas do NH3;
   - Raio Arcano e Feixe do Vazio: o raio entre o Arcanista e o alvo enquanto canaliza, mais forte a cada pulso no Raio;
   - Marca Arcana: a marca no alvo com o número de pilhas legível;
   - Orbes Arcanos: os orbes girando em volta do Arcanista e os disparos até o alvo;
   - Sifão Arcano: o impacto no alvo e um sinal no Arcanista enquanto a Mana sobe mais rápido.
4. Conferir a escolha da segunda classe no Mago pela ferramenta do Esc (T-411): subir ao 20, ver as duas opções, escolher, desfazer e escolher a outra.
5. Toda peça nova tem origem e licença registradas em `CREDITS.md`.

## Critérios de aceite

- amostra aprovada pelo designer antes do resto;
- as 10 habilidades com ícone no painel de habilidades e na barra;
- cada habilidade mostra o efeito no jogo (capturas em `tasks/reports/interface/mago-efeitos/`, com uma prancha final como a da T-412);
- a escolha do Piromante e do Arcanista funciona na tela de habilidades;
- `./scripts/check.sh --fast` passa no Windows.

## Resultado

_(preencher ao concluir)_
