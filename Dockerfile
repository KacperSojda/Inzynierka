FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

# Kopiuj projekt do kontenera
COPY INZYNIERKA/*.csproj ./INZYNIERKA/

# Przywróć zależności
WORKDIR /app/INZYNIERKA
RUN dotnet restore

# Kopiuj cały projekt do kontenera
COPY INZYNIERKA/. .

# Zbuduj i opublikuj
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app
COPY --from=build /app/INZYNIERKA/out .

ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "INZYNIERKA.dll"]
