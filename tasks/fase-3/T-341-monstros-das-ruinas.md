# T-341 — Monstros das Ruínas: Sacerdote Morto e Cavaleiro Espectral

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** — (integrar depois da T-340: as duas mexem em `monsters.json` e nos perfis de IA)
- **Libera:** T-343, T-345, T-351
- **Áreas que pode alterar:** `design/combat-encounters-v0.1.md`, `data/monsters.json`, `data/abilities_monsters.json`, `data/statuses.json`, `data/encounters.json`, `data/maps/` (arenas de teste novas), `src/Game.Core/Combat/Ai/`, `src/Game.Core/` (só mecânica nova exigida pelos monstros), `tests/`

## Contexto

As Ruínas da Estrada Antiga (`design/first-map-v0.1.md` §4) têm níveis 9–11: Guarda Esquelético 9 (já existe), **Sacerdote Morto 10** e **Cavaleiro Espectral 11, elite**. A área introduz mortos-vivos, corredores estreitos e **prioridade de alvo**: os guardas protegem os sacerdotes, que ficam atrás. O Cavaleiro é uma elite opcional na Torre Quebrada.

O Clérigo é eficiente contra mortos-vivos (`design/classes-v0.1.md`): as habilidades dele já têm `bonusVsTags` com `undead` e `corrupted` (+25%). Os monstros novos precisam da tag `undead`, e o Ent da T-340, da `corrupted`.

## Requisitos

1. **Design** (seção nova em `design/combat-encounters-v0.1.md`):
   - **Sacerdote Morto (10):** suporte inimigo. Proposta: cura um aliado (cast de 2 s, **interrompível**), maldição que reduz a cura recebida, e fica atrás dos guardas (perfil de IA de suporte).
   - **Cavaleiro Espectral (11, elite):** duelo de elite. Proposta: investida em linha (telegráfo), golpe giratório, e "Escudo Espectral" que reflete parte do dano até ser quebrado ou interrompido. Deve ser vencível por um grupo de 2–3 no nível 10–11, e por um solo bem equipado, com dificuldade.
2. **Dados e Core:** como na T-340. A IA de suporte precisa escolher o aliado mais ferido. A interrupção já existe no Core (`AbilitySystem`): confirme que um cast de cura de monstro é interrompível pelas habilidades com interrupção.
3. **Testes de design** com o perfil esperado do nível:
   - grupo típico do Pátio dos Mortos (3 Guardas + 1 Sacerdote) contra um grupo de 2: ≥ 80%; solo: 40–70% (o solo é possível, mas arriscado);
   - quem focar o Sacerdote primeiro termina mais rápido que quem não focar (estratégia nova no simulador, se precisar);
   - Cavaleiro: grupo de 3 nível 10–11 com incomuns 70–90% em 60–120 s.
4. Relatório `tasks/reports/balance-v0.12.md`.

## Critérios de aceite

- seção de design com os dois monstros;
- testes de design passando;
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
