# SmartAuth

Zaawansowany (referencyjny) system uwierzytelniania oparty o .NET 9, PostgreSQL (z rozszerzeniem `pgvector`) i SPA (React + Vite). Aktualna konfiguracja wspiera klasyczne logowanie e‑mail + hasło oraz przygotowanie pod wieloskładnikowe (kod 2FA – makieta).

## Spis treści
1. Cel projektu
2. Architektura i moduły
3. Stos technologiczny
4. Szybki start (TL;DR)
5. Uruchamianie – szczegóły
6. Baza danych i migracje
7. Konfiguracja (appsettings / zmienne środowiskowe)
8. Feature Flags
9. Uwierzytelnianie i przepływ logowania
10. Endpointy API (kontrakty)
11. Frontend (React / Vite)
12. Testy (backend + frontend)
13. Health-checki i observability
14. Build i publikacja (Backend + SPA)
15. Bezpieczeństwo – uwagi
16. FAQ / Troubleshooting
17. Następne kroki (roadmap sugestii)

---
## 1. Cel projektu
Pokazanie modułowego podejścia do uwierzytelniania użytkownika z możliwością stopniowego włączania kolejnych metod 2FA (kod, biometria – twarz/głos) i wykorzystaniem wektorów (pgvector) w przyszłych funkcjach podobieństw/embeddings.

## 2. Architektura i moduły
Struktura solution:
- `SmartAuth.AppHost` – projekt startowy (Aspire Distributed Application) orkiestrujący usługi i kontenery (PostgreSQL z `pgvector`).
- `SmartAuth.Api` – Minimal API (endpoints) + logika scenariuszy (komendy / handlery / walidacja).
- `SmartAuth.Web` – host ASP.NET dla SPA + (w dev) SpaProxy -> Vite dev server.
- `SmartAuth.Web/ClientApp` – właściwa aplikacja React (Vite + TypeScript + Vitest + Testing Library).
- `SmartAuth.Infrastructure` – EF Core DbContext, konfiguracje, migracje, integracja pgvector.
- `SmartAuth.Domain` – encje domenowe, interfejsy, wspólne typy.
- `SmartAuth.ServiceDefaults` – observability (OpenTelemetry), health-checki bazowe, resilience pipelines.
- `SmartAuth.Tests` – testy integracyjne (xUnit + Testcontainers + PostgreSQL + pgvector).

### Przepływ wysokiego poziomu
```
[ Client (React SPA) ]
          |
          | HTTP (JSON, JWT Bearer)
          v
[ SmartAuth.Web ]  (serwuje statyczne pliki + proxy w dev)
          |
          v
[ SmartAuth.Api ] -- EF Core --> [ PostgreSQL + pgvector ]
          |
          +-- OpenTelemetry eksport -> (opcjonalny OTLP endpoint)
```

## 3. Stos technologiczny
Backend: .NET 9, Minimal API, EF Core 9, Npgsql, pgvector EF, OpenTelemetry, Swashbuckle.
Frontend: React 18, Vite, TypeScript, React Router, Vitest, Testing Library.
Inne: Testcontainers, PBKDF2 (Rfc2898DeriveBytes) dla haseł, JWT (HMAC SHA256).

## 4. Szybki start (TL;DR)
Wymagania lokalne:
- .NET SDK 9
- Node.js (zalecane LTS, np. 20.x) + npm
- Docker Desktop (musi działać dla Testcontainers oraz kontenera bazy przy AppHost)

Komendy (Windows CMD):
```
REM Uruchom całą aplikację (AppHost + Postgres + API + Web)
dotnet run --project SmartAuth.AppHost

REM (Osobno) uruchom tylko API (wymaga ręcznie skonfigurowanej bazy / connection string)
dotnet run --project SmartAuth.Api

REM (Osobno) uruchom Web (SPA proxy -> Vite)
dotnet run --project SmartAuth.Web

REM Testy backend
dotnet test SmartAuth.sln

REM Testy frontendu
cd SmartAuth.Web\ClientApp
npm ci
npm test
```
Po starcie AppHost wypatruj w konsoli adresów HTTP (dynamiczne porty). Otwórz:
- Swagger: `http://localhost:<api-port>/swagger`
- SPA: `http://localhost:<web-port>/` (proxy dev lub statyczne pliki jeśli publish)

## 5. Uruchamianie – szczegóły
### 5.1 Z AppHost (zalecane)
AppHost (Aspire) tworzy kontener Postgres (obraz `pgvector/pgvector:pg17`) z montowanym katalogiem `SmartAuth.AppHost/db-init` zawierającym skrypty inicjalizujące (tworzenie bazy, włączenie rozszerzenia `vector`). Projekty API i Web startują po gotowości bazy.

```
dotnet run --project SmartAuth.AppHost
```
Zatrzymanie: Ctrl+C. Dane bazy utrwalane w volume `auth-db-data`.

