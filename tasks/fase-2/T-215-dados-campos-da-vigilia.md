# T-215 — Dados: Campos da Vigília (surgimentos e níveis)

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** T-205, T-214
- **Libera:** T-230
- **Áreas que pode alterar:** `data/`, `tests/Game.Data.Tests/`, `tests/Game.Design.Tests/`

## Contexto

`design/first-map-v0.1.md` §2: Estrada Leste (poucos inimigos), Fazenda Abandonada (ratos e lobos), Clareira dos Caçadores (respawn maior de lobos), Acampamento dos Salteadores (arqueiro atrás de corpo a corpo, prioridade de alvo). Níveis 1–4.

## Requisitos

1. Pontos de surgimento com monstro, quantidade, raio de patrulha e tempo de respawn para cada subárea.
2. Densidade que deixe um jogador solo subir do nível 1 ao 4–5 nos Campos em ~1 hora (curva de XP da T-203).
3. Teste de design com o simulador: perfil esperado de cada nível limpa um grupo típico de cada subárea com ≥ 85%.

## Critérios de aceite

- dados validados e teste de design passando;
- `./scripts/check.sh` completo passa.

## Resultado

**Feito** (VPS, 2026-10-28). `./scripts/check.sh` completo passa: 83 testes de design (21 novos) em cerca de 4 min 37 s. Relatório: `tasks/reports/balance-v0.8.md`.

**Dados**
- `data/zones.json`, `zone.vigil_fields`: 11 pontos de surgimento em 8 grupos, cada um com subárea (`area`), monstro, quantidade, raio, nível e renascimento:
  - Estrada Leste: 2 grupos de 2 Ratos nv1;
  - Fazenda Abandonada: 2 Ratos nv1 + 1 Lobo nv2, e 2 Lobos nv2;
  - Clareira dos Caçadores: 2 matilhas de 3 Lobos nv3, renascimento de 40 s;
  - Acampamento dos Salteadores: 2 grupos com Lobo nv4 na frente e Salteador nv4 atrás.
- `data/arenas.json`: mapa provisório dos Campos redesenhado (cerca maior na Fazenda, paliçada no Acampamento) para os grupos ficarem a ≥ 12 tiles entre si e da chegada.
- Campo novo `area` no ponto de surgimento (`MonsterSpawnDefinition.Area` e schema). O "raio de patrulha" do requisito é o `radius` de surgimento: os monstros não patrulham (a IA atual só fica parada ou persegue).

**Testes**
- `tests/Game.Design.Tests/FieldsDesignTests.cs`:
  - **perfil esperado do nível do grupo contra cada grupo típico**, nas 4 classes: ≥ 85%. Hoje dá 100% nos 20 lotes. Os encontros são montados do próprio `zones.json`, sem duplicar dados;
  - **ritmo solo**: com o modelo do relatório (§3), 1 hora termina no nível 4–5. Hoje: nível 4 em 26 min, 5 em 43, 6 em 75.
- `tests/Game.Data.Tests/ZoneLoaderTests.cs`: as 4 subáreas existem; grupos diferentes a ≥ 12 tiles entre si e da chegada.

**Decisões**
- **Mago com `kite` nos Campos.** Parado (`rotation`), o perfil esperado perde para 2 Lobos do mesmo nível (3–7%). Fugindo, vence 100%. Os testes antigos continuam com `rotation`.
- **Grupos pequenos** (2–3 monstros) porque nos níveis 1–4 o personagem tem só 1 a 4 pontos de habilidade. O Acampamento ficou com 1 lobo + 1 salteador: com 2 lobos o Mago vence só 43%.
- **Intervalo de 60 s por grupo no modelo de ritmo** (andar, saque e recuperar sem descanso nos Campos): é premissa, não medida, e o relatório mostra a sensibilidade (40 s → nível 4 em 20 min; 90 s → 36 min).

**Fora das áreas da tarefa:** `src/Game.Core/Definitions/Zones.cs` (campo `Area`), `data/schemas/game-data.schema.json` (o mesmo campo) e `tests/Game.Sim/ContentLoading.cs` (`Zones = db.Zones`, para o simulador ver as zonas).

**Pendências**
- **T-230 (playtest):** medir o tempo real entre lutas e as idas à Cidade, e conferir as distâncias com o mapa e os obstáculos reais.
- **T-055:** os mapas do Tiled devem manter as distâncias entre os grupos.
- Com vários jogadores na mesma área, o renascimento vira o limite (relatório §4).
