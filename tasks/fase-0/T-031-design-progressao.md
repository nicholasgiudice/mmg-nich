# T-031 — Design de progressão: atributos, equipamento e reformulação das habilidades

- **Ambiente:** qualquer (documento)
- **Modelo:** Humano + **Forte**
- **Depende de:** T-025
- **Libera:** tarefas de atributos/escala no Core e de reformulação das habilidades (fase 2)
- **Áreas que pode alterar:** `design/`, `tasks/`

## Contexto

O playtest (`tasks/reports/playtest-v0.1.md`) aprovou o combate como base, mas o designer decidiu que **todas as habilidades serão reformuladas** e que elas devem ficar mais fortes com **pontos de atributos e equipamentos**. Hoje cada efeito tem só um valor `base` fixo; o Core não tem atributos nem escala. `tech/stack-proposal-v0.1.md` já prevê o formato `"scaling": { "attack": 1.2 }` nos efeitos. Itens e equipamento estão no esboço da fase 2.

## Objetivo

Um documento de design (`design/progression-v0.1.md`) que permita a outras tarefas implementar atributos, equipamento e as novas habilidades sem decisões de design em aberto.

## Requisitos

1. **Atributos**: lista (ex.: Força, Agilidade, Inteligência, Espírito, Vitalidade), o que cada um afeta (dano físico/mágico, cura, vida, Mana, regeneração, velocidade de ataque...), valores iniciais por classe, como o jogador ganha pontos (nível? marcos?) e se a distribuição é livre ou guiada.
2. **Equipamento**: espaços (arma, armadura, acessórios...), que atributos/efeitos os itens dão, raridade, e o que cai de monstros e bosses do primeiro mapa (`design/first-map-v0.1.md`).
3. **Fórmulas**: dano, cura, escudo e defesa/resistência com escala por atributo (ex.: `base + escala × atributo`), crítico (se houver), e como o nível/equipamento esperado em cada área do mapa se traduz em números.
4. **Diretrizes para reformular as habilidades**: papel de cada classe, quantas habilidades no início e como novas se desbloqueiam, faixas de custo de Mana e recarga por tipo de habilidade, o que escala com o quê. Partir das listas de `design/classes-v0.1.md`.
5. **Metas para o simulador**: duração e taxa de vitória esperadas por encontro em cada faixa de progressão, para os testes de design futuros.
6. Lista de decisões pendentes (se houver) para o humano.

## Critérios de aceite

- `design/progression-v0.1.md` criado, revisado e aprovado pelo designer;
- cada habilidade das quatro classes tem papel, custo/recarga alvo e atributo de escala definidos;
- fórmulas com exemplos numéricos.

## Resultado

**Proposta entregue em 2026-10-17, aguardando aprovação do designer** (critério de aceite). Documento: `design/progression-v0.1.md`.

**Decisões do designer usadas:** nível com XP; pontos de atributo totalmente livres; habilidades liberadas por nível com barra de 6 (básico + 1–5); 6 espaços de equipamento.

**Conteúdo:**
- Regra de calibração: nível 1 com equipamento inicial = números atuais do protótipo (todas as escalas conferidas: Golpe de Espada 18, Golpe Pesado 51, Disparo 17, Projétil 15, Explosão 44, Cura 50, Corrente 39, Escudos 45; vida 240/130/110/150).
- Níveis 1–15, curva de XP `50 × L^1,8` (~600 monstros normais, ~6–7 h), XP por monstro e por diferença de nível, níveis por área do mapa.
- 5 atributos (FOR, AGI, INT, ESP, VIT), perfil inicial por classe, vida/Mana/regeneração, defesa (armadura e resistência), crítico.
- Fórmula `base + Σ coeficiente × estatística` com `scaling` nos efeitos (formato já previsto na proposta de stack) e exemplo do nível 1 ao 15.
- Equipamento: 6 espaços, armas por classe, 3 tipos de armadura, raridades e afixos, tabela de queda.
- Habilidades: 4 normais + 1 assinatura por classe (níveis 1, 1, 4, 8, 12), com papel, custo, recarga e escala; 2 por classe ficam fora do MVP.
- Metas do simulador por faixa de progressão e ordem de implementação na fase 2.

**Revisão 1 (designer: "o Arqueiro ficou fraco, não tem um atributo que aumenta o ataque"):** cada classe agora tem um atributo de ataque explícito (FOR Guerreiro, AGI Arqueiro, INT Mago, ESP Clérigo); Disparo escala 1,0 × AGI (igual ao Golpe de Espada com FOR); a Marca do Caçador escala com AGI (16% + 0,75% × AGI, máx. 60%); Armadilha 2 + 0,5 × AGI; Chuva de Flechas passa para o nível 4 e a Armadilha para o 8. Tabela comparativa Guerreiro × Arqueiro no nível 15 na seção 7.

**Pendências (seção 10 do documento):** redistribuição de pontos, habilidades fora do MVP, assinaturas, nível máximo 15, arma do Mago, crítico em cura.
