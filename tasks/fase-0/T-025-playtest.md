# T-025 — Playtest do protótipo

- **Ambiente:** Windows
- **Modelo:** Humano (um agente leve pode organizar as anotações depois)
- **Depende de:** T-018, T-022, T-023, T-024
- **Libera:** T-030

## Objetivo

Responder a pergunta da fase 0: **o combate básico é divertido e cada classe joga de modo diferente?** (`docs/vision-mvp-v0.1.md`, critérios de sucesso).

## Roteiro

1. Jogar cada encontro solo com a classe principal, pelo menos 3 vezes.
2. Jogar cada encontro solo com uma classe que **não** é a principal.
3. Jogar a Matriarca com cada uma das quatro classes (bots no resto).
4. Se possível, convidar 1–3 pessoas para jogar sem explicação prévia.

## Perguntas por encontro

- Eu sabia por que tomei dano?
- Tive pelo menos duas formas válidas de vencer?
- Alguma habilidade foi inútil ou obrigatória?
- Precisei me mover? O recurso importou?
- Foi divertido? O que mais irritou?

Ver também as "Falhas de design" de cada encontro em `design/combat-encounters-v0.1.md`.

## Entrega

`tasks/reports/playtest-v0.1.md` com as respostas, uma nota de 1 a 5 por encontro e por classe, e a lista dos 5 problemas mais importantes.

**Decisão ao final:** seguir para a fase 1, ou fazer mais uma rodada de ajustes na fase 0 (nesse caso, criar novas tarefas T-017+ a partir dos problemas).

## Resultado

Concluída em 2026-10-17. Relatório: `tasks/reports/playtest-v0.1.md`.

**Roteiro:** jogado pelo designer no Windows, com as classes nos encontros solo e na Matriarca (com bots no resto do grupo). Sem jogadores externos nesta rodada. Notas de 1 a 5 não foram atribuídas (retorno qualitativo).

**O que o playtest gerou:** T-019 (monstros mais lentos, dano das classes +50%), T-026 (ataque contínuo, Espaço no alvo do Tab), T-027 (Mana para todas as classes, Campo de Relâmpagos, Explosão instantânea) e T-028 (prévia de área, mira limitada ao alcance, aviso de interrupção). Avaliação final do designer: "agora está legal".

**Decisão:** **fase 0 aprovada como base do combate**; sem nova rodada de ajuste fino de números na fase 0. As habilidades serão **reformuladas** junto com atributos e equipamentos. Próximos passos: **T-031** (design de progressão: atributos, equipamento, fórmulas e diretrizes das habilidades) e **T-030** (planejar a fase 1, rede), que podem andar em paralelo; reformulação e rebalanceamento das habilidades na fase 2, com itens e equipamento.
