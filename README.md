Aplikacja webowa, ułatwiająca nawiązywanie relacji między użytkownikami z wykorzystaniem komunikacji w czasie rzeczywistym oraz modeli sztucznej inteligencji.

Główne funkcjonalności:
- Czat w czasie rzeczywistym pozwalającym na prywatne i grupowe rozmowy oparte na protokole WebSockets.
- Wyszukiwarka umożliwiająca dobieranie użytkowników, na podstawie tagów, danych użytkownika, oraz sztucznej inteligencji
- Ai wykorzystane do generowania podpowiedzi, korekty wiadomości, tłumaczenia, cenzury, podumowania wiadomości, oraz wspierania matchmakingu

Wykorzystane technologie:
- Backend: C#, ASP.NET Core MVC 8.0
- Baza danych: PostgreSQL, Enity Framework Core
- Frontend: HTML, CSS, BootStrap, JavaScript, SignalR
- Google Gemini API

Uruchomienie lokalne:

Wymagania:
- .NET 8.0 SDK
- Serwer PostgreSQL
- Klucz Gemini API 

Instalacja:
1. Sklonuj repozytorium: >> git clone https://github.com/KacperSojda/Inzynierka.git
2. Przywróć pakiety: >> dotnet restore
3. Uzupełnij plik appsettings.json:
  - Baza danych:
  "ConnectionStrings": {
    "DefaultConnection": "ConnectionStrings twojej bazy danych"
  },
  Konfiguracja Sztucznej inteligencji:
  "ApiKeys": {
    "Gemini": "Klucz API Gemini"
  },
  "EndPoints": {
    "Gemini": "EndPoint wybranej wersji Gemini"
  },
  Konfiguracja skrzynki pocztowej:
  "EmailConfiguration": {
    "SmtpServer": "SerwerSmtp",
    "SmtpPort": 0,
    "SmtpUsername": "Adres email",
    "SmtpPassword": "hasło"
}
5. Zaaktualizuj baze danych: >> dotnet ef database update
6. Uruchom aplikacje >> dotnet run

Instalacja samych bibliotek:
1. Dodaj źródło >> dotnet nuget add source "https://nuget.pkg.github.com/KacperSojda/index.json" --name "Inz_Git" --username "TWÓJ_NICK" --password "TWÓJ_TOKEN_PAT"
2. Dodaj pakiety >> dotnet add package Inzynierka.Services
