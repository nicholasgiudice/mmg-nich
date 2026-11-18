# T-394 — Dono por item no saque e item largado livre para todos

- **Ambiente:** Windows (Core, dados e protocolo rodam em qualquer um; o cliente precisa do build do Godot)
- **Modelo:** Médio
- **Depende de:** —
- **Libera:** —
- **Áreas que pode alterar:** `src/Game.Core/Combat/Items/`, `src/Game.Core/Definitions/Items.cs`, `src/Game.Data/GameDataValidator.Items.cs`, `src/Game.Protocol/` (codec do `GroundItem` e `ProtocolVersion`), `src/Game.Site/Pages.Items.cs`, `client/scripts/Presentation/WorldView.cs`, `data/items.json`, `tests/`, `design/itens.md`, `design/bestiario.md`

## Contexto

Pedido do designer em 2026-11-18. Até agora, cada item que caía de um monstro tinha como donos **todos** os participantes da morte, e quem apertasse F primeiro levava. Em grupo, o saque virava corrida. Além disso, o item largado por um jogador só podia ser pego por ele mesmo, então não dava para passar um item a outra pessoa.

## Requisitos

1. **Item que cai de monstro:** cada item tem um dono, escolhido entre os participantes da morte (os mesmos que recebem a XP, T-310). Durante um prazo configurável, só o dono pega; depois, qualquer participante pega. Quem não participou nunca pega.
2. A escolha do dono é determinística (nada de relógio nem `Random` fora da simulação). Numa queda com vários itens, os donos se alternam entre os participantes.
3. **Item largado por um jogador:** sem dono; qualquer jogador pega, a qualquer momento.
4. O prazo vem de `data/items.json` (`rules.items.ownerDuration`, em segundos), validado.
5. O cliente destaca com "[F]" só o item que o jogador pode pegar naquele momento, seguindo a mesma regra.
6. Testes no Core, nos dados e no protocolo.

## Critérios de aceite

- testes novos cobrindo a escolha do dono, o rodízio, o prazo, quem não participou e o item largado;
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Cuidados de integração

- Sobe `ProtocolVersion` para **14**. Na mesma rodada da integração, rode o `deploy-dev.sh` e publique o cliente novo (`export-client.ps1` e `deploy-download.ps1`), senão o cliente publicado deixa de entrar no servidor.

## Resultado

**Feito** em 2026-11-18, na branch `task/T-394-dono-do-saque`.

**Regra no Core** (`GroundItem`, `Gear`):
- `GroundItem` ganhou `Owner` (id do dono escolhido; 0 = ninguém) e `FreeAtTick` (a partir de quando os participantes pegam), além de `Owners` (os participantes) e `ExpiresAtTick`.
- `GroundItem.CanPickUp(id, tick)` decide quem pega e é usado pelo Core (`Gear.PickUp`) e pelo cliente. A regra:
  - `Owners` vazio: qualquer um pega;
  - antes de `FreeAtTick`: só o `Owner`;
  - depois: qualquer um de `Owners`.
  - O `IsOwner` antigo saiu.
- `Gear.DropLoot`: os participantes ficam em ordem de id.
  - O dono do primeiro item é o de índice (id do monstro + tick da morte) mod participantes, e os itens seguintes da mesma queda vão para os próximos da lista (rodízio). O chefe derruba 2 itens, que vão para 2 pessoas diferentes.
  - A escolha não usa o RNG da simulação; veja as decisões.
- `Gear.Drop` (largar do inventário) cria o item com `Owners` vazio.
- `Gear.SpawnGround` ganhou o parâmetro opcional `owner`. `FreeAtTick` = tick atual + `OwnerDurationTicks` quando há dono, e o tick atual quando não há.

**Dados:**
- `rules.items.ownerDuration` = **30 s** em `data/items.json` (`ItemRules.OwnerDurationTicks`, padrão 30 s).
- O validador exige o valor entre 0 e `groundDuration` (120 s). Com 0, o item fica livre para os participantes desde o início.

**Protocolo 14:** o codec do `GroundItem` leva `Owner` e `FreeAtTick`.

**Cliente:** `WorldView.RenderGroundItems` usa `CanPickUp` com o tick do snapshot. O item de outro dono aparece apagado e passa a ter "[F]" quando o prazo acaba. O servidor continua decidindo; o cliente só mostra.

**Site:** a página de itens mostra o novo prazo, "Tempo só do dono do item que cai".

**Documentos:** `design/itens.md` (tabela de regras e a seção "Quem pega o item no chão") e `design/bestiario.md` (seção de quedas).

**Testes:**
- `ItemTests`:
  - dono e `FreeAtTick` na queda de quem joga sozinho;
  - rodízio dos donos com 3 participantes e o chefe, e o primeiro dono mudando de morte para morte;
  - escolher o dono não muda as quedas sorteadas (mesmas quedas com 1 ou 3 participantes);
  - item largado pego por outro jogador na hora;
  - item largado que expira (separado do teste anterior).
- `PartyTests`:
  - o dono pertence aos participantes;
  - só o dono pega durante o prazo; depois dele, o outro membro do grupo pega e quem não participou continua sem poder pegar.
- `ItemLoaderTests`: leitura de `ownerDuration` e os dois erros de validação (negativo e maior que `groundDuration`).
- `ProtocolTests`: ida e volta de `Owner` e `FreeAtTick`.

**Decisões:**
- **Sem RNG para escolher o dono.** A primeira versão sorteava o dono pelo RNG da simulação. Isso muda a sequência de números de toda luta em grupo, e o `WatcherDesignTests.Without_the_cleric_the_win_rate_drops_clearly`, que na `main` já fica exatamente no limite (30% de vitória, limite ≤ 30%), passou a dar 32%. Trocar o sorteio por (id do monstro + tick da morte) mantém a escolha determinística e variada de morte para morte, sem tocar no RNG: as simulações de balanceamento ficam idênticas às da `main`.
- O "rodízio" vale dentro de uma mesma queda; entre mortes diferentes, o primeiro dono muda pela conta acima. Um rodízio que atravessasse várias mortes precisaria guardar estado por grupo. Fica como possível ajuste depois do playtest.
- O `WatcherDesignTests` citado acima está no limite na `main`: qualquer mudança futura que mexa na sequência do RNG em lutas de grupo pode derrubá-lo. Vale o designer rever o limite ou o número de partidas.
- Item largado é público para **qualquer** jogador, não só para o grupo (pedido do designer).

**Pendências:** sem prévia dentro do jogo. A integração precisa do `deploy-dev.sh` e de um cliente novo publicado (protocolo 14).
