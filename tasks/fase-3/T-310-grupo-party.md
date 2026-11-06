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

_(preencher ao concluir)_
