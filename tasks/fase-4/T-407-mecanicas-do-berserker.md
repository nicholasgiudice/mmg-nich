# T-407 — Core: mecânicas novas do Berserker

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-406 (sai da branch dela: mesmos arquivos de dano e estados)
- **Libera:** T-408
- **Áreas que pode alterar:** `src/Game.Core/` (efeitos, estados, dano, áreas, recargas), `src/Game.Protocol/` (só formato de rede de eventos novos e `ProtocolVersion`), `src/Game.Data/` (carga e validação dos campos novos), `data/schemas/`, `tests/Game.Core.Tests/`, `tests/Game.Data.Tests/`
- **Branch:** sai de `trilha/segundas-classes` e é integrada nela, não na `main` (seção 6f de `tasks/README.md`).

## Contexto

Design em `design/segunda-classe-v0.1.md`, seção 5 (Berserker e a tabela de mecânicas). Esta tarefa cria **as mecânicas** no Core, cada uma com teste e com dados de teste. As habilidades de verdade vêm na T-408.

**Base:** crie a branch a partir de `task/T-406-mecanicas-do-guardiao`, que ainda não está na trilha porque subiu `ProtocolVersion` para 16. Se esta tarefa precisar de evento novo na rede, suba o número a partir do da T-406. A T-407 entra na `trilha/segundas-classes` junto com a rodada do protocolo (seção 6f de `tasks/README.md`).

Já existem: estado com mais dano causado, velocidade e dano recebido (o `status.frenzy` dos monstros), metade da duração de controles (`controlDurationMultiplier`), reforço do próximo golpe (`empower`, do Recuo Rápido), vários acertos com intervalo (`hits`), Sangramento e reações depois do dano (`IDamageReaction`).

## Requisitos

1. **Dano extra pela vida do alvo** (Execução): o efeito de dano ganha um bônus quando o alvo está abaixo de uma fração da vida máxima (por exemplo, `+50%` abaixo de `30%`), conferido no momento do acerto.
2. **Zerar a recarga ao matar** (Execução NH5): se o acerto da habilidade mata o alvo, a recarga dela volta a zero.
3. **Vida pelo dano causado** (Sede de Sangue, passiva; depois o Selo da Luz e o Julgamento do Inquisidor):
   - modificador de estado: fração do dano final causado que volta como cura para quem causou;
   - opções: a fração **dobra** quando quem causa está abaixo de uma fração da vida; **matar** um monstro cura uma fração da vida máxima;
   - a cura respeita a regra de cura recebida (`healingTakenMultiplier`) e aparece no log como cura da habilidade que deu o estado;
   - dano refletido ou desviado não gera vida (sem laço).
4. **Área que acompanha quem anda** (Tempestade de Aço; depois o Bastião, se virar aura): uma área presa a uma entidade, que acerta em volta dela a cada intervalo, pela duração, seguindo a posição atual. O lançador pode andar, com a velocidade reduzida pelo estado da habilidade.
5. Validação dos campos novos e schema.

## Critérios de aceite

Testes no Core:

- Execução de teste: alvo com 50% de vida leva o dano normal; com 20%, leva +50%;
- golpe que mata zera a recarga; golpe que não mata não zera;
- estado com 10% de vida pelo dano: 100 de dano curam 10; abaixo do limite de vida curam 20; matar cura a fração da vida máxima; dano refletido não cura;
- área presa ao lançador acerta em volta da posição nova depois que ele anda; acaba com a duração ou com a morte dele;
- `./scripts/check.sh` completo passa.

## Resultado

Feito em 2026-11-21, na VPS, branch `task/T-407-mecanicas-do-berserker` (a partir de `task/T-406-mecanicas-do-guardiao` em `6eca961`).

### O que foi feito

1. **Dano extra pela vida do alvo** (`damage.lowHealthBonus`): `{ belowHealth, bonus }`. Se a vida atual do alvo está
   **abaixo** (estritamente) de `belowHealth × vida máxima` no momento do acerto (antes do dano), o dano é multiplicado por
   `1 + bonus`. Vale por acerto (habilidade com vários acertos confere a cada um).
2. **Zerar a recarga ao matar** (`damage.resetCooldownOnKill`): se esse dano matar o alvo, `AbilityState.ReadyAtTick` da
   habilidade de origem volta ao tick atual (pronta já). Só para dano de habilidade (não de status nem de área). Com cargas,
   a carga em recarga volta no tick seguinte. O cliente vê a recarga nova pelo `AbilityState` do snapshot (sem evento novo).
