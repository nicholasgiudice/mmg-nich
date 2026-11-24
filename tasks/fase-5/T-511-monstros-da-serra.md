# T-511 — Monstros da Serra Partida

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** 510 (mesmos arquivos de dados)
- **Libera:** T-512, T-513, T-532, T-540
- **Áreas que pode alterar:** as mesmas da T-510

## Contexto

A Serra Partida (`design/mapas-16-40-v0.1.md` §3.3) é o campo do nível 21 ao 25, a **primeira área de segunda classe**. Corredores de rocha e trilhas em encosta. O truque é ter **monstros em fila** nos desfiladeiros (serve ao Atirador de Elite e ao Berserker) e grupos que pedem dano em área (§4 do esboço).

## Requisitos

1. **Design** (seção nova em `design/combat-encounters-v0.1.md`). Propostas (o designer pode trocar):
   - **Mineiro Possuído** (`monster.possessed_miner`, 21, normal, tag `humanoid`): corpo a corpo, em grupos de 3 a 5. *Picaretada*: golpe com telegráfo que atordoa (`status.stunned`) por 1 s.
   - **Lobo da Montanha** (`monster.mountain_wolf`, 22, normal, tag `beast`, `ai.pack`): matilha de 4 a 5, como o Lobo Cinzento, mais forte.
   - **Morcego das Cavernas** (`monster.cave_bat`, 22, normal, tag `beast`, `ai.swarm`): bandos de 5 a 6, pouca vida. Alvo de dano em área.
   - **Golem de Pedra** (`monster.stone_golem`, 25, **elite**, tag `construct`): lento, resistência a físico, fraqueza a arcano. *Rocha Rolante*: linha longa com telegráfo de 1,5 s que atravessa o desfiladeiro e acerta todos na linha (monstros não).
2. **Grupos maiores:** do 21 em diante, parte dos grupos tem de 5 a 10 monstros. Os testes usam o **perfil esperado com segunda classe** (as 8 segundas classes, nível 21–25).
3. **Core:** só o que faltar, com teste.
4. **Testes de design:**
   - solo, nas 8 segundas classes, contra cada grupo típico: vitória ≥ 85%;
   - um grupo grande (8 monstros) leva menos tempo com Piromante ou Berserker que com Arcanista (o design das classes pede isso; o teste só mede e registra);
   - Golem: solo ≥ 60%, grupo de 2 ≥ 90%.
5. **Arte:** só os ids (T-521, T-540). O Lobo da Montanha pode usar o sprite do Lobo Cinzento com outra cor.

## Critérios de aceite

- seção de design com os quatro monstros;
- testes de design passando, com relatório em `tasks/reports/`;
- `design/bestiario.md` atualizado;
- `./scripts/check.sh` completo passa.

## Resultado

_(a preencher)_
