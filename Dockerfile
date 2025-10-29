# --------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj
COPY SvcAnalysisOrchestrator.csproj .
RUN dotnet restore SvcAnalysisOrchestrator.csproj

# Copy the rest of the source
COPY . .

# Publish
RUN dotnet publish SvcAnalysisOrchestrator.csproj -c Release -o /out

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install curl for container healthchecks
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*

COPY --from=build /out .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Trackunit.Svc.AnalysisOrchestrator.dll"]