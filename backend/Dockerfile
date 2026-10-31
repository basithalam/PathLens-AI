# PathLens AI — Backend Dockerfile (API + Worker)
# Single image, entrypoint selects role via DOTNET_ROLE env.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY PathLens.slnx ./
COPY src/PathLens.Api/PathLens.Api.csproj src/PathLens.Api/
COPY src/PathLens.Application/PathLens.Application.csproj src/PathLens.Application/
COPY src/PathLens.Domain/PathLens.Domain.csproj src/PathLens.Domain/
COPY src/PathLens.Infrastructure/PathLens.Infrastructure.csproj src/PathLens.Infrastructure/
COPY src/PathLens.Worker/PathLens.Worker.csproj src/PathLens.Worker/
RUN dotnet restore src/PathLens.Api/PathLens.Api.csproj
RUN dotnet restore src/PathLens.Worker/PathLens.Worker.csproj

COPY src/ src/
# Publish both projects; Runtime image will pick one.
RUN dotnet publish src/PathLens.Api/PathLens.Api.csproj -c Release -o /app/api
RUN dotnet publish src/PathLens.Worker/PathLens.Worker.csproj -c Release -o /app/worker

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
# Copy both publish outputs into separate folders via the build stage
COPY --from=build /app/api ./api
COPY --from=build /app/worker ./worker

# Shared settings — override with environment in docker-compose / deployment
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_ROLE=api
EXPOSE 8080

# Entrypoint switches on DOTNET_ROLE: api (default) or worker
COPY docker-entrypoint.sh ./docker-entrypoint.sh
RUN chmod +x ./docker-entrypoint.sh
ENTRYPOINT ["./docker-entrypoint.sh"]
