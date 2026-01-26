# Dockerfile per backend .NET 8 ottimizzato per Google Cloud Run.
# Implementa una strategia Multi-Stage Build per separare la compilazione (SDK) dall'esecuzione (Runtime),
# garantendo un'immagine finale leggera. Include hardening di sicurezza (esecuzione tramite utente non-root)
# e configurazione della porta 8080 per la compatibilità con l'infrastruttura serverless.

# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY src/ ./src/
RUN dotnet restore ./src/Template.Web/Template.Web.csproj
RUN dotnet publish ./src/Template.Web/Template.Web.csproj -c Release -o /app/out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 8080
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
COPY --from=build /app/out ./
USER appuser
ENTRYPOINT ["dotnet", "Template.Web.dll"]
