# Authentication Flow (Mermaid)

```mermaid
flowchart TD
    Start([Start]) --> Register[Register\nPOST /api/auth/register]
    Register --> Login[Login with email+password\nPOST /api/auth/login]

    Login --> Decision{Any 2FA method required?}

    Decision -- No --> FinalJwt[Issue final access JWT\nrequires2Fa=false]
    FinalJwt --> Done([Authenticated])

    Decision -- Yes --> TempJwt[Issue temporary JWT\nrequires2Fa=true + methods[]]
    TempJwt --> PickMethod[Client selects method\n(code/totp/face/voice)]

    PickMethod --> Verify2FA[Verify second factor\nPOST /api/auth/2fa/*/verify\nAuthorization: Bearer temp token]
    Verify2FA --> Final2[Issue final access JWT]
    Final2 --> Done
```
