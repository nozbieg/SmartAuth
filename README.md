# SmartAuth

SmartAuth to przykładowy system uwierzytelniania z wieloma metodami (hasło, TOTP, rozpoznawanie twarzy, głosu) wykorzystujący:
- .NET 9 / EF Core 9
- PostgreSQL + rozszerzenie `pgvector`
- Testcontainers do testów integracyjnych

## Struktura
- `SmartAuth.Domain` – encje domenowe
- `SmartAuth.Infrastructure` – EF Core (DbContext, migracje, konfiguracje)
- `SmartAuth.Api` / `SmartAuth.Web` – warstwa API / web (skrócone)
- `SmartAuth.Tests` – testy integracyjne (xUnit + Testcontainers)

## Wymagania
- Docker Desktop (dla Testcontainers)
- .NET SDK 9

## Uruchamianie testów
```cmd
dotnet test SmartAuth.sln
```
Testy automatycznie uruchomią kontener PostgreSQL z rozszerzeniem `vector`.