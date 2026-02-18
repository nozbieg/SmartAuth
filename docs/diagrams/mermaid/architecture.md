flowchart LR
  subgraph Orchestration["Orkiestracja Aspire"]
    AppHost["SmartAuth.AppHost\n(Aspire AppHost)"]
  end

  subgraph WebTier["Warstwa Web"]
    WebHost["SmartAuth.Web\n(host ASP.NET)"]
    SPA["ClientApp\n(SPA React + Vite)"]
    WebHost --> SPA
  end

  subgraph ApiTier["Backend"]
    Api["SmartAuth.Api\n(Minimal API)"]
    Infra["SmartAuth.Infrastructure\n(EF Core, migracje, pgvector)"]
    Domain["SmartAuth.Domain\n(encje, typy współdzielone)"]
    Defaults["SmartAuth.ServiceDefaults\n(OTel, health)"]
    Api --> Infra
    Api --> Domain
    Api --> Defaults
  end

  DB["PostgreSQL authdb\n(pgvector)"]

  AppHost --> WebHost
  AppHost --> Api
  AppHost --> DB

  SPA -->|HTTP/JSON| Api
  Api -->|EF Core| DB
