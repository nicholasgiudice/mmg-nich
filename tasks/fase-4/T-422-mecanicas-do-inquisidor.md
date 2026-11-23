# T-422 — Core: mecânicas novas do Inquisidor

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-421
- **Libera:** T-423
- **Áreas que pode alterar:** `src/Game.Core/` (efeitos, estados, áreas, dano, cura, ataque básico), `src/Game.Data/` (carga e validação dos campos novos), `data/schemas/`, `tests/Game.Core.Tests/`, `tests/Game.Data.Tests/`
- **Branch:** sai da `main` com a T-421 integrada (ou da branch da T-421, se ela ainda não tiver entrado; registrar a base no Resultado). É integrada na `main` (seção 6g de `tasks/README.md`).

## Contexto

Design em `design/segunda-classe-v0.1.md`, seção 8 (Inquisidor e a tabela "Mecânicas do Clérigo"). Aprovado pelo designer **como está** em 2026-11-22. Esta tarefa cria **as mecânicas** no Core, cada uma com teste e com dados de teste. As habilidades de verdade vêm na T-423.

O Inquisidor tem Martelo da Justiça, Consagração, Selo da Luz, Julgamento e Ira Divina (assinatura). O Martelo e o Julgamento (até o NH4) cabem no que existe: dano sagrado, `bonusVsTags`, arco, atordoamento.

O que já existe e pode ser reaproveitado:

- áreas no chão (`spawn_area`, `data/*`, `src/Game.Core/Combat/Areas/`), cada uma com **um** filtro `affects` (inimigos ou aliados);
- vida pelo dano no **atacante** (`lifesteal` no estado, `LifestealReaction`, T-407), com as regras de dano indireto, cura recebida e evento `HealApplied`;
- alvos extras em volta do alvo (`extra_targets`), corrente (`chain`);
- o ataque básico de cada classe em `classes.json` (`basicAttack`; do Clérigo, `cleric.holy_strike`, alcance 6).

## Requisitos

1. **Área que fere e cura** (Consagração): no mesmo lugar e pela mesma duração, um pulso por segundo fere os monstros e cura os aliados. Se duas áreas lançadas juntas (uma com `affects: enemy`, outra com `affects: ally`) resolverem, só o teste; se não, o ajuste mínimo (por exemplo, filtro de facção por efeito dentro da área).
2. **Vida pelo dano para quem bate no alvo marcado** (Selo da Luz): um estado no **monstro** faz todo aliado de quem lançou que causar dano nele se curar de uma fração do dano. Reaproveitar as regras da vida pelo dano da T-407 (dano final, sem dano indireto, cura recebida, sem publicar cura sem efeito). A cura sai em nome de quem bateu, atribuída à habilidade do selo.
3. **Cura pelo dano do próprio golpe** (Julgamento NH5: 20% do dano): um efeito de dano pode curar quem causou numa fração do dano final.
4. **Ataque básico trocado por um estado** (Ira Divina): enquanto um estado está ativo, o ataque básico de quem o tem vira outra habilidade, dada pelo estado. O cliente continua mandando a intenção de ataque básico; quem troca é o Core. Sugestão: o estado aponta para uma habilidade nos dados (`inquisitor.divine_wrath_strike`) com o Golpe Sagrado mais forte e alvos extras num raio de 2 em volta do alvo.
5. **Cura dos aliados perto pelo dano** (Ira Divina): um efeito de dano pode curar os aliados a até N tiles de quem causou numa fração do dano final de cada acerto.
6. Validação dos campos novos e schema.

Se o cliente precisar saber da troca do ataque básico (ícone ou alcance), subir `ProtocolVersion` a partir do número que estiver na `main` e registrar no Resultado. Preferir não mandar nada novo: a troca pode ficar só no servidor.

## Critérios de aceite

Testes no Core:

- Consagração de teste: no mesmo pulso, o monstro no raio leva dano e o aliado no raio é curado; o monstro não é curado e o aliado não leva dano;
- com o Selo de 10% num monstro, dois aliados que causam 100 cada se curam 10 cada; quem não é aliado do lançador não se cura; dano refletido não cura;
- Julgamento de teste com 20% e 200 de dano cura 40 quem bateu;
- com o estado da Ira Divina, o ataque básico acerta todos no raio de 2 em volta do alvo com o dano maior; quando o estado acaba, volta ao Golpe Sagrado normal, com a recarga normal;
- cada acerto da Ira Divina de teste cura os aliados a até 4 tiles em 10% do dano e não cura quem está mais longe;
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
