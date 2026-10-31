# T-221 — Cliente: tela de personagem (atributos) e de habilidades (pontos e NH)

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** T-203, T-212
- **Libera:** T-230
- **Áreas que pode alterar:** `client/`

## Contexto

`design/progression-v0.1.md` §3 e §7: pontos livres com o atributo sugerido e prévia do efeito de cada ponto ("+1 FOR: Golpe Pesado 51 → 53"); pontos de habilidade com prévia do próximo NH. O `SkillPlan` da T-064 já tem `PointBudget` e `Enforce` pensados para isso.

## Requisitos

1. Nível, XP e barra de XP no HUD; aviso de "subiu de nível" com pontos disponíveis.
2. Tela de personagem: atributos, estatísticas derivadas (vida, Mana, crítico, velocidade de ataque, armadura, resistência), botão + por atributo com prévia, atributo sugerido destacado.
3. Tela de habilidades: o painel da T-064 ligado aos pontos de verdade (`Enforce`), requisitos de nível por NH, prévia do próximo NH; manda as intenções da T-203.
4. Redistribuir (só fora de combate, T-203).

## Critérios de aceite

- gastar pontos online e ver o efeito na barra e nos números;
- `dotnet build client/` e `./scripts/check.sh` passam.

## Resultado

**Feito** (Windows, 2026-10-29). `dotnet build client/` e `./scripts/check.sh` completo passam.

**HUD** (`client/scripts/Hud/Hud.cs`)
- Barra de XP no quadro do jogador: "Nível N · XP x / y" ou "Nível 15 (máximo)". Sem `CharacterProgress` (encontros de teste), só o nível.
- Botões "Personagem (C)" e "Habilidades (K)" abaixo do quadro. Com pontos livres, ficam verdes e mostram a quantidade ("● 5").
- `LevelUp` mostra o aviso grande "Nível N! 5 pontos de atributo (C) · 2 de habilidade (K)".
- `ProgressionRejected` mostra o motivo em português no rodapé das telas, ou no aviso grande se estiverem fechadas.
- Tooltips da barra e o texto das habilidades usam os valores com as estatísticas do personagem (`AbilityText.Describe(..., stats)`: base + Σ coeficiente × estatística, sem crítico).
- Log: filtro "Prog." e cor própria para `LogCategory.Progressao` (pendência da T-203).

**Tela de personagem** (`CharacterPanel`, tecla C)
- Os cinco atributos com o valor (pontos distribuídos entre parênteses), o botão + (`SpendAttributePointIntent`) e a prévia do próximo ponto.
- A prévia lista as duas habilidades da barra que mais mudam ("+1: Disparo Potente 46 → 48 · Disparo 20 → 21") e as derivadas que mudam (vida, Mana e Mana/s, crítico, intervalo do ataque básico). As contas são as do Core (`StatMath`).
- Atributo sugerido com ★ e em destaque.
- Estatísticas: vida e Mana máximas, Mana/s, crítico (e multiplicador), velocidade de ataque e intervalo do ataque básico, dano da arma, armadura e resistência (com a redução contra um atacante do mesmo nível).

**Tela de habilidades** (`SkillPanel`, tecla K)
- É o `SkillPlan` da T-064 ligado aos pontos de verdade. `SkillPlan.FromState` monta o plano com os NH do servidor, `Enforce` ligado, `PointBudget` = pontos do nível, requisito de nível por NH e NH0 permitido.
- Por habilidade: NH atual/máximo, botão + (`RaiseSkillRankIntent`), "NH n exige nível X" (em laranja se ainda não dá), o texto do NH atual e a prévia do próximo NH com as estatísticas.
- O botão + só fica ativo quando o Core aceitaria. `SkillPlan.RaiseBlocker` checa na mesma ordem do `Leveling.RaiseSkillRank`, e a dica do botão diz o motivo.

