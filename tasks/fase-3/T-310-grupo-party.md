# T-310 — Grupo (party): convite, membros e divisão de XP e saque

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-301
- **Libera:** T-311, T-320, T-350
- **Áreas que pode alterar:** `src/Game.Core/Combat/Progression/`, `src/Game.Core/Combat/Items/` (direito ao saque), `src/Game.Core/Definitions/` (regras), `src/Game.Protocol/`, `src/Game.Server/`, `data/rules.json`, `data/schemas/`, `src/Game.Data/` (validação das regras novas), testes correspondentes

## Contexto

O playtest da fase 2 (`tasks/reports/playtest-v0.2.md`, P2) pediu um **sistema de grupo para dividir a XP**. A visão (`docs/vision-mvp-v0.1.md`) fala em grupo ideal de quatro, com vantagens reais para quem joga junto.

Hoje (`ProgressionSystem.AwardKill`) a XP é dividida só entre os **participantes** da morte: quem deu dano nos últimos 30 s, ou curou ou pôs escudo em quem deu dano. A fórmula é `Share`, com bônus de 10% por membro extra (`groupBonusPerMember`). O direito ao saque (T-204) usa os mesmos participantes. Quem está junto, mas não acertou aquele monstro, não ganha nada.

O grupo também é a base da instância de dungeon (T-350), que é criada por grupo.

## Requisitos

1. **Grupo no servidor**, até **4 membros**, com líder. Ações: convidar (por id da entidade ou nome do personagem), aceitar ou recusar o convite (o convite expira em 30 s), sair, e o líder pode expulsar. Quando o líder sai, a liderança passa para o membro mais antigo. Grupo com 1 membro se desfaz.
2. O grupo vive no servidor, entre zonas: trocar de zona não tira ninguém do grupo. Desconectar tira o personagem do grupo. O grupo **não** é gravado no banco.
3. **XP:** quando um monstro morre, todo membro do grupo de algum participante ganha parte da XP se estiver **vivo, na mesma zona e a até `partyShareRange` tiles** do monstro (proposta: 30). A divisão usa a mesma fórmula `Share` sobre o total de quem recebe (participantes de fora do grupo também entram, como hoje). O modificador de diferença de nível continua sendo calculado por pessoa.
4. **Saque:** os membros que receberam XP também têm direito aos itens que caíram daquele monstro. O primeiro que pegar leva, como hoje.
5. **Core:** o Core continua sem saber de conexões. Basta um componente replicado `PartyMembership` (id do grupo, determinístico, atribuído pelo servidor), que o `ProgressionSystem` consulta. Regra nova em `data/rules.json` (`progression.partyShareRange`, `party.maxMembers`, `party.inviteSeconds`).
6. **Protocolo v+1:** intenções ou mensagens de convite, resposta, saída e expulsão, e o estado do grupo para os membros (lista com id, nome, classe, nível, vida e Mana, zona). As recusas vêm com motivo: grupo cheio, já está em grupo, convite expirado, alvo não existe.
7. Fogo amigo continua impossível (já é), e nada de PvP.

## Critérios de aceite

- testes do Core: dois personagens no mesmo grupo, um mata sozinho e o outro, a 10 tiles, recebe a parte dele; a 40 tiles não recebe; fora do grupo, só o participante recebe;
- testes do servidor: convite, aceite, recusa, expiração, cheio (5º membro), saída, passagem de liderança, troca de zona mantém o grupo, desconexão tira do grupo;
- bots de rede (`Game.NetBots`) com opção de entrar em grupo, rodando 60 s sem quedas;
- `./scripts/check.sh` completo passa.

## Resultado

**Feito (branch `task/T-310-grupo-party`):**

