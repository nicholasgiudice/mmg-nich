# T-406 — Core: mecânicas novas do Guardião

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** —
- **Libera:** T-407
- **Áreas que pode alterar:** `src/Game.Core/` (efeitos, estados, dano, ameaça), `src/Game.Data/` (carga e validação dos campos novos), `data/schemas/`, `tests/Game.Core.Tests/`, `tests/Game.Data.Tests/`
- **Branch:** sai de `trilha/segundas-classes` e é integrada nela, não na `main` (seção 6f de `tasks/README.md`).

## Contexto

Design em `design/segunda-classe-v0.1.md`, seção 5 (Guardião e a tabela "Mecânicas do Guerreiro: o que já existe e o que é novo"). Esta tarefa cria **as mecânicas** no Core, cada uma com teste e com dados de teste. As habilidades de verdade vêm na T-408.

O que já existe e pode ser reaproveitado:

- dano em `DamageResolver.Deal` (`src/Game.Core/Combat/Effects/CoreEffects.cs`), com escudos, defesa, multiplicadores de estado e reações depois do dano (`IDamageReaction`);
- estados com `modifiers`, `flags`, `frontal`, `reflect`, `immuneToTags` e efeitos periódicos (`data/statuses.json`, `src/Game.Core/Combat/Status/`);
- avanço (`displacement` com `kind: dash` e `direction: toward_target`), escudo, atordoamento, tirar estados (`remove_status`), estados em aliados num raio (Muralha Viva) e ameaça (`threat`).

## Requisitos

1. **Desviar dano de um aliado** (Interceptação, Juramento; depois o Elo da Vida do Sacerdote):
   - um estado no aliado aponta para quem recebe o desvio (quem aplicou o estado, ou outra entidade escolhida pelo efeito), com a **fração desviada** e uma **redução** opcional sobre a parte desviada;
   - no `DamageResolver`, antes de aplicar o dano no aliado, a fração sai do dano dele e vai para o protetor. A parte desviada passa pela **defesa do protetor** e pela redução do estado, mas não pelos escudos do aliado;
   - dano desviado **não é desviado de novo** (sem cadeia nem laço), e nada é desviado para um protetor morto ou longe demais (distância máxima no estado);
   - eventos e log de combate mostram o desvio (quem protegeu, quanto).
2. **Avançar até um aliado** (Interceptação): `displacement` com `toward_target` quando o alvo é um aliado. Se já funciona, só o teste; se não, o ajuste.
3. **Imunidade a empurrão** (Bastião NH5): flag de estado que ignora `displacement` do tipo `knockback` vindo de inimigos.
4. **Ameaça multiplicada pelo dano** (Golpe de Escudo): o efeito de dano (ou o de ameaça) pode gerar ameaça igual a N vezes o dano causado, em vez da ameaça padrão (`rules.threat.damageMultiplier`).
5. Validação dos campos novos e schema.

## Critérios de aceite

Testes no Core:

- aliado com desvio de 30% recebe 70% do dano; o protetor recebe 30% menos a defesa e a redução dele; o total bate com a conta;
- dois protetores em cadeia não fazem laço; protetor morto ou longe não recebe nada;
- escudo do aliado não absorve a parte desviada;
- Interceptação de teste leva o Guardião até o aliado;
- com a imunidade, um empurrão de monstro não move o alvo; sem ela, move;
- golpe com ameaça × 3 gera três vezes a ameaça do dano;
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
