# T-062 — Dados: todos os níveis e as habilidades que faltam

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** T-060, T-061
- **Libera:** T-063, T-064
- **Áreas que pode alterar:** `data/`, `tests/Game.Data.Tests/`, `tests/Game.Core.Tests/` (testes que leem os dados reais)

## Contexto

Passar para `data/*.json` as tabelas de `design/skills-v0.1.md`: os NH 2–5 das 16 habilidades normais e os NH 1–3 das 4 assinaturas, mais as habilidades do MVP que ainda não existem no jogo.

## Requisitos

1. `ranks` de todas as habilidades normais conforme as tabelas (valores e comportamentos).
2. Habilidades novas com todos os níveis: **Chuva de Flechas** (Arqueiro, área com telegráfo aliado) e as assinaturas **Muralha Viva**, **Flecha Devastadora**, **Cataclismo Arcano** e **Milagre**.
3. Listas das classes com as 5 habilidades do MVP (assinatura marcada como tal); a barra do protótipo continua mostrando só as habilidades com NH ≥ 1 do personagem.
4. Teste de conteúdo: para cada habilidade, o valor no "perfil nível 1" de cada NH bate com a coluna da tabela do documento (tolerância de arredondamento).

## Critérios de aceite

- todas as habilidades do MVP com todos os NH nos dados, validados;
- `./scripts/check.sh` completo passa (testes de design ainda rodam com NH1).

## Resultado

_(preencher ao concluir)_
