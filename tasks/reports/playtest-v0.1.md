# Relatório de playtest v0.1 (T-025)

Playtest do protótipo de combate da fase 0, feito pelo designer no Windows (Godot 4.7.2 .NET) entre 2026-10-16 e 2026-10-17, com o cliente das T-020 a T-024 e os ajustes que o próprio playtest gerou (T-019, T-026, T-027, T-028). Sem jogadores externos nesta rodada.

Pergunta da fase 0 (`docs/vision-mvp-v0.1.md`): **o combate básico é divertido e cada classe joga de modo diferente?**

## 1. Linha do tempo

| Rodada | O que o designer relatou | O que mudou |
|---|---|---|
| 1 | "O combate não está muito fluido. Os bichos acabam sendo muito rápidos e as classes além do Guerreiro estão com dificuldades. O dano das habilidades deveria ser maior." | **T-019**: Lobo, Rato e Salteador 5,0/4,2 → 4,0 (mais lentos que todas as classes); dano das classes +50% (Explosão +30%); monstros reequilibrados (`balance-v0.3.md`). **T-026**: ataque contínuo segurando o botão, sem spam de intenções. |
| 2 | "Ficou melhor assim." Pediu atacar com **Espaço** no alvo do **Tab**. | **T-026**: Espaço ataca o alvo selecionado e mira nele. |
| 3 | "Todas as classes deveriam usar Mana. O Mago deveria ter skills em área que não sejam melee." | **T-027**: Mana para todas as classes (ganhos por evento mantidos); Campo de Relâmpagos (área à distância); Explosão Elemental instantânea (`balance-v0.4.md`). **T-028**: prévia da área segurando a tecla e conjuração ao soltar, mira limitada ao alcance, aviso de conjuração interrompida. |
| 4 | "Agora está legal. Posteriormente precisamos balancear as skills, que deverão ficar mais fortes com pontos de atributos e equipamentos. Vamos precisar reformular todas as skills." | Decisão abaixo. |

## 2. Respostas às perguntas do roteiro

| Pergunta | Resposta |
|---|---|
| Eu sabia por que tomei dano? | Não houve queixa sobre isso. O HUD (números flutuantes, log filtrável, resumo de morte com dano por fonte) e os telegráfos da Matriarca ficaram de pé desde a T-022/T-023; continua valendo conferir com jogadores externos. |
| Tive pelo menos duas formas válidas de vencer? | No início, não para as classes de distância: monstros mais rápidos que o jogador impediam afastar-se, e só o Guerreiro (mais vida, sem precisar fugir) funcionava. Depois da T-019, afastar-se (kite) passou a funcionar — no simulador o Mago que se afasta recebe 34 de dano contra 195 de quem fica trocando golpes. |
| Alguma habilidade foi inútil ou obrigatória? | A Explosão Elemental parecia "corpo a corpo": caía no cursor e era cancelada ao andar (0,6 s de conjuração) sem aviso visível. Resolvido na T-027/T-028. O designer pretende **reformular todas as habilidades** junto com atributos e equipamentos. |
| Precisei me mover? O recurso importou? | Movimento passou a importar depois da T-019. Recursos diferentes por classe (Vigor, Foco, Fé) foram trocados por **Mana para todas** a pedido do designer (T-027); cada classe ainda ganha Mana extra do seu jeito (Guerreiro ao bater/apanhar, Clérigo ao curar...). |
| Foi divertido? O que mais irritou? | Irritou: falta de fluidez (um clique = um ataque; monstros alcançando sempre), dano baixo. Após os ajustes: "agora está legal". |

## 3. Notas (1 a 5)

O designer não atribuiu notas numéricas nesta rodada; o retorno foi qualitativo (seção 1). Notas por encontro e por classe ficam para a próxima rodada, já com o sistema de habilidades reformulado.

| | Nota |
|---|---|
| Encontros (Três Lobos, Lobo e Salteador, Quatro Ratos, Guarda Esquelético, Matriarca) | não atribuída |
| Classes (Guerreiro, Arqueiro, Mago, Clérigo) | não atribuída |

## 4. Os 5 problemas mais importantes

1. **Habilidades precisam ser reformuladas** e escalar com **atributos e equipamentos**, que ainda não existem. Hoje cada efeito tem só um valor `base` fixo; o Core não tem atributos nem a fórmula `base + escala por atributo` prevista em `tech/stack-proposal-v0.1.md`. Balancear números agora seria retrabalho.
2. **Ritmo do combate** dependia de números fora de proporção (monstros mais rápidos que o jogador, dano baixo). Corrigido na T-019, mas a sensação precisa ser revalidada depois da reformulação.
3. **Fluidez do controle**: um clique por ataque, falta de ataque pelo teclado, área do Mago confusa. Corrigido nas T-026 e T-028; pendências menores: não dá para cancelar a área depois de apertar a tecla; apertar uma habilidade um pouco antes de ficar pronta é recusado (sem fila).
4. **Matriarca longa** (~5 min com o grupo de bots; vida 10000 para manter 60–90% de vitória do grupo `kite+dodge`). Trocar vida por pressão/dano na reformulação.
5. **Bots usam pouco as habilidades de área** (só com 2+ inimigos no raio) e não representam um jogador humano: vencem os encontros solo 100% enquanto o designer sentiu dificuldade. O simulador valida limites de design, não a dificuldade sentida — o playtest humano continua necessário.

## 5. Decisão

**Fase 0 aprovada como base do combate.** O núcleo (simulação a 20 Hz, pipeline de habilidades, status, IA, telegráfos, cliente com HUD e controle) funciona e ficou "legal" após os ajustes. **Não** haverá mais uma rodada de ajuste fino de números na fase 0: as habilidades serão **reformuladas** junto com o sistema de progressão.

Próximos passos, nesta ordem:

1. **Design de progressão e habilidades** (nova tarefa T-031): atributos e como são ganhos, equipamento e seus atributos, fórmula de dano/cura/defesa com escala, e diretrizes para reformular as habilidades das quatro classes.
2. **T-030 — planejar a fase 1 (rede)**: servidor, protocolo e jogo online não dependem dos números das habilidades e podem avançar em paralelo ao design.
3. **Depois (fase 2, junto com itens e equipamento)**: atributos e escala no Core, reformulação de todas as habilidades em `data/`, rebalanceamento com o simulador e nova rodada de playtest com jogadores externos.
