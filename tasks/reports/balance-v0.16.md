# Relatório de balanceamento v0.16 (T-342): Floresta Baixa

A área de nível 5–8, ao norte da Cidade. Design: `design/first-map-v0.1.md` §3. Mapa: `data/maps/lower_forest.tmj`. Testes: `tests/Game.Design.Tests/ForestZoneDesignTests.cs` (design) e `tests/Game.Sim.Tests/ZoneMapTests.cs` (percurso e espaçamento). Imagens: `tasks/reports/mapas-t342/`.

## 1. Mapa

O mapa tem 200 × 160 tiles de mata fechada: árvores com copas na camada `above` sobre arbustos que bloqueiam a passagem. Entre elas há trilhas estreitas, com 3 a 4 tiles de largura, ligando clareiras. A Cidade fica ao sul: a borda sul do mapa é a muralha norte da Cidade, com o portão de 2 tiles. É o mesmo padrão dos portões oeste e leste.

| Subárea | Onde | Nível | Grupos |
|---|---|---|---|
| **Trilha dos Caçadores** | do portão até a encruzilhada, no centro, e uma volta pelo leste | 5–6 | 6 grupos de 2: Aranhas nv5 e nv6 e um par de Fadas nv6 |
| **Bosque das Teias** | oeste | 5–6 | 7 grupos de 2 ou 3 Aranhas e a **Aranha Grande** nv6 (elite) no fundo, ao noroeste, com renascimento de 3 min. Há poças escuras (veneno) no chão e mais árvores secas. |
| **Clareira da Névoa** | norte | 6–7 | 7 pares de Fadas e o **Ent Corrompido** nv7 na clareira grande, com renascimento de 2 min. As árvores são de copa redonda. |
| **Ninho da Matriarca** | nordeste | 7 | 2 grupos de Aranhas diante de uma encosta de pedra com trepadeiras, onde fica a boca de uma caverna. A caverna é o portão para o Covil da Matriarca. |

- Os grupos ficam a pelo menos 12 tiles entre si (teste `Forest_groups_are_at_least_12_tiles_apart`).
- Ida e volta do Covil:
  - entrar na caverna leva ao Covil (`zone.matriarch_lair`, o mapa de antes), pela chegada `from_forest`;
  - o portão oeste do Covil volta para diante da caverna (`from_lair`).
- O portão do Covil nos Campos saiu. A estrada dos Campos agora acaba no campo, antes da borda leste, e a sebe fechou a borda.

## 2. Método

- Cada grupo típico (pontos da mesma subárea a até 4 tiles) vira um encontro na arena aberta do protótipo, como nos Campos e nas Ruínas.
- O jogador enfrenta o grupo sozinho, no perfil esperado do nível do grupo, nas 4 classes.
- Estratégias com `+dodge`, como na T-340.
- Limite de 120 s por luta. Lotes de 100 seeds.

## 3. Resultados

| Grupo | Vitória (4 classes) | Tempo médio |
|---|---|---|
| 2 Aranhas nv5 | 100% | 2–13 s |
| 2 Aranhas nv6 | 100% | 2–13 s |
| 3 Aranhas nv5 / nv6 | 100% | 5–20 s |
| 2 / 3 Aranhas nv7 (Ninho) | 100% | 2–16 s |
| Aranha Grande nv6 (elite) | 100% | 18–37 s |
| 2 Fadas nv6 | 100% | 9–27 s |
| 2 Fadas nv7 | 100% | 8–27 s |
| Ent Corrompido nv7 | 96–100% | 23–36 s |

A meta é ≥ 85%, e todos os grupos passam.

**Ritmo solo**, sem meta. É o modelo dos Campos: o tempo médio de vitória das 4 classes, mais 60 s fora da luta, limitado pelo renascimento.

| Nível | Tempo para subir |
|---|---|
| 5 → 6 | 19,9 min |
| 6 → 7 | 24,3 min |
| 7 → 8 | 33,6 min |
| **Total** | **≈ 78 min** |

Para comparar:
- nos Campos, o nível 1 → 5 leva cerca de 1 h (v0.8);
- nas Ruínas, o nível 9 → 12 leva cerca de 202 min, uns 65–72 min por nível (v0.14).

A Floresta fica entre as duas. A T-380 avalia a curva.

## 4. Observações

- **A Floresta é fácil para quem caça sozinho.** As Aranhas morrem em segundos, e até a Aranha Grande e o Ent vencem quase sempre. A dificuldade do design ("emboscadas", "visibilidade reduzida") vem do mapa: as árvores cortam a linha de visão e as trilhas são estreitas. O simulador não mede isso, porque usa a arena aberta. Se no playtest a área parecer fácil, o próximo passo é usar grupos maiores ou a Emboscada na Névoa (3 Aranhas + Fada) da T-340.
- **Sem arte nesta fase**, registrado como pendência:
  - não há tile de teia no chão nem de névoa;
  - não há poste de placa, então as placas de perigo da Trilha ficaram de fora;
  - as poças de veneno são só cenário.
- **Matriarca:** a luta não mudou, porque o Covil é o mesmo mapa. A `zone.test_instance` da T-350 usa o mapa do Covil, então a saída dela também passou a levar à Floresta. Os testes do servidor trocam essa saída para os Campos, como antes.

## 5. Online (servidor local, `--world --dev-login`)

Rodei 2 bots de nível 1 com `--route`, numa cópia de `data/` no scratchpad sem os monstros da Floresta, como na T-352. O caminho e os portões são os do repositório.

| Etapa | `--start-zone` | `--route` | Resultado |
|---|---|---|---|
| Cidade → Ninho | `zone.city` | `zone.lower_forest/nest_entrance` | 2/2 em 42–46 s |
| Floresta → Covil | `zone.lower_forest` | `zone.matriarch_lair/Antessala` | 2/2 em 37–42 s |
| Covil → Floresta | `zone.matriarch_lair` | `zone.lower_forest/nest_entrance` | 2/2 em 1 s (a chegada fica diante da caverna) |
| Floresta → Cidade | `zone.lower_forest` | `zone.city/plaza` | 2/2 em 4–5 s |
