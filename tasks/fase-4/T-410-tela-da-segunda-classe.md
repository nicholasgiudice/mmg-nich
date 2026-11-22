# T-410 — Cliente: escolha da segunda classe e tela de habilidades

- **Ambiente:** Windows
- **Modelo:** Humano + Médio
- **Depende de:** T-404, T-408, T-409
- **Libera:** T-413
- **Áreas que pode alterar:** `client/scripts/Hud/` (`SkillPanel`, `SkillDetailText`, `SkillIcon`, painéis novos), `client/scripts/Authority/` (enviar a intenção nova), `client/scenes/`, `client/assets/art/` (só peças do kit de interface), `tests/Game.Client.Tests/`, `tasks/reports/interface/`
- **Branch:** sai da `main` e é integrada na `main` (a fase 4 entrou na `main` em 2026-11-22; seção 6f de `tasks/README.md`).

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

Não sobe `ProtocolVersion`. Sai da `main` (com a T-404, a T-405 e a T-408) e pode entrar depois da T-411.

## Critérios de aceite

- personagem de nível 20 escolhe o Guardião pela tela, a barra ganha as habilidades aprendidas e a tela passa a mostrar a segunda classe (servidor local com `--dev-tools`, seção 6f de `tasks/README.md`, com a ferramenta da T-405 para chegar ao 20). O servidor local não grava o personagem; manter a segunda classe depois de sair e voltar é coberto pelos testes de servidor da T-404;
- a confirmação aparece antes de enviar; cancelar não envia nada;
- passivas não vão para a barra (teste de cliente ou captura);
- capturas aprovadas pelo designer;
- `./scripts/check.sh --fast` passa no Windows.

## Resultado

**Feito** (Windows, 2026-11-22; **aprovado pelo designer**), na branch `task/T-410-tela-da-segunda-classe`, saída de `task/T-411-botao-de-teste-no-esc` (a T-411 entra antes; as duas mexem na tela e no `Main`/`Hud`). Não sobe `ProtocolVersion`.

- **Duas abas na tela de habilidades:** "Classe base" e "Segunda classe" (as duas partes juntas passavam da altura da tela em 1080). A aba da segunda classe:
  - antes do nível 20: "A segunda classe libera no nível 20. Opções: Guardião ou Berserker.";
  - no 20, sem escolha: as duas opções lado a lado, com nome, papel (do design) e as 5 habilidades com ícone e o efeito do NH1 (passiva e assinatura marcadas); a aba avisa "Segunda classe (escolher!)";
  - "Escolher X" abre a confirmação: "A escolha é definitiva: não pode ser desfeita nem trocada depois." (Escolher / Cancelar). Confirmar manda `ChooseSecondClassIntent`; cancelar não manda nada;
  - depois da escolha: a aba vira "Segunda classe: X" e mostra a grade das 5 habilidades como a da base, com os NH e o nível de cada NH (21, 24, 27, 30, 33; assinatura 35, 37, 40), o + e o "?" do detalhe.
- **Escolha aceita:** aviso "Segunda classe: X! +5 pontos de atributo (C)" e "Agora você é X." na tela. **Recusa:** frase própria para a segunda classe (em combate, nível baixo, já escolhida, não é desta classe), em `SecondClassText`.
- **Passivas:** moldura roxa e o texto "Passiva" na célula; não vão para a barra (o ícone não arrasta). O NH da passiva conta nos pontos (fica em `AbilityState.Ranks`, não em `Known`).
- **Barra:** uma habilidade ativa da segunda classe, quando é aprendida, vai sozinha para o primeiro espaço vazio de 6 a 0 (`HotbarLayout.PlaceInFirstEmpty`). Só na hora em que é aprendida: se o jogador tirar da barra, ela não volta.
- **Detalhe:** o "?" das habilidades novas mostra todos os NH com o nível da segunda classe, como nas da base.
- **Quadro do personagem:** o status de uma passiva (que não acaba) aparece só com o nome, sem o número gigante ("Sede de Sangue 461168601842739000,0s" da T-412).
- **Testes** (`tests/Game.Client.Tests/SecondClassPanelTests.cs`, 7 novos; 118 no total): o plano com a segunda classe dá o mesmo motivo de recusa que o Core em cada habilidade, nos níveis 20 a 40, para Guardião e Berserker (com a passiva); os níveis 21/24/27/30/33 e 35/37/40; sem segunda classe, o plano é só a base; textos da confirmação e das recusas; toda segunda classe dos dados tem papel; a barra recebe a habilidade no primeiro espaço vazio de 6 a 0.
- **Prévia no jogo** (servidor local `--world --dev-login --dev-tools`, níveis pela ferramenta da T-411), em `tasks/reports/interface/segunda-classe/`: `1-antes-do-20.png`, `2-escolha.png`, `3-confirmacao.png`, `4-depois-guardiao.png` (nível 21, Guardião), `5-berserker-passiva-e-barra.png` (Investida aprendida e já na tecla 6; Sede de Sangue com moldura de passiva; status da passiva sem tempo no quadro) e `6-detalhe.png` (detalhe da Interceptação, NH I a V nos níveis 21 a 33).

**Desvios (fora das áreas listadas, o mínimo)**
- `client/scripts/Match/SkillPlan.cs`: o plano conhece a segunda classe (`FromState(..., secondClassId)`, `SecondClass`, `SecondAbilities`, `IsSecond`, nível exigido da segunda classe, NH da passiva).
- `client/scripts/Input/HotbarLayout.cs`: `PlaceInFirstEmpty`.
- `client/scripts/Hud/SecondClassText.cs` (novo, sem Godot, para os testes). O papel de cada segunda classe está no cliente (texto de apresentação, tirado do design), porque `classes.json` não tem descrição; as 6 que ainda não existem nos dados usam ids provisórios.

**Pendências**
- ~~Capturas aprovadas pelo designer.~~ Aprovadas em 2026-11-22.
- O texto das habilidades (`AbilityText`) não mostra a fração do desvio da Interceptação nem os números das passivas: o detalhe da Interceptação diz "Sem mudança nos números" no NH II (30% → 35%). Fica para uma tarefa de textos (vale também para Bastião, Fúria e Sede de Sangue).
- A seleção de personagem ainda não mostra a segunda classe (`secondClass` da API, T-404).
