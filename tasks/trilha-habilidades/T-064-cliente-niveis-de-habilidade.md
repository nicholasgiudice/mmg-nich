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

_(preencher ao concluir)_
