# T-405 — Servidor: ferramenta de teste para subir de nível

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** T-404 (sai da branch dela)
- **Libera:** T-411
- **Áreas que pode alterar:** `src/Game.Server/` (opção nova e tratamento das mensagens), `src/Game.Protocol/` (mensagens novas e `ProtocolVersion`), `src/Game.Core/Combat/Progression/` (só os métodos de apoio da ferramenta), `deploy/compose.yml` e `scripts/deploy-dev.sh` (só a variável da opção nova), `src/Game.Server/README.md`, `tests/`
- **Branch:** sai de `trilha/segundas-classes` e é integrada nela, não na `main` (seção 6f de `tasks/README.md`).

## Contexto

Design em `design/segunda-classe-v0.1.md`, seção 4.2. Ainda não há áreas do nível 16 ao 40, e o designer precisa testar as segundas classes. Decisão do designer: um **botão no menu do Esc** (cliente, T-411) que pede ao servidor para subir de nível e para desfazer a segunda classe. Hoje o único atalho de desenvolvimento é o `--dev-login` (`ServerOptions`, `MMO_DEV_LOGIN`, `GAME_DEV_LOGIN` no `deploy/compose.yml`).

A ferramenta **não pode ser ligada por engano** num servidor aberto a jogadores.

## Requisitos

1. **Opção nova do servidor**, `--dev-tools` (env `MMO_DEV_TOOLS`), **desligada por padrão**. No `deploy/compose.yml`, variável `GAME_DEV_TOOLS` com padrão `false`; o servidor de teste da VPS liga pelo `.env` ou pelo `deploy-dev.sh` (escolher o mais simples e documentar). Com a opção ligada, o servidor escreve um aviso no log ao subir, como já faz com o `--dev-login`.
2. **O cliente fica sabendo** se a ferramenta está ligada (por exemplo, um campo na resposta ao `Hello`), para só mostrar o botão quando ela existe.
3. **Mensagem nova do cliente** com o pedido, um de:
   - subir **+1 nível**; subir **+5 níveis**; ir ao **nível 20**; ir ao **nível 40**;
   - **desfazer a segunda classe**: tira a segunda classe e devolve os pontos de habilidade gastos nela e os +5 de atributo (se os +5 já estiverem distribuídos, refaz a distribuição de atributos inteira, como `ResetPoints`);
   - **redistribuir grátis**: o mesmo que `ResetPoints`, sem custo.
4. **Subir de nível pelo caminho normal:** dar a XP que falta pelo mesmo caminho da XP de monstro (`Leveling.GrantExperience`), para os pontos de atributo e de habilidade saírem iguais aos de quem subiu jogando. Nada acima do nível máximo.
5. **Recusa:** sem `--dev-tools`, o servidor recusa qualquer pedido da ferramenta e registra no log quem pediu. Em combate, recusa também (como `ResetPoints`).
6. O personagem é gravado depois de cada pedido aceito.
7. `ProtocolVersion` + 1 (em cima do da T-404). Documentar a opção em `src/Game.Server/README.md`.

## Integração

Sai da branch da T-404. **T-406, T-407, T-404, T-405 e T-411 entram juntas** na `trilha/segundas-classes`. O deploy com a ferramenta ligada só acontece no dia do playtest da trilha (seção 6f de `tasks/README.md`).

## Critérios de aceite

- **sem a opção, o servidor recusa** cada tipo de pedido (teste);
- com a opção: +1, +5, ir ao 20 e ir ao 40 dão o mesmo nível, os mesmos pontos de atributo e os mesmos pontos de habilidade que subir com XP de monstro (teste comparando os dois caminhos);
- desfazer a segunda classe devolve os pontos e permite escolher outra (teste);
- `./scripts/check.sh` completo passa.

## Resultado

Concluída em 2026-11-22, na VPS, branch `task/T-405-ferramenta-de-teste-servidor` (saída de `task/T-404-servidor-segunda-classe`, `ae917c8`). Não entra na trilha sozinha: vai na rodada do protocolo (seção 6f). Sem push e sem deploy.

### Formato das mensagens novas (para a T-411)

