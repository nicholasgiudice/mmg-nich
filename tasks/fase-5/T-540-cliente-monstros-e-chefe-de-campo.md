# T-540 — Cliente: monstros do sul, água funda e aviso de chefe de campo

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** T-502, T-503, T-510, T-511, T-512, T-521
- **Libera:** T-550
- **Áreas que pode alterar:** `client/` (sprites, efeitos, telegráfos, HUD), `client/assets/art/sprites.json`, `tasks/reports/`

## Contexto

Mesmo papel da T-345 na fase 3: conferir no jogo a leitura dos monstros novos (T-510 a T-512), com os sprites da T-521, e mostrar o que as peças novas de sistema trazem:
- a água funda (`status.deep_water`, T-502): o jogador precisa entender por que ficou lento;
- o aviso de chefe de campo (evento da T-503).

## Requisitos

1. **Monstros:** os nove monstros novos aparecem com o sprite certo, o tamanho certo (o raio do Core bate com o desenho) e as animações de andar, atacar, conjurar e morrer. Os telegráfos novos (Bote, Agarrão, Rocha Rolante, Martelo, Fornalha, poças e Lodo da Bruxa) são legíveis e chegam antes do golpe.
2. **Água funda:** ícone do estado na barra de estados, com o nome "Água funda", e um efeito leve no personagem (respingo ou ondulação nos pés) enquanto está dentro. O efeito some no tick em que ele sai.
3. **Aviso de chefe de campo:** texto no alto da tela por alguns segundos e uma linha no log ("A Bruxa do Brejo despertou na Cabana da Bruxa" / "A Bruxa do Brejo foi derrotada"), no tema pedra e ouro. O texto sai do nome do monstro e da subárea, não fica fixo no código.
4. **Protocolo:** usa o evento da T-503. Esta tarefa sai da branch da T-503, ou da `main` se a T-503 já estiver integrada, e as duas saem juntas (cliente e servidor do mesmo commit).
5. O designer aprova no jogo, com imagens no relatório.

## Critérios de aceite

- prancha no jogo com os nove monstros e os telegráfos, em `tasks/reports/cliente-t540/`;
- online local: dois clientes na mesma zona recebem o aviso da Bruxa; um cliente em outra zona não recebe;
- `./scripts/check.sh` completo passa.

## Resultado

**Estado: pronto, falta a aprovação do designer no jogo (requisito 5) e a dele para os sprites da T-521.** Imagens em `tasks/reports/cliente-t540/`.

### Base e integração (cuidado importante)

A branch `task/T-540-cliente-monstros-e-chefe-de-campo` sai de `origin/task/T-503-anuncio-de-chefe-de-campo` (`7d21202`, protocolo 20, evento `FieldBossAnnounced`) e tem, nesta ordem, os merges de:

1. `origin/task/T-512-capataz-forjado` (`2d5ef60`; já contém a T-502, a T-510 e a T-511): `7f27e1b`;
2. `origin/task/T-521-sprites-dos-monstros-do-sul` (`b05c565`): `ce4d411`;
3. `origin/task/T-541-cliente-mestre-e-renascimento` (`6ff2e97`; contém a T-501): `600417a`.

Um conflito só, em `tests/Game.Data.Tests/TiledMapLoaderTests.cs` (T-502 e T-503 acrescentaram um teste no mesmo lugar): ficaram os dois testes. `./scripts/check.sh --fast` passou na base combinada, antes de qualquer mudança minha.

**A T-503 e a T-540 entram juntas na `main`, com um cliente novo:** o servidor da T-503 sobe o protocolo para 20 e manda um evento que só esta tarefa sabe mostrar. As demais (T-501, T-502, T-510 a T-512, T-521, T-541) vêm no mesmo pacote porque esta branch as contém.

### O que foi feito

