# T-053 — Cliente: sprites e animações de personagens e monstros

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** T-050
- **Libera:** T-054, T-055
- **Áreas que pode alterar:** `client/`, `CREDITS.md`

## Contexto

Hoje cada unidade é um círculo colorido (`EntityView`). A apresentação só lê o estado (posição, direção, conjuração, vida, eventos), então trocar por sprites não mexe em regra nem em rede.

## Requisitos

1. Sprites das 4 classes e dos monstros do protótipo (Rato, Lobo, Salteador, Guarda Esquelético, Aranhas, Matriarca) a partir dos pacotes da T-050.
2. Animações por estado: parado, andando (4 direções pela direção do movimento/`Facing`), atacando (`AbilityStarted`/`AbilityExecuted`), conjurando (`AbilityState.Cast`), tomando dano, morrendo.
3. Mantém o que já existe por cima do sprite: nome, barra de vida, ícones de status, marcas de controle, anel de seleção, números flutuantes.
4. Mapeamento classe/monstro → sprite em dados do cliente (não hardcoded por id em código).
5. Escala: o pé do sprite no centro do círculo de colisão; tamanho coerente com 32 px por tile.

## Critérios de aceite

- todos os encontros jogáveis com sprites animados;
- leitura do combate igual ou melhor que com os círculos (telegráfos e status visíveis);
- `dotnet build client/` e `./scripts/check.sh` passam.

## Resultado

_(preencher ao concluir)_
