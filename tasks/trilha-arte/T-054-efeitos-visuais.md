# T-054 — Cliente: efeitos visuais de habilidades, áreas e telegráfos

- **Ambiente:** Windows
- **Modelo:** Leve
- **Depende de:** T-053
- **Libera:** T-055
- **Áreas que pode alterar:** `client/`, `CREDITS.md`

## Contexto

Telegráfos, áreas e impactos hoje são formas desenhadas (T-023). Com arte, ganham efeitos — sem perder a legibilidade que o playtest validou.

## Requisitos

1. Projéteis (flechas, projétil arcano), impactos (explosão, relâmpago, golpe), áreas (poça de veneno, teia, campo de relâmpagos, chuva de flechas) e escudos com arte dos pacotes.
2. Telegráfos continuam com contorno e preenchimento progressivo claros por cima de qualquer efeito.
3. Opção de intensidade (normal/reduzida) para quem achar poluído.

## Critérios de aceite

- na Matriarca continua dando para ver e desviar de todos os ataques telegrafados;
- `dotnet build client/` e `./scripts/check.sh` passam.

## Resultado

**Feito** (Windows, 2026-10-31). `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa. Na Matriarca, os telegráfos continuam legíveis por cima de todos os efeitos (verificado por capturas; veja abaixo).

**Arte** (`client/assets/art/effects/`, créditos em `CREDITS.md`, os dois pacotes em CC-BY-SA 3.0 e sem modificação):
- `effects.png` do [LPC] Items and game effects (Tuomo Untinen);
- `magic_iceshield.png` do [LPC] Farming tilesets, magic animations… (Daniel Eddeland).

**Configuração** (`effects.json`, lido por `Presentation/EffectCatalog.cs`): quadros por coluna e linha da folha.
- **Impactos:** por escola de dano. Físico: estouro vermelho; arcano: brilho ciano; fogo: chamas; gelo: redemoinho de gelo; sagrado: raios de luz; natureza: fumaça verde. Algumas fontes têm impacto próprio: o campo de relâmpagos usa o relâmpago, a poça usa veneno e o cataclismo usa fogo.
- **Projéteis:** flecha nos disparos do Arqueiro e do Salteador, brilho arcano no Projétil Arcano. Voam do corpo de quem usou até o alvo (no máximo 0,22 s), e o impacto espera a chegada. O acerto no Core já aconteceu; isto é só leitura.
- **Áreas:** quadros espalhados dentro da área, cada um com fase própria. Poça de veneno: fumaça verde; campo de relâmpagos: relâmpagos; chuva de flechas: flechas caindo; fogo do cataclismo: chamas. Ficam por baixo do contorno da área, que não mudou (tracejado enquanto não está armada).
- **Escudos:** bolha do escudo de gelo em volta de quem tem escudo ativo (`ShieldState`): Escudo Sagrado, Barreira Arcana e o escudo do Guarda.

**Telegráfos por cima de tudo:** o contorno (e a seta da Investida) passou para um filho em `ZLayers.TelegraphEdges`, a camada mais alta do mundo. Impactos, projéteis e escudos ficam em `ZLayers.Effects`, logo abaixo. O preenchimento progressivo continua no chão. Nova ordem: chão < áreas < preenchimento dos telegráfos < rastros < mortos < unidades < decoração alta (`Above` passou de 6 para 7) < efeitos < contorno dos telegráfos.

**Intensidade** (`Presentation/EffectSettings.cs`): botão "Efeitos: normais/reduzidos" na pausa, gravado em `user://settings.cfg`. O modo reduzido mostra só os impactos e projéteis que saem do jogador ou vão para ele, e deixa a arte das áreas (35%) e os escudos (40%) mais transparentes. Telegráfos e contornos não mudam. Para depuração, `--effects=reduced|normal` vale só na execução; `--circles` também tira os efeitos.

**Como foi verificado:** capturas na Matriarca com o Mago (campo de relâmpagos, barreira, Projétil Arcano), aos 4, 5, 9,6, 9,9, 11 e 17 s.
- Investida (linha vermelha) com o contorno visível por cima da poça e das unidades.
- Poça de veneno, campo de relâmpagos, impactos de fogo, luz e golpe.
- Bolhas no Mago e no Clérigo.
- O modo reduzido, com a mesma cena aos 4 s.
- A pausa com o botão.

**Decisões e desvios**
- **Teia sem arte:** nenhum pacote aprovado tem teia. A teia continua desenhada (raios e anéis brancos), que já era clara. O relâmpago do pacote parece teia, mas usá-lo nas duas coisas confundiria o campo de relâmpagos com a teia da Matriarca.
- **Armadilha de contenção** continua desenhada (não estava na lista).
- **A flecha é o quadro de "tiro errado"** do pacote (um traço de flecha). Não há flecha inteira nos pacotes de efeito.
- **Contorno dos telegráfos acima das unidades e das copas.** Antes ele ficava abaixo das unidades; o requisito 2 ("por cima de qualquer efeito") e o §6 da direção de arte ("a arte de efeito entra por baixo do contorno") pedem que ele fique acima dos efeitos, e os efeitos ficam acima das unidades para serem vistos. O `design/art-direction-v0.1.md` (fora das áreas da tarefa) ainda descreve a ordem antiga: atualizar na próxima revisão do documento.

**Pendências**
- Ver na T-230 (playtest) se os efeitos poluem; o modo reduzido já existe.
- Atualizar a ordem de desenho em `design/art-direction-v0.1.md` §6.
