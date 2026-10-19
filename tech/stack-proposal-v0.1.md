# Proposta de Stack Tecnológica — v0.1

Documento complementar a `tech/architecture-v0.1.md`. Mantém as decisões já tomadas (Godot 4 no cliente, servidor autoritativo, protótipo local antes de qualquer infraestrutura online, sem rAthena) e propõe uma resposta concreta para o que ficou em aberto: **linguagem, servidor, rede, dados e persistência**.

---

## 1. Análise geral do projeto

### O que o jogo exige tecnicamente

| Requisito de design | Implicação técnica |
|---|---|
| Combate em tempo real, visão de cima, movimentação livre | Simulação em ticks fixos, interpolação no cliente, predição de movimento do próprio jogador |
| Habilidades com cooldown e custo de Mana | Sistema de habilidades orientado a dados e a eventos (a proposta original previa recursos próprios por classe gerados por eventos; o designer trocou por Mana para todas, só por tempo — T-027/T-032) |
| Ameaça, controle com redução em elites/bosses, veneno, maldição, medo | Tabela de ameaça, efeitos de status com tags e retorno decrescente |
| Bosses com fases, selos, invocações | Máquina de estados de IA configurável por dados |
| "O jogador sabe por que tomou dano" | Todo efeito gera um evento rastreável → log de combate e feedback visual |
| Grupo ideal de 4, PvE solo possível | Instâncias pequenas (dungeon) + zonas abertas compartilhadas |
| Cliente não decide dano, loot, inventário | Toda regra de jogo roda no servidor; cliente só envia intenções |
| Protótipo deve permitir migrar a autoridade depois | **A lógica de combate não pode depender do Godot** |

### Ponto crítico identificado

O documento atual coloca Godot como cliente e deixa o servidor em aberto (Godot headless, Go, Rust). O risco real não é escolher o servidor errado — é **escrever o combate do protótipo dentro de nós e scripts do Godot** e depois precisar reescrever tudo em outra linguagem para o servidor. Com 4 classes, ~30 habilidades, 10 tipos de inimigo e 2 bosses com fases, essa reescrita seria cara e introduziria divergências de comportamento entre protótipo e jogo online.

A proposta abaixo resolve isso com uma regra simples: **a simulação do jogo é uma biblioteca separada, escrita uma vez, usada pelo cliente no protótipo e pelo servidor depois.**

---

## 2. Stack recomendada (resumo)

> **O motor continua sendo o Godot 4.** "Godot 4 .NET" é a edição oficial do mesmo Godot 4 com suporte a C# (além de GDScript), disponível no site do Godot ao lado da edição padrão. Nada muda em relação à decisão de `architecture-v0.1.md`, apenas qual edição baixar.

| Camada | Escolha | Observação |
|---|---|---|
| Cliente | **Godot 4.x (versão .NET)** | Mantém a decisão atual |
| Linguagem | **C# em todo o projeto** | Cliente, simulação e servidor na mesma linguagem |
| Simulação / regras | **Biblioteca C# pura (`Game.Core`)**, sem dependência do Godot | Coração do projeto |
| Servidor de jogo | **Aplicação console .NET (LTS atual)** | Loop próprio a 20 ticks/s |
| Rede | **UDP com LiteNetLib** | Canais confiáveis e não confiáveis, usado no cliente e no servidor |
| Serialização | **MemoryPack** (ou MessagePack-CSharp) | Binário, rápido, gerado em tempo de compilação |
| Dados de design | **JSON versionado no repositório + JSON Schema** | Lido pelo cliente, servidor e testes |
| Mapas | **Tiled (export JSON)** como fonte de verdade | Godot importa para renderizar; servidor lê colisão e spawns |
| Banco de dados | **PostgreSQL** | Contas, personagens, inventário, progresso |
| Autenticação/meta | **ASP.NET Core (minimal API)** | Login, lista de personagens, emissão de token de sessão |
| Testes | **xUnit + simulador headless de encontros** | Valida balanceamento sem abrir o jogo |
| Infra inicial | **Docker Compose na VPS Linux existente (Ubuntu 24.04)** | Kubernetes só se um dia for necessário — ver seção 10 |
| Proxy/TLS | **Caddy** | HTTPS automático para a API; o jogo usa UDP direto |
| Observabilidade | **Serilog + OpenTelemetry → Prometheus/Grafana** | Entra na fase online |

---

## 3. Por que C# em todo o projeto

