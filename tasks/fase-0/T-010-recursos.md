# T-010 — Recursos por classe orientados a eventos

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** T-008
- **Libera:** T-014, T-022
- **Áreas que pode alterar:** `src/Game.Core/Resources/`, `ClassDefinition` e carregador (campo de regras de recurso), registro de sistemas, `tests/`

## Contexto

Leia `design/classes-v0.1.md` e a seção 3 de `design/class-map-validation-v0.1.md`. Pontos-chave:

- **Vigor** (Guerreiro) deve recompensar decisões: mais ao bloquear golpe forte e interromper do que ao apenas apanhar;
- **Foco** (Arqueiro): acertos a distância segura e em alvo marcado;
- **Mana** (Mago): regeneração passiva, gasto relevante em área;
- **Fé** (Clérigo): curas relevantes, purificação, proteção preventiva, ataques contra corrompidos/mortos-vivos.

## Requisitos

1. **Regras de geração** declaradas em dados na `ClassDefinition` (`resourceRules`), cada uma com:
   - evento gatilho (`damage_taken`, `damage_dealt`, `heal_done`, `shield_applied`, `status_removed`, `interrupt_done`, `ability_executed`…);
   - quantidade fixa ou proporcional ao valor do evento;
   - condições opcionais: valor mínimo, tag do alvo, tag da habilidade, distância mínima/máxima ao alvo, alvo com status de tag X, fonte com status de tag X (ex.: dano recebido **enquanto** em Guarda Firme conta como bloqueio), cura efetiva mínima em % da vida máxima;
   - recarga interna opcional da regra.
2. **Regeneração/decaimento passivo** por segundo (já na definição da T-005), com opção de só regenerar fora de combate se fizer sentido — registrar decisão.
3. Sistema que avalia as regras sobre os eventos do tick.
4. Efeito `resource` (ganhar/gastar) registrado.
5. Evento `ResourceChanged` com motivo (regra/efeito/regeneração).

## Critérios de aceite

- Teste: Guerreiro ganha mais Vigor bloqueando em Guarda Firme do que recebendo o mesmo dano sem guarda;
- teste: Foco só é gerado acima da distância mínima;
- teste: cura com excedente total não gera Fé; purificação gera;
- teste: recarga interna da regra impede geração repetida;
- `./scripts/check.sh` passa.

## Resultado

_(preencher ao concluir)_
