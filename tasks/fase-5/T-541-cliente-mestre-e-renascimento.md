# T-541 — Cliente: mestre das classes e tela de morte com o destino

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** T-501 (para o destino na tela de morte); o mestre não depende de nada
- **Libera:** T-550
- **Áreas que pode alterar:** `client/scripts/Main.cs` (`TalkTo`), `client/scripts/Hud/` (tela de habilidades e da segunda classe), `client/scripts/Match/DeathScreen.cs`, `tasks/reports/`

## Contexto

**Mestre das classes.** O design da segunda classe previa escolher falando com um mestre na Cidade. A T-403 deixou a escolha na tela de habilidades, em qualquer lugar fora de combate, "até os mapas serem refeitos". O designer decidiu manter as duas formas (`design/mapas-16-40-v0.1.md` §8, decisão 5): a tela continua, e o mestre é um atalho com cara de jogo. O Core e o servidor não mudam: a escolha continua validada como hoje (nível 20, fora de combate, definitiva).

Hoje, clicar num NPC sem loja só mostra "não tenho nada para vender" (`Main.TalkTo`, T-323). A Cidade já tem um NPC sem função no Salão de Treinamento (`npc.trainer`). O Forte do Passo terá o `npc.class_master` (T-531).

**Tela de morte.** O botão diz "Retornar à Cidade" (T-397). Com a T-501, quem morre na Serra ou nas Minas renasce no Forte do Passo.

## Requisitos

1. Clicar no `npc.trainer` (Cidade) ou no `npc.class_master` (Forte), ao alcance:
   - nível 20 ou mais, sem segunda classe: abre a tela de habilidades já na escolha da segunda classe;
   - abaixo do 20: o mestre diz, sobre o personagem, que volte no nível 20;
   - já com segunda classe: abre a tela de habilidades.
   Longe, "Chegue mais perto", como nas lojas.
2. A lista de NPCs que são mestres fica num lugar só, no cliente (sem `if` espalhado pelo código).
3. Tela de morte: o botão mostra o destino real, "Retornar à Cidade" ou "Retornar ao Forte do Passo", lido do `respawn` da zona em que o personagem morreu (T-501), nos dados que o cliente já tem.
4. O designer aprova no jogo.

## Critérios de aceite

- no jogo (servidor local com `--dev-tools`): personagem no 19 fala com o mestre e recebe o aviso; sobe para o 20 pelo Esc, fala de novo e a tela abre na escolha; escolhe e vê a tela de habilidades;
- morrer na Cidade ou no Brejo mostra "Retornar à Cidade"; na Serra, "Retornar ao Forte do Passo" (depois da T-532; antes, conferir com uma zona de teste);
- `./scripts/check.sh` completo passa.

## Resultado

**Estado: parcial.** Requisitos 1 e 2 (mestre das classes) prontos e verificados no jogo. **Pendente: requisito 3 (tela de morte com o destino)**, que depende do campo `respawn` por zona da T-501 (ainda em outra sessão); fazer depois de rebasear na T-501. O critério "morrer na Cidade/Brejo/Serra" e a aprovação do designer (requisito 4) também ficam para essa segunda parte.

### O que foi feito (mestre)

