# T-009 — Efeitos de status e retorno decrescente

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** T-008
- **Libera:** T-012, T-014
- **Áreas que pode alterar:** `src/Game.Core/Status/`, registro de sistemas, `src/Game.Core/Definitions/` (regras globais), `src/Game.Data/` (carregar regras), `tests/`

## Contexto

Veneno, maldição, medo, lentidão, congelamento, Marca do Caçador, Guarda Firme e Bênção são status. O design exige que controle seja reduzido contra elites e bosses (`design/classes-v0.1.md`, regras de balanceamento).

## Requisitos

1. **Sistema de status**: aplicar, renovar ou empilhar conforme a definição; duração em ticks; expiração; remoção por tag com quantidade.
2. **Efeitos periódicos** (dano/cura ao longo do tempo) usando o registro de efeitos da T-008, com a fonte original preservada nos eventos.
3. **Modificadores** agregados (velocidade, dano recebido, dano causado) alimentando o `IModifierSource` da T-008. Regra de agregação definida e testada (multiplicativa recomendada).
4. **Modificador frontal**: redução de dano recebido apenas quando a fonte está dentro de um arco à frente do alvo (para o Guarda Esquelético e Guarda Firme). Parâmetros: ângulo e multiplicador.
5. **Flags**: `stunned` (não age, não move), `rooted` (não move), `silenced` (não usa habilidades exceto ataque básico). Implementar o `IActionGate` da T-008 e o bloqueio de movimento.
6. **Controle reduzido**: status com tag `control` têm duração multiplicada por rank do alvo e por retorno decrescente em aplicações repetidas dentro de uma janela. Valores em `data/rules.json` (novo arquivo, ex.: `{ "controlDurationByRank": { "normal": 1.0, "elite": 0.5, "boss": 0.25 }, "controlDiminishing": { "windowSeconds": 15, "steps": [1.0, 0.5, 0.25, 0.0] } }`). Adicionar a definição e o carregamento em `Game.Data`.
7. Efeitos `apply_status` e `remove_status` registrados.
8. Eventos: `StatusApplied`, `StatusRefreshed`, `StatusStacked`, `StatusExpired`, `StatusRemoved` (com quem removeu, para Fé/Purificação).

## Critérios de aceite

- Testes: renovar vs empilhar, expiração exata em ticks, DoT com fonte correta;
- stun impede habilidade e movimento; root só movimento; silence permite ataque básico;
- controle em boss dura 25% e retorno decrescente chega a imunidade;
- modificador frontal reduz só quando atacado pela frente;
- `./scripts/check.sh` passa.

## Resultado

_(preencher ao concluir)_