- **Core.** Novo componente replicável `PartyMembership { PartyId }` (`Combat/Progression/ProgressionComponents.cs`). Em `ProgressionSystem.AwardKill`, depois de achar os participantes (como antes), `WithParty` soma os membros dos grupos deles que estão **vivos, no mesmo mundo (zona) e a até `partyShareRange`** do monstro. A divisão continua `Share` sobre o total (quem é de fora do grupo também entra), e o modificador de nível é calculado por pessoa. A mesma lista vai para `Gear.DropLoot`, então quem recebeu a parte também é dono do saque (o primeiro que pegar leva). A ordem é pelo id da entidade (determinística).
- **Regras.** `progression.partyShareRange` (30) e a seção nova `party` (`maxMembers` 4, `inviteTimeout` 30 s) em `data/rules.json`, no schema e no `GameDataValidator` (`PartyRules` em `Definitions/Progression.cs`, `RulesDefinition.Party`).
- **Protocolo v8.** Tag de componente 19 (`PartyMembership`) e mensagens 12–19 em `src/Game.Protocol/PartyMessages.cs`:
  - cliente → servidor: `PartyInvite(Target?, Name?)`, `PartyRespond(InviteId, Accept)`, `PartyLeave`, `PartyKick(MemberId)`;
  - servidor → cliente: `PartyInviteReceived`, `PartyState` (id, líder, "você" e `PartyMemberInfo[]` com id, entidade, nome, classe, nível, vida, Mana, zona e se está vivo), `PartyNotice` (entrou, saiu, expulso, novo líder, desfeito, convite recusado, expirado ou enviado) e `PartyRejected` (cheio, já em grupo, convite expirado, alvo não existe, não é líder, sem grupo, a si mesmo).
- **Servidor.** `src/Game.Server/Parties/PartyManager.cs` cuida dos grupos e convites (sem rede, por meio de `IPartyHost`). O `WorldServer` implementa o host:
  - ao entrar ou trocar de zona, recoloca o `PartyMembership` na entidade nova;
  - na desconexão, tira o personagem do grupo;
  - expira convites pelo tick do servidor;
  - manda o `PartyState` na hora em que alguém entra e confere a cada 5 ticks (`PartyStateIntervalTicks`) se mudou (vida, Mana, zona, líder), reenviando só quando os bytes mudam.
  - O grupo não é gravado.
- **Bots.** `Game.NetBots --party`: grupos de até `maxMembers` em ordem. O primeiro de cada bloco convida os outros pelo nome, de novo a cada 5 s; os outros aceitam. No fim sai a linha `GRUPO`. README atualizado.
- **Testes:**
  - Core (`tests/Game.Core.Tests/PartyTests.cs`): membro a 10 tiles recebe a parte; a 40 tiles não recebe; fora do grupo e de outro grupo não recebem; membro morto não recebe; saque com dono para quem recebeu XP.
  - Servidor (`tests/Game.Server.Tests/PartyTests.cs`): convite por entidade e por nome, aceite, recusa, expiração, alvo inexistente, já em grupo, só o líder convida e expulsa, 5º membro recusado, saída com passagem de liderança, grupo de 1 desfeito, troca de zona mantém o grupo, desconexão tira do grupo.
  - Dados: regras novas, padrões e erros. Protocolo: ida e volta de todas as mensagens novas.
- **Bots de rede, 60 s** (servidor Release `--world --dev-login`, 8 bots):
  - parados: 8/8 em grupo de 4, 0 quedas, 0 recusas;
  - com `--roam`: 130 trocas de zona, 8/8 continuam em grupo de 4, 0 quedas, nenhum erro no log do servidor.
- `./scripts/check.sh` completo passa.

**Decisões:**

- O id de membro na rede é o id da conexão no servidor: é estável enquanto ela durar, e a entidade muda a cada zona. Expulsar usa esse id.
- O campo do convite no JSON é `inviteTimeout` (segundos), e não `inviteSeconds`. O carregador converte `XxxTicks` ← `xxx`, igual a `combatTimeout` e `participationWindow`.
- Sem grupo, quem convida vira líder quando o convite é aceito. Em grupo, só o líder convida. Convidar de novo a mesma pessoa renova o convite.
- Vários convites podem ficar pendentes ao mesmo tempo. Se o grupo encher, quem aceitar depois recebe "grupo cheio" (é o caso do 5º membro). Se o grupo se desfaz, os convites pendentes dele caem e quem aceitar depois recebe "convite expirado".
- Convite por entidade só acha alguém na mesma zona de quem convida. Por nome, acha em qualquer zona, sem diferenciar maiúsculas.
- Quem recebe XP pelo grupo precisa estar vivo e perto. Os participantes diretos continuam como antes, sem checar distância nem se estão vivos.
- Grupo só existe no mundo aberto. A `ZoneRoom` (sala de encontro) ignora as mensagens de grupo.

**Pendências:** a interface no cliente fica para a T-311. A instância de dungeon por grupo fica para a T-350.
