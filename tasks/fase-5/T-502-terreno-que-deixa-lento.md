# T-502 — Core: terreno que deixa lento

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** —
- **Libera:** T-510 (ordem de integração), T-530, T-540
- **Áreas que pode alterar:** `src/Game.Core/` (movimento, estados, leitura das áreas do mapa), `src/Game.Data/TiledMapReader.cs`, `data/statuses.json`, `data/schemas/`, `data/maps/` (arena de teste nova), `data/encounters.json`, `design/map-conventions-v0.1.md` (§6), `tests/`

## Contexto

O Brejo dos Afogados (`design/mapas-16-40-v0.1.md` §3.1) tem **água funda que deixa lento**. É o truque da área: o jogador aprende a escolher onde lutar. Os homens-lagarto do brejo não sentem a água.

Hoje o mapa aceita áreas de evento (`areas`, `design/map-conventions-v0.1.md` §6) e o Core guarda as propriedades como texto, mas nenhuma área muda o movimento. A classe `web` está no documento como exemplo e não tem mecânica. Lentidão por estado já existe (`status.slow`, `status.mist_chill`), e a predição do cliente (T-108) já lida com velocidade vinda de estado.

## Requisitos

1. Classe de área nova **`slow_terrain`** (retângulo na camada `areas`), com as propriedades:
   - `status` (obrigatória): id do estado aplicado enquanto a unidade está dentro;
   - `ignoreTags` (opcional): tags de monstro que não sentem o terreno, separadas por vírgula (ex.: `amphibious`).
2. Estado novo `status.deep_water`: −40% de velocidade, sem dano. Entra ao pisar na área e sai **no mesmo tick** em que a unidade sai dela (não fica um resto de duração). Vale para jogadores e monstros.
3. Não conta como controle: não sofre retorno decrescente, não é limpo pela Purificação e não é bloqueado pelas imunidades a controle dos elites e chefes. É terreno, não efeito de habilidade. Registre a escolha se o Core pedir outra forma.
4. A IA **não** desvia da água (é a forma mais simples). O jogador usa isso a seu favor. Registre se mudar.
5. Erros de carga: `slow_terrain` sem `status`, ou com um estado que não existe.
6. Documentar a classe em `design/map-conventions-v0.1.md` §6.

## Critérios de aceite

- testes do Core: velocidade cai ao entrar e volta ao sair, no tick certo; monstro com tag em `ignoreTags` não é afetado; duas áreas sobrepostas não acumulam;
- arena de teste com um trecho de água: um encontro do simulador mostra que lutar fora da água rende mais que dentro, para uma classe corpo a corpo;
- o estado aparece no snapshot como os outros (o cliente mostra o ícone sem mudança no protocolo). Se precisar subir `ProtocolVersion`, a partir do número da `main`;
- `./scripts/check.sh` completo passa.

## Resultado

_(a preencher)_
