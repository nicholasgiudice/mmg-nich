# T-434 — Cliente: ícones e efeitos das habilidades do Atirador de Elite e do Caçador

- **Ambiente:** Windows
- **Modelo:** Humano + Médio
- **Depende de:** T-433
- **Libera:** T-435
- **Áreas que pode alterar:** `client/assets/art/icons/`, `client/assets/art/effects/`, `client/scripts/Presentation/` (catálogos de ícones e efeitos, `EffectCatalog`, `AreaView`), `client/scripts/` da tela de habilidades (só se a escolha da segunda classe precisar de ajuste para o Arqueiro), `CREDITS.md`, `design/art-direction-v0.1.md` (só para registrar a origem das peças novas), `tasks/reports/`
- **Branch:** sai da `main` e é integrada na `main` (seção 6h de `tasks/README.md`).

## Contexto

As 10 habilidades novas do Arqueiro (T-433, ids `marksman.*` e `hunter.*`) precisam de ícone e de efeito visual. Faça como na T-412 (Guerreiro), que o designer aprovou:

- ícones gerados no PixelLab (modelo Pro) com ícones do Arqueiro da T-087 como referência de estilo, em 64 px e reduzidos para 32 px; o caminho que funcionou e o que deu errado estão no Resultado da T-412;
- novas linhas em `client/assets/art/icons/icons.png` e `icons.json`, com a origem em `source`;
- efeitos em `client/assets/art/effects/` (`effects.json`, `EffectCatalog`);
- o designer prefere arte simples, com pouco detalhe, e quer ver uma **amostra aprovada no jogo antes** de fazer o resto.

Se a T-424 (Clérigo) já tiver entrado, as linhas novas de `icons.png` vêm depois das dela.

## Requisitos

1. **Amostra primeiro:** propor o ícone e o efeito de 2 habilidades (uma do Atirador, uma do Caçador; sugestão: Tiro Certeiro e Nuvem Tóxica), mostrados no jogo, e esperar a aprovação do designer antes das outras 8.
2. Um **ícone** para cada uma das 10 habilidades, no mesmo tamanho e estilo dos atuais; as **passivas** (Mira Firme e Flecha Envenenada) diferentes o bastante para a moldura de passiva da T-410 funcionar.
3. Um **efeito visual** para cada habilidade ativa, lido de longe:
   - Flecha Perfurante: a flecha atravessando a linha;
   - Tiro Debilitante e Ferida Aberta: impacto e um sinal no alvo enquanto durar;
   - Tiro Certeiro: um tiro mais forte que o Disparo, com impacto maior;
   - Olho de Falcão: um sinal no Atirador enquanto durar;
   - Nuvem Tóxica: a nuvem no chão;
   - Flecha Dispersa: as linhas do alvo até os monstros que recebem a cópia;
   - Praga: a área ao lançar e o pulo do veneno de um monstro para outro;
   - o Veneno do Caçador com o número de pilhas legível no alvo.
4. Conferir a escolha da segunda classe no Arqueiro pela ferramenta do Esc (T-411): subir ao 20, ver as duas opções, escolher, desfazer e escolher a outra.
5. Toda peça nova tem origem e licença registradas em `CREDITS.md`.

## Critérios de aceite

- amostra aprovada pelo designer antes do resto;
- as 10 habilidades com ícone no painel de habilidades e na barra;
- cada habilidade ativa mostra o efeito no jogo (capturas em `tasks/reports/interface/arqueiro-efeitos/`, com uma prancha final como a da T-412);
- a escolha do Atirador de Elite e do Caçador funciona na tela de habilidades;
- `./scripts/check.sh --fast` e o completo passam no Windows.

## Resultado

**Feito** (Windows, 2026-11-23), na branch `task/T-434-icones-e-efeitos-do-arqueiro`, saída da `main` em `a9ccc70` (com a T-433 e a T-443). **O designer aprovou a amostra** (Tiro Certeiro e Nuvem Tóxica, mais os ícones das duas passivas) em 2026-11-23, nesta sessão.

