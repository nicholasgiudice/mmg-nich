# T-013 — Log de combate e resumo de morte

- **Ambiente:** VPS
- **Modelo:** Leve
- **Depende de:** T-008
- **Libera:** T-022
- **Áreas que pode alterar:** `src/Game.Core/CombatLog/`, `tests/`

## Contexto

Critério de design repetido em todos os documentos: "o jogador deve entender por que tomou dano ou perdeu o combate".

## Requisitos

1. Consumidor de eventos que produz linhas em português com nomes das definições, por exemplo:
   - "Lobo Cinzento usou Mordida em Você: 12 de dano (4 absorvidos)";
   - "Você foi atordoado por Golpe Pesado (2,0 s)";
   - "Matriarca da Floresta entrou na fase 2".
2. Cada linha tem tick, categoria (dano, cura, status, recurso, fase, morte) e ids envolvidos, para o cliente filtrar e colorir.
3. **Resumo de morte**: ao morrer, as últimas N entradas (padrão 10 ou últimos 5 segundos) que afetaram a entidade, com dano total por fonte.
4. Novos tipos de evento das tarefas 009–012 devem ser fáceis de adicionar (formatador por tipo). Formatar os que existirem quando esta tarefa rodar; registrar os que ficaram para depois.

## Critérios de aceite

- Testes com eventos sintéticos para cada formato;
- resumo de morte com agregação por fonte;
- `./scripts/check.sh` passa.

## Resultado

_(preencher: eventos ainda sem formatação, se houver)_
