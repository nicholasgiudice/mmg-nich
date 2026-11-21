# T-412 — Cliente: ícones e efeitos das habilidades do Guardião e do Berserker

- **Ambiente:** Windows
- **Modelo:** Humano + Médio
- **Depende de:** T-408
- **Libera:** T-413
- **Áreas que pode alterar:** `client/assets/art/icons/`, `client/assets/art/effects/`, `client/scripts/Presentation/` (catálogos de ícones e efeitos, `EffectCatalog`, `AreaView`), `CREDITS.md`, `design/art-direction-v0.1.md` (só para registrar a origem das peças novas), `tasks/reports/`

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

_(preencher ao concluir)_
