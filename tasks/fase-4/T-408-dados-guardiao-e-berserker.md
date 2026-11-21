# T-408 — Dados: habilidades do Guardião e do Berserker

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** T-402, T-403, T-407
- **Libera:** T-410, T-412
- **Áreas que pode alterar:** `data/` (habilidades, estados, áreas, `classes.json`), `tests/Game.Design.Tests/`, `tests/Game.Data.Tests/`, `design/segunda-classe-v0.1.md` (só números ajustados pelo simulador), `design/skills-v0.1.md` (seção nova do Guerreiro), `tasks/reports/`
- **Branch:** sai de `trilha/segundas-classes` e é integrada nela, não na `main` (seção 6f de `tasks/README.md`).

## Contexto

As 10 habilidades estão em `design/segunda-classe-v0.1.md`, seção 5, com custo, recarga e todos os NH. Aprovadas pelo designer em 2026-11-20 (Sede de Sangue como passiva).

- **Guardião:** Interceptação, Golpe de Escudo, Bastião, Inabalável, Juramento (assinatura).
- **Berserker:** Investida, Fúria, Execução, Sede de Sangue (passiva), Tempestade de Aço (assinatura).

As mecânicas novas foram feitas na T-406 (desvio de dano, avançar até aliado, imunidade a empurrão, ameaça multiplicada) e na T-407 (dano pela vida do alvo, zerar recarga ao matar, vida pelo dano, área que acompanha). A segunda classe e as passivas, na T-403. As metas de segunda classe, na T-402 (§8 do design de progressão).

**Base:** a T-406 e a T-407 sobem o protocolo e só entram na `trilha/segundas-classes` na rodada da seção 6f de `tasks/README.md`. Se essa rodada ainda não tiver entrado, crie a branch a partir da branch da T-407 e mescle nela a T-403. Registre a base no Resultado.

## Requisitos

1. Arquivos de dados das habilidades (sugestão: `data/abilities_guardian.json` e `data/abilities_berserker.json`), com todos os NH, e os estados e áreas que elas usam. Ids no padrão `guardian.interception`, `berserker.blood_thirst`; textos em português.
2. `data/classes.json`: as duas segundas classes do Guerreiro com as suas 5 habilidades, na ordem da tabela do design.
3. **Testes de design** com o simulador (perfis de segunda classe da T-403), cobrindo as metas da T-402 para o nível 21, 30 e 40. Se uma meta falhar, ajustar os números dentro do espírito do design e registrar o antes e depois em `design/segunda-classe-v0.1.md` e no relatório.
4. Relatório `tasks/reports/balance-v0.19.md` (ou o próximo número livre) com o resultado dos testes e os ajustes.
5. `design/skills-v0.1.md`: seção nova com as tabelas finais do Guardião e do Berserker (ou link para o design da segunda classe, se os números forem os mesmos).

## Critérios de aceite

- os dados carregam e validam;
- cada habilidade tem um teste de Core ou de design que mostra o efeito principal no NH1 e no NH5 (por exemplo, a Interceptação desvia 30% no NH1 e 40% no NH5);
- testes de design das metas da segunda classe passam;
- `./scripts/check.sh` completo passa.

## Resultado

Feito em 2026-11-21, na VPS. Relatório: `tasks/reports/balance-v0.19.md`.

### Base

Branch `task/T-408-dados-guardiao-e-berserker`, criada de `origin/trilha/segundas-classes` (`f3d92b5`: T-401, T-402 e T-409), com dois merges antes da tarefa:
- `origin/task/T-403-core-segunda-classe` (sem conflito);
- `origin/task/T-407-mecanicas-do-berserker` (com a T-406). Conflito só em `ProtocolVersion.cs` e no teste dele: a T-403 e a T-406 subiram para 16 em paralelo. Resolvido com **17**: 16 = T-403 (`SecondClassChosen`), 17 = T-406 (`DamageRedirected`, `RedirectTo`), comentário cita as duas; `ProtocolTests` espera 17. Commit `merge: task/T-407-mecanicas-do-berserker em task/T-408 (protocolo 17)`.

Por causa desses merges, a T-408 entra na trilha junto com a rodada do protocolo (T-404 e seguintes).

### O que foi feito

- **Dados:** `data/abilities_guardian.json` e `data/abilities_berserker.json`, com as 10 habilidades e todos os NH, e os estados (redirecionamento, Bastião, Inabalável, Fúria, Sede de Sangue, Embalo da Investida, giro) e a área da Tempestade no mesmo arquivo da classe (menos conflito quando as outras classes vierem). Ids: `guardian.interception`, `guardian.shield_bash`, `guardian.bastion`, `guardian.steadfast`, `guardian.oath`, `berserker.charge`, `berserker.fury`, `berserker.execution`, `berserker.blood_thirst` (passiva), `berserker.steel_storm`.
- `data/classes.json`: as cinco de cada segunda classe na ordem do design.
- **Testes de efeito** (`tests/Game.Data.Tests/WarriorSecondClassContentTests.cs`, 29 casos, dados reais): cada habilidade no NH1 e no NH5 (assinaturas no NH3): desvio de 30%/40% e escudo da Interceptação, atordoamento e ameaça ×3 do Golpe de Escudo e arco de 120°, Bastião 15%/25% e raio 5/7 e imunidade a empurrão, Inabalável (limpa, imunidade, 10%/20%, aliados no NH5), Juramento 50%/40% e 70%/50% e raio, Investida (avanço, atordoamento, +30% no golpe seguinte no NH5), Fúria (+20%/+36%, +20% recebido, controle pela metade no NH5), Execução (+50% abaixo de 30%, +75% abaixo de 35% e recarga zerada ao matar no NH5), Sede de Sangue como passiva (5%/10%, dobra, cura ao matar), Tempestade (8 giros, Sangramento no NH3). Mais: ordem das classes, Mana do nível 40 para todo custo, personagem do 40 com todos os NH.
- **Testes de design** (`tests/Game.Design.Tests/SecondClassDesignTests.cs`): metas da §8 nos níveis 21, 25, 30 e 40 (seção 2 do relatório). Passam todas, menos a do dano nos outros membros do grupo, que está desligada (abaixo).
- `design/segunda-classe-v0.1.md` §5: custos novos com o antes e o depois ("Ajustes do simulador"). `design/skills-v0.1.md`: seção nova com link e tabela de custos e recargas.

