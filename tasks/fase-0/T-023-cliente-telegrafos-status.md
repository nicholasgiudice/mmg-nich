# T-023 — Cliente: telegráfos, áreas e status

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** T-012, T-021
- **Libera:** T-025
- **Áreas que pode alterar:** `client/`

## Requisitos

1. **Telegráfos**: desenhar círculo, cone e linha a partir de `TelegraphStarted`, com preenchimento progressivo até o tick de resolução; sumir em `TelegraphResolved`.
2. **Áreas persistentes**: poça de veneno, teia, armadilha — cor e forma distintas, legíveis no chão, com fade ao expirar.
3. **Status** sobre entidades: ícones pequenos (siglas/cores no protótipo); efeitos fortes (atordoado, preso, congelado) com indicação visual no corpo.
4. **Feedback de impacto**: piscar ao receber dano, rastro em dash/investida, aviso de mudança de fase do boss.
5. Paleta e intensidade discretas: nada que esconda telegráfos.

## Critérios de aceite

- Na Matriarca, é possível ver e evitar todos os ataques telegrafados;
- status de controle são visíveis sem abrir o log.

## Resultado

Concluída em 2026-10-16 no Windows (Godot 4.7.2 .NET).

**Branch:** `task/T-023-cliente-telegrafos-status` saiu de `task/T-022-cliente-hud` (a T-022 ainda não estava em `main`). Integrar T-022 antes desta.

**Feito** (tudo em `client/`):
1. **Telegráfos** (`Presentation/TelegraphView.cs`): desenhados a partir das entidades com `TelegraphState` no snapshot (origem e direção no `Transform`), em vez de só reagir a `TelegraphStarted`/`TelegraphResolved` — o resultado é o mesmo (nasce no início, some na resolução), e funciona também se o cliente entrar no meio. Contorno da área inteira desde o início; preenchimento cresce até a borda no tick de resolução (círculo pelo raio, cone pelo alcance, linha pelo comprimento). Investida (`charge`) tem seta na ponta. Laranja-avermelhado se pode atingir o jogador, azul se é aliado.
2. **Áreas persistentes** (`AreaView.cs`): poça de veneno (verde com bolhas), teia (branca com fios), armadilha (dourada com marcas); contorno tracejado até armar; some aos poucos no último meio segundo. Áreas desconhecidas: roxo (hostil) / azul (aliada).
3. **Status sobre as unidades** (`StatusVisuals.cs` + `EntityView`): até 5 ícones (2 letras do nome; amarelo controle, verde veneno, laranja marca, verde-azulado buff, vermelho debuff). No corpo: **congelado** = halo azul-gelo; **preso/enraizado** = espinhos marrons em volta; **atordoado** = estrelas girando sobre a cabeça; **silenciado** = símbolo roxo. Flags (`stunned`, `rooted`, `silenced`) e tag `freeze` vêm dos dados.
4. **Impacto**: unidade pisca em branco ao perder vida; rastro em `Displaced` (dash/investida azul-claro, empurrão laranja); aviso grande "Matriarca da Floresta — Fase N" no HUD em `PhaseChanged` (não na fase inicial).
5. **Paleta discreta**: preenchimentos com 10–40% de opacidade; ordem de desenho (`ZLayers`): arena < áreas < telegráfos < rastros < unidades mortas < unidades — telegráfos nunca ficam escondidos por áreas.

**Verificação** (capturas): na Matriarca, Investida (linha com seta) a 0,8 s, Poça de Veneno (círculo preenchendo) a 1,8 s e a área de veneno a 2,6 s; Prisão de Gelo do Mago mostra o rato congelado (halo + "Co"); Marca do Caçador ("Ma") e Armadilha armada no Arqueiro; `--fake-authority` continua rodando. `dotnet build client/` sem avisos; `./scripts/check.sh` OK (332 testes).

**Não verificado:** o aviso de mudança de fase (exige baixar a vida da Matriarca a 70%, inviável sem bots no cliente) e o visual de "preso" e "atordoado" (nenhum dado atual aplica atordoamento; a armadilha não chegou a prender nas capturas). Conferir no playtest da T-025.

**Outros:** o `Main.tscn` foi regravado pelo editor do Godot no formato canônico (uids e `unique_id`); sem mudança de comportamento. Fases não têm nome nos dados, por isso o aviso usa o número.
