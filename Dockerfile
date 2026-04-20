FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["INZYNIERKA.sln", "./"]
COPY ["INZYNIERKA/INZYNIERKA.csproj", "INZYNIERKA/"]
COPY ["INZYNIERKA.Domain/INZYNIERKA.Domain.csproj", "INZYNIERKA.Domain/"]
COPY ["INZYNIERKA.Tests/INZYNIERKA.Tests.csproj", "INZYNIERKA.Tests/"]
COPY ["INZYNIERKA.E2ETests/INZYNIERKA.E2ETests.csproj", "INZYNIERKA.E2ETests/"]
COPY ["INZYNIERKA.Services/INZYNIERKA.Services.csproj", "INZYNIERKA.Services/"]
COPY ["INZYNIERKA.Data/INZYNIERKA.Data.csproj", "INZYNIERKA.Data/"]

RUN dotnet restore

COPY . .

WORKDIR "/src/INZYNIERKA"
RUN dotnet publish -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "INZYNIERKA.dll"]