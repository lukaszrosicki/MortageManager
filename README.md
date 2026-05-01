# Mortgage Pro

Aplikacja webowa do symulacji i zarządzania harmonogramem spłat kredytu hipotecznego (m.in. raty równe i malejące, nadpłaty, podgląd harmonogramu w czasie rzeczywistym).

## O projekcie

### Stos technologiczny

| Warstwa | Technologie |
|--------|-------------|
| Runtime / język | **.NET 10** (C#), nullable reference types, implicit usings |
| Web | **ASP.NET Core** — MVC (**Razor** / **Controllers with Views**), **SignalR** (aktualizacje w czasie rzeczywistym) |
| Dane | **Entity Framework Core 10** + **SQLite** (lokalna baza plikowa) |
| Uwierzytelnianie | **ASP.NET Core Identity** (role, m.in. `Admin`) |
| Testy | **xUnit**, **Microsoft.NET.Test.Sdk**, **coverlet** (pokrycie kodu) |
| Konteneryzacja | **Docker** (`Dockerfile`, **Docker Compose**) |

### Architektura repozytorium

Rozwiązanie jest podzielone na warstwy (czysta architektura / podział odpowiedzialności):

- **`MortgagePro.Domain`** — encje domeny i reguły modelu
- **`MortgagePro.Application`** — przypadki użycia, serwisy (np. silnik kalkulacyjny `ReactiveMortgageEngine`)
- **`MortgagePro.Infrastructure`** — EF Core, `DbContext`, implementacja dostępu do danych, Identity
- **`MortgagePro.WebUI`** — interfejs użytkownika (MVC, widoki, Huby SignalR, konfiguracja hosta)
- **`MortgagePro.Tests`** — testy jednostkowe logiki silnika (bez bazy i serwera WWW)

Punkt wejścia do pracy w IDE: plik **`MortgagePro.slnx`**.

## Jak uruchomić

Masz przygotowane dwie ścieżki na uruchomienie tego systemu - za pomocą **Docker Compose** (najszybsza i najbardziej izolowana) oraz klasycznie przez **.NET CLI**.

## Opcja 1: Uruchomienie przez Docker Compose (Zalecane)

Upewnij się, że masz zainstalowanego i uruchomionego [Docker Desktop](https://www.docker.com/products/docker-desktop/).

1. Otwórz terminal (np. PowerShell lub Wiersz polecenia).
2. Przejdź do głównego katalogu projektu, gdzie znajduje się plik `docker-compose.yml`.
3. Uruchom kontener:
   ```bash
   docker-compose up --build -d
   ```
4. Po uruchomeniu aplikacji będzie dostępna pod adresem: **http://localhost:8080** (lub opisanym w Dockerfile).

5. To shut down the app run:
   ```bash
   docker compose down
   ```

## Opcja 2: Uruchomienie z użyciem .NET CLI

Wymaga zainstalowanego **.NET 10 SDK** (zgodnego z `TargetFramework` w projekcie, obecnie **net10.0**).

1. Otwórz terminal i przejdź do folderu z WebUI:
   ```bash
   cd MortgagePro.WebUI
   ```
2. Zbuduj i uruchom projekt poleceniem:
   ```bash
   dotnet run
   ```
3. Zwróć uwagę na wypisany w konsoli adres (zazwyczaj **http://localhost:5000** lub pod innym używanym lokalnie).
4. Otwórz w przeglądarce. Baza SQLite utworzy się sama.

## Opcja 3: Visual Studio

1. Otwórz plik `MortgagePro.slnx` lub folder projektu w Visual Studio 2022 / JetBrains Rider.
2. Zaznacz `MortgagePro.WebUI` jako projekt startowy.
3. Kliknij `F5` / Uruchom.

## Testy jednostkowe

Projekt testów: **`MortgagePro.Tests`**. Używany jest **xUnit** (oraz `Microsoft.NET.Test.Sdk` i `coverlet.collector` do zbierania pokrycia).

### Co jest testowane

Klasa **`ReactiveMortgageEngineTests`** sprawdza logikę **`ReactiveMortgageEngine`** w warstwie domeny/aplikacji m.in.:

- generowanie harmonogramu (raty równe i malejące, liczba miesięcy, salda),
- nadpłaty i strategie (np. skrócenie okresu),
- aktualizacje kaskadowe (`cascade`).

Nie wymagają one uruchomionej aplikacji webowej ani bazy danych — to testy **jednostkowe** w pamięci.

### Wymagania

Potrzebny jest **.NET SDK** zgodny z `TargetFramework` testów (w pliku `MortgagePro.Tests/MortgagePro.Tests.csproj` — obecnie **net10.0**).

### Uruchomienie z linii poleceń

Z **głównego katalogu** repozytorium (tam, gdzie jest `MortgagePro.slnx`):

```bash
dotnet test MortgagePro.Tests/MortgagePro.Tests.csproj
```

Alternatywnie, przez plik rozwiązania (jeśli twoja wersja `dotnet` obsługuje `*.slnx`):

```bash
dotnet test MortgagePro.slnx
```

## Dane dostępowe Admina

Podczas startu aplikacji i tworzenia struktury bazy danych SQLite, tworzone jest domyślne konto Administratora o następujących poświadczeniach:

- **Login:** `admin@admin.com`
- **Hasło:** `Admin123`
