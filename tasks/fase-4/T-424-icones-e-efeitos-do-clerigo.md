# T-424 — Cliente: ícones e efeitos das habilidades do Sacerdote e do Inquisidor

- **Ambiente:** Windows
- **Modelo:** Humano + Médio
- **Depende de:** T-423
- **Libera:** T-425
- **Áreas que pode alterar:** `client/assets/art/icons/`, `client/assets/art/effects/`, `client/scripts/Presentation/` (catálogos de ícones e efeitos, `EffectCatalog`, `AreaView`), `client/scripts/` da tela de habilidades (só se a escolha da segunda classe precisar de ajuste para o Clérigo), `CREDITS.md`, `design/art-direction-v0.1.md` (só para registrar a origem das peças novas), `tasks/reports/`
- **Branch:** sai da `main` e é integrada na `main` (seção 6g de `tasks/README.md`).

## Contexto

As 10 habilidades novas do Clérigo (T-423, ids `priest.*` e `inquisitor.*`) precisam de ícone e de efeito visual. Faça como na T-412 (Guerreiro), que o designer aprovou:

- ícones gerados no PixelLab (modelo Pro) com ícones do Clérigo da T-087 como referência de estilo, em 64 px e reduzidos para 32 px; o caminho que funcionou e o que deu errado estão no Resultado da T-412;
- novas linhas em `client/assets/art/icons/icons.png` e `icons.json`, com a origem em `source`;
- efeitos em `client/assets/art/effects/` (`effects.json`, `EffectCatalog`);
- o designer prefere arte simples, com pouco detalhe, e quer ver uma **amostra aprovada no jogo antes** de fazer o resto.

A tela de escolha e a tela de habilidades da T-410 leem as segundas classes de `classes.json`: com a T-423, o Clérigo já deve mostrar o Sacerdote e o Inquisidor. Conferir.

## Requisitos

1. **Amostra primeiro:** propor o ícone e o efeito de 2 habilidades (uma do Sacerdote, uma do Inquisidor; sugestão: Inspiração e Consagração), mostrados no jogo, e esperar a aprovação do designer antes das outras 8.
2. Um **ícone** para cada uma das 10 habilidades, no mesmo tamanho e estilo dos atuais.
3. Um **efeito visual** para cada habilidade, lido de longe:
   - Oração Restauradora, Bênção, Graça Veloz e Inspiração: o pulso em volta do Sacerdote ao lançar e um sinal pequeno em cada aliado enquanto durar, diferente para cada uma (cura, atributos, velocidade, Mana);
   - Santuário: a área em volta do Sacerdote e um sinal nos aliados protegidos;
   - Consagração: a área no chão;
   - Selo da Luz: a marca no monstro enquanto durar;
   - Martelo da Justiça e Julgamento: impacto no alvo;
   - Ira Divina: um sinal no Inquisidor enquanto durar e o Golpe Sagrado em área.
4. Conferir a escolha da segunda classe no Clérigo pela ferramenta do Esc (T-411): subir ao 20, ver as duas opções com as habilidades, escolher, desfazer e escolher a outra.
5. Toda peça nova tem origem e licença registradas em `CREDITS.md`.

## Critérios de aceite

- amostra aprovada pelo designer antes do resto;
- as 10 habilidades com ícone no painel de habilidades e na barra;
- cada habilidade mostra o efeito no jogo (capturas em `tasks/reports/interface/clerigo-efeitos/`, com uma prancha final como a da T-412);
- a escolha do Sacerdote e do Inquisidor funciona na tela de habilidades;
- `./scripts/check.sh --fast` passa no Windows.

## Resultado

**Feito** (Windows, 2026-11-23), na branch `task/T-424-icones-e-efeitos-do-clerigo`, saída da `main` (`a9ccc70`). **O designer aprovou a amostra** (Inspiração e Consagração) em 2026-11-23 e autorizou usar o crédito do PixelLab para o resto.

**Prancha final:** `tasks/reports/interface/clerigo-efeitos/prancha-final.png` (os 10 ícones, as duas barras e um recorte de cada efeito). Amostra aprovada: `amostra-prancha.png`, `amostra-inspiracao.png`, `amostra-consagracao.png`. Capturas inteiras: `sacerdote-grupo.png`, `sacerdote-santuario.png`, `inquisidor-ira-divina.png`, `inquisidor-julgamento.png`. Escolha da segunda classe: `escolha/1-nivel-20.png` a `escolha/7-inquisidor.png`.

### Ícones

