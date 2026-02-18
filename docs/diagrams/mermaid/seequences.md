%% Referenced workspace files:
%% - [SmartAuth.Web](SmartAuth.Web/)
%% - [SmartAuth.Api/Program.cs](SmartAuth.Api/Program.cs)
%% - [SmartAuth.Infrastructure](SmartAuth.Infrastructure/)
%% - [SmartAuth.Domain](SmartAuth.Domain/)
%% - [README.md](README.md)
%% - [AGENTS.md](AGENTS.md)

sequenceDiagram
  participant User as User
  participant SPA as SPA\n[SmartAuth.Web/ClientApp]
  participant Api as Api\n[SmartAuth.Api/Program.cs]
  participant DB as "Auth DB\n(Postgres / pgvector) (planned)"
  participant TwoFA as "2FA Service\n(dev placeholder)"

  Note over TwoFA,Api: 2FA may currently be a dev placeholder

  User->>SPA: Open app and submit email + password
  SPA->>Api: POST /login { email, password }
  Api->>Api: Validate credentials\n(uses [SmartAuth.Infrastructure](SmartAuth.Infrastructure/) & [SmartAuth.Domain](SmartAuth.Domain/))
  Api->>DB: Query user & 2FA requirement (if applicable)

  alt 2FA required
    Api-->>SPA: 200 { temp_jwt, available_2fa_methods }
    SPA->>Api: POST /2fa/verify { method, code, temp_jwt }
    Api->>TwoFA: Verify code (dev placeholder behavior)
    TwoFA-->>Api: verification result
    Api-->>SPA: 200 { access_jwt }
  else no 2FA
    Api-->>SPA: 200 { access_jwt }
  end

  SPA->>User: Store access_jwt and proceed
```// filepath: docs/diagrams/mermaid/sequence-login.md
%% Referenced workspace files:
%% - [SmartAuth.Web](SmartAuth.Web/)
%% - [SmartAuth.Api/Program.cs](SmartAuth.Api/Program.cs)
%% - [SmartAuth.Infrastructure](SmartAuth.Infrastructure/)
%% - [SmartAuth.Domain](SmartAuth.Domain/)
%% - [README.md](README.md)
%% - [AGENTS.md](AGENTS.md)

sequenceDiagram
  participant User as User
  participant SPA as SPA\n[SmartAuth.Web/ClientApp]
  participant Api as Api\n[SmartAuth.Api/Program.cs]
  participant DB as "Auth DB\n(Postgres / pgvector) (planned)"
  participant TwoFA as "2FA Service\n(dev placeholder)"

  Note over TwoFA,Api: 2FA may currently be a dev placeholder

  User->>SPA: Open app and submit email + password
  SPA->>Api: POST /login { email, password }
  Api->>Api: Validate credentials\n(uses [SmartAuth.Infrastructure](SmartAuth.Infrastructure/) & [SmartAuth.Domain](SmartAuth.Domain/))
  Api->>DB: Query user & 2FA requirement (if applicable)

  alt 2FA required
    Api-->>SPA: 200 { temp_jwt, available_2fa_methods }
    SPA->>Api: POST /2fa/verify { method, code, temp_jwt }
    Api->>TwoFA: Verify code (dev placeholder behavior)
    TwoFA-->>Api: verification result
    Api-->>SPA: 200 { access_jwt }
  else no 2FA
    Api-->>SPA: 200 { access_jwt }
  end

  SPA->>User: Store access_jwt and proceed