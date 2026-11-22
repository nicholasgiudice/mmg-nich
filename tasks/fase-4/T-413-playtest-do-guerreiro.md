# T-413 — Playtest do Guerreiro com segunda classe

- **Ambiente:** Windows
- **Modelo:** Humano
- **Depende de:** T-410, T-411, T-412
- **Libera:** detalhar as outras três classes (seção 7 de `tasks/README.md`)
- **Áreas que pode alterar:** `tasks/reports/`
- **Branch:** sai de `trilha/segundas-classes` e é integrada nela, não na `main` (seção 6f de `tasks/README.md`).

## Contexto

Fecha o bloco do Guerreiro da fase 4 (`design/segunda-classe-v0.1.md`). O objetivo é validar o **sistema** de segunda classe antes de fazer as outras seis: escolha definitiva, pontos e NH até o 40, passivas, barra de 10 teclas, e o Guardião e o Berserker em jogo.

Não há áreas acima do nível 15. O teste usa a ferramenta do menu do Esc (T-411) para subir de nível, contra os monstros que existem (Ruínas, Cripta e o Vigia), que ficam fáceis para um personagem alto: o que se avalia é a classe, não o desafio.

**O teste é local, no Windows, sem deploy na VPS** (decisão do designer, 2026-11-22; seção 6f de `tasks/README.md`): servidor com `dotnet run --project src/Game.Server -c Release -- --world --dev-login --dev-tools --port 7777` a partir de um checkout da trilha, e o cliente da trilha pela tela de entrada sem conta, em `127.0.0.1:7777`. Sem banco, o personagem começa no nível 1 e não é gravado: cada sessão de teste recomeça pela ferramenta do Esc.

## Requisitos

1. Personagem Guerreiro novo: subir ao 20 com a ferramenta, distribuir os pontos, escolher o **Guardião**; testar cada habilidade nova; subir ao 40 e testar os NH altos e a assinatura.
2. Desfazer a segunda classe com a ferramenta e repetir com o **Berserker**.
3. Em grupo (2 a 4 personagens: várias janelas do cliente no mesmo PC, ou pessoas na rede local pelo IP do PC), na Cripta: o Guardião protegendo o grupo (Interceptação, Bastião, Juramento).
4. Anotar: o que a segunda classe mudou no jeito de jogar; se cada habilidade faz o que a descrição diz; se a passiva fica clara; se a barra de 10 teclas é confortável; o que irritou e os problemas.

## Critérios de aceite

- `tasks/reports/playtest-v0.4.md` com as respostas, os problemas e a decisão: seguir para as outras classes ou corrigir antes. Registrar também se as segundas classes que ainda esperam aprovação (Atirador de Elite, Caçador, Piromante, Arcanista, Inquisidor) mudam por causa do que se aprendeu.

## Resultado

_(preencher ao concluir)_
