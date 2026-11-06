# T-330 — Mapas maiores: Cidade-Fortaleza e Campos da Vigília

- **Ambiente:** Windows (Tiled)
- **Modelo:** Humano + Médio
- **Depende de:** T-331
- **Libera:** T-342, T-343
- **Áreas que pode alterar:** `data/maps/city.tmj`, `data/maps/vigil_fields.tmj`, `data/maps/matriarch_lair.tmj` (só o portão de volta), `data/zones.json`, `data/maps/tilesets/`, `client/assets/art/tiles/` (tilesets novos, com créditos), `CREDITS.md`, `tests/Game.Design.Tests/FieldsDesignTests.cs` e `tests/Game.Sim.Tests/ZoneMapTests.cs` (só metas e contagens), `tasks/reports/`

## Contexto

O playtest da fase 2 pediu **mapas bem maiores** (`tasks/reports/playtest-v0.2.md`, P6). Hoje a Cidade tem 40×30 tiles e os Campos têm 96×64. Com a câmera em 1× e tela cheia (1920×1080, tiles de 32 px), cabem cerca de 60×34 tiles na tela, então os Campos têm menos de dois "telas" de largura.

Depois da T-331, portões, surgimentos e NPCs ficam nos objetos do próprio mapa. Por isso, aumentar o mapa é trabalho só no Tiled. As convenções estão em `design/map-conventions-v0.1.md`, e a sensação de cada área em `design/first-map-v0.1.md` §1–§2.

## Requisitos

1. **Cidade:** cerca de **80×60**. Mantém os seis prédios com os NPCs, a praça central e o portão leste. Reserva os portões **norte** (Floresta Baixa) e **oeste** (Ruínas). Nesta tarefa eles ficam fechados (parede ou portão sem `transitions`): a T-342 e a T-343 os abrem. O portão sul fica fechado (`first-map-v0.1.md`).
2. **Campos:** cerca de **200×140**, com as quatro subáreas de hoje (Estrada Leste, Fazenda Abandonada, Clareira dos Caçadores, Acampamento dos Salteadores) maiores e mais espaçadas. A estrada liga tudo, e a cidade continua visível no começo (muralha na borda oeste).
3. **Densidade:** mais pontos de surgimento, para o ritmo de caça não cair com as distâncias maiores. Grupos a ≥ 12 tiles entre si e da chegada (teste da T-215). Os níveis continuam 1–4 por subárea.
4. **Ritmo:** o modelo de ritmo (`FieldsDesignTests`) assume 60 s entre grupos. Medir, com bots `--roam` ou à mão, o tempo médio para andar de um grupo ao próximo. Se passar de 60 s, aumentar a densidade em vez de mudar a meta. Registrar a medição.
5. **Covil da Matriarca:** continua ligado aos Campos até a T-342, que o muda para a Floresta. Ajuste o portão de volta, se a borda leste mudar.
6. Desempenho: rodar 16 bots `--roam` no mundo maior por 60 s e anotar o tick médio, o p99 e os bytes por jogador (tem que continuar abaixo de 2 ms em média).
7. O designer aprova os dois mapas antes do commit final (como na T-055).

## Critérios de aceite

- `ZoneMapTests`: todo surgimento, NPC e portão alcançável a pé a partir do nascimento;
- `FieldsDesignTests` passando com a nova densidade;
- medições de ritmo e de desempenho no Resultado;
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
