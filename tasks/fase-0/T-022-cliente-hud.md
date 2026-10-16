# T-022 — Cliente: HUD e log de combate

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** T-010, T-013, T-021
- **Libera:** T-025
- **Áreas que pode alterar:** `client/`

## Contexto

O HUD só lê estado e eventos do `IGameAuthority`. Clareza acima de enfeite: `design/combat-encounters-v0.1.md` pede que o combate seja compreensível sem excesso de efeitos.

## Requisitos

1. **Quadro do jogador**: vida (com sobreposição de escudo), recurso com cor por tipo (Vigor, Foco, Mana, Fé).
2. **Barra de habilidades**: ícone/sigla, tecla, custo, varredura de recarga, estado indisponível (sem recurso, fora de alcance).
3. **Barra de conjuração** do jogador.
4. **Quadro do alvo**: nome, vida, status ativos, barra de conjuração do alvo (essencial para interrupções).
5. **Quadros de grupo** para o encontro de grupo (vida, recurso, status negativos removíveis destacados).
6. **Números flutuantes** de dano e cura (cores por escola; absorvido indicado).
7. **Log de combate** (T-013) em painel recolhível, com filtro por categoria.
8. **Resumo de morte** ao morrer: dano por fonte e últimas entradas.

## Critérios de aceite

- Todos os elementos funcionam nos cinco encontros;
- em 30 segundos de combate, um jogador consegue responder "por que perdi vida?" olhando o HUD/log.

## Resultado

Concluída em 2026-10-16 no Windows (Godot 4.7.2 .NET).

**Branch:** `task/T-022-cliente-hud` saiu de `task/T-021-cliente-input-camera` (a T-021 estava sendo integrada em paralelo). Integrar a T-021 antes; o merge desta continua limpo.

**Feito** (tudo em `client/`, o HUD só lê snapshots e eventos):
- `scripts/Hud/Hud.cs`: raiz do HUD (nó `Hud/HudRoot` na cena), posicionada pelo tamanho da tela a cada frame.
  1. **Quadro do jogador**: nome, vida com sobreposição de escudo (`240/240 (+45)`), recurso com cor por tipo (Vigor laranja, Foco amarelo, Mana azul, Fé dourada), status ativos com tempo restante.
  2. **Barra de habilidades** (`AbilitySlot`): ataque básico (M1), 1–4, poção (Q); sigla, tecla, custo, cargas, varredura de recarga com segundos, tom azul sem recurso, tom vermelho fora de alcance (alvo único contra o alvo selecionado; área em ponto contra a mira), borda dourada durante a conjuração; tooltip com nome, custo, recarga e conjuração.
  3. **Barra de conjuração** do jogador acima da barra de habilidades.
  4. **Quadro do alvo** (alvo selecionado da T-021): nome com "(Elite)/(Chefe)", vida, status, barra de conjuração do alvo em vermelho ("Conjurando: X 0,8 s").
  5. **Quadros de grupo**: demais jogadores (vida + escudo, recurso, status). Status com tags que alguma habilidade de classe remove (`remove_status` nos dados — hoje `poison`, `curse`) aparecem destacados em verde com ◆.
  6. **Números flutuantes** (`WorldView`): dano `-23` na cor da escola, `(8 absorvido)` / `Absorvido 12` quando o escudo segura, cura `+15` em verde.
  7. **Log de combate** (`CombatLogPanel`): linhas da T-013 com tempo e cor por categoria, botão para recolher, filtro por categoria (Dano, Cura, Status, Hab., Rec., Fase, Morte); acrescenta só as linhas novas.
  8. **Resumo de morte** (`DeathSummaryPanel`): ao morrer, dano por fonte nos últimos 5 s (total, %, golpes, absorvido; fontes de mesmo nome levam o número da entidade) e as últimas entradas.
- `Authority/ICombatLogSource.cs` + `LocalAuthority`: o `CombatLogRecorder` precisa do `World`, que a interface de autoridade não expõe; a autoridade local grava o log a cada tick e o oferece por esta interface do cliente. O HUD funciona sem log se a autoridade não a implementar (ex.: `FakeAuthority`). Na fase 1 o cliente de rede implementa com linhas do servidor.
- Clique sobre um controle do HUD (filtros, Fechar) não vira ataque (`Hud.IsPointerOverUi` → `PlayerInput`).
- Janela padrão 1280×720 (em 1152×648 a barra de habilidades encostava no log); linha de ajuda/FPS foi para o canto inferior esquerdo.
- `--capture-after=segundos` para capturas mais longas.

**Verificação:** capturas nos cinco encontros (Três Lobos, Lobo + Salteador, Quatro Ratos, Guarda Esquelético, Matriarca) com `--demo-input`/`--auto-move`: todos os elementos aparecem e atualizam; escudo do Mago, veneno destacado na Matriarca, quadros de grupo no encontro de grupo, conjuração de alvo, recargas e resumo de morte do Guerreiro ("Lobo Cinzento #2 — Mordida do Lobo: 21 (33%) — 3 golpes" + últimas entradas). `dotnet build client/` sem avisos; `./scripts/check.sh` OK (332 testes); `--fake-authority` continua rodando.

**Pendências / observações:**
- Filtros e recolher do log não foram clicados de fato (só verificados por código e captura); conferir no playtest.
- Algumas linhas do log da T-013 mostram ids crus (`area.venom_pool surgiu`) e o dano periódico aparece como "Matriarca causou dano" — formatação do Core, fora desta tarefa.
- Conjuração interrompida (`CastInterrupted`) aparece só no log, sem aviso visual próprio (T-023).
