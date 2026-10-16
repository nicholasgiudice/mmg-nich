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

**Feito:** pasta `src/Game.Core/Spatial/` (`GridMap`, `Collision`, `Pathfinder`, `SpatialQuery`, `ForcedMovement`) e `MovementSystem` reescrito (mesma linha em `SystemOrder.Movement`). 25 testes novos em `tests/Game.Core.Tests/SpatialTests.cs`; `./scripts/check.sh` passa (42 testes Core); teste de hash/determinismo da T-006 intacto.

**Configurar o mapa (T-008):** `new WorldBuilder(seed).WithMap(gridMap)` ou `WorldBuilder.CreateDefault(seed, gridMap)`. Leitura: `world.Map` (`GridMap?`; null = plano aberto sem paredes, comportamento antigo). O mapa é estático e não entra no hash/snapshot. `GridMap.FromAscii("#..", ...)` ou `new GridMap(w, h, (x,y) => bloqueado)`. Linha 0 = y 0; célula (x,y) cobre [x,x+1)x[y,y+1); fora dos limites = bloqueado.

**API pública (namespace `Game.Core.Spatial`)**
- Consultas: `world.Spatial` (`ISpatialQuery`, força bruta, trocável): `InRadius(center, r, filter?)`, `InCone(origin, dir, anguloTotalRad, alcance, filter?)`, `InOrientedRect(origin, dir, comprimento, largura, filter?)`, `Nearest(point, maxDist?, filter?)`. Só entidades vivas com `Transform`; o raio do `Collider` conta como corpo (acerta se o corpo toca a área); resultados em ordem de id; `filter` é `Func<Entity,bool>`.
- Linha de visão: `map.HasLineOfSight(a, b)`, `map.HasLineOfSight(a, b, radius)` (corpo grosso: 3 raios), `map.Raycast(a, b, out hit)` (DDA; passar exatamente por um vértice verifica as duas células laterais, conservador).
- Pathfinding: `Pathfinder.FindPath(map, start, goal, radius = 0, smooth = true)` retorna waypoints sem o ponto inicial, terminando exatamente em `goal`; vazio se sem caminho ou início/destino bloqueado. A* 8 direções, sem cortar quinas, desempate determinístico; suavização por LoS (com `radius`). Não considera outras entidades.
- Deslocamento forçado: `ForcedMovement.Displace(world, entity, direction, distance)` retorna distância percorrida; para na parede (não desliza), ignora outras entidades, não muda facing; sem mapa move livre. Usar para dash/knockback; a separação entre entidades corrige sobreposições nos ticks seguintes.
- Baixo nível: `Collision.MoveCircle(map, pos, radius, delta)` (sub-steps, desliza), `Collision.PushOut(map, pos, radius)`.

**Movimento:** `MovementSystem` aplica `Movement.Direction * Speed * dt` com sub-steps (<= 0.25 tile ou metade do raio), resolve colisão círculo x células (push-out pelo ponto mais próximo, o que produz o deslize). Separação: pares sobrepostos (entidades vivas com `Collider`) corrigem `0.5 * sobreposição` por tick, metade para cada, sem empurrar para dentro de parede; posições idênticas separam em x (menor id para -x). Facing = direção da intenção ao andar, a menos que `Movement.LockFacing = true` (componente `Movement` ganhou esse campo, incluído em Clone e hash; T-008 deve ligá-lo ao mirar).

**Decisões/desvios:** alterei fora das áreas listadas, o mínimo necessário: `World` (propriedades `Map` e `Spatial`), `WorldBuilder` (`WithMap`, parâmetro `map` em `CreateDefault`) e `Movement` em `Components.cs`. Entidades sem `Collider` colidem com paredes como ponto (raio 0) e não participam da separação. Raio de colisão >= 0.5 não passa por corredores de 1 tile (PushOut limitado a 4 passes). Pendência: nenhuma.