**Redistribuir:** botão nas duas telas, com confirmação no segundo clique (`ResetPointsIntent`). Em combate, o servidor recusa e a tela mostra "Só dá para redistribuir fora de combate".

**Outros ajustes**
- `PlayerInput(ContentCatalog)`: a entrada resolve cada habilidade no NH que o estado do jogador tem. Online, antes ela usava sempre o NH1; agora o NH muda ao gastar pontos. No local continua igual, porque o estado vem do plano do menu.
- Esc fecha as telas antes de pausar.
- `--demo-input` aceita `spend:<atributo>`, `raise:<habilidade>` e `reset_points` (o mesmo que os botões) e imprime XP, nível e eventos de pontos.

**Testes:** `tests/Game.Client.Tests/SkillPlanTests.cs` compila o `SkillPlan.cs` do cliente por link, como o `MovementPredictor`. Ele confere, nas 4 classes e nos níveis 1–15, que o `RaiseBlocker` dá a mesma resposta que o `Leveling.RaiseSkillRank` do Core, subindo os pontos até acabarem. Também confere o personagem novo: tudo no NH0, assinatura exige nível e o ataque básico não recebe pontos. **Fora das áreas da tarefa:** o `.csproj` desses testes ganhou a referência ao `Game.Data` e o link.

**Testado online (VPS 31.97.162.124:7777, protocolo v7, modo mundo, login só com nome, na Cidade)**
- Guerreiro nível 1: as duas telas abertas, ponto gasto em Golpe Pesado e GP I na barra. O segundo ponto foi recusado com "Sem pontos de habilidade livres" e `spend:str` com "Sem pontos de atributo livres".
- Mago: aprende Explosão Elemental, redistribui fora de combate (`PointsReset`, ponto devolvido), aprende Prisão de Gelo, e a barra troca para PG I.
- Barra de XP, botões, prévias e estatísticas conferidos nas capturas.

**Testado só localmente** (`dotnet run --project src/Game.Server -- --world --dev-login --start-zone zone.vigil_fields`)
- Subir de nível online leva vários minutos de caça. Para testar, `data/rules.json` (`xpCurveBase` 3) e `data/zones.json` (um Rato só, perto do nascimento) foram alterados **temporariamente** e revertidos antes do commit.
- Arqueiro: matou o Rato, subiu para o nível 2 (aviso e barra de XP), aprendeu Disparo Potente e Marca do Caçador, gastou 3 em DES, 1 em VIT e 1 em AGI. DES 15 (+3), vida 149 e prévias certas.
- Redistribuir em combate foi recusado (`InCombat`) com a mensagem na tela.

**Decisões**
- **Atributo sugerido derivado dos dados:** é o atributo com a maior soma de coeficientes `scaling` no ataque básico e nas habilidades da classe (Guerreiro FOR, Arqueiro DES, Mago INT, Clérigo INT). A tabela do design também sugere VIT/AGI como segundo atributo, mas isso não sai dos dados e a tarefa só pode alterar `client/`. Para mostrar os dois, falta um campo `suggestedAttributes` em `classes.json`, numa tarefa de dados.
- A prévia mostra o valor sem crítico, sem bônus contra tags e sem o multiplicador por acerto (ex.: 85% por flecha), como valor de referência.
- A tela de habilidades não muda nada localmente: o + manda a intenção e o painel se refaz com o snapshot seguinte. A autoridade continua decidindo.
- Mundo aberto: `Main` não cria o `MatchTracker` quando o `EncounterId` do `Welcome` não é um encontro (é uma zona). Antes, a Cidade mostrava "Vitória" logo ao entrar e cobria as telas. O resto da troca de zona continua com a T-223.

**Pendências**
- Abrir o Godot no editor: os `.cs.uid` das 4 classes novas não foram gerados (não estão em cenas, e o build via `dotnet` não precisou).
- Segundo atributo sugerido (acima).
- Custo de redistribuir e "só na cidade" (§10) ficam para quando houver economia (já era pendência da T-203).

