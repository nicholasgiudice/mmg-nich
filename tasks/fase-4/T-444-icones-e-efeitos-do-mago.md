# T-444 — Cliente: ícones e efeitos das habilidades do Piromante e do Arcanista

- **Ambiente:** Windows
- **Modelo:** Humano + Médio
- **Depende de:** T-443
- **Libera:** T-445
- **Áreas que pode alterar:** `client/assets/art/icons/`, `client/assets/art/effects/`, `client/scripts/Presentation/` (catálogos de ícones e efeitos, `EffectCatalog`, `AreaView`, desenho dos orbes), `client/scripts/` da tela de habilidades (só se a escolha da segunda classe precisar de ajuste para o Mago), `CREDITS.md`, `design/art-direction-v0.1.md` (só para registrar a origem das peças novas), `tasks/reports/`
- **Branch:** sai da `main` e é integrada na `main` (seção 6i de `tasks/README.md`).

## Contexto

As 10 habilidades novas do Mago (T-443, ids `pyromancer.*` e `arcanist.*`) precisam de ícone e de efeito visual. Faça como na T-412 (Guerreiro), que o designer aprovou:

- ícones gerados no PixelLab (modelo Pro) com ícones do Mago da T-087 como referência de estilo, em 64 px e reduzidos para 32 px; o caminho que funcionou e o que deu errado estão no Resultado da T-412;
- novas linhas em `client/assets/art/icons/icons.png` e `icons.json`, com a origem em `source`;
- efeitos em `client/assets/art/effects/` (`effects.json`, `EffectCatalog`);
- o designer prefere arte simples, com pouco detalhe, e quer ver uma **amostra aprovada no jogo antes** de fazer o resto.

Se a T-424 ou a T-434 já tiverem entrado, as linhas novas de `icons.png` vêm depois das delas.

## Requisitos

1. **Amostra primeiro:** propor o ícone e o efeito de 2 habilidades (uma do Piromante, uma do Arcanista; sugestão: Bola de Fogo e Orbes Arcanos), mostrados no jogo, e esperar a aprovação do designer antes das outras 8.
2. Um **ícone** para cada uma das 10 habilidades, no mesmo tamanho e estilo dos atuais.
3. Um **efeito visual** para cada habilidade, lido de longe:
   - Queimadura: um sinal de fogo no monstro enquanto durar;
   - Combustão: a explosão em cada monstro que queimava;
   - Rajada de Chamas: o cone de fogo;
   - Chão em Chamas: a área no chão;
   - Bola de Fogo: o projétil e a explosão;
   - Meteoro: o aviso no chão, o impacto e o chão em chamas do NH3;
   - Raio Arcano e Feixe do Vazio: o raio entre o Arcanista e o alvo enquanto canaliza, mais forte a cada pulso no Raio;
   - Marca Arcana: a marca no alvo com o número de pilhas legível;
   - Orbes Arcanos: os orbes girando em volta do Arcanista e os disparos até o alvo;
   - Sifão Arcano: o impacto no alvo e um sinal no Arcanista enquanto a Mana sobe mais rápido.
4. Conferir a escolha da segunda classe no Mago pela ferramenta do Esc (T-411): subir ao 20, ver as duas opções, escolher, desfazer e escolher a outra.
5. Toda peça nova tem origem e licença registradas em `CREDITS.md`.

## Critérios de aceite

- amostra aprovada pelo designer antes do resto;
- as 10 habilidades com ícone no painel de habilidades e na barra;
- cada habilidade mostra o efeito no jogo (capturas em `tasks/reports/interface/mago-efeitos/`, com uma prancha final como a da T-412);
- a escolha do Piromante e do Arcanista funciona na tela de habilidades;
- `./scripts/check.sh --fast` passa no Windows.

## Resultado

**Feito** (Windows, 2026-11-23), na branch `task/T-444-icones-e-efeitos-do-mago`, saída da `main` em `a9ccc70` (com a T-443). **O designer aprovou a amostra** (Bola de Fogo e Orbes Arcanos) em 2026-11-23, antes das outras 8. Não sobe `ProtocolVersion` (continua 19).

A T-424 e a T-434 ainda não estavam na `main`: as linhas novas de `icons.png` estão em y = 442 e 476, logo depois das do Guerreiro. Se uma delas entrar antes, as linhas do Mago vão para o fim: refazer a folha com os ícones de `mago-efeitos/escolhidos/` e mudar os `y` em `icons.json`.

