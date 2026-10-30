# Relatório de balanceamento v0.8 (T-215): Campos da Vigília

Primeiros dados de uma zona aberta (`data/zones.json`, `zone.vigil_fields`). Simulador `tests/Game.Sim`, 100 seeds por linha, mesmas opções dos testes de design. Base: `balance-v0.7.md` (perfis por nível e monstros por nível).

## 1. Subáreas e grupos

Cada ponto de surgimento tem `area` (subárea do `design/first-map-v0.1.md` §2). Um **grupo** são os pontos da mesma subárea a até 4 tiles um do outro: o jogador puxa o grupo inteiro de uma vez. Grupos diferentes ficam a pelo menos 12 tiles (o aggro por proximidade é 8) e longe da chegada pelo portão. Isso é conferido em `ZoneLoaderTests`.

| Subárea | Grupos | Composição | Nível do grupo | Renascimento |
|---|---|---|---|---|
| Estrada Leste | 2 | 2 Ratos nv1 | 1 | 40 s |
| Fazenda Abandonada | 2 | 2 Ratos nv1 + 1 Lobo nv2; 2 Lobos nv2 | 2 | 45–50 s |
| Clareira dos Caçadores | 2 | 3 Lobos nv3 | 3 | 40 s (o menor para o tamanho do grupo) |
| Acampamento dos Salteadores | 2 | 1 Lobo nv4 na frente + 1 Salteador nv4 atrás | 4 | 60 s |

O mapa provisório (`arena.vigil_fields`, 96×64) foi redesenhado para caber isso. A Fazenda tem cerca e casa, e o Acampamento tem paliçada. Os mapas do Tiled (T-055) devem manter as distâncias.

**Por que não maiores.** As sondagens (30 seeds) mostraram o limite do perfil esperado nos níveis baixos, que tem só 1 a 4 pontos de habilidade:

- **Mago parado (`rotation`):** contra 2 Lobos do mesmo nível, vence 3–7%; contra 3 Lobos nv3, mesmo no nível 4, vence 0%. Fugindo (`kite`), vence 100% em todas as matilhas de lobos. Por isso o teste usa `kite` para o Mago, como para o Arqueiro, que também ataca de longe.
- **Acampamento:** com 2 Lobos nv3 + Salteador nv4, o Mago vence só 43%, porque o salteador atira enquanto ele foge. Ficou 1 Lobo + 1 Salteador nv4 (100%).
- **Guerreiro:** contra 2 Salteadores nv3, vence 93% em 34 s, porque persegue quem foge. O Acampamento põe um lobo na frente.

## 2. Perfil esperado contra cada grupo típico (`FieldsDesignTests`)

O perfil esperado é o do nível do grupo: atributos sugeridos, equipamento comum do nível e NH focado. Meta: ≥ 85%.

| Grupo | Classe | Vitória | Tempo médio (s) | p90 (s) | Dano recebido | Mortes |
|---|---|---|---|---|---|---|
| Estrada: 2 Ratos nv1 | Guerreiro | 100% | 13,7 | 13,8 | 31 | 0 |
| | Arqueiro | 100% | 13,5 | 13,8 | 2 | 0 |
| | Mago | 100% | 13,7 | 13,8 | 4 | 0 |
| | Clérigo | 100% | 31,7 | 32,1 | 83 | 0 |
| Fazenda: 2 Ratos nv1 + Lobo nv2 | Guerreiro | 100% | 11,9 | 13,7 | 73 | 0 |
| | Arqueiro | 100% | 20,8 | 21,8 | 2 | 0 |
| | Mago | 100% | 20,6 | 22,8 | 6 | 0 |
| | Clérigo | 100% | 30,9 | 31,2 | 206 | 0 |
| Fazenda: 2 Lobos nv2 | Guerreiro | 100% | 9,7 | 10,1 | 79 | 0 |
| | Arqueiro | 100% | 18,4 | 19,1 | 5 | 0 |
| | Mago | 100% | 22,7 | 24,7 | 4 | 0 |
| | Clérigo | 100% | 30,9 | 31,2 | 297 | 0 |
| Clareira: 3 Lobos nv3 | Guerreiro | 100% | 19,8 | 20,3 | 197 | 0 |
| | Arqueiro | 100% | 33,8 | 35,8 | 8 | 0 |
| | Mago | 100% | 33,7 | 36,3 | 2 | 0 |
| | Clérigo | 100% | 60,1 | 61,6 | 884 | 0 |
| Acampamento: Lobo nv4 + Salteador nv4 | Guerreiro | 100% | 26,4 | 28,6 | 251 | 0 |
| | Arqueiro | 100% | 11,2 | 11,8 | 101 | 0 |
| | Mago | 100% | 14,9 | 15,7 | 169 | 0 |
| | Clérigo | 100% | 25,9 | 26,9 | 390 | 0 |

O Clérigo é o mais lento, como no v0.7: ele se cura e apanha muito. Contra a matilha da Clareira leva 60 s, mas vence sempre.

## 3. Ritmo solo: nível 1 → 4–5 em ~1 hora

Modelo (`FieldGroups.Pace`):

- No nível L, o jogador caça os grupos do nível min(L, 4).
- Cada grupo custa o tempo médio de vitória das 4 classes mais **60 s fora da luta**. São ~10 s para andar e pegar o saque e ~50 s para recuperar: nos Campos não há descanso, então é poção ou volta à Cidade a cada poucos grupos.
- O ritmo é limitado pelo renascimento: no máximo o que os grupos daquele nível devolvem.
- A XP é a regra da T-203: 6 × nível do monstro, com bônus ou penalidade pela diferença de nível.

| Nível | XP para o próximo | XP/min | Minutos no nível | Acumulado |
|---|---|---|---|---|
| 1 | 50 | 9,2 | 5,4 | 5 |
| 2 | 174 | 17,1 | 10,2 | 16 |
| 3 | 361 | 33,4 | 10,8 | 26 |
| 4 | 606 | 36,2 | 16,7 | 43 |
| 5 | 906 | 28,6 | 31,6 | 75 |

Com 1 hora, o jogador está no **nível 5**. O teste exige nível 4 em até 60 min e nível 6 só depois de 60 min.

O nível 5 rende menos XP por minuto que o 4 porque os Campos acabam no nível 4: os monstros ficam abaixo do jogador (−20%) e o próximo passo é a Floresta (nível 5). O renascimento só limita quem é mais rápido que o modelo: hoje, nenhum nível é limitado por ele.

## 4. Premissas e riscos

- **O intervalo de 60 s é premissa, não medida.** Com 40 s o nível 4 chega em 20 min; com 90 s, em 36 min. Medir no playtest da T-230 (tempo entre lutas, idas à Cidade) e ajustar renascimentos ou densidade.
- **Vários jogadores na mesma área** dividem os grupos. Com 2 jogadores caçando juntos, o nível 3 já fica perto do limite do renascimento. O teste é para um jogador sozinho, como pede a tarefa.
- Monstros não voltam ao ponto quando perdem o alvo (T-214). Um grupo puxado longe fica longe até morrer.
- Os encontros dos grupos rodam na arena aberta do protótipo, e não no mapa dos Campos: obstáculos e distâncias reais ficam para a T-230.
