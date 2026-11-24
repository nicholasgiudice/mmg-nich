# T-075 — Site: segundas classes, habilidades e mecânicas da fase 4

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** T-071, T-072, T-401, T-408, T-423, T-433, T-443
- **Libera:** —
- **Áreas que pode alterar:** `src/Game.Site/`, `site/content/site.json`, `tests/Game.Site.Tests/`, `README.md` (se precisar)

## Contexto

O site (`src/Game.Site`, gerado de `data/*.json` por `scripts/build-site.sh`, T-071, e publicado pela T-072) parou nas 4 classes base. A fase 4 criou as **segundas classes**, escolhidas no nível 20, duas por classe base (`second_class.guardian`, `berserker`, `marksman`, `hunter`, `pyromancer`, `arcanist`, `priest` e `inquisitor`, em `data/classes.json`, com habilidades em `data/abilities_<nome>.json`). O `Usage` do site só segue a barra da classe base, então nenhuma delas aparece: as tarefas da fase 4 só conferiram que o gerador rodava (94 páginas) e anotaram as lacunas como pendências (Resultados das T-423, T-432, T-433, T-441, T-442, T-443 e T-444). O designer pediu para atualizar o site com essas informações.

Leia esses Resultados, `design/segunda-classe-v0.1.md` e `design/classes-v0.1.md`.

## Requisitos

