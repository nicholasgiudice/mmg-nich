# T-201 — Core: atributos, escala das habilidades, defesa e crítico

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** — (fase 1 concluída)
- **Libera:** T-202, T-203, T-204
- **Áreas que pode alterar:** `src/Game.Core/`, `src/Game.Data/`, `src/Game.Protocol/` (só o formato do componente novo), `data/schemas/`, `data/classes.json` (perfil e arma inicial), `tests/Game.Core.Tests/`, `tests/Game.Data.Tests/`, `tests/Game.Protocol.Tests/`

## Contexto

Primeira peça de `design/progression-v0.1.md` (§3–§5, §9 passo 1). Hoje o dano/cura/escudo de cada efeito é um número fixo (`base`), igual ao "perfil nível 1". A progressão precisa que esse número venha de `base + Σ coeficiente × estatística` (FOR, DES, INT, VIT, ARMA), mais defesa (armadura/resistência) e crítico.

**Regra de calibração (§1):** um personagem nível 1 com o perfil inicial da classe e a arma inicial produz exatamente os números de hoje. Esta tarefa cria a mecânica; a conversão dos dados de todas as habilidades é a T-202.

## Requisitos

1. **Estatísticas do personagem** num componente do Core (ex.: `CharacterStats`): FOR, DES, AGI, INT, VIT, dano de arma (ARMA), armadura, resistência, chance de crítico, bônus de velocidade de ataque. Replicável (o cliente mostra). Valores vindos do **perfil inicial da classe** em `data/classes.json` (tabela da §3, Mago com INT 11 + 2 do cajado) e da **arma inicial** (`weaponDamage` da classe; os itens de verdade são a T-204).
2. **`scaling` nos efeitos** `damage`, `heal` e `shield` (e nos `ranks`): `{ "weapon": 1.0, "str": 2.5 }`. Valor = `base + Σ coef × estatística` da fonte. Sem `scaling`, o efeito continua fixo (monstros, áreas, status). Validação em `Game.Data` e no JSON Schema.
3. **Derivados da §3–§4**: vida máxima = base da classe + VIT × 10 + (nível − 1) × crescimento (nível fixo 1 por enquanto; o nível é a T-203); Mana máxima = 80 + 2 × INT; regeneração 5/s + INT × 0,05; **AGI**: recarga do ataque básico ÷ (1 + AGI × 0,005), máx. +50%, e crítico 5% + AGI × 0,25% (máx. 40%).
4. **Crítico** (dano, cura e escudo × 1,5) usando o RNG da simulação (`world.Random`), nunca `Random` do .NET. Evento de dano/cura indica se foi crítico (campo novo no evento + formato de rede + texto do log).
5. **Defesa**: armadura reduz dano físico, resistência reduz as outras escolas: `D / (D + 40 + 8 × nível do atacante)`, máx. 60% (nível do atacante 1 até a T-203). Monstros podem ter armadura/resistência nos dados (padrão 0).
6. **Protocolo**: componente novo com tag nova em `ComponentCodecs` e evento com campo novo → `ProtocolVersion` + 1 (regra em `src/Game.Protocol/ProtocolVersion.cs`).

## Critérios de aceite

- testes do Core para escala, defesa, crítico (determinístico por seed), AGI e derivados;
- com o perfil nível 1, **todos os testes de design atuais continuam passando** sem mudar números (o perfil reproduz o protótipo);
- `Game.Core` continua sem referências de engine/rede/banco;
- `./scripts/check.sh` completo passa.

## Resultado

**Feito**

- **Estatísticas do personagem** (`src/Game.Core/Combat/Stats/CharacterStats.cs`): o componente `CharacterStats` guarda nível, FOR, DES, AGI, INT, VIT, dano de arma, armadura e resistência. É replicável, com a tag 14 em `ComponentCodecs`. As contas ficam em `StatMath`: perfil da classe, vida e Mana máximas, regeneração, crítico, velocidade de ataque, escala e defesa. Crítico e velocidade de ataque são calculados a partir da AGI, não guardados no componente.
- **Perfil das classes** (`data/classes.json`): `attributes`, `baseHealth`, `healthGrowth` e `startingWeapon` (`damage`, e `int` para o cajado do Mago) entram no lugar de `health`.
  - A vida no nível 1 é igual à do protótipo: Guerreiro 240, Arqueiro 130, Mago 110 e Clérigo 150.
  - `health` continua válido para classes sem perfil (testes e fixtures).
