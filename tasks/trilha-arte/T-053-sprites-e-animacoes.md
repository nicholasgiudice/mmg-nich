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

**Feito** (Windows, 2026-10-31). `dotnet build client/` sem avisos, `./scripts/check.sh` completo passa. Conferido no cliente com capturas dos 5 encontros e das 4 classes.

**O que mudou**
- `client/assets/art/sprites.json`: mapeamento definição (`class.*`, `monster.*`) → folha, escala inteira, pivô (pés) e animações por papel (`idle`, `walk`, `attack`, `cast`, `death`). Há layouts reaproveitáveis (`ulpc`, `lpc_spider`, `lpc_giant_rat`, `lpc_wolf`). Uma animação pode ter quadro e pivô próprios.
- `SpriteCatalog` (novo) lê o JSON com o `FileAccess` do Godot. Textura sem importação (editor ainda não aberto) é lida direto do PNG. Sprite sem textura fica de fora, e a unidade continua círculo.
- `EntityView` desenha o quadro da animação com o pivô no centro do círculo de colisão e filtro *nearest*. Sob os pés fica a sombra achatada com a cor da facção (ou cinza, se morto).
  - Prioridade das animações: morte (segura o último quadro) > ataque (uma vez a cada `AbilityExecuted`) > conjuração (`AbilityState.Cast`) > andar (posição mudou nos últimos 0,12 s) > parado.
  - Direção: 4, a partir do `Facing` interpolado.
  - Dano: piscar branco. Congelado: tom azulado.
  - Nome, barra de vida, status, marcas de controle, seleção e textos flutuantes continuam por cima. A altura vem de `HeadPx`, não mais do raio.
- `WorldView` liga o ataque ao evento. `Main` carrega o catálogo. `--circles` volta ao desenho antigo.
- Arte em `client/assets/art/` (~3 MB), com `CREDITS.txt` por pasta de monstro e `*-credits.csv` por personagem. As entradas também estão em `CREDITS.md`.

**Decisões**
- As folhas do gerador LPC foram exportadas à mão pelo designer. O navegador do agente não grava downloads, e a captura de tela altera os pixels.
- As folhas exportadas têm o layout padrão do LPC nas linhas 0–53 e, abaixo delas, as armas grandes em quadros de 128 px (espada) e 192 px (maça). A linha padrão de golpe sai sem a arma, então Guerreiro, Salteador e Esqueleto usam `slash_128`, e o Clérigo usa `slash_192`. O Arqueiro usa `shoot`, e o Mago usa `spellcast` para atacar e conjurar.
- Pivô medido nas folhas: humanoides (32, 60); rato (40, 50); aranha (32, 44); lobo (16, 52) de frente/costas e (32, 28) de lado.
- O "tomar dano" é o piscar branco (o `hurt` do LPC é a queda, usada na morte), como em `design/art-direction-v0.1.md` §5.
- A sombra no chão já mostra a facção, então o indicador de direção (linha branca) não é desenhado com sprite.

**Pendências / observações**
- A aranha pequena (raio 0,35) continua visualmente maior que o seu círculo. Fica para decisão de dados (raio) se incomodar no playtest.
- O Salteador usa "Disparo de Besta" no jogo, mas a folha dele tem espada. Se quiser, troque a arma no gerador por uma besta (`weapon=Crossbow`) e reexporte.
- A câmera continua em zoom 1×. Os sprites ficam legíveis, mas pequenos. Zoom 2× é uma decisão de câmera/HUD, fora desta tarefa.
- Os sprites ainda não aparecem nas telas de login e de criação de personagem (T-220).
