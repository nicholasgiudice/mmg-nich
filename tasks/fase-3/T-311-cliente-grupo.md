# T-311 — Cliente: grupo (convidar, lista de membros, sair)

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** T-310
- **Libera:** —
- **Áreas que pode alterar:** `client/`

## Contexto

A T-310 criou o grupo no servidor: até 4 membros, convite, XP e saque divididos com quem está perto. Esta tarefa leva o grupo para a tela. O cliente só envia intenções e mostra o estado que o servidor manda (`AGENTS.md`).

## Requisitos

1. **Convidar:** clicar com o botão direito em outro jogador abre um menu pequeno com "Convidar para o grupo". Também dá para convidar pelo nome, numa caixa de texto do painel do grupo.
2. **Convite recebido:** janela com "<nome> convidou você para o grupo", os botões Aceitar e Recusar, e o tempo restante. Ela some quando o convite expira.
3. **Painel do grupo** (lado esquerdo, abaixo das barras do jogador): um quadro por membro com nome, classe (ícone), nível e barras de vida e Mana. O líder tem uma coroa. Membro em outra zona aparece esmaecido, com o nome da zona.
4. Clicar no quadro de um membro seleciona aquele membro como alvo (útil para cura). Usar a mesma intenção de alvo que já existe.
5. Botão "Sair do grupo". O líder tem também "Expulsar" no menu do botão direito do quadro.
6. As recusas (grupo cheio, já está em grupo, expirado...) aparecem como texto curto, como as recusas de item da T-222.
7. Log de combate: o ganho de XP dividido continua aparecendo como hoje. Entradas de grupo (entrou, saiu) ficam na categoria "Sistema" ou numa categoria nova.

## Critérios de aceite

- dois clientes contra o servidor local (`--world --dev-login`): convidar, aceitar, ver o outro no painel com a vida mudando, matar um monstro sozinho e ver a XP no log dos dois, sair do grupo;
- a T-085 continua valendo (o debug fica escondido; Ctrl+F mostra);
- `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
