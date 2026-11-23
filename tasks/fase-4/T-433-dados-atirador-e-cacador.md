# T-433 — Dados: habilidades do Atirador de Elite e do Caçador

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** T-431, T-432
- **Libera:** T-434
- **Áreas que pode alterar:** `data/` (habilidades, estados, áreas, `classes.json`), `tests/Game.Sim/` (só a estratégia do bot para as habilidades novas), `tests/Game.Design.Tests/`, `tests/Game.Data.Tests/`, `design/segunda-classe-v0.1.md` (só números ajustados pelo simulador), `design/progression-v0.1.md` (§8, metas do Arqueiro), `design/skills-v0.1.md` (seção nova do Arqueiro), `tasks/reports/`
- **Branch:** sai da `main` com a T-431 e a T-432 integradas. É integrada na `main` (seção 6h de `tasks/README.md`).

## Contexto

As 10 habilidades estão em `design/segunda-classe-v0.1.md`, seção 6, com custo, recarga e todos os NH. Aprovadas pelo designer em 2026-11-22:

- **Atirador de Elite:** Flecha Perfurante, Tiro Debilitante, Mira Firme (passiva), Tiro Certeiro (**sem conjuração**), Olho de Falcão (assinatura, **15/17/20 s**).
- **Caçador:** Flecha Envenenada (passiva), Ferida Aberta, Nuvem Tóxica, Flecha Dispersa, Praga (assinatura).

As mecânicas novas são da T-431 (dano menor atrás na linha, crítico pela distância, alcance de um estado, ignorar armadura, crítico garantido) e da T-432 (efeito em todo acerto, veneno pelo NH da passiva, dano maior se o alvo andar, copiar estados, estado que pula). O Guerreiro serve de exemplo: `data/abilities_guardian.json`, `tests/Game.Data.Tests/WarriorSecondClassContentTests.cs` e `tests/Game.Design.Tests/SecondClassDesignTests.cs` (T-408, `tasks/reports/balance-v0.19.md`).

**Atenção aos números que o designer mudou:** o dano do Tiro Certeiro foi feito para um tiro que pedia 1,5 s parado, e o Olho de Falcão passou de 6–8 s para 15–20 s. Os dois juntos são a rajada do Atirador. Se passarem das metas, ajustar primeiro o dano do Tiro Certeiro ou o custo, e não a duração que o designer pediu. Registrar o antes e o depois.

## Requisitos

1. Arquivos de dados das habilidades (sugestão: `data/abilities_marksman.json` e `data/abilities_hunter.json`), com todos os NH, e os estados e áreas que elas usam, no mesmo arquivo da classe. Ids no padrão `marksman.piercing_arrow`, `hunter.poison_arrow`; textos em português.
2. `data/classes.json`: as duas segundas classes do Arqueiro (`second_class.marksman`, `second_class.hunter`) com as suas 5 habilidades, na ordem da tabela do design.
3. **Bot do simulador:** a estratégia do Arqueiro usa as habilidades novas de um jeito razoável (manter distância de 6+ tiles com o Atirador; Tiro Certeiro e Olho de Falcão juntos; Nuvem Tóxica e Flecha Dispersa no Caçador quando há 2 ou mais monstros).
4. **Metas novas** em `design/progression-v0.1.md`, §8 ("Metas da segunda classe"). Se o simulador mostrar que uma delas não faz sentido, ajustar a meta e registrar o motivo no relatório:
   - **Atirador × Arqueiro só com a base**, solo, contra um alvo único forte (elite ou chefe do nível): vence ≥ 85% e em no máximo **85%** do tempo do Arqueiro só com a base; no grupo típico pode ser mais lento;
   - **Caçador × Arqueiro só com a base**, solo, grupo típico (3 monstros normais do nível): vence ≥ 85% e em no máximo **85%** do tempo; contra alvo único pode ser mais lento;
   - **Olho de Falcão:** o dano do Atirador nos 20 s dela não passa de **2,5×** o dano dele nos mesmos 20 s sem ela (meta proposta; ajustar se o simulador mostrar outra coisa);
   - as metas gerais da segunda classe (nível 21, distribuição de pontos, diferença entre segundas classes, com as do Guerreiro e as do Clérigo que já estiverem na `main`).
5. **Testes de design** com o simulador nos níveis 21, 25, 30 e 40. Se uma meta falhar, ajustar os números dentro do espírito do design e registrar o antes e o depois em `design/segunda-classe-v0.1.md` ("Ajustes do simulador", como na seção 5) e no relatório.
6. Relatório `tasks/reports/balance-v0.21.md` (ou o próximo número livre) com o resultado dos testes e os ajustes.
7. `design/skills-v0.1.md`: seção do Arqueiro com link e tabela de custos e recargas, como a do Guerreiro.
8. Conferir que o site (`src/Game.Site`, `scripts/build-site.sh`) gera as páginas das habilidades novas sem erro.

## Critérios de aceite

