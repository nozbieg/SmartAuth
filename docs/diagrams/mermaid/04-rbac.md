```mermaid
flowchart LR
  %% ====== Klasyczny RBAC (ANSI/NIST) ======
  U[Użytkownik] -->|przypisanie roli (UA)| R[Rola]
  R -->|nadanie uprawnienia (PA)| P[Uprawnienie]

  P -->|zezwala na| O[Operacja / Akcja]
  O -->|wykonywana na| Z[Zasób]

  %% Sesja (kontekst wykonania)
  U -->|logowanie / start| S[Sesja]
  S -->|aktywuje role| R
  S -->|egzekwuje| P

  %% Hierarchia ról (opcjonalnie)
  R2[Rola nadrzędna] -->|dziedziczenie| R3[Rola podrzędna]

  %% Ograniczenia / polityki (opcjonalnie)
  C[Ograniczenia (np. SoD, czas, lokalizacja)] -.-> S
  C -.-> R
  C -.-> P
  ```