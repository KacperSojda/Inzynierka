# Etap build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

# Kopiujemy pliki projektu (wszystkie .csproj)
COPY INZYNIERKA/*.csproj ./INZYNIERKA/

# Przechodzimy do katalogu projektu i przywracamy zależności
WORKDIR /app/INZYNIERKA
RUN dotnet restore

# Kopiujemy resztę plików projektu
COPY INZYNIERKA/. .

# Budujemy i publikujemy projekt w trybie Release
RUN dotnet publish -c Release -o out

# Etap runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Instalujemy natywne biblioteki potrzebne przez Npgsql i inne zależności systemowe
RUN apt-get update && \
    apt-get install -y libssl1.1 libicu66 && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Kopiujemy opublikowaną aplikację z etapu build
COPY --from=build /app/INZYNIERKA/out .

# Ustawiamy port, na którym aplikacja będzie nasłuchiwać
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

# Uruchamiamy aplikację
ENTRYPOINT ["dotnet", "INZYNIERKA.dll"]
