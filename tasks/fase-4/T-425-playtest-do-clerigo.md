# T-425 — Playtest do Clérigo com segunda classe

- **Ambiente:** Windows
- **Modelo:** Humano
- **Depende de:** T-424
- **Libera:** —
- **Áreas que pode alterar:** `tasks/reports/`
- **Branch:** sai da `main` e é integrada na `main` (seção 6g de `tasks/README.md`).

## Contexto

Fecha o bloco do Clérigo da fase 4 (`design/segunda-classe-v0.1.md`, seção 8). O playtest do Guerreiro (T-413) foi deixado para depois pelo designer em 2026-11-22. Se ele ainda não tiver sido feito, este playtest pode cobrir também o Guardião e o Berserker, com um relatório só.

O teste é local, no Windows, como o da T-413: servidor com `dotnet run --project src/Game.Server -c Release -- --world --dev-login --dev-tools --port 7777` a partir da `main`, e o cliente pela tela de entrada sem conta, em `127.0.0.1:7777`. A ferramenta do menu do Esc (T-411) sobe de nível e desfaz a segunda classe.

## Requisitos

1. Personagem Clérigo novo: subir ao 20 com a ferramenta, distribuir os pontos, escolher o **Sacerdote**; testar cada habilidade nova; subir ao 40 e testar os NH altos e o Santuário.
2. Desfazer a segunda classe com a ferramenta e repetir com o **Inquisidor**, de preferência na Cripta (mortos-vivos).
3. Em grupo (2 a 4 personagens: várias janelas do cliente no mesmo PC, ou pessoas na rede local pelo IP do PC): o Sacerdote com Bênção, Graça Veloz e Inspiração num grupo com Guerreiro; o Inquisidor com o Selo da Luz e a Consagração.
4. Anotar: o que a segunda classe mudou no jeito de jogar; se cada habilidade faz o que a descrição diz; se a Inspiração se nota na Mana dos aliados; se o Inquisidor fica diferente o bastante do Sacerdote; o que irritou e os problemas.

## Critérios de aceite

- `tasks/reports/playtest-v0.4.md` (ou o próximo número livre) com as respostas, os problemas e a decisão: seguir para o Arqueiro e o Mago ou corrigir antes.

## Resultado

_(preencher ao concluir)_
