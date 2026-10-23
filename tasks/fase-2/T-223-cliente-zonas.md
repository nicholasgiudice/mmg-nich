# T-223 — Cliente: troca de zona, Cidade e Campos

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** T-214, T-220
- **Libera:** T-230
- **Áreas que pode alterar:** `client/`

## Contexto

O servidor da T-214 tem várias zonas e portões. O cliente precisa trocar de mapa sem voltar ao menu.

## Requisitos

1. Ao trocar de zona: tela curta de transição, mapa novo (arena da zona), câmera e interesse recomeçam.
2. Indicação visual dos portões e do nome da zona.
3. Morte no mundo aberto: tela com "Renascer na Cidade".
4. Mapas do Tiled (T-052) se existirem; senão os provisórios.

## Critérios de aceite

- ir da Cidade aos Campos e voltar online, morrer e renascer;
- `dotnet build client/` e `./scripts/check.sh` passam.

## Resultado

_(preencher ao concluir)_
