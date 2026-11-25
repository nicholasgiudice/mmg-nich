# Relatório de balanceamento v0.28 (T-533): as Minas Fundas

Masmorra instanciada do nível 24 ao 27, para 4 pessoas (`design/mapas-16-40-v0.1.md` §3.4), com o Capataz Forjado (T-512)
como chefe. Mapa e imagens: `tasks/reports/minas-fundas/README.md`. Testes: `tests/Game.Design.Tests/MinesDesignTests.cs` e
`ZoneGoalDesignTests.Mines_in_a_party_of_four_give_at_least_1_3_times_the_xp_per_hour_of_the_field_per_person` (100 seeds por
lote). Os números v0.23 a v0.27 são das outras tarefas da fase 5.

## 1. Como foi medido

- **Salas:** cada grupo (pontos da mesma subárea a até 4 tiles) vira um encontro na arena aberta do protótipo contra o grupo
  de 4 de referência da masmorra (`ZonePace.Party`: Guardião, Caçador, Piromante e Sacerdote), no perfil esperado do nível do
  grupo, sem raros, com o limite de 180 s. São os mesmos lotes do ritmo de XP.
- **Chefe:** o encontro da T-512 (`encounter.forged_overseer`) posto na Fornalha real (`map.deep_mines`, paredes de verdade),
  com o Capataz no ponto do mapa e o grupo no pé da descida da Antessala. Perfil de grupo do nível 27 com segunda classe e
  raros, como na T-512 (`OverseerDesignTests.Group`).
- **Ida completa** (`ZonePace.Dungeon`): os grupos da rota na ordem do mapa, saindo da chegada, e o chefe por último. Tempo =
  lutas (tempo médio de vitória) + 45 s de pausa por grupo (`CryptDesignTests.PauseSeconds`) + caminhada pela velocidade do
  grupo. XP de cada pessoa dividida entre 4 (com o bônus de grupo). O chefe entra com a luta do grupo de referência na Fornalha.
- **Campo da faixa:** a Serra Partida solo nos mesmos níveis (`ZoneGoalDesignTests.SoloHunt`: pares, sem os grupos grandes e
  sem o Golem), como a T-505 e a T-532 deixaram.

## 2. Resultados

### Salas (meta: grupo de 4 do nível vence ≥ 80%)

| Sala | Grupos | Vitória | Tempo |
|---|---|---|---|
| Galeria de Entrada (nv24) | 4 Mineiros; 3 Mineiros + 2 Morcegos; 4 Mineiros; 5 Mineiros em fila | 100% | 2 s |
| Poço dos Carrinhos (nv24–25) | 13 grupos de 6 a 10 (Mineiros e Morcegos, 3 bandos só de Morcegos) | 100% | 2–3 s |
| Veio Rico (nv26) | 5 Lobos; o Golem de Pedra (elite) + 4 Lobos; 4 Mineiros + 4 Morcegos | 100% | 2 s; o Golem 11 s |
| Veio Estreito (nv26) | 5 Lobos | 100% | 2 s |
| Antessala (nv26) | 4 Mineiros + 4 Morcegos (a guarda, nas duas rotas) | 100% | 2 s |

### Chefe na Fornalha real (metas da T-512)

| Grupo (nv27, raros) | Vitória | Tempo | Mortes por luta | Meta |
|---|---|---|---|---|
| Guardião + Atirador + Piromante + Sacerdote | **100%** | **270 s** | 0,00 | ≥ 75% em 3–6 min ✔ |
| Berserker + Atirador + Piromante + Inquisidor | **9%** | 342 s | 3,68 | ≤ 40% ✔ |
| Guardião + Caçador + Piromante + Sacerdote (referência da masmorra) | 100% | 261 s | — | medido |

Na primeira versão a Fornalha tinha 30 × 25 livres (a tarefa pedia cerca de 30 × 24): o grupo com os papéis vencia 100% em
266 s, mas o grupo sem eles também vencia **83%** (meta ≤ 40%). Na sala menor o Capataz anda pouco atrás de quem não é o
tanque, e o fogo do Piromante no chão acerta; a luta sem os papéis fica em ~316 s e acaba antes da Fornalha Transbordando. Com
**40 × 28** livres, o tamanho do salão em que a T-512 balanceou o chefe, a separação volta (9%). A Cripta fez o mesmo pelo
mesmo motivo (38 × 26 em vez de 30 × 24, T-352).

### Duração de uma ida (meta: 20 a 35 min)

| Rota | Grupos | Lutas | Pausas | Caminhada | Total |
|---|---|---|---|---|---|
| pelo Veio Rico (Golem) | 21 + o chefe | 5,2 min | 15,8 min | 1,3 min | **22,2 min** |
| pelo Veio Estreito | 19 + o chefe | 5,0 min | 14,2 min | 1,2 min | **20,4 min** |

A primeira versão (10 e 9 grupos) levava 13,2 e 11,2 min: as lutas de sala acabam em 2 s, e o tempo vem quase todo das
pausas. Para caber na meta, a Galeria de Entrada virou um U com 4 grupos pequenos, o Poço ganhou 13 grupos e a Antessala, a
guarda da Fornalha.

### XP por hora (meta da T-505: masmorra ≥ 1,3 × o campo da faixa, por pessoa, num grupo de 4)

| Nível | Serra solo | Minas pelo Veio Rico | Minas pelo Veio Estreito |
|---|---|---|---|
| 24 | 14.394 XP/h | 28.330 XP/h (10.496 XP por ida) | 27.810 XP/h (9.460 por ida) |
| 25 | 11.495 XP/h | 24.962 XP/h (9.248 por ida) | 24.391 XP/h (8.297 por ida) |
| **Masmorra ÷ campo** | | **2,06×** | **2,02×** |

O teste confere as duas rotas: quem escolhe a rápida também tem de render mais que o campo.

## 3. Observações para o designer e a T-550

- **As salas são fáceis para 4.** O grupo com Piromante e Caçador limpa 8 a 10 monstros do nível em 2 s; só o Golem dura
  mais (11 s). É o mesmo achado da Serra (T-511, T-532: os grupos grandes rendem de 3 a 5 vezes mais para quem tem dano em
  área) e da Cripta (Guardas e Sacerdotes morrem em 10–20 s). O desafio da masmorra é o chefe. Endurecer as salas pede monstros
  "de masmorra" (rank elite ou mais vida), fora das áreas desta tarefa (`data/monsters.json`).
- **A XP por hora fica no dobro do campo**, acima da meta de 1,3×. Ela vem das pausas de 45 s do modelo depois de lutas de 2 s
  e do chefe (~2.000 XP por pessoa a cada ida). Diminuir os grupos para baixar a XP também baixaria a duração para menos de 20
  min. Se o playtest achar a masmorra rentável demais, o caminho é mexer na XP do chefe ou dos monstros, não no mapa.
- **Duração e chamadas em cadeia:** no Poço os grupos ficam a 12–14 tiles uns dos outros (a regra dos mapas) e os monstros
  puxam a 14 com linha de visão. Puxar um grupo pode trazer o vizinho, o que encurta a ida. Os carrinhos de minério bloqueiam
  a visão e o caminho entre parte dos grupos.
- **Derrota no chefe:** quem cai renasce no Forte e volta pela boca da mina à mesma cópia. Quando o grupo inteiro cai (ou sai),
  o Capataz recomeça: vida cheia, fase 1, sem a Fornalha Esquentando nem a Transbordando, sem os Mineiros invocados (pendência
  da T-512, ver o Resultado da T-533). As salas já limpas continuam limpas.
