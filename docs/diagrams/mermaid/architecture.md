%% Referenced workspace files:
%% - [SmartAuth.AppHost](SmartAuth.AppHost/)
%% - [SmartAuth.Api/Program.cs](SmartAuth.Api/Program.cs)
%% - [SmartAuth.Web](SmartAuth.Web/)
%% - [SmartAuth.Infrastructure](SmartAuth.Infrastructure/)
%% - [SmartAuth.Domain](SmartAuth.Domain/)
%% - [SmartAuth.ServiceDefaults](SmartAuth.ServiceDefaults/)
%% - [AGENTS.md](AGENTS.md)

flowchart LR
  subgraph Aspire["Aspire AppHost\n[SmartAuth.AppHost]"]
    direction TB
    Web["Web\n[SmartAuth.Web]\nSPA React/Vite (planned)"]
    Api["Api\n[SmartAuth.Api/Program.cs]"]
    Postgres["Postgres\npgvector (planned)"]
  end

  Aspire --> Web
  Aspire --> Api
  Aspire --> Postgres

  Web -->|HTTP / REST / fetch| Api
  Api -->|uses| Infrastructure["Infrastructure\n[SmartAuth.Infrastructure]"]
  Api -->|uses| Domain["Domain\n[SmartAuth.Domain]"]
  Api -->|uses| ServiceDefaults["ServiceDefaults (telemetry)\n[SmartAuth.ServiceDefaults]"]
  Api -->|connects to| Postgres

  classDef planned fill:#fff3c4,stroke:#f5b400,stroke-width:2;
  class Postgres,Web planned