- Gerados no PixelLab (modelo Pro, 64 × 64), com quatro ícones do Clérigo da T-087 (Cura Direta, Escudo Sagrado, Golpe Sagrado, Milagre, ampliados para 64 px) como referência de estilo, e reduzidos para 32 px como na T-412 (média por área e a paleta da própria candidata).
- Duas chamadas: a primeira com 10 gerações da assinatura e a segunda paga com o crédito (US$ 0,24), com o aval do designer. A da amostra tinha quatro habilidades no pedido (Inspiração, Consagração, Oração Restauradora, Selo da Luz); a do resto tinha seis habilidades no pedido (16 candidatos, dois ou três de cada, todos utilizáveis). Folhas em `clerigo-efeitos/candidatos/` (`amostra-folha.png`, `resto-folha.png`), escolhidos em 32 e 64 px em `escolhidos/`.
- Escolhidos: Oração Restauradora a9, Inspiração a1, Consagração a4, Selo da Luz a14 (cruz de luz, diferente do Escudo Sagrado, que já é um disco dourado com cruz); Bênção b0, Graça Veloz b3, Santuário b6, Martelo da Justiça b9, Julgamento b12, Ira Divina b15 (a = `67f51a2e…`, b = `0beec438…`).
- `icons.png` ganhou duas linhas no fim, combinadas com a T-434 e a T-444 (nota na pasta compartilhada): y = 442 Sacerdote e y = 476 Inquisidor, na ordem do design. A folha passou de 272 × 442 para 272 × 510; as linhas existentes não mudaram. `icons.json` aponta para elas com `source` = `pixellab:<job>#n`. Créditos em `CREDITS.md` e `icons-credits.txt`; origem registrada em `design/art-direction-v0.1.md`.

### Efeitos (desenhados pelo cliente, sem arte nova)

| Habilidade | Efeito | Onde |
|---|---|---|
| Oração Restauradora | onda verde até o raio (6); sinais de + verdes subindo dos lados de cada aliado | `bursts`, `auras` (`motes`, `plus`) |
| Bênção | onda dourada; setas douradas para cima | `bursts`, `auras` (`motes`, `chevron`) |
| Graça Veloz | onda azul-clara; arcos de vento girando nos pés e riscos de velocidade nos lados das pernas (também no Passo Livre) | `bursts`, `auras` (`wind`) |
| Inspiração | onda azul; gotas azuis subindo (só nos aliados: o Sacerdote não recebe) | `bursts`, `auras` (`motes`, `drop`) |
| Santuário | onda clara; cúpula de luz translúcida sobre cada protegido | `bursts`, `auras` (`dome`) |
| Consagração | chão dourado no raio, com oito marcas girando devagar e fiapos de luz subindo; a área de cura (sobreposta) não é desenhada | `AreaView` (`Consecrate`; `Hidden` para `area.consecration_mend`) |
| Selo da Luz | selo dourado girando nos pés do monstro enquanto dura | `auras` (`sigil`) |
| Martelo da Justiça | impacto sagrado de sempre, mais uma onda dourada curta no alvo e o atordoamento | `bursts` com `"at": "target"` |
| Julgamento | coluna de luz caindo no alvo e clarão nos pés | `pillars`, `PillarView` (novo) |
| Ira Divina | labaredas douradas no Inquisidor enquanto dura (o estilo da Fúria, em dourado); cada Golpe Sagrado solta uma onda dourada no alvo, até o raio da área (2) | `auras` (`flames`), `bursts` com `"at": "target"` para `inquisitor.divine_wrath_strike_*` |

- Estilos novos de sinal no corpo (`AuraKind`): `Motes` (três sinais subindo dos lados do corpo, no formato de `glyph`: `plus`, `drop`, `chevron`, `spark`), `Wind`, `Dome` e `Sigil`. Vários estados com sinais no mesmo personagem sobem em fases diferentes, para não se sobreporem.
- A troca do ataque básico na Ira Divina é do Core, mas o evento `AbilityExecuted` já sai com o id do Golpe Sagrado (`inquisitor.divine_wrath_strike_*`), local ou pelo servidor; por isso a onda no alvo funciona sem o cliente saber da troca.
- Código: `EffectCatalog` (estilos, `glyph`, `at`, `pillars`), `EntityView` (os quatro sinais novos), `AreaView` (Consagração), `WorldView` (onda no alvo e coluna) e `PillarView` (novo).

### Escolha da segunda classe (requisito 4)

Conferida no servidor local (`--world --dev-login --dev-tools`, porta 7791, para não colidir com as outras sessões) com o cliente em `127.0.0.1:7791`, pelo roteiro de cliques do cliente: Esc → Teste → Ir ao nível 20; K → Segunda classe: o Sacerdote e o Inquisidor aparecem com as habilidades e os ícones novos; Escolher Sacerdote → confirmação → "Agora você é Sacerdote."; Esc → Teste → Desfazer a segunda classe → "Segunda classe desfeita. Os pontos voltaram."; K → escolha de novo → Escolher Inquisidor → "Agora você é Inquisidor.". Capturas em `clerigo-efeitos/escolha/`.

### Desvios

- **`client/scripts/Hud/SkillPanel.cs`** (área permitida para a escolha): na tela de escolha, os ícones das habilidades esticavam na vertical quando o resumo tinha várias linhas (comum no Sacerdote); agora ficam em 28 × 28 (`SizeFlagsVertical = ShrinkBegin`).
- Para as capturas, a barra salva do jogo local (`user://hotbar.cfg`, chave `local-class.cleric`) foi preenchida à mão e depois apagada, como na T-412.
- O `import` do Godot gerou `client/scripts/Account/OnlineCounterText.cs.uid`, que falta na `main` (arquivo de outra tarefa); ficou fora do commit.

### Pendências

- O Martelo da Justiça usa o impacto sagrado de sempre, mais a onda curta; não tem arte de impacto própria.
- Sem teste automático para os efeitos (só apresentação, dependem do Godot); conferidos nas capturas.
- PixelLab: a conta ficou com 6 gerações da assinatura (renova em 2026-12-05) e US$ 4,76 de crédito.
- `./scripts/check.sh --fast` passa no Windows.
