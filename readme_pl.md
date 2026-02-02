# SmartAuth

SmartAuth to referencyjny system uwierzytelniania zbudowany w oparciu o .NET 10, PostgreSQL (pgvector) oraz SPA w React + Vite. Aktualna implementacja koncentruje się na logowaniu e‑mail/hasło i podstawowym przepływie 2FA, a kolejne metody (TOTP, biometria) są planowane w dalszym rozwoju.

## Funkcje (obecne)
- Rejestracja i logowanie e‑mail + hasło.
- Opcjonalny krok 2FA z tymczasowym tokenem.
- Backend w postaci Minimal API z EF Core i PostgreSQL.
- SPA React hostowane przez ASP.NET w trybie dev i po buildzie.
- Orkiestracja lokalnego środowiska przez Aspire AppHost.

## Układ rozwiązania
- `SmartAuth.AppHost` — host Aspire orkiestrujący usługi (w tym Postgresa).
- `SmartAuth.Api` — Minimal API i logika uwierzytelniania.
- `SmartAuth.Web` — host ASP.NET dla SPA.
- `SmartAuth.Web/ClientApp` — klient React + Vite.
- `SmartAuth.Infrastructure` — DbContext EF Core, migracje, integracja pgvector.
- `SmartAuth.Domain` — encje domenowe i współdzielone typy.
- `SmartAuth.ServiceDefaults` — domyślna obserwowalność (OpenTelemetry, health checks).
- `SmartAuth.Tests` — testy integracyjne (xUnit + Testcontainers).

## Szczegóły implementacyjne
### Minimal API
- `SmartAuth.Api` korzysta z ASP.NET Minimal API, a rejestracja endpointów jest opakowana w metody rozszerzeń takie jak `UseAuthEndpoints` oraz `UseFeatureFlagEndpoints`.
- Wspólne zachowania są realizowane przez filtry endpointów (np. `MediatorEndpointFilter`).

### Własna implementacja mediatora
- W `SmartAuth.Infrastructure/Commons/Mediator.cs` znajduje się własny mediator z interfejsami `IRequest`, `IRequestHandler` i `IMediator`.
- Żądania przechodzą walidację, a następnie są kierowane do handlerów zwracających `CommandResult` lub `CommandResult<T>`.

### Pobieranie modeli przy starcie
- `SmartAuth.AppHost` wywołuje `ModelFetcher.TryFetchModelsIfNeeded` podczas uruchamiania.
- Mechanizm czyta konfigurację `ModelFetching` i pobiera brakujące pliki modeli przez osadzony skrypt PowerShell.

### Przepływ autoryzacji
- **Rejestracja**: e‑mail + hasło → utworzenie użytkownika.
- **Logowanie**: weryfikacja danych; jeśli wymagane jest 2FA, zwracany jest tymczasowy JWT i dostępne metody.
- **Weryfikacja 2FA**: poprawny kod zamienia tymczasowy token na finalny JWT dostępu.

### Standard odpowiedzi API
- Handlery zwracają `CommandResult`/`CommandResult<T>`, które są mapowane na odpowiedzi HTTP.
- Sukces: `200 OK` z payloadem.
- Błędy: JSON z polami `code`, `message`, `detail`, `status`, `metadata` i `traceId`.

## Wymagania
- .NET SDK 10
- Node.js (zalecane LTS) + npm
- Docker (dla bazy lokalnej i Testcontainers)

## Szybki start (Aspire)
Aspire to zalecany sposób uruchamiania aplikacji lokalnie.

```bash
dotnet run --project SmartAuth.AppHost
```

Po starcie sprawdź w konsoli przydzielone porty:
- Swagger API: `http://localhost:<api-port>/swagger`
- SPA: `http://localhost:<web-port>/`

## Uruchamianie projektów bez Aspire (opcjonalnie)
Jeśli chcesz uruchamiać serwisy bez Aspire:

1. Uruchom PostgreSQL z pgvector (np. kontener Docker).
2. Ustaw connection string:

```bash
export ConnectionStrings__authdb="Host=localhost;Port=5432;Database=authdb;Username=postgres;Password=postgres"
```

3. Uruchom API i Web:

```bash
dotnet run --project SmartAuth.Api

dotnet run --project SmartAuth.Web
```

## Konfiguracja
Kluczowe ustawienia są w `appsettings.json` i można je nadpisywać zmiennymi środowiskowymi:
- `ConnectionStrings:authdb`
- `Jwt` (issuer, audience, key, czasy życia tokenów)
- `FeatureFlags` (włącza opcjonalne funkcje)

Przykład nadpisania:

```bash
export Jwt__Key="your-strong-dev-secret"
```

## Testy
Backend:
```bash
dotnet test SmartAuth.sln
```

Frontend:
```bash
cd SmartAuth.Web/ClientApp
npm ci
npm test
```

### Testy jednostkowe
- Testy jednostkowe/integracyjne backendu znajdują się w `SmartAuth.Tests` i uruchamiane są przez `dotnet test`.
- Testy jednostkowe frontendu działają w Vitest z katalogu `SmartAuth.Web/ClientApp`.

### Frontend (dev)
- Aplikacja SPA React znajduje się w `SmartAuth.Web/ClientApp` i korzysta z Vite.
- Serwer developerski uruchomisz przez `npm run dev`, a host ASP.NET może służyć jako proxy do API.

## Uwagi
- Przepływ 2FA z kodem używa obecnie placeholdera w trybie deweloperskim.
- Aspire AppHost zarządza lokalnym wykrywaniem usług i kontenerami.

---
Jeśli potrzebujesz więcej szczegółów (kontrakty API, bezpieczeństwo, wdrożenie), daj znać, a rozbuduję odpowiednią sekcję.
