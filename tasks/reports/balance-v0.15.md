# Relatório de balanceamento v0.15 (T-352): Cripta do Vigia

A dungeon de grupo do primeiro mapa: níveis 12–14 e o Vigia Sem Rosto (nível 15) no fim. Design: `design/first-map-v0.1.md` §5. Mapa: `data/maps/watcher_crypt.tmj`. Testes: `tests/Game.Design.Tests/CryptDesignTests.cs` (design), `tests/Game.Sim.Tests/ZoneMapTests.cs` (percurso) e `tests/Game.Server.Tests/CryptInstanceTests.cs` (instância e selos). Imagens: `tasks/reports/mapas-t352/`.

## 1. Mapa

O mapa tem 140 × 76 tiles, todo em rocha, com salas de piso de pedra rachada. Toda parede ao norte de um piso tem face de pedra de 3 tiles (as bordas). As salas são:

| Sala | Nível | Grupos |
|---|---|---|
| **Entrada** | — | a escada que sobe para as Ruínas, na parede norte (a saída), entre dois lampiões |
| **Salão dos Escudos** | 12 | 7 grupos. São três alas separadas por muros, com uma passagem estreita entre cada uma. Duas passagens são guardadas por um Cavaleiro Espectral; a terceira, por Guardas com Sacerdotes. |
| **Galeria das Cinzas** | 13 | 4 grupos: o corredor de entrada, duas fileiras de colunas e o Cavaleiro no fim. Há duas áreas `curse_area` marcadas, só como cenário. |
| **Câmara dos Juramentos** | 13–14 | 2 grupos. Daqui saem as duas rotas. |
| Rota segura (**Passagem Estreita**) | 13 | 1 grupo de 3 Guardas |
| Rota de risco (**Rota dos Mortos**) | 14 | 2 grupos, um deles com o Cavaleiro Espectral, que é a elite com mais saque |
| **Antessala** | 14 | um Cavaleiro Espectral, por onde as duas rotas passam |
| **Sala do Vigia** | 15 | 38 × 26 livres, sem obstáculos. Os selos ficam ao norte, ao sul e a leste. A porta é estreita, a leste, atrás do Vigia. |

## 2. Método

- **Salas:** cada grupo (pontos da mesma subárea a até 4 tiles) vira um encontro na arena aberta do protótipo. Enfrenta um grupo de 4 (Guerreiro, Arqueiro, Mago, Clérigo) no perfil esperado do nível do grupo, com limite de 180 s.
- **Chefe:** o encontro da T-351 na Sala do Vigia real. O Vigia e os selos ficam nos pontos do mapa e o grupo começa na mesma posição relativa do salão de teste. Perfil de raros, `interrupt=save` e `followInjured=1`, como em `WatcherDesignTests`.
- **Lotes:** 100 seeds.

## 3. Resultados

| Grupo | Vitória | Tempo | Mortes |
|---|---|---|---|
| Salão: 5 Guardas nv12 | 100% | 17 s | 0 |
| Salão: Cavaleiro nv12 | 98% | 61 s | 0,47 |
| Salão: 5 Guardas + Sacerdote nv12 | 100% | 20 s | 0 |
| Salão: Cavaleiro + 3 Guardas nv12 | 98% | 70 s | 0,40 |
| Salão: 4 Guardas + 2 Sacerdotes nv12 | 100% | 18 s | 0 |
| Galeria: 4 Guardas nv13 | 100% | ≈ 12 s | 0 |
| Galeria: 4 Guardas + 1–2 Sacerdotes nv13 | 100% | 13–15 s | 0 |
| Galeria: Cavaleiro + 2 Guardas nv13 | 86% | 77 s | 1,17 |
| Câmara: 5 Guardas nv13 + 2 Sacerdotes nv14 | 100% | 12 s | 0 |
| Câmara: 4 Guardas nv14 | 100% | 9 s | 0 |
| Passagem Estreita: 3 Guardas nv13 | 100% | 10 s | 0 |
| Rota dos Mortos: 4 Guardas + 2 Sacerdotes nv14 | 100% | 10 s | 0 |
| Rota dos Mortos: Cavaleiro + 2 Guardas nv14 | 89% | 77 s | 1,23 |
| Antessala: Cavaleiro nv14 | 91% | 71 s | 1,28 |
| **Vigia, nível 14** | **69%** | **356 s** | 0,79 |
| **Vigia, nível 15** | **77%** | **330 s** | 0,78 |

- **Meta das salas** (grupo de 4 vence ≥ 80%): todos os grupos passam.
- **Meta do chefe** (T-351: média de 50–80%, cada nível entre 40% e 90%, 4–6 min): média de 73%. Passa, mas o nível 14 fica a 4 s do teto de 6 min.

**Duração estimada** de uma ida completa: lutas, mais 45 s de pausa por grupo, mais a caminhada da entrada ao chefe passando por todos os grupos, mais o chefe no nível 14.

| Rota | Grupos | Lutas | Pausas | Caminhada | Total |
|---|---|---|---|---|---|
| Segura | 15 | 13,3 min | 11,2 min | 1,1 min | **25,6 min** |
| De risco | 16 | 14,6 min | 12,0 min | 1,2 min | **27,7 min** |

A meta é 25–40 min. A estimativa conta uma única tentativa no chefe. Cada falha soma o tempo de refazer, porque a cópia acaba quando os selos caem (§4).

## 4. Observações

- **Guardas e Sacerdotes são fáceis para 4.** Num grupo de 4 no nível esperado, morrem em 10–20 s, mesmo em grupos de 5 a 7. O que pesa são os Cavaleiros (elite), com 86–98% de vitória e cerca de 1 morte por luta. Por isso eles guardam as passagens. Para endurecer as salas sem mexer nos monstros, seria preciso uma versão "de dungeon" dos monstros, com rank elite. Isso fica fora desta tarefa (`data/monsters.json`) e pode entrar depois do playtest (T-380).
- **Porta da Sala do Vigia.** Os bots recuam para longe do chefe e fogem do medo, e com a porta no lado oeste saíam da sala. No nível 14, a vitória caiu de 60% para 50% e o tempo passou do teto. Uma porta a noroeste também não serviu. Com a porta estreita (2 tiles) a leste, atrás do Vigia, a luta volta à da T-351. O grupo chega por um corredor que contorna a sala pelo sul.
- **Tamanho da Sala do Vigia.** A tarefa sugeria cerca de 30 × 24. Com 30 × 24 a vitória caiu para 27–37% no nível 14, porque o grupo não tinha espaço para recuar. A sala ficou com a área livre do salão de teste da T-351 (38 × 26), onde a luta foi balanceada.
- **Falha do encontro:** se os três selos caem, todos saem pela saída para a escada nas Ruínas e a cópia acaba. A próxima entrada recomeça a Cripta inteira. Se todos morrem, o Vigia continua como está e o grupo renasce na entrada (T-350).

## 5. Online (servidor local, `--world --dev-login`)

- 2 bots em grupo (`--party --party-size 2 --route zone.watcher_crypt/watcher_arena`), saindo das Ruínas (`--start-zone zone.old_road_ruins`):
  - entraram na **mesma cópia** (instância 1);
  - chegaram à Sala do Vigia em 79 e 89 s;
  - morreram para o Vigia (são nível 1) e renasceram na entrada da mesma cópia.
- Para os bots de nível 1 chegarem ao fim, rodei com uma cópia de `data/` no scratchpad sem os grupos das salas, como na medição da T-350. O caminho e a instância são os do repositório.
