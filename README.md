# Jak uruchomić projekt Mortgage Pro

Masz przygotowane dwie ścieżki na uruchomienie tego systemu - za pomocą **Docker Compose** (najszybsza i najbardziej izolowana) oraz klasycznie przez **.NET CLI**.

## Opcja 1: Uruchomienie przez Docker Compose (Zalecane)

Upewnij się, że masz zainstalowanego i uruchomionego [Docker Desktop](https://www.docker.com/products/docker-desktop/).

1. Otwórz terminal (np. PowerShell lub Wiersz polecenia).
2. Przejdź do głównego katalogu projektu, gdzie znajduje się plik `docker-compose.yml`.
3. Uruchom kontener:
   ```bash
   docker-compose up --build -d
   ```
4. Po ukończeniu aplikacji będzie dostępna pod adresem: **http://localhost:8080** (lub opisanym w Dockerfile).

## Opcja 2: Uruchomienie z użyciem .NET CLI

Wymaga zainstalowanego **.NET 8.0 SDK**.

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

## Dane dostępowe Admina

Podczas startu aplikacji i tworzenia struktury bazy danych SQLite, tworzone jest domyślne konto Administratora o następujących poświadczeniach:
- **Login:** `admin@admin.com`
- **Hasło:** `Admin123`