1. **Código compartilhado de verdade.** O Godot .NET consegue referenciar projetos C# comuns. A mesma `Game.Core` roda dentro do cliente (protótipo e predição) e dentro do servidor (autoridade). Nenhuma regra é escrita duas vezes.
2. **Servidor sem o peso de um motor.** O servidor é um processo .NET comum: inicia rápido, usa pouca memória, é fácil de testar, depurar e perfilar, e não carrega SceneTree, renderização ou física do Godot.
3. **Desempenho suficiente com folga.** .NET moderno é muito eficiente para esse tipo de carga (centenas de entidades por zona a 20 Hz), com GC previsível se a simulação evitar alocações no loop.
4. **Ecossistema maduro** para tudo o que vem depois: rede (LiteNetLib), banco (Npgsql/EF Core ou Dapper), HTTP (ASP.NET Core), testes, observabilidade.
5. **Tipagem estática** ajuda num domínio com muitas regras cruzadas (status, ameaça, recursos, fases de boss).

### Custo assumido

- **Exportação Web:** o Godot 4 com C# não exporta para navegador. Se jogar no browser for um objetivo, esta proposta precisa ser revista (ver seção 9). Para PC (Windows/Linux/macOS) não há problema.
- **GDScript fica de fora da lógica de jogo.** Pode continuar sendo usado em ferramentas de editor ou efeitos puramente visuais, mas a recomendação é padronizar em C# para evitar duas linguagens.

---

## 4. Arquitetura

### Visão por fase

```text
FASE 0 — Protótipo local (agora)

  Godot (C#)
  ├── Apresentação: sprites, animações, UI, áudio, input
  └── LocalAuthority ──► Game.Core (simulação completa, in-process)


FASE 1+ — Online

  Godot (C#)                                 Servidor de Zona (.NET)
  ├── Apresentação                           ├── Loop fixo 20 Hz
  ├── Predição de movimento ─► Game.Core     ├── Game.Core (autoridade)
  └── NetClient ◄──── UDP / LiteNetLib ────► ├── Interest management (AOI)
                                             └── Persistência assíncrona
                                                     │
                     HTTPS                           ▼
  Godot ◄──────────────────► Auth/Meta API ──► PostgreSQL
                             (ASP.NET Core)
```

A troca entre `LocalAuthority` e `NetClient` acontece atrás de uma única interface (por exemplo `IGameAuthority`): o cliente envia **intenções** (mover, usar habilidade X no alvo/direção Y, interagir) e recebe **estado e eventos**. No protótipo a autoridade é local; online é o servidor. O código de apresentação não muda.

### Game.Core — a simulação

Responsabilidades separadas, como pede `architecture-v0.1.md`:

```text
Intenção (UseAbility, Move, Interact)
   ↓
Validação  — cooldown, recurso, alcance, linha de visão, estado (atordoado, canalizando)
   ↓
Execução   — efeitos: Dano, Cura, Escudo, AplicarStatus, Deslocamento, Invocar, Ameaça
   ↓
Eventos    — DamageDealt, StatusApplied, Interrupted, ResourceGained, PhaseChanged...
   ↓
Consumidores — geração de recurso por evento, IA, log de combate, rede, apresentação
```

Diretrizes:

- **Estado em dados simples** (entidades com componentes de vida, recurso, posição, status, ameaça), sistemas que processam esse estado a cada tick. Não é necessário adotar um framework ECS completo no início; uma estrutura simples de entidades + sistemas resolve. Se a escala exigir, bibliotecas como Arch podem ser avaliadas depois.
- **Tick fixo e determinístico o suficiente:** relógio da simulação controlado pelo tick, RNG com seed injetada, nada de `DateTime.Now` ou física do Godot dentro da regra.
- **Colisão e movimento próprios:** colisão simples (círculos contra grade/polígonos do mapa) e pathfinding A* em grade dentro do Core. A física do Godot não deve decidir nada que o servidor precise concordar.
- **Recursos configuráveis por dados:** o Core suporta ganho de recurso por eventos (bloqueio, interrupção, purificação, acerto em alvo marcado — `resourceRules`), mas o design atual usa só regeneração de Mana por tempo (decisão de 2026-10-19, T-032). O mecanismo fica disponível para efeitos futuros (ex.: roubo de Mana em equipamento, `design/ideias-futuras.md`).
- **Status com tags** (`veneno`, `maldição`, `controle`, `medo`) e **retorno decrescente** para controle em elites e bosses.
- **IA por máquina de estados** descrita em dados: fases do boss por limiar de vida/tempo, rotação de ataques, invocações, condições de falha (selos destruídos).
- **Telegráfos como entidades:** cones, linhas e áreas de aviso existem na simulação com tempo de preparação, para que cliente e servidor concordem sobre o que acerta quem.

### Rede