3. **Vida pelo dano causado** (`lifesteal` no status), nova reação `LifestealReaction` (`Combat/Status/Lifesteal.cs`):
   - cura `fraction × acúmulos` do dano final (`DamageDealt.Amount`, depois da defesa e antes dos escudos do alvo);
   - abaixo de `belowHealth` da vida (conferido antes da cura) a fração é multiplicada por `belowHealthMultiplier` (padrão 2);
   - se o dano matou um inimigo (facção diferente), soma `killHeal × vida máxima`;
   - passa pela cura recebida (`healingTakenMultiplier`), não é crítica; sai como `HealApplied` de quem causou nele mesmo,
     com `SourceId` = `lifesteal.ability` (a habilidade que deu o estado) ou, sem ela, o status;
   - dano refletido (sem modificadores) e a parte desviada para um protetor (T-406) não geram vida: `DamageReport` ganhou
     `Indirect`, preenchido pelo `DamageResolver`. Dano em si mesmo e de fonte morta também não;
   - cura sem efeito (vida cheia) não publica nada, para não encher o log.
4. **Área que acompanha quem anda** (`area.followOwner`): nasce na posição de quem a criou e, a cada tick (o movimento
   roda antes das áreas), vai para a posição atual dele antes de acertar. Acaba com a duração ou quando o dono morre ou
   some (motivo novo `AreaExpireReason.OwnerDied`). A velocidade menor vem de um estado com `speedMultiplier` na própria
   habilidade (nada novo). O cliente já desenha a área pela posição do snapshot, então ela anda na tela sem mudança.
5. **Log de combate**: cura em si mesmo ganhou linha própria: "Berserker recuperou 10 de vida com Sede de Sangue"
   (vale também para poções, que antes saíam como "Você curou Você em 50 de vida").
6. **Validação e schema**: `GameDataValidator` e `data/schemas/game-data.schema.json` para os quatro campos.

### Campos novos nos dados (para a T-408)

```json
{ "type": "damage", "base": 20, "scaling": { "weapon": 1.3, "str": 3.2 },
  "lowHealthBonus": { "belowHealth": 0.3, "bonus": 0.5 }, "resetCooldownOnKill": true }

{ "id": "status.bloodthirst_5", "name": "Sede de Sangue", "defaultDuration": 1,
  "lifesteal": { "fraction": 0.1, "belowHealth": 0.3, "belowHealthMultiplier": 2, "killHeal": 0.05,
                 "ability": "berserker.bloodthirst" } }

{ "id": "area.steel_storm", "radius": 3, "duration": 4, "interval": 0.5, "followOwner": true,
  "effects": [ { "type": "damage", "base": 6, "scaling": { "weapon": 0.5, "str": 1.2 } } ] }
```

Faixas: `lowHealthBonus.belowHealth` em (0, 1], `bonus` > 0; `lifesteal.fraction` em (0, 1], `belowHealth` e `killHeal`
em [0, 1], `belowHealthMultiplier` ≥ 1, `ability` precisa existir. A Tempestade de Aço usa `spawn_area` com `followOwner`
mais um `apply_status` em si com `speedMultiplier: 0.8` e duração de 4 s. Oito giros: duração 4 s e intervalo 0,5 s
(o primeiro giro sai 0,5 s depois de usar, o último no tick final).

### Testes

- `tests/Game.Core.Tests/BerserkerMechanicsTests.cs` (14): Execução com 50%, 30% e 20% de vida; golpe que mata zera a
  recarga e o seguinte que não mata não zera; 10% de 100 cura 10; abaixo de 30% cura 20; matar cura 10 + 5% da vida
  máxima; cura recebida pela metade; dano refletido não cura; parte desviada não cura quem atacou; vida cheia não publica
  cura; área presa anda com o lançador (20% mais devagar) e acerta em volta da posição nova; acaba com a duração (8
  giros) e com a morte do dono.
- `tests/Game.Data.Tests/BerserkerMechanicsLoaderTests.cs` (10): carga dos campos e erros de validação.

### Decisões

- Vida pelo dano usa o `Amount` (dano final antes dos escudos, com excesso de morte), o mesmo número que o reflexo usa.
  Se a vida devolvida ficar alta contra monstros fracos, trocar para o dano aplicado é uma mudança só na reação.
- "Abaixo de" é estrito nas duas regras de vida (30% exatos não ganham bônus).
- A cura é atribuída a uma habilidade por campo nos dados (`lifesteal.ability`) e não guardada no estado: sem campo novo
  no snapshot e sem depender de como a T-403 aplica o estado da passiva.

### Desvios

- `ProtocolVersion` continua **16** (o da T-406): nenhum evento nem campo novo na rede. O valor novo de
  `AreaExpireReason` (`OwnerDied`) vai pelo mesmo formato de enum. Cliente e servidor saem juntos, como sempre.
- Mudança no log de combate (Core, dentro da área): a linha de autocura citada acima.

### Pendências

- As habilidades de verdade (Investida, Fúria, Execução, Sede de Sangue, Tempestade de Aço) e os textos do site são da T-408.
- Contra chefes a vida devolvida pode precisar de redução (design, seção 5); hoje não há regra por rank.
- O cliente não destaca a recarga zerada (só vê o número novo no snapshot).
