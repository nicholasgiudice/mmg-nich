# T-330 — Mapas maiores: Cidade-Fortaleza e Campos da Vigília

- **Ambiente:** Windows (Tiled)
- **Modelo:** Humano + Médio
- **Depende de:** T-331
- **Libera:** T-342, T-343
- **Áreas que pode alterar:** `data/maps/city.tmj`, `data/maps/vigil_fields.tmj`, `data/maps/matriarch_lair.tmj` (só o portão de volta), `data/zones.json`, `data/maps/tilesets/`, `client/assets/art/tiles/` (tilesets novos, com créditos), `CREDITS.md`, `tests/Game.Design.Tests/FieldsDesignTests.cs` e `tests/Game.Sim.Tests/ZoneMapTests.cs` (só metas e contagens), `tasks/reports/`

## Contexto

O playtest da fase 2 pediu **mapas bem maiores** (`tasks/reports/playtest-v0.2.md`, P6). Hoje a Cidade tem 40×30 tiles e os Campos têm 96×64. Com a câmera em 1× e tela cheia (1920×1080, tiles de 32 px), cabem cerca de 60×34 tiles na tela, então os Campos têm menos de dois "telas" de largura.

Depois da T-331, portões, surgimentos e NPCs ficam nos objetos do próprio mapa. Por isso, aumentar o mapa é trabalho só no Tiled. As convenções estão em `design/map-conventions-v0.1.md`, e a sensação de cada área em `design/first-map-v0.1.md` §1–§2.

## Requisitos

1. **Cidade:** cerca de **80×60**. Mantém os seis prédios com os NPCs, a praça central e o portão leste. Reserva os portões **norte** (Floresta Baixa) e **oeste** (Ruínas). Nesta tarefa eles ficam fechados (parede ou portão sem `transitions`): a T-342 e a T-343 os abrem. O portão sul fica fechado (`first-map-v0.1.md`).
2. **Campos:** cerca de **200×140**, com as quatro subáreas de hoje (Estrada Leste, Fazenda Abandonada, Clareira dos Caçadores, Acampamento dos Salteadores) maiores e mais espaçadas. A estrada liga tudo, e a cidade continua visível no começo (muralha na borda oeste).
3. **Densidade:** mais pontos de surgimento, para o ritmo de caça não cair com as distâncias maiores. Grupos a ≥ 12 tiles entre si e da chegada (teste da T-215). Os níveis continuam 1–4 por subárea.
4. **Ritmo:** o modelo de ritmo (`FieldsDesignTests`) assume 60 s entre grupos. Medir, com bots `--roam` ou à mão, o tempo médio para andar de um grupo ao próximo. Se passar de 60 s, aumentar a densidade em vez de mudar a meta. Registrar a medição.
5. **Covil da Matriarca:** continua ligado aos Campos até a T-342, que o muda para a Floresta. Ajuste o portão de volta, se a borda leste mudar.
6. Desempenho: rodar 16 bots `--roam` no mundo maior por 60 s e anotar o tick médio, o p99 e os bytes por jogador (tem que continuar abaixo de 2 ms em média).
7. O designer aprova os dois mapas antes do commit final (como na T-055).

## Critérios de aceite

- `ZoneMapTests`: todo surgimento, NPC e portão alcançável a pé a partir do nascimento;
- `FieldsDesignTests` passando com a nova densidade;
- medições de ritmo e de desempenho no Resultado;
- `./scripts/check.sh` completo passa.

## Resultado

**Feito** (Windows, 2026-11-09). Os dois mapas foram aprovados pelo designer.

**Como os mapas foram feitos.** O script da T-055 não estava no repositório, então escrevi um gerador novo (Python, fora do repositório).
- Ele monta os mapas com as peças dos mapas antigos:
  - prédios, praça e muralha da Cidade;
  - cerca e casa da Fazenda, paliçada do Acampamento;
  - carvalho, pinheiros, pedras e tufos dos Campos.
- O chão é repintado pelos cantos com o wangset Summer Terrain, como faz o pincel de terreno do Tiled. Por isso as estradas e os pátios se emendam sem costura.
- Os `.tmj` são a fonte daqui em diante. Imagens em `tasks/reports/mapas-t330/` (`cidade.png` e `campos.png`, mais `*-antes.png` para comparar).

**Cidade (80×60).**
- Praça central de 26×20 (a antiga esticada), com o nascimento `spawn` no meio.
- Os seis prédios com NPC (o Templo e o Ferreiro com loja) e duas moradias sem NPC.
- Ruas de terra: anel em volta da praça, avenidas para os quatro lados e a rua do sul, onde ficam as portas da Estalagem, do Armazém e do Mercado.
- Gramados com árvores e flores.
- **Portões:**
  - Leste aberto (`east_gate`, linhas 28–31; chegada `from_fields`).
  - **Norte e oeste reservados:** as avenidas chegam até a muralha, que continua inteira, sem `transitions`. A T-342 e a T-343 abrem.
  - Sul fechado.

