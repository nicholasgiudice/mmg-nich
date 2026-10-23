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

_(preencher ao concluir)_
