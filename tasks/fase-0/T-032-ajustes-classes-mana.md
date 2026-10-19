# T-032 — Ajustes de classes, Mana por tempo e progressão das habilidades

- **Ambiente:** VPS ou Windows (dados, simulador e documentos)
- **Modelo:** Médio
- **Depende de:** T-027, T-031
- **Libera:** T-120 (planejamento da fase 2 usa o design atualizado)
- **Áreas que pode alterar:** `data/`, `tests/` (testes que leem os dados reais), `design/`, `docs/`, `tech/` (só texto que cite recursos de classe), `README.md`, `tasks/`

## Contexto

Pedidos do designer (2026-10-19):

1. O Arqueiro precisa de uma habilidade de dano (hoje só o ataque básico causa dano): um tiro mais forte. O arquivo das classes cita habilidades que não existem no jogo — alinhar.
2. O ataque básico do Mago não gasta Mana; Campo de Relâmpagos com área maior.
3. Mana não pode vir de ataques: hoje Guerreiro, Arqueiro e Clérigo ganham Mana ao bater/curar e o Mago só regenera com o tempo.
4. Progressão final: a cada nível o personagem ganha pontos de atributo e **um ponto de habilidade**; cada habilidade tem **5 níveis**, cada nível aumenta o valor base e alguns mudam o comportamento (ex.: Disparo Potente 1 → 2 → 3 flechas; Explosão Elemental 1 → 2 → 3 acertos). Com 15 níveis o jogador não pode ter tudo no máximo.
5. Documento de ideias futuras, começando com equipamentos de roubo de vida e de Mana.

Decisões do designer: barra continua com **5 teclas** (4 habilidades + assinatura); assinatura liberada no **nível 12** com **3 níveis**.

## Requisitos

1. **Dados do protótipo**: `archer.power_shot` (Disparo Potente) no lugar da Armadilha na lista do Arqueiro (a Armadilha fica para depois do MVP); Projétil Arcano sem custo; Campo de Relâmpagos com raio 3,5; sem regras de ganho de Mana por evento (`resourceRules`) — só regeneração por tempo.
2. Rebalancear com o simulador; testes de design passando (limites só mudam com justificativa); relatório `tasks/reports/balance-v0.5.md`.
3. `design/progression-v0.1.md`: pontos e níveis de habilidade (substitui a liberação por nível), conjunto final de habilidades, tabela de níveis 1–5 (e 1–3 da assinatura) de todas as habilidades com o que muda em cada uma.
4. `design/classes-v0.1.md`: as 5 habilidades do MVP de cada classe e uma seção "Depois do MVP"; Mana só por tempo.
5. `design/ideias-futuras.md` com roubo de vida e de Mana em equipamentos.
6. Demais documentos (`docs/`, `design/`, `tech/`, `README.md`) sem citar Vigor/Foco/Fé ou "recurso próprio por classe" como regra atual. Resultados de tarefas já concluídas ficam como histórico.

## Fora do escopo

Implementar níveis de habilidade, pontos e múltiplos acertos no Core (fase 2, detalhada pela T-120).

## Critérios de aceite

- Arqueiro com habilidade de dano; Mago sem custo no básico; nenhuma classe ganha Mana por atacar/curar;
- toda habilidade do MVP tem os 5 (ou 3) níveis descritos;
- `./scripts/check.sh` completo passa.

## Resultado

Concluída em 2026-10-19 no Windows. Relatório: `tasks/reports/balance-v0.5.md`.

**Feito — dados do protótipo:**
1. **Disparo Potente** (`archer.power_shot`, tecla 1 do Arqueiro): alvo único, alcance 9, 40 de dano, custo 25, recarga 7 s. A Armadilha de Contenção saiu da lista do Arqueiro (fica nos dados para uma especialização), porque a barra tem 5 teclas.
2. Projétil Arcano sem custo; Campo de Relâmpagos com raio 3,5 (alvo e zona).
3. **Mana só por tempo**: removidas as `resourceRules` das três classes; regeneração 5/s para todas.
4. Reequilíbrio (detalhes no relatório): Explosão 80 de Mana e 8 s de recarga; mordida do Lobo 12; Guarda Esquelético 440 de vida; Matriarca 8000 de vida e mordidas 62 (luta ~3,5 min).
5. Testes de conteúdo: lista do Arqueiro, "Mana só por tempo" (sem `resourceRules`, regeneração > 0) e "Arqueiro tem habilidade de dano / básico do Mago sem custo".
6. Teste de design do Arqueiro parado: o `idle` continua sem vencer; o `rotation` (que agora vence com o Disparo Potente) passou a ser medido pelo custo — ≥ 1,4 × o dano do `kite` (justificativa no relatório).

**Feito — design e documentos:**
- `design/progression-v0.1.md` §7 reescrita: 1 ponto de habilidade por nível; 4 habilidades normais com 5 NH e assinatura com 3 NH (níveis 12, 13, 15); requisito `2n − 1` por NH; multiplicador por NH (1,0 → 1,8) e mudanças de comportamento no NH3 e NH5 de todas as 20 habilidades (ex.: Disparo Potente 1 → 2 → 3 flechas; Explosão 1 → 2 → 3 acertos; Golpe Circular 1 → 2 → 3 giros); exemplos de distribuição dos 15 pontos e comparação Guerreiro × Arqueiro no nível 15. Também: calibração pelo NH1, Mana só por tempo (§4), metas do simulador para distribuições (§8), implementação (§9) e decisões de 2026-10-19 (§10).
- `design/classes-v0.1.md` reescrito: 5 habilidades do MVP por classe + "Depois do MVP"; Mana só por tempo; atributos de referência.
- `design/ideias-futuras.md` (novo): roubo de vida e roubo de Mana em equipamento.
- `design/combat-encounters-v0.1.md` e `design/class-map-validation-v0.1.md`: sem Vigor/Foco e só com habilidades do MVP.
- `docs/vision-mvp-v0.1.md`, `README.md`, `tech/stack-proposal-v0.1.md`: Mana para todas, por tempo; o mecanismo de ganho por eventos continua no Core, sem uso nos dados.
- Resultados de tarefas antigas e relatórios anteriores ficaram como histórico.

**Decisões:** a Armadilha foi a habilidade que saiu do Arqueiro (Recuo é o que permite o kite; Chuva é a área dele). Nos NH com acertos extras, cada acerto vale uma porcentagem do NH1 (3 × 85% = 2,55 ×), para compensar não ter efeito extra.

**Verificação:** `./scripts/check.sh` completo OK (23 testes de design).

**Pendências:** implementação dos pontos/NH/múltiplos acertos no Core (fase 2, T-120); margem estreita da Matriarca (89% para limite de 90%).