- **Monstros do sul.** Os nove ids já estavam no `sprites.json` (T-521) e batem com os dados. Único ajuste: o **Capataz Forjado** (raio 0,9 no Core) estava do tamanho de um jogador; ganhou `scale` 2, como a Bruxa. Os outros raios batem com o desenho (conferido nas capturas). As animações de andar, atacar, conjurar e morrer saem dos layouts da T-521.
- **Telegráfos.** Já são desenhados pelo cliente a partir dos dados (forma, atraso, âncora); conferi no jogo todos os novos: Bote (linha com carga e seta), Agarrão, Picaretada, Martelo (cones), Rocha Rolante (linha de 14 tiles, 1,5 s antes), Pisão de Pedra, Poça Venenosa (círculo na mira, 1 s antes) e Lodo que Puxa. Nenhum precisou de mudança.
- **Poça e Lodo da Bruxa.** `area.witch_poison_puddle` é poça verde com a arte de veneno (`effects.json`). `area.pulling_mud` ganhou um desenho próprio no `AreaView` (`Kind.Mud`): lama marrom em volta dela, com braços em espiral girando para dentro. Antes caía no roxo genérico.
- **Dardos dos monstros.** `bolts` em `effects.json` para o Cuspe Venenoso da Serpente, o Dardo do Brejo e o Dardo do Lodo da Bruxa e a Escória Ardente do Capataz (projétil colorido ao usar a habilidade).
- **Água funda (`status.deep_water`).**
  - Quadro do personagem (a "barra de estados"): linha `≈ Água Funda` em azul-água, **sem contagem**: o relógio de 1 s do estado só o mantém vivo e mostrar `0,8 s` enganaria. Vale para todo estado com a tag `terrain` (dos dados), não só este.
  - Chip `Ág` azul acima da unidade (cor por tag `terrain`, em `StatusVisuals`).
  - Efeito no corpo: novo estilo de aura `ripple` (`AuraKind.Ripple`, `EntityView`): poça azul com duas ondas abrindo e respingos subindo. As auras vêm do snapshot a cada frame, então o efeito, o chip e a linha somem junto com o estado, no tick em que `StatusRemoved` chega.
  - Dado: `status.deep_water` em `effects.json` (`auras`). O nome exibido é o do dado, "Água Funda".
- **Fornalha do Capataz.** Auras nos três estados (`overseer_heat` anel, `overseer_overdrive` e `overseer_meltdown` labaredas). **Não conferi essas auras no jogo** (as capturas do Capataz pegaram o início da luta; só o chip `Fo`).
- **Aviso de chefe de campo (`FieldBossAnnounced`).**
  - Banner novo, em painel de pedra com texto dourado (`Hud.AnnounceFieldBoss`), logo abaixo da tela do chefe quando há uma, 6 s, some devagar no último segundo. Não usei o banner das fases porque ele é magenta, fora do tema.
  - Linha no log (categoria Fase) com o mesmo texto: "A Bruxa do Brejo despertou na Estrada Leste" / "A Bruxa do Brejo foi derrotada".
  - O texto sai do nome do monstro (`data/monsters.json`) e da subárea que vem no evento (`FieldBossText`, `client/scripts/Hud/FieldBossText.cs`, sem Godot e testado). Nada fixo no código.
  - A linha do log é registrada pelo cliente (`FieldBossText.Formatters()` nos três `new CombatLogRecorder` do cliente), porque o Core não tem formatador para o evento e esta tarefa não mexe no Core. O registro só vale se o Core ainda não tiver o dele (sem erro de duplicidade se um dia tiver).

### Decisões e limitações

- **Gênero do nome.** O artigo ("A"/"O") e o particípio ("derrotada"/"derrotado") saem do fim da primeira palavra (termina em "a" = feminino): "Bruxa do Brejo" ok, "Capataz Forjado" e "Golem de Pedra" ok. Um nome que fuja disso (ex.: "Vigia", masculino) sairia errado. A saída certa é um campo de gênero no dado do monstro; fica de pendência (Core/dados, fora desta tarefa). A preposição ("na Cabana", "no Brejo", "nas Minas") segue o mesmo critério e cai em "em" quando não dá para saber.
- `Sopro da Fornalha` (`circle_self` de raio 30, sem telegráfo) fica sem aviso desenhado; é decisão da T-512 e é lido pelo log e pelo dano.
- A Serpente do Charco só tem andar em 4 direções e morte provisória (T-521): conferido apenas parado/andando nas capturas.

