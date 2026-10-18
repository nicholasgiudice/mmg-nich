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

Concluída em 2026-10-17 no Windows (Godot 4.7.2 .NET). Branch `task/T-028-cliente-previa-area`, a partir de `task/T-027-mana-e-mago` (integrar T-027 antes).

**Feito** (`client/`):
1. **Pré-visualização** (`Presentation/AimPreview.cs`): habilidades com área mirada (`circle_at_point`, `line` — Explosão Elemental, Campo de Relâmpagos, Armadilha) mostram, **enquanto a tecla está apertada**, o anel tracejado do alcance máximo e a forma atingida em ciano (diferente do laranja dos telegráfos inimigos). **A habilidade sai ao soltar a tecla** (`PlayerInput`, novo `justReleased`). As demais (alvo único, `self`, `circle_self`) continuam saindo no aperto e repetindo com a tecla mantida (T-026).
2. **Conjuração interrompida**: em `CastInterrupted`, texto flutuante na unidade ("Interrompido — você se moveu" para o jogador, "Interrompido"/"Impedido"/"Cancelado" nos demais casos, inclusive inimigos interrompidos pelo jogador); a barra de conjuração do HUD fica vermelha por 0,8 s com "<habilidade> — interrompido (você se moveu)".
3. **Mira limitada ao alcance** (`PlayerInput.ClampAim`): cursor além do alcance de uma área em ponto mira no ponto mais distante permitido, na direção do cursor (antes: "Fora de alcance"). Por isso a barra de habilidades não marca mais essas áreas como fora de alcance.
- `project.godot` regravado pelo editor do Godot no formato dele (mesmos atalhos, quebras de linha diferentes).

**Verificação:** captura do Mago segurando a tecla 4 (anel de alcance + círculo do Campo); `--demo-input=ability_1,ability_4` conjura a Explosão ao soltar com a mira puxada para o alcance (ratos a mais de 9 tiles) e recusa o Campo por falta de Mana; Clérigo andando com Cura Direta mostra "Interrompido — você se moveu". `dotnet build client/` sem avisos; `./scripts/check.sh` OK.

**Pendências:** não há como cancelar a área depois de apertar a tecla (soltar sempre conjura); um botão de cancelar (ex.: clique direito) pode entrar no próximo playtest.
