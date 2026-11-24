# T-540 — Cliente: monstros do sul, água funda e aviso de chefe de campo

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** T-502, T-503, T-510, T-511, T-512, T-521
- **Libera:** T-550
- **Áreas que pode alterar:** `client/` (sprites, efeitos, telegráfos, HUD), `client/assets/art/sprites.json`, `tasks/reports/`

## Contexto

Mesmo papel da T-345 na fase 3: conferir no jogo a leitura dos monstros novos (T-510 a T-512), com os sprites da T-521, e mostrar o que as peças novas de sistema trazem:
- a água funda (`status.deep_water`, T-502): o jogador precisa entender por que ficou lento;
- o aviso de chefe de campo (evento da T-503).

## Requisitos

1. **Monstros:** os nove monstros novos aparecem com o sprite certo, o tamanho certo (o raio do Core bate com o desenho) e as animações de andar, atacar, conjurar e morrer. Os telegráfos novos (Bote, Agarrão, Rocha Rolante, Martelo, Fornalha, poças e Lodo da Bruxa) são legíveis e chegam antes do golpe.
2. **Água funda:** ícone do estado na barra de estados, com o nome "Água funda", e um efeito leve no personagem (respingo ou ondulação nos pés) enquanto está dentro. O efeito some no tick em que ele sai.
3. **Aviso de chefe de campo:** texto no alto da tela por alguns segundos e uma linha no log ("A Bruxa do Brejo despertou na Cabana da Bruxa" / "A Bruxa do Brejo foi derrotada"), no tema pedra e ouro. O texto sai do nome do monstro e da subárea, não fica fixo no código.
4. **Protocolo:** usa o evento da T-503. Esta tarefa sai da branch da T-503, ou da `main` se a T-503 já estiver integrada, e as duas saem juntas (cliente e servidor do mesmo commit).
5. O designer aprova no jogo, com imagens no relatório.

## Critérios de aceite

- prancha no jogo com os nove monstros e os telegráfos, em `tasks/reports/cliente-t540/`;
- online local: dois clientes na mesma zona recebem o aviso da Bruxa; um cliente em outra zona não recebe;
- `./scripts/check.sh` completo passa.

## Resultado

_(a preencher)_
