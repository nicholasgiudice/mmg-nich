# T-221 — Cliente: tela de personagem (atributos) e de habilidades (pontos e NH)

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** T-203, T-212
- **Libera:** T-230
- **Áreas que pode alterar:** `client/`

## Contexto

`design/progression-v0.1.md` §3 e §7: pontos livres com o atributo sugerido e prévia do efeito de cada ponto ("+1 FOR: Golpe Pesado 51 → 53"); pontos de habilidade com prévia do próximo NH. O `SkillPlan` da T-064 já tem `PointBudget` e `Enforce` pensados para isso.

## Requisitos

1. Nível, XP e barra de XP no HUD; aviso de "subiu de nível" com pontos disponíveis.
2. Tela de personagem: atributos, estatísticas derivadas (vida, Mana, crítico, velocidade de ataque, armadura, resistência), botão + por atributo com prévia, atributo sugerido destacado.
3. Tela de habilidades: o painel da T-064 ligado aos pontos de verdade (`Enforce`), requisitos de nível por NH, prévia do próximo NH; manda as intenções da T-203.
4. Redistribuir (só fora de combate, T-203).

## Critérios de aceite

- gastar pontos online e ver o efeito na barra e nos números;
- `dotnet build client/` e `./scripts/check.sh` passam.

## Resultado

_(preencher ao concluir)_
