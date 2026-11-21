# Playtest v0.3: fase 3 (T-380)

Servidor na VPS, cliente Godot no Windows. Primeiro mapa completo: Cidade, Campos, Floresta Baixa e Ninho da Matriarca, Ruínas da Estrada Antiga, Cripta e o Vigia Sem Rosto, com grupo, lista de batalha, ouro, lojas e consumíveis. O teste correu de 2026-11-16 a 2026-11-19, com correções feitas no meio, a pedido do designer. O designer o aprovou em 2026-11-19.

**Decisão: seguir para a fase 4 (T-390).** O designer considerou o playtest bom, e os problemas que apareceram já foram corrigidos durante o teste (abaixo). Ficam duas frentes para o planejamento:

1. **Ajustes nas classes:** o designer ainda acha que as classes precisam de ajustes.
2. **Mudança nos níveis:** o designer vai mudar os níveis. No playtest v0.2 ele tinha adiado aumentar o número de níveis ("depois, não agora"); o detalhe da mudança (nível máximo, curva de XP, faixas das zonas) ainda precisa ser definido com ele na T-390.

## O que foi testado

| Parte do roteiro | Feito? | Observação |
|---|---|---|
| 1. Correções da fase 2 (recargas na troca de zona, corpos das invocações, aggro em cadeia, ritmo de XP e queda) | sim | nenhuma voltou como problema |
| 2. Solo do nível 1 ao 8, comprando poções no Templo | sim | tempo por nível e ouro ganho **não foram anotados** |
| 3. Grupo, lista de batalha, Matriarca e Ruínas | sim | rendeu os pedidos de lista compacta, marca do alvo e mini mapa |
| 4. Cripta e Vigia com 4 pessoas no nível 13–15 | sim | rendeu a correção da morte na Cripta (T-385) e do Guarda Esquelético (T-387); o número de pessoas não foi registrado |
| 5. Ping de cada pessoa, em números | **não registrado** | pendente desde a T-111 e a T-230 |
| 6. O que irritou, o que faltou, problemas | sim | virou as tarefas T-381 a T-397 |

## Balanceamento feito durante o teste

Commit `e9e4464` (2026-11-16), pedido do designer:

- regeneração de Mana por INT: 0,05 → 0,1;
- Explosão Elemental: dano base +50% em todos os níveis de habilidade, e 4 acertos no nível 5 (eram 3);
- Poção de Cura Menor e Poção de Mana Menor: recarga de 20 s → 12 s;
- Poção de Cura Menor cura 60 + 2 × VIT (eram 60 fixos).

Isso deixou os encontros mais fáceis do que as metas dos testes de design: **19 casos de `tests/Game.Design.Tests` estão ignorados** com o motivo `PlaytestRebalance.Pending`, esperando a revisão das metas. Essa revisão casa com os ajustes de classe e a mudança de níveis, e deve entrar na fase 4.

## Bugs e problemas (todos corrigidos durante o teste)

| # | Problema | Tarefa |
|---|---|---|
| B1 | Ao morrer na Cripta, o personagem renascia dentro da Cripta | T-385 |
| B2 | O alvo escolhido com Tab às vezes não recebia o ataque básico | T-386 |
| B3 | O Guarda Esquelético não usava o ataque básico | T-387 |
| B4 | Na loja, não dava para comprar mais de uma poção de uma vez | T-384 |

## Pedidos de interface (todos feitos durante o teste)

| # | Pedido | Tarefa |
|---|---|---|
| P1 | Caixa "Lembrar e-mail" no login | T-381 |
| P2 | Lista de batalha compacta (5 linhas, miniatura do monstro, ícone de ataque) | T-382 |
| P3 | Tirar a vida do alvo do topo da tela | T-383 |
| P4 | Marca melhor no monstro selecionado | T-388 |
| P5 | Mini mapa e mapa da zona, com os membros do grupo em qualquer ponto da zona | T-391, T-392, T-393 |
| P6 | Dono por item no saque, e item largado livre para todos | T-394 |
| P7 | Menu do Esc (trocar de personagem, sair do jogo), com espera em combate | T-395, T-396 |
| P8 | Tela de morte com "Retornar à Cidade" | T-397 |

## Observações para a T-390

- **Classes e níveis** são a primeira coisa a conversar com o designer: o que muda em cada classe e como ficam os níveis. Os dois mexem em `data/` (classes, habilidades, `rules.json`), nas faixas de nível das zonas e nos testes de design.
- **Revisar as metas dos testes de design** e reativar os 19 casos marcados com `PlaytestRebalance.Pending`, depois de fechar classes e níveis. Rever com o simulador antes de fechar os números.
- As avaliações pedidas por tarefas da fase 3 (Floresta fácil demais para quem caça sozinho, aviso da Fada de 0,6 s, ritmo do nível 5 ao 8) não tiveram queixa do designer.

## Pendências que continuam abertas

1. **Ping com números por pessoa** e um teste com ping alto de verdade (150 ms ou mais): pendente desde a T-111. Precisa ser feito antes de abrir o jogo a jogadores de fora, e cabe nos testes de carga da fase 4.
2. **Tempo por nível e ouro por hora** medidos em jogo: não foram anotados. Fazem mais sentido depois da mudança de níveis.
