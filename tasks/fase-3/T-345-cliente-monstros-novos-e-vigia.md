# T-345 — Cliente: sprites e leitura dos monstros novos e do Vigia

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** T-340, T-341, T-351, T-056 (sprites dos monstros novos)
- **Libera:** T-380
- **Áreas que pode alterar:** `client/`, `CREDITS.md`

## Contexto

A T-340 e a T-341 criaram a Fada da Névoa, o Ent Corrompido, o Sacerdote Morto e o Cavaleiro Espectral. A T-351 criou o Vigia Sem Rosto, as sombras, os selos, o medo e o fim do encontro com falha. O cliente desenha os monstros por `client/assets/art/sprites.json` (T-053) e os efeitos por `effects.json` (T-054), com pacotes LPC aprovados em `design/art-direction-v0.1.md`. Sem sprite, a unidade aparece como círculo.

**A arte já existe (T-056, `tasks/reports/arte-monstros-fase-3.md`):** o Cavaleiro, o Sacerdote, o Vigia (×2), a Sombra e a Fada foram montados no gerador LPC e estão no `sprites.json` com os ids `monster.spectral_knight`, `monster.dead_priest`, `monster.faceless_watcher`, `monster.shade` e `monster.mist_fairy`. Os fantasmas usam o campo `tint` (cor e transparência). A variante `preview_fase3` (`--art=preview_fase3`) mostrava os sprites no lugar dos monstros antigos enquanto os dados não existiam. **O Ent Corrompido não tem arte**: os candidatos LPC foram reprovados e ele vai para encomenda.

## Requisitos

1. **Sprites:**
   - confira se os ids criados pela T-340, T-341 e T-351 batem com as chaves da T-056 no `sprites.json`; se mudaram, renomeie as chaves;
   - **Ent Corrompido:** até a encomenda chegar, use um sprite provisório (uma variação de cor de um sprite existente) e registre;
   - ajuste o `tint` e o tamanho no jogo, se precisar, e **remova a variante `preview_fase3`**, que deixa de ser necessária;
   - a Fada "na névoa" (requisito 2) é um estado de jogo: a transparência dela vem desse estado, por cima do `tint` normal.
2. **Leitura das mecânicas novas:**
   - Fada "na névoa": semitransparente e sem barra de vida, com aviso antes de sumir;
   - medo: ícone sobre o personagem e a barra de controle (como os outros controles);
   - selos: barra de vida própria, sempre visível na tela do chefe (topo da tela, com os 3 selos);
   - o cast do Último Comando: barra de cast destacada, porque **precisa** ser interrompido;
   - o fim com falha: tela "Expedição fracassada" (como o resumo de morte).
3. Telegráfos e contornos de todas as habilidades novas legíveis por cima dos efeitos (regra da T-054). Conferir com capturas.
4. Os nomes dos monstros novos aparecem no log, na lista de batalha (T-312, se já integrada) e no site gerado (T-071), sem mudança manual.

## Critérios de aceite

- capturas de cada monstro novo em combate (encontros de teste locais) e da luta do Vigia nas três fases;
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Resultado

**Feito.** As capturas estão em `tasks/reports/cliente-t345/`.

**Sprites** (`client/assets/art/sprites.json`):
- os ids da T-056 batem com os dados da T-340, T-341 e T-351;
- a variante `preview_fase3` saiu. O mecanismo `--art` continua, e `variants` ficou vazio;
- **Ent Corrompido (provisório):** a folha do Esqueleto em ×2, com `tint` de madeira e o golpe `slash_128`, até a encomenda chegar;
- **selo do Vigia** (`monster.watcher_seal`): o medalhão de pedra do `terrain_summer` (layout `seal`), que escurece quando quebra (animação de morte);
- o `tint` e o tamanho dos outros monstros não precisaram de ajuste.

**Leitura das mecânicas:**
- **Fada na névoa** (`status.in_mist`, um status com a tag `mist` e fora de alvo): o sprite fica com 30% de opacidade, sem barra de vida, sem nome e sem status. O aviso antes de sumir são anéis de névoa que sobem dos pés enquanto ela conjura uma habilidade com a tag `ai.vanish` (o Véu de Névoa, 0,6 s).
- **Medo** (`flags.feared`): balão roxo com "!" acima das plaquetas de status, tremendo, como as estrelas do atordoamento. A plaqueta "Pa" (controle) segue igual às outras. No online, o cliente não prevê o próprio movimento com medo, porque quem move é o servidor (como no atordoamento).
- **Tela do chefe** (`BossPanel`): no topo da tela, enquanto há um chefe vivo (rank `boss`). Mostra o nome com a fase, a vida e a conjuração, e uma barra para cada objetivo do encontro, com o estado Intacto, em ruína (vida) ou Quebrado. O quadro do alvo desce para baixo dela.
- **Último Comando:** a conjuração do chefe que mira um objetivo fica vermelha e pulsando, com "INTERROMPA!".
- **Fim com falha:** o encontro local mostra "Expedição fracassada" na tela de resultado. No mundo aberto (a Cripta), o `EncounterFailed` abre o painel "Expedição fracassada", no lugar do resumo de morte. Os selos não contam mais como jogadores na derrota.
- **Avisos de instância** (pendência da T-352): `InstanceNotice` e `InstanceRejected` aparecem como mensagens, como as do grupo.
- **Telegráfos e nomes:** os telegráfos continuam por cima dos efeitos (capturas do Ent e do Vigia). Os nomes no log, na lista de batalha e no site vêm dos dados, sem mudança manual.

**Capturas:** cada monstro novo em combate e o Vigia nas três fases. Para isso, o cliente ganhou três argumentos de depuração, só nos encontros locais:
- `--capture-every=s`: uma série de capturas;
- `--enemy-level=n`: inimigos e objetivos no nível n;
- `--enemy-health=f`: inimigos começando com a fração f da vida.

O grupo do encontro local é de nível 1, e sem esses argumentos a luta do Vigia não sai da fase 1. Os dados não mudam.

**Testes:** `tests/Game.Client.Tests/WatcherReadingTests.cs` cobre a falha na tela de resultado, os selos fora da contagem de jogadores e os textos de instância. É fora das áreas da tarefa: o mínimo para testar o que mudou.

**Decisões:**
- O aviso de sumiço e a transparência vêm das tags dos dados (`ai.vanish` e `mist`), e não do id da Fada. O "objetivo" do Último Comando é qualquer alvo com `EncounterObjective`.
- O quadro do alvo continua aparecendo quando o alvo é o chefe. Repete a vida, mas mostra os status dele.

**Pendências:**
- **Arte:** a encomenda do Ent; um ícone de medo e um selo de verdade, se o designer quiser.
- **T-380:** avaliar no playtest se o aviso da Fada (0,6 s) dá tempo de reagir.
