# T-055 — Mapas: Cidade-Fortaleza e Campos da Vigília

- **Ambiente:** Windows (Tiled)
- **Modelo:** Humano + Médio
- **Depende de:** T-051, T-052, T-053
- **Libera:** fase 2 ("Cidade + Campos online") e fase 3
- **Áreas que pode alterar:** `data/maps/`, `data/` (spawns e encontros dos mapas), `design/`

## Contexto

`design/first-map-v0.1.md` descreve a Cidade-Fortaleza (hub seguro, portões) e os Campos da Vigília (Estrada Leste, Fazenda Abandonada, Clareira dos Caçadores, Acampamento dos Salteadores). A fase 2 precisa dos dois jogáveis online; Floresta, Ruínas e Cripta ficam para a fase 3.

## Requisitos

1. Cidade-Fortaleza no Tiled: ruas, praça, pontos de NPC (marcadores, sem lógica ainda), portões com transição de zona.
2. Campos da Vigília: as quatro subáreas com spawns dos monstros dos níveis 1–4 (`design/progression-v0.1.md` §2) e o Acampamento como encontro de Salteadores.
3. Colisão, spawns e áreas seguindo as convenções da T-051; validação do carregador sem erros.
4. Teste no simulador: um personagem nível 1–4 percorre os Campos (bots) sem ficar preso em colisão.

## Critérios de aceite

- os dois mapas carregam no Core e aparecem no cliente com a arte;
- percurso dos bots sem travar;
- aprovado pelo designer (playtest de exploração).

## Resultado

_(preencher ao concluir)_
