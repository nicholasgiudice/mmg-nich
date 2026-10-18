# T-028 — Cliente: pré-visualização de área e aviso de conjuração interrompida

- **Ambiente:** Windows
- **Modelo:** Leve
- **Depende de:** T-026, T-027
- **Libera:** nova rodada de playtest (T-025)
- **Áreas que pode alterar:** `client/`

## Contexto

No playtest (2026-10-17) a Explosão Elemental "parecia corpo a corpo": ela cai no ponto do cursor (se o mouse está perto do Mago, explode perto dele) e, por ter conjuração, era cancelada ao andar sem aviso visível (só no log).

## Requisitos

1. **Pré-visualização**: enquanto o jogador segura uma tecla de habilidade com alvo em área (`circle_at_point`, `circle_self`, `line`), desenhar no chão a forma que será atingida (posição do cursor limitada ao alcance, ou em volta do jogador) e um anel do alcance máximo. Cor discreta, diferente dos telegráfos inimigos.
2. **Aviso de conjuração interrompida**: em `CastInterrupted` do jogador, texto flutuante ("Interrompido — você se moveu", "Interrompido") e a barra de conjuração do HUD some com destaque.
3. Mira de área: se o cursor está além do alcance, mirar no ponto mais distante dentro do alcance (na direção do cursor) em vez de ser recusado "Fora de alcance".

## Critérios de aceite

- é possível ver onde a área vai cair antes de soltar a habilidade;
- conjuração interrompida é visível sem abrir o log;
- `dotnet build client/` e `./scripts/check.sh` passam.

## Resultado

_(preencher ao concluir)_
