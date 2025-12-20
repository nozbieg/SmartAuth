# SmartAuth

Zaawansowany (referencyjny) system uwierzytelniania oparty o .NET 9, PostgreSQL (z rozszerzeniem `pgvector`) i SPA (React + Vite). Aktualna konfiguracja wspiera klasyczne logowanie e‑mail + hasło oraz etapowy model wieloskładnikowy (kod 2FA – placeholder oraz wstępna konfiguracja TOTP w interfejsie).

## Spis treści
1. Cel projektu
2. Architektura i moduły
3. Stos technologiczny
4. Szybki start (TL;DR)
5. Uruchamianie – szczegóły
6. Baza danych i migracje
7. Konfiguracja (appsettings / zmienne środowiskowe)
7.1 Konfiguracja TOTP
8. Feature Flags
8.1 Rozszerzanie feature flags
9. Uwierzytelnianie i przepływ logowania
10. Endpointy API (kontrakty)
11. Frontend (React / Vite)
12. Testy (backend + frontend)
13. Health-checki i observability
14. Build i publikacja (Backend + SPA)
15. Bezpieczeństwo – uwagi
16. FAQ / Troubleshooting
17. Następne kroki (roadmap sugestii)
18. Skrócone komendy (cheat‑sheet)
19. Pokrycie testów (coverage)
20. Modele biometryczne (Face / Liveness)
20.5 Ręczne wywołanie pobierania modeli
21. Filtry, rozszerzenia i wewnętrzne komponenty

---
## 1. Cel projektu
Pokazanie modułowego podejścia do uwierzytelniania użytkownika z możliwością stopniowego włączania kolejnych metod 2FA (kod, TOTP, biometria – twarz/głos) i wykorzystaniem wektorów (pgvector) w przyszłych funkcjach podobieństw/embeddings.

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
Backend: .NET 9, Minimal API, EF Core 9, Npgsql, pgvector EF, OpenTelemetry, Swashbuckle, ONNX Runtime (inferencja modeli biometrycznych), QRCoder (generowanie kodów QR dla provisioning TOTP w przyszłości).
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

### 7.1 Konfiguracja TOTP
Sekcja `Totp` (opcjonalna, przyszłe wdrożenie pełnej weryfikacji) może wyglądać następująco:
```json
"Totp": {
  "Issuer": "SmartAuthDev",
  "Digits": 6,
  "StepSeconds": 30,
  "SkewSteps": 1,
  "Algorithm": "SHA1"  // docelowo SHA256 można rozważyć
}
```
Opis:
- `Issuer` – wyświetlany w aplikacji uwierzytelniającej (Google/Microsoft Authenticator).
- `Digits` – ilość cyfr kodu jednorazowego.
- `StepSeconds` – długość okna czasowego.
- `SkewSteps` – tolerancja odchylenia (przesunięcie czasowe w przód/tył).
- `Algorithm` – domyślnie SHA1 (kompatybilność z większością aplikacji), można rozszerzyć.
Override (CMD):
```
set Totp__StepSeconds=45
set Totp__Digits=6
```
> Uwaga: Aktualna implementacja backendu zawiera tylko podstawową konfigurację `TotpOptions`; pełna walidacja TOTP i generacja sekretów jest w fazie planowania.

## 8. Feature Flags
Endpoint: `GET /api/feature-flags` zwraca JSON `{ "twofa_code": true|false }`.
Uwaga: Implementacja posiada klasę `FeatureFlagsConfig` która obecnie zwraca `true` niezależnie od appsettings.

### 8.1 Rozszerzanie feature flags
Aby dodać nową flagę (np. `twofa_totp`):
1. Dodaj pole w konfiguracji (`appsettings.json`):
```json
"FeatureFlags": { "twofa_code": true, "twofa_totp": false }
```
2. Rozszerz klasę/rekord reprezentujący wynik (`Contracts/FeatureFlags.cs`).
3. Użyj w SPA przez kontekst `FeatureFlagsContext` (pobranie i interpretacja). W komponencie można warunkować widoki: `flags.twofa_totp && <TotpConfig />`.
4. (Opcjonalnie) Dodać test frontendowy weryfikujący zachowanie przy włączonej/wyłączonej fladze.

## 9. Uwierzytelnianie i przepływ logowania
- Rejestracja: e‑mail + hasło -> zapis użytkownika (PBKDF2 hash + salt, normalizacja e‑maila do lower-case, status Active).
- Logowanie:
  1. Weryfikacja e‑mail/hasło.
  2. Jeśli brak aktywnych metod 2FA -> wydanie finalnego JWT (access token).
  3. Jeśli są metody 2FA -> wydanie tokenu tymczasowego (temp JWT) + lista metod (np. `['code']` / w przyszłości `['totp']`).