- os dados carregam e validam;
- cada habilidade tem um teste de efeito com os dados reais no NH1 e no NH5 (assinaturas no NH3). Por exemplo: a Flecha Perfurante dá 60% e 80% atrás; a Mira Firme dá +6% e +12% de crítico a 6+ tiles; o Tiro Certeiro sai sem conjuração; o Olho de Falcão dura 15 s e 20 s; a Flecha Envenenada chega a 3 e a 5 pilhas;
- todo custo cabe na Mana do Arqueiro no nível 40;
- testes de design das metas passam;
- `./scripts/check.sh` completo passa.

## Resultado

Feito em 2026-11-23, **no Windows** (a tarefa não usa tela), a pedido do integrador. Relatório: `tasks/reports/balance-v0.21.md`.

### Base

Branch `task/T-433-dados-atirador-e-cacador`, criada de `9d3a372` (`main` local com T-421, T-422, T-431, T-432 e T-442) e **rebaseada em `e471604`** (`main` com a T-441 e a T-423) antes do `check.sh` completo, a pedido do integrador. Conflito só na lista de opções de `src/Game.Bots/BotOptions.cs` (a T-423 também acrescentou opções); a opção desta tarefa que se chamava `buffRange` virou `selfBuffRange` para não misturar com o `buffRange` dos reforços de grupo do Clérigo.

### O que foi feito

- **Dados:** `data/abilities_marksman.json` e `data/abilities_hunter.json`, com as 10 habilidades e todos os NH, e os estados (Debilitado, Mira Firme por NH, Olho de Falcão, Flecha Envenenada, o apelido `status.hunter_venom` e o Veneno do Caçador por NH, Ferida Aberta por NH, lentidão da nuvem, Praga) e as áreas da Nuvem Tóxica no arquivo da classe. Ids: `marksman.piercing_arrow`, `marksman.crippling_shot`, `marksman.steady_aim` (passiva), `marksman.true_shot`, `marksman.hawk_eye`, `hunter.poison_arrow` (passiva), `hunter.open_wound`, `hunter.toxic_cloud`, `hunter.scatter_arrow`, `hunter.plague`. Os campos novos da T-431 e da T-432 estão em uso (`behindFraction`, `ignoreArmor`, `crit`, `rangeBonus`, `hitEffects`, `byRank`, `movingBonus`, `copy_status`, `apply_status.stacks`, `jumpOnExpire`).
- `data/classes.json`: as cinco de cada segunda classe do Arqueiro, na ordem do design (só a entrada do Arqueiro mudou).
- **Testes de efeito** (`tests/Game.Data.Tests/ArcherSecondClassContentTests.cs`, 24 casos, dados reais): ordem das classes, Mana do 40 para todo custo, personagem do 40 com todos os NH; e cada habilidade no NH1 e no NH5 (assinaturas no NH3): Flecha Perfurante 60%/80% atrás, linha de 10/14 e recarga 8/6 s; Tiro Debilitante (lentidão, −15%/−25% de dano, 4/6 s); Mira Firme +6%/+12% de crítico só a 6+ tiles, +1 de alcance e × 1,65 no NH5; Tiro Certeiro sem conjuração (dano no mesmo tick), 30%/50% de armadura ignorada, recarga 10/8 s; Olho de Falcão 15/20 s, todo disparo crítico, +2 de alcance no NH3, custo 70; Flecha Envenenada 3/5 pilhas, 4/6 s, o veneno do NH; Ferida Aberta 6/10,8 por segundo, ×1,5/×2 com o alvo andando; Nuvem Tóxica (raio 3/4, 6/8 s, dano por segundo, pilhas a cada 2 s, lentidão no NH5); Flecha Dispersa (copia veneno e Ferida Aberta para os 2/4 mais perto a até 4 tiles); Praga (3/5 pilhas no raio, 8/10 s, o veneno pula ao acabar).
- **Testes de design:** as duas segundas classes entraram em `SecondClassDesignTests.Specs` (nível 21, pontos na base ou na segunda classe e diferença entre segundas classes, genéricos da T-423); as metas próprias do Arqueiro estão em `tests/Game.Design.Tests/ArcherSecondClassDesignTests.cs` (elite, grupo típico, Olho de Falcão), nos níveis 25, 30 e 40 (Olho de Falcão no 37 e no 40). Passam todas (relatório, §2).
- **Bot do simulador** (`src/Game.Bots/ConfigurableBot.cs`, duas opções, desligadas por padrão): `groupOnly` (Nuvem Tóxica e Flecha Dispersa só com 2+ monstros) e `selfBuffRange` (o Atirador usa o Olho de Falcão com o inimigo a até 12 tiles; padrão 6, o de antes). Estratégia: o Atirador abre com o Olho de Falcão e o Tiro Certeiro, para de se aproximar a ~9,5 tiles (`kiteDistance=8`) e deixa o Tiro Debilitante de lado; o Caçador usa a barra com `groupOnly`.
- `design/segunda-classe-v0.1.md` §6 ("Ajustes do simulador (T-433)"), `design/progression-v0.1.md` §8 ("Metas do Arqueiro" e a revisão do Guardião), `design/skills-v0.1.md` (seção nova com custos e recargas).
- Site: `./scripts/build-site.sh` gera sem erro (94 páginas). O gerador não tem páginas de habilidades de segunda classe (de nenhuma classe), então as novas não aparecem (pendência).

