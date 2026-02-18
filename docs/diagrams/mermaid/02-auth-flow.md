# Przepływ uwierzytelniania (Mermaid)

```mermaid
flowchart TD
    Start([Start]) --> Register[Rejestracja\nPOST /api/auth/register]
    Register --> Login[Logowanie emailem i hasłem\nPOST /api/auth/login]

    Login --> Decision{Czy wymagana jest metoda 2FA?}

    Decision -- Nie --> FinalJwt[Wydanie końcowego JWT\nrequires2Fa=false]
    FinalJwt --> Done([Uwierzytelniono])

    Decision -- Tak --> TempJwt[Wydanie tokena tymczasowego\nrequires2Fa=true + methods (lista)]
    TempJwt --> PickMethod[Klient wybiera metodę\n(code/totp/face/voice)]

    PickMethod --> Verify2FA[Weryfikacja drugiego składnika\nPOST /api/auth/2fa/*/verify\nAuthorization: Bearer temp token]
    Verify2FA --> Final2[Wydanie końcowego JWT]
    Final2 --> Done
```