**Prancha final:** `tasks/reports/interface/mago-efeitos/prancha-final.png` (os 10 ícones, as duas barras e um recorte de cada efeito). Amostra aprovada: `amostra-prancha.png`, `amostra-bola-de-fogo-voo.png`, `amostra-bola-de-fogo-explosao.png` e `amostra-orbes.png`. Escolha da segunda classe: `escolha-1-opcoes.png` a `escolha-5-arcanista.png`.

### Ícones

- Gerados no PixelLab (modelo Pro), em 64 × 64, com quatro ícones do Mago da T-087 como referência de estilo (Projétil Arcano, Explosão Elemental, Barreira Arcana e Cataclismo Arcano), como na T-412. Foram três gerações de 16 candidatos (30 gerações da assinatura, nada perdido):
  - `a` = `c6992f0c`: Bola de Fogo, Orbes, Combustão, Raio;
  - `b` = `bb12a08d`: Rajada, Chão, Meteoro, Marca;
  - `c` = `53289bf1`: Sifão, Feixe.

  Folhas em `mago-efeitos/candidatos/`; os escolhidos em 32 e 64 px em `mago-efeitos/escolhidos/`.
- O PixelLab devolve cada candidato com uma margem cinza (ou escura) por fora da moldura. A redução recorta na moldura dourada (mais 1 px de contorno) e reduz para 32 px (média por área e a paleta da própria candidata, sem pontilhado).
- Escolhidos, os mais simples de cada grupo, como o designer prefere:
  - Piromante: Combustão a10, Rajada de Chamas b0, Chão em Chamas b4, Bola de Fogo a0, Meteoro b9;
  - Arcanista: Raio Arcano a12, Marca Arcana b12, Orbes Arcanos a4, Sifão Arcano c3, Feixe do Vazio c12.
- `icons.png` ganhou duas linhas: y = 442 para o Piromante e y = 476 para o Arcanista, na ordem de `classes.json`. Em `icons.json`, cada ícone aponta para elas com `source` = `pixellab:<job>#n`. Créditos em `CREDITS.md` e `icons-credits.txt`; origem registrada em `design/art-direction-v0.1.md`.

### Efeitos (desenhados pelo cliente, sem arte nova)

| Habilidade | Efeito | Onde |
|---|---|---|
| Queimadura | três chamas pequenas no corpo do monstro e brasas subindo, enquanto dura (todos os NH, pelo prefixo `status.burn`) | `auras` (`burning`), `EntityView` |
| Combustão | explosão (clarão no chão, raio 1,6) em cada monstro atingido, ou seja, em cada um que queimava | `hitBursts` por fonte, `BurstView` (estilo explosão) |
| Rajada de Chamas | cone de fogo saindo do Piromante para o meio dos atingidos, no alcance e na abertura do NH dele (90° → 120° no NH3), com línguas de fogo na borda | `cones`, `ConeView` (novo) |
| Chão em Chamas | área laranja com o fogo do pacote da T-054 espalhado; o fundo fica forte mesmo sendo área do jogador | `areas` (`fire`, espalhado), estilo em `AreaView` |
| Bola de Fogo | bola laranja com rastro até o primeiro monstro da linha e, na chegada, explosão no raio da explosão do NH de quem usou (2 / 2,5 / 3); sem monstro na linha, a bola voa o comprimento da linha e se apaga | `bolts`, `BoltView` (novo) + `BurstView` |
| Meteoro | o aviso no chão de sempre (telegráfo); na segunda metade dele, uma pedra em chamas cai na diagonal e, quando o aviso some, explode do tamanho do aviso. No NH3, a área `area.meteor_flames`, com o mesmo estilo do Chão em Chamas | `meteors`, `MeteorView` (novo), `areas` |
| Raio Arcano | raio roxo de quem canaliza até o alvo, do `AbilityState.Cast` até o último pulso; mais largo e mais claro a cada pulso que já saiu | `beams` (`grow`), `BeamView` (novo) |
| Feixe do Vazio | raio roxo-escuro e grosso, do mesmo jeito; no NH3, ao terminar, o anel roxo do `status.void_exposed` no alvo | `beams`, `auras` (`ring`) |
| Marca Arcana | losango roxo à direita da barra de vida do alvo, com o número de pilhas em branco | `auras` (`mark`, contagem = pilhas) |
| Orbes Arcanos | orbes roxos girando em volta do peito do Arcanista, um por disparo do NH (2, 2, 3, 3, 4, lidos do `count` do `current_target` do status), metade atrás e metade na frente do sprite; cada disparo é uma bolinha roxa até o alvo, e o impacto espera a chegada | `auras` (`orbs`), `shots` por fonte (`status.arcane_orbs`) |
| Sifão Arcano | explosão pequena roxa no alvo, uma gota de Mana azul voltando do alvo para o Arcanista e, enquanto a regeneração dura, anel azul nos pés e gotas subindo | `hitBursts`, `shots` (`reverse`), `auras` (`mana`) |

