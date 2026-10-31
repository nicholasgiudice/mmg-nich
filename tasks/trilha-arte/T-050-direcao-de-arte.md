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

**Parcial — aguardando aprovação do designer** (Windows, 2026-10-31). Só documentação; nenhum arquivo de arte baixado.

**Feito**
- `design/art-direction-v0.1.md`: estilo (família LPC, três quartos, 32 px), paleta (Liberated Palette + cores reservadas para gameplay, tiradas do cliente atual), escala (pivô nos pés, raio de colisão × sprite por monstro, só escala inteira), 4 direções, animações mínimas mapeadas para as folhas LPC, regras de legibilidade e ordem de desenho, pacotes, lacunas e organização no repositório.
- Licenças conferidas nas páginas dos 8 pacotes em 2026-10-31 (GitHub do LPC Revised e do gerador Universal LPC; OpenGameArt para os demais; Kenney para o descartado).
- `CREDITS.md`: regra para adicionar créditos e a tabela dos pacotes escolhidos, ainda sem assets incluídos.

**Decisões**
- Terreno e estruturas do **LPC Revised** (OGA-BY 3.0, sem share-alike), incluindo a versão já configurada para o Tiled, que serve à T-051 e à T-055. Personagens do **gerador Universal LPC**, filtrando por licença e evitando GPL. Monstros de pacotes LPC avulsos (lobo, rato, aranha). A Matriarca é a aranha recolorida em escala ×2.
- Em pacotes com várias licenças, adotar a mais permissiva. Nada de criptografia do `.pck` enquanto houver CC-BY/CC-BY-SA.
- Assets em `client/assets/art/<tipo>/`. No Git só entram as imagens usadas, sem LFS. Pacotes completos ficam fora do repositório (`..\mmg-art-src\`).
- A "morte" usa a animação `hurt` do LPC (que é a queda). "Tomando dano" é o piscar branco que já existe.

**Downloads e cena de teste** (2026-10-31, com confirmação do designer)
- Pacotes dos itens 1 (só `palette.json`, `Scale Guide.png` e `Credits.txt`), 2 e 4 a 8 baixados em `..\mmg-art-src\`, fora do repositório (~6 MB).
- Os 6 humanoides foram montados no gerador LPC. As combinações estão em §7 do documento.
- Duas cenas em `..\mmg-art-src\preview\`: personagens sobre o terreno do LPC Revised, e escala dos monstros com o círculo de colisão (inclui a Matriarca recolorida ×2).
- Na cena, a aranha grande foi ampliada ×1,5 só para comparar tamanho. No jogo, a regra de escala inteira (§3) continua valendo.

**Ajustes pedidos pelo designer** (2026-10-31)
- Personagens: Guerreiro musculoso, sem capacete e com cabelo. Cabelo no Arqueiro, Mago e Clérigo. Mago vestido (o *Robe* do gerador só existe para corpo feminino).
- Rato trocado pelo rato gigante do pacote [LPC] bears, deer, lions and more (CC-BY 4.0, baixado com confirmação).
- Lobo recolorido em cinza claro.
- Matriarca: três opções de cor em escala ×2 na cena de teste, a escolher.

**Pendências**
- Aprovação do designer (critério de aceite) e escolha da Matriarca (A, B ou C).
- Completar as entradas de `CREDITS.md` quando cada asset entrar no repositório (T-052/T-053/T-054).
- Lacunas registradas no §7: ícones de habilidades, tema de interface, tela de créditos no jogo (exigida pelas licenças), monstros da fase 3.
