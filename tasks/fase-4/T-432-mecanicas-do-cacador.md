# T-432 — Core: mecânicas novas do Caçador

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-431
- **Libera:** T-433
- **Áreas que pode alterar:** `src/Game.Core/` (efeitos, estados, dano, áreas, progressão das passivas), `src/Game.Data/` (carga e validação dos campos novos), `data/schemas/`, `tests/Game.Core.Tests/`, `tests/Game.Data.Tests/`
- **Branch:** sai da `main` com a T-431 integrada (ou da branch da T-431, se ela ainda não tiver entrado; registrar a base no Resultado). É integrada na `main` (seção 6h de `tasks/README.md`).

## Contexto

Design em `design/segunda-classe-v0.1.md`, seção 6 (Caçador e a tabela "Mecânicas do Arqueiro"). Aprovado pelo designer **como está** em 2026-11-22. Esta tarefa cria **as mecânicas** no Core, cada uma com teste e com dados de teste. As habilidades de verdade vêm na T-433.

O Caçador tem Flecha Envenenada (passiva), Ferida Aberta, Nuvem Tóxica, Flecha Dispersa e Praga (assinatura). Ele usa um veneno próprio, o **Veneno do Caçador**, que acumula em pilhas. O dano por pilha, o máximo de pilhas e a duração vêm do NH da Flecha Envenenada.

O que já existe e pode ser reaproveitado:

- estados com pilhas (`stacking: stack`, `maxStacks`) e dano periódico (Sangramento, veneno);
- áreas no chão (Campo de Relâmpagos), estados em área;
- passivas como estado permanente aplicado pelo NH (T-403);
- reações depois do dano (`IDamageReaction`, como a vida pelo dano da T-407).

## Requisitos

1. **Efeito em todo acerto** (Flecha Envenenada): uma passiva aplica um estado no alvo **a cada acerto** de quem a tem (ataque básico e qualquer habilidade de dano; não dano periódico, não dano refletido nem desviado). Cada acerto põe 1 pilha e renova a duração de todas.
2. **Estado que depende do NH de outra habilidade** (Veneno do Caçador): o veneno que a Nuvem Tóxica, a Flecha Dispersa e a Praga põem tem o dano, as pilhas e a duração do NH da Flecha Envenenada de quem lança. Decidir e registrar o que acontece sem pontos na Flecha Envenenada (sugestão: usa o NH1).
3. **Dano maior se o alvo andar** (Ferida Aberta): um dano periódico causa mais (+50%, +100% no NH5) nos pulsos em que o alvo se moveu desde o pulso anterior. Usar a posição do Core, não a física do Godot.
4. **Copiar estados para outros monstros** (Flecha Dispersa): o efeito copia estados do alvo com certas etiquetas (veneno e sangramento) para até N monstros a até 4 tiles, com as mesmas pilhas e o tempo que falta, e quem lançou como fonte.
5. **Estado que pula ao acabar** (Praga): enquanto o efeito da Praga dura, quando o Veneno do Caçador **acaba pelo tempo** num monstro, ele passa com todas as pilhas para o monstro mais próximo a até 4 tiles que ainda não tem o veneno. Sem cadeia infinita: o pulo só vale enquanto a Praga dura.
6. Validação dos campos novos e schema.

## Critérios de aceite

Testes no Core:

- com a passiva de teste, 5 disparos seguidos deixam o alvo com o máximo de pilhas (3 no NH1) e a duração renovada; um tique de veneno não põe pilha;
- a Nuvem Tóxica de teste põe o veneno com os números do NH da passiva de quem lança (NH1 e NH5) e, sem a passiva, a regra decidida;
- sangramento de teste com +50%: num alvo parado causa o valor base por pulso, num alvo que anda causa 1,5 vez;
- Flecha Dispersa de teste copia 2 pilhas de veneno e o sangramento para 2 monstros a até 4 tiles, e não para um a 5 tiles nem para um aliado;
- com a Praga ativa, o veneno que acaba num monstro pula para o mais próximo com todas as pilhas; depois que a Praga acaba, não pula mais;
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
