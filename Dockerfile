# syntax=docker/dockerfile:1.7
# Multi-stage build. Son image Alpine tabanlı, slim ve non-root.

ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-alpine AS build
WORKDIR /src

# csproj'ları önce kopyala — restore cache'ini optimize eder.
COPY global.json Directory.Build.props ./
COPY src/CleanCore.Domain/*.csproj          src/CleanCore.Domain/
COPY src/CleanCore.Application/*.csproj     src/CleanCore.Application/
COPY src/CleanCore.Infrastructure/*.csproj  src/CleanCore.Infrastructure/
COPY src/CleanCore.Api/*.csproj             src/CleanCore.Api/

RUN dotnet restore src/CleanCore.Api/CleanCore.Api.csproj

# Kaynak kodun geri kalanı.
COPY src/ src/

RUN dotnet publish src/CleanCore.Api/CleanCore.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# --- Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-alpine AS runtime
WORKDIR /app

# Non-root user — Alpine default 'app' kullanıcısı yok, manuel oluşturuyoruz.
RUN addgroup -S app && adduser -S app -G app
USER app

COPY --from=build --chown=app:app /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

ENTRYPOINT ["dotnet", "CleanCore.Api.dll"]
