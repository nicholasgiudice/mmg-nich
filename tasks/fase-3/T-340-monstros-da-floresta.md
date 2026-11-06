# T-340 — Monstros da Floresta Baixa: Fada da Névoa e Ent Corrompido

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** —
- **Libera:** T-342, T-345
- **Áreas que pode alterar:** `design/bestiario.md`, `design/combat-encounters-v0.1.md`, `data/monsters.json`, `data/abilities_monsters.json`, `data/statuses.json`, `data/encounters.json`, `data/maps/` (arenas de teste novas), `src/Game.Core/Combat/Ai/` (perfil novo, se precisar), `src/Game.Core/` (só mecânica nova exigida pelos monstros), `tests/`

## Contexto

A Floresta Baixa (`design/first-map-v0.1.md` §3) tem níveis 5–8 (`design/progression-v0.1.md` §2): Aranha do Bosque 5, **Fada da Névoa 6**, **Ent Corrompido 7** e Matriarca 8. As aranhas e a Matriarca já existem (fase 0). A Fada e o Ent não existem nem em design: `design/bestiario.md` é gerado a partir dos dados. A pendência vem da T-205.

A Floresta deve ensinar **posicionamento, controle de área, veneno e emboscadas**. Grupos têm vantagem, mas jogar solo é possível.

## Requisitos

1. **Design** (seção nova em `design/combat-encounters-v0.1.md`), com papel, habilidades, telegráfos e a resposta esperada de cada classe:
   - **Fada da Névoa (6):** à distância e difícil de localizar. Proposta: alterna entre visível e "na névoa" (não pode ser alvo por até 2 s, com aviso visual antes de sumir), lança um projétil que **lentifica** e foge de quem chega perto. Pouca vida.
   - **Ent Corrompido (7):** lento e resistente, com controle. Proposta: golpe em área ao redor com telegráfo de 1,2 s, raízes que **prendem** quem fica na área marcada, resistência alta a dano físico e fraqueza a fogo.
   O designer pode trocar as propostas. Registre o que for decidido.
2. **Dados**: o Ent leva a tag `corrupted`, porque as habilidades do Clérigo já têm bônus contra ela. Arquivos: (`monsters.json`, `abilities_monsters.json`, `statuses.json`) com nível, categoria, vida e potência no nível 1 (a escala por nível é da T-206).
3. **Core:** só o que faltar para as mecânicas, como um estado "não pode ser alvo", lentidão ou prisão, se ainda não existirem, cada um com teste. Os perfis de IA novos ficam em `Combat/Ai/Profiles/`.
4. **Encontros de teste** (`encounters.json`) e testes de design no simulador, com o perfil esperado do nível da área (T-206):
   - solo, nas 4 classes, contra um grupo típico de cada monstro: vitória ≥ 85% em até 40 s;
   - o perfil fraco (3 níveis abaixo): ≤ 50%;
   - um grupo misto (aranhas + fada) mostra vantagem real de grupo de 2 sobre solo.
5. **Arte:** o sprite fica para a T-345 (Windows). Aqui, só o id de definição. **Use os ids `monster.mist_fairy` e `monster.corrupted_ent`**: a T-056 já ligou a Fada com esse id no `client/assets/art/sprites.json`. Se precisar de outro id, registre no Resultado, para a T-345 renomear a chave. A Fada usa um sprite humanoide LPC (pés no centro do quadro de 64 px), e um raio de 0,35 a 0,40 combina com ele. O Ent ainda não tem arte: os candidatos LPC foram reprovados e ele vai para encomenda (`tasks/reports/arte-monstros-fase-3.md`).

## Critérios de aceite

- seção de design com os dois monstros;
- testes de design passando, com o relatório `tasks/reports/balance-v0.11.md`;
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