**Prancha final:** `tasks/reports/interface/arqueiro-efeitos/prancha-final.png` (os 10 ícones, as duas abas da segunda classe, as duas barras e um recorte de cada efeito). Amostra aprovada: `amostra-prancha.png`, `amostra-tiro-certeiro.png`, `amostra-nuvem-toxica.png` e `amostra-painel-*.png`. Recortes: `efeito-*.png`; capturas inteiras: `atirador-*.png` e `cacador-*.png`; escolha da segunda classe: `escolha-segunda-classe.png`.

### Ícones

- Como na T-412: PixelLab, modelo Pro, 64 × 64, várias habilidades por geração (16 candidatos: 4 de cada, ou 8 de cada com duas), com dois ou três ícones do Arqueiro da T-087 como referência de estilo; o escolhido foi recortado na moldura dourada e reduzido para 32 px (média por área e a paleta da própria candidata, sem pontilhado).
- Diferença da T-412: com `no_background: false` o PixelLab pôs uma paisagem em volta da moldura, então o recorte procura a moldura (linhas e colunas com muito dourado) antes de reduzir.
- Escolhidos (candidato da folha): Tiro Certeiro a1, Nuvem Tóxica a4, Mira Firme a9, Flecha Envenenada a13 (folha `amostra-folha.png`); Flecha Perfurante b1, Tiro Debilitante b4, Olho de Falcão b9, Ferida Aberta b14 (`atirador-cacador-folha.png`); Flecha Dispersa c1, Praga c8 (`dispersa-praga-folha.png`). Folhas em `arqueiro-efeitos/candidatos/`, escolhidos em 32 e 64 px em `arqueiro-efeitos/escolhidos/`.
- Passivas: Mira Firme é um olho azul com a linha de mira pontilhada; Flecha Envenenada, uma ponta de flecha pingando veneno. As duas diferem das ativas e aparecem com a moldura roxa da T-410. O Olho de Falcão é uma cabeça de falcão, para não confundir com a Mira Firme.
- `icons.png` ganhou as linhas **y = 510** (Atirador) e **y = 544** (Caçador), na ordem dos dados. **As linhas y = 442 e 476 ficaram vazias para o Clérigo (T-424)**, que roda ao mesmo tempo: na integração, basta colar as faixas de cada branch numa folha só (nota na pasta compartilhada das sessões). `icons.json` aponta para elas com `source` = `pixellab:<job>#n`. Créditos em `CREDITS.md`, `icons-credits.txt` e `design/art-direction-v0.1.md`.
- Custo: 3 gerações Pro de 64 px. A primeira (amostra) usou 10 gerações da assinatura; as outras duas, com a autorização do designer, saíram do crédito pré-pago (US$ 5,00 → US$ 4,60), porque as sessões do Clérigo e do Mago tinham gastado as gerações da assinatura.

### Efeitos (desenhados pelo cliente, sem arte nova)

| Habilidade | Efeito | Onde |
|---|---|---|
| Tiro Certeiro | flecha 1,5× maior com risco dourado atrás e rastro dourado do arco ao alvo; impacto 1,8× maior e anel dourado que se abre no chão quando a flecha chega | `projectiles` e `impacts.sources` com `{ animation, scale, glow }` (novo), `EffectView`, `BurstView` (com atraso) |
| Flecha Perfurante | a flecha vai de quem usou até o fim da linha (comprimento do NH), na direção mirada, atravessando os alvos; o impacto de cada alvo espera a flecha passar por ele | `pierces` (novo), `WorldView.Pierce` |
| Tiro Debilitante | flecha e impacto físico; corrente pesada girando nos pés enquanto dura (metade de trás atrás do corpo, a da frente por cima dos pés, para não sumir sob o lobo) | `auras` estilo `hobble` |
| Olho de Falcão | olho dourado pulsando ao lado da cabeça do Atirador enquanto dura | `auras` estilo `eye` |
| Nuvem Tóxica | gás verde-limão do tamanho da área, de bolotas que giram devagar, com bolhas subindo; a área do veneno que acompanha (`area.toxic_cloud_venom*`) não é desenhada | `AreaView` (estilos `Cloud` e `Hidden`) |
| Ferida Aberta | flecha com impacto de sangue quando chega (a habilidade não dá dano na hora); gotas de sangue escorrendo enquanto dura; sangue a cada pulso | `projectiles` com `impact` (novo), `auras` estilo `drip`, `impacts.sources` |
| Flecha Dispersa | linha verde do alvo até cada monstro que recebeu a cópia, e um anel verde nele | `spreads` (novo), `WorldView.SpreadLines` (pelos `StatusApplied/Stacked/Refreshed` do lote com as etiquetas do `copy_status`) |
| Praga | onda roxa no ponto mirado, até o raio da área no NH; anel roxo no Caçador enquanto a Praga dura; no pulo, linha roxa de quem perdeu o veneno até quem recebeu, anel roxo e o texto "Praga" (no meio de vários monstros a linha fica curta) | `bursts` com `at: aim` (novo), `jumps` (novo), `WorldView.AimBurst` e `JumpLine` |
| Veneno do Caçador | gota verde com o número de acúmulos ao lado do corpo (o maior entre as instâncias) | `auras` estilo `stacks` |

