# ===== ЕТАП 1: ЗБІРКА =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["HRService.csproj", "./"]
RUN dotnet restore "HRService.csproj"

COPY . .
RUN dotnet publish "HRService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ===== ЕТАП 2: RUNTIME =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

RUN mkdir -p /app/logs \
    && useradd -m -u 1000 appuser \
    && chown -R appuser /app

USER appuser

ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Development
ENV LOG_DIRECTORY=/app/logs

EXPOSE 80

ENTRYPOINT ["dotnet", "HRService.dll"]