- **Servidor autoritativo com tick de 20 Hz**; cliente renderiza a 60+ FPS interpolando entidades remotas com ~100 ms de atraso.
- **Predição apenas do movimento do próprio jogador**, com reconciliação quando o servidor corrige a posição. Habilidades tocam animação imediatamente (feedback), mas dano, cura e status só aparecem quando o servidor confirma.
- **Mensagens:** canal não confiável para snapshots de posição; canal confiável e ordenado para eventos de combate, inventário e chat futuro.
- **Snapshots delta** por entidade e **interest management por grade** (o jogador recebe apenas entidades próximas). Com ~50 entidades visíveis, a banda por jogador fica na casa de poucos KB/s.
- **Validação no servidor:** velocidade máxima de movimento, alcance e linha de visão de habilidades, cooldowns, recurso. O cliente nunca envia dano, posição final de projétil ou resultado de loot.

### Topologia do mundo

O primeiro mapa encaixa naturalmente em processos separados:

| Processo | Conteúdo | Tipo |
|---|---|---|
| Zona: Cidade-Fortaleza | hub seguro, NPCs, mercado | compartilhada |
| Zona: Campos da Vigília | área inicial | compartilhada |
| Zona: Floresta Baixa | inclui Ninho da Matriarca | compartilhada |
| Zona: Ruínas da Estrada Antiga | inclui Torre Quebrada | compartilhada |
| Instância: Cripta do Vigia | dungeon + boss | uma por grupo, sob demanda |

No começo, **todas as zonas podem rodar no mesmo processo** (cada uma com seu loop). A separação em processos/máquinas só é necessária quando houver carga real. A transição entre zonas é feita pelos portões da cidade, o que torna o handoff de jogador simples (salvar estado → conectar na nova zona).

### Persistência

- **PostgreSQL** como banco único: `accounts`, `characters`, `inventory_items`, `equipment`, `quest_progress`, `unlocks` (chave da Cripta permanente).
- Colunas relacionais para o que é consultado e protegido por integridade (itens, moedas); `jsonb` para dados flexíveis de progresso.
- O servidor de zona mantém o estado em memória e **grava de forma assíncrona** em pontos definidos (troca de zona, loot, transação, intervalo periódico, logout). Operações de item usam transação para evitar duplicação.
- Redis **não é necessário** no MVP. Entra só se houver presença/chat entre muitos processos.

### Dados de design

Classes, habilidades, monstros, tabelas de loot e encontros ficam em `data/*.json`, validados por JSON Schema e carregados pelo `Game.Core`.

Motivos para não usar Resources do Godot (`.tres`) como fonte de verdade: o servidor e os testes não dependem do Godot, e JSON facilita revisão em diff, validação em CI e futuras ferramentas de balanceamento.

Exemplo do formato esperado:

```json
{
  "id": "warrior.heavy_strike",
  "class": "warrior",
  "cost": { "resource": "mana", "amount": 30 },
  "cooldown": 6.0,
  "castTime": 0.4,
  "targeting": { "type": "melee_arc", "range": 1.6, "angle": 90 },
  "effects": [
    { "type": "damage", "school": "physical", "base": 42, "scaling": { "attack": 1.2 } },
    { "type": "interrupt", "tags": ["cast"] }
  ]
}
```

### Mapas

Recomendação: **Tiled** como editor e fonte de verdade, exportando JSON.

- O cliente importa o mapa no Godot (plugin de importação) para renderizar tiles, decoração e iluminação.
- O servidor lê o mesmo JSON para extrair **camada de colisão, grade de navegação, pontos de spawn, áreas de evento** (altares, selos, teias) e transições de zona.

Alternativa aceitável: desenhar direto com `TileMapLayer` no Godot e manter um script de editor que exporta colisão/spawns para JSON. Funciona, mas cria uma etapa de exportação que pode ficar desatualizada; com Tiled o arquivo é o mesmo para os dois lados.

---

## 5. Testes e balanceamento

O design já define critérios mensuráveis (encontros 1–5, matriz de decisões, "o boss não pode morrer sem o grupo se mover"). Como a simulação é independente do Godot, dá para testá-los automaticamente:

- **Testes unitários (xUnit)** para regras: custo e cooldown, geração de recurso por evento, redução de controle em boss, ameaça.
- **Simulador headless de encontros (`Game.Sim`)**: roda os cinco cenários com bots simples (estratégias "ficar parado", "só ataque básico", "usar rotação ideal") milhares de vezes e mede tempo para matar, dano recebido, taxa de vitória.
  - Exemplo de verificação direta do design: *"se o Guerreiro vence os 3 Lobos usando só Provocação + ataque básico, o combate está raso"* vira um teste que falha quando isso acontece.