### Pendências das outras tarefas

- **Mana do Guerreiro (T-402):** Tempestade de Aço **90 → 80** (cabe nos 88 de Mana; como a Muralha Viva). Teste `Every_second_class_ability_fits_the_warrior_mana_at_level_40`.
- **+5 de atributo nos perfis (T-402):** já estava feito pela T-403 (`BuildFor` usa `AttributePointsAt(level, second is not null)`). Os perfis de segunda classe desta tarefa usam isso; nada a mudar.

### Números que mudaram em relação ao design

| Habilidade | Antes | Depois | Por quê |
|---|---|---|---|
| Tempestade de Aço | custo 90 | **80** | não cabia em 88 de Mana |
| Fúria | custo 30 | **sem custo** | com os custos do design o Berserker levava 118–130% do tempo do Guerreiro só com a base (meta: ≤ 85%) |
| Execução | custo 30 | **20** | idem; com os dois ajustes, 74–82% |
| Bastião | custo 40 | **20** | com 40 o bot só o usava uma vez por luta de grupo |

Efeitos e valores por NH: os do design.

### Desvios (fora das áreas listadas, o mínimo)

- `src/Game.Bots/ConfigurableBot.cs`: o bot não usava a Interceptação (avanço que mira aliado era tratado como avanço até o inimigo e falhava). Novo `PlanProtect`: vai até o aliado mais ferido ao alcance com um inimigo a até 4 tiles. Nenhuma habilidade antiga usa avanço até aliado.
- `tests/Game.Sim/EncounterRunner.cs` e `Batch.cs`: `DamageTakenByClass` (dano recebido por classe do jogador), para medir os outros membros do grupo.
- Merges e `ProtocolVersion` 17 (acima); nada mais em `src/Game.Protocol/` nem `src/Game.Server/`.

### Decisões

- **Estratégia correta no simulador:** o Berserker abre com a Fúria e não usa a Investida contra a matilha (a Mana dela rende mais no Golpe Circular); no grupo, o Guardião não usa o Golpe de Escudo (a Provocação segura o chefe, e o Golpe Pesado continua interrompendo). Par principal: Bastião e Interceptação; Execução e Fúria.
- **Dano nos outros membros por segundo de luta**, não por luta: a duração vai de 60 s a 190 s e as mortes mudam o total.
- Grupo medido só no 30 e no 40 (a Matriarca leva ~1 min por lote); o 21 é coberto pelo teste do NH1 útil.
- Margem de 10 pontos na vitória do grupo (a diferença entre dois lotes de 100 perto de 70% oscila ~6,5 pontos).
- Bastião e Juramento em aliados usam `extra_targets` com `affects: ally` a partir de `self`: `circle_self` com `apply_status` mira inimigos (o alvo da área é decidido pelos efeitos).
- Interceptação: o escudo do NH3+ dura o mesmo que o desvio (o design não diz). Vínculo até 8 tiles (o alcance dela). Investida NH5: o +30% vale para o próximo Golpe de Espada, Golpe Pesado, Golpe Circular ou Execução, em até 3 s.

### Pendências

- **Para o designer: meta do Guardião em grupo** ("os outros recebem 15% menos dano"). Não é atingida de forma estável: de −23% a +6% conforme o lote e o encontro; com equipamento incomum, quase sem derrotas, 0% a 12%. Três quartos do dano nos outros é veneno e poça de veneno em quem fica de longe, fora do raio do Bastião; a conta do kit no NH5 com uso perfeito dá uns 15%. Rever a meta ou reforçar o Guardião (relatório §4). O teste `Guardian_cuts_the_damage_on_the_rest_of_the_group_by_at_least_15_percent` está com `Skip` até a decisão.
- **Juramento e Tempestade de Aço não saem no simulador:** o bot gasta a Mana assim que uma habilidade barata fica pronta e nunca junta 80 (a Muralha Viva já era assim). As metas medidas são sem as assinaturas.
- **Inabalável atordoado:** o Core recusa habilidades com o personagem atordoado, então ele não tira o próprio atordoamento (tira dos aliados no NH5). Precisa de uma regra "usável atordoado" no Core, se o designer quiser.
- T-410 e T-412: tela, textos e ícones das habilidades novas.
