# T-014 — Conteúdo do protótipo em JSON

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** T-009, T-010, T-011, T-012
- **Libera:** T-016, T-024
- **Áreas que pode alterar:** `data/` (exceto `data/schemas/`, salvo correções), `tests/Game.Data.Tests/`

## Contexto

Leia todos os documentos em `design/` e a seção 5 de `tasks/README.md` (escopo do protótipo). O conteúdo é a tradução do design para dados; números são um primeiro chute que a T-016 vai ajustar.

## Requisitos

1. **Classes** (4): vida, velocidade, raio, recurso e regras de geração conforme `design/classes-v0.1.md` e ajustes de `design/class-map-validation-v0.1.md`.
2. **Habilidades**: ataque básico + 3 por classe (tabela da seção 5 do `tasks/README.md`). Cada uma deve ter um papel claro e um custo real:
   - Guerreiro: Golpe Pesado (dano alto + interrupção), Golpe Circular (área curta + ameaça), Guarda Firme (redução de dano frontal, lentidão própria, gera Vigor ao bloquear);
   - Arqueiro: Marca do Caçador (aumenta dano recebido pelo alvo, gera Foco ao acertar marcado), Recuo Rápido (dash para trás), Armadilha de Contenção (área que prende);
   - Mago: Explosão Elemental (área com custo alto de Mana), Prisão de Gelo (controle), Barreira Arcana (escudo próprio);
   - Clérigo: Cura Direta, Escudo Sagrado (escudo preventivo, gera Fé), Corrente Sagrada (dano sagrado forte contra `undead`/`corrupted`, cura limitada).
3. **Monstros**: Rato de Campo (`swarm`), Lobo Cinzento (`pack`), Salteador da Estrada (`ranged_kiter`), Guarda Esquelético (`guardian`, `undead`, guarda frontal, golpe pesado telegrafado), Matriarca da Floresta (boss, 3 fases conforme design), Aranha Menor e Aranha Grande (invocações).
4. **Status e áreas** necessários (veneno, sangramento, teia, lentidão, congelamento, marca, guarda, agressividade da matilha, frenesi).
5. **Arenas** (ASCII), uma por encontro, refletindo o design:
   - Campos (lobos): área aberta com poucos obstáculos;
   - Acampamento (lobo + salteador): obstáculos que o Arqueiro pode usar e que também bloqueiam sua visão;
   - Fazenda (ratos): espaço médio, rotas de aproximação;
   - Portão Partido (guarda): corredor e espaço lateral para flanquear;
   - Ninho da Matriarca: área central aberta, duas bordas livres, sem paredes que atrapalhem a leitura.
6. **Encontros**: os cinco cenários com posições iniciais.
7. Metas iniciais para orientar os números (a T-016 valida):
   - encontros solo duram entre 20 e 60 s com jogo correto;
   - Clérigo solo vence, mas 20–60% mais devagar que as outras classes;
   - Matriarca com grupo de 4 dura 2–4 minutos.

## Critérios de aceite

- `Game.Data` carrega `data/` sem erros de validação (teste automatizado que carrega a pasta real);
- cada habilidade da tabela existe e está ligada à classe certa;
- cada encontro referencia arena, classes e monstros existentes;
- `./scripts/check.sh` passa.

## Resultado

**Feito.** Conteúdo do protótipo em `data/` (mantido `rules.json`): `classes.json`, `abilities_{warrior,archer,mage,cleric,monsters}.json`, `statuses.json`, `areas.json`, `arenas.json`, `monsters.json`, `encounters.json`. 4 classes (ataque básico + 3 habilidades da tabela da seção 5), 7 monstros (Rato, Lobo, Salteador, Guarda Esquelético, Matriarca, Aranha Menor, Aranha Grande), 11 status, 3 áreas, 5 arenas ASCII, 5 encontros (`encounter.three_wolves`, `wolf_and_bandit`, `four_rats`, `skeleton_guard`, `forest_matriarch`).

**Correção em Game.Data (fora de `data/`, mínima).** `GameDataLoader.LoadDirectory` agora ignora arquivos cuja primeira pasta relativa é `schemas` (`IsSchemaFile`). Teste: `Loader_ignores_schemas_subfolder`. Sem isso `LoadDirectory("data")` tentava ler o JSON Schema como conteúdo.

**Testes.** `tests/Game.Data.Tests/PrototypeContentTests.cs`: carrega a pasta `data/` real com zero erros; tabela de habilidades por classe (básico + 3, ids existem, prefixo da classe); monstros e perfis de IA; os cinco cenários; referências de encontro (arena, classes, monstros).

