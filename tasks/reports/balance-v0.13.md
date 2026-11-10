# Relatório de balanceamento v0.13 (T-351): o Vigia Sem Rosto

O chefe final do primeiro mapa: nível 15, três fases, selos a proteger. Design: `design/combat-encounters-v0.1.md` § Cripta do Vigia. Testes: `tests/Game.Design.Tests/WatcherDesignTests.cs` (design) e `tests/Game.Core.Tests/WatcherTests.cs` (mecânicas).

Meta (`design/progression-v0.1.md` §8): grupo de 4 no nível 14–15 com raros, vitória de 50–80% em 4–6 min.

## 1. Método

- **Encontro:** `encounter.faceless_watcher`, arena `map.watcher_hall`. É um salão aberto de 40 × 28, cópia provisória do ninho da Matriarca; a sala real fica para a T-352.
  - O Vigia nasce no centro-leste.
  - Os 3 Selos ficam ao norte, ao sul e a leste, perto das bordas.
  - O grupo é Guerreiro + Arqueiro + Mago + Clérigo.
- **Perfil (T-206):** nível 14 ou 15, atributos sugeridos, equipamento **raro** completo do nível e NH focado (a assinatura entra a partir do nível 13).
- **Estratégia dos bots:**
  - Guerreiro e Clérigo usam `rotation+dodge`; Arqueiro e Mago usam `kite+dodge`, como nos encontros da fase 3.
  - Opções novas desta tarefa:
    - `interrupt=save` (Guerreiro): guarda o Golpe Pesado, e a Mana dele, para quem está conjurando, e anda até essa pessoa.
    - `followInjured=1` (Clérigo): anda até o aliado ferido que está fora do alcance da cura, como os selos.
  - Esta é a "estratégia correta". O padrão dos bots (`interrupt=auto`) gasta o Golpe Pesado no giro da rotação e quase nunca o tem pronto no Último Comando: 3% de vitória no nível 15, em 30 execuções.
- **Lotes:** 100 seeds nos lotes da meta; 50 nas comparações, que ficam longe do limite. O tempo vale só para as vitórias.

## 2. Números finais (nível 1; entre parênteses, no nível 15)

O Vigia é chefe, com vida × 5,2 e potência × 6,32. Selos e invocados são normais no nível 15, com vida × 6,6 e potência × 3,38.

| | Números |
|---|---|
| Vigia | vida 45.000 (234.000), velocidade 3,4, raio 0,9 |
| Golpe de Ferro (fase 1) | físico 38 (240), a cada 1,6 s |
| Golpe Espectral (fases 2–3) | arcano 36 (228), a cada 1,6 s |
| Corte do Juramento | cone de 90° × 4, telegráfo de 1,2 s, físico 120 (758), recarga de 7 s |
| Investida de Ferro (fase 1) | linha de 8 × 1,8, telegráfo de 1,2 s, físico 150 (948), recarga de 12 s |
| Chamado da Guarda (fase 1) | conjuração de 2,5 s, 2 Guardas (máximo 2), recarga de 24 s |
| Sombras do Rosto Vazio (fase 2) | conjuração de 2 s, 3 Sombras (máximo 3), recarga de 20 s |
| Chão do Pavor (fases 2–3) | círculo de 2,5, telegráfo de 1,2 s, arcano 70 (442) + medo de 2 s, recarga de 8 s |
| Rosto Vazio (fase 2 em diante) | dano sagrado recebido × 1,4 |
| Último Comando (fase 3) | conjuração de 4 s no selo mais ferido, arcano 2.000 (12.640), recarga de 10 s, **também quando interrompido** |
| Sombra | vida 150 (990), Toque Gélido arcano 18 (61) a cada 1,4 s |
| Selo do Juramento | vida 380 (2.508); intacto (não pode ser alvo) até a fase 3; na fase 3, arcano 4 (25) por segundo |

**Limiares de fase:** 65% e 25%.

## 3. Grupo de 4 com raros (meta)

