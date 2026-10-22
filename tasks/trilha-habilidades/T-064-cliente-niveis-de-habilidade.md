# T-064 — Cliente: escolha de níveis de habilidade e visual dos acertos extras

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** T-062
- **Libera:** nova rodada de playtest
- **Áreas que pode alterar:** `client/`

## Contexto

Enquanto não existem níveis do personagem e XP (fase 2), o jogador precisa conseguir testar os NH no protótipo. Quando a progressão existir, a mesma tela vira a distribuição de pontos de habilidade.

## Requisitos

1. **Menu de cenários**: escolher o NH de cada habilidade da classe (1–5; assinatura 0–3) ou um perfil pronto (NH1, focado, espalhado), com a regra dos 15 pontos visível (sem bloquear no modo de teste).
2. **Barra de habilidades**: mostra o NH de cada habilidade (ex.: pontos ou "III"); tooltip com o efeito do NH atual e do próximo.
3. **Visual**: flechas extras, explosões repetidas, giros, saltos da Corrente e cargas (ex.: "2" no Recuo) legíveis, sem esconder telegráfos.
4. Estrutura pronta para virar a tela de habilidades com pontos quando a fase 2 trouxer níveis do personagem.

## Critérios de aceite

- é possível jogar qualquer encontro com qualquer combinação de NH escolhida no menu;
- os acertos extras aparecem claramente;
- `dotnet build client/` e `./scripts/check.sh` passam.

## Resultado

Feito no Windows. Verificado no Godot 4.7.2 com capturas (menu, Disparo Potente NH5, Corrente Sagrada NH5, Chuva de Flechas NH5 com o Recuo NH5).

- **Menu** (`MenuScreen` + `Match/SkillPlan.cs`): painel "Níveis de habilidade" à direita, com um seletor de NH por habilidade da classe escolhida (normais 1–5, assinatura 0–3), resumo do NH escolhido, botões de perfil (NH1, Focado 5+5, Espalhado 3+3+3+3, os mesmos do simulador) e "Pontos usados: X de 15", em vermelho quando passa, **sem bloquear** (modo de teste). O plano fica guardado por classe durante a sessão e vale para "Reiniciar" e "Próximo encontro".
- **Partida** (`MatchSetup`): os NH do plano vão para o jogador (`PlayerSpawnDefinition.Ranks`); assinatura no NH0 fica fora da barra. **Bots aliados ficam no NH1** (decisão: o teste é do jogador; os bots seguem o balanceamento medido).
- **Depuração:** `--ranks=nh1|focado|espalhado` e `--rank=habilidade:NH,...` pulam o menu como os outros argumentos.
- **Barra:** NH em romanos no canto inferior esquerdo de cada slot; cargas por recarga no canto superior direito (ex.: "2" no Recuo NH5), e o slot fica livre enquanto houver carga. Tooltip com o NH atual e o próximo (`Hud/AbilityText.cs` gera o texto da definição: custo, recarga, cargas, conjuração, acertos, efeitos, saltos, alvos extras, áreas, levantar caído).
- **Correção ligada:** HUD e entrada usavam a definição do NH1. Agora usam a do NH do jogador (raio da prévia de área, alcance, recarga, cargas).
- **Visual** (`WorldView`): habilidade com vários acertos mostra "Nome ×3" uma vez no jogador e um rastro fino até o alvo a cada flecha (só em alvo único: áreas já têm telegráfo). Os saltos da Corrente desenham um raio dourado entre os alvos (0,8 s). Unidade levantada mostra "Levantado!". Números que saem em sequência rápida na mesma unidade (três flechas, giros) ficam lado a lado em vez de empilhados. Nada é desenhado por cima dos telegráfos (rastros ficam na camada de rastros).
- **Fase 2:** `SkillPlan` já tem `PointBudget` (hoje 15) e `Enforce` (hoje desligado): com níveis do personagem basta passar o nível como orçamento, ligar `Enforce` e permitir NH0 nas normais.

**Pendências / observações:**
- Áreas não têm nome nos dados, então o log mostra o id (ex.: `area.rain_slow surgiu`); isso já acontecia antes.
- A linha de ajuda no canto inferior esquerdo encosta no começo da barra em 1280×720 (já acontecia antes, não mudou com a tarefa).
- Não há testes automáticos do cliente (o projeto não tem testes de Godot); a verificação foi por capturas.