- `client/scripts/Hud/ClassMasters.cs` (novo): a lista de NPCs mestres num lugar só (`npc.trainer` da Cidade e `npc.class_master` do Forte, T-531, que ainda não está no mapa) e a decisão pura `Talk(nível, nívelDaSegundaClasse, temSegundaClasse)` → `TooLow` / `ChooseSecondClass` / `OpenSkills`, mais a fala do mestre. O nível da segunda classe vem de `Rules.Progression.SecondClassLevel`, não é fixo no código.
- `Main.TalkTo`: NPC mestre (mesmo sem loja) segue o caminho da loja: longe, "Chegue mais perto" (mesmo alcance `Shops.InteractRange`); perto, `TalkToClassMaster`. Abaixo do 20, a fala sai sobre o personagem ("Instrutor: volte no nível 20, quando a segunda classe libera."); no 20+ sem segunda classe, abre a tela de habilidades já na aba da segunda classe (a das duas opções); com segunda classe, abre a tela de habilidades na aba da classe base. NPC que não é mestre nem tem loja continua com "não tenho nada para vender".
- `Hud.OpenSkills(bool)` e `SkillPanel.OpenOnTab(bool)`: mudança pequena no Hud (3 linhas), para a T-540 não conflitar.
- Core e servidor intocados: a escolha continua validada como na T-403/T-410 (nível 20, fora de combate, definitiva).
- Roteiro de teste (`--demo-input`): dois verbos novos, `talk:<id do npc>` (clica no NPC mais perto com esse id, tenha loja ou não; generaliza o `shop` da T-323) e `devtool:<DevToolAction>` (o pedido do menu Esc, ex.: `devtool:ToSecondClassLevel`).
- Testes: `tests/Game.Client.Tests/ClassMastersTests.cs` (quem é mestre; as três decisões nos níveis 1, 19, 20, 35; texto). `ClassMasters.cs` entra no projeto de testes por link, como os outros arquivos do cliente.

### Testado no jogo (servidor local `--world --dev-login --dev-tools`, cliente com `--server=127.0.0.1:...`)

Capturas em `tasks/reports/T-541/`:

- `mestre-a-nivel-1.png`: nível 1 perto do Instrutor → a fala "volte no nível 20" sobre o personagem (esmaecendo). Não deu para testar o nível 19 exato: o nível 1 exercita o mesmo ramo, e o 19 vs 20 está nos testes de unidade.
- `mestre-b-nivel-20.png`: nível 20 (pela ferramenta de teste) → a tela de habilidades abre direto na aba "Segunda classe (escolher!)" com Guardião e Berserker.
- `mestre-c-ja-escolheu.png`: escolhe Guardião e confirma (cliques), fala de novo → tela de habilidades na aba "Classe base" (a escolha foi aceita: "Segunda classe: Guardião!").
- `mestre-d-longe.png`: nível 20, longe do Instrutor → "Chegue mais perto", tela não abre.

O mestre do Forte não foi testado no jogo (o NPC ainda não existe no mapa; entra na T-531); usa a mesma lista e o mesmo caminho do Instrutor. Neste worktree os sprites não foram importados, então os NPCs aparecem como círculos.

### Verificação

`dotnet format` limpo, `dotnet build -warnaserror` sem avisos, e `./scripts/check.sh` completo: todos os projetos passam, menos `Game.Design.Tests` com **4 falhas em 304** (43 min, máquina disputada por outras sessões). São as mesmas 4 já registradas na T-500, que existiam na `main` antes desta tarefa (Vigia sem o Clérigo, Cripta 71%, Guardião 10% e 6%); esta tarefa só mexe no cliente e não toca o Core.

### Para o designer aprovar

Servidor: `dotnet run --project src/Game.Server -- --world --dev-login --dev-tools --port 7791`. Cliente: `Godot_v4.7.2-stable_mono_win64.exe --path client res://scenes/Main.tscn -- --server=127.0.0.1:7791 --name=teste --class=class.warrior`. Andar até o Instrutor (Salão de Treinamento, noroeste do spawn da Cidade) e clicar nele: fala de "volte no nível 20"; Esc > ferramenta de teste > "Ir ao nível 20"; clicar de novo → escolha; escolher e clicar de novo → habilidades. Longe: "Chegue mais perto".

### Desvios e decisões

- A fala do mestre abaixo do 20 usa o nome do NPC ("Instrutor: ..."), no estilo da fala atual "não tenho nada para vender".
- Com segunda classe já escolhida, o mestre abre a tela na aba da classe base (a da segunda classe continua a um clique).

### Pendente

- Requisito 3 (tela de morte lê `respawn` da zona, T-501): `client/scripts/Match/DeathScreen.cs` e `Main` (`UpdateDeath`), depois da T-501.
- Requisito 4: aprovação do designer.
