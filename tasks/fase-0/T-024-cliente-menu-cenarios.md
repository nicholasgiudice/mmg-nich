# T-024 — Cliente: menu de cenários

- **Ambiente:** Windows
- **Modelo:** Leve
- **Depende de:** T-014, T-015, T-021
- **Libera:** T-025
- **Áreas que pode alterar:** `client/`

## Requisitos

1. Tela inicial listando os cinco encontros de `data/` (nome + classe sugerida).
2. Seleção da classe do jogador. No encontro de grupo, o jogador escolhe uma classe e as outras três são controladas por bots de `Game.Bots` (estratégia `rotation` de grupo).
3. Seleção de seed (opcional, padrão aleatório) para reproduzir partidas.
4. Durante o jogo: `R` reinicia, `Esc` abre pausa (continuar, reiniciar, voltar ao menu).
5. Tela de resultado: vitória/derrota, tempo, dano causado/recebido, curas, mortes.
6. Referenciar `Game.Bots` no `.csproj` do cliente.

## Critérios de aceite

- Os cinco encontros podem ser jogados em sequência sem reiniciar o executável;
- grupo com 3 bots completa a Matriarca de vez em quando sem o jogador fazer nada além de sobreviver (bots funcionais).

## Resultado

_(preencher ao concluir)_
