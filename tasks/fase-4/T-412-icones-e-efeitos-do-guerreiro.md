# T-412 — Cliente: ícones e efeitos das habilidades do Guardião e do Berserker

- **Ambiente:** Windows
- **Modelo:** Humano + Médio
- **Depende de:** T-408
- **Libera:** T-413
- **Áreas que pode alterar:** `client/assets/art/icons/`, `client/assets/art/effects/`, `client/scripts/Presentation/` (catálogos de ícones e efeitos, `EffectCatalog`, `AreaView`), `CREDITS.md`, `design/art-direction-v0.1.md` (só para registrar a origem das peças novas), `tasks/reports/`
- **Branch:** sai de `trilha/segundas-classes` e é integrada nela, não na `main` (seção 6f de `tasks/README.md`).

## Contexto

As 10 habilidades novas (T-408, ids `guardian.*` e `berserker.*`) precisam de ícone e de efeito visual. Hoje:

- os **ícones** das habilidades vêm do pacote CC0 escolhido na T-080 (`client/assets/art/icons/`, catálogo em `client/scripts/Presentation/`);
- os **efeitos** vêm de `client/assets/art/effects/` (`effects.json`, `EffectCatalog`), com os créditos ao lado;
- o designer prefere arte simples, com pouco detalhe, e quer ver uma **amostra aprovada antes** de fazer o resto. O PixelLab está aprovado para peças de interface.

## Requisitos

1. **Amostra primeiro:** propor o ícone e o efeito de 2 habilidades (uma do Guardião, uma do Berserker), mostrados no jogo, e esperar a aprovação do designer antes das outras 8.
2. Um **ícone** para cada uma das 10 habilidades, no mesmo tamanho e estilo dos ícones atuais; as **passivas** (Sede de Sangue) com o ícone diferente o bastante para a moldura de passiva da T-410 funcionar.
3. Um **efeito visual** para cada habilidade ativa, lido de longe:
   - Interceptação: a linha entre o Guardião e o aliado enquanto o desvio dura;
   - Juramento e Bastião: a área em volta do Guardião;
   - Tempestade de Aço: o giro acompanhando o Berserker;
   - Fúria e Inabalável: um sinal no personagem enquanto durarem;
   - os golpes (Golpe de Escudo, Investida, Execução): impacto no alvo.
4. Toda peça nova tem origem e licença registradas em `CREDITS.md`.

## Critérios de aceite

- amostra aprovada pelo designer antes do resto;
- as 10 habilidades com ícone no painel de habilidades e na barra;
- cada habilidade ativa mostra o efeito no jogo (capturas em `tasks/reports/interface/guerreiro-efeitos/`);
- `./scripts/check.sh --fast` passa no Windows.

## Resultado

**Feito** (Windows, 2026-11-22), na branch `task/T-412-icones-e-efeitos-do-guerreiro`. **O designer aprovou a amostra** (Interceptação e Fúria) em 2026-11-22. A branch sai de `task/T-408-dados-guardiao-e-berserker` (`c4d39e2`), porque as habilidades novas só existem lá; entra na `trilha/segundas-classes` depois da rodada do protocolo (seção 6f de `tasks/README.md`).

**Prancha final:** `tasks/reports/interface/guerreiro-efeitos/prancha-final.png` (os 10 ícones, as duas barras e um recorte de cada efeito). Amostra aprovada: `amostra-prancha.png`, `amostra-interceptacao.png` e `amostra-furia.png`. Recortes de cada efeito: `efeito-*.png`; capturas inteiras: `guardiao-grupo.png` e `berserker-tempestade.png`.

### Ícones

