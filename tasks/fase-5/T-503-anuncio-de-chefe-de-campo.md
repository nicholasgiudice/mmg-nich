# T-503 — Servidor e protocolo: anúncio de chefe de campo

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** —
- **Libera:** T-530, T-540
- **Áreas que pode alterar:** `src/Game.Data/TiledMapReader.cs`, `src/Game.Core/Definitions/Zones.cs`, `src/Game.Server/Zones/`, `src/Game.Protocol/`, `design/map-conventions-v0.1.md` (§5), `tests/`

## Contexto

A região do sul tem **chefes de campo**: monstros fortes no mundo aberto, com renascimento longo. O primeiro é a Bruxa do Brejo (nível 20, renasce em 10 min, T-510), depois o Chefe de Guerra do Planalto (fase 6). O tempo longo já existe (`respawn` do `monster_spawn`, em segundos). O que falta é o jogador **saber** que o chefe nasceu, sem ficar parado na frente do ponto.

## Requisitos

1. Propriedade nova `announce` (bool, padrão `false`) no `monster_spawn` (`design/map-conventions-v0.1.md` §5).
2. Quando um monstro de um ponto com `announce` nasce, e quando morre, o servidor manda um evento para **todos os jogadores da zona** (não só os da área de interesse), com o id do monstro, o nome da subárea e se nasceu ou morreu.
3. Só são anunciados os nascimentos e as mortes que acontecem com o jogador na zona. Quem entra depois não recebe o estado atual do chefe: vê o chefe se chegar perto. Registre se mudar.
4. Evento novo no protocolo: tag nova e `ProtocolVersion` + 1 a partir do número da `main` (19 em 2026-11-23). O cliente mostra o aviso na T-540. Protocolo novo pede cliente novo: siga os "Cuidados de integração" da fase 5 em `tasks/README.md` antes de integrar na `main`.

## Critérios de aceite

- teste de servidor: dois jogadores em pontos distantes da mesma zona recebem o evento de nascimento e de morte; um jogador em outra zona não recebe;
- teste de carga: `announce` com valor que não é bool dá erro;
- teste de protocolo: ida e volta do evento novo;
- `./scripts/check.sh` completo passa.

## Resultado

**Feito.** Branch `task/T-503-anuncio-de-chefe-de-campo`. **Esta branch NÃO entra na `main` sozinha:** `ProtocolVersion` sobe para **20** e o cliente ainda não conhece o evento novo. A T-540 (cliente) sai desta branch e as duas entram juntas, com um cliente novo (cuidados de integração da fase 5, `tasks/README.md` §6j). Ordem de integração da fase: 501, 502, 503 (rebase nos arquivos em comum: `Zones.cs`, `TiledMapReader`).

**Contrato para a T-540**
- Evento do Core `FieldBossAnnounced(string MonsterId, string? Area, bool Spawned)` (`src/Game.Core/Events/GameEvent.cs`), **tag de rede 5** em `EventCodecs` (campos na ordem: `MonsterId`, `Area`, `Spawned`; `Area` é string opcional, nula se o ponto não está em nenhuma subárea).
- `Spawned = true`: o monstro nasceu (renascimento); `false`: morreu. `MonsterId` é o id da definição (`monster.bog_witch`); o nome para exibir vem de `data/monsters.json`. `Area` é o nome da subárea do ponto, pronto para mostrar.
- Chega dentro de um `EventsMessage` normal, a **todos** os jogadores da zona (ou da cópia) onde o monstro está, perto ou longe. O evento não cita nenhuma entidade de propósito: o filtro de interesse (`EventEntities.Relevant`) só deixa passar a todos o evento sem entidade. O cliente não precisa de nada novo no `Welcome`.
- `ProtocolVersion.Current` = **20** (era 19 na `main`).

**O que mudou**
- `monster_spawn` ganha a propriedade `announce` (bool, padrão `false`): `MapSpawn.Announce` → `MonsterSpawnDefinition.Announce` (`TiledMapReader`, `ZoneComposer`). Valor que não é `true`/`false` é erro de carga (`'announce' deve ser bool`), como o `objective`. `design/map-conventions-v0.1.md` §5 documenta.
- `OpenZone` publica `FieldBossAnnounced` quando o monstro de um ponto com `announce` nasce (renascimento) e quando morre (na primeira vez que o ponto o vê morto).

**Decisões e registros**
- **Só o que acontece com jogador na zona é anunciado** (requisito 3), e foi registrado em §5 do documento de convenções. O nascimento inicial da zona (construtor, sem jogador) não gera evento. Como a zona vazia não avança, nada nasce nem morre sem plateia; o renascimento que vence durante a parada é feito pelo `CatchUp` no primeiro tick com jogador e aí anuncia normalmente. Quem entra depois não recebe o estado do chefe: vê o chefe se chegar perto.
- O evento é publicado depois do `World.Step()` do tick, então sai com o tick seguinte (50 ms depois). Sem efeito para o jogador.
- Ponto com `count` > 1 anuncia cada monstro (um evento por monstro); chefe de campo é um ponto com `count` 1.
- Instâncias também anunciam (mesma classe `OpenZone`); como nenhum ponto de instância usa `announce`, é irrelevante hoje.
- O dado real ainda não tem chefe de campo (vem na T-510/T-530). O teste de servidor marca o ponto do rato dos Campos com `announce` por conteúdo montado no teste.
- `ProtocolTests` tinha `Assert.Equal(19, ProtocolVersion.Current)` na prova da T-405; virou `>= 19` para não quebrar a cada subida.

**Testes**
- Servidor (`FieldBossAnnounceTests`): dois jogadores a 45+ tiles do chefe e entre si recebem a morte e o renascimento, na ordem e com a subárea; um jogador na Cidade não recebe nada; ponto sem `announce` (dado real) não anuncia.
- Carga (`TiledMapLoaderTests`): `announce` true/false/ausente lido certo; `"sim"` e número dão erro `'announce' deve ser bool`.
- Protocolo (`ProtocolTests`): ida e volta com e sem subárea e a tag 5; o teste que cobre todo evento do Core e o de ida e volta campo a campo passam com o evento novo.
- `./scripts/check.sh` completo: `dotnet format` e `dotnet build -warnaserror` sem avisos; todos os projetos de teste passam, **exceto o `Game.Design.Tests`**, com as mesmas falhas de balanceamento que a T-500 já registrou na `main` (`Guardian_cuts_the_damage...` níveis 30 e 40, `A_group_of_four_with_rares_beats_the_watcher...` 71%, e o do Vigia sem Clérigo). Não têm relação com esta tarefa (que só acrescenta um evento e não muda a simulação); precisam de correção à parte.
