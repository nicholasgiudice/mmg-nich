# T-086 — HUD: quadros do personagem e do alvo no tema pedra e ouro

- **Ambiente:** Windows
- **Modelo:** Leve
- **Depende de:** kit de interface pedra e ouro (`client/assets/art/ui/`, `tasks/reports/interface/kit-pedra-ouro.md`)
- **Libera:** —
- **Áreas que pode alterar:** `client/scripts/Hud/`, `tasks/reports/interface/`

## Contexto

O designer aprovou em 2026-11-06 o kit de interface pedra e ouro, gerado no PixelLab, e pediu que as barras de vida e Mana do personagem passassem a usá-lo. O resto do HUD continua desenhado por código (`HudTheme`).

## Requisitos

1. O quadro do personagem (vida, recurso, nível e XP) e o quadro do alvo usam o painel de pedra (`panel.png`) como 9-slice, em 1×.
2. As barras desses quadros ganham contorno dourado.
3. Os quadros compactos (aliados no modo de arena) continuam como estão; o painel do grupo da T-311 usa o kit por conta própria.
4. Sem a textura, o quadro volta ao painel escuro de antes.

## Critérios de aceite

- captura no jogo com o quadro do personagem e o do alvo;
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Resultado

**Feito** (Windows, 2026-11-06).
- `HudTheme.StonePanel()`: `StyleBoxTexture` de `res://assets/art/ui/panel.png` com margens de textura de 22 px e margens de conteúdo de 24 (laterais), 21 (topo) e 24 (base), para o nome e as barras não encostarem na pedra. Sem a textura, devolve o `StyleBoxFlat` escuro de antes.
- `HudBar.BorderColor` (preto por padrão) e `HudTheme.BarBorderGold`: as barras dos quadros de pedra têm contorno dourado.
- `UnitFrame`: os quadros não compactos (personagem e alvo) usam o painel de pedra; os compactos seguem escuros.
- O quadro fica maior que antes (a borda de pedra tem ~22 px); os botões Personagem, Habilidades e Inventário e o alvo já se posicionam pelo tamanho do quadro, então nada mais mudou de lugar.
- Captura: `tasks/reports/interface/t086-after.png` (Guerreiro contra Lobo e Salteador, Lobo como alvo).

**Pendências:** os botões Personagem, Habilidades e Inventário, a barra de habilidades, a lista de batalha, o log e as janelas continuam no estilo antigo. Levá-los ao kit é outra tarefa, depois da T-311 (que também mexe no `Hud.cs`).