### Números que mudaram em relação ao design

| Habilidade | Antes | Depois | Por quê |
|---|---|---|---|
| Tiro Certeiro | custo 35 | **20** | o Arqueiro tem 90 de Mana; com 35, o Tiro Certeiro não saía junto com o Olho de Falcão (70) e o Atirador ficava em 84–98% do tempo da base contra a elite (meta: 85%); com 20, 73–78% |

O dano do Tiro Certeiro não precisou cair, e a duração do Olho de Falcão é a do designer (15/17/20 s). Os outros números são os do design.

### Metas revistas (com o motivo no relatório)

- **Atirador e Caçador no 25:** no máximo o tempo da base (era 85%); os 85% valem do 30 em diante. O perfil do 25 tem só 2 NH da segunda classe e o Arqueiro não tem Mana sobrando; o Caçador fica em 90% no 25 (79% no 30, 63% no 40). Relatório §3.
- **Olho de Falcão:** no máximo **1,5×** o dano em 20 s sem ele (proposta: 2,5×, que nunca é alcançada: o teto é 1,65×). Medido: 0,98–0,99×. Relatório §5.
- **Diferença entre segundas classes:** o **Guardião** até **2,0×** (era 1,6×), como o Sacerdote sem limite; com as duas do Arqueiro no Lobo e Salteador, a média caiu e o Guardião foi a 1,68× (30) e 1,76× (40). Relatório §4.

### Decisões

- **Alvo único forte = a Aranha Grande** (elite) do nível, sozinha, na arena dos Três Lobos: é a única elite que se enfrenta sozinho (o Cavaleiro Espectral mata o Arqueiro solo em 100% dos lotes; o Ent é normal).
- **Par do perfil focado:** Tiro Certeiro e Mira Firme (Atirador); Flecha Envenenada e Ferida Aberta (Caçador; com a Nuvem no par o Caçador ficava mais lento: os Lobos saem da nuvem correndo atrás do Arqueiro).
- **Passivas:** a Flecha Envenenada usa o mesmo estado em todos os NH (o veneno do NH vem do apelido `byRank`); a Mira Firme, um estado por NH.
- O Olho de Falcão aplica o estado sem `target: self` (a mira já é `self`), para o bot o ver como reforço próprio.
- O Tiro Debilitante não tem a etiqueta `control` (elites e chefes não o encurtam); etiqueta nova `weaken`.

### Desvios (fora das áreas listadas, o mínimo)

- `src/Game.Bots/ConfigurableBot.cs` e `BotOptions.cs`: as opções `groupOnly` e `selfBuffRange` (o bot fica em `src/`, não em `tests/Game.Sim/`). Sem as opções, nada muda nos testes de antes.
- `tests/Game.Design.Tests/SecondClassDesignTests.cs`: as duas entradas do Arqueiro em `Specs` e o limite do Guardião (2,0×), como pediu o integrador.
- Nada em `src/Game.Core` nem no schema.

### `check.sh` completo

`./scripts/check.sh` completo passou no Windows, sobre `e471604`, com o PC dividido com outros agentes: formatação, build sem avisos e todos os testes (Core 590, Data 507, Protocol 74, Client 128, Server 117 + 1 ignorado, Persistence 2 + 8 ignorados, Sim 75, Site 15, Api 39 + 7 ignorados; os ignorados pedem banco, como antes) e **Design 281 em 10 min 36 s**.

### Pendências

- **Olho de Falcão com 70 de Mana quase não rende** (0,98–0,99× do dano em 20 s; elite do 40 em 8,9 s com ele e 9,1 s sem). Decisão do designer: manter, baixar para 40 ou 30 (medido no relatório §5) ou dar algo além do crítico.
- **Caçador contra alvo único** é quase tão rápido quanto o Atirador (78–89% × 73–78% do tempo da base contra a elite).
- **Nuvem Tóxica no bot:** mira no monstro mais perto; contra quem persegue o Arqueiro, os monstros saem dela. Um bot que mire entre o Arqueiro e o grupo, se o designer quiser medir a nuvem.
- **Site:** páginas das habilidades de segunda classe (de todas as classes) e rótulo da etiqueta `weaken`; o `Describe`/`Usage` do site ainda não descrevem os campos da T-432 (pendência da T-432).
- **Cliente** (T-434): alcance a mais da Mira Firme e do Olho de Falcão na mira (`RangeBonus.Of`, pendência da T-431); ícones e textos.
- A diferença entre segundas classes deve ser revista de novo com o Mago (T-443), quando a última base sair da média.
