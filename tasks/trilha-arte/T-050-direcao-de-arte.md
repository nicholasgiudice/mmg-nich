# T-050 — Direção de arte e escolha dos pacotes de assets

- **Ambiente:** Windows (ver e testar os pacotes)
- **Modelo:** Humano + Leve
- **Depende de:** —
- **Libera:** T-052, T-053, T-055
- **Áreas que pode alterar:** `design/`, `CREDITS.md` (novo), `tasks/`

## Contexto

Decisão do designer (2026-10-19): arte vinda de **pacotes de assets**, estilo **simples como o Tibia, mas não idêntico** — visão de cima levemente inclinada, tiles de 32×32 px (a escala que o cliente já usa: 1 unidade = 1 tile = 32 px). Candidato principal: **LPC (Liberated Pixel Cup)** + o *Universal LPC Character Generator* (32×32, equipamento visível no personagem; licenças CC-BY-SA 3.0 / GPL 3, algumas partes CC-BY). Complementos possíveis em CC0 (ex.: Ninja Adventure, Kenney) só se a paleta ficar coerente.

## Requisitos

1. `design/art-direction-v0.1.md`: estilo e referências, paleta, escala (32 px por tile, tamanho do personagem em relação ao círculo de colisão de ~0,4 tile), perspectiva, direções de animação (4), animações mínimas (parado, andando, atacando, conjurando, tomando dano, morrendo), regras de legibilidade (telegráfos e áreas sempre visíveis por cima do cenário, cores de facção).
2. Lista dos pacotes escolhidos com link, autor, **licença conferida na página do pacote**, o que cada um cobre (tiles, personagens, monstros, efeitos, interface) e lacunas (o que nenhum pacote cobre).
3. `CREDITS.md` com a atribuição exigida por cada licença; regra de como adicionar créditos ao incluir novos assets.
4. Decidir onde os assets ficam no repositório (`client/assets/...`) e se arquivos-fonte grandes ficam fora do Git (Git LFS ou pasta externa).
5. Baixar arquivos só com confirmação do designer (nome, origem e tamanho de cada um).

## Critérios de aceite

- direção de arte aprovada pelo designer;
- licenças conferidas e créditos registrados.

## Resultado

_(preencher ao concluir)_