| Nível | Vitória | Tempo (vitórias) | Selos perdidos | Falhas | Mortes |
|---|---|---|---|---|---|
| 14 | 57% | 316 s | 1,77 | 42 | 0,65 |
| 15 | 80% | 295 s | 1,12 | 20 | 0,36 |
| **Média** | **68,5%** | | | | |

As duas lutas ficam entre 4 e 6 min. Quase toda derrota é **falha por selos**: em média, menos de 1 morte de jogador por luta.

**Como o grupo perde, nas linhas do tempo de 12 lutas no nível 14:**
1. O Guerreiro cai no fim da fase 2 ou no começo da fase 3. O Golpe Espectral e as Sombras são arcanos, e a armadura dele não reduz esse dano.
2. Sem Guerreiro, ninguém interrompe.
3. Três Últimos Comandos depois (~45 s), os selos acabam.

Com o Guerreiro vivo, a interrupção acerta 5 a 8 conjurações de 6 a 8.

**Degrau entre os níveis 14 e 15.** São cerca de 23 pontos de vitória entre os dois níveis. O Golpe Espectral foi o ajuste que moveu a vitória dos dois níveis juntos:

| Golpe Espectral | Nível 14 | Nível 15 |
|---|---|---|
| 38 | 50% | 72% |
| 36 | 57% | 80% |
| 35 | 60% | 85% |
| 32 | 65% | 93% |

Como na T-341 (Cavaleiro), **a meta é a média dos dois níveis**. Cada nível precisa ficar entre 40% e 90%. Hoje os dois estão dentro de 50–80%, mas o 15 está no limite.

## 4. Medição pedida pela T-206: nível 12+ com a assinatura

O Vigia com o grupo de 4 e raros, 60 seeds por linha. "Sem assinatura" põe a assinatura num nível que não se alcança (`signatureRankLevels` 16/17/18), e os pontos vão para as habilidades normais.

| Nível | NH da assinatura (perfil focado) | Com assinatura | Sem assinatura |
|---|---|---|---|
| 12 | 0 (os 12 pontos vão para as normais) | 0% | 0% |
| 13 | 1 | 23%, 360 s | 17%, 349 s |
| 14 | 2 | 52%, 316 s | 52%, 304 s |
| 15 | 3 | 82%, 296 s | 85%, 288 s |

**Leitura:**
- A curva de 12 a 15 é íngreme, com 20 a 30 pontos por nível. Quase todo o salto vem da vida, dos atributos e dos itens. **A assinatura quase não muda a curva**: +6 pontos no 13, igual no 14 e −3 no 15.
- Sem a assinatura, as lutas ficam **mais curtas** (7 a 12 s). Os pontos nas normais rendem mais dano do que Cataclismo, Flecha Devastadora e Milagre usados 2 a 3 vezes por luta.
- O perfil focado só põe ponto na assinatura a partir do nível 13: no 12, os 12 pontos fecham as normais e o par no NH5.
- A preocupação da T-206 ("a assinatura muda a curva") **não se confirmou neste encontro**. O que muda a curva é o nível.
- Pendência: medir a assinatura num solo do nível 12+, quando houver conteúdo solo desses níveis. O Milagre NH3 (revive) e a Muralha Viva podem pesar mais onde a morte é a derrota.

## 5. Comparações (nível 15, exceto onde indicado)

| Situação | Vitória | Meta |
|---|---|---|
| Grupo completo, interrompendo (`save`) | 80% | 50–80% (média) |
| Sem interromper (`never`), nível 14 | 0% | < 30% |
| Sem interromper (`never`), nível 15 | 0% | < 30% |
| Padrão dos bots (`auto`), 30 seeds | 3% | — |
| Sem Clérigo (Guerreiro + Arqueiro + Mago + Mago) | 0% (1 morte por luta; os selos caem) | cai de forma clara |
| Sem Guerreiro (Arqueiro + Arqueiro + Mago + Clérigo) | 0% (ninguém interrompe) | cai de forma clara |

