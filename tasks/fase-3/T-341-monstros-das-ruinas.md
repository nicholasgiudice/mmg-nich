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
5. **Arte:** o sprite fica para a T-345. **Use os ids `monster.dead_priest` e `monster.spectral_knight`**, já ligados no `client/assets/art/sprites.json` pela T-056 (folhas do gerador LPC). Se precisar de outro id, registre no Resultado. Os dois são humanoides LPC como o Guarda Esquelético: um raio de 0,45 a 0,50 combina com o sprite.

## Critérios de aceite

- seção de design com os dois monstros;
- testes de design passando;
- `./scripts/check.sh` completo passa.

## Resultado

Feito em 2026-11-09, na VPS, na branch `task/T-341-monstros-das-ruinas`.

**Design.** Seção nova "Ruínas da Estrada Antiga — Sacerdote Morto e Cavaleiro Espectral" em `design/combat-encounters-v0.1.md`, com papel, habilidades, telegráfos, resposta das classes, grupos típicos e falhas de design, mais duas linhas na matriz de decisões. O `design/bestiario.md` também foi atualizado, como na T-340, com os dois monstros e os dois encontros.

**Dados** (números do nível 1; ids `monster.dead_priest` e `monster.spectral_knight`, como pedido; raio 0,45 e 0,5):
- **Sacerdote Morto** (nv10, `undead`, IA `ai.support`): vida 180, velocidade 3,0.
  - Remendar os Mortos: conjuração de 2 s, interrompível; cura 105 o aliado mais ferido; recarga 6 s.
  - Maldição Definhante: `status.withering_curse`, cura recebida −50% por 8 s; tag `curse`, que a Purificação remove.
  - Dardo Sepulcral: arcano 10, a cada 2,2 s.
- **Cavaleiro Espectral** (nv11, elite, `undead`, IA `ai.brute`): vida 4500.
  - Escudo Espectral: `status.spectral_shield`, devolve 30% do dano direto por 6 s; quebra com 8% da vida máxima em dano ou com uma interrupção.
  - Golpe Giratório: círculo de 2,4 com telegráfo de 1 s, físico 130.
  - Lâmina Espectral: físico 55.
  - Investida Espectral: linha de 8 com telegráfo de 1 s e avanço, físico 140.
- **Encontros** `encounter.dead_courtyard` (3 Guardas nv9 + Sacerdote nv10; Guerreiro + Clérigo) e `encounter.spectral_knight` (Cavaleiro nv11; Guerreiro + Arqueiro + Clérigo), em `map.broken_gate` (corredor estreito que abre para um pátio). Nenhuma arena nova.

**Core** (mecânica nova exigida pelos monstros):
- `StatModifiers.HealingTakenMultiplier` (`modifiers.healingTakenMultiplier`), aplicado no efeito `heal`.
- `StatusDefinition.Reflect` (`reflect.fraction`) e `StatusDefinition.Breaks` (`breaks.damageFraction`, `breaks.interrupt`).
  - O reflexo e a quebra por dano ficam numa reação de dano nova, `ReflectAndBreakReaction`. Dano vindo de status não é refletido, então não há rebote infinito.
  - O efeito `interrupt` agora também quebra os status com `breaks.interrupt` (`StatusManager.BreakOnInterrupt`).
  - `StatusInstance.DamageTaken`, que entra no hash e zera ao renovar.
- Perfil `ai.support` (`SupportProfile`): cura o aliado mais ferido (nunca a si mesmo), fica atrás dos protetores e ataca só com instantâneas.
- `ai.brute` ganhou a tag `ai.buff`: habilidade em si mesmo com o alvo a até `buffDistance`. O Ent não tem nenhuma, então nada muda para ele.
- **Interrupção de cura de monstro: confirmada.** O Golpe Pesado do Guerreiro cancela a cura do Sacerdote, e a cura não acontece (`RuinsMonsterTests.Dead_priest_heal_is_interrupted_by_the_warrior_heavy_strike`).
- 13 testes em `tests/Game.Core.Tests/RuinsMonsterTests.cs`.

**Testes de design** (`tests/Game.Design.Tests/RuinsDesignTests.cs`; relatório `tasks/reports/balance-v0.12.md`):
- **Pátio, par nv10:** 100% nos 5 pares testados (meta ≥ 80%).
- **Pátio, solo nv10:** média de 57% (meta 40–70%). Guerreiro 38%, Arqueiro 3%, Mago 89%, Clérigo 100% (devagar).
- **Focar o Sacerdote:** o par do encontro vai de 55,5 s a 43,4 s. Sozinho, com foco, as 4 classes vencem 100%. Para isso entrou a opção `focusFirst` nos bots.
- **Cavaleiro, grupo de 3 nv10–11 com incomuns:** média de 82% (meta 70–90%), em 75–95 s.

**Decisões e desvios.**
- **O Sacerdote não cura a si mesmo.** Curando-se, ele resistia a quem focava nele de longe: Arqueiro e Mago sozinhos ficavam em 10–33%. Assim, a lição "mate o Sacerdote primeiro" funciona para todas as classes.
- **Solo do Pátio medido com foco no mais próximo** (o padrão dos bots). Com foco no Sacerdote, o solo vence 100%; a meta de 40–70% vale para quem não prioriza.
- **A meta do Cavaleiro é a média de níveis 10 e 11.** Há um degrau grande: nível 10 fica em 48–80%, nível 11 em 100%. Com mais dano, o nível 11 cai para 70–90%, mas o nível 10 cai a 0–25% (balance-v0.12 §3).
- **Solo do Cavaleiro não é viável** (0% no nv11 incomum ou raro; 5% só para o Mago no nv14 raro). A meta de grupo de 3 (60–120 s) fixa a vida em 22 500, e um solo precisaria de ~3× o tempo sob o mesmo dano. Priorizei a meta testada (balance-v0.12 §4).
- **Fora das "Áreas que pode alterar"** (mínimo):
  - `data/schemas/game-data.schema.json` (campos novos de status);
  - `src/Game.Data/GameDataValidator.cs` (validação dos campos novos);
  - `src/Game.Site/Describe.cs` (texto de cura recebida, reflexo e quebra);
  - `src/Game.Bots/ConfigurableBot.cs` e `BotOptions.cs` (opção `focusFirst`, desligada por padrão; a tarefa prevê "estratégia nova no simulador");
  - `design/bestiario.md` (referência dos dados, como na T-340).

**Pendências.**
- **T-345:** mostrar a conjuração da cura do Sacerdote, a maldição e o Escudo Espectral (com a quebra) no cliente.
- **T-343:** posicionar o Pátio e a Torre Quebrada no mapa real e rever a linha de visão com o corredor.
- **Solo do Cavaleiro:** decidir se a meta "vencível por solo bem equipado" continua. Saídas possíveis: um Cavaleiro mais fraco no mundo aberto, ou dano mais concentrado em telegráfos.
- **Arqueiro no Pátio:** sozinho e sem foco, ele quase sempre perde (3%). Os 3 Guardas sem o Sacerdote ele vence (100%). Com o Sacerdote vivo, o Escudo Erguido corta 70% do dano frontal e a cura recupera o resto. Com foco no Sacerdote, vence 100%. Conferir no playtest se o jogador entende isso sem dica.
- **Interrupção:** só o Guerreiro tem interrupção hoje; a quebra do Escudo por interrupção vale só para ele.
- `design/habilidades.md` § Monstros não foi atualizado (fora das áreas, como na T-340).

