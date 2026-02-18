flowchart LR
  subgraph Orchestration["Aspire Orchestration"]
    AppHost["SmartAuth.AppHost\n(Aspire AppHost)"]
  end

  subgraph WebTier["Web"]
    WebHost["SmartAuth.Web\n(ASP.NET host)"]
    SPA["ClientApp\n(React + Vite SPA)"]
    WebHost --> SPA
  end

  subgraph ApiTier["Backend"]
    Api["SmartAuth.Api\n(Minimal API)"]
    Infra["SmartAuth.Infrastructure\n(EF Core, migrations, pgvector)"]
    Domain["SmartAuth.Domain\n(Entities, shared types)"]
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
