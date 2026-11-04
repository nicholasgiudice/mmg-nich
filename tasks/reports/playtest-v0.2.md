# Playtest v0.2: fase 2 (T-230)

Servidor na VPS (Docker, API com HTTPS), cliente Godot no Windows. Contas e personagens persistentes, Cidade e Campos da Vigília online, a Matriarca como instância de encontro. Relato do designer em 2026-11-04.

**Decisão: seguir para a fase 3 (T-240).** Nenhum problema impede o jogo. A fase 3 deve começar por um bloco de correções (os bugs e o ajuste de queda de itens abaixo), antes do conteúdo novo. Os pedidos de sistemas novos (grupo, lista de batalha, moeda, lojas, consumíveis, mapas maiores) também entram no planejamento da fase 3.

## O que foi testado

| Parte do roteiro | Feito? | Observação |
|---|---|---|
| 1. Conta, personagem, nível 1 → 4–5 nos Campos, equipar, gastar pontos | sim | |
| 2. Grupo nos Campos e Matriarca no nível 7–8 | sim, com **2 pessoas** | o roteiro pedia 2–4; o teste com 4 pessoas (pendente desde a T-111) continua em aberto |
| 3. Anotações (tempo de nível, queda de itens, pontos, ping, problemas) | sim | ping relatado como "bem estável", sem números por pessoa |

## Respostas

- **Tempo para subir de nível:** rápido. Aumentar um pouco o XP exigido por nível. Aumentar o número de níveis fica para depois, **não agora**.
- **A queda de itens empolga?** Cai item demais: a taxa de queda está **alta**.
- **A distribuição de pontos é clara?** Sem queixa.
- **Ping:** estável para as duas pessoas. Não houve relato de pulos nem de dessincronização.

## Bugs

| # | Problema | Onde provavelmente mexer |
|---|---|---|
| B1 | **Cooldown das habilidades zera ao trocar de mapa** (Cidade ↔ Campos). Com isso, dá para trocar de zona só para recarregar as habilidades. | servidor/Core: estado de cooldown precisa viajar com o personagem na troca de zona (T-214) |
| B2 | **Os corpos das aranhas ficam no chão para sempre** na Matriarca. | Core/cliente: remover a entidade morta (ou o cadáver) depois de um tempo, em ticks |
| B3 | **Um monstro puxa todos os outros:** quando um monstro próximo pega o jogador como alvo, monstros longe, fora da tela, também vêm. | Core: IA de aggro, provavelmente `aggroRange`/propagação de alvo entre monstros (`ai.swarm` tem `aggroRange: 14`) |

## Ajustes de balanceamento

1. **Diminuir a taxa de queda de itens** (`data/items.json`, tabelas de queda).
2. **Aumentar um pouco o XP necessário por nível** (`data/rules.json`: `xpCurveBase`/`xpCurveExponent`). Não aumentar o nível máximo agora.

Rever com o simulador (T-206) antes de fechar os números.

## Pedidos de sistemas novos

| # | Pedido | Motivo relatado |
|---|---|---|
| P1 | **Lista de batalha**: painel com todos os monstros visíveis na tela, onde se clica para escolher o alvo | mirar um monstro em movimento clicando nele é difícil |
| P2 | **Grupo (party)** com divisão de XP | jogar com mais pessoas hoje não divide a experiência |
| P3 | **Moeda (ouro)**, que cai dos monstros | base para uma economia no servidor |
| P4 | **NPCs vendedores e lojas**: comprar consumíveis, como poções de vida e de mana | a poção hoje aparece como habilidade, e não deveria |
| P5 | **Consumíveis na barra de atalhos**: a barra passa a aceitar itens consumíveis além de habilidades | acompanha P4 |
| P6 | **Mapas bem maiores** | os mapas atuais estão pequenos |

### Observações para a T-240

- **Economia está fora de escopo pelo `AGENTS.md`**, salvo se a tarefa pedir explicitamente, e a visão (`docs/vision-mvp-v0.1.md`) adia a economia para depois do núcleo. O pedido do designer (P3, P4) é a autorização para incluí-la. As tarefas da fase 3 que criarem moeda e lojas precisam dizer isso explicitamente. Sugestão: limitar ao mínimo (ouro que cai, NPC vende consumíveis por preço fixo, sem troca entre jogadores) e registrar a decisão na visão ou no design.
- O esboço da fase 3 já previa "NPCs, missões mínimas, mercado e armazém": P3 e P4 se encaixam ali.
- `design/class-map-validation-v0.1.md` (item 7) já diz que consumíveis devem permitir grupos sem Clérigo, o que reforça P4 e P5.
- P6 (mapas maiores) influencia a Floresta e as Ruínas, que vão ser feitas na fase 3, e pode pedir revisão da Cidade e dos Campos (T-055). Também toca o interest management (T-106) e o surgimento de monstros (T-215).

## Pendências para o próximo playtest

1. **Quatro pessoas reais**, em redes diferentes, na Matriarca: pendente desde a T-111. Este teste teve 2.
2. **Ping com números por pessoa** e um teste com ping alto de verdade (150 ms ou mais).
3. Confirmar as correções B1–B3 e o novo ritmo de XP e de queda de itens.