- **Testes de carga** (fase online): clientes bot conectando por UDP para medir tick time e banda por zona.

---

## 6. Estrutura de repositório sugerida

```text
mmo-rpg/
├── client/                    # projeto Godot (.NET)
│   ├── project.godot
│   ├── scenes/
│   ├── presentation/          # nós, animação, UI, VFX — sem regras de jogo
│   └── Client.csproj          # referencia src/Game.Core
├── src/
│   ├── Game.Core/             # simulação pura (sem Godot)
│   ├── Game.Data/             # carregamento e validação de data/*.json
│   ├── Game.Protocol/         # mensagens de rede compartilhadas (fase 1)
│   ├── Game.Server/           # servidor de zona (fase 1)
│   └── Game.Api/              # auth/meta HTTP (fase 2)
├── tests/
│   ├── Game.Core.Tests/
│   └── Game.Sim/              # simulador de encontros
├── data/                      # classes, habilidades, monstros, loot, encontros
├── maps/                      # arquivos Tiled
├── docs/  design/  tech/
└── MmoRpg.sln
```

---

## 7. Roadmap técnico

| Fase | Entrega | Stack envolvida |
|---|---|---|
| **0 — Protótipo de combate** | Arena, 4 classes com 3 habilidades, 3 inimigos, boss simplificado, tudo local | Godot .NET + `Game.Core` + `Game.Data` + testes |
| **0.5 — Validação** | Simulador dos 5 encontros, ajuste de números, log de combate visível | `Game.Sim` |
| **1 — Rede mínima** | Servidor dedicado, 4 jogadores na mesma arena, login de desenvolvimento, sem banco | `Game.Server`, LiteNetLib, `Game.Protocol` |
| **2 — Persistência** | Contas, personagens, inventário, Cidade + Campos online | PostgreSQL, `Game.Api`, Docker Compose |
| **3 — Primeiro mapa completo** | Floresta, Ruínas, Cripta instanciada, Vigia Sem Rosto | Múltiplas zonas, instâncias sob demanda |
| **4 — Operação** | Métricas, logs, deploy automatizado, testes de carga | OpenTelemetry, Grafana, CI/CD |

A fase 0 já segue a regra do `vision-mvp-v0.1.md`: nada de servidor, login ou banco antes do combate provar que é divertido. A diferença é que o código escrito agora sobrevive inteiro às fases seguintes.

---

## 8. Alternativas avaliadas

