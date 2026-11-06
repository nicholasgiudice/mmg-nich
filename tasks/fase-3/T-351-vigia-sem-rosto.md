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
3. **Dados** do chefe, das sombras e da invocação de guardas (reaproveitando `monster.skeleton_guard`).
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

_(preencher ao concluir)_
