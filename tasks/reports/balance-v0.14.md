# Relatório de balanceamento v0.14 (T-343): Ruínas da Estrada Antiga

A área de nível 9–11 que sai pelo portão oeste da Cidade. Design: `design/first-map-v0.1.md` §4. Mapa: `data/maps/old_road_ruins.tmj`. Testes: `tests/Game.Design.Tests/RuinsZoneDesignTests.cs` (design), `tests/Game.Data.Tests/ZoneLoaderTests.cs` e `tests/Game.Sim.Tests/ZoneMapTests.cs` (zona e percurso). Imagens: `tasks/reports/mapas-t343/`.

## 1. Método

- **Grupos:** como nos Campos (T-215), pontos da mesma subárea a até 4 tiles formam um grupo. Cada grupo típico (um por composição) vira um encontro na arena aberta do protótipo, com os monstros nos níveis do mapa. Monstros à distância e de apoio (o Sacerdote) começam atrás.
- **Perfil:** `CharacterProfile.Expected(nível do grupo)`; no solo, NH focado.
- **Estratégia dos bots:** a da T-341. Guerreiro e Clérigo usam `rotation+dodge`; Arqueiro e Mago usam `kite+dodge`.
- **Limite de tempo:** 120 s nos grupos solo; 240 s no Pátio dos Mortos, o mesmo do encontro da T-341.
- **Lotes:** 40 seeds na medição; o `./scripts/check.sh` completo roda os testes com 100.

## 2. Distribuição

| Subárea | Grupos | Composição | Renascimento |
|---|---|---|---|
| Portão Partido | 8 | 1–2 Guardas nv9 | 45 s |
| Pátio dos Mortos | 5 | 3 Guardas nv9 + Sacerdote nv10 (4 grupos); altar: 2 Guardas nv9 + Guarda nv10 + Sacerdote nv10 | 45 s |
| Torre Quebrada | 1 | Cavaleiro Espectral nv11, parado (raio 0) | 300 s |
| Estrada para a Cripta | 9 | 2 Guardas nv10, 2 nv11 ou nv11 + nv10 | 45 s |

Todos os grupos ficam a ≥ 12 tiles uns dos outros e do nascimento da zona.

## 3. Resultados

| Teste | Meta | Resultado |
|---|---|---|
| Grupos solo (Portão e Estrada), cada classe sozinha | ≥ 85% | 100% em todos |
| Pátio, cada par | ≥ 80% | 100% em todos |
| Pátio, solo (média das 4 classes) | 40–70%, ao menos uma classe ≥ 50% | 62–64% |
| Torre | um Cavaleiro nv11, renascimento de 5 min | ok (a luta em grupo é da T-341) |

**Ritmo solo** (modelo dos Campos: tempo médio de vitória das 4 classes + 60 s fora da luta, limitado pelo renascimento), sem meta:

| Nível | Tempo para subir |
|---|---|
| 9 → 10 | 65,2 min |
| 10 → 11 | 64,1 min |
| 11 → 12 | 72,5 min |
| **Total** | **≈ 202 min** |

O número fica para a T-380 avaliar a curva do primeiro mapa.

## 4. Decisões

- **Sem Sacerdote na Estrada.** O requisito pede monstros "mistos", nível 10–11, e a Estrada é área solo. Com Sacerdote, o Guerreiro sozinho venceu 5% e o Arqueiro 23–50%. Os grupos mistos da Estrada são Guardas de nível 10 e 11 juntos; os Sacerdotes ficam só no Pátio, que é "grupo recomendado".
- **240 s no Pátio.** Com 120 s, o Clérigo sozinho perdia por tempo e a média caía para 37–39%. Com o limite do encontro da T-341, a média fica em 62–64%.
- **Altar:** só cenário; a maldição do design não tem mecânica nesta fase.

## 5. Online (servidor local, `--world --dev-login`)

- 4 bots (um por classe, nível 1), com `--route zone.old_road_ruins/crypt_entrance`, saíram da Cidade pelo portão oeste e chegaram à entrada da Cripta em 45–51 s. Cada um morreu uma vez no caminho e voltou.
- 2 bots começando nas Ruínas, com `--route zone.city/plaza`, voltaram à Cidade em 6 s.
- O desempenho com 16 bots não foi medido.
