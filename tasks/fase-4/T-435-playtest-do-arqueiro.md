# T-435 — Playtest do Arqueiro com segunda classe

- **Ambiente:** Windows
- **Modelo:** Humano
- **Depende de:** T-434
- **Libera:** —
- **Áreas que pode alterar:** `tasks/reports/`
- **Branch:** sai da `main` e é integrada na `main` (seção 6h de `tasks/README.md`).

## Contexto

Fecha o bloco do Arqueiro da fase 4 (`design/segunda-classe-v0.1.md`, seção 6). Pode ser feito junto com o playtest do Clérigo (T-425) e do Guerreiro (T-413), se o designer preferir um relatório só.

O teste é local, no Windows, como o da T-413: servidor com `dotnet run --project src/Game.Server -c Release -- --world --dev-login --dev-tools --port 7777` a partir da `main`, e o cliente pela tela de entrada sem conta, em `127.0.0.1:7777`. A ferramenta do menu do Esc (T-411) sobe de nível e desfaz a segunda classe.

## Requisitos

1. Personagem Arqueiro novo: subir ao 20 com a ferramenta, distribuir os pontos, escolher o **Atirador de Elite**; testar cada habilidade nova; subir ao 40 e testar os NH altos e o Olho de Falcão com o Tiro Certeiro.
2. Desfazer a segunda classe com a ferramenta e repetir com o **Caçador**, contra grupos de monstros.
3. Em grupo (2 a 4 personagens), contra o Vigia ou um grupo da Cripta: o Atirador de longe; o Caçador espalhando o veneno.
4. Anotar: o que a segunda classe mudou no jeito de jogar; se cada habilidade faz o que a descrição diz; se as passivas ficam claras (a Mira Firme pela distância, as pilhas da Flecha Envenenada); se o Tiro Certeiro sem conjuração e o Olho de Falcão de 20 s ficaram fortes demais; se a Ferida Aberta e a Praga se notam; o que irritou e os problemas.

## Critérios de aceite

- `tasks/reports/playtest-v0.5.md` (ou o próximo número livre) com as respostas, os problemas e a decisão: seguir para o Mago ou corrigir antes.

## Resultado

_(preencher ao concluir)_
