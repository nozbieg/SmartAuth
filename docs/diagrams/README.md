# Pakiet diagramów SmartAuth

Ten folder zawiera wersjonowane diagramy architektury i uwierzytelniania, przygotowane na podstawie aktualnej struktury repozytorium i kodu.

## Zawartość

- `mermaid/`
  - `01-architecture.md` – architektura wysokiego poziomu (Aspire + projekty + Postgres/pgvector).
  - `02-auth-flow.md` – przepływ rejestracji/logowania/decyzji 2FA.
  - `03-auth-sequence.md` – sekwencja logowania w runtime: token tymczasowy + weryfikacja 2FA.
- `plantuml/`
  - `01-component.puml` – widok komponentów/projektów i ich zależności.
  - `02-sequence-login-2fa.puml` – sekwencja logowania oraz opcjonalnego 2FA.
  - `03-class-domain.puml` – kluczowe klasy modelu domeny/infrastruktury i relacje.
  - `04-deployment.puml` – topologia wdrożenia/uruchomienia oparta o Aspire.
  - `05-activity-login.puml` – diagram aktywności logowania z gałęzią 2FA.
- `out/`
  - `svg/` i `png/` – artefakty generowane przez skrypty renderujące.

## Jak przeglądać pliki Mermaid

- Otwórz pliki Markdown bezpośrednio w GitHub/GitLab lub w VS Code z obsługą podglądu Mermaid.
- Każdy plik Mermaid zawiera jeden blok:

```markdown
```mermaid
...
```
```

## Jak renderować PlantUML do SVG/PNG

Z katalogu głównego repozytorium:

- Bash (Linux/macOS):
  - `bash scripts/render-diagrams.sh`
- PowerShell (Windows):
  - `pwsh ./scripts/render-diagrams.ps1`

Oba skrypty:
- wymagają Dockera,
- renderują wszystkie pliki `docs/diagrams/plantuml/*.puml` do:
  - `docs/diagrams/out/svg`
  - `docs/diagrams/out/png`

Jeśli Docker nie jest zainstalowany/uruchomiony, skrypty wypisują pomocny komunikat i kończą się kodem różnym od zera.

## Uwagi

- Diagramy bazują na aktualnych ścieżkach kodu w:
  - `SmartAuth.AppHost`, `SmartAuth.Api`, `SmartAuth.Web`, `SmartAuth.Web/ClientApp`, `SmartAuth.Infrastructure`, `SmartAuth.Domain`, `SmartAuth.ServiceDefaults`, `SmartAuth.Tests`.
- Pokazane są wyłącznie aktualnie zaimplementowane przepływy runtime.
- Jeśli w przyszłości pojawią się nowe/przyszłe funkcje, oznaczaj je w diagramach etykietą **planned**.
