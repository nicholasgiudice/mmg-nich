# T-503 — Servidor e protocolo: anúncio de chefe de campo

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** —
- **Libera:** T-530, T-540
- **Áreas que pode alterar:** `src/Game.Data/TiledMapReader.cs`, `src/Game.Core/Definitions/Zones.cs`, `src/Game.Server/Zones/`, `src/Game.Protocol/`, `design/map-conventions-v0.1.md` (§5), `tests/`

## Contexto

A região do sul tem **chefes de campo**: monstros fortes no mundo aberto, com renascimento longo. O primeiro é a Bruxa do Brejo (nível 20, renasce em 10 min, T-510), depois o Chefe de Guerra do Planalto (fase 6). O tempo longo já existe (`respawn` do `monster_spawn`, em segundos). O que falta é o jogador **saber** que o chefe nasceu, sem ficar parado na frente do ponto.

## Requisitos

1. Propriedade nova `announce` (bool, padrão `false`) no `monster_spawn` (`design/map-conventions-v0.1.md` §5).
2. Quando um monstro de um ponto com `announce` nasce, e quando morre, o servidor manda um evento para **todos os jogadores da zona** (não só os da área de interesse), com o id do monstro, o nome da subárea e se nasceu ou morreu.
3. Só são anunciados os nascimentos e as mortes que acontecem com o jogador na zona. Quem entra depois não recebe o estado atual do chefe: vê o chefe se chegar perto. Registre se mudar.
4. Evento novo no protocolo: tag nova e `ProtocolVersion` + 1 a partir do número da `main` (19 em 2026-11-23). O cliente mostra o aviso na T-540. Protocolo novo pede cliente novo: siga os "Cuidados de integração" da fase 5 em `tasks/README.md` antes de integrar na `main`.

## Critérios de aceite

- teste de servidor: dois jogadores em pontos distantes da mesma zona recebem o evento de nascimento e de morte; um jogador em outra zona não recebe;
- teste de carga: `announce` com valor que não é bool dá erro;
- teste de protocolo: ida e volta do evento novo;
- `./scripts/check.sh` completo passa.

## Resultado

_(a preencher)_