- Gerados no PixelLab (modelo Pro) com quatro ícones do Guerreiro da T-087 como referência de estilo. Cada geração em 64 × 64 com quatro habilidades no pedido devolve 16 ícones únicos de 64 px (quatro de cada); o escolhido foi reduzido para 32 px (média por área e a paleta da própria candidata, sem pontilhado), o tamanho dos ícones atuais.
- Escolhidos (candidato da folha): Interceptação g0, Golpe de Escudo g5, Bastião g9, Inabalável g15, Juramento j0; Investida b0, Fúria b4, Execução b11, Sede de Sangue b13, Tempestade de Aço j8. Folhas em `guerreiro-efeitos/candidatos/`, escolhidos em 32 e 64 px em `guerreiro-efeitos/escolhidos/`.
- `client/assets/art/icons/icons.png` ganhou duas linhas (y = 374 Guardião, y = 408 Berserker, na ordem do design); `icons.json` aponta para elas com `source` = `pixellab:<job>#n`. Créditos em `CREDITS.md` e `icons-credits.txt`.
- A Sede de Sangue (passiva) é uma lâmina pingando sangue, diferente dos rostos e das armas das ativas, para a moldura de passiva da T-410.
- Custo: 50 gerações da assinatura. 20 se perderam na primeira tentativa (ícones únicos de 32 px com a referência em grade de 2 × 2: o PixelLab desenhou quatro ícones de 16 px em cada candidato); as outras 30 deram os 10 ícones.

### Efeitos (desenhados pelo cliente, sem arte nova)

| Habilidade | Efeito | Onde |
|---|---|---|
| Interceptação | corrente dourada tracejada, andando do protegido até o Guardião, enquanto o desvio dura; anel fino dourado nos pés do protegido | `auras` (`link`) em `effects.json`, `LinkView` |
| Bastião | onda dourada saindo do Guardião até o raio do NH dele; anel dourado pulsando nos pés de quem recebeu | `bursts` e `auras` (`ring`), `BurstView` |
| Juramento | onda clara até o raio; anel claro nos aliados | `bursts` e `auras` (`ring`) |
| Inabalável | anel prateado nos pés enquanto dura | `auras` (`ring`) |
| Fúria | labaredas vermelhas e amarelas em volta do corpo e faíscas subindo, enquanto dura | `auras` (`flames`) |
| Tempestade de Aço | quatro lâminas girando em volta do Berserker, acompanhando a área que segue o dono | `AreaView` (estilo `Spin` para `area.steel_storm*`) |
| Execução | impacto de sangue no alvo (quadros do pacote de efeitos da T-054) | `impacts.sources` e animação `blood` em `effects.json` |
| Golpe de Escudo, Investida | impacto físico de sempre, mais o atordoamento (estrelas) e o rastro do avanço que já existiam | — |

- As auras são por prefixo de id de status (`status.bastion` vale para `status.bastion_28` e os outros NH). A onda usa o raio dos alvos extras da habilidade no NH de quem usou (o Bastião vai de 5 para 7 no NH3); o `radius` do `effects.json` só vale se a habilidade não tiver alcance.
- Código: `EffectCatalog` (auras e ondas), `StatusVisuals.Auras`, `EntityView` (anel e labaredas), `LinkView` e `BurstView` (novos), `WorldView` (liga tudo), `AreaView` (`Spin`).

### Desvios (fora das áreas listadas, o mínimo)

- **`client/scripts/Match/MatchSetup.cs` e `client/scripts/Main.cs`:** opções de depuração do encontro local para as capturas, `--level=n` e `--second-class=id`. O jogador (e, com `--second-class`, quem tem a classe base dela, inclusive um bot) é trocado por um personagem com progressão (`Leveling.SpawnCharacter`, o caminho do personagem salvo), no nível pedido, com a segunda classe e NH1 em cada habilidade dela que o nível deixa. Assim deu para capturar a Interceptação usada por um Guardião bot (`--class=class.mage --encounter=encounter.forest_matriarch --level=21 --second-class=second_class.guardian`). Servem também para as prévias da T-410.
- Para pôr as habilidades novas na barra durante as capturas, a barra salva do jogo local (`user://hotbar.cfg`, chave `local-class.warrior`) foi editada à mão e depois restaurada; a T-410 é que põe as habilidades da segunda classe na barra.

### Pendências

- **Painel de habilidades:** as habilidades da segunda classe ainda não aparecem na tela de habilidades (T-410); os ícones já estão no catálogo e aparecem na barra.
- **Duração da passiva no quadro do personagem (para a T-410):** o quadro mostra "Sede de Sangue 461168601842739000,0s", porque o status da passiva não acaba e a tela de status não trata isso.
- A moldura dourada dos ícones novos é um pouco mais fina e escura que a dos ícones da T-087.
- Sem teste automático para os efeitos (só apresentação, dependem do Godot); conferidos nas capturas.
- `./scripts/check.sh --fast` e o completo passam no Windows.
