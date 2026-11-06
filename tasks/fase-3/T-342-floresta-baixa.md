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

_(preencher ao concluir)_
