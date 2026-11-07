# T-311 — Cliente: grupo (convidar, lista de membros, sair)

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** T-310
- **Libera:** —
- **Áreas que pode alterar:** `client/`

## Contexto

A T-310 criou o grupo no servidor: até 4 membros, convite, XP e saque divididos com quem está perto. Esta tarefa leva o grupo para a tela. O cliente só envia intenções e mostra o estado que o servidor manda (`AGENTS.md`).

**Arte da interface (aprovada em 2026-11-06):** a janela de convite e o painel do grupo usam o kit pedra e ouro de `client/assets/art/ui/` (painel, moldura de membro, botão em quatro estados e a coroa do líder). Margens de 9-slice, uso e prévia em `tasks/reports/interface/kit-pedra-ouro.md`. O resto do HUD continua como está; aplicar o kit nele é outra tarefa.

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

**Feito** (Windows, 2026-11-07). `dotnet build client/` sem avisos e `./scripts/check.sh` completo passa. Verificado com dois clientes contra o servidor local (`--world --dev-login --start-zone zone.vigil_fields`), com capturas em `tasks/reports/T-311/`.

**O que foi feito**
- `Hud/PartyModel.cs` (sem Godot, 11 testes em `tests/Game.Client.Tests/PartyModelTests.cs`): guarda o último `PartyState` e os convites recebidos ainda válidos (o mais antigo aparece primeiro; um convite renovado da mesma pessoa troca o anterior; entrar num grupo descarta os convites). `PartyText` tem os textos de convite, avisos e recusas.
- `NetAuthority`: recebe `PartyState`, `PartyInviteReceived`, `PartyNotice` e `PartyRejected`, e envia `PartyInvite` (por entidade ou nome), `PartyRespond`, `PartyLeave` e `PartyKick`. Avisos e recusas viram texto para o painel (`TakePartyMessages`).
- **Convidar:** botão direito sobre outro jogador abre o menu "Convidar <nome> para o grupo" (só no mundo aberto). No painel, "+ Convidar" abre a caixa de nome (Enter ou "Convidar"). Em grupo, só o líder vê o botão.
- **Convite recebido:** `Hud/PartyInviteDialog.cs`, no painel de pedra do kit, com coroa, texto, "Expira em N s", Aceitar e Recusar. Some quando o convite expira ou é respondido.
- **Painel do grupo** (`Hud/PartyPanel.cs`), abaixo dos botões Personagem/Habilidades/Inventário: um quadro de pedra por membro, com o ícone da classe, nome e nível, coroa no líder, vida e Mana. Membro em outra zona fica esmaecido, com o nome da zona no lugar das barras; caído aparece como "(caído)".
- Clicar no quadro de um membro na mesma zona seleciona a entidade dele como alvo (`PlayerInput.Select`, a mesma intenção do clique no mapa). O líder abre "Expulsar do grupo" com o botão direito no quadro. "Sair do grupo" fica embaixo do painel.
- **Recusas e avisos** aparecem como texto curto no painel por 4 s (ex.: "Ninguém com esse nome está em jogo.", "Convite enviado para Lyra.", "O grupo se desfez.").
- **Log:** entrou, saiu, expulso, novo líder e grupo desfeito vão para a categoria nova **Sistema** ("Sist." no filtro). A XP dividida continua como antes ("Você ganhou 3 de XP").
- `HudTheme`: `UiPiece`, `MemberFrame`, `StoneButton` e `Crown`, com as peças do kit pedra e ouro (`client/assets/art/ui/`).
- Enquanto a caixa de nome tem o foco, as teclas são texto: o `Main` não lê movimento nem Esc/R, e o HUD não abre C/K/I/B.
- Roteiro de teste (`--demo-input`): `invite:nome`, `accept`, `decline`, `leave` e `kick:nome`.

**Teste com dois clientes** (Thorin, Guerreiro, e Lyra, Clériga):
- `1-lyra-convite.png`: Thorin convida pelo nome e a janela aparece para a Lyra, com o tempo correndo.
- `2-lyra-grupo.png` e `2-thorin-grupo.png`: Lyra aceita; cada um vê o outro no painel (Thorin com a coroa) e a vida mudando. Thorin mata um Rato de Campo sozinho e os dois logs mostram "Thorin ganhou 3 de XP" e "Você ganhou 3 de XP".
- `4-lyra-recusa.png`: convite para um nome que não existe mostra "Ninguém com esse nome está em jogo.".
- `3-thorin-lyra-saiu.png`: Lyra sai; Thorin vê "O grupo se desfez." e o log Sistema com "Lyra entrou no grupo", "Lyra saiu do grupo" e "O grupo se desfez".
- A T-085 continua valendo: o log só aparece com Ctrl+F (nas capturas ele foi aberto pelo roteiro).

**Decisões e desvios**
- **Fora das áreas da tarefa (mínimo):** `src/Game.Core/CombatLog/` ganhou `LogCategory.Sistema` e `CombatLogRecorder.Note(tick, texto, origem)`, com teste em `CombatLogTests`. O evento de origem (`PartyLogNote`) fica no cliente, porque o protocolo exige formato de rede para todo evento do Core, e esse nunca vai pela rede. A tarefa pedia a categoria "Sistema", que não existia. `tests/Game.Client.Tests` passou a referenciar `Game.Protocol` (o modelo usa as mensagens do grupo).
- No mundo aberto o painel do grupo **substitui** os quadros compactos de aliados, que mostravam todo jogador por perto. Na sala de encontro e no modo local, os quadros de aliados continuam como antes.
- O ícone de classe é o do ataque básico da classe, porque ainda não há ícones de classe.
- O texto do log do grupo é perdido na troca de zona (o log recomeça a cada `Welcome`, como já acontecia).

**Pendências**
- O menu do botão direito sobre outro jogador e o "Expulsar" não foram exercitados nas capturas: o roteiro de teste mira sempre o inimigo mais próximo. O caminho no servidor está coberto pelos testes da T-310. Vale conferir à mão no playtest (T-380).
