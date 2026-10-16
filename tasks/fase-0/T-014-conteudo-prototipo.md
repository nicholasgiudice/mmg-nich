# T-014 — Conteúdo do protótipo em JSON

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** T-009, T-010, T-011, T-012
- **Libera:** T-016, T-024
- **Áreas que pode alterar:** `data/` (exceto `data/schemas/`, salvo correções), `tests/Game.Data.Tests/`

## Contexto

Leia todos os documentos em `design/` e a seção 5 de `tasks/README.md` (escopo do protótipo). O conteúdo é a tradução do design para dados; números são um primeiro chute que a T-016 vai ajustar.

## Requisitos

1. **Classes** (4): vida, velocidade, raio, recurso e regras de geração conforme `design/classes-v0.1.md` e ajustes de `design/class-map-validation-v0.1.md`.
2. **Habilidades**: ataque básico + 3 por classe (tabela da seção 5 do `tasks/README.md`). Cada uma deve ter um papel claro e um custo real:
   - Guerreiro: Golpe Pesado (dano alto + interrupção), Golpe Circular (área curta + ameaça), Guarda Firme (redução de dano frontal, lentidão própria, gera Vigor ao bloquear);
   - Arqueiro: Marca do Caçador (aumenta dano recebido pelo alvo, gera Foco ao acertar marcado), Recuo Rápido (dash para trás), Armadilha de Contenção (área que prende);
   - Mago: Explosão Elemental (área com custo alto de Mana), Prisão de Gelo (controle), Barreira Arcana (escudo próprio);
   - Clérigo: Cura Direta, Escudo Sagrado (escudo preventivo, gera Fé), Corrente Sagrada (dano sagrado forte contra `undead`/`corrupted`, cura limitada).
3. **Monstros**: Rato de Campo (`swarm`), Lobo Cinzento (`pack`), Salteador da Estrada (`ranged_kiter`), Guarda Esquelético (`guardian`, `undead`, guarda frontal, golpe pesado telegrafado), Matriarca da Floresta (boss, 3 fases conforme design), Aranha Menor e Aranha Grande (invocações).
4. **Status e áreas** necessários (veneno, sangramento, teia, lentidão, congelamento, marca, guarda, agressividade da matilha, frenesi).
5. **Arenas** (ASCII), uma por encontro, refletindo o design:
   - Campos (lobos): área aberta com poucos obstáculos;
   - Acampamento (lobo + salteador): obstáculos que o Arqueiro pode usar e que também bloqueiam sua visão;
   - Fazenda (ratos): espaço médio, rotas de aproximação;
   - Portão Partido (guarda): corredor e espaço lateral para flanquear;
   - Ninho da Matriarca: área central aberta, duas bordas livres, sem paredes que atrapalhem a leitura.
6. **Encontros**: os cinco cenários com posições iniciais.
7. Metas iniciais para orientar os números (a T-016 valida):
   - encontros solo duram entre 20 e 60 s com jogo correto;
   - Clérigo solo vence, mas 20–60% mais devagar que as outras classes;
   - Matriarca com grupo de 4 dura 2–4 minutos.

## Critérios de aceite

- `Game.Data` carrega `data/` sem erros de validação (teste automatizado que carrega a pasta real);
- cada habilidade da tabela existe e está ligada à classe certa;
- cada encontro referencia arena, classes e monstros existentes;
- `./scripts/check.sh` passa.

## Resultado

_(preencher: decisões de design tomadas ao traduzir para números)_
