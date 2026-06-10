# syntax=docker/dockerfile:1.7

ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-alpine AS build
WORKDIR /src

ARG WEBAPI_DIR=src/Crm.WebApi

COPY . .
RUN dotnet restore "${WEBAPI_DIR}"
RUN dotnet publish "${WEBAPI_DIR}" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-alpine AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0

COPY --from=build /app/publish ./

USER 1654
EXPOSE 8080

ENTRYPOINT ["dotnet", "Crm.WebApi.dll"]