### 5.2 Bez AppHost (manualnie)
1. Uruchom lokalny Postgres (np. docker):
```
docker run --name smartauth-pg -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d pgvector/pgvector:pg17
```
2. (Opcjonalnie) Utwórz bazę `authdb` jeśli nie istnieje.
3. Dodaj connection string do `SmartAuth.Api/appsettings.Development.json` lub ustaw zmienną środowiskową:
```
ConnectionStrings__authdb=Host=localhost;Port=5432;Database=authdb;Username=postgres;Password=postgres
```
4. Uruchom API i Web:
```
dotnet run --project SmartAuth.Api
set ASPNETCORE_URLS=http://localhost:5200
REM powyżej opcjonalnie

cd SmartAuth.Web
dotnet run
```
Frontend przy pierwszym buildzie wykona `npm ci` (target `NpmInstall_IfNeeded`).

## 6. Baza danych i migracje
- Migracje znajdują się w `SmartAuth.Infrastructure/Migrations`.
- Przy starcie `SmartAuth.Api` działa `MigrationRunnerHostedService` wykonujący pending migracje.
- Rozszerzenie `pgvector` deklarowane w modelu (`modelBuilder.HasPostgresExtension("vector")`).

Tworzenie nowej migracji:
```
dotnet ef migrations add MyNewMigration -p SmartAuth.Infrastructure -s SmartAuth.Api
```
Aktualizacja bazy (gdyby trzeba ręcznie):
```
dotnet ef database update -p SmartAuth.Infrastructure -s SmartAuth.Api
```

## 7. Konfiguracja (appsettings / env)
Kluczowe sekcje:
- `ConnectionStrings.authdb` – łańcuch połączenia do bazy (w trybie AppHost dostarczany automatycznie przez Aspire).
- `Jwt`:
  - `Issuer`, `Audience`
  - `Key` (symetryczny sekretny klucz – DEV placeholder, wymienić w prod!)
  - `AccessTokenMinutes`, `TempTokenMinutes`
- `FeatureFlags` – włączenie metod 2FA (obecnie używane tylko `twofa_code`).

Override przez zmienne środowiskowe (przykład CMD):
```
set Jwt__Key=NOWY_BARDZIEJ_TAJNY_KLUCZ
set FeatureFlags__twofa_code=true
```

## 8. Feature Flags
Endpoint: `GET /api/feature-flags` zwraca JSON `{ "twofa_code": true|false }`.
Uwaga: Implementacja posiada klasę `FeatureFlagsConfig` która obecnie zwraca `true` niezależnie od appsettings.

## 9. Uwierzytelnianie i przepływ logowania
- Rejestracja: e‑mail + hasło -> zapis użytkownika (PBKDF2 hash + salt, normalizacja e‑maila do lower-case, status Active).
- Logowanie:
  1. Weryfikacja e‑mail/hasło.
  2. Jeśli brak aktywnych metod 2FA -> wydanie finalnego JWT (access token).
  3. Jeśli są metody 2FA -> wydanie tokenu tymczasowego (temp JWT) + lista metod (np. `["code"]`).
- Weryfikacja 2FA (kod): wymaga nagłówka `Authorization: Bearer <temp_token>` i kodu (obecnie makieta – akceptuje tylko `123456`). Sukces -> wydanie finalnego JWT.

Schemat tokenów:
- Temp token – krótszy TTL (`TempTokenMinutes`), claim `typ=temp`.
- Final token – TTL `AccessTokenMinutes`, standardowe claims (`sub`, `email`, `name`, `role`).

## 10. Endpointy API (kontrakty)
Base path: (dynamiczny port) `http://localhost:<api-port>`

1. POST `/api/auth/register`
Request JSON:
```
{ "email": "user@example.com", "password": "Secret!123", "displayName": "User" }
```
Response 200:
```
{ "message": "Registration completed successfully" }
```
Błędy: 409 (email istnieje), 400 (walidacja).

2. POST `/api/auth/login`
Request:
```
{ "email": "user@example.com", "password": "Secret!123" }
```
Response gdy brak 2FA:
```
{ "requires2Fa": false, "token": "<JWT>" }
```
Response gdy 2FA wymagane:
```
{ "requires2Fa": true, "token": "<TEMP_JWT>", "methods": ["code"] }
```

3. POST `/api/auth/2fa/code/verify` (Authorization: Bearer <TEMP_JWT>)
Request:
```
{ "code": "123456" }
```
Response:
```
{ "jwt": "<FINAL_JWT>" }
```

4. GET `/api/feature-flags`
Response:
```
{ "twofa_code": true }
```

5. Health / Info:
- `GET /health/live` – szybki stan (self)
- `GET /health/ready` – szczegóły (DB, migracje)
- `GET /_info` – meta (nazwa, środowisko)

Swagger: `/swagger`.

