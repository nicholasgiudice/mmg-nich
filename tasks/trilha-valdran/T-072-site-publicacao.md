# T-072 — Publicar o site em valdran.nich.click

- **Ambiente:** VPS
- **Modelo:** Humano + Leve
- **Depende de:** T-071; DNS de `valdran.nich.click` apontando para a VPS (humano)
- **Libera:** —
- **Áreas que pode alterar:** `deploy/caddy/Caddyfile`, `deploy/compose.yml`, `deploy/.env.example`, `scripts/build-site.sh`, `scripts/deploy-dev.sh`, `README.md`

## Contexto

A T-213 pôs o Caddy na frente da API (`api.nich.click`) e deixou no `deploy/caddy/Caddyfile` um modelo comentado para um segundo site (`{$MMO_SITE_DOMAIN}`, `root * /srv/site`, `file_server`). A T-071 gera o site estático em `site/dist/`. Falta servir esse site em **`https://valdran.nich.click`**.

**Parte do humano (antes de começar):** criar o registro DNS `A` de `valdran.nich.click` para o IP da VPS (o mesmo de `api.nich.click`, `31.97.162.124` segundo a T-213) e confirmar com `dig +short valdran.nich.click`. Sem isso o Let's Encrypt não emite o certificado.

## Requisitos

1. Caddyfile: bloco `{$MMO_SITE_DOMAIN}` com `import common`, `root * /srv/site` e `file_server`, mais cabeçalhos de cache razoáveis para CSS e imagens. A página 404 deve ser a do site, se a T-071 gerar uma.
2. Compose: `MMO_SITE_DOMAIN` passado ao serviço `caddy` e o diretório do site montado só para leitura em `/srv/site`. Fica numa pasta fora do repositório, por exemplo `/srv/valdran-site`, para o deploy trocar o conteúdo sem reiniciar o Caddy. Variável no `deploy/.env.example`.
3. Atualização: `scripts/build-site.sh --publish` (ou passo no `scripts/deploy-dev.sh`) gera o site e troca o conteúdo publicado de forma atômica, gerando numa pasta nova e trocando um link simbólico, sem meia página no ar. Rodar o deploy de novo com os mesmos dados não deve mudar nada.
4. Não mexer no bloco da API nem nas portas: 80 e 443 já estão abertas.

## Critérios de aceite

- `curl -I https://valdran.nich.click/` → `200`, certificado válido do Let's Encrypt para `valdran.nich.click`; `http://` redireciona para `https://`;
- páginas de classe, habilidade, monstro e item abrem no navegador do celular e do computador;
- `https://api.nich.click/` continua respondendo como antes;
- mudar um número em `data/`, rodar a atualização e ver o site novo no ar;
- README: seção "Site do jogo" com DNS, variável, como publicar e como voltar à versão anterior.

## Resultado

Concluída em 2026-11-03 (VPS). O site está no ar em https://valdran.nich.click.

**Antes de começar:** `dig +short valdran.nich.click` deu `31.97.162.124`, o mesmo IP de `api.nich.click` e da VPS. A T-071 foi integrada em `main` (merge `7bc8316`) antes de esta branch sair de `main`.

**O que foi feito**

- `deploy/caddy/Caddyfile`: novo bloco `{$MMO_SITE_DOMAIN}` com `import common`, `root * /srv/site/current` e `file_server`. Cache: HTML e `/` com `no-cache`, CSS e JS com `max-age=3600`, imagens com `max-age=86400`. O comentário-modelo da T-213 saiu, porque virou o bloco de verdade. O bloco da API ficou como estava.
- `deploy/compose.yml`: o serviço `caddy` recebe `MMO_SITE_DOMAIN` (obrigatória, como `MMO_API_DOMAIN`) e monta `${MMO_SITE_DIR:-/srv/valdran-site}` só para leitura em `/srv/site`. As portas não mudaram. `deploy/.env.example` ganhou as duas variáveis.
- `scripts/build-site.sh`:
  - `--publish` gera em `releases/.tmp-<pid>`, calcula o id (hash de caminhos e bytes dos arquivos), guarda em `releases/<id>/` e troca `current` com `ln -s` num nome temporário seguido de `mv -T` (rename atômico);
  - o mesmo conteúdo dá o mesmo id, então "nada mudou" e nada é tocado;
  - ficam as 5 versões mais recentes do histórico (`MMO_SITE_KEEP`);
  - `--rollback` volta à versão anterior (repetir volta mais uma); `--list` lista as versões;
  - `MMO_SITE_DIR` vem do ambiente, senão de `deploy/.env`, senão `/srv/valdran-site`;
  - sem opção, gera em `site/dist/` como antes.
- `scripts/deploy-dev.sh`: depois do `docker compose up -d`, roda `./scripts/build-site.sh --publish`.
- README: a seção "Site do jogo" ganhou "Publicação na VPS", com DNS, variáveis, como publicar, como voltar, cache e como recriar só o Caddy.

**Na VPS (fora do Git)**

- `deploy/.env`: acrescentado `MMO_SITE_DOMAIN=valdran.nich.click`. Esse arquivo não está nas áreas da tarefa, mas sem a variável o Compose não sobe.
- Publicado em `/srv/valdran-site` (versão `e1da2d5f9b0b`). O Caddy foi recriado com `docker compose -f deploy/compose.yml up -d --no-deps caddy`, depois de validar o Caddyfile com `caddy validate` num contêiner descartável. A API ficou fora do ar só pelos segundos da recriação. Os outros contêineres não foram tocados.

**Verificação (critérios de aceite)**

- `curl -I https://valdran.nich.click/` → `HTTP/2 200`. Certificado do Let's Encrypt (`CN = valdran.nich.click`, emissor YE2, válido até 2027-02-01). `http://` → `308` para `https://`.
- Páginas de classe, habilidade, monstro e item, créditos, CSS e sprites respondem 200 por HTTPS, com os cabeçalhos de cache esperados.
- `https://api.nich.click/` continua com `{"service":"mmo-api","status":"ok"}` no GET e 405 no HEAD, igual a antes.
- Atualização ao vivo: mudei a vida do Lobo para 221 em `data/monsters.json`, rodei `--publish` e o site mostrou 221. Com `--rollback` voltou a 220. Restaurei o dado, e um novo `--publish` disse "nada mudou" (mesmo id `e1da2d5f9b0b`). O dado não foi commitado.
- `./scripts/check.sh` completo passou.

**Desvios e decisões**

- O `root` é `/srv/site/current`, e não `/srv/site` como na tarefa. O Docker resolve um link simbólico na hora de montar, então montar `current` direto prenderia a versão. Por isso a pasta inteira é montada e o Caddy segue o link a cada pedido.
- Página 404: a T-071 não gera uma, então ficou a 404 padrão do Caddy (corpo vazio). Se o gerador ganhar um `404.html`, basta um `handle_errors { rewrite * /404.html; file_server }` no bloco.
- O CSS e o JS não têm hash no nome, por isso o cache é de só 1 h. Com nome com hash dá para usar `immutable`.

**Pendências**

- Abrir no navegador do celular e do computador (a VPS não tem tela). Pelo `curl`, todas as seções respondem.
- Os commits estão só nesta branch e na `main` local; o push fica com o humano.

