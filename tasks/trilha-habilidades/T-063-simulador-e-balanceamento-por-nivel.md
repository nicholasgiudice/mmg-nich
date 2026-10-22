# T-063 — Simulador e balanceamento por nível de habilidade

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** T-062
- **Libera:** nova rodada de playtest
- **Áreas que pode alterar:** `tests/Game.Sim/`, `tests/Game.Sim.Tests/`, `tests/Game.Design.Tests/`, `src/Game.Bots/` (usar NH e efeitos novos), `data/` (números), `tasks/reports/`

## Contexto

Os testes de design validam o protótipo com todas as habilidades no NH1. Com os NH implementados, é preciso medir distribuições de pontos e garantir que nenhum NH ou combinação quebra os encontros.

## Requisitos

1. Simulador aceita NH por classe/habilidade (`--rank class.archer:archer.power_shot=5` ou perfis prontos: "NH1", "focado", "espalhado").
2. Bots usam os comportamentos novos (cargas, áreas, saltos) sem estratégia nova obrigatória.
3. Testes de design por perfil (metas de `design/progression-v0.1.md` §8): distribuições "duas habilidades no NH5" e "pontos espalhados" vencem ≥ 70% nos encontros solo; nenhuma habilidade no NH5 sozinha resolve um encontro sem movimento (mesma regra da Explosão).
4. Ajustar números em `data/` se necessário e atualizar `design/skills-v0.1.md` com o que mudou.
5. Relatório `tasks/reports/balance-v0.6.md`.

## Critérios de aceite

- testes de design passando para NH1 e para os perfis novos;
- relatório criado.

## Resultado

Feito no Windows (pedido do humano). Detalhes e tabelas em `tasks/reports/balance-v0.6.md`.

- **Simulador:** `--ranks nh1|focado|espalhado` e `--rank classe:habilidade=NH` (repetível, por cima do perfil); o rótulo do lote mostra `NH:<perfil>`. `RunSpec.Ranks` aplica os NH aos jogadores do encontro (`RankProfiles` em `tests/Game.Sim`). Os perfis gastam os 15 pontos do nível 15: `focado` = 5+5+1+1 e assinatura 3; `espalhado` = 3+3+3+3 e assinatura 3.
- **Bots:** passam a ler a definição do NH que o personagem tem (`BotContext.AbilityDef`), em vez de sempre a do NH1. Cargas, acertos extras, saltos e alvos extras vêm do Core; nenhuma estratégia nova foi preciso.
- **Testes de design** (`RankDesignTests`): `focado` e `espalhado` vencem ≥ 70% nos 4 encontros solo (na prática 100%); na Matriarca nenhum perfil vence menos que o NH1; "habilidade NH5 sozinha não resolve" vale para Golpe Pesado, Golpe Circular, Disparo Potente e Chuva de Flechas. Os 23 testes do NH1 seguem iguais.
- **Números:** nenhum mudou em `data/` (e `design/skills-v0.1.md` não precisou mudar).

**Pendência — decisão de design:** no Mago, a Explosão e o Campo no NH3+ resolvem os 4 ratos sozinhos (spam mais rápido que a rotação). Nem custo de Mana (máximo 100) nem cortar o dano por explosão até 1,5× o NH1 inverte isso: contra monstros do nível 1, qualquer Explosão mais forte que a do NH1 mata antes de a Mana acabar. O teste está escrito e **ignorado** (`Mage_rank_5_area_skill_does_not_solve_the_rats_alone`) até o designer escolher entre as opções do relatório (§4). A regra da Corrente Sagrada não se aplica: a rotação solo do Clérigo já é só a Corrente.

**Limite das medições:** os encontros são os do protótipo (monstros do nível 1) e os valores dos NH são o "perfil nível 1"; os perfis medem distribuições entre si, não a curva de dificuldade. Para medir isso faltam monstros e atributos por nível (fase 2).

**Desvio:** `tests/Game.Sim/README.md` documenta as opções novas.
