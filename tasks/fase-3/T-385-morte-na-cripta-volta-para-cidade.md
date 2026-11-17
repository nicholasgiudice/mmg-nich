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

Concluída em 2026-11-17 (VPS), branch `task/T-385-morte-na-cripta-cidade`.

- `WorldServer.MoveThroughGatesAndRespawn`: o morto que pede para renascer vai sempre para o nascimento da zona inicial (`_start`), cheio, sem perder nada, inclusive vindo de uma instância. A saída da instância usa o mesmo `Move` de antes, então a cópia continua para o resto do grupo e segue a regra de `emptyTimeout` se ficar vazia. A volta pelo portão não mudou: o dono da cópia é o grupo (ou o personagem), e o portão leva para a mesma cópia enquanto ela existir.
- Gravação: o `Move` grava ao renascer, agora na Cidade; quem está dentro continua gravado na zona de origem (sem mudança).
- Documentação: comentário do `WorldServer`, `src/Game.Server/README.md` e nota no requisito 5 da T-350 (mudança do designer em 2026-11-16). A nota na T-350 está fora das "Áreas que pode alterar", mas é pedida pelo requisito 5 desta tarefa.
- Teste: `InstanceTests.Death_respawns_at_the_instance_entrance_with_the_monsters_as_they_were` virou `Death_in_an_instance_respawns_in_the_city_and_the_gate_leads_back_to_the_same_copy` (dois personagens com conta no mesmo grupo): o primeiro morre na cópia, pede para renascer e aparece no nascimento da Cidade, com vida cheia e `InstanceId` 0; o segundo continua na cópia; a gravação do primeiro é a Cidade e a do segundo é a zona de origem; o primeiro volta pelo portão e cai na mesma cópia, com a Matriarca ainda ferida. Conferi que o teste falha com a regra antiga.
- Sem pendências. O relatório `tasks/reports/balance-v0.15.md` ainda cita a regra antiga, mas é um registro histórico e ficou como está.

