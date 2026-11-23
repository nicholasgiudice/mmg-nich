# T-441 — Core: mecânicas novas do Piromante

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-432 (copiar estados para outros monstros)
- **Libera:** T-442
- **Áreas que pode alterar:** `src/Game.Core/` (efeitos, estados, mira, áreas, telegráfos), `src/Game.Data/` (carga e validação dos campos novos), `data/schemas/`, `tests/Game.Core.Tests/`, `tests/Game.Data.Tests/`
- **Branch:** sai da `main` com a T-432 integrada. É integrada na `main` (seção 6i de `tasks/README.md`).

## Contexto

Design em `design/segunda-classe-v0.1.md`, seção 7 (Piromante e a tabela "Mecânicas do Mago"). Aprovado pelo designer **como está** em 2026-11-22. Esta tarefa cria **as mecânicas** no Core, cada uma com teste e com dados de teste. As habilidades de verdade vêm na T-443.

O Piromante tem Combustão, Rajada de Chamas, Chão em Chamas, Bola de Fogo e Meteoro (assinatura). Todas aplicam a **Queimadura** (dano de fogo por segundo, 4 s) do NH da habilidade que queimou; uma nova renova a antiga. A Rajada de Chamas e o Chão em Chamas cabem no que existe (arco, área no chão, estado com dano periódico).

O que já existe e pode ser reaproveitado:

- estados com dano periódico e etiquetas; `remove_status` por etiqueta;
- arcos e áreas no chão (Golpe Pesado, Campo de Relâmpagos), mira em linha;
- telegráfo de área com aviso (Cataclismo Arcano);
- copiar estados para monstros perto (T-432, Flecha Dispersa).

## Requisitos

1. **Queimadura por NH:** cada habilidade aplica a versão da Queimadura do próprio NH. Se a Queimadura de um NH mais baixo renovar uma de NH mais alto, decidir e registrar a regra (sugestão: fica a mais forte, com a duração renovada).
2. **Dano só em quem tem um estado, e tirar o estado** (Combustão): um efeito de área em volta de quem lança causa dano só nos monstros que têm um estado com certa etiqueta (`burn`) e, conforme o NH, tira esse estado ou não. No NH5, a Queimadura de cada monstro atingido também passa para os monstros a até 2 tiles (reaproveitar a cópia de estados da T-432).
3. **Explodir no primeiro que acerta** (Bola de Fogo): um disparo em linha para no primeiro monstro que encontra e aplica os efeitos numa área em volta dele (raio 2 a 3). Sem monstro na linha, explode no fim do alcance ou não explode; decidir e registrar.
4. **Área depois do impacto** (Meteoro NH3): depois do aviso e do impacto, deixar uma área no chão (o Chão em Chamas NH1, por 5 s) no mesmo lugar. Se `spawn_area` já roda depois de um telegráfo, só o teste.
5. Validação dos campos novos e schema.

## Critérios de aceite

Testes no Core:

- Combustão de teste com 3 monstros no raio, 2 queimando: só os 2 levam dano; no NH1 a Queimadura acaba neles, no NH3 continua;
- no NH5, a Queimadura de um monstro atingido passa para outro a 2 tiles e não para um a 3;
- Bola de Fogo de teste com 2 monstros em fila: explode no primeiro e acerta os dois se o segundo estiver no raio; um monstro fora do raio não é atingido;
- Meteoro de teste no NH3 deixa a área no chão depois do impacto, e ela queima quem fica nela;
- a regra de Queimadura de NH diferentes vale como registrada;
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
