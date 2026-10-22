# T-062 — Dados: todos os níveis e as habilidades que faltam

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** T-060, T-061
- **Libera:** T-063, T-064
- **Áreas que pode alterar:** `data/`, `tests/Game.Data.Tests/`, `tests/Game.Core.Tests/` (testes que leem os dados reais)

## Contexto

Passar para `data/*.json` as tabelas de `design/skills-v0.1.md`: os NH 2–5 das 16 habilidades normais e os NH 1–3 das 4 assinaturas, mais as habilidades do MVP que ainda não existem no jogo.

## Requisitos

1. `ranks` de todas as habilidades normais conforme as tabelas (valores e comportamentos).
2. Habilidades novas com todos os níveis: **Chuva de Flechas** (Arqueiro, área com telegráfo aliado) e as assinaturas **Muralha Viva**, **Flecha Devastadora**, **Cataclismo Arcano** e **Milagre**.
3. Listas das classes com as 5 habilidades do MVP (assinatura marcada como tal); a barra do protótipo continua mostrando só as habilidades com NH ≥ 1 do personagem.
4. Teste de conteúdo: para cada habilidade, o valor no "perfil nível 1" de cada NH bate com a coluna da tabela do documento (tolerância de arredondamento).

## Critérios de aceite

- todas as habilidades do MVP com todos os NH nos dados, validados;
- `./scripts/check.sh` completo passa (testes de design ainda rodam com NH1).

## Resultado

Feito no Windows (pedido do humano). Os dados foram gerados por script a partir das tabelas de `design/skills-v0.1.md` (valores do "perfil nível 1", arredondados por acerto como o jogo faz).

**O que entrou nos dados:**
- `ranks` (NH 2–5) das 16 habilidades normais, com os comportamentos da T-061: atordoar (Golpe Pesado), giros extras e Sangramento (Golpe Circular), Guarda Firme mais forte e com reflexo, Provocação com raio/duração e redução de dano, flechas extras (Disparo Potente), Marca no 2º alvo e com lentidão, Recuo com bônus no próximo Disparo e 2 cargas, explosões extras, Prisão de Gelo em área e com +20%, Barreira que devolve Mana e protege o aliado mais próximo, Campo de Relâmpagos mais longo e maior, Cura Direta no aliado mais ferido, saltos da Corrente, Escudo Sagrado que remove veneno e protege o mais ferido, Purificação com cura e imunidade.
- Habilidades novas com todos os NH: **Chuva de Flechas** (`archer.rain_of_arrows`, normal, telegráfo de 0,6 s e ondas a cada 0,5 s) e as assinaturas (`"signature": true`) **Muralha Viva** (`warrior.living_wall`), **Flecha Devastadora** (`archer.devastating_arrow`), **Cataclismo Arcano** (`mage.arcane_cataclysm`) e **Milagre** (`cleric.miracle`).
- Status novos (status não tem NH, então cada variação é um status): `status.stunned`, `status.frozen_deep`, `status.hunters_mark_strong`, `status.hunters_mark_slowing`, `status.firm_guard_strong`, `status.firm_guard_reflect`, `status.taunt_resolve`, `status.retreat_focus`, `status.rain_slow`, `status.purified`. Áreas novas: `area.lightning_field_2..5`, `area.rain_slow`, `area.cataclysm_fire`.
- Classes com as 5 habilidades do MVP (4 normais + a assinatura por último). A Armadilha de Contenção continua nos dados, fora da barra (pós-MVP).

**Decisões (o documento não dizia):**
- Raio do "inimigo mais próximo do alvo" da Marca: **5 tiles**. Raio do "aliado mais próximo/mais ferido" (Barreira, Cura Direta, Escudo Sagrado): **8 tiles**.
- Escudo Sagrado NH5: o aliado mais ferido recebe o escudo **inteiro** (o documento só fala em 50% na Barreira e na Cura Direta).
- Flecha Devastadora NH1 ("acerta o primeiro inimigo da linha"): o Core não tem "primeiro da linha", então o NH1 é **alvo único** (alcance 12); do NH2 em diante é **linha** de 12 × 1,2 que atravessa todos. Canalização = tempo de conjuração (mover cancela, como toda conjuração).
- Chão em chamas do Cataclismo: 22 por segundo (10% de 221) por 4 s, raio 6. Lentidão da Chuva NH5: área de raio 4 por 2 s, nascendo só na 1ª onda (`onHit: 1`).
- Muralha Viva NH3 remove status com as tags `stun`, `root` e `freeze` (atordoamento e enraizamento, incluindo congelado).
- Milagre NH3 levanta 1 aliado caído a até 8 tiles do Clérigo.

**Desvio:** o requisito 3 (barra só com NH ≥ 1) pediu uma mudança mínima no Core: `EntityFactory.SpawnPlayer` só põe a **assinatura** nas habilidades conhecidas quando o encontro informa um NH para ela (assinatura começa no NH0). Normais continuam no NH1 por padrão. O cliente e os bots já usam as habilidades conhecidas, então nada mais mudou.

**Testes:** `tests/Game.Data.Tests/SkillRankTableTests.cs` compara acertos e o valor do perfil nível 1 de cada NH com a tabela do documento (tolerância de arredondamento; alvos extras e o chão em chamas não entram, como no documento), mais a Marca e o número de NH. `tests/Game.Core.Tests/RealRankContentTests.cs` usa os dados reais: assinatura fora da barra no NH0 e cada habilidade de cada classe executada em cada NH. Lista das classes atualizada em `PrototypeContentTests`. `./scripts/check.sh` completo passa (23 testes de design com NH1) e o cliente compila.

**Atenção para a T-063:** o Arqueiro agora tem a Chuva de Flechas no NH1 na barra (é habilidade normal), e os bots passam a poder usá-la. Os testes de design continuam passando, mas o balanceamento por NH ainda não foi medido.
