# T-007 — Movimento, colisão, linha de visão e pathfinding

- **Ambiente:** VPS
- **Modelo:** Forte
- **Depende de:** T-006
- **Libera:** T-008, T-020
- **Áreas que pode alterar:** `src/Game.Core/Spatial/`, sistema de movimento em `src/Game.Core/Simulation/`, `tests/Game.Core.Tests/`

## Contexto

Posicionamento é central no design: o Arqueiro usa obstáculos, ratos cercam, o Guerreiro posiciona o boss, a Matriarca reduz o espaço com teias. A física do Godot **não** participa; tudo é do Core para que cliente e servidor concordem.

## Objetivo

Mapa em grade, movimento com colisão, consultas espaciais, linha de visão e pathfinding.

## Requisitos

1. **`GridMap`**: largura, altura, células bloqueadas; `GridMap.FromAscii(string[] linhas)` (`#` bloqueado, `.` livre). 1 célula = 1 unidade de mundo.
2. **Movimento com colisão**: entidades são círculos; o sistema de movimento (substitui o da T-006) aplica `MoveIntent` com velocidade × dt, colide com células bloqueadas e **desliza** ao longo de paredes. Sem atravessar paredes nem com velocidade alta (sub-steps se necessário).
3. **Separação entre entidades**: empurrão suave para que entidades não se sobreponham por completo (simples; não precisa ser física real).
4. **Deslocamento forçado**: função para mover uma entidade até uma distância numa direção, parando em parede (usada por dash e knockback na T-008).
5. **Consultas espaciais**: entidades num raio, num cone (origem, direção, ângulo, alcance), num retângulo orientado (linha), mais próxima de um ponto. Força bruta é aceitável (< 200 entidades), atrás de uma interface para trocar depois.
6. **Linha de visão**: raycast em grade (DDA) entre dois pontos; paredes bloqueiam.
7. **Pathfinding**: A* em 8 direções sem cortar quinas de parede, retorna waypoints; suavização simples de caminho (remover pontos com linha de visão direta).
8. Direção (facing) atualizada pelo movimento, a menos que outra regra defina (ex.: mirar).

## Fora do escopo

Habilidades, IA (só fornece as ferramentas).

## Critérios de aceite

- Testes: deslizar em parede, não atravessar parede em alta velocidade, cantos, separação de duas entidades sobrepostas;
- testes de cone, raio, retângulo e linha de visão;
- A* encontra caminho em labirinto pequeno e retorna vazio quando não há caminho;
- determinismo preservado (teste de hash da T-006 continua passando);
- `./scripts/check.sh` passa.

## Resultado

_(preencher ao concluir)_
