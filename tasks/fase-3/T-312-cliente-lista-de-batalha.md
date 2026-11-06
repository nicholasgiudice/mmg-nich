# T-312 — Cliente: lista de batalha

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** —
- **Libera:** —
- **Áreas que pode alterar:** `client/` (inclusive o Input Map em `project.godot`)

## Contexto

O playtest da fase 2 (`tasks/reports/playtest-v0.2.md`, P1) mostrou que é difícil selecionar, clicando no mapa, o monstro que você quer atacar enquanto ele se move. O pedido é uma **lista de batalha**: um painel com todos os monstros que estão na tela, onde se clica para escolher o alvo.

O cliente já recebe do servidor as entidades dentro da área de interesse. A lista é só outra forma de escolher o alvo e usa a mesma intenção de alvo que já existe (`PlayerInput`).

## Requisitos

1. **Painel "Batalha"** (canto direito, abaixo do nome da zona), com uma linha por monstro **visível na tela** (dentro da área da câmera), vivo. Cada linha mostra:
   - nome e nível;
   - barra de vida;
   - uma marca quando o monstro está mirando o próprio jogador. Esse alvo já vem replicado, porque o cliente o mostra hoje. Se não vier, deixe a marca de fora e registre no Resultado.
2. Ordem: o alvo atual primeiro, depois por distância. A ordem não pode "pular" a cada frame: reordenar no máximo 4 vezes por segundo, ou só quando a diferença de distância for maior que 1 tile.
3. **Clique na linha** seleciona o monstro como alvo, como o clique no mapa. Passar o mouse sobre a linha destaca o monstro no mapa (anel ou contorno).
4. **Tecla para trocar de alvo:** Tab avança para o próximo da lista e Shift+Tab volta. Usar o Input Map e `InputActions` (como a T-081 e a T-085).
5. Elites e chefes têm destaque (cor do nome ou ícone). Monstros de outro grupo de aggro não precisam de diferença.
6. O painel pode ser escondido e mostrado com uma tecla (proposta: B), e o estado fica gravado em `user://settings.cfg`. Com mais de 10 monstros, o painel rola.
7. Com a lista vazia, mostrar só o título (ou esconder o corpo do painel).

## Critérios de aceite

- contra o servidor local (`--world --dev-login`), nos Campos: a lista mostra os monstros da tela, o clique numa linha seleciona e o ataque básico vai nesse alvo; Tab passa pelos monstros;
- capturas de tela com a lista ao lado de uma matilha em movimento;
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