- O ponto mirado e o alvo vêm do `AbilityStarted` do mesmo lote de eventos (instantâneas executam no mesmo tick).
- O pulo da Praga foi conferido também por um log temporário (removido): o `StatusExpired` de um veneno e o `StatusApplied` do mesmo status em outro monstro chegam no mesmo lote.

### Alcance a mais no cliente (pendência da T-431)

- `PlayerInput.InReach` (aviso "fora de alcance" e ataque automático), `PlayerInput.ClampAim` (limite da mira das áreas) e o desenho da mira (`AimPreview`: anel do alcance e comprimento da linha) somam `RangeBonus.Of(content, statusEffects, tick, ability)`, como o Core. A barra também (`Hud.IsOutOfRange`, o vermelho de "fora de alcance").
- O construtor de `PlayerInput` com `ContentCatalog` ganhou as funções de tecla opcionais, para testar. Testes novos em `tests/Game.Client.Tests/RangeBonusInputTests.cs` (sem bônus: fora de alcance; com `status.hawk_eye_3`: dentro; `ClampAim` com +2).

### Escolha da segunda classe (T-411)

Servidor local (`--world --dev-login --dev-tools --port 7777`) e cliente em `127.0.0.1:7777`: Esc → Teste → Ir ao nível 20; na tela de habilidades, as duas opções com os ícones novos; escolheu o Atirador de Elite; Esc → Teste → Desfazer a segunda classe; escolheu o Caçador. Tudo funcionou sem ajuste na tela de habilidades (`escolha-segunda-classe.png`).

### Desvios (fora das áreas listadas, o mínimo)

- `client/scripts/Input/PlayerInput.cs`, `client/scripts/Hud/Hud.cs` e `client/scripts/Main.cs` (uma linha: passar o bônus à prévia): o alcance a mais, pedido junto com esta tarefa.
- `tests/Game.Client.Tests/RangeBonusInputTests.cs`: testes do alcance.
- `EntityView`, `StatusVisuals`, `EffectView`, `BurstView` e `WorldView` ficam em `client/scripts/Presentation/`, como os da T-412.
- Para as capturas, a barra salva do jogo local (`user://hotbar.cfg`, chave `local-class.archer`) foi editada à mão e depois tirada, sem mexer nas chaves das outras sessões.

### Pendências

- Integração de `icons.png` com o Clérigo (T-424) e o Mago (T-444): as três branches mexem no mesmo PNG; as faixas não se sobrepõem.
- A moldura dourada dos ícones novos é um pouco mais fina e escura que a da T-087 (como na T-412).
- O sinal do Olho de Falcão e a corrente do Tiro Debilitante são pequenos em sprites grandes; ver no playtest (T-435).
- Sem teste automático para os efeitos (só apresentação, dependem do Godot); conferidos nas capturas.
- `./scripts/check.sh --fast` e o completo passam no Windows.
