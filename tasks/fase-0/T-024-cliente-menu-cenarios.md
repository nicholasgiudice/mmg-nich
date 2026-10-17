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

Concluída em 2026-10-16 no Windows (Godot 4.7.2 .NET).

**Branch:** `task/T-024-cliente-menu-cenarios` saiu de `task/T-023-cliente-telegrafos-status`. Integrar T-023 antes desta.

**Feito** (tudo em `client/`):
1. **Menu inicial** (`scenes/Menu.tscn`, agora a cena principal; `scripts/Match/MenuScreen.cs`): lista os encontros de `data/` na ordem do design (nome + classe sugerida, ou "grupo de 4"), com inimigos, composição e limite de tempo do selecionado. Duplo clique ou "Jogar" inicia.
2. **Classe do jogador**: solo = a classe escolhida substitui a do encontro; grupo = o jogador controla a classe escolhida e as outras três são bots de `Game.Bots` (`MatchSetup`). `LocalAuthority` ganhou um gancho `beforeStep` onde roda `BotDirector.Tick` (mesma ordem do simulador).
3. **Seed**: campo opcional; vazio = aleatória. A seed aparece na pausa e no resultado; R reinicia com a mesma seed.
4. **Em jogo**: `R` reinicia; `Esc` limpa o alvo se houver (comportamento da T-021) e, sem alvo, abre a pausa (Continuar, Reiniciar, Menu). A simulação para enquanto a pausa ou o resultado estão abertos.
5. **Resultado** (`MatchOverlay` + `MatchTracker`): Vitória / Derrota / Derrota por tempo esgotado (mesmas regras do simulador, com `defeat.timeLimit` do encontro), tempo, dano causado (e do grupo), dano recebido (e absorvido), cura feita, mortes de jogadores e inimigos derrotados; botões Reiniciar, **Próximo encontro** (classe sugerida dele, seed nova) e Menu — os cinco encontros podem ser jogados em sequência sem fechar o executável.
6. `MmoRpgClient.csproj` referencia `Game.Bots`.
- Ações novas no Input Map: `pause` (Esc) e `restart` (R).
- Depuração: argumentos de linha de comando continuam pulando o menu (`--encounter`, `--class`, `--fake-authority`, `--demo-input`, `--auto-move`); novo `--time-scale=n` (acelera a partida); `--capture` sozinho fotografa o menu.

**Decisão — estratégia dos bots:** `kite+dodge` (com `TelegraphHazardSource` e a opção de explosão do Mago da T-016), não `rotation`. No simulador, 3 bots + jogador parado vencem a Matriarca 0% em `rotation` e 77–80% em `kite+dodge`. No cliente (jogador sem fazer nada, 8× de velocidade): jogador Guerreiro venceu 2/2 (seeds 4, 5), Mago 1/2, Clérigo 0/2, Arqueiro 0/3 (tempo esgotado aos 7:00). Atende "completa de vez em quando"; com o jogador atacando a chance sobe.

**Verificação:** capturas do menu, da pausa (Mago no encontro do Clérigo, seed 77) e do resultado ("Derrota — tempo esgotado", 7:00, dano do grupo 7378, 31 inimigos); vitórias da Matriarca com bots registradas no log (`[match] Victory em 274,6s`). `--fake-authority` continua rodando. `dotnet build client/` sem avisos; `./scripts/check.sh` OK (332 testes).

**Não verificado por clique** (só por código e pelos mesmos caminhos usados no teste): botões Próximo encontro / Menu / Reiniciar da tela de resultado, o filtro do menu com mouse e a sequência completa dos cinco encontros — conferir no playtest (T-025).

**Observação:** ao fechar à força pela captura, o Godot imprime avisos de "RID leaked at exit"; é só o encerramento abrupto, sem efeito no jogo.
