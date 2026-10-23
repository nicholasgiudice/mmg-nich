# Migrations do banco (T-210): bundle do EF Core (src/Game.Persistence) num executável autocontido.
# Contexto do build: raiz do repositório. Roda uma vez a cada "docker compose up" (serviço migrate) e sai.
# A conexão vem de MMO_DB_CONNECTION (DesignTimeGameDbContextFactory).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo
RUN dotnet tool install --global dotnet-ef --version 10.0.13
ENV PATH="$PATH:/root/.dotnet/tools"
COPY global.json Directory.Build.props ./
COPY src/Game.Persistence/ src/Game.Persistence/
RUN dotnet ef migrations bundle --project src/Game.Persistence --configuration Release \
    --runtime linux-x64 --self-contained --output /out/efbundle

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0
WORKDIR /app
COPY --from=build /out/efbundle ./efbundle
USER $APP_UID
ENTRYPOINT ["/app/efbundle"]
