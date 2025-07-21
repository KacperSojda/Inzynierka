FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY INZYNIERKA/*.csproj ./INZYNIERKA/
WORKDIR /app/INZYNIERKA
RUN dotnet restore
COPY INZYNIERKA/. .
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/INZYNIERKA/out .

ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "INZYNIERKA.dll"]