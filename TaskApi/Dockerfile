# ===== ЕТАП 1: ЗБІРКА =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копіюємо csproj і відновлюємо залежності
COPY ["TaskApi.csproj", "./"]
RUN dotnet restore "TaskApi.csproj"

# Копіюємо весь код і публікуємо додаток
COPY . .
RUN dotnet publish "TaskApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ===== ЕТАП 2: RUNTIME =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Копіюємо тільки готові publish-файли
COPY --from=build /app/publish .

# Створюємо папку для логів і непривілейованого користувача
RUN mkdir -p /app/logs \
    && useradd -m -u 1000 appuser \
    && chown -R appuser /app

USER appuser

# Налаштування ASP.NET Core
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Development
ENV LOG_DIRECTORY=/app/logs

EXPOSE 80

ENTRYPOINT ["dotnet", "TaskApi.dll"]