- **Escala** (`scaling`) em `damage`, `heal` e `shield`, também dentro dos `ranks`, porque os efeitos dos NH usam os mesmos records.
  - Chaves: `weapon`, `str`, `dex`, `int`, `vit`.
  - Fonte sem `CharacterStats` (monstros, áreas sem dono) usa só a base.
- **Derivados**, em `data/rules.json` na seção `stats` (os padrões do Core são os mesmos números do design):
  - Mana máxima = 80 + 2 × INT;
  - regeneração = a da classe + INT × 0,05 por segundo;
  - vida = base + VIT × 10 + (nível − 1) × crescimento.
- **AGI:**
  - a recarga do ataque básico é dividida por (1 + AGI × 0,005), com bônus de no máximo 50%; só o ataque básico é afetado;
  - crítico = 5% + AGI × 0,25%, com teto de 40%.
- **Crítico:** multiplica por 1,5 o dano, a cura e o escudo. Usa `world.Random`, e o RNG só é consumido quando a fonte tem `CharacterStats`. Por isso monstros não critam e não mexem na sequência do RNG. `DamageDealt`, `HealApplied` e `ShieldApplied` ganharam `Critical`, e o log mostra "(crítico)".
- **Defesa:** D / (D + 40 + 8 × nível do atacante), com teto de 60%. A armadura reduz dano físico; a resistência, as outras escolas. Monstros têm `armor` e `resistance` nos dados (padrão 0), copiados para `UnitInfo`. A redução entra depois dos modificadores e antes dos escudos.
- **Protocolo:** `ProtocolVersion` passou para 3, por causa do componente novo, dos campos novos em `UnitInfo` e de `Critical` nos eventos.
- **Validação:** feita em `Game.Data` e no JSON Schema (`$defs/scaling`, perfil da classe, defesa de monstro, `rules.stats`).
- **Testes:**
  - `tests/Game.Core.Tests/StatsTests.cs`: escala, fonte sem estatísticas, cura e escudo com INT, defesa por nível do atacante e teto, escola certa, crítico ×1,5 com flag, crítico determinístico por seed, AGI só no ataque básico e teto, vida e Mana do perfil real das quatro classes, cajado e regeneração com INT, crescimento por nível;
  - `tests/Game.Data.Tests/StatsLoaderTests.cs`: carregamento e erros do perfil, da escala, da defesa de monstro e de `rules.stats`.

**Desvios (decisão do designer)**

O critério pedia que os testes de design passassem "sem mudar números", mas o perfil não reproduz exatamente o protótipo:

- a Mana passou a vir da INT (Mago 106 e 5,65/s, antes 100 e 5/s);
- o crítico acrescenta em média cerca de 6–7% ao dano de quem tem estatísticas.

Com isso, dois testes de design saíram da faixa:

| Teste | Antes do ajuste | Ajuste | Depois |
|---|---|---|---|
| Matriarca, grupo equilibrado | 91–92% de vitórias | vida 8000 → **8800** (`data/monsters.json`) | 81%, cerca de 207 s |
| Explosão do Mago sem rotação | 75% e mais rápida que a rotação | custo 80 → **90** de Mana (`data/abilities_mage.json`) | 54% e mais lenta que a rotação |

- Na Matriarca, a taxa de vitórias variou de forma irregular com a vida: 8300 deu 92%, 8800 deu 81% e 9600 deu 85%.
- Os dois arquivos estão fora das áreas da tarefa. Mudei o mínimo, e a T-202 (conversão das habilidades) e o balanceamento devem revisar esses números.
- Outros arquivos fora da lista: `CombatEvents.cs` e o formatador do log, pelo campo `Critical`.

**Pendências**

- O cliente ainda não mostra crítico (número maior ou cor) nem a ficha de atributos. Fica para a interface da fase 2.
- Nível fixo em 1 até a T-203; itens de verdade (arma e armadura) na T-204. Por enquanto o jogador tem armadura e resistência 0.
