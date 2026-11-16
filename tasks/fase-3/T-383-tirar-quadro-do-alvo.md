# T-383 — Tirar a vida do alvo do topo da tela

- **Ambiente:** Windows
- **Modelo:** Leve
- **Depende de:** —
- **Libera:** —
- **Áreas que pode alterar:** `client/scripts/Hud/` (`Hud`, `UnitFrame`), `client/scripts/Presentation/EntityView.cs` (só se for preciso mostrar a conjuração no monstro), `tasks/reports/interface/`

## Contexto

Pedido do designer durante o playtest da fase 3 (T-380), em 2026-11-16. Ao atacar um monstro, o quadro do alvo (`Hud._target`, um `UnitFrame` com vida, estados e conjuração) aparece no topo da tela. Num mapa com chefe, ficam no topo a vida do chefe (`BossPanel`) **e** a do monstro atacado, o que polui a tela. O designer pediu: tirar a vida do monstro que você está atacando.

## Requisitos

1. O quadro do alvo **sai do topo da tela**. A vida do monstro continua visível na barra sobre a cabeça dele (`EntityView`), e o alvo continua marcado no mapa e na lista de batalha.
2. O painel do chefe (`BossPanel`) fica como está.
3. O quadro do alvo também mostrava a **conjuração** do monstro (útil para interromper). Confira se ela já aparece sobre o monstro. Se não aparecer, mostre só uma barra fina de conjuração sobre o monstro selecionado, enquanto ele conjura.
4. Se, ao ver a tela, o designer preferir esconder o quadro só quando o painel do chefe estiver aberto, faça assim e registre.

## Critérios de aceite

- capturas no jogo: atacando um monstro nos Campos e atacando um monstro na Cripta com o Vigia ativo; nenhuma das duas mostra a vida do alvo no topo;
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Resultado

**Feito.** As capturas estão em `tasks/reports/interface/alvo-fora-do-topo/`.

**O que mudou:**
- **Quadro do alvo:** o `Hud._target` saiu. No topo da tela fica só o painel do chefe, quando houver um. A vida do alvo continua na barra sobre a cabeça dele. A seleção continua marcada no mapa (anel amarelo) e na lista de batalha.
- **Conjuração no monstro (requisito 3):** a conjuração aparecia só no quadro. Sobre o monstro havia apenas a animação de conjurar, sem barra. Agora o monstro selecionado mostra uma barra fina vermelha (3 px) logo abaixo da barra de vida, enquanto conjura. Ela enche do início ao fim da conjuração. Monstros não selecionados não ganham barra, para não poluir. A barra do chefe continua no `BossPanel`, com nome e tempo.
- **Limpeza:** o `UnitFrame` perdeu o modo `showCast`, que só o quadro do alvo usava.

**Capturas:**
- `antes-cripta-vigia.png`, `depois-cripta-vigia.png` e `antes-depois.png`:
  - mostram o encontro local do Vigia (`--encounter=encounter.faceless_watcher --enemy-level=1 --enemy-health=0.6`), na fase 2, com o guerreiro atacando uma Sombra;
  - a simulação é determinística, então o antes e o depois são o mesmo quadro;
  - antes, o quadro "Sombra 56 / 150" aparecia embaixo do painel do chefe; agora o topo fica só com o chefe.
- `depois-campos.png`: contra o servidor local (`--world --dev-login --start-zone zone.vigil_fields`), atacando ratos nos Campos da Vigília, sem nada no topo.
- `conjuracao-sobre-o-vigia.png`: ampliação ×2 do Vigia selecionado conjurando "Sombras do Rosto Vazio". A barra vermelha está sob a vida, em três momentos.

**Desvios:**
- **Branch:** foi criada a partir de `docs/correcoes-playtest-v0.3`, e não de `main`, porque o arquivo desta tarefa só existe lá. Essa branch é `main` mais um commit, só de documentação.
- **`WorldView.cs`:** não estava nas áreas da tarefa. Mudou só uma chamada, a que passa o progresso da conjuração do alvo selecionado ao `EntityView`.
- **Captura "na Cripta":** foi feita no encontro local do Vigia (mapa `map.watcher_hall`), não na instância `zone.watcher_crypt` online. A instância pede um personagem de nível 12 a 15 e o servidor não tem comando de depuração para isso. O que a tarefa verifica, o painel do chefe aberto com outro monstro atacado, é o mesmo nos dois.

**Verificação:**
- `dotnet build client/`: sem avisos.
- `./scripts/check.sh` completo: build e formatação passam, e todos os projetos de teste passam menos um teste.
- O teste que falhou foi `InstanceTests.Account_character_inside_is_saved_at_the_origin_and_comes_back_inside_or_at_the_entrance`, do servidor, numa única execução. Na linha 245, o jogador voltou fora da instância. Ele levou 8 s, rodando junto com os `Game.Design.Tests`.
- Rodado de novo sozinho três vezes, e com o `Game.Server.Tests` inteiro (74 de 74), ele passou sempre. Esta tarefa não toca no servidor. Parece um teste intermitente sob carga.

**Pendências:**
- **Teste intermitente:** vale abrir uma tarefa de VPS para o `InstanceTests` acima, se a falha voltar.
- **Requisito 4:** se o designer preferir, no playtest, que o quadro volte e se esconda só com o painel do chefe aberto, basta devolver o `_target` ao `Hud` com `Visible = selected && !_boss.Visible`.
- **T-386:** na captura dos Campos, o Tab escolhe o rato longe e o ataque automático não sai até o jogador chegar perto. É uma das hipóteses da T-386, então não foi mexido aqui.
