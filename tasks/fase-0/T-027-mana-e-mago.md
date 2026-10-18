# T-027 — Mana para todas as classes e Mago com área à distância

- **Ambiente:** VPS ou Windows (só `dotnet`, simulador e testes)
- **Modelo:** Médio
- **Depende de:** T-019
- **Libera:** T-028, nova rodada de playtest (T-025)
- **Áreas que pode alterar:** `data/` (classes, habilidades, áreas — números e conteúdo), `tests/` (testes que leem os dados reais), `design/classes-v0.1.md` (registrar a decisão), `tasks/`

## Contexto

Decisões do designer após o playtest (2026-10-17):

1. **Todas as classes usam Mana** para as habilidades (antes: Vigor, Foco, Fé e Mana). Muda o design de `design/classes-v0.1.md`, que dava a cada classe um recurso próprio gerado por eventos.
2. **O Mago precisa de habilidades em área que não sejam corpo a corpo**: adicionar uma nova habilidade de área à distância e tornar a Explosão Elemental instantânea (sem 0,6 s de conjuração, que era cancelada ao andar).

A pré-visualização da área e o aviso de conjuração interrompida ficam na T-028 (cliente).

## Requisitos

1. Recurso das quatro classes = `mana`, com regeneração passiva para todas; custos das habilidades em `mana`. As regras de ganho por evento de cada classe (`resourceRules`) podem continuar, agora gerando Mana, para preservar a identidade de cada classe — registrar a decisão.
2. **Campo de Relâmpagos** (lista de habilidades do Mago no design): área à distância (`circle_at_point`) que cria uma zona de dano periódico por alguns segundos (`spawn_area`). Entra como 4ª habilidade do Mago.
3. **Explosão Elemental instantânea** (`castTime` 0), compensando com custo/recarga.
4. Rebalancear com o simulador; todos os testes de design continuam passando (ajustar limites só com justificativa no relatório).
5. Atualizar `design/classes-v0.1.md` (recursos) e criar `tasks/reports/balance-v0.4.md`.

## Critérios de aceite

- nenhuma habilidade de classe custa outro recurso que não `mana`;
- Mago com 2 habilidades de dano em área usáveis à distância e nenhuma com conjuração parada;
- `./scripts/check.sh` completo passa; relatório criado.

## Resultado

Concluída em 2026-10-17 no Windows. Relatório: `tasks/reports/balance-v0.4.md`.

**Feito:**
1. **Mana para todas as classes** (`data/classes.json`): Guerreiro e Arqueiro começam com 100 e regeneram 4/s, Clérigo 100 e 3/s, Mago igual (100, 5/s). Custos em `mana` com os mesmos valores. As regras de ganho por evento (`resourceRules`) foram mantidas, agora gerando Mana — cada classe continua com um jeito próprio de reabastecer (decisão registrada em `design/classes-v0.1.md`).
2. **Campo de Relâmpagos** (`mage.lightning_field`, tecla 4 do Mago; nova `area.lightning_field`): área à distância (alcance 9, raio 2,5), 12 de dano no impacto + zona de 5 s com 8 de dano por segundo; custo 40, recarga 12 s.
3. **Explosão Elemental instantânea** (sem os 0,6 s de conjuração), recarga 4 → 6 s.
4. Matriarca vida 9000 → 10000 (grupo `kite+dodge` foi a 91%; agora 78%).
5. Teste de conteúdo atualizado (`PrototypeContentTests`: lista de habilidades do Mago).

**Verificação:** 23 testes de design passam sem afrouxar limites; `./scripts/check.sh` completo OK.

**Decisões:** o dano no impacto do Campo faz os bots o tratarem como dano em área (sem mexer em `Game.Bots`). A conversão de Vigor/Foco/Fé para Mana mantém os ganhos por evento para não apagar a identidade das classes.

**Pendências:** bots usam pouco as áreas (`minAoe=2`); Matriarca segue longa (~5 min) — trocar vida por dano numa próxima rodada; pré-visualização e aviso de interrupção na T-028.