- `effects.json` ganhou as seções `bolts`, `shots`, `hitBursts`, `cones`, `beams` e `meteors`, e os estilos de aura `orbs`, `burning`, `mark` e `mana` (descritos no `_comment`). Uma habilidade sem entrada nessas seções fica como antes.
- Código:
  - `EffectCatalog`: as seções novas;
  - `StatusVisuals`: quantos orbes e quantas pilhas;
  - `EntityView`: as quatro auras;
  - `BurstView`: estilo explosão;
  - `AreaView`: cor do fogo;
  - `WorldView`: liga tudo;
  - views novas: `BoltView`, `ConeView`, `BeamView` e `MeteorView`.

### Pendências do Core (Resultados da T-441 e da T-442)

- **Orbes:** desenhados a partir do status, sem entidades (acima).
- **Canalização na barra de conjuração:** já aparecia, porque usa o `AbilityState.Cast` (do uso até o último pulso). Agora a barra de uma canalização **esvazia** em vez de encher, tanto a do jogador (`Hud.UpdateCast`) quanto a barra fina do alvo selecionado (`WorldView`).
- **Previsão de movimento:** `NetAuthority.SpeedMultiplier` multiplica pelo `channel.moveSpeedMultiplier` da habilidade em `Cast`, no NH de quem usa, como o `ChannelSpeed` do Core. Assim o servidor não precisa corrigir a posição ao andar no Raio NH5 e no Feixe. **Não conferido no jogo:** só pela leitura do código e pelo build, porque as capturas usam o encontro local, que não tem previsão.
- **Bola de Fogo:** continua uma linha comum na mira, e o ponto da explosão não vai pela rede. O efeito sai no primeiro alvo: `Targets[0]` do `AbilityExecuted`, que segue a ordem da resolução (o primeiro alvo, depois os mais perto dele). Protocolo sem mudança.

### Escolha da segunda classe pela ferramenta do Esc

Servidor local (`--world --dev-login --dev-tools --port 7777`) e cliente em `127.0.0.1:7777`, com o roteiro do cliente (`--demo-input` com `pause`, cliques e `skills_panel`), numa execução só:

1. Esc → Teste → "Ir ao nível 20";
2. aba "Segunda classe (escolher!)": Piromante e Arcanista lado a lado, com os ícones novos (`escolha-1-opcoes.png`);
3. "Escolher Piromante" → confirmar (`escolha-2`, `escolha-3`);
4. Esc → Teste → "Desfazer a segunda classe" → Desfazer ("Segunda classe desfeita. Os pontos voltaram.", `escolha-4`);
5. "Escolher Arcanista" → confirmar (`escolha-5`).

A tela de habilidades não precisou de ajuste.

### Desvios (fora das áreas listadas, o mínimo)

- `client/scripts/Authority/NetAuthority.cs` (previsão ao canalizar) e `client/scripts/Hud/Hud.cs` (barra de canalização esvaziando): pedidos pela tarefa (pendências da T-442), mas fora das áreas listadas.
- `client/scripts/Match/MatchSetup.cs` e `client/scripts/Main.cs`: no encontro local, `--rank=id:n` agora vale também para as habilidades da segunda classe; antes, o `--second-class` deixava todas no NH1. Foi preciso para capturar o Meteoro NH3, o Raio NH5 e o Feixe NH3 (por exemplo, `--class=class.mage --level=40 --second-class=second_class.pyromancer --rank=pyromancer.meteor:3`).
- Para pôr as habilidades na barra durante as capturas, a barra salva do jogo local (`user://hotbar.cfg`, chave `local-class.mage`) foi editada à mão e depois restaurada, como na T-412.

### Pendências e observações

- A tela da segunda classe descreve a Marca Arcana e os Orbes Arcanos só pelo nome do status ("Marca Arcana", "Orbes Arcanos") e a Combustão só pelo custo. O texto vem da descrição dos dados (`AbilityText` e o site), que ainda não conhecem `if_status` nem `current_target` (pendência das T-441 e T-442).
- O fogo do Chão em Chamas usa os quadros de fogo do pacote da T-054, como o Cataclismo Arcano; os outros efeitos são formas desenhadas.
- A moldura dourada dos ícones novos é um pouco mais fina e escura que a dos ícones da T-087, como na T-412.
- Sem teste automático para os efeitos (só apresentação, dependem do Godot); conferidos nas capturas.
- `./scripts/check.sh --fast` passa no Windows: formatação, build sem avisos e os testes sem `Game.Design.Tests`. O completo fica para a integração.
