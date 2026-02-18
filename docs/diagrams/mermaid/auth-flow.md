flowchart TD
  Start([Start]) --> Register[Rejestracja\nPOST /api/auth/register]
  Register --> Login[Logowanie\nPOST /api/auth/login]
  Login --> Need2FA{Czy wymagane 2FA?}

  Need2FA -- Nie --> DirectJwt[Zwróć końcowy JWT]
  DirectJwt --> End([Sukces logowania])

  Need2FA -- Tak --> TempToken[Zwróć token tymczasowy + methods[]]
  TempToken --> SelectMethod[Wybór metody 2FA]
  SelectMethod --> Verify[Weryfikacja 2FA\nPOST /api/auth/2fa/*/verify]
  Verify --> FinalJwt[Zwróć końcowy JWT]
  FinalJwt --> End
