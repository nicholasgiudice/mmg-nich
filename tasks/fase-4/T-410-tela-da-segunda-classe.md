# T-410 — Cliente: escolha da segunda classe e tela de habilidades

- **Ambiente:** Windows
- **Modelo:** Humano + Médio
- **Depende de:** T-404, T-408, T-409
- **Libera:** T-413
- **Áreas que pode alterar:** `client/scripts/Hud/` (`SkillPanel`, `SkillDetailText`, `SkillIcon`, painéis novos), `client/scripts/Authority/` (enviar a intenção nova), `client/scenes/`, `client/assets/art/` (só peças do kit de interface), `tests/Game.Client.Tests/`, `tasks/reports/interface/`

## Contexto

Design em `design/segunda-classe-v0.1.md`, seções 1 e 4. No nível 20, o jogador escolhe uma de duas segundas classes, e a escolha é **definitiva**. Decisão da T-403: a escolha é feita pela **tela de habilidades**, fora de combate (o mestre na Cidade fica para depois).

O servidor já aceita a escolha (`NetIntentKind.ChooseSecondClass`, T-404) e replica a segunda classe do personagem. As habilidades do Guardião e do Berserker estão em `data/` (T-408). A barra tem 10 teclas (T-409).

A interface usa o **kit de pedra e ouro** (aprovado; `tasks/trilha-interface/`) e cada tela nova aparece **primeiro como prévia no jogo** para o designer aprovar.

## Requisitos

1. **Tela de habilidades** (`SkillPanel`) com duas partes: **classe base** e **segunda classe**. Antes do nível 20, a segunda parte mostra que libera no 20. Depois da escolha, mostra as 5 habilidades da segunda classe com os NH e o nível exigido de cada NH (21, 24, 27, 30, 33; assinatura 35, 37, 40).
2. **Passivas** com moldura diferente das ativas e o texto "Passiva". Não podem ser arrastadas para a barra.
3. **Escolha:** no nível 20, sem segunda classe, a tela mostra as duas opções lado a lado: nome, papel (do design) e as 5 habilidades com o resumo do NH1. Escolher abre uma confirmação que diz claramente que **a escolha não pode ser desfeita**. Confirmar envia a intenção; a recusa do servidor (em combate, por exemplo) aparece como mensagem.
4. Ao escolher, as habilidades da segunda classe aprendidas podem ir para a barra (espaços 6 a 0 vazios), seguindo o que a barra já faz para a classe base.
5. O detalhe da habilidade (`SkillDetailText`) mostra todos os NH das habilidades novas, como já faz com as da base.
6. **Prévia no jogo:** capturas em `tasks/reports/interface/segunda-classe/` da tela antes do 20, da escolha, da confirmação e da tela depois da escolha, para o designer aprovar.

## Integração

Não sobe `ProtocolVersion`. Pode entrar na `main` depois da T-404, da T-405 e da T-411, com um cliente novo publicado.

## Critérios de aceite

- personagem de nível 20 escolhe o Guardião pela tela, a barra ganha as habilidades aprendidas, e a tela mostra a segunda classe depois de sair e voltar (servidor de teste, com a ferramenta da T-405 para chegar ao 20);
- a confirmação aparece antes de enviar; cancelar não envia nada;
- passivas não vão para a barra (teste de cliente ou captura);
- capturas aprovadas pelo designer;
- `./scripts/check.sh --fast` passa no Windows.

## Resultado

_(preencher ao concluir)_
