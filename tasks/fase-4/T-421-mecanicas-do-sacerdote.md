# T-421 — Core: mecânicas novas do Sacerdote

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** —
- **Libera:** T-422
- **Áreas que pode alterar:** `src/Game.Core/` (efeitos, estados, estatísticas, recursos, dano), `src/Game.Data/` (carga e validação dos campos novos), `data/schemas/`, `tests/Game.Core.Tests/`, `tests/Game.Data.Tests/`
- **Branch:** sai da `main` e é integrada na `main` (seção 6g de `tasks/README.md`).

## Contexto

Design em `design/segunda-classe-v0.1.md`, seção 8 (Sacerdote e a tabela "Mecânicas do Clérigo"). Aprovado pelo designer em 2026-11-22, com a **Inspiração** no lugar do Elo da Vida. Esta tarefa cria **as mecânicas** no Core, cada uma com teste e com dados de teste. As habilidades de verdade vêm na T-423.

O Sacerdote tem Oração Restauradora, Bênção, Graça Veloz, Inspiração e Santuário (assinatura). A Graça Veloz já cabe no que existe (velocidade, tirar estados, imunidade por etiqueta).

O que já existe e pode ser reaproveitado:

- estados com `modifiers` fixos, `flags` e efeitos periódicos (`periodic`), aplicados por `StatusManager` e rodados em `StatusModule` com a fonte do estado (`src/Game.Core/Combat/Status/`);
- cura (`heal`, com `scaling` pelas estatísticas da fonte), estados em aliados num raio (Muralha Viva, Bastião);
- estatísticas do personagem em `CharacterStats` (`src/Game.Core/Combat/Stats/`), hoje somadas da classe, dos pontos e do equipamento (`ItemMath`);
- regeneração de Mana em `ResourceSystem` (5/s + INT × 0,1 + afixo `ManaRegen` do equipamento);
- dano em `DamageResolver.Deal`, com desvio (T-406) e vida pelo dano (T-407).

## Requisitos

1. **Cura por segundo num estado** (Oração Restauradora): um estado com `periodic` de `heal` que escala pela INT **de quem lançou**. Provavelmente já funciona: confirmar com teste (cura pela INT da fonte, `healingTakenMultiplier` do alvo, evento `HealApplied` e log atribuídos à habilidade). Se não funcionar, o ajuste.
2. **Estatísticas vindas de um estado, pela INT de quem lançou** (Bênção e Inspiração):
   - um estado pode somar **atributos** (FOR, DES, AGI, INT, VIT, um a um ou todos) e **regeneração de Mana** a quem o tem;
   - o valor é `base + escala × estatística de quem lançou`, calculado **ao aplicar** e fixo enquanto o estado dura (renovar recalcula). Guardar o valor no estado, não reler a fonte a cada tick;
   - os atributos entram em `CharacterStats` como os do equipamento, e tudo que deriva deles muda junto (vida máxima pela VIT, Mana máxima pela INT, dano, crítico, velocidade pela AGI com os tetos atuais);
   - ao acabar, a vida e a Mana atuais não passam da nova máxima;
   - a regeneração a mais soma na de `ResourceSystem`. Isso não fere a regra "Mana vem só do tempo": é regeneração, não Mana na hora.
3. **Deixar quem lançou de fora** (Inspiração): o efeito que aplica estado nos aliados do raio pode excluir o próprio lançador. Se já existe uma opção, usá-la; se não, criar.
4. **Vida mínima de 1** (Santuário): flag de estado. Um dano que mataria quem tem o estado o deixa com 1 de vida. Vale para todo dano, inclusive periódico e a parte desviada para um protetor (T-406). Não muda cura, escudo nem os eventos de dano (o número mostrado é o dano aplicado de verdade).
5. **Efeito ao acabar o estado** (Santuário NH3): lista de efeitos que roda quando o estado **acaba pelo tempo** (não quando é tirado ou quando quem o tem morre), com a fonte do estado.
6. **Cura pela vida máxima do alvo** (Santuário NH3: 20% da vida máxima): a cura aceita um valor em fração da vida máxima de quem recebe, somado ao `base` e ao `scaling`.
7. Validação dos campos novos e schema.

Se algum campo novo for para o snapshot (por exemplo, para o cliente mostrar os atributos com a Bênção), subir `ProtocolVersion` a partir do número que estiver na `main` e registrar no Resultado. Preferir não mandar nada novo pela rede: vida e Mana máximas já vão no snapshot.

## Critérios de aceite

Testes no Core:

- estado de cura por segundo com escala de INT cura pela INT de quem lançou, não pela do alvo; cura recebida pela metade cura metade;
- estado de atributos com `2 + 0,08 × INT` e lançador com INT 62 dá +7 em cada atributo (6,96; a regra de arredondamento fica registrada no Resultado); a vida máxima do aliado sobe com a VIT e volta ao acabar, sem a vida atual passar da máxima;
- o valor não muda se a INT do lançador mudar com o estado ativo; renovar recalcula;
- estado de regeneração de Mana com +4/s: o aliado recupera 4 de Mana a mais por segundo, e para ao acabar;
- Inspiração de teste num grupo de 3: os outros dois ganham o estado, o lançador não;
- com a vida mínima, um golpe de 500 num alvo com 100 de vida deixa 1; sem o estado, mata; vale para dano periódico e para a parte desviada;
- efeito ao acabar roda quando o tempo acaba e não roda quando o estado é tirado (`remove_status`) ou quando o alvo morre;
- cura de 20% da vida máxima cura 20% de quem recebe;
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
