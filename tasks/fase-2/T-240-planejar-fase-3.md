# T-240 — Detalhar a fase 3 em tarefas

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-230
- **Libera:** —
- **Áreas que pode alterar:** `tasks/`, `tech/` (somente se uma decisão técnica mudar)

## Contexto

Converter o esboço da fase 3 (`tasks/README.md`, seção 7: Floresta, Ruínas, Cripta instanciada, Vigia Sem Rosto, NPCs e missões) em tarefas, aproveitando o que a fase 2 e a trilha de arte já entregaram.

## Requisitos

1. Ler `tasks/reports/`, os Resultados da fase 2 e o código atual.
2. Criar `tasks/fase-3/T-3XX-*.md` com ambiente, modelo, dependências, áreas, requisitos e critérios de aceite verificáveis.
3. Atualizar `tasks/README.md`: índice da fase 3, ondas e grafo.

## Critérios de aceite

- toda tarefa da fase 3 pode ser entregue a um agente sem contexto além de `AGENTS.md` e o próprio arquivo;
- dependências sem ciclos.

## Resultado

Concluída em 2026-11-04 (feita no Windows, só documentos). **21 tarefas** em `tasks/fase-3/`, e índice, ondas, cuidados de integração e grafo na seção 6e de `tasks/README.md`.

**Branch criada a partir da T-230**, que ainda não estava na `main` (`task/T-230-playtest-fase-2` foi mesclada nesta branch). Integre a T-230 antes desta.

**Ordem da fase:** primeiro as correções do playtest v0.2, depois os sistemas pedidos pelo designer, por último o conteúdo do primeiro mapa.
- **Correções:**
  - T-301: três bugs, com a causa já localizada no código;
    - recargas guardadas em tick absoluto e perdidas ao recriar a entidade;
    - invocações sem ponto de surgimento nunca removidas;
    - `GroupId` nunca definido no `OpenZone`, então a zona inteira é puxada junta;
  - T-302: ritmo de XP e taxa de queda.
- **Sistemas:**
  - grupo: T-310, T-311;
  - lista de batalha: T-312;
  - ouro, consumíveis como itens, NPCs e lojas: T-320 a T-323;
  - zonas lidas do Tiled: T-331;
  - mapas maiores: T-330.
- **Conteúdo:**
  - monstros novos: T-340, T-341;
  - Floresta e Ruínas: T-342, T-343;
  - instâncias de dungeon: T-350;
  - Vigia Sem Rosto: T-351;
  - Cripta: T-352;
  - cliente dos monstros novos: T-345.
- **Fechamento:** playtest com 4 pessoas (T-380) e planejamento da fase 4 (T-390).

**Decisões**
- **Economia mínima autorizada explicitamente** nas tarefas T-320 a T-323: ouro dos monstros e loja com preço fixo. Sem troca entre jogadores, mercado, crafting ou armazém. O `AGENTS.md` deixa a economia fora de escopo salvo pedido explícito; o pedido é do designer, no playtest v0.2.
- **A poção deixa de ser habilidade** e vira item empilhável comprado no Templo, com recarga por tipo (T-320).
- **Chave da Cripta adiada:** a entrada fica aberta, com aviso. Os fragmentos de chave dependem de missões, que ficam fora desta fase.
- **A Matriarca volta para o Ninho, dentro da Floresta** (T-342). Hoje o Covil fica ligado aos Campos.
- **Portões, surgimentos e NPCs passam para o mapa do Tiled** (T-331), antes dos mapas grandes, para o designer editar posições no editor e não no JSON.
- **O nível máximo continua 15**, como o designer pediu.
- **O grupo não é gravado**: ele vive no servidor e se desfaz ao desconectar.

**Pendências**
- T-216 (destino do backup) continua aberta, fora das ondas.
- Os monstros novos e o Vigia não tinham design: a T-340, a T-341 e a T-351 começam escrevendo a seção de design, com propostas que o designer pode trocar.
