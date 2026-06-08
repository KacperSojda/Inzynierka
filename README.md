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
  - Baza danych: <br /> <br />
  "ConnectionStrings": { <br />
  &emsp; "DefaultConnection": "ConnectionStrings twojej bazy danych" <br />
  }, <br /> <br />
  Konfiguracja Sztucznej inteligencji: <br /> <br />
  "ApiKeys": { <br />
  &emsp; "Gemini": "Klucz API Gemini" <br />
  }, <br />
  "EndPoints": { <br />
  &emsp; "Gemini": "EndPoint wybranej wersji Gemini" <br />
  }, <br /> <br />
  Konfiguracja skrzynki pocztowej: <br /> <br />
  "EmailConfiguration": { <br />
  &emsp; "SmtpServer": "Serwer Smtp", <br />
  &emsp; "SmtpPort": "Port Smtp", <br />
  &emsp; "SmtpUsername": "Adres email", <br />
  &emsp; "SmtpPassword": "hasło" <br />
} <br />
5. Zaaktualizuj baze danych: >> dotnet ef database update
6. Uruchom aplikacje >> dotnet run

Instalacja samych bibliotek:
1. Dodaj źródło >> dotnet nuget add source "https://nuget.pkg.github.com/KacperSojda/index.json" --name "Inz_Git" --username "TWÓJ_NICK" --password "TWÓJ_TOKEN_PAT"
2. Dodaj pakiety >> dotnet add package Inzynierka.Services
