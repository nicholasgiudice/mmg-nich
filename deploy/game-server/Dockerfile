# Servidor de zona (src/Game.Server). Contexto do build: raiz do repositório.
# Uso: docker compose -f deploy/compose.yml build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo
COPY global.json Directory.Build.props ./
COPY src/ src/
RUN dotnet publish src/Game.Server -c Release -o /out

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /out ./
COPY data/ /app/data/
ENV MMO_DATA_DIR=/app/data
USER $APP_UID
EXPOSE 7777/udp
ENTRYPOINT ["dotnet", "Game.Server.dll"]
