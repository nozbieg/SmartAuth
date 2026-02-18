sequenceDiagram
  autonumber
  actor User as Użytkownik
  participant SPA as SmartAuth.Web/ClientApp
  participant API as SmartAuth.Api
  participant DB as authdb (PostgreSQL)

  Użytkownik->>SPA: Podaje email i hasło
  SPA->>API: POST /api/auth/login
  API->>DB: Pobranie użytkownika + metod 2FA
  DB-->>API: Dane użytkownika

  alt 2FA wymagane
    API-->>SPA: 200 { requires2Fa: true, token: tempJwt, methods[] }
    Użytkownik->>SPA: Wprowadza kod / biometrię
    SPA->>API: POST /api/auth/2fa/*/verify (Bearer tempJwt)
    API-->>SPA: 200 { jwt: accessJwt }
  else 2FA niewymagane
    API-->>SPA: 200 { requires2Fa: false, token: accessJwt }
  end