| Opção | Prós | Contras | Veredito |
|---|---|---|---|
| **Godot headless como servidor (GDScript)** | Zero duplicação, uma ferramenta só | SceneTree e física do motor no servidor, desempenho de GDScript, mais difícil testar fora do motor, escala por processo limitada | Viável para o slice; plano B se a equipe preferir GDScript |
| **Servidor em Go** | Ótimo para rede e concorrência, deploy simples | Regras de combate escritas duas vezes (Go no servidor, GDScript/C# no cliente para predição e protótipo) | Não recomendado pelo custo de duplicação |
| **Servidor em Rust** | Máximo desempenho e segurança de memória | Curva de aprendizado alta, duplicação ou integração via gdext complexa; desempenho extra não é necessário nesta escala | Não recomendado para o MVP |
| **Nakama** | Contas, amigos, grupos, chat e matchmaking prontos | Lógica autoritativa em tempo real via módulos Go/Lua/TS, novamente fora do C# | Pode substituir `Game.Api` no futuro para recursos sociais |
| **SpacetimeDB** | Banco + servidor em um, modelo pensado para MMO | Modelo de programação incomum, ecossistema jovem, aposta arriscada para o núcleo | Acompanhar, não adotar agora |
| **Colyseus (Node.js)** | Rápido de começar | TypeScript separado do cliente, mesma duplicação | Não recomendado |
| **Unity / Unreal** | Ecossistemas grandes | Contradiz a decisão já tomada; Godot atende bem 2D | Manter Godot |

---

## 9. Decisões tomadas nesta proposta

1. **Linguagem: C# em todo o projeto.** Cliente (Godot 4 .NET), simulação, servidor e API. GDScript não é usado para regras de jogo.
2. **Plataforma alvo: PC (Windows e Linux).** É onde um MMO de combate em tempo real com teclado/mouse faz mais sentido, e é totalmente suportado pelo Godot .NET. Navegador e mobile ficam fora do MVP; se um dia entrarem, o cliente é que se adapta — `Game.Core` e servidor não mudam.
3. **Mapas: Tiled (JSON) como fonte de verdade.** Um único arquivo lido pelo cliente e pelo servidor, sem etapa de exportação para manter sincronizada.
4. **Desenvolvimento e hospedagem: VPS Linux** com .NET, Docker Compose e PostgreSQL; editor do Godot e Tiled na máquina local (seção 10).

### Ainda em aberto (não afeta código)

- **Estilo de arte 2D:** pixel art (Aseprite) ou ilustrado (Krita/Spine). Afeta apenas o pipeline de animação no Godot.

---

## 10. Ambiente: VPS Linux

O projeto é desenvolvido e será hospedado numa VPS **Ubuntu 24.04 LTS, 2 vCPU, 8 GB RAM, ~96 GB de disco, sem interface gráfica**. A stack proposta se encaixa bem nesse ambiente, com uma ressalva importante sobre o editor do Godot.

### O que roda na VPS e o que roda na máquina local

| Atividade | Onde | Motivo |
|---|---|---|
| `Game.Core`, `Game.Data`, testes, `Game.Sim` | **VPS** | Só precisam do SDK .NET; nenhuma dependência gráfica |
| Servidor de jogo, API, PostgreSQL | **VPS** | Linux é o ambiente nativo para tudo isso |
| Build/export do cliente | **VPS** (`godot --headless --export-release`) | Godot exporta sem tela; gera builds Windows/Linux para os jogadores |
| Editor do Godot (cenas, animações, UI) | **Máquina local com tela** | O editor precisa de interface gráfica |
| Tiled (mapas), arte, playtest | **Máquina local** | Idem |

Consequência prática: a separação entre regras (`Game.Core`) e apresentação (Godot) deixa de ser só uma boa prática e passa a ser o que permite desenvolver a maior parte do jogo direto na VPS. Regras, balanceamento e servidor são escritos e testados ali; a máquina local fica para a parte visual. O repositório Git é o ponto de sincronização entre as duas.

### Instalação base na VPS

- **.NET SDK (LTS atual)** pelos pacotes oficiais do Ubuntu/Microsoft;
- **Docker Engine + Docker Compose plugin**;
- **Godot .NET headless + export templates** (somente quando for automatizar builds do cliente);
- **ufw** como firewall.

### Dimensionamento

Para as fases 0 a 2 (e provavelmente a 3), a VPS atual é suficiente:

| Serviço | Memória estimada | CPU |
|---|---|---|
| PostgreSQL | 300–500 MB | baixo |
| Servidor de zonas (.NET, todas as zonas num processo) | 200–600 MB | 1 núcleo dá conta de dezenas de jogadores a 20 Hz |
| API (ASP.NET Core) | ~100 MB | baixo |
| Caddy | ~30 MB | baixo |
| Builds/testes durante desenvolvimento | picos de 1–2 GB | usa os 2 núcleos temporariamente |

O gargalo provável é CPU durante builds coincidindo com servidor em uso. Quando houver jogadores reais, separar **desenvolvimento e produção** (segunda VPS ou, no mínimo, não compilar na mesma máquina em horário de jogo).

### Implantação

```text
VPS Ubuntu
├── Caddy        :443/tcp  → api (HTTPS automático)
├── api          (rede interna do Compose)
├── game-server  :7777/udp (exposto diretamente; UDP não passa pelo Caddy)
└── postgres     (somente rede interna — nunca exposto)
```

- **Docker Compose** para tudo; o servidor de jogo pode alternativamente rodar como serviço **systemd** se o Docker atrapalhar o UDP ou o debug.
- **Firewall:** liberar apenas SSH, 443/tcp e a porta UDP do jogo.
- **Serviços sem root:** contêineres e processos rodando com usuário próprio.
- **Backup:** `pg_dump` agendado via cron, copiado para armazenamento fora da VPS (backup na mesma máquina não protege contra perda da VPS).
- **Latência:** para jogadores no Brasil, a VPS deve estar em datacenter em São Paulo ou o mais próximo possível — combate em tempo real é sensível a ping.
- **CI/CD:** GitHub Actions (ou script na própria VPS) para rodar testes, gerar builds do cliente e reiniciar o servidor.

---

## 11. Recomendação final

Adotar **Godot 4 .NET + C# em todo o projeto**, com a simulação de combate isolada numa biblioteca `Game.Core` sem dependência do motor. Começar exatamente pelo protótipo local já previsto, mas com essa separação desde o primeiro commit. Quando o combate for validado, o servidor dedicado .NET com LiteNetLib reaproveita 100% das regras, e PostgreSQL + uma API ASP.NET Core cobrem contas e persistência do MVP sem infraestrutura desnecessária.