1. **Segundas classes no site:** uma página por segunda classe (função, forças e fraquezas, pontos de atributo extras, nível de escolha, as 4 normais com 5 NH e a assinatura com 3 NH), com link a partir da página da classe base e do índice de classes, e na página inicial se couber. As habilidades delas entram no índice de habilidades e ganham a página própria, como as da base. O `Usage` passa a contar as habilidades das segundas classes como em uso, para que não caiam no "fora do site".
2. **Textos em `site/content/site.json`:** resumo, função, forças e fraquezas das 8 segundas classes, tirados do design, sem números. Rótulos das etiquetas novas (pelo menos `sanctuary`, `seal`, `blessing`, `weaken`, `burn`, `arcane_mark`, `arcane_orbs`, `mana_regen` e `void_exposed`; rodar o gerador e ver os avisos para pegar todas).
3. **`Describe.cs`:** descrever em português os campos e efeitos novos, que hoje saem crus ou pelo nome do tipo: `stats`, `undying`, `onExpire`, `attackerLifesteal`, `basicAttack`, `selfHeal`, `allyHeal`, `copy_status`, `hitEffects`, `if_status`, `explodeRadius`, canalização (`Channel`) e `current_target` (`CurrentTargetEffect`). Conferir no Core (`src/Game.Core/Definitions/`, `*Mechanics.cs`) o que cada um faz, para o texto não mentir. Revisar também as habilidades da base que a trilha de habilidades mudou.
4. **Regras:** o que já existe da T-401 (nível 40, pontos extras, NH por nível) continua; conferir se o índice de classes ainda bate com `data/rules.json`.
5. **Testes** (`tests/Game.Site.Tests`): o teste de cobertura cobre toda segunda classe e toda habilidade de segunda classe; um número de habilidade de segunda classe num NH herdado sai igual ao JSON; nenhum campo de efeito conhecido sai cru (nenhum nome de tipo C# como `CurrentTargetEffect` nem chave JSON crua no HTML); links internos sem quebra.
6. Nenhum id aparece no texto do site (regra do commit `4cf929d`).

## Critérios de aceite

- `./scripts/build-site.sh` gera sem erro e sem avisos de texto faltando;
- `./scripts/check.sh` completo passa;
- o site local (`python3 -m http.server -d site/dist`) responde 200 nas páginas novas;
- **não** rodar `build-site.sh --publish`: publicar fica com o humano depois da revisão.

## Resultado

Feito em 2026-11-24, na VPS, branch `task/T-075-site-segundas-classes`. **Não publicado**: falta o humano revisar e rodar `./scripts/build-site.sh --publish`.

**Páginas:** o site passou de **94 para 144 páginas**: 8 páginas de segunda classe, 40 páginas de habilidade de segunda classe e 2 do Golpe Sagrado da Ira Divina (o ataque básico que o status troca, NH1–2 e NH3). `./scripts/build-site.sh` gera sem erro e **sem nenhum aviso de texto faltando** (classe, monstro ou etiqueta). Sobram só os 3 avisos de antes desta tarefa: Armadilha de Contenção, a área dela e `status.slow` ficam fora do site porque nenhuma classe os usa.

**O que foi feito**

- `Usage.cs`:
  - as habilidades das segundas classes contam como usadas, ligadas à segunda classe;
  - o percurso dos efeitos ficou numa fila: um status pode trocar o ataque básico (`basicAttack`, Ira Divina), e a habilidade trocada entra na fila e passa a contar como usada;
  - o percurso agora segue o status de cada NH das passivas (`passiveStatus`), `onExpire`, `hitEffects`, o `byRank` (cada status do NH tem como fonte a habilidade do `byRank` naquele NH), `if_status`, `current_target` e o `onComplete` da canalização;
  - guarda os status de passiva (a página de status mostra "permanente" em vez da duração de 1 s dos dados).
- `Describe.cs`:
  - **dano:** `allyHeal`, `selfHeal`, `behindFraction`, `ignoreArmor` (armadura no físico, resistência nas outras escolas), `lowHealthBonus`, `resetCooldownOnKill`, `movingBonus` e `threatMultiplier`;
  - **outros efeitos:** cura com `maxHealthFraction`, `stacks` no `apply_status`, `copy_status`, `if_status` (com `consume`) e `current_target`;
  - **alvo e canalização:** `explodeRadius` na linha; canalização (pulsos, intervalo, crescimento por pulso, andar canalizando e `onComplete`);
  - **status:** `redirect`, `lifesteal`, `attackerLifesteal`, `stats` (atributos e Mana por segundo, pelos atributos de quem aplicou), `crit` (bônus por distância, multiplicador próprio, crítico garantido), `rangeBonus`, `basicAttack`, `hitEffects`, `onExpire`, `byRank`, `jumpOnExpire`, `exclusive` e as travas `undying` e `knockbackImmune`;
  - **áreas:** `followOwner`, nas áreas e no resumo da área.
  - Cada texto foi conferido com os comentários dos records do Core e com `StatusManager`, `DamageRedirect`, `StatusModule` (o `onExpire` só roda quando o status acaba pelo tempo) e `HunterMechanics`.
  - O efeito sem texto continua saindo pelo nome do tipo de propósito: o teste novo acusa.
- **Rótulos de status com o mesmo nome:** o status do `byRank` sai como "Veneno do Caçador (pelo NH de Flecha Envenenada)". As variantes de um grupo `exclusive` saem pela força: "Queimadura (força 1)" a "(força 5)", em vez da lista de cinco habilidades. As duas áreas da Consagração (uma fere, outra cura) se distinguem por quem afetam: "(inimigos)" e "(aliados)".
- **Páginas:**
  - **Página de segunda classe:** textos, classe base, a outra opção, nível de escolha, +5 pontos de atributo, quantas habilidades normais e de assinatura e quantos NH, passivas, níveis por NH da segunda classe e os cartões das habilidades.
  - **Classe base, índice de classes e página inicial:** a classe base e o índice de classes ganharam os cartões das segundas classes; a página inicial ganhou uma linha com os links das 8 e conta "4 classes e 8 segundas classes".
  - **Índice de habilidades:** um `h3` por segunda classe, debaixo da classe base, com âncora no id da segunda classe e etiquetas "passiva" e "ataque básico".
  - **Página de habilidade:**
    - o nível exigido por NH usa `secondClassRankLevels` e `secondClassSignatureRankLevels`;
    - a passiva mostra o status que mantém em cada NH e o que ele faz, sem custo, recarga nem alvo;
    - a canalizada ganha a coluna "Canalização" e uma nota sobre o que corta a canalização;
    - "Usada por" mostra "(segunda classe)" e, no Golpe Sagrado da Ira Divina, "Inquisidor: ataque básico durante Ira Divina NH1–2".
  - **Busca:** inclui as segundas classes.
- `site/content/site.json`:
  - textos das 8 segundas classes (resumo, função, forças e fraquezas), tirados de `design/segunda-classe-v0.1.md`, sem números;
  - 24 rótulos de etiqueta: `arcane_mark`, `arcane_orbs`, `bastion`, `blessing`, `burn`, `crit`, `divine_wrath`, `fury`, `haste`, `hawk_eye`, `heal`, `hot`, `hunter_venom`, `inspiration`, `lifesteal`, `mana_regen`, `passive`, `plague`, `redirect`, `sanctuary`, `steadfast`, `steel_storm`, `void_exposed` e `weaken`. `seal` já tinha rótulo.
- **Testes** (`tests/Game.Site.Tests`, de 15 para 19):
  - `DataIds` inclui os ids das segundas classes, então os testes de cobertura e de "nenhum id no texto" passam a cobri-las;
  - `Every_second_class_and_its_abilities_are_on_the_site`: página, links da classe base, do índice de classes e da página inicial, âncora no índice de habilidades, página e link de cada habilidade, e nenhum aviso de texto faltando;
  - `Second_class_inherited_rank_cost_and_required_level_match_json`: custo herdado no NH2 e nível exigido pelas listas da segunda classe em `rules.json`, também numa passiva;
  - `Channel_pulses_match_json`: pulsos da canalização;
  - `No_raw_field_or_type_name_in_the_text`: nenhum nome de tipo de `Game.Core.Definitions` e nenhuma chave camelCase ou snake_case dos dados no texto visível. Conferido à mão: tirando o caso `CurrentTargetEffect` do `Describe`, o teste falha com `referencia/status.html: CurrentTargetEffect`.
  - O teste de links internos já existia e continua passando com as páginas novas.
- `README.md`: um item sobre as segundas classes na seção "Site do jogo". `tasks/README.md`: linha da T-075 na trilha Valdran.

**Conferido**

- **Regras:** o índice de classes lê `rules.progression` e `rules.stats` direto dos dados. Comparado com `data/rules.json`: nível máximo 40, NH da base 1/3/5/7/9 e 12/13/15, segunda classe no 20 com +5 pontos, NH 21/24/27/30/33 e 35/37/40, teto de velocidade de ataque 60%. Bate.
- **Habilidades da base:** nenhuma usa campo novo da fase 4 (procurado em `abilities_warrior/archer/mage/cleric.json`). A última mudança nelas (`e9e4464`) só mexe em números, que o site já lê dos dados.
- **Site local:** `python3 -m http.server` responde 200 na página inicial, no índice de classes, nas 8 páginas de segunda classe, no índice de habilidades, em Feixe do Vazio, Flecha Envenenada, Golpe Sagrado da Ira Divina, status, áreas e busca.
- **Revisão do texto:** li o texto gerado das páginas do Inquisidor, Ira Divina, Golpe Sagrado, Flecha Envenenada, Mira Firme, Combustão, Feixe do Vazio, Raio Arcano, Bola de Fogo, Consagração, dos índices e das entradas de status novas.
- `./scripts/check.sh` completo: formatação, build sem avisos e todos os testes fora do Design passam: Core 597, Data 533, Sim 75, Protocol 74, Server 117 + 1 ignorado, Client 131, Persistence 2 + 8 ignorados, Api 39 + 7 ignorados e **Site 19**. O Design deu **301 de 304 em 37 min 39 s**; os 3 que falham **já falham em `main`** (`3891ac4`, conferido numa cópia limpa, com os mesmos números) e não dependem do site:
  - `SecondClassDesignTests.Guardian_cuts_the_damage_on_the_rest_of_the_group_by_at_least_15_percent`, nível 40 (90%) e nível 30 (93%);
  - `CryptDesignTests.A_group_of_four_with_rares_beats_the_watcher_in_the_real_room_80_to_99_percent_in_4_to_6_minutes` (vitória em 71%).

  Esta branch não mexe em `src/Game.Core`, `src/Game.Data`, `data/` nem nos testes de design.

**Não conferido**

- **Visual:** a VPS não tem navegador. Não vi como os cartões novos, a coluna larga da canalização e os `h3` das segundas classes ficam no computador e no celular, só o HTML e o texto. Vale abrir no Windows antes de publicar, principalmente `classes/index.html`, `habilidades/index.html` e `habilidades/arcanist.void_beam.html`.
- Os textos das segundas classes são minha leitura do design. O designer pode querer revê-los.

**Decisões e desvios**

- **Golpe Sagrado da Ira Divina:** não aparece nos cartões da página do Inquisidor, para não repetir "Golpe Sagrado" ao lado das 5 habilidades da barra. Fica no índice de habilidades, debaixo do Inquisidor, com a etiqueta "ataque básico" e "(durante Ira Divina NH…)", e na página própria.
- **Status do `byRank`:** o "Veneno do Caçador" que a Flecha Envenenada aplica não fica em ninguém, mas tem entrada na página de status: ela explica que vai o status do NH e liga para cada um. Sem isso, o efeito da passiva ligaria para um status sem página.
- **Fora das áreas da tarefa:** só `tasks/README.md`, com a linha da tarefa, como o fluxo pede.

**Pendências**

- Os 3 testes de design acima falham em `main`, fora do site: cabe a uma tarefa de balanceamento.

- Publicar com `./scripts/build-site.sh --publish` depois da revisão (humano).
- A tela da segunda classe no cliente (`client/scripts/Hud/AbilityText.cs`) continua sem descrever `if_status` e `current_target` (pendência da T-444). Fica fora desta tarefa: os textos daqui estão no `Game.Site`, não no cliente.