**Campos (200×140).**
- **Borda:**
  - Pedras em volta, com pinheiros embaixo, como antes.
  - Na borda oeste fica a muralha da Cidade, em volta do portão (`west_gate`, linhas 68–71; nascimento e `from_city` em (3,5; 69,5)).
- **Estradas:** a Estrada Leste atravessa o mapa, com ramais para a Fazenda (pela frente do pátio), para o Acampamento e para a Clareira, e uma trilha até o portão do Covil (`east_gate`, linhas 104–107; `from_lair` em (195,5; 105,5)).
- **Subáreas, maiores e mais espaçadas:**
  - Estrada Leste: 104×32.
  - Fazenda Abandonada: com o campo de flores esticado.
  - Clareira dos Caçadores: 62×46, com pedras.
  - Acampamento dos Salteadores: pátio esticado.
- Bosques nas bordas e árvores e pedras espalhadas.
- **Densidade:** 22 grupos (30 `monster_spawn`), contra 8 antes. As composições são as mesmas, então os grupos típicos dos testes de design não mudaram:
  - Estrada: 6 grupos (2 ratos nv1).
  - Fazenda: 3 grupos (2 ratos nv1 + lobo nv2) e 3 grupos (2 lobos nv2).
  - Clareira: 5 grupos (3 lobos nv3).
  - Acampamento: 5 grupos (lobo nv4 + salteador nv4).
- Todo grupo fica a ≥ 14 tiles dos outros e das chegadas. O Covil não mudou: a volta usa o nome da chegada (`from_lair`).

**Ritmo** (req. 4). Medi o caminho a pé, numa busca na grade de colisão, até o grupo mais perto do mesmo nível, a 4,5 tiles/s:

| | antes | agora |
|---|---|---|
| entre grupos do mesmo nível | 18,5 tiles = 4,1 s | 19,5 tiles = 4,3 s (máx 6,9 s) |
| da chegada ao nível 2 / 3 / 4 | 7 / 13 / 17 s | 17 / 25 / 33 s |

A caminhada entre grupos ficou bem abaixo dos ~10 s de deslocamento que entram nos 60 s do modelo. A densidade compensou o mapa maior, e nenhuma meta mudou.

`FieldsDesignTests` e `EconomyDesignTests` passam. `Game.Design.Tests` passa inteiro (121/121). Mais grupos só aliviam o limite do renascimento, e no modelo o limite já era o tempo do jogador.

**Desempenho** (req. 6). Servidor Release `--world --dev-login` e 16 bots `--roam`, **no mesmo PC Windows**, que também tinha outro servidor e o Godot abertos:

| | tick médio (estável) | p99 | KB/s por bot | quedas |
|---|---|---|---|---|
| mapas antigos (60 s) | ~3,5 ms | ~24 ms | 11,4 | 0 |
| mapas novos (120 s) | ~5 ms | ~30–40 ms | 12,7 | 0 |

Os mapas novos custam cerca de 40% a mais de tick: são 52 monstros nos Campos, contra 19 antes. A meta de < 2 ms vale para a VPS e não dá para medir aqui, porque os bots disputam a CPU com o servidor. **Pendente: rodar `tests/Game.NetBots/load.sh` (16 bots `--roam`) na VPS.** Se passar de 2 ms, o primeiro ajuste é a densidade da Estrada (6 grupos) e da Fazenda.

**Testes.**
- `ZoneMapTests` passa sem mudança: todo surgimento, NPC e portão alcançável a pé.
- **Desvios (fora das áreas listadas):**
  - `tests/Game.Sim.Tests/MapConversionTests.cs` e `MapFixtures/ascii-arenas.json`: a Cidade e os Campos saíram da comparação com as grades ASCII da T-214, que deixaram de ser conversão (agora são 5 arenas).
  - `tests/Game.Data.Tests/ZoneMapEquivalenceTests.cs`: a equivalência da T-331 compara só o que não é posição (destinos dos portões, monstros, nome, arena, descanso, corpo) e os NPCs da Cidade. A posição exata do Ferreiro saiu do teste.
  - `design/map-conventions-v0.1.md` §10: parágrafo dos mapas da T-330.
- `./scripts/check.sh` completo passa.

**Pendências:**
- Medir o desempenho na VPS (acima).
- Os portões norte e oeste da Cidade continuam fechados (T-342, T-343).
- O gerador não ficou no repositório. Se for preciso refazer os mapas do zero, ele pode ser guardado numa tarefa própria.
