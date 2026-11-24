# T-500 — Detalhar a fase 5 (região do sul, 16 ao 27) em tarefas

- **Ambiente:** Windows
- **Modelo:** **Forte**
- **Depende de:** esboço `design/mapas-16-40-v0.1.md` aprovado
- **Libera:** T-501 a T-550
- **Áreas que pode alterar:** `tasks/`, `design/mapas-16-40-v0.1.md`

## Contexto

Com as oito segundas classes prontas (fase 4), o designer pediu mapas novos (2026-11-23). O mundo de hoje termina no 15, a segunda classe entra no 20 e o nível máximo é 40. O esboço `design/mapas-16-40-v0.1.md` propõe uma região ao sul da Cidade, em faixas de nível, com um segundo ponto seguro. O designer aprovou o esboço e pediu para detalhar.

## Requisitos

1. Registrar as decisões do designer no esboço (§8).
2. Criar `tasks/fase-5/T-5XX-*.md` para o 16–27 (Brejo, Forte do Passo, Serra, Minas Fundas): peças de sistema, monstros, dados, arte, mapas, cliente e playtest, com ambiente, modelo, dependências, áreas e critérios verificáveis.
3. Atualizar `tasks/README.md`: estado das fases, índice, ondas, cuidados de integração, grafo e o esboço da fase 6.

## Critérios de aceite

- tarefas criadas e `tasks/README.md` atualizado;
- decisões registradas no esboço.

## Resultado

Feito em 2026-11-23, no Windows, na branch `task/T-500-planejar-fase-5`.

**Decisões** (`design/mapas-16-40-v0.1.md` §8). O designer aprovou o esboço sem mudanças. Onde o esboço deixava a escolha aberta, a T-500 ficou com a opção mais simples:
- **curva de XP mantida**: a T-505 mede e o playtest decide;
- **mestre das classes e tela de habilidades**: as duas formas valem;
- **renascimento por zona**, não por nível: o esboço dizia "do 21 em diante", e a regra por zona dá o mesmo efeito, com menos casos.

**Tarefas** (19, de T-500 a T-560; os números deixam espaço por grupo):
- **sistema (VPS):** T-501 renascer no ponto seguro da zona; T-502 terreno que deixa lento; T-503 anúncio de chefe de campo (sobe o protocolo); T-505 XP por hora de cada área e metas;
- **monstros e dados (VPS):** T-510 Brejo e a Bruxa; T-511 Serra; T-512 o Capataz Forjado e as Minas; T-513 itens, loja do Forte, poções e quedas;
- **arte (Windows):** T-520 tilesets no PixelLab (3 opções por bioma, amostra no jogo); T-521 sprites LPC (ids fixados antes, para rodar em paralelo aos dados);
- **mapas (Windows):** T-530 Brejo e o portão sul da Cidade; T-531 Forte do Passo; T-532 Serra Partida; T-533 Minas Fundas;
- **cliente (Windows):** T-540 monstros, água funda e aviso de chefe; T-541 mestre das classes e tela de morte com o destino;
- **fechamento:** T-550 playtest (junto com os playtests de classe ainda abertos); T-560 detalhar a fase 6.

**Fases:** a região do sul passa a ser as fases 5 (16–27) e 6 (26–40). A Operação vai para a fase 7, como já tinha acontecido quando a segunda classe entrou na frente dela.

**O que ficou de fora:** missões, chave da masmorra, portas que abrem, IA com rota (vai para a fase 6, com o Chefe de Guerra) e a mudança da curva de XP.

**Verificação:** a tarefa só mexe em `.md`.
- O `dotnet format` não roda nesta máquina: o processo auxiliar do MSBuild cai ao abrir a solução ("The server disconnected unexpectedly"), antes de olhar qualquer arquivo.
- `dotnet build -warnaserror` passou, sem avisos.
- `dotnet test` passou em todos os projetos menos o `Game.Design.Tests`, que deu **4 falhas em 304** (21 min). Elas já existiam antes desta tarefa: o build testado era o código da `main` em `2cd506b`, sem nenhuma mudança da T-500. Falharam:
  - `WatcherDesignTests.Without_the_cleric_the_win_rate_drops_clearly`: sem o Clérigo, o grupo ainda vence 99%;
  - `CryptDesignTests.A_group_of_four_with_rares_beats_the_watcher_in_the_real_room_80_to_99_percent_in_4_to_6_minutes`: 71%, a meta é de 80 a 99%;
  - `SecondClassDesignTests.Guardian_cuts_the_damage_on_the_rest_of_the_group_by_at_least_15_percent`, níveis 30 e 40: o Guardião corta 10% e 6%.
  As duas primeiras provavelmente vêm das mudanças recentes no Vigia (`e3f039e`, chefe persegue o alvo; `6799131`, chuva de 3 a 5 círculos). Precisam de uma correção à parte.
