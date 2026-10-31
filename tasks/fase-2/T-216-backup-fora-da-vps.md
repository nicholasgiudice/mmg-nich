# T-216 — Backup do banco fora da VPS (destino a escolher)

- **Ambiente:** VPS
- **Modelo:** Humano + Leve
- **Depende de:** T-213
- **Libera:** —
- **Áreas que pode alterar:** `deploy/`, `scripts/`, `README.md`

## Contexto

A T-213 deixou o `scripts/backup-db.sh` (dump compactado com data, mantém os últimos N, agendamento e restauração testada) com o **destino da cópia fora da VPS configurável, mas não definido**. O humano decidiu escolher o destino depois. Esta tarefa fecha essa pendência: sem ela, o backup fica só na própria VPS e se perde junto com ela.

Não bloqueia o playtest interno (T-230). Convém concluir antes de abrir o jogo a jogadores de fora, porque a partir daí os personagens deles passam a ter valor.

## Decisão do humano (preencher antes de executar)

Escolher **um** destino e anotar aqui:

| Opção | Como funciona | Observações |
|---|---|---|
| Outro servidor por `rsync`/SSH | o script envia o dump por SSH para outra máquina | precisa de uma segunda máquina ligada; chave SSH só com permissão de escrita na pasta do backup |
| Armazenamento de objetos (S3 compatível: Backblaze B2, Cloudflare R2, AWS S3...) | o script envia com `rclone` ou `aws s3 cp` | pago por GB (centavos para este volume); usar chave restrita a um bucket |
| Computador pessoal (Windows) puxando da VPS | uma tarefa agendada no Windows baixa o dump por `scp` | só copia quando o computador está ligado |

Destino escolhido: _(preencher)_

## Requisitos

1. Configurar o destino escolhido nas variáveis do `deploy/.env` que a T-213 criou. As credenciais ficam só na VPS, nunca no repositório.
2. A credencial usada pelo backup só pode escrever no destino do backup (bucket, pasta ou usuário dedicado).
3. Retenção no destino: os últimos N dumps (padrão: 14 diários), definida por variável ou pela regra de ciclo de vida do armazenamento.
4. Falha no envio fica registrada no log do agendamento e não apaga os dumps locais.
5. Documentar no README como recuperar um dump do destino e restaurar.

## Critérios de aceite

- backup diário chegando ao destino fora da VPS, conferido em dois dias seguidos;
- um dump baixado do destino restaurado num banco vazio, com contas e personagens presentes;
- README atualizado.

## Resultado

_(preencher ao concluir)_