**Leitura:**
- O Vigia exige as quatro funções: segurar, interromper, curar e proteger os selos, e dano.
- Sem Guerreiro, a derrota é certa, porque **só o Guerreiro tem interrupção**. Grupos sem Guerreiro não vencem o Vigia hoje.
- Isso vale para o MVP, mas fecha a composição. Se o designer quiser outra composição possível, uma interrupção em outra classe (ex.: o Mago) ou uma forma alternativa de parar o Comando resolveria.

## 6. Como se chegou aos números

- **Selos atingidos cedo.** Os ataques em área do chefe (investida e corte) destruíam selos já na fase 1, antes de "começarem a ruir". Daí o **Selo Intacto** (`untargetable`) até a fase 3.
- **Interrupção sem efeito.** Interromper não dava recarga, e o chefe reconjurava o Comando no tick seguinte. Daí `cooldownOnInterrupt`: interrompido, o Comando entra na recarga cheia.
- **Golpe Pesado desperdiçado.** O Guerreiro começava o golpe longe demais e errava, ou chegava sem Mana. O modo `save` só usa o golpe com quem conjura ao alcance, anda até ele e reserva os 30 de Mana.
- **Revive dos selos.** O Milagre NH3 revivia selos destruídos. Os objetivos agora ficam fora do `revive`.
- **Vida e dano:**
  - Vida 18.000 dava lutas de 113 s; 45.000 dá 5 min.
  - Golpes básicos de 55–80 matavam o Guerreiro em qualquer nível, porque a cura sustentada do Clérigo é ~60–80/s.
  - Os golpes básicos ficaram em 36–38; o perigo grande ficou nos golpes telegrafados.
- **Fase 3 e ruína:** a fase 3 entra em 25% (era 30%) e a ruína dá 4 por segundo (era 6). Assim a fase 3 dura ~60–80 s, e o Clérigo divide a cura entre o Guerreiro e os selos sem perder os dois.

## 7. Mecânicas novas no Core

- **Medo** (`flags.feared`):
  - Quem está com medo não age e foge do ponto de origem: o centro do telegráfo ou da área, ou a posição de quem aplicou. A intenção de movimento é ignorada.
  - Com a tag `control`, vale o rank (chefe × 0,25), o retorno decrescente e o `immuneToTags`.
  - No código: `StatusInstance.Origin`; `IModifierSource.ForcedDirection` consumido pelo `MovementSystem`.
- **Objetivos do encontro** (`objectives`):
  - Unidades da facção dos jogadores, sem IA e com velocidade 0. A separação não as empurra.
  - Não puxam aggro e não são revividas.
  - `ObjectiveSystem` publica `ObjectiveDestroyed` e, quando o último cai, `EncounterFailed`. O simulador conta isso como derrota.
- **`cooldownOnInterrupt`** (habilidade): interrompida, entra na recarga cheia.
- **`modifiers.damageTakenBySchool`** (status): multiplicador de dano recebido por escola.
- **`onEnterObjectives`** (fase): efeitos em cada objetivo vivo, com o chefe como fonte.
- **`ai.objective`** (tag do `ai.boss`): usa a habilidade no objetivo inimigo mais ferido.

## 8. Conferências

- `WatcherTests` (Core, 16 testes):
  - medo: fuga, fim, origem no ponto, cancela a conjuração, rank e imunidade;
  - selos: fixos, aliados, cura e escudo, falha uma vez só, sem falha sem objetivos, sem aggro e sem revive;
  - recarga ao ser interrompida;
  - dano por escola;
  - efeitos de fase nos objetivos e `ai.objective`;
  - dados reais: fases e selos, Último Comando × Golpe Pesado, Chão do Pavor;
  - log.
- `WatcherContentTests` (dados, 4 testes) e `WatcherDesignTests` (design, 5 testes).
- Os outros testes de design não mudaram: as opções novas dos bots vêm desligadas e nenhum monstro antigo mudou.
