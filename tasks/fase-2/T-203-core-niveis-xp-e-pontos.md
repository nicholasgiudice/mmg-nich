# T-203 — Core: níveis, XP, pontos de atributo e de habilidade

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-201
- **Libera:** T-205, T-206, T-212, T-221
- **Áreas que pode alterar:** `src/Game.Core/`, `src/Game.Data/`, `src/Game.Protocol/`, `data/rules.json`, `data/monsters.json` (nível e categoria), `data/schemas/`, testes correspondentes

## Contexto

`design/progression-v0.1.md` §2, §3 e §7: nível máximo 15, XP por monstro com modificador de diferença de nível e divisão em grupo, 5 pontos de atributo livres por nível, 1 ponto de habilidade por nível, requisito de nível por NH (`2n − 1`; assinatura 12/13/15). Os NH já existem no Core (T-060); falta ligá-los a pontos ganhos.

## Requisitos

1. **Nível e XP** no personagem (componente replicável, com XP para o próximo nível): curva `round(50 × L^1,8)`; nível máximo 15. Monstros ganham `level` e `category` (normal/elite/chefe) nos dados.
2. **XP ao matar**: `6 × nível do monstro × multiplicador da categoria` (1/5/30) × modificador de diferença (+10% por nível acima, máx. +30%; −20% por nível abaixo; 0 com 5 ou mais abaixo). Dividida entre quem participou (causou dano ou curou/protegeu quem lutou nos últimos N s) com +10% por membro além do primeiro. Eventos `XpGained` e `LevelUp`.
3. **Pontos**: ao subir de nível, +5 pontos de atributo livres e +1 ponto de habilidade. Intenções novas: `SpendAttributePoint(atributo)` e `RaiseSkillRank(habilidade)`, validadas no Core (pontos disponíveis, requisito de nível por NH, máximo da habilidade, assinatura a partir do 12). A assinatura entra na barra no NH1, como hoje.
4. **Derivados por nível**: vida máxima com o crescimento por nível da classe (§4); nível do atacante na fórmula de defesa (T-201).
5. **Redistribuição**: intenção que devolve todos os pontos (atributos e habilidades). Na fase 2 é **grátis** e só é permitida fora de combate (economia está fora de escopo; o custo em moedas da §10 fica para depois). Registrar no Resultado.
6. Protocolo: componentes e intenções novos → `ProtocolVersion` + 1.

## Critérios de aceite

- testes do Core: curva de XP, modificador de diferença, divisão em grupo, subir de nível, gastar pontos, requisitos de NH, redistribuição;
- um personagem nível 1 sem pontos gastos continua igual ao de hoje (testes de design passam);
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
