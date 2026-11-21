# T-405 — Servidor: ferramenta de teste para subir de nível

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** T-404 (sai da branch dela)
- **Libera:** T-411
- **Áreas que pode alterar:** `src/Game.Server/` (opção nova e tratamento das mensagens), `src/Game.Protocol/` (mensagens novas e `ProtocolVersion`), `src/Game.Core/Combat/Progression/` (só os métodos de apoio da ferramenta), `deploy/compose.yml` e `scripts/deploy-dev.sh` (só a variável da opção nova), `src/Game.Server/README.md`, `tests/`
- **Branch:** sai de `trilha/segundas-classes` e é integrada nela, não na `main` (seção 6f de `tasks/README.md`).

## Contexto

Design em `design/segunda-classe-v0.1.md`, seção 4.2. Ainda não há áreas do nível 16 ao 40, e o designer precisa testar as segundas classes. Decisão do designer: um **botão no menu do Esc** (cliente, T-411) que pede ao servidor para subir de nível e para desfazer a segunda classe. Hoje o único atalho de desenvolvimento é o `--dev-login` (`ServerOptions`, `MMO_DEV_LOGIN`, `GAME_DEV_LOGIN` no `deploy/compose.yml`).

A ferramenta **não pode ser ligada por engano** num servidor aberto a jogadores.

## Requisitos

1. **Opção nova do servidor**, `--dev-tools` (env `MMO_DEV_TOOLS`), **desligada por padrão**. No `deploy/compose.yml`, variável `GAME_DEV_TOOLS` com padrão `false`; o servidor de teste da VPS liga pelo `.env` ou pelo `deploy-dev.sh` (escolher o mais simples e documentar). Com a opção ligada, o servidor escreve um aviso no log ao subir, como já faz com o `--dev-login`.
2. **O cliente fica sabendo** se a ferramenta está ligada (por exemplo, um campo na resposta ao `Hello`), para só mostrar o botão quando ela existe.
3. **Mensagem nova do cliente** com o pedido, um de:
   - subir **+1 nível**; subir **+5 níveis**; ir ao **nível 20**; ir ao **nível 40**;
   - **desfazer a segunda classe**: tira a segunda classe e devolve os pontos de habilidade gastos nela e os +5 de atributo (se os +5 já estiverem distribuídos, refaz a distribuição de atributos inteira, como `ResetPoints`);
   - **redistribuir grátis**: o mesmo que `ResetPoints`, sem custo.
4. **Subir de nível pelo caminho normal:** dar a XP que falta pelo mesmo caminho da XP de monstro (`Leveling.GrantExperience`), para os pontos de atributo e de habilidade saírem iguais aos de quem subiu jogando. Nada acima do nível máximo.
5. **Recusa:** sem `--dev-tools`, o servidor recusa qualquer pedido da ferramenta e registra no log quem pediu. Em combate, recusa também (como `ResetPoints`).
6. O personagem é gravado depois de cada pedido aceito.
7. `ProtocolVersion` + 1 (em cima do da T-404). Documentar a opção em `src/Game.Server/README.md`.

## Integração

Sai da branch da T-404. **T-406, T-407, T-404, T-405 e T-411 entram juntas** na `trilha/segundas-classes`. O deploy com a ferramenta ligada só acontece no dia do playtest da trilha (seção 6f de `tasks/README.md`).

## Critérios de aceite

- **sem a opção, o servidor recusa** cada tipo de pedido (teste);
- com a opção: +1, +5, ir ao 20 e ir ao 40 dão o mesmo nível, os mesmos pontos de atributo e os mesmos pontos de habilidade que subir com XP de monstro (teste comparando os dois caminhos);
- desfazer a segunda classe devolve os pontos e permite escolher outra (teste);
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
