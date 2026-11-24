# T-505 — Simulador: XP por hora de cada área e metas do 16 ao 27

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** —
- **Libera:** T-530, T-532, T-533
- **Áreas que pode alterar:** `src/Game.Sim/`, `tests/Game.Design.Tests/`, `tests/Game.Sim.Tests/`, `tasks/reports/`

## Contexto

`design/segunda-classe-v0.1.md` §3: "o que decide se fica bom é a XP por minuto, não a XP por monstro", e "a meta a definir é a XP por hora de cada área". Hoje os testes de design das zonas (`ForestZoneDesignTests`, `RuinsZoneDesignTests`) medem o ritmo de uma faixa de níveis com um modelo de ritmo, sem meta.

O designer decidiu **manter a curva de XP** (`design/mapas-16-40-v0.1.md` §8, decisão 3). Com ela, do 15 ao 20 são ~5 h e do 20 ao 25 ~8 h (§1 do esboço). As áreas novas precisam render perto disso, e a masmorra mais que o campo, porque dá mais trabalho e pede grupo.

## Requisitos

1. Uma medida só de **XP por hora** de uma zona, para um perfil (classe, nível, segunda classe, equipamento esperado) e um tamanho de grupo, contando:
   - o tempo de luta de cada grupo de monstros (simulador);
   - o deslocamento entre os grupos (distância no mapa a pé), o descanso e o renascimento dos monstros, como o modelo de ritmo de hoje;
   - a divisão de XP do grupo (T-310).
2. Medir as zonas de hoje (Campos, Floresta, Ruínas, Cripta) e registrar no relatório, como referência do que o designer já jogou e aprovou.
3. **Metas** para as zonas novas, em testes de design que os mapas (T-530, T-532, T-533) vão usar:
   - **campo:** com o perfil esperado solo, subir a faixa da zona leva entre 75% e 130% do tempo da curva (Brejo 16→20: ~5 h; Serra 21→25: ~7 h);
   - **masmorra:** XP por hora, por pessoa, num grupo de 4, pelo menos **1,3 ×** a do campo da mesma faixa;
   - **chefe de campo:** só a medida, sem meta.
   O designer pode trocar os números. Registre o que for decidido.
4. A medida aceita uma zona qualquer pelo id, para a fase 6 reaproveitar.

## Critérios de aceite

- relatório `tasks/reports/balance-v0.23.md` (ou o próximo número livre) com a XP por hora das zonas de hoje e as metas;
- testes de design das metas prontos para receber as zonas novas (podem ficar marcados como pendentes até os mapas existirem, como o `PlaytestRebalance.Pending`);
- `./scripts/check.sh` completo passa.

## Resultado

_(a preencher)_
