# T-017 — Mecânicas faltantes do design

- **Ambiente:** VPS
- **Modelo:** Forte
- **Depende de:** T-016
- **Libera:** T-018
- **Áreas que pode alterar:** `src/Game.Core/` (exceto `Authority/`, que é do cliente), `src/Game.Data/`, `data/`, `tests/Game.Core.Tests/`, `tests/Game.Data.Tests/`, `scripts/check.sh`, `AGENTS.md` (só a menção ao modo rápido), `tasks/README.md` (tabela da seção 5)

## Contexto

As tarefas T-014 e T-016 registraram lacunas entre o design e o Core (ver `## Resultado` delas e `tasks/reports/balance-v0.1.md`, §4). Esta tarefa fecha as lacunas que afetam o playtest. O cliente está sendo construído em paralelo no Windows (T-020 a T-024): **não altere `src/Game.Core/Authority/`** nem quebre APIs públicas usadas pelo cliente (`World`, `EncounterSetup`, eventos existentes, `CombatLogRecorder`, `Game.Bots`). Acrescentar é permitido; renomear/remover não.

## Requisitos

### Core

1. **Alvo por efeito**: campo opcional `target` em qualquer efeito: `targets` (padrão, os alvos resolvidos da habilidade) ou `self` (o conjurador). Permite "dano no inimigo + cura no conjurador" (Corrente Sagrada). A regra de facção da T-008 (habilidade só com efeitos benéficos afeta aliados) deve considerar só os efeitos com `target: targets`.
2. **Bônus por tag**: campo opcional `bonusVsTags` no efeito `damage` (ex.: `{ "undead": 0.5 }` = +50% contra alvos com essa tag; tags somam).
3. **Cargas (consumíveis)**: `AbilityDefinition.charges` (usos por encontro; ausente = ilimitado). Novo motivo de rejeição `NoCharges`. Campo `ClassDefinition.consumables` (lista de ids de habilidade) — consumíveis ficam separados de `abilities` para o cliente e os bots os distinguirem. Evento ou campo para o cliente saber as cargas restantes (ex.: em `AbilityExecuted` ou estado consultável).
4. **Controle reduzido por status**: modificador `controlDurationMultiplier` em `StatModifiers`, aplicado à duração de status com tag `control` recebidos (Frenesi da Matriarca).
5. **Tempo de armar**: `AreaDefinition.armTime` (segundos); a área não dispara antes de armada. Evento `AreaArmed` (com formatador de log).

### Conteúdo (`data/`)

6. **Guerreiro — Provocação** (`warrior.taunt`): efeito `threat` com `tauntDuration`; recarga relevante (8–12 s).
7. **Clérigo — Purificação** (`cleric.purify`): remove 1 status com tag `poison` ou `curse` de um aliado (ou do próprio Clérigo); gera Fé (regra `status_removed`).
8. **Corrente Sagrada**: dano sagrado + cura limitada no próprio Clérigo (`target: self`), com bônus contra `undead`/`corrupted` via `bonusVsTags` (remover a resistência negativa a sagrado do Guarda se ela ficar redundante).
9. **Consumível**: `item.minor_healing_potion` (cura moderada, 2–3 cargas por encontro, recarga de ~20 s) em `consumables` das quatro classes.
10. **Frenesi** com `controlDurationMultiplier` (ex.: 0,5) e **Armadilha** com `armTime` (~0,5 s).
11. Atualizar a tabela da seção 5 de `tasks/README.md` (Guerreiro e Clérigo passam a ter 4 habilidades; consumível comum a todos).

### Ferramentas

12. **`./scripts/check.sh --fast`**: pula `tests/Game.Design.Tests` (lentos). Sem argumento continua rodando tudo. Atualizar `AGENTS.md`: `--fast` durante o trabalho, `check.sh` completo antes do commit.

## Fora do escopo

Altar do Guarda Esquelético (exige objetos interativos); rebalanceamento fino e bots (T-018). Os testes de design (`Game.Design.Tests`) devem continuar passando — se alguma mudança de conteúdo os quebrar, ajuste números de forma mínima e registre; o rebalanceamento completo é da T-018.

## Critérios de aceite

- Testes de Core para: `target: self`, `bonusVsTags`, cargas e `NoCharges`, `controlDurationMultiplier`, `armTime`;
- teste de Data: as novas habilidades existem e estão ligadas às classes certas; consumível presente nas quatro classes;
- `./scripts/check.sh` (completo) passa.

## Resultado

_(preencher ao concluir: formatos JSON novos com exemplos, APIs novas que o cliente pode usar — cargas, `consumables` —, números escolhidos)_
