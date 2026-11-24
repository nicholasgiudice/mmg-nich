# T-510 — Monstros do Brejo dos Afogados e a Bruxa do Brejo

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** — (integrar depois da T-502, que traz a água funda)
- **Libera:** T-511, T-513, T-530, T-540
- **Áreas que pode alterar:** `design/bestiario.md`, `design/combat-encounters-v0.1.md`, `data/monsters.json`, `data/abilities_monsters.json`, `data/statuses.json`, `data/encounters.json`, `data/maps/` (arenas de teste novas), `src/Game.Core/Combat/Ai/` (perfil novo, se precisar), `src/Game.Core/` (só mecânica nova exigida pelos monstros), `tests/`

## Contexto

O Brejo dos Afogados (`design/mapas-16-40-v0.1.md` §3.1) é o campo do nível 16 ao 20, a ponte entre o primeiro mapa e a segunda classe. Dá para jogar sozinho com a classe base. O truque da área é a água funda (T-502). Termina com a **Bruxa do Brejo**, chefe de campo nível 20.

Mesmo formato da T-340 e da T-341: design primeiro, depois dados, Core só no que faltar, e testes de design no simulador.

## Requisitos

1. **Design** (seção nova em `design/combat-encounters-v0.1.md`), com papel, habilidades, telegráfos e a resposta esperada de cada classe. Propostas (o designer pode trocar; registre o que for decidido):
   - **Afogado** (`monster.drowned`, 16, normal, tag `undead`): lento, em bandos de 3 a 4. *Agarrão*: telegráfo curto e `status.snared` por 1,5 s. Ensina a não ficar cercado na água.
   - **Homem-lagarto do brejo** (`monster.marsh_lizardman`, 17, normal, tags `humanoid` e `amphibious`): rápido, em pares. Não sente a água funda. *Bote*: investida em linha com telegráfo, que pune quem luta parado.
   - **Serpente do Charco** (`monster.marsh_serpent`, 18, normal, tag `beast`): à distância, foge de quem chega perto (`ai.ranged_kiter`). *Cuspe*: projétil com `status.poison`.
   - **Bruxa do Brejo** (`monster.marsh_witch`, 20, **chefe**, tag `humanoid`): três fases, como a Matriarca:
     1. poças venenosas no chão, com telegráfo;
     2. invoca afogados (`summon`, como a Matriarca invoca aranhas);
     3. *Lodo que Puxa*: área grande de `status.deep_water` em volta dela por alguns segundos, mais projéteis.
2. **Dados** com nível, categoria, vida e potência no nível 1, como os monstros de hoje. Tags que as classes já usam (`undead`) mantêm o bônus do Clérigo.
3. **Core:** só o que faltar, cada peça com teste. A área de `status.deep_water` da Bruxa usa o estado da T-502.
4. **Encontros de teste e testes de design**, com o perfil esperado do nível (T-206/T-402):
   - solo, nas 4 classes base, contra cada grupo típico (afogados, lagartos, serpentes, misto): vitória ≥ 85%;
   - perfil fraco (3 níveis abaixo): ≤ 50%;
   - **Bruxa:** grupo de 3 do nível 19–20 vence ≥ 80%; solo ≤ 30%.
5. **Arte:** só os ids. Os sprites vêm da T-521 e o cliente liga na T-540. Use os ids acima: a T-521 monta os sprites com eles. Raio de 0,35 a 0,45 para os humanoides LPC.

## Critérios de aceite

- seção de design com os quatro monstros;
- testes de design passando, com o relatório `tasks/reports/balance-v0.NN.md` (próximo número livre);
- `design/bestiario.md` atualizado;
- `./scripts/check.sh` completo passa.

## Resultado

_(a preencher)_
