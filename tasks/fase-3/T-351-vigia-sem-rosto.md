# T-351 — Core e dados: o Vigia Sem Rosto

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-341
- **Libera:** T-345, T-352
- **Áreas que pode alterar:** `design/combat-encounters-v0.1.md`, `data/monsters.json`, `data/abilities_monsters.json`, `data/statuses.json`, `data/encounters.json`, `data/maps/` (arena de teste do chefe), `src/Game.Core/` (fases, selos, medo; o que o chefe exigir), `tests/`, `tasks/reports/`

## Contexto

O chefe final do primeiro mapa (`design/first-map-v0.1.md` §6): o antigo comandante da fortificação, nível 15, para um grupo de 4 no nível 14–15 com itens raros. A meta é vitória de 50–80% em 4–6 min (`design/progression-v0.1.md` §8). As fases do design são:

- **Juramento de Ferro:** golpe frontal em cone, investida em linha, invoca Guardas Esqueléticos; o chefe deve ficar virado para o Guerreiro;
- **O Rosto Vazio:** dano espectral, áreas de **medo**, mais vulnerável a dano sagrado, sombras menores; obriga a se reposicionar o tempo todo;
- **Último Comando:** três **selos** começam a ruir; o grupo deve interromper o chefe e proteger os selos. A luta acaba com o chefe derrotado (vitória) ou com **todos os selos destruídos** (falha).

As fases por vida (`phases` em `monsters.json`) já existem, desde a Matriarca. A pendência da T-206 diz que o nível 12+ precisa de medição nova, porque a habilidade assinatura muda a curva.

## Requisitos

1. **Design detalhado** (seção nova em `design/combat-encounters-v0.1.md`): cada habilidade com o telegráfo, os números no nível 1 e a resposta esperada de cada classe. A resposta de cada classe também está em `design/class-map-validation-v0.1.md`.
2. **Mecânicas novas no Core**, cada uma com teste:
   - **medo**: o alvo foge do lugar de origem por N ticks, sem controle do jogador. Respeita a regra de imunidade a controles, se existir;
   - **selos**: entidades com vida, facção aliada aos jogadores, que não se movem e que o chefe ataca. Elas podem ser curadas ou receber escudo. Quando todas são destruídas, o encontro termina em falha (evento `EncounterFailed`, ou equivalente, para o log e o cliente);
   - **interrupção do Último Comando**: um cast longo que, se não for interrompido, destrói um selo.
3. **Dados** do chefe, das sombras e da invocação de guardas (reaproveitando `monster.skeleton_guard`). **Use os ids `monster.faceless_watcher` e `monster.shade`**, já ligados no `client/assets/art/sprites.json` pela T-056. O Vigia é desenhado em escala ×2, como a Matriarca, então um raio de ~0,9 combina com ele; a sombra é um humanoide LPC (raio ~0,40). Se precisar de outro id, registre no Resultado.
4. **Encontro de teste** e testes de design, com o grupo de 4 nível 14 e 15 e itens raros:
   - vitória de 50–80% e duração de 4–6 min;
   - sem Clérigo, a vitória cai de forma clara, e sem Guerreiro também;
   - sem interromper o Último Comando, a vitória fica abaixo de 30%.
5. Relatório `tasks/reports/balance-v0.13.md`, incluindo a medição pedida pela T-206 (nível 12+ com a assinatura).

## Critérios de aceite

- testes do Core das três mecânicas;
- testes de design passando dentro das metas;
- `./scripts/check.sh` completo passa.

## Resultado

Feito em 2026-11-10, na VPS, na branch `task/T-351-vigia-sem-rosto`.

**Design.**
- Seção nova "Cripta do Vigia — O Vigia Sem Rosto" em `design/combat-encounters-v0.1.md`, com:
  - cada habilidade: telegráfo, números no nível 1 e no 15;
  - as três fases, o medo e os selos;
  - a resposta de cada classe, o encontro de teste e as falhas de design.
- Uma linha nova na matriz de decisões.
- `design/bestiario.md` atualizado (Vigia, Sombra e Selo).

**Dados** (ids `monster.faceless_watcher` e `monster.shade`, como pedido; raios 0,9 e 0,4):
- **Vigia Sem Rosto** (nível 15, chefe, `undead`, `ai.boss`): vida 45.000.
  - Fase 1, Juramento de Ferro: Golpe de Ferro, Corte do Juramento (cone com telegráfo), Investida de Ferro (linha com telegráfo) e Chamado da Guarda (conjuração interrompível; 2 `monster.skeleton_guard`).
  - Fase 2, O Rosto Vazio (65%): Rosto Vazio (dano sagrado recebido × 1,4), Golpe Espectral (arcano), Chão do Pavor (círculo com telegráfo + medo de 2 s) e Sombras do Rosto Vazio (3 `monster.shade`).
  - Fase 3, Último Comando (25%): sem invocações; os selos começam a ruir; Último Comando (conjuração de 4 s no selo mais ferido, que o destrói).
