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

_(preencher ao concluir)_
