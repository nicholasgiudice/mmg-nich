# T-054 — Cliente: efeitos visuais de habilidades, áreas e telegráfos

- **Ambiente:** Windows
- **Modelo:** Leve
- **Depende de:** T-053
- **Libera:** T-055
- **Áreas que pode alterar:** `client/`, `CREDITS.md`

## Contexto

Telegráfos, áreas e impactos hoje são formas desenhadas (T-023). Com arte, ganham efeitos — sem perder a legibilidade que o playtest validou.

## Requisitos

1. Projéteis (flechas, projétil arcano), impactos (explosão, relâmpago, golpe), áreas (poça de veneno, teia, campo de relâmpagos, chuva de flechas) e escudos com arte dos pacotes.
2. Telegráfos continuam com contorno e preenchimento progressivo claros por cima de qualquer efeito.
3. Opção de intensidade (normal/reduzida) para quem achar poluído.

## Critérios de aceite

- na Matriarca continua dando para ver e desviar de todos os ataques telegrafados;
- `dotnet build client/` e `./scripts/check.sh` passam.

## Resultado

_(preencher ao concluir)_
