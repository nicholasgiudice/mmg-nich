# T-011 — IA de monstros e ameaça

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-008
- **Libera:** T-012, T-014, T-015
- **Áreas que pode alterar:** `src/Game.Core/AI/`, `src/Game.Core/Threat/`, registro de sistemas, `tests/`

## Contexto

Leia `design/combat-encounters-v0.1.md` (comportamento de cada inimigo) e a seção 6 de `design/class-map-validation-v0.1.md` ("controle de ameaça não pode ser botão automático").

## Requisitos

1. **IA produz intenções** (`MoveIntent`, `UseAbilityIntent`) iguais às do jogador. Nada de alterar estado diretamente.
2. **Tabela de ameaça** por monstro: dano gera ameaça (1:1 por padrão), cura gera ameaça reduzida para os monstros engajados com o alvo curado, efeito `threat` (somar ameaça ou `taunt` que força o alvo por uma duração). Valores globais em `data/rules.json` (se a T-009 já criou o arquivo e sua definição, estender; senão criar e a T-009 estende).
3. **Aggro** por proximidade (raio no perfil) e por dano recebido; monstros do mesmo grupo de spawn entram juntos em combate.
4. **Perfis de IA** configurados por id + parâmetros em `MonsterDefinition`:
   - `melee_chaser`: persegue pelo pathfinding e ataca;
   - `pack` (Lobo): o grupo foca o mesmo alvo por alguns segundos; quando um membro morre, os outros recebem um status de agressividade;
   - `ranged_kiter` (Salteador): mantém distância preferida, recua quando alguém se aproxima, usa linha de visão;
   - `swarm` (Rato): aproxima rápido tentando cercar (posições de ataque distribuídas em volta do alvo); um rato troca de alvo ocasionalmente (usa RNG da simulação);
   - `guardian` (Guarda Esquelético): alterna fase de guarda (status com modificador frontal da T-009) e golpe pesado com conjuração visível.
5. Seleção de habilidade do monstro: lista com prioridade/condições simples (alcance, recarga), sem árvore complexa.
6. Eventos: `AggroGained`, `TargetChanged`, `Taunted`.

## Fora do escopo

Fases de boss, invocações e telegráfos (T-012).

## Critérios de aceite

- Teste: provocação força o alvo pela duração e depois volta para a tabela;
- teste: lobos focam o mesmo alvo; morte de um aplica agressividade aos outros;
- teste: Salteador mantém distância de um jogador que se aproxima em campo aberto;
- teste: ratos se distribuem em volta do alvo (não ficam empilhados);
- teste: Guarda alterna guarda e golpe pesado;
- determinismo preservado;
- `./scripts/check.sh` passa.

## Resultado

_(preencher ao concluir)_