### Como mostrei no jogo

Sem a T-530 (mapa do Brejo) nesta base, nenhum monstro do sul está numa zona. Usei os encontros locais, com `--level=40 --enemy-level=n` e, nos chefes, `--enemy-health=f` (argumentos da T-345):

- monstros e telegráfos: `encounter.drowned_pack`, `marsh_lizardmen`, `marsh_serpents`, `marsh_witch`, `miner_gang`, `mountain_wolves`, `cave_bats`, `stone_golem`, `forged_overseer`; água funda: `encounter.terrain_water`;
- aviso online: servidor `dotnet run --project src/Game.Server -- --world --dev-login --dev-tools --start-zone zone.vigil_fields --data <cópia>`, com **uma cópia da pasta `data/`** (fora do repositório) em que o ponto `field_rat_1` dos Campos da Vigília vira `monster.marsh_witch`, `announce` verdadeiro, renascimento de 8 s, e a Bruxa com 1 de vida (para morrer no primeiro golpe), sem os outros monstros; a mesma cópia foi posta em `client/data/` (cópia ignorada pelo Git), e o build normal a restaurou. **Os dados de verdade não mudaram.**
- três clientes (`--server=127.0.0.1:7791`): Aria e Bento nos Campos; Cora andou para o oeste e passou pelo portão da Cidade.

Imagens:

| Arquivo | O que mostra |
|---|---|
| `prancha-monstros.png` | os nove monstros ampliados, nas folhas da T-521 (o Capataz em escala 2) |
| `prancha-telegrafos.png` | Bote, Agarrão, Picaretada, Rocha Rolante, Pisão, Martelo, Poça (na mira e em vigor) e Lodo que Puxa |
| `agua-funda.png` | fora e dentro da água: quadro do personagem, chip `Ág`, ondas e respingos |
| `aviso-chefe-de-campo.png` | morte e renascimento da Bruxa: Aria e Bento (mesma zona) recebem o banner; Cora (Cidade) não recebe nada |
| `aviso-chefe-log.png` | as duas linhas no log de combate |

Critério "dois clientes na mesma zona recebem o aviso; um em outra zona não": cumprido (imagem acima). O servidor já tem o teste automático equivalente (T-503, `FieldBossAnnounceTests`).

### Testes

`tests/Game.Client.Tests/FieldBossTextTests.cs` (texto com a Bruxa e o Capataz reais, preposições, linha no log pelo `CombatLogRecorder`, registro duplo sem erro); `FieldBossText.cs` entra no projeto de testes por link, como os outros arquivos do cliente. `Hud`, `EntityView` e `AreaView` dependem do Godot e foram conferidos só no jogo.

### Pendências

- **Aprovação do designer** (requisito 5): os telegráfos, a água funda, o banner e a escala do Capataz; e **os sprites da T-521**, que ainda esperam por ela (o `tint` do Lobo da Serra, o Afogado, o Homem-lagarto, a Bruxa, o Mineiro, a troca de criatura do Capataz, o Golem, o Morcego e a Serpente).
- Conferir as auras da Fornalha do Capataz numa luta que chegue às fases finais.
- Quando a T-530 trouxer a Bruxa com `announce` no mapa do Brejo, ver o aviso com a subárea certa ("na Cabana da Bruxa").
- Campo de gênero no dado do monstro (acima).
- Os arquivos `.uid` de `ClassMasters.cs` e `DeathDestination.cs` (T-541) e `OnlineCounterText.cs` ficaram sem versionar (o Godot os gera ao abrir o projeto); não os incluí por não serem desta tarefa.

### Verificação

`./scripts/check.sh` completo: `dotnet format` limpo, `dotnet build -warnaserror` sem avisos, todos os projetos de teste passam (Client 156, Core 623, Data 548, Protocol 76, Server 123, Sim 78, Site 15, Api 39) **exceto 4 falhas em `Game.Design.Tests` (421 de 425)**, as mesmas que já existem na `main` e não têm relação com esta tarefa (Vigia sem o Clérigo, Cripta com grupo de quatro, Guardião ×2).