| O quê | Formato |
|---|---|
| Ferramenta ligada? | `Welcome` ganha `bool DevTools = false` **no fim** (depois de `InstanceId`). `true` só com o servidor em `--dev-tools`; vem em todo `Welcome` (também nos de troca de zona). O cliente só mostra o botão do menu do Esc com `DevTools == true`. |
| Pedido (cliente → servidor) | `DevToolRequest(DevToolAction Action)`, tag **26** da união `INetMessage`, canal confiável. `DevToolAction` (byte): `LevelUp1`, `LevelUp5`, `ToSecondClassLevel` (vai ao 20), `ToMaxLevel` (vai ao 40), `UndoSecondClass`, `FreeReset`. A entidade é sempre a da conexão. |
| Resposta (servidor → cliente) | `DevToolResult(DevToolAction Action, DevToolOutcome Outcome, int Level)`, tag **27**, sempre uma por pedido. `Level` = nível depois do pedido (0 sem progressão). `DevToolOutcome` (byte): `Done`, `Disabled` (servidor sem `--dev-tools`), `InCombat` (em combate ou conjurando), `Dead`, `NoProgression`, `AlreadyAtLevel` (já no nível pedido, acima dele ou no máximo), `NoSecondClass` (desfazer sem segunda classe). |
| O que muda no personagem | chega pelo caminho de sempre: `XpGained` (monstro `EntityId.None`, fonte `dev.tools`) e um `LevelUp` por nível; `PointsReset` no `FreeReset`; o `UndoSecondClass` não tem evento próprio: o `CharacterProgress` replicado chega com `SecondClass = null` e os pontos novos (e o `DevToolResult` com `Done`). |
| `ProtocolVersion` | **17 → 18** |

### O que foi feito

- **Opção:** `--dev-tools` / `MMO_DEV_TOOLS` (`ServerOptions.DevTools`, padrão `false`), no `--help`. Ligada, o `Program` escreve `Warning` ao subir ("Ferramenta de teste ativa: … Nunca num servidor aberto a jogadores"), como o `--dev-login`. `deploy/compose.yml`: `MMO_DEV_TOOLS: ${GAME_DEV_TOOLS:-false}`. Escolha para ligar num servidor do Compose: `GAME_DEV_TOOLS=true` no `deploy/.env` (o mais simples; não mexe no `deploy-dev.sh`).
- **Servidor:** `src/Game.Server/DevTools.cs` (`DevTools.Apply`) tem as regras, usadas pelo `WorldServer` e pela `ZoneRoom`. O pedido entra numa fila por conexão (até 8) e é aplicado no tick seguinte, antes das intenções; os eventos saem com esse tick. Sem `--dev-tools`: `Disabled` e `Warning` no log com nome, conexão, personagem (ou "sem conta") e ação. Com: recusa morto (`Dead`) e em combate ou conjurando (`InCombat`, a mesma regra da redistribuição, `Leveling.InCombat`); aceito, grava na hora (`SaveNow`, motivo "ferramenta de teste") quando há conta. Com `--dev-login` não há banco: só muda o personagem em memória.
- **Subir de nível:** `Leveling.ExperienceToReach` calcula a XP que falta para chegar exatamente ao nível pedido (limitado a `maxLevel`) e `Leveling.GrantExperience` dá essa XP, o mesmo método da XP de monstro. O personagem fica no começo do nível pedido (XP 0 no nível).
- **Desfazer a segunda classe:** `Leveling.UndoSecondClass` tira a segunda classe, os NH das habilidades dela (pontos de habilidade de volta, barra e passivas refeitas) e os `secondClassAttributePoints` (+5). Se os +5 já estavam distribuídos (pontos livres < 5), devolve **todos** os pontos de atributo, como o `ResetPoints`, mas só os atributos. Os NH da classe base ficam.
- **Redistribuir grátis:** chama o `Leveling.ResetPoints`, que no Core já é grátis (não há custo em ouro no código nem em `data/`; o "1ª vez grátis, depois ouro" do design ainda não existe). Com custo um dia, a ferramenta continua sem custo, porque não passa pelo caminho que cobraria.
- **Documentação:** seção "Ferramenta de teste (T-405)" em `src/Game.Server/README.md`, com o comando do playtest local (`dotnet run --project src/Game.Server -c Release -- --world --dev-login --dev-tools --port 7777`, cliente em `127.0.0.1:7777` pela entrada sem conta) e o aviso do log. Conferido rodando o servidor: sobe com protocolo v18 e os dois avisos.
- **Testes:**
  - `tests/Game.Server.Tests/DevToolServerTests.cs`: opção desligada por padrão (argumento e variável); **sem a opção cada tipo de pedido é recusado** (`Disabled`), nada muda e o log registra quem pediu, no mundo e na sala; **+1, +5, ir ao 20, +1 e ir ao 40** dão o mesmo nível, os mesmos pontos de atributo e de habilidade, a mesma XP para o próximo e a mesma vida máxima que um segundo personagem na mesma zona subindo com XP de monstros normais do próprio nível, um `LevelUp` por nível chega ao cliente, e nada passa do 40 (`AlreadyAtLevel`); +5 no 38 para no 40; **desfazer a segunda classe** com os +5 distribuídos devolve todos os pontos e deixa escolher o Berserker depois do Guardião (o cliente vê `SecondClass` null); redistribuição grátis; em combate cada pedido é recusado; com banco cada pedido aceito grava (nível 20 na linha, `secondClass` some depois de desfazer) e o recusado não grava; na sala, pedido aceito grava e personagem de encontro sem progressão dá `NoProgression`. Sem banco (`--dev-login`), os testes do mundo aberto mostram o pedido funcionando só em memória.
  - `tests/Game.Core.Tests/DevToolSupportTests.cs`: `ExperienceToReach` (limites e teto), a XP de uma vez contra a XP de monstro aos pedaços (1→2, 2→7, 1→20, 20→40, 1→40), desfazer com e sem os +5 distribuídos usando a segunda classe de teste com habilidades e passiva (NH e passiva saem, os NH da base ficam, o build continua válido), e desfazer sem segunda classe.
  - `tests/Game.Protocol.Tests/ProtocolTests.cs`: round trip das duas mensagens com as tags 26 e 27, `Welcome.DevTools` e versão 18.

