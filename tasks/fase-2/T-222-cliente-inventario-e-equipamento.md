# T-222 — Cliente: inventário, equipamento e saque

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** T-204, T-212
- **Libera:** T-230
- **Áreas que pode alterar:** `client/`

## Contexto

Itens, equipamento e queda vêm da T-204. O cliente mostra e manda intenções; não decide nada.

## Requisitos

1. Janela de inventário (grade) e de equipamento (6 espaços) com arrastar ou clique para equipar/desequipar; tooltip com estatísticas, raridade (cor) e comparação com o equipado.
2. Item no chão visível no mundo com a cor da raridade; pegar com clique ou tecla quando em alcance.
3. Mensagens de recusa claras (classe não usa, inventário cheio).

## Critérios de aceite

- pegar, equipar e ver a estatística mudar online;
- `dotnet build client/` e `./scripts/check.sh` passam.

## Resultado

_(preencher ao concluir)_
