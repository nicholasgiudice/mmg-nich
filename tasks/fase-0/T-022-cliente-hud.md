# T-022 — Cliente: HUD e log de combate

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** T-010, T-013, T-021
- **Libera:** T-025
- **Áreas que pode alterar:** `client/`

## Contexto

O HUD só lê estado e eventos do `IGameAuthority`. Clareza acima de enfeite: `design/combat-encounters-v0.1.md` pede que o combate seja compreensível sem excesso de efeitos.

## Requisitos

1. **Quadro do jogador**: vida (com sobreposição de escudo), recurso com cor por tipo (Vigor, Foco, Mana, Fé).
2. **Barra de habilidades**: ícone/sigla, tecla, custo, varredura de recarga, estado indisponível (sem recurso, fora de alcance).
3. **Barra de conjuração** do jogador.
4. **Quadro do alvo**: nome, vida, status ativos, barra de conjuração do alvo (essencial para interrupções).
5. **Quadros de grupo** para o encontro de grupo (vida, recurso, status negativos removíveis destacados).
6. **Números flutuantes** de dano e cura (cores por escola; absorvido indicado).
7. **Log de combate** (T-013) em painel recolhível, com filtro por categoria.
8. **Resumo de morte** ao morrer: dano por fonte e últimas entradas.

## Critérios de aceite

- Todos os elementos funcionam nos cinco encontros;
- em 30 segundos de combate, um jogador consegue responder "por que perdi vida?" olhando o HUD/log.

## Resultado

_(preencher ao concluir)_
