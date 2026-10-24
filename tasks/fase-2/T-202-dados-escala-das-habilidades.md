# T-202 — Dados: escala de todas as habilidades e NH

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** T-201
- **Libera:** T-206
- **Áreas que pode alterar:** `data/abilities_*.json`, `data/classes.json`, `tests/Game.Data.Tests/`, `design/skills-v0.1.md` (só se encontrar erro na tabela)

## Contexto

`design/skills-v0.1.md` dá cada NH como fórmula (ex.: Golpe Pesado NH1 = `dano 15 + 1 × ARMA + 2,5 × FOR`). Hoje os dados guardam só o resultado no perfil nível 1 (51). Com a T-201 os efeitos aceitam `scaling`.

## Requisitos

1. Reescrever **todas** as habilidades das quatro classes (ataques básicos, as 4 normais e a assinatura de cada uma, em todos os NH) como `base` + `scaling`, copiando as fórmulas da tabela. Habilidades de monstros, áreas e status ficam fixas.
2. A Marca do Caçador escala com DES (bônus `16% + 0,75% × DES`, teto 60%/65%): se o formato de status não permitir escala, registrar a decisão (ex.: status por faixa de DES) no Resultado.
3. Atualizar o teste de conteúdo da T-062 (`SkillRankTableTests`) para calcular o "perfil nível 1" com o perfil inicial de cada classe e a arma inicial, e conferir contra as tabelas do documento.

## Critérios de aceite

- valores no perfil nível 1 idênticos aos de hoje (teste de conteúdo e testes de design passam sem mudar);
- nenhuma habilidade de classe com dano/cura/escudo fixo;
- `./scripts/check.sh` completo passa.

## Resultado

**Feito** (VPS, 2026-10-24). `./scripts/check.sh` completo passa, e os testes de design passam sem nenhuma mudança neles.

**Dados** (`data/abilities_{warrior,archer,mage,cleric}.json`)
- Todo dano, cura e escudo das habilidades de classe virou `base` + `scaling`, copiado das tabelas de `design/skills-v0.1.md`. Isso vale para os 4 ataques básicos, as 4 normais e a assinatura de cada classe, em todos os NH, inclusive saltos da Corrente e alvos extras (Barreira NH5, Cura Direta NH5, Escudo Sagrado NH5). Custo, recarga, alcance e os demais campos não mudaram.
- No perfil nível 1, os números são os de hoje, a menos de arredondamento do documento: o dano não é arredondado no jogo (Golpe Pesado NH2 dá 61,2, contra 61 na tabela). Cura e escudo são arredondados no jogo e dão exatamente os números da tabela.

**Decisões**
- **Disparo (ataque básico do Arqueiro):** o documento dizia `1 × ARMA + 1 × DES = 18`, mas a calibração (T-031, `balance-v0.3`) e os dados usam 17. Fiquei com `1 × ARMA + 0,92 × DES` (17,04 no nível 1) e corrigi a linha em `design/skills-v0.1.md`. Uma base −1 seria mais limpa, mas o validador não aceita base negativa. No nível 15, isso dá cerca de 75 em vez dos 80 da tabela de `progression-v0.1.md` §8, que não alterei.
- **Marca do Caçador:** o status não aceita escala, e o modificador `damageTakenMultiplier` é fixo. Os status continuam com o valor do perfil nível 1 (DES 12): +25% no NH1–3 e +30% no NH4–5. Para escalar com DES de verdade, seria preciso escalar o modificador pela fonte no Core, ou ter status por faixa de DES em `statuses.json`. As duas opções ficam fora das áreas desta tarefa.
- **Cura própria da Corrente Sagrada:** fica fixa (10/12/14/16/36), porque a tabela não dá fórmula para ela. É a única exceção ao "nenhum valor fixo", e o teste a lista explicitamente.
- Áreas (pulsos do Campo de Relâmpagos, chão em chamas do Cataclismo, lentidão da Chuva), status e habilidades de monstros ficaram fixos, como pede a tarefa.

**Testes** (`tests/Game.Data.Tests/SkillRankTableTests.cs`)
- O "perfil nível 1" agora é calculado com os atributos iniciais de cada classe e o equipamento inicial (`ItemMath`, T-204) e conferido contra as tabelas do documento, NH a NH.
- Novo: os ataques básicos dão 18 / 17 / 15 / 12.
- Novo: nenhuma habilidade de classe tem dano, cura ou escudo sem `scaling`, em nenhum NH, inclusive efeitos aninhados.

**Pendências**
- Marca do Caçador escalando com DES (ver acima): precisa do Core.
- Rever o valor do Disparo no nível 15 em `progression-v0.1.md` §8, se a fórmula 0,92 × DES ficar.