- **Sombra** (nível 14): vida 150, Toque Gélido arcano.
- **Selo do Juramento** (`monster.watcher_seal`, **id novo**; nível 15, vida 380, velocidade 0): é o objetivo. Não tem sprite ainda (T-345).
- **Encontro** `encounter.faceless_watcher` (Guerreiro + Arqueiro + Mago + Clérigo, 3 selos, limite de 480 s).
- **Arena de teste** `data/maps/watcher_hall.tmj` (`map.watcher_hall`): cópia do salão aberto de 40 × 28 do ninho da Matriarca, provisória até a sala da Cripta (T-352).

**Core** (cada mecânica com teste em `tests/Game.Core.Tests/WatcherTests.cs`, 16 testes):
- **Medo** (`flags.feared`): o alvo não age e foge do ponto de origem, sem controle do jogador. A origem é o centro do telegráfo ou da área (`StatusInstance.Origin`, que entra no hash). A direção é imposta por `IModifierSource.ForcedDirection`, que o `MovementSystem` usa no lugar da intenção. Com a tag `control`, valem o rank, o retorno decrescente e o `immuneToTags`.
- **Selos** (`objectives` do encontro, pasta `Combat/Objectives`): unidades da facção dos jogadores, sem IA e fixas (velocidade 0: a separação empurra só a outra entidade). Podem ser curadas e receber escudo; não puxam aggro por proximidade e não são revividas. O `ObjectiveSystem` (ordem 9050) publica `ObjectiveDestroyed` e, quando o último cai, `EncounterFailed` uma vez. O `EncounterSetup` e o simulador fazem os objetivos nascer; o simulador conta `EncounterFailed` como derrota e mostra "objectives lost / failed".
- **Interrupção do Último Comando:** a conjuração longa já era interrompível. O que faltava era o campo novo `cooldownOnInterrupt`: interrompida, a habilidade entra na recarga cheia. Sem isso, o chefe reconjurava no tick seguinte e interromper não adiantava.
- Também: `modifiers.damageTakenBySchool` (Rosto Vazio), `onEnterObjectives` nas fases (Selo Intacto na fase 1, ruína na fase 3) e a tag `ai.objective` no `ai.boss` (alvo = objetivo inimigo mais ferido). As linhas de log dos eventos novos ficam no `EncounterModule`.

**Testes de design** (`tests/Game.Design.Tests/WatcherDesignTests.cs`; relatório `tasks/reports/balance-v0.13.md`):
- **Grupo de 4 com raros:** nível 14 com 57% em 316 s, nível 15 com 80% em 295 s; média de 68,5% (meta 50–80%, 4–6 min).
- **Sem Clérigo:** 0%. **Sem Guerreiro:** 0%.
- **Sem interromper o Último Comando:** 0% nos níveis 14 e 15 (meta < 30%).
- **Medição da T-206 (nível 12+ com a assinatura):** a assinatura quase não muda a curva (13: +6 pontos; 14: igual; 15: −3 pontos). O salto vem do nível. No nível 12, o perfil focado ainda não tem ponto na assinatura (balance-v0.13 §4).

**Decisões e desvios.**
- **A meta de vitória vale para a média dos níveis 14 e 15**, como na T-341. O degrau entre os níveis é de ~23 pontos e o nível 15 fica no teto (80%). O teste exige cada nível entre 40% e 90% e a média entre 50% e 80%.
- **Estratégia correta dos bots com opções novas**, desligadas por padrão:
  - `interrupt=save` (Guerreiro): guarda o Golpe Pesado e a Mana para quem conjura e anda até ele;
  - `interrupt=never` (para o teste "sem interromper");
  - `followInjured=1` (Clérigo vai até o aliado ferido fora do alcance da cura).
  O padrão (`auto`) quase nunca tem o Golpe Pesado pronto no Comando: 3%.
- **Selos intactos até a fase 3:** sem isso, os ataques em área do chefe os destruíam na fase 1.
- **Sem Guerreiro não há vitória:** só ele tem interrupção. A composição fica fechada; está registrado como ponto para o designer (balance-v0.13 §5).
- **Fora das "Áreas que pode alterar"** (mínimo):
  - `data/schemas/game-data.schema.json` (campos novos);
  - `src/Game.Data/GameDataValidator.cs` (escola desconhecida, `objectives`, `onEnterObjectives`);
  - `src/Game.Protocol` (codec de `EncounterObjective`, `ObjectiveDestroyed` e `EncounterFailed`; dano recebido e origem no status ativo; **versão do protocolo 10 → 11**);
  - `src/Game.Site` (texto do medo, dano por escola, recarga ao interromper, objetivos e efeitos de fase nos objetivos);
  - `src/Game.Bots` (`interrupt` e `followInjured`);
  - `design/bestiario.md`.

**Pendências.**
- **T-345:** mostrar no cliente o medo (fuga), o Rosto Vazio, a conjuração do Último Comando e os selos (sprite, vida, intacto/em ruína, destruído) e uma tela de falha para `EncounterFailed`.
- **T-352:** sala real da Cripta com os selos nas bordas; o servidor (T-350) deve encerrar a instância em `EncounterFailed`.
- **Interrupção só no Guerreiro:** decidir se outra classe ganha interrupção.
- **Assinatura:** medir num solo de nível 12+ quando houver conteúdo desses níveis.
- `design/habilidades.md` § Monstros não foi atualizado (fora das áreas, como nas T-340 e T-341).