**Decisões ao traduzir o design em números**
- Vigor (Guerreiro): +5 por acerto, +12% do dano recebido, +40% do dano recebido enquanto em Guarda Firme (`sourceHasStatusTag: guard`, "bloquear"), +15 por interrupção; decai 3/s só fora de combate. Golpe Pesado custa 30, Circular 25. Guarda Firme sem custo, recarga 14 s, arco frontal 180 graus x0,4 e lentidão própria 0,5 por 4 s. Golpe Pesado e Circular dão ameaça (sem Provocação no protótipo).
- Foco (Arqueiro): +4 por acerto a 4+ tiles ("distância segura"), +8 em alvo marcado (`targetHasStatusTag: mark`); regen 3/s. Marca = +25% de dano recebido por 8 s (custo 10); Armadilha (custo 25, área `on_enter_once`, dano 8 + prende 3 s, tag `control`); Recuo = dash para trás de 5 tiles, recarga 8.
- Mana (Mago): regen 5/s; básico custa 3 (baixo custo); Explosão 45 (raio 3, conjuração 0,6 s), Prisão de Gelo 25 (congela = `rooted` 2,5 s, tag `control`, sofre redução em elite/boss pelas `rules`), Barreira 30 (escudo 45 por 6 s).
- Fé (Clérigo): começa com 40, sem regen passiva. Gera por acerto (+5), +4 extra em alvo `undead`, 15% da cura efetiva e 15% do escudo. Cura Direta 20 (50 de cura, 0,8 s), Escudo Sagrado 15 (aliado, 45), Corrente Sagrada 25 (dano 26). Fraqueza do Guarda a sagrado: `resistances.holy = -0,25` (+25% de dano).
- Monstros: Lobo (`ai.pack`; `ai.rally` aplica `status.pack_aggression`: +30% dano, +15% velocidade por 8 s ao perder um membro); Rato (`ai.swarm`); Salteador (`ai.ranged_kiter`, preferido 7 tiles, recua 4); Guarda (`ai.guardian`: `ai.guard` -> `status.skeleton_guard` frontal 140 graus x0,3 por 3 s, `ai.heavy` com cone telegrafado de 1 s); Matriarca (4500 de vida, `ai.boss`, 3 fases: Caçada = investida telegrafada, mordida, poça de veneno, 2 aranhas; Teia Sufocante em 66% = Aranha Grande (elite) + teia (lentidão 0,5) + poças a cada 6 s; Frenesi em 33% = `status.frenzy` (+35% velocidade, +15% dano, +20% dano recebido), sem invocar, mordida com sangramento).
- Arenas: Campos 32x20 aberta com poucos obstáculos; Acampamento 32x20 com blocos que o Arqueiro usa e que bloqueiam visão; Fazenda 26x20 com casa central e cercas; Portão Partido 34x20 com corredor (4 de largura, x 1..12) abrindo para salão com pilares; Ninho 40x28 só com paredes de borda.
- Números calibrados no simulador (vida dos monstros alta em relação ao dano base para dar lutas de 20 s+). T-016 refina.

**Lacunas do Core (não alterei o Core; conteúdo simplificado)**
- Corrente Sagrada não cura o conjurador: habilidade com dano + cura vai toda para inimigos (a facção é decidida pela habilidade). "Cura limitada" ficou de fora; só dano forte + Fé extra contra `undead`.
- Dano extra contra `undead`/`corrupted` é via `resistances.holy` negativa do monstro, não por habilidade; regra de recurso (`targetTag`) aceita uma tag só.
- Armadilha não tem tempo de armar; "três trechos de teia no chão" fixos não existem (áreas só nascem por habilidades): a teia vem da habilidade da Matriarca na fase 2.
- "Reduz a duração de controles" no Frenesi não é possível por status; já vale o multiplicador de boss (`rules.control`).
- Altar do Guarda Esquelético (bênção) não modelado; sem Purificação no protótipo, veneno só expira.

**Resultados do simulador** (`--runs 100`, seed 1, jitter padrão)

| Encontro | Estratégia | Vitória | Tempo médio (s) |
|---|---|---|---|
| three_wolves (Guerreiro) | rotation | 100% | 21,0 |
| wolf_and_bandit (Arqueiro) | kite | 100% | 25,5 |
| wolf_and_bandit (Arqueiro) | rotation | 0% (cercado; punido por não se mover) | - |
| four_rats (Mago) | rotation | 83% | 17,1 |
| four_rats (Mago) | kite | 100% | 18,1 |
| skeleton_guard (Clérigo) | rotation | 100% | 26,3 |
| forest_matriarch (grupo de 4) | kite (todos) | 91% | 202,5 (p50 182, p90 303) |
| forest_matriarch (grupo de 4) | kite, Guerreiro e Clérigo em rotation | 68% | 329 |
| forest_matriarch (grupo de 4) | rotation (parados) | 0% | - |

Clérigo vs outras classes no mesmo encontro (ensaio com cópia dos dados fora do repositório): Guerreiro 21,0 s, Arqueiro 29,2 s (kite), Mago 32,7 s (kite). O Clérigo (26,3 s) fica 25% mais lento que o Guerreiro e ~22% mais lento que a média das classes em seus próprios encontros (21,5 s); mais rápido que Arqueiro/Mago no Guarda por causa do ponto fraco sagrado (a rever na T-016). Mago solo (17 s) fica um pouco abaixo da meta de 20 s.

**Pendências para a T-016:** fonte de perigos para `+dodge`, calibração fina (Mago curto; Arqueiro em `rotation` não vence, por design do bot), Matriarca com bots mais realistas, lacunas acima.
