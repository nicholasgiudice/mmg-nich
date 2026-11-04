# T-091 — Direção de arte v0.2 (PixelLab)

- **Ambiente:** Windows
- **Modelo:** Humano + Leve
- **Depende de:** T-090 (com a decisão "seguir")
- **Libera:** T-092, T-093, T-094, T-095, T-096
- **Áreas que pode alterar:** `design/art-direction-v0.2.md` (novo), `client/assets/art/pixellab/` (referências de estilo e manifest), `tasks/reports/`

## Contexto

O piloto (T-090, `tasks/reports/arte-pixellab-piloto.md`) aprovou o PixelLab. Antes de gerar dezenas de assets, é preciso fixar as regras, para que tudo fique com a mesma cara. A v0.1 (`design/art-direction-v0.1.md`) continua valendo para o que não mudar, como a escala e a legibilidade.

## Requisitos

Escrever `design/art-direction-v0.2.md`, com a aprovação do designer:

1. **Estilo:** uma frase de estilo comum a todos os prompts, e as imagens de **referência de estilo** (as melhores do piloto, ou uma nova). As imagens ficam em `client/assets/art/pixellab/style/` e entram em toda geração.
2. **Paleta:** uma paleta fixa, se a ferramenta aceitar (a animação tem opção de paleta em beta). Senão, uma etapa de "reduzir cores" para a paleta escolhida.
3. **Escala e quadros:** tile de 32 px; personagens e monstros por tamanho (pequeno, médio, grande e chefe), com o quadro, o pivô e a relação com o raio de colisão (§3 da v0.1). Escala só inteira.
4. **Direções e vista:** 4 ou 8 direções (o cliente usa 4 hoje; 8 exigiria mudança no `EntityView`, e isso deve ser registrado como decisão) e a vista "low top-down" ou "high top-down".
5. **Animações mínimas** por tipo de unidade (parado, andar, atacar, conjurar, morrer) e o que fazer quando uma não sair bem.
6. **Equipamento visível:** o LPC mostrava a arma e a armadura no boneco, porque o personagem era montado em camadas. Com o PixelLab o personagem é uma imagem fechada. Decida:
   - (a) um visual fixo por classe;
   - (b) poucas variações por faixa de equipamento;
   - (c) a arma desenhada separada.
   Registre a escolha e o impacto no inventário com o boneco (T-083).
7. **Convenção de prompts e o `manifest.json`** (formato fixo; o da T-090 como base). Proibido citar jogos, marcas ou artistas no prompt.
8. **Licença:** a seção nova com os termos do PixelLab (uso comercial permitido, proibido treinar modelos com as imagens; conferir os termos de uso na data). Também o que muda nas exigências do LPC à medida que os assets dele saem (créditos, CC-BY-SA, DRM). A tela de créditos continua exigida enquanto houver asset LPC.
9. **Ordem de troca:** a troca é por categoria inteira (todo o terreno, todos os personagens...), para nunca misturar estilos numa mesma cena por muito tempo. A opção `--art=pixellab` da T-090 vale até a troca terminar.

## Critérios de aceite

- `design/art-direction-v0.2.md` aprovado pelo designer;
- as referências de estilo no repositório;
- uma lista de todos os assets a gerar, com a tarefa responsável (T-092 a T-096).

## Resultado

_(preencher ao concluir)_
