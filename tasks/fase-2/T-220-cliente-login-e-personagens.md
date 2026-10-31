# T-220 — Cliente: login, seleção e criação de personagem

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** T-211
- **Libera:** T-223, T-230
- **Áreas que pode alterar:** `client/`

## Contexto

Com a `Game.Api` (T-211), o cliente deixa o "Jogar online com nome" (T-107) e passa a entrar com conta.

## Requisitos

1. Telas: login/cadastro, lista de personagens (nome, classe, nível), criar (nome + classe, com a descrição da classe) e apagar (com confirmação).
2. "Jogar": pede o token de jogo e conecta no servidor com ele (`NetAuthority`).
3. Erros claros (senha errada, nome em uso, servidor fora). O token de sessão fica só em memória (não gravar senha nem token em disco).
4. O modo local (menu de cenários) continua existindo para teste.

## Critérios de aceite

- entrar, criar personagem e jogar contra a VPS de ponta a ponta;
- `dotnet build client/` e `./scripts/check.sh` passam.

## Resultado

**Feito, menos o teste de ponta a ponta contra a VPS** (Windows, 2026-10-31). `dotnet build client/` sem avisos, `./scripts/check.sh` completo passa. O critério "entrar, criar personagem e jogar contra a VPS" **não foi verificado**. A API da VPS só é alcançável dentro da rede do Compose (a T-213, Caddy, não foi feita), e o acesso do agente à VPS por SSH foi negado. Veja "Pendências".

**Telas** (`client/scripts/Account/AccountScreen.cs`, cena `scenes/Account.tscn`, agora a cena inicial do projeto)
- **Entrar:** servidor de contas (URL da API), servidor do jogo (`host:porta`; padrão = host da API e porta 7777), e-mail e senha (campo oculto). Botões "Entrar" e "Criar conta" (cadastra e já entra). Enter na senha entra.
- **Personagens:** lista com nome, classe, nível e zona (nome da zona vem de `data/zones.json`). Botões Jogar (ou duplo clique), Criar personagem, Apagar e Sair da conta. Lista vazia mostra "Nenhum personagem ainda" e desativa Jogar/Apagar.
- **Criar:** nome (até 16 caracteres) e classe, com a descrição da classe (`ClassText`, resumo de `design/classes-v0.1.md`) e a **prévia animada do sprite** da classe andando (`SpritePreview`, usa o catálogo da T-053).
- **Apagar:** diálogo de confirmação com nome, classe e nível ("Isso não pode ser desfeito").
- **Jogar:** pede o token de jogo (`POST /characters/{id}/play`) e conecta no `address` que a API devolve. Se ele vier vazio (hoje vem, sem `MMO_GAME_SERVER_ADDRESS`), usa o servidor do jogo da tela de entrada. O token vai no `Hello.Ticket` (`OnlineSettings.Ticket` → `NetAuthority`). Ao sair, cair ou ser recusado, volta à lista de personagens (`Main.BackToMenu`) com a mensagem do erro. Sem sessão válida, volta ao login.
- **Modo de teste:** o menu de encontros locais continua, pelo botão "Modo de teste (encontros locais)". O menu ganhou "Entrar com conta" para voltar. Os argumentos de partida direta (`--encounter`, `--server`...) continuam indo direto ao jogo.

**API** (`ApiClient`): cadastro, login, listar, criar, apagar e jogar, com `Authorization: Bearer`. O token de sessão e a conta ficam **só em memória** (campos estáticos, sobrevivem à troca de cena). Senha e tokens nunca vão para disco.

**Erros claros:** os `code` da API viram mensagens em português (senha errada, e-mail já usado, nome em uso ou inválido, limite de personagens, personagem não encontrado). Códigos sem mensagem própria usam o `title` da API. Também há mensagens para:
- servidor de contas fora do ar ou inacessível;
- muitas tentativas (429);
- sessão expirada (401 → volta ao login);
- as recusas novas do servidor do jogo: `Unauthorized`, `AlreadyConnected`, `Unavailable`.

**Outras mudanças:** o corpo dos pedidos vai com tamanho conhecido (`StringContent`), não em blocos (*chunked*), para funcionar com qualquer proxy na frente da API. O `ContentLoader` do cliente passou a carregar `Zones`.

**Como foi verificado:** uma API falsa local, que imita o contrato da T-211 (fora do repositório), mais o `Game.Server` local com `--dev-login`, sem banco. Capturas das telas de entrar, lista, lista vazia, criar (com o sprite), senha errada e servidor de contas fora do ar.
- **Pelo cliente:** cadastro, login, criar personagem e "Jogar". O pedido do token deu 200, o cliente conectou e mandou o token no `Hello`, e o servidor recusou com `Unauthorized`, que é o esperado sem banco. O cliente mostrou "Entrada recusada: o token de jogo é inválido ou expirou...".
- **Só na API falsa, sem tela:** Apagar.

Argumentos de depuração novos (depois de `--`): `--api=url`, `--game-server=host:porta`, `--login=email:senha`, `--register`, `--create=Nome:class.id`, `--play=Nome`, `--account-view=create` e `--capture=arquivo.png`.

**Decisões**
- O endereço da API não fica gravado: o padrão é `http://127.0.0.1:8080` ou `--api=`. Quando a T-213 publicar a API com HTTPS, troque o padrão `DefaultApi` em `AccountScreen` para o endereço público.
- O encontro em `MatchSettings` é irrelevante online (a sala do servidor decide); fica o primeiro da lista.

**Pendências**
- **Teste de ponta a ponta contra a VPS** (critério de aceite). Precisa da API acessível de fora: a T-213 (Caddy), ou um túnel SSH até o contêiner `api`, ou publicar a porta 8080 temporariamente. Também precisa de `MMO_GAME_SERVER_ADDRESS=31.97.162.124:7777` no `deploy/.env`, para a API mandar o endereço.
- Depois que o login com conta estiver validado: `GAME_DEV_LOGIN=false` na VPS (pendência da T-212).
- O botão Apagar não foi testado pela tela, só a chamada à API falsa.