## 11. Frontend (React / Vite)
Lokalne uruchomienie (sam SPA):
```
cd SmartAuth.Web\ClientApp
npm ci
npm run dev
```
Domyślnie Vite na porcie 5173. W trybie równoległym uruchom `SmartAuth.Web` aby korzystać z proxy (SpaProxy). W produkcji build dist kopiowany do `wwwroot` podczas `dotnet publish` (Target `BuildSpa`).

Budowanie SPA:
```
npm run build
```
Podgląd builda:
```
npm run preview
```

## 12. Testy
### 12.1 Backend
```
dotnet test SmartAuth.sln
```
Cechy:
- Testcontainers – automatycznie startuje tymczasowy Postgres z pgvector.
- Brak potrzeby lokalnej bazy (Docker wymagany).
- Pokrycie (opcjonalnie):
```
dotnet test SmartAuth.sln /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### 12.2 Frontend
```
cd SmartAuth.Web\ClientApp
npm ci
npm test          REM jednorazowo (vitest run)
npm run test:watch
npm run test:ui   REM interfejs web Vitest
```
Środowisko testowe: jsdom + Testing Library.

## 13. Health-checki i observability
- Live: podstawowy self-check tag `live`.
- Ready: DB connectivity, DbContext, brak pending migracji.
- OpenTelemetry: automatyczne instrumentacje (ASP.NET Core, HttpClient, runtime). Eksporter OTLP aktywuje się jeśli ustawiona `OTEL_EXPORTER_OTLP_ENDPOINT`.

Przykład (CMD) włączenia eksportu (lokalny collector):
```
set OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
```

## 14. Build i publikacja
### 14.1 API + Web (zintegrowane SPA)
```
dotnet publish SmartAuth.Web -c Release -o publish/web
```
Publikacja kopiuje wyniki Vite do `wwwroot`.

### 14.2 Całość z AppHost (kompozycja)
AppHost jest głównie trybem deweloperskim. Do produkcji można:
- Oddzielnie wdrożyć infrastrukturę (Postgres) IaC.
- Wdrożyć `SmartAuth.Api` i `SmartAuth.Web` jako kontenery albo single artifact (publish) + reverse proxy (nginx / YARP).

## 15. Bezpieczeństwo – uwagi
- Klucz JWT w repo to TYLKO DEV. Zmień na silny sekret (co najmniej 256‑bit) i trzymaj w menedżerze sekretów / zmiennych środowiskowych (Azure Key Vault, AWS KMS itp.).
- PBKDF2 parametry: Iterations = 100 000, KeySize = 32, SaltSize = 16 – można zwiększyć w produkcji w zależności od profilu wydajności.
- Brak rate limiting / lockout przy błędach logowania – dodać.
- Kod 2FA makieta (stała wartość `123456`) – wymienić na TOTP (RFC 6238) lub dostawę SMS/E-mail/push.
- Rozważyć rotację kluczy oraz JWK endpoint do walidacji (jeśli w przyszłości microservices).

## 16. FAQ / Troubleshooting
| Problem | Rozwiązanie |
|---------|-------------|
| API nie startuje – brak connection string | Użyj AppHost lub ustaw `ConnectionStrings__authdb`. |
| Testy zawieszają się | Sprawdź czy Docker działa i port 5432 nie jest zajęty. |
| Brak Swagger | Upewnij się że środowisko to `Development` lub swagger nie został wyłączony. |
| 2FA zawsze wymaga kodu / zawsze brak | Zweryfikuj flagę `FeatureFlags.twofa_code` oraz implementację `FeatureFlagsConfig`. |
| SPA nie ładuje API | Sprawdź w konsoli przeglądarki port API, CORS (jeśli rozdzielone), oraz temp JWT vs final JWT. |

## 17. Następne kroki (sugestie)
- Prawdziwy moduł TOTP (np. RFC 6238) + generacja i provisioning (QR code).
- Rate limiting + lockout po X nieudanych logowaniach.
- Refresh tokeny + rotacja.
- Audyt logów bezpieczeństwa + alerty (OpenTelemetry / SIEM).
- Obsługa face/voice (embeddingi) z wykorzystaniem pgvector.
- Dodanie testów kontraktowych (Swagger / NSwag) i testów E2E (Playwright / Cypress) dla SPA.
- Hardening nagłówków HTTP (SecurityHeadersMiddleware).

## 18. Skrócone komendy (cheat‑sheet)
```
REM Start pełny (AppHost)
dotnet run --project SmartAuth.AppHost

REM Testy backend
dotnet test

REM Frontend (watch)
cd SmartAuth.Web\ClientApp && npm run dev

REM Testy frontendu
cd SmartAuth.Web\ClientApp && npm test

REM Tworzenie migracji
dotnet ef migrations add <Name> -p SmartAuth.Infrastructure -s SmartAuth.Api

REM Publikacja Web (SPA embedded)
dotnet publish SmartAuth.Web -c Release -o publish/web
```

---
Happy coding! Jeśli czegoś brakuje – rozbuduj README wraz z ewolucją projektu.
