# T-385 — Morte na Cripta renasce na Cidade

- **Ambiente:** VPS
- **Modelo:** Médio
- **Depende de:** —
- **Libera:** —
- **Áreas que pode alterar:** `src/Game.Server/`, `tests/Game.Server.Tests/`, `src/Game.Server/README.md`

## Contexto

Relato do designer no playtest da fase 3 (T-380), em 2026-11-16: ao morrer na Cripta, o personagem renasce dentro da Cripta, e não na Cidade.

É a regra que a T-350 escolheu (requisito 5: "renasce na entrada da instância"), que deixou registrado que o designer podia preferir outra. Ele prefere a Cidade. O ponto está em `WorldServer.MoveThroughGatesAndRespawn`:

```csharp
var home = p.Zone!.InstanceId > 0 ? p.Zone : _start;
```

## Requisitos

1. Morto numa instância que pede para renascer vai para o **nascimento da zona inicial** (a Cidade), cheio, sem perder nada, como em qualquer outra zona.
2. A instância continua existindo para o resto do grupo. Se ficar vazia, segue a regra da T-350 (some depois de `emptyTimeout`).
3. O personagem pode voltar a entrar pelo portão: se a instância do grupo ainda existir, entra na mesma cópia, com os monstros como estavam (regra da T-350, sem mudança).
4. A posição gravada continua sendo a da zona de origem, nunca a instância.
5. Atualizar o comentário do `WorldServer`, o `src/Game.Server/README.md` e o requisito 5 da T-350 (nota de que o designer mudou a regra em 2026-11-16).

## Critérios de aceite

- teste de servidor: personagem morre na instância, pede para renascer e aparece na Cidade; outro membro do grupo continua na instância; o primeiro volta pelo portão e cai na mesma cópia;
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
