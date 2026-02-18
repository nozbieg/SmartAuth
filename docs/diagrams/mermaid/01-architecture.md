# Architecture (Mermaid)

```mermaid
flowchart LR
    subgraph Aspire[SmartAuth.AppHost (Aspire)]
        AppHost[Program.cs\nDistributedApplication Builder]
    end

    subgraph Runtime[Runtime Resources]
        Web[SmartAuth.Web\nASP.NET host + static SPA]
        SPA[SmartAuth.Web/ClientApp\nReact + Vite SPA]
        Api[SmartAuth.Api\nMinimal API + JWT + OpenAPI]
        Db[(PostgreSQL authdb\npgvector extension)]
    end

    subgraph Core[Core Libraries]
        Infra[SmartAuth.Infrastructure\nEF Core + Biometrics + Security]
        Domain[SmartAuth.Domain\nEntities: User/Auth/Biometrics]
        Defaults[SmartAuth.ServiceDefaults\nHealth + OTel + Discovery]
        Tests[SmartAuth.Tests\nIntegration/Unit tests]
    end

    AppHost -->|orchestrates| Web
    AppHost -->|orchestrates| Api
    AppHost -->|provisions| Db

    Web -->|serves| SPA
    SPA -->|HTTP /api/auth/*| Api
    Web -. service defaults .-> Defaults
    Api -. service defaults .-> Defaults

    Api -->|uses| Infra
    Infra -->|uses| Domain
    Infra -->|Npgsql + EF Core| Db

    Tests -->|references| Api
```
