# Game.Sim — simulador headless de encontros (T-015)

Roda encontro × estratégia × N seeds sem abrir o jogo e mede vitória, tempo, dano, cura, recurso, uso de habilidades, mortes e trocas de fase. Cada execução é um mundo independente (`EncounterSetup.Create`) controlado por bots de `src/Game.Bots`; execuções rodam em paralelo (`Parallel.For`), e o resultado não depende do paralelismo.

## Exemplos

```bash
# conteúdo real (data/ por padrão)
dotnet run --project tests/Game.Sim -- --encounter encounter.warrior_wolves --strategy rotation --runs 200

# comparar estratégias e salvar JSON
dotnet run --project tests/Game.Sim -- --encounter encounter.warrior_wolves \
  --strategy rotation,basic_only,taunt_and_basic --runs 500 --out /tmp/wolves.json

# grupo com papéis por classe, e "parado" vs "se move"
dotnet run --project tests/Game.Sim -- --encounter encounter.matriarch \
  --strategy rotation,rotation+dodge --role class.cleric=heal_only --runs 200

# fixtures do simulador
dotnet run --project tests/Game.Sim -- --data tests/Game.Sim/Fixtures --strategy rotation,kite --runs 1000
dotnet run --project tests/Game.Sim -- --list

# níveis de habilidade (T-063): perfil pronto e/ou uma habilidade
dotnet run --project tests/Game.Sim -- --encounter encounter.four_rats --ranks espalhado
dotnet run --project tests/Game.Sim -- --encounter encounter.four_rats --rank class.mage:mage.elemental_blast=5

# T-206: perfil de personagem por nível contra monstros do nível da área (rules.monsters)
dotnet run --project tests/Game.Sim -- --encounter encounter.four_rats --level 9 --monster-level 9
dotnet run --project tests/Game.Sim -- --encounter encounter.forest_matriarch --strategy kite+dodge --level 8 --gear incomum --monster-level 8
```

Opções: `--data <dir>`, `--encounter <ids|all>`, `--strategy <a,b>`, `--role <classe>=<estratégia>` (repetível, `*` = todas), `--opt <chave>=<valor>` (repetível), `--ranks <nh1|focado|espalhado>`, `--rank <classe>:<habilidade>=<NH>` (repetível, por cima do perfil), perfil de personagem (T-206, ignora `--ranks`): `--level <n>`, `--attrs <sugerido|espalhado>` (espalhado = metade no sugerido, metade nos outros), `--gear <comum|incomum|raro>`, `--gear-level <n>`, `--skills <focado|espalhado>`, `--focus <classe>=<h1>,<h2>`; `--monster-level <n>` (nível de todos os monstros), `--runs`, `--seed` (execução *i* usa seed+*i*), `--jitter <tiles>` (variação das posições iniciais por seed; padrão 0,5), `--max-seconds` (usado se o encontro não tiver `defeat.timeLimit`), `--parallel`, `--out <arquivo.json>`, `--per-run`, `--list`. Código de saída 2 em erro de uso ou de dados.

## Estratégias (`src/Game.Bots`)

| Nome | Comportamento |
|---|---|
| `idle` | parado; só ataque básico se houver alvo em alcance |
| `basic_only` | persegue e usa só o ataque básico |
| `rotation` | persegue e usa as habilidades da classe na ordem da definição (prioridade), com condições simples por papel (ver abaixo); básico quando nada mais serve |
| `kite` | como `rotation`, mas mantém distância: recua quando o inimigo chega perto (escolhe direção livre, prefere atrás de obstáculos), aproxima quando longe; usa só instantâneas ao recuar e o recuo (dash para trás) se existir |
| `taunt_and_basic` | só provocação + básico (testa a falha de design "Guerreiro raso") |
| `explosion_spam` | só a habilidade de área de maior dano + básico (testa "Mago raso") |
| `heal_only` | só cura (no aliado mais ferido; segue o aliado), sem atacar |

Qualquer nome aceita o sufixo `+dodge` (`rotation+dodge`): se o bot está dentro de um perigo anunciado, sai dele por cima do movimento normal.

### Como as estratégias escolhem habilidades (guiado por dados)

O papel de cada habilidade é deduzido dos efeitos, não do id (`AbilityInfo`): `taunt` (efeito `threat` com `tauntDuration`), `heal`, `defensive` (escudo/status em si ou aliado), `dash` (deslocamento tipo dash; para trás = recuo), `interrupt`, `damage` (área = `circle_self`/`circle_at_point`/`line`). Condições do `rotation`: cura só abaixo de `healBelow` (0,75) de vida; provocação com 2+ inimigos ou aliados; defensiva com inimigo a até 6 tiles; dash para a frente com o alvo a 3,5+ tiles, para trás perto e com pouca vida (ou fugindo no `kite`); interrupção só se algum inimigo está conjurando; área só com `minAoe` (2) inimigos atingíveis. Tudo passa por `AbilityValidator` (recarga, recurso, alcance, linha de visão).

### Configuração por `--opt` (a T-014/T-016 ajustam isto sem mudar código)

Chaves globais ou por classe (`class.warrior:taunt=...`, a da classe vence). Seletor = lista de ids de habilidade ou `tag:nome`.

| Chave | Efeito | Padrão |
|---|---|---|
| `taunt` | habilidades usadas por `taunt_and_basic` | papel `taunt` |
| `explosion` | habilidade usada por `explosion_spam` | área ofensiva de maior dano (ou a de maior dano) |
| `heal` | habilidades usadas por `heal_only` | papel `heal` |
| `order` | habilidades que o `rotation`/`kite` tentam primeiro, nessa ordem | ordem da classe |
| `skip` | habilidades que o `rotation`/`kite` nunca usam | nenhuma |
| `healBelow`, `minAoe`, `kiteDistance`, `retreatDistance` | limiares numéricos | 0.75, 2, 85% do alcance do básico (máx. 7), 55% da distância preferida |

Ex.: `--opt class.warrior:taunt=warrior.provoke --opt class.mage:explosion=tag:aoe --opt class.warrior:skip=warrior.charge`.

## Métricas

Taxa de vitória (vitória / derrota / tempo esgotado), tempo até vitória (média, p50, p90, só execuções vencidas), dano recebido e causado por jogadores, cura efetiva e excedente, recurso ganho/gasto/desperdiçado, tempo com recurso no máximo, mortes de jogadores e inimigos, trocas de fase (qualquer evento cujo tipo tenha "Phase" no nome), distância andada, % de usos de habilidade feitos parado e usos médios por habilidade (e quantas habilidades distintas). Valores são médias por execução.

Recurso desperdiçado: exato para ganhos de habilidades (efeito `resource`) e parcial para regras da classe (só truncamento de regras de quantidade fixa), porque o Core não publica `ResourceChanged` quando o ganho é totalmente perdido; use "tempo com recurso no máximo" como indicador complementar.

## Perigos (se mover)

`Game.Bots.IHazardSource` fornece `Hazard`s (círculo, retângulo, cone, com tick de resolução). `RunSpec.HazardSourceFactory` a liga ao simulador; sem ela não há perigos e `+dodge` equivale à estratégia base. A T-016 implementa a fonte a partir dos eventos de telegráfo da T-012.

## Testes

`tests/Game.Sim.Tests` (determinismo por seed, paralelo = sequencial, 1000 execuções < 1 min, estratégias, dodge, CLI). Fixtures próprias em `tests/Game.Sim/Fixtures/`.
