# T-501 — Servidor: renascer no ponto seguro da zona

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** —
- **Libera:** T-531, T-532, T-533, T-541
- **Áreas que pode alterar:** `src/Game.Core/Definitions/Zones.cs`, `src/Game.Data/` (leitura e validação de `zones.json`), `data/schemas/`, `data/zones.json`, `src/Game.Server/Zones/`, `src/Game.Server/README.md`, `design/map-conventions-v0.1.md` (§11), `tests/`

## Contexto

Hoje quem morre e pede para renascer vai sempre para o nascimento da zona inicial, a Cidade (`WorldServer.MoveThroughGatesAndRespawn`, T-385), venha de onde vier, inclusive de uma instância.

Com a região do sul (`design/mapas-16-40-v0.1.md` §2 e §8, decisão 6), quem morre da Serra Partida em diante renasce no **Forte do Passo**, o segundo ponto seguro. A decisão do designer fala "do 21 em diante". A regra fica **por zona**, não pelo nível do personagem: é mais simples e o jogador entende ("morri na Serra, volto ao Forte").

## Requisitos

1. Campo opcional `respawn` em `zones.json`: id de uma zona `safe`. Sem o campo, vale a zona inicial (a Cidade), como hoje.
2. Validação na carga: `respawn` precisa apontar para uma zona que existe e é `safe`. Zona `safe` com `respawn` é erro.
3. O servidor manda quem renasce para o nascimento (`spawn`) da zona do campo `respawn`, cheio e sem perder nada, como hoje. Vale também para as instâncias (cada instância tem o próprio `respawn`).
4. Nenhuma zona de hoje muda: Campos, Floresta, Covil, Ruínas e Cripta continuam renascendo na Cidade. As zonas novas (T-531 a T-533) põem o campo.
5. Documentar o campo em `design/map-conventions-v0.1.md` §11 e no README do servidor.

## Critérios de aceite

- testes de servidor: morrer numa zona com `respawn` leva à zona indicada; sem o campo, à Cidade; numa instância com `respawn`, sai da cópia e vai à zona indicada, e o portão leva de volta à mesma cópia (como o teste da T-385);
- testes de carga: `respawn` para zona inexistente, para zona que não é `safe`, e em zona `safe` dão erro com o arquivo e o campo;
- não precisa subir `ProtocolVersion` (o cliente lê `zones.json` nos dados). Se subir, a partir do número da `main`;
- `./scripts/check.sh` completo passa.

## Resultado

Feito em 2026-11-24, no Windows, na branch `task/T-501-renascer-no-ponto-seguro`.

- **Definição:** `ZoneDefinition.Respawn` (string opcional; no JSON, `respawn`), com a descrição no schema (`data/schemas/game-data.schema.json`).
- **Validação** (`GameDataValidator.ValidateZone`), com o erro no caminho `zones[n].respawn` do `zones.json`:
  - zona desconhecida;
  - zona que não é `safe`;
  - zona `safe` com o campo.
- **Servidor** (`WorldServer.RespawnZoneOf`):
  - quem pede para renascer vai ao nascimento da zona do `respawn` de onde morreu (aberta ou cópia de instância). Sem o campo, vai à zona inicial (`--start-zone`, padrão a Cidade), como antes;
  - o morto que sai do jogo é gravado nessa mesma zona, na posição (0, 0), que na entrada vira o nascimento.
  - Se a zona do campo não estiver no servidor (só acontece com dados de teste montados à mão, porque a carga já confere), vale a zona inicial.
- **Instância:** a cópia continua para o resto do grupo, e o portão leva de volta à mesma cópia, como na T-385.
- **Dados:** nenhuma zona de hoje mudou. O campo entra nas zonas novas (T-531 a T-533).
- **Protocolo:** não mudou. A tela de morte do cliente (T-541) lê o campo dos dados que o cliente já tem.
- **Documentação:** `design/map-conventions-v0.1.md` §11 e `src/Game.Server/README.md` (morte e instância).
- **Testes:**
  - carga (`ZoneLoaderTests`): o campo é lido, a ausência dá null, e os três erros;
  - servidor (`RespawnZoneTests`, novo): os dados reais ainda não têm uma segunda zona segura, então o teste cria `zone.test_fort`, uma cópia da Cidade, e aponta para ela os Campos e a instância de teste. Casos: morrer nos Campos renasce cheio no nascimento do Forte; o morto com conta é gravado no Forte; morrer na instância renasce no Forte, e o portão do Forte leva de volta à mesma cópia; a Floresta, sem o campo, continua renascendo na Cidade;
  - os 3 casos com `respawn` falham com a regra antiga (conferido desfazendo só o `WorldServer`).
- **Desvios:** nenhum fora das áreas da tarefa.
- **Verificação:** `./scripts/check.sh` completo: formatação e build passam, e todos os projetos de teste passam menos o `Game.Design.Tests`, com 4 falhas em 304.
- **Pendências:** nenhuma desta tarefa. Os 4 testes de design que já falhavam na `main` (registrados na T-500) continuam falhando; nenhum deles toca em renascimento.
