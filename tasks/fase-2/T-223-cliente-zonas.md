# T-223 — Cliente: troca de zona, Cidade e Campos

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** T-214, T-220
- **Libera:** T-230
- **Áreas que pode alterar:** `client/`

## Contexto

O servidor da T-214 tem várias zonas e portões. O cliente precisa trocar de mapa sem voltar ao menu.

## Requisitos

1. Ao trocar de zona: tela curta de transição, mapa novo (arena da zona), câmera e interesse recomeçam.
2. Indicação visual dos portões e do nome da zona.
3. Morte no mundo aberto: tela com "Renascer na Cidade".
4. Mapas do Tiled (T-052) se existirem; senão os provisórios.

## Critérios de aceite

- ir da Cidade aos Campos e voltar online, morrer e renascer;
- `dotnet build client/` e `./scripts/check.sh` passam.

## Resultado

**Feito no servidor local; falta o teste online na VPS** (Windows, 2026-10-31). `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa. O critério "ir da Cidade aos Campos e voltar online, morrer e renascer" foi verificado contra o `Game.Server` local com `--world --dev-login`. Na VPS o Compose ainda roda a sala de encontro (`GAME_WORLD=false`, veja "Pendências").

**Troca de zona** (`Main.UpdateOnline`, a cada `Welcome` novo):
- arena da zona (mapa do Tiled se a arena tiver `source`; hoje `arena.city` e `arena.vigil_fields` são provisórios em ASCII e aparecem como grade, T-052);
- câmera com as bordas novas, e vai direto ao jogador (`CameraRig.SetBounds` zera a suavização);
- unidades, chão e alvo apagados (`WorldView.Clear`, `PlayerInput.ClearTarget`): os ids da zona nova não têm relação com os da antiga. O interesse recomeça no servidor, e o cliente já zerava o estado pela época nova;
- tela curta de transição: escuro com o nome da zona por 0,35 s, que clareia em 0,6 s (`Match/ZoneScreen.cs`).

**Indicações na tela**
- Nome da zona no canto superior direito, com "Zona segura" na Cidade ou a faixa de nível dos monstros nos Campos ("Monstros nível 1 a 4", calculada dos pontos de surgimento).
- Portões desenhados na arena (`ArenaView.ShowGates`): retângulo dourado translúcido e o texto "Portão: <zona de destino>".

**Morte no mundo aberto:** faixa no alto da tela com "Você morreu em <zona>." e o botão **Renascer na Cidade** (`NetAuthority.RequestRespawn` → `RespawnRequest`). O resumo da morte (HUD) continua no centro. A faixa some ao pedir, e o `Welcome` da Cidade faz a transição.

**Outros ajustes:** no mundo aberto, R não reinicia e a pausa não mostra "Reiniciar" nem a seed (reiniciar a cena gastaria o token de jogo, que é de uso único). A sala de encontro e o modo local continuam iguais.

**Argumento de depuração novo:** `--auto-respawn=segundos` (aperta "Renascer na Cidade" sozinho).

**Como foi verificado** (servidor local `--world --dev-login`, cliente com `--server`, capturas de tela):
- nascer na Cidade (portão e nome da zona aparecendo; transição capturada);
- `--auto-move=1,0` até o portão leste → Campos da Vigília (`[net] na sala zone.vigil_fields`, época 2), com o portão de volta desenhado;
- Mago parado nos Campos até morrer: faixa com o botão, sem cobrir o resumo da morte;
- com `--auto-respawn=6`: morreu → pediu → Cidade (época 3) → andou de novo até os Campos (época 4) → morreu → Cidade, duas voltas seguidas;
- encontro local (`--encounter=encounter.three_wolves`) sem nada de zona na tela.
- Não verificado: clicar o botão com o mouse (o argumento chama o mesmo método) e voltar dos Campos à Cidade pelo portão oeste a pé (o servidor testa isso na T-214).

**Pendências**
- **VPS: `GAME_WORLD=true` no `deploy/.env`** e reiniciar o game-server, para o servidor da VPS rodar o mundo aberto em vez da sala de encontro. Depois, o teste online de ponta a ponta pelo humano: entrar com conta, ir aos Campos, voltar, morrer e renascer.
- **T-055:** os mapas do Tiled da Cidade e dos Campos (o cliente já desenha quando a arena tiver `source`).
