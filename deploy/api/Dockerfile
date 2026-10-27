# API de contas e personagens (src/Game.Api, T-211). Contexto do build: raiz do repositório.
# Uso: docker compose -f deploy/compose.yml build api

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo
COPY global.json Directory.Build.props ./
COPY src/ src/
RUN dotnet publish src/Game.Api -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out ./
COPY data/ /app/data/
ENV MMO_DATA_DIR=/app/data \
    ASPNETCORE_HTTP_PORTS=8080
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Game.Api.dll"]
