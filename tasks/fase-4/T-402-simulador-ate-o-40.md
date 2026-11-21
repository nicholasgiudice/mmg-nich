# T-402 — Simulador até o nível 40 e metas dos testes de design

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-401
- **Libera:** T-408
- **Áreas que pode alterar:** `tests/Game.Sim/`, `tests/Game.Sim.Tests/`, `tests/Game.Design.Tests/`, `data/rules.json` (só os tetos de AGI, se a análise pedir), `design/progression-v0.1.md` (§8 metas), `design/segunda-classe-v0.1.md` (só para registrar números medidos), `tasks/reports/`
- **Branch:** sai de `trilha/segundas-classes` e é integrada nela, não na `main` (seção 6f de `tasks/README.md`).

## Contexto

O simulador (`tests/Game.Sim`) monta personagens com `CharacterProfile.Expected(level)` e `Weak(level)`: pontos divididos entre os atributos sugeridos da classe (`CharacterProfiles.Suggested`) e equipamento do nível. Os testes de design (`tests/Game.Design.Tests`) usam esses perfis contra os encontros de cada área.

Dois problemas para a fase 4:

1. Os perfis e os testes só foram pensados até o nível 15. Com o nível 40 (T-401), é preciso saber se as fórmulas continuam de pé: escala dos monstros por nível (`rules.monsters`), defesa (`defensePerAttackerLevel`, teto de 60%), tetos de AGI (velocidade de ataque no máximo com AGI 100, crítico com AGI 140) e itens por nível.
2. **19 casos de teste de design estão desligados** com o motivo `PlaytestRebalance.Pending` (`tests/Game.Design.Tests/PlaytestRebalance.cs`), desde o balanceamento do playtest de 2026-11-16 (`tasks/reports/playtest-v0.3.md`). O jogo ficou mais fácil que as metas.

## Requisitos

1. **Perfis até o 40:** `CharacterProfile` aceita qualquer nível até `maxLevel`, com os pontos de atributo do nível (incluindo os +5 da segunda classe a partir do 20, pela regra da T-401) e equipamento do nível. Sem segunda classe ainda (ela chega na T-403); deixar o perfil pronto para receber a segunda classe depois.
2. **Relatório `tasks/reports/balance-v0.18.md`** (ou o próximo número livre), com, do nível 1 ao 40, de 5 em 5:
   - vida, Mana e dano das habilidades principais de cada classe no perfil esperado;
   - redução de defesa de um personagem contra um monstro do mesmo nível;
   - quando o perfil esperado bate nos tetos de AGI e de defesa;
   - tempo para matar um monstro normal do próprio nível (usando monstros existentes com o nível ajustado) e XP por minuto que isso dá.
3. **Tetos de AGI:** se o relatório mostrar que o perfil esperado bate no teto antes do nível 40, propor o ajuste (teto maior ou valor por ponto menor) e aplicar em `data/rules.json`, com o antes e depois no relatório.
4. **Metas revistas:** rever as metas da §8 do design de progressão à luz do balanceamento de 2026-11-16 e reativar os 19 casos de `PlaytestRebalance.Pending`. Cada caso termina passando com uma meta nova justificada no relatório, ou removido com o motivo. `PlaytestRebalance.cs` é apagado se não sobrar nenhum uso.
5. **Metas da segunda classe** (para a T-408 usar): escrever na §8 do design de progressão as metas que um personagem de segunda classe deve cumprir, por exemplo:
   - no mesmo nível e equipamento, Berserker mata um grupo típico mais rápido que o Guerreiro só com a base;
   - Guardião reduz o dano recebido pelo grupo num encontro de grupo;
   - nenhuma segunda classe mais de 1,6× mais lenta que a média das outras no solo.

## Critérios de aceite

- `./scripts/check.sh` completo passa, sem nenhum `PlaytestRebalance.Pending` restante;
- relatório com as tabelas pedidas e as decisões tomadas;
- perfis de nível 21 a 40 funcionam no simulador (teste).

## Resultado

_(preencher ao concluir)_