### Desvios e decisões

- **Playtest local, sem deploy (mudança de plano do designer):** não mexi no `scripts/deploy-dev.sh` e não fiz deploy. `GAME_DEV_TOOLS` fica `false` no `compose.yml`.
- **`deploy/.env.example`** (fora das áreas listadas): uma linha comentada `# GAME_DEV_TOOLS=false`, junto das outras `GAME_*`, para documentar onde ligar.
- **Testes existentes tocados:** `WorldRig` ganhou um parâmetro opcional de log (para conferir o aviso de recusa); em `SecondClassTests` o conteúdo de teste e algumas constantes passaram de `private` a `internal`, para os testes novos do Core usarem a mesma segunda classe de teste.
- As ações "ir ao 20" e "ir ao 40" se chamam `ToSecondClassLevel` e `ToMaxLevel` e leem `progression.secondClassLevel` e `progression.maxLevel`: os números ficam em `data/`, não no código.
- A comparação com "XP de monstro" dá a XP em pedaços do tamanho que a morte de um monstro normal do próprio nível dá a quem lutou sozinho (`rules.Share(rules.MonsterXp(…), 1)`, a mesma conta do `ProgressionSystem.AwardKill`), pelo mesmo `GrantExperience`. Não mata monstros de verdade no teste.
- A ferramenta vale também na sala de encontro (`ZoneRoom`), porque lá também precisa recusar sem a opção. Na sala, só personagens com conta têm progressão.
- Desfazer a segunda classe não publica evento novo do Core (seria tag nova em `EventCodecs`): o cliente fica sabendo pelo `DevToolResult` e pelo `CharacterProgress` replicado.
- Os testes com PostgreSQL real (`PostgresAccountTests`) foram pulados, como sempre nesta VPS sem banco de teste. A gravação foi testada com o `FakeStore`.

### Pendências

- Rodada do protocolo (T-403, T-406, T-407, T-404, T-405, T-408, T-411): a T-405 usa 18 em cima do 17 da T-404; o número final e as tags 26/27 da união saem na integração, se outra tarefa da rodada também acrescentar mensagens.
- T-411: botão no menu do Esc, só com `Welcome.DevTools`; mandar `DevToolRequest` e mostrar o `DevToolResult` (texto para cada `DevToolOutcome`).
- Antes de abrir o jogo a jogadores de fora: conferir que nenhum servidor sobe com `--dev-tools` / `GAME_DEV_TOOLS=true`.
