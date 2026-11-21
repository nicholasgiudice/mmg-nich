# T-407 — Core: mecânicas novas do Berserker

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-406 (sai da branch dela: mesmos arquivos de dano e estados)
- **Libera:** T-408
- **Áreas que pode alterar:** `src/Game.Core/` (efeitos, estados, dano, áreas, recargas), `src/Game.Protocol/` (só formato de rede de eventos novos e `ProtocolVersion`), `src/Game.Data/` (carga e validação dos campos novos), `data/schemas/`, `tests/Game.Core.Tests/`, `tests/Game.Data.Tests/`
- **Branch:** sai de `trilha/segundas-classes` e é integrada nela, não na `main` (seção 6f de `tasks/README.md`).

## Contexto

Design em `design/segunda-classe-v0.1.md`, seção 5 (Berserker e a tabela de mecânicas). Esta tarefa cria **as mecânicas** no Core, cada uma com teste e com dados de teste. As habilidades de verdade vêm na T-408.

**Base:** crie a branch a partir de `task/T-406-mecanicas-do-guardiao`, que ainda não está na trilha porque subiu `ProtocolVersion` para 16. Se esta tarefa precisar de evento novo na rede, suba o número a partir do da T-406. A T-407 entra na `trilha/segundas-classes` junto com a rodada do protocolo (seção 6f de `tasks/README.md`).

Já existem: estado com mais dano causado, velocidade e dano recebido (o `status.frenzy` dos monstros), metade da duração de controles (`controlDurationMultiplier`), reforço do próximo golpe (`empower`, do Recuo Rápido), vários acertos com intervalo (`hits`), Sangramento e reações depois do dano (`IDamageReaction`).

## Requisitos

1. **Dano extra pela vida do alvo** (Execução): o efeito de dano ganha um bônus quando o alvo está abaixo de uma fração da vida máxima (por exemplo, `+50%` abaixo de `30%`), conferido no momento do acerto.
2. **Zerar a recarga ao matar** (Execução NH5): se o acerto da habilidade mata o alvo, a recarga dela volta a zero.
3. **Vida pelo dano causado** (Sede de Sangue, passiva; depois o Selo da Luz e o Julgamento do Inquisidor):
   - modificador de estado: fração do dano final causado que volta como cura para quem causou;
   - opções: a fração **dobra** quando quem causa está abaixo de uma fração da vida; **matar** um monstro cura uma fração da vida máxima;
   - a cura respeita a regra de cura recebida (`healingTakenMultiplier`) e aparece no log como cura da habilidade que deu o estado;
   - dano refletido ou desviado não gera vida (sem laço).
4. **Área que acompanha quem anda** (Tempestade de Aço; depois o Bastião, se virar aura): uma área presa a uma entidade, que acerta em volta dela a cada intervalo, pela duração, seguindo a posição atual. O lançador pode andar, com a velocidade reduzida pelo estado da habilidade.
5. Validação dos campos novos e schema.

## Critérios de aceite

Testes no Core:

- Execução de teste: alvo com 50% de vida leva o dano normal; com 20%, leva +50%;
- golpe que mata zera a recarga; golpe que não mata não zera;
- estado com 10% de vida pelo dano: 100 de dano curam 10; abaixo do limite de vida curam 20; matar cura a fração da vida máxima; dano refletido não cura;
- área presa ao lançador acerta em volta da posição nova depois que ele anda; acaba com a duração ou com a morte dele;
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
