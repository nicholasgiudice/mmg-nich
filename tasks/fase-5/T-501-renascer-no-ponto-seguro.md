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

_(a preencher)_
