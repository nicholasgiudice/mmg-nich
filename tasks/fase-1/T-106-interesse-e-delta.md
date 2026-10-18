# T-106 — Interest management por grade e snapshots delta

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-105
- **Libera:** T-109, T-111
- **Áreas que pode alterar:** `src/Game.Server/`, `src/Game.Protocol/`, testes correspondentes

## Contexto

`tech/stack-proposal-v0.1.md`: snapshots delta por entidade e interest management por grade — o jogador recebe só entidades próximas; meta de poucos KB/s por jogador com ~50 entidades visíveis. As arenas do protótipo são pequenas, mas os mapas da fase 3 não.

## Requisitos

1. **Grade de interesse** no servidor (célula configurável, ex.: 16 tiles; raio de interesse ex.: 2 células): cada jogador recebe só as entidades das células próximas, mais as que sempre importam (o próprio grupo, chefes do encontro).
2. **Entrada e saída** do interesse: mensagens de criação/remoção de entidade no cliente (o cliente não pode "perder" entidade sem saber).
3. **Delta por cliente**: envia só componentes que mudaram desde o último snapshot **confirmado** por aquele cliente (ack de tick); snapshot completo periódico ou quando o ack fica velho demais.
4. Eventos também filtrados por interesse (eventos de entidades fora do alcance não vão).
5. Medição: bytes/s por jogador no encontro da Matriarca e num teste sintético com 100 entidades espalhadas; registrar no Resultado.

## Critérios de aceite

- teste: cliente que anda até o fim de um mapa sintético recebe e remove entidades corretamente;
- teste: aplicar a sequência de deltas reproduz o mesmo estado que o snapshot completo (com perda simulada de pacotes);
- banda por jogador medida e registrada;
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
