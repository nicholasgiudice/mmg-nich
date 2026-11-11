# T-342 — Mapa e zona: Floresta Baixa (com o Ninho da Matriarca)

- **Ambiente:** Windows (Tiled)
- **Modelo:** Humano + Médio
- **Depende de:** T-330, T-331, T-340
- **Libera:** T-380
- **Áreas que pode alterar:** `data/maps/` (mapa novo `lower_forest.tmj`, `city.tmj` para abrir o portão norte, `vigil_fields.tmj` e `matriarch_lair.tmj` para a mudança do Covil), `data/zones.json`, `data/maps/tilesets/`, `client/assets/art/tiles/` (com créditos), `CREDITS.md`, `tests/` (testes de percurso e de design da área), `tasks/reports/`

## Contexto

A Floresta Baixa (`design/first-map-v0.1.md` §3) é a área de nível 5–8, saindo pelo **portão norte** da Cidade. Subáreas: Trilha dos Caçadores, Bosque das Teias, Clareira da Névoa e **Ninho da Matriarca** (opcional, para grupo pequeno). A floresta bloqueia a visão, tem caminhos estreitos, e a cidade "desaparece" rápido.

Hoje a Matriarca fica no `zone.matriarch_lair`, com o portão na borda leste dos Campos: foi um atalho da fase 2 para o playtest. No design, ela fica dentro da Floresta. Os monstros Fada e Ent vêm da T-340. Os mapas maiores e a leitura de surgimentos do Tiled vêm da T-330 e da T-331.

## Requisitos

1. **Mapa** `data/maps/lower_forest.tmj`, com cerca de **200×160**, as quatro subáreas em `areas`, e caminhos estreitos com clareiras. Use árvores e a camada `above` (copas) para a sensação de mata fechada, sem esconder telegráfos (o contorno fica acima, T-054).
2. **Zona** `zone.lower_forest` (tipo `field`), com a faixa de nível 5–8:
   - Trilha: poucos monstros, níveis 5–6;
   - Bosque das Teias: aranhas 5–6 e aranha grande 6 (elite);
   - Clareira da Névoa: fadas 6–7 e um Ent 7;
   - entrada do Ninho.
3. **Matriarca:** o Ninho passa a ser a zona `zone.matriarch_lair`, com o portão **a partir da Floresta**. O portão nos Campos sai. O `matriarch_lair.tmj` pode ser reaproveitado; só o portão muda.
4. **Portão norte da Cidade** aberto, ligando à Floresta.
5. **Densidade e ritmo:** grupos a ≥ 12 tiles entre si. Teste de design da área como o `FieldsDesignTests`: o perfil esperado nível 5–8 vence ≥ 85% de cada grupo típico, e o modelo de ritmo leva do nível 5 ao 8 num tempo registrado no relatório (sem meta fixa, só a medição; a T-380 avalia).
6. O designer aprova o mapa.

## Critérios de aceite

- teste de percurso: todo surgimento e portão alcançável a pé;
- testes de design da Floresta passando;
- online (servidor local `--world --dev-login --start-zone zone.lower_forest`): entrar pela Cidade, chegar ao Ninho e voltar;
- `./scripts/check.sh` completo passa.

## Resultado

**Feito.** Relatório: `tasks/reports/balance-v0.16.md`; imagens em `tasks/reports/mapas-t342/`.

- **Mapa** `data/maps/lower_forest.tmj` (200 × 160): mata fechada (copas na camada `above`), trilhas de 3 a 4 tiles e clareiras.
  - Subáreas: Trilha dos Caçadores (centro e leste), Bosque das Teias (oeste), Clareira da Névoa (norte) e Ninho da Matriarca (nordeste).
  - A borda sul é a muralha norte da Cidade, com o portão de 2 tiles (`south_gate`).
  - A entrada do Ninho é a boca de uma caverna numa encosta de pedra (`nest_gate`, dentro da área `nest_entrance`).
- **Zona** `zone.lower_forest` (campo, nível 5–8) em `data/zones.json`, com 24 grupos a ≥ 12 tiles entre si:
  - Trilha: Aranhas nv5–6 e um par de Fadas nv6;
  - Bosque: Aranhas nv5–6 e a Aranha Grande nv6 (elite, renasce em 3 min);
  - Clareira: Fadas nv6–7 e o Ent nv7 (renasce em 2 min);
  - Ninho: Aranhas nv7.
- **Cidade:** portão norte (`north_gate`, 2 × 4 tiles na muralha, como o oeste) e a chegada `from_forest`.
- **Matriarca:**
  - o portão oeste do Covil (`matriarch_lair.tmj`) leva à Floresta, e a chegada `from_fields` passou a se chamar `from_forest`;
  - nos Campos, o portão leste e a chegada `from_lair` saíram, a estrada acaba antes da borda e a sebe fecha a borda.
- **Testes:**
  - percurso e espaçamento (`ZoneMapTests`): tudo alcançável a pé, as quatro subáreas, os níveis 5–8, o portão norte e a ida e volta do Covil;
  - design (`ForestZoneDesignTests`).
- **Resultados:**
  - todos os grupos vencem com 96–100% no perfil esperado, nas 4 classes;
  - o ritmo solo do nível 5 ao 8 é de cerca de 78 min (20, 24 e 34 min por nível);
  - online, os bots fizeram Cidade → Ninho → Covil e Covil → Floresta → Cidade;
  - o `./scripts/check.sh` completo passa.

**Decisões:**
- **Placas, teias e névoa:** o tileset não tem poste de placa, teia nem névoa. Uma plaqueta sobre um pedaço de cerca ficou ruim, e o único tile translúcido parecia um quadrado marrom. Ficaram de fora. A Clareira tem árvores de copa redonda, e o Bosque tem poças escuras e árvores secas.
- **Visibilidade reduzida e emboscadas:** vêm do mapa, das árvores que cortam a linha de visão e das trilhas estreitas. A Emboscada na Névoa (3 Aranhas + Fada) da T-340 não entrou como grupo de campo, para a área continuar boa para jogar sozinho.
- **Covil:** o mapa é o mesmo, só o portão mudou. A `zone.test_instance` usa esse mapa, então a saída dela também passou a levar à Floresta. Os testes do servidor (`InstanceTests`) trocam essa saída para os Campos, como antes.
- Como na T-330, na T-343 e na T-352, o gerador do mapa ficou fora do repositório. O `.tmj` é a fonte daqui em diante.

Em `tests/`, também mudei:
- `ZoneMapEquivalenceTests`: o portão do Covil mudou de zona;
- `ZoneLoaderTests`: a instância de teste sai na Floresta;
- `InstanceTests`: a saída da instância de teste.

**Pendências:**
- **Arte:** teia no chão, névoa e poste de placa.
- **T-380:** avaliar se a Floresta está fácil demais para quem caça sozinho e o ritmo do nível 5 ao 8.
- **T-345:** mostrar no cliente o portão do Ninho e a área `nest_entrance` (aviso para grupo pequeno, nível 8).
