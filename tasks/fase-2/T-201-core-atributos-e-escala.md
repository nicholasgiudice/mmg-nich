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

_(preencher ao concluir)_
