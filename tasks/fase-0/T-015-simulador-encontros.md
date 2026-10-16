# T-015 — Simulador headless de encontros e bots

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-011
- **Libera:** T-016, T-024
- **Áreas que pode alterar:** `src/Game.Bots/` (novo), `tests/Game.Sim/` (novo), `MmoRpg.sln`, `tests/`

## Contexto

Os documentos de design definem falhas mensuráveis ("se o Guerreiro vence só com Provocação e ataque básico, o combate está raso"). O simulador permite testá-las milhares de vezes sem abrir o jogo. Os bots também serão usados pelo cliente (companheiros controlados por IA no encontro de grupo, T-024) e, na fase 1, como clientes de carga.

## Requisitos

1. **`src/Game.Bots`** (biblioteca, `TargetFramework=$(SharedTargetFramework)`, referencia só `Game.Core`): interface de controlador que, a cada tick, lê o estado do mundo e devolve intenções para uma entidade jogador. Estratégias:
   - `idle` (parado, só ataque básico se houver alvo em alcance);
   - `basic_only` (persegue e usa só ataque básico);
   - `rotation` (lista de prioridade por classe com condições simples: recurso, recarga, vida, número de inimigos próximos);
   - `kite` (para classes à distância: mantém distância e usa obstáculos de forma simples);
   - variações por classe para testar falhas do design, ex.: Guerreiro `taunt_and_basic`, Mago `explosion_spam`, Clérigo `heal_only`.
   - Estratégias de grupo (Matriarca): cada bot com um papel; regra simples de sair de telegráfos e áreas perigosas (opcional por estratégia, para comparar "parado" vs "se move").
2. **`tests/Game.Sim`** (console): carrega `data/`, roda encontro × estratégias × N seeds, mede:
   - taxa de vitória, tempo até vitória (média/p50/p90);
   - dano recebido, cura, recurso gasto e desperdiçado (acima do máximo);
   - usos por habilidade, mortes, troca de fase de boss.
3. Saída em tabela no terminal e JSON (`--out arquivo.json`).
4. CLI exemplo: `dotnet run --project tests/Game.Sim -- --encounter encounter.warrior_wolves --strategy rotation --runs 200`.
5. Pode rodar em paralelo entre seeds (cada mundo é independente).
6. Até a T-014 entrar, usar fixtures de teste; depois funciona com `data/` real sem mudança de código.

## Critérios de aceite

- Mesma seed → mesmo resultado (teste);
- 1000 execuções de um encontro solo de fixture terminam em menos de 1 minuto na VPS (2 vCPU);
- README curto em `tests/Game.Sim/README.md` com exemplos;
- `./scripts/check.sh` passa.

## Resultado

_(preencher ao concluir)_
