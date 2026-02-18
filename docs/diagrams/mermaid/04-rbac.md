```mermaid
graph LR
  U["Użytkownik"] -->|UA: przypisanie roli| R["Rola"]
  R -->|PA: nadanie uprawnienia| P["Uprawnienie"]
  P -->|pozwala na| O["Operacja"]
  O -->|na| Z["Zasób"]

  U -->|start sesji| S["Sesja"]
  S -->|aktywuje role| R
  S -->|egzekwuje| P

  RN["Rola nadrzędna"] -->|dziedziczenie| RP["Rola podrzędna"]

  C["Ograniczenia: SoD / czas / lokalizacja"] -.-> S
  C -.-> R
  C -.-> P

  ```