- Weryfikacja 2FA (kod): wymaga nagłówka `Authorization: Bearer <temp_token>` i kodu (obecnie placeholder – akceptuje tylko `123456`). Sukces -> wydanie finalnego JWT.

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

Zakres testów (Vitest + @testing-library/react):
- Strony: `LoginPage`, `RegisterPage`, `LandingPage`.
- Komponenty układu: `AuthLayout`, `AppLayout`, `Footer`.
- UI: `Card`, `Button`.
- 2FA: `TotpConfig` (konfiguracja TOTP – front-endowy przepływ), `TotpVerifyForm` (weryfikacja kodu).
- Kontekst: `FeatureFlagsContext` (ładowanie i obsługa błędów).
- Routing / kontrola dostępu: `RouteGuard`.
- Commons: `featureFlags` (pobranie JSON z API).

Uruchomienie z raportem pokrycia:
```
npm test -- --coverage
```
Raport: `text` + `lcov` (konfig w `vitest.config.ts`).

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
- Kod 2FA placeholder (stała wartość `123456`) – wymienić na prawdziwy TOTP (RFC 6238) lub dostawę SMS/E-mail/push.
- Rozważyć rotację kluczy oraz JWK endpoint do walidacji (jeśli w przyszłości microservices).

## 16. FAQ / Troubleshooting
| Problem | Rozwiązanie |
|---------|-------------|
| API nie startuje – brak connection string | Użyj AppHost lub ustaw `ConnectionStrings__authdb`. |
| Testy zawieszają się | Sprawdź czy Docker działa i port 5432 nie jest zajęty. |
| Brak Swagger | Upewnij się że środowisko to `Development` lub swagger nie został wyłączony. |
| 2FA zawsze wymaga kodu / zawsze brak | Zweryfikuj flagę `FeatureFlags.twofa_code` oraz implementację `FeatureFlagsConfig`. |
| SPA nie ładuje API | Sprawdź w konsoli przeglądarki port API, CORS (jeśli rozdzielone), oraz temp JWT vs final JWT. |
| Pokrycie frontendu 0% | Użyj `npm test -- --coverage` po wcześniejszym `npm ci`. |

## 17. Następne kroki (sugestie)
- Prawdziwy moduł TOTP (np. RFC 6238) + generacja i provisioning (QR code).
- Rate limiting + lockout po X nieudanych logowaniach.
- Refresh tokeny + rotacja.
- Audyt logów bezpieczeństwa + alerty (OpenTelemetry / SIEM).
- Obsługa face/voice (embeddingi) z wykorzystaniem pgvector.
- Dodanie testów kontraktowych (Swagger / NSwag) i testów E2E (Playwright / Cypress) dla SPA.
- Hardening nagłówków HTTP (SecurityHeadersMiddleware).
- Skanowanie zależności (GitHub Dependabot / Snyk) + tajne skanowanie (secret scanning).

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

REM Pokrycie frontendu
cd SmartAuth.Web\ClientApp && npm test -- --coverage

REM Tworzenie migracji
dotnet ef migrations add <Name> -p SmartAuth.Infrastructure -s SmartAuth.Api

REM Publikacja Web (SPA embedded)
dotnet publish SmartAuth.Web -c Release -o publish/web
```

## 19. Pokrycie testów (coverage)
Frontend: raport tekstowy i `lcov` (do integracji z narzędziami pokrycia / badge). Lokalizacja w katalogu projektu po uruchomieniu `npm test -- --coverage`.
Backend: użyj parametrów Coverlet (`CollectCoverage=true`, `CoverletOutputFormat=opencover`). Możliwe rozszerzenie o raport HTML (narzędzia zewnętrzne np. ReportGenerator).

---
## 20. Modele biometryczne (Face / Liveness)
Zarządzanie plikami modeli odbywa się przez sekcję `ModelFetching` w `SmartAuth.AppHost/appsettings.json` (i `.Development`). Cała logika sprawdzania brakujących plików jest w kodzie C# (`ModelFetcher`), a osadzony skrypt PowerShell (`tools/fetch-models.ps1`) służy wyłącznie do pobrania wskazanych brakujących modeli z postępem.

### 20.1 Konfiguracja (appsettings)
Przykład:
```json
"ModelFetching": {
  "Skip": false,
  "Verbose": true,
  "Directory": "SmartAuth.Infrastructure/models",
  "Models": {
    "FaceDetector": {
      "FileName": "ultraface.onnx",
      "Url": "https://github.com/onnx/models/raw/main/validated/vision/body_analysis/ultraface/models/version-RFB-640.onnx"
    },
    "FaceEmbedder": {
      "FileName": "arcface.onnx",
      "Url": "https://huggingface.co/FoivosPar/Arc2Face/resolve/da2f1e9aa3954dad093213acfc9ae75a68da6ffd/arcface.onnx?download=true"
    },
    "PassiveLiveness": {
      "FileName": "liveness_passive_v1.onnx",
      "Url": null
    }
  }
}
```
Opis pól:
- `Skip` – globalne wyłączenie mechanizmu pobierania.
- `Verbose` – włącza dodatkowe logi (domyślnie true).
- `Directory` – docelowy katalog zapisu plików modeli (względny lub absolutny).
- `Models` – słownik definicji modeli; `Url=null` oznacza brak źródła (model nie zostanie pobrany jeśli brakuje pliku).

### 20.2 Model UltraFace (Face Detector)

Aktualnie używamy **UltraFace** (version-RFB-640) z [ONNX Model Zoo](https://github.com/onnx/models/tree/main/validated/vision/body_analysis/ultraface) jako głównego detektora twarzy.

#### Specyfikacja modelu:
| Parametr | Wartość |
|----------|---------|
| Nazwa pliku | `ultraface.onnx` |
| Źródło | ONNX Model Zoo (GitHub) |
| Rozmiar | ~1.2 MB |
| Wejście | `[1, 3, 480, 640]` - NCHW, RGB |
| Normalizacja | `(pixel - 127) / 128` → zakres [-1, 1] |
| Wyjścia | `scores[1, 17640, 2]`, `boxes[1, 17640, 4]` |

#### Dlaczego UltraFace?
- ✅ **Lekki i szybki** - tylko ~1.2 MB, szybka inferencja
- ✅ **Sprawdzony** - oficjalny model z ONNX Model Zoo
- ✅ **Prosty format wyjściowy** - boxes są już znormalizowane do [0,1]
- ✅ **Dobra dokładność** - wykrywa twarze z wysoką pewnością

#### Poprzedni model (RetinaFace)
Wcześniej używany model `retinaface_standard_conversion.onnx` z HuggingFace miał problemy z konwersją - zwracał nieprawidłowe wartości score (prawie identyczne dla obu klas: background i face), co uniemożliwiało poprawną detekcję.

### 20.3 Implementacja OnnxFaceDetector

Klasa `OnnxFaceDetector` (`SmartAuth.Infrastructure/Biometrics/OnnxFaceDetector.cs`) realizuje detekcję twarzy przy użyciu ONNX Runtime.

#### Architektura:
```
┌─────────────────────────────────────────────────────────────┐
│                    OnnxFaceDetector                         │
├─────────────────────────────────────────────────────────────┤
│  DetectAsync(rgbImage, width, height)                       │
│      │                                                      │
│      ▼                                                      │
│  PrepareInputTensor()                                       │
│      - Resize do 640x480 (bilinear interpolation)           │
│      - Normalizacja: (pixel - 127) / 128                    │
│      - Format: NCHW [1, 3, 480, 640]                        │
│      │                                                      │
│      ▼                                                      │
│  ONNX Runtime Inference                                     │
│      │                                                      │
│      ▼                                                      │
│  ParseOutputs()                                             │
│      - Filtrowanie po ConfidenceThreshold (0.7)             │
│      - Dekodowanie boxes [x1, y1, x2, y2] → [x, y, w, h]    │
│      - Skalowanie do oryginalnych wymiarów                  │
│      │                                                      │
│      ▼                                                      │
│  ApplyNms()                                                 │
│      - Non-Maximum Suppression (threshold 0.3)              │
│      - Sortowanie po score, usuwanie nakładających się      │
│      │                                                      │
│      ▼                                                      │
│  FaceDetectionResult                                        │
│      - Lista FaceCandidate (max 10)                         │
│      - Sortowanie po Area (największe pierwsze)             │
└─────────────────────────────────────────────────────────────┘
```

#### Kluczowe parametry:
```csharp
private const int ModelWidth = 640;
private const int ModelHeight = 480;
private const float ConfidenceThreshold = 0.7f;  // Min pewność detekcji
private const float NmsThreshold = 0.3f;         // Próg IoU dla NMS
private const int MaxFaces = 10;                 // Max zwracanych twarzy
private const int MinFaceSize = 20;              // Min rozmiar twarzy (px)
```

#### Preprocessing obrazu:
1. **Bilinear interpolation** - resize do 640x480 z zachowaniem jakości
2. **Normalizacja** - `(pixel - 127) / 128` → zakres [-1, 1]
3. **Format NCHW** - [batch, channels, height, width]

#### Postprocessing:
1. **Filtrowanie** - tylko detekcje z score > 0.7
2. **Dekodowanie boxes** - z formatu [x1, y1, x2, y2] (znormalizowane 0-1) do [x, y, width, height] (piksele)
3. **Skalowanie** - przeskalowanie do oryginalnych wymiarów obrazu
4. **NMS** - usunięcie nakładających się detekcji (IoU > 0.3)
5. **Sortowanie** - największe twarze pierwsze
6. **Estymacja landmarków** - przybliżone pozycje: oczy, nos, usta

#### Przykład użycia:
```csharp
var detector = new OnnxFaceDetector(biometricsOptions);
var result = await detector.DetectAsync(rgbBytes, 640, 480, cancellationToken);

foreach (var face in result.Faces)
{
    Console.WriteLine($"Face at ({face.Box.X}, {face.Box.Y}) " +
                      $"size {face.Box.Width}x{face.Box.Height}, " +
                      $"confidence: {face.Confidence:P0}");
}
```

#### Typowe wyniki:
```
Input: 640x480 RGB image
Output: 
  - 17640 detekcji (wszystkie anchory)
  - ~5-10 kandydatów po filtrze confidence
  - 1-3 twarze po NMS
  - Face: box=(274,133,125x162), score=1.000
```

### 20.4 Przepływ pobierania modeli
1. `ModelFetcher` (AppHost startup) wczytuje konfigurację.
2. Sprawdza dla każdego wpisu czy plik (`Directory/FileName`) istnieje.
3. Tworzy listę brakujących modeli posiadających `Url`.
4. Generuje JSON (`MODELS_SPEC`) i uruchamia osadzony skrypt PowerShell z env: `MODELS_TARGET_DIR`, `MODELS_SPEC`, `MODEL_FETCH_VERBOSE`.
5. Skrypt wykonuje pobieranie strumieniowe z paskiem postępu i zapisuje manifest `checksums.json`.

### 20.5 Skrypt PowerShell (osadzony)
Parametry środowiskowe (ustawiane przez C#):
- `MODELS_TARGET_DIR` – lokalizacja docelowa.
- `MODELS_SPEC` – JSON lista brakujących modeli (każdy: `name`, `url`, `fileName`).
- `MODEL_FETCH_VERBOSE` – steruje poziomem logów (`true` / `false`).
- `NO_PROGRESS` – jeśli `true`, wyłącza pasek postępu.

Skrypt nie wykonuje logiki biznesowej (nie sprawdza czy plik istnieje) – przyjmuje, że dostaje tylko brakujące modele.

### 20.6 Dodawanie nowego modelu
Wystarczy dodać wpis w `ModelFetching.Models`:
```json
"MyNewModel": { "FileName": "my_new_model.onnx", "Url": "https://example.com/my_new_model.onnx" }
```
Po restarcie AppHost brakujący plik zostanie pobrany automatycznie.

### 20.7 Ręczne wywołanie pobierania modeli
Standardowo mechanizm uruchamia się przy starcie `AppHost`. Jeśli potrzebujesz wymusić ponowne pobranie (np. wyczyściłeś katalog):
1. Usuń pliki modeli z katalogu wskazanego w `ModelFetching:Directory`.
2. Ustaw (opcjonalnie) zmienne środowiskowe dla trybu bez paska postępu:
```
set NO_PROGRESS=true
```
3. Uruchom ponownie:
```
dotnet run --project SmartAuth.AppHost
```
> Jeżeli potrzebne jest całkowite pominięcie procesu (np. offline): `set ModelFetching__Skip=true` lub w `appsettings.Development.json` ustaw `"Skip": true`.

## 21. Filtry, rozszerzenia i wewnętrzne komponenty
Krótki opis istotnych elementów kodu:
- `MediatorEndpointFilter` – filtr endpointów integrujący prosty mediator/wzorzec obsługi komend (walidacja + wykonanie). Ułatwia jednolite mapowanie i obsługę błędów.
- Metody rozszerzeń w `Extensions/AuthenticationInstall.cs`, `HandlersInstall.cs`, `ValidatorsInstall.cs` – kapsułkują rejestrację DI (czytelniejsze `Program.cs`).
- `MigrationRunnerHostedService` – uruchamia automatyczne migracje przy starcie API, redukując manualne kroki.
- `CommandResultHttpMapping` – mapuje rezultaty logiki (sukces / błędy domenowe) na odpowiedzi HTTP (status + payload).
- `AuthCrypto` – odpowiedzialny za hashowanie haseł (PBKDF2). Przyszłe rozszerzenia: Argon2 / scrypt.
- `TokenUtilities` – generowanie JWT (final oraz tymczasowy) + claims.
- `CosineSimilarity` – pomocnicze obliczanie podobieństwa wektorów (planowane użycie w biometriach / embeddings).
- `MicrosoftAuthenticatorClient` – (zalążek) generowania materiałów provisioning dla aplikacji TOTP (docelowo QR kod + otpauth URI).

> Dodając nowe komponenty, zachowuj konwencję: publikuj prostą metodę rozszerzeń do rejestracji w DI, aby utrzymać `Program.cs` zwięzły.

---
Happy coding! Jeśli czegoś brakuje – rozbuduj README wraz z ewolucją projektu.
