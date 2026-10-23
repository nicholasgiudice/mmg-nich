# T-202 — Dados: escala de todas as habilidades e NH

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** T-201
- **Libera:** T-206
- **Áreas que pode alterar:** `data/abilities_*.json`, `data/classes.json`, `tests/Game.Data.Tests/`, `design/skills-v0.1.md` (só se encontrar erro na tabela)

## Contexto

`design/skills-v0.1.md` dá cada NH como fórmula (ex.: Golpe Pesado NH1 = `dano 15 + 1 × ARMA + 2,5 × FOR`). Hoje os dados guardam só o resultado no perfil nível 1 (51). Com a T-201 os efeitos aceitam `scaling`.

## Requisitos

1. Reescrever **todas** as habilidades das quatro classes (ataques básicos, as 4 normais e a assinatura de cada uma, em todos os NH) como `base` + `scaling`, copiando as fórmulas da tabela. Habilidades de monstros, áreas e status ficam fixas.
2. A Marca do Caçador escala com DES (bônus `16% + 0,75% × DES`, teto 60%/65%): se o formato de status não permitir escala, registrar a decisão (ex.: status por faixa de DES) no Resultado.
3. Atualizar o teste de conteúdo da T-062 (`SkillRankTableTests`) para calcular o "perfil nível 1" com o perfil inicial de cada classe e a arma inicial, e conferir contra as tabelas do documento.

## Critérios de aceite

- valores no perfil nível 1 idênticos aos de hoje (teste de conteúdo e testes de design passam sem mudar);
- nenhuma habilidade de classe com dano/cura/escudo fixo;
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
