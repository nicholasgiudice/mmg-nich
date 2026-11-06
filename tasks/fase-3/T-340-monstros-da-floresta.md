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

Feito em 2026-11-06, na VPS, na branch `task/T-340-monstros-da-floresta`.

**Design.** Seção nova "Floresta Baixa — Fada da Névoa e Ent Corrompido" em `design/combat-encounters-v0.1.md`, com papel, habilidades, telegráfos, resposta de cada classe, grupos típicos e falhas de design, mais duas linhas na matriz de decisões. O `design/bestiario.md` foi atualizado com os dois monstros, os encontros novos e as resistências.

**Dados** (números do nível 1; ids `monster.mist_fairy` e `monster.corrupted_ent`, como pedido):
- **Fada da Névoa** (nv6, normal): vida 110, velocidade 3,4, raio 0,38, tag `fey`, IA `ai.mist_kiter`. Dardo de Névoa (`mist_fairy.mist_bolt`): arcano 14 e `status.mist_chill` (−20% de velocidade, 2,5 s). Véu de Névoa (`mist_fairy.mist_veil`): conjuração de 0,6 s, que é o aviso, e depois `status.in_mist` por 2 s (não pode ser alvo, +20% de velocidade), com recarga de 8 s.
- **Ent Corrompido** (nv7, normal): vida 430, velocidade 2,4, raio 0,7, tags `plant` e `corrupted`, resistências físico 0,25 e fogo −0,3 (fraqueza), IA `ai.brute`. Habilidades: Pisão Corrompido (círculo de 2,6 com telegráfo de 1,2 s, físico 44); Raízes Famintas (círculo de 1,6 no alvo com telegráfo de 1 s, natureza 16 e `status.snared` por 2 s, que já existia); Rajada de Espinhos (alvo único a até 8, natureza 14); Açoite de Galhos (corpo a corpo, físico 19).
- Encontros de teste `encounter.mist_fairies`, `encounter.corrupted_ent` e `encounter.mist_ambush` (3 Aranhas nv5 + Fada nv6, Guerreiro + Arqueiro), todos em `map.fields`. Nenhuma arena nova.

**Core.**
- `StatusFlags.Untargetable` (`flags.untargetable` no JSON e no esquema) e `StatusManager.IsUntargetable`. A entidade fica fora de alvo de unidade, arcos, linhas, círculos, telegráfos, áreas persistentes, correntes, alvos extras e da troca de alvo dos acertos extras; ela mesma ainda se alcança (`TargetResolver.CanBeHitBy`).
- Perfis `ai.mist_kiter` (`MistKiterProfile`) e `ai.brute` (`BruteProfile`) em `Combat/Ai/Profiles/`.
- Lentidão e prisão já existiam. A fraqueza a fogo usa a resistência negativa, que a fórmula já aceitava.
- Testes em `tests/Game.Core.Tests/ForestMonsterTests.cs`.

**Testes de design** (`tests/Game.Design.Tests/ForestDesignTests.cs`, relatório `tasks/reports/balance-v0.11.md`):
- perfil esperado: 100% nas 4 classes contra os dois grupos, entre 9,6 e 35,7 s;
- perfil fraco: 0–39%;
- Emboscada: o par vence em 51% do tempo médio dos solos e cada um recebe 28% do dano que receberia sozinho.

**Decisões e desvios.**
- **A Fada visível não foge.** A proposta era fugir de quem chega perto, como o Salteador. Assim o Guerreiro nunca a alcançava (0%), então a fuga ficou só na névoa (some, se afasta e reaparece longe).
- **Rajada de Espinhos no Ent**, que não estava na proposta. Sem ela, quem ataca de longe e desvia vencia o Ent sem tomar dano, mesmo 3 níveis abaixo.
- **Perfil novo `ai.brute`.** O `ai.melee_chaser` usa habilidades de área com o alvo fora de alcance, porque a validação de área nunca falha. O Pisão sairia no vazio.
- **Exceção do Clérigo no perfil fraco**, a mesma do Guarda Esquelético (balance-v0.7 §3): ele se cura indefinidamente e vence fraco, mas com ≥ 1,3× o tempo (1,40× e 1,47×).
- **Bots com `+dodge`** nos testes da Floresta (desviam dos telegráfos), como na Matriarca.
- **Fora das "Áreas que pode alterar"** (mínimo): `data/schemas/game-data.schema.json` (a flag nova) e `src/Game.Site/Describe.cs` (o texto "não pode ser alvo" na descrição de status). Nos testes existentes que assumiam só os 5 encontros e os 7 monstros do protótipo, mudei os filtros e as listas: `MonsterScalingTests`, `PrototypeContentTests` e `ProgressionContentTests`.

**Pendências.**
- **T-345:** mostrar a conjuração do véu e o estado `status.in_mist`, impedir a seleção de alvo na névoa no cliente (o Core já rejeita) e criar a arte do Ent, que segue em encomenda.
- **T-342:** medir o ritmo de XP da Floresta com a zona real e rever a linha de visão com árvores.
- `design/habilidades.md` § Monstros não foi atualizado (fora das áreas da tarefa).
- O `ai.ranged_kiter` do Salteador tem o mesmo problema de parar sem linha de visão que corrigi na Fada; não mexi para não mudar o balanço dos Campos.
- **Tempo da suíte de design:** a T-340 acrescenta 19 lotes de 100 seeds, cerca de 1 min sozinha na VPS. A suíte inteira (102 testes) passou em 15,6 min de parede e 9 min de CPU, com outra sessão rodando testes ao mesmo tempo. Se o limite de 5 min do `DesignHarness` voltar a apertar, rodar os testes da Floresta com menos seeds.
