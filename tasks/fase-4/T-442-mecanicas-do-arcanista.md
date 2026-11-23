# T-442 — Core: mecânicas novas do Arcanista

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-421 (regeneração de Mana vinda de um estado), T-441
- **Libera:** T-443
- **Áreas que pode alterar:** `src/Game.Core/` (habilidades, conjuração, efeitos, estados, movimento), `src/Game.Data/` (carga e validação dos campos novos), `data/schemas/`, `tests/Game.Core.Tests/`, `tests/Game.Data.Tests/`; `src/Game.Protocol/` só se os orbes ou a canalização precisarem ir para o cliente
- **Branch:** sai da `main` com a T-421 e a T-441 integradas. É integrada na `main` (seção 6i de `tasks/README.md`).

## Contexto

Design em `design/segunda-classe-v0.1.md`, seção 7 (Arcanista e a tabela "Mecânicas do Mago"). Aprovado pelo designer em 2026-11-22, com uma mudança: o **Sifão Arcano aumenta a regeneração de Mana** por 5 s em vez de devolver Mana na hora. Esta tarefa cria **as mecânicas** no Core, cada uma com teste e com dados de teste. As habilidades de verdade vêm na T-443.

O Arcanista tem Raio Arcano, Marca Arcana, Orbes Arcanos, Sifão Arcano e Feixe do Vazio (assinatura). Ele rende mais quanto mais tempo fica no mesmo alvo. A Marca Arcana cabe no que existe (pilhas com dano periódico).

O que já existe e pode ser reaproveitado:

- conjuração (`castTime`, cancelada ao se mover, `CastControl`) e habilidades com vários acertos (`hits.count`, `hits.interval`);
- regeneração de Mana vinda de um estado (T-421, Inspiração);
- mais dano recebido num estado (Marca do Caçador);
- invocações (`summon`, `maxAlive`), hoje usadas por monstros.

## Requisitos

1. **Canalização com pulsos:** a habilidade aplica os efeitos em pulsos (a cada 0,5 s) **enquanto** quem lança canaliza. Andar, ser atordoado ou silenciado corta a canalização, e os pulsos que faltam não saem. Se `hits` com conjuração já faz isso, ajustar só o que falta.
2. **Dano que sobe a cada pulso** (Raio Arcano): cada pulso causa uma fração a mais que o anterior (+15%, acumulando: pulso n = base × 1,15^(n−1)).
3. **Andar devagar canalizando** (Raio Arcano NH5, Feixe do Vazio): a habilidade pode deixar andar a 50% da velocidade sem cortar a canalização.
4. **Efeito ao terminar a canalização** (Feixe do Vazio NH3): se a canalização vai até o fim, um efeito final no alvo (+15% de dano recebido de todos por 6 s). Cortada no meio, não sai.
5. **Orbes que atiram sozinhos no alvo atual** (Orbes Arcanos): por 10 s, de 2 a 4 orbes atiram no alvo atual do Arcanista a cada 1,5 s. Definir no Core o que é o "alvo atual" (sugestão: o último alvo de unidade que o Arcanista escolheu, se ainda vivo e no alcance; sem alvo, os orbes não atiram). Sugestão de forma: um estado no Arcanista com efeito periódico no alvo atual, um disparo por orbe, sem entidades novas. Se preferir invocações, registrar por quê. Os disparos são dano do Arcanista (crítico, log e ameaça dele).
6. **Sifão Arcano:** confirmar com teste que a regeneração vinda de um estado (T-421) serve no próprio lançador (+6 Mana/s por 5 s).
7. Validação dos campos novos e schema.

Se o cliente precisar saber dos orbes (quantos e onde desenhar) ou da canalização em andamento além do que já vai no snapshot, subir `ProtocolVersion` a partir do número da `main` e registrar no Resultado.

## Critérios de aceite

Testes no Core:

- canalização de teste de 6 pulsos: parado, saem 6; andar depois do 3º corta e saem 3; atordoar corta;
- com +15% por pulso e base 100, os pulsos causam 100, 115, 132, 152… (arredondamento registrado);
- com "andar devagar", andar não corta e a velocidade cai à metade enquanto canaliza; volta ao normal ao acabar;
- Feixe de teste até o fim deixa o alvo com +15% de dano recebido por 6 s; cortado no meio, não deixa;
- Orbes de teste com 3 orbes atiram 3 vezes a cada 1,5 s no alvo atual; trocar de alvo muda o alvo dos orbes; sem alvo, não atiram;
- Sifão de teste dá +6 de Mana por segundo ao próprio Arcanista por 5 s;
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
