# T-445 — Playtest do Mago com segunda classe

- **Ambiente:** Windows
- **Modelo:** Humano
- **Depende de:** T-444
- **Libera:** fechamento da fase 4 (seção 7 de `tasks/README.md`)
- **Áreas que pode alterar:** `tasks/reports/`
- **Branch:** sai da `main` e é integrada na `main` (seção 6i de `tasks/README.md`).

## Contexto

Fecha o bloco do Mago da fase 4 (`design/segunda-classe-v0.1.md`, seção 7). É a última das quatro classes. Se os playtests das outras (T-413, T-425, T-435) ainda estiverem abertos, este pode ser o playtest da fase inteira, com as oito segundas classes e um relatório só.

O teste é local, no Windows, como o da T-413: servidor com `dotnet run --project src/Game.Server -c Release -- --world --dev-login --dev-tools --port 7777` a partir da `main`, e o cliente pela tela de entrada sem conta, em `127.0.0.1:7777`. A ferramenta do menu do Esc (T-411) sobe de nível e desfaz a segunda classe.

## Requisitos

1. Personagem Mago novo: subir ao 20 com a ferramenta, distribuir os pontos, escolher o **Piromante**; testar cada habilidade nova contra grupos; subir ao 40 e testar os NH altos e o Meteoro.
2. Desfazer a segunda classe com a ferramenta e repetir com o **Arcanista**, contra um alvo forte (o Vigia).
3. Em grupo (2 a 4 personagens): o Piromante limpando grupos da Cripta; o Arcanista no Vigia.
4. Anotar: o que a segunda classe mudou no jeito de jogar; se cada habilidade faz o que a descrição diz; se a Mana do Piromante dá conta; se o Arcanista fica tempo demais parado canalizando; se o Sifão se nota; o que irritou e os problemas.

## Critérios de aceite

- `tasks/reports/playtest-v0.6.md` (ou o próximo número livre) com as respostas, os problemas e a decisão: fechar a fase 4 ou corrigir antes.

## Resultado

_(preencher ao concluir)_
