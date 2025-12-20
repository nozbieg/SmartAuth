namespace SmartAuth.Infrastructure.Commons;

public static class Messages
{
    /// <summary>
    /// Komunikaty walidacji pól formularzy
    /// </summary>
    public static class Validation
    {
        public static string Required(string fieldName) => $"{fieldName} jest wymagany/a.";
        public static string Invalid(string fieldName) => $"{fieldName} jest nieprawidłowy/a.";
        public static string TooShort(string fieldName, int minLength) => $"{fieldName} musi mieć co najmniej {minLength} znaków.";
        public static string TooLong(string fieldName, int maxLength) => $"{fieldName} może mieć maksymalnie {maxLength} znaków.";
        public static string InvalidFormat(string fieldName) => $"Format {fieldName} jest nieprawidłowy.";
        
        // Konkretne pola
        public const string EmailRequired = "Email jest wymagany.";
        public const string PasswordRequired = "Hasło jest wymagane.";
        public const string CodeRequired = "Kod jest wymagany.";
        public const string SetupIdRequired = "Identyfikator konfiguracji jest wymagany.";
        public const string FaceImageRequired = "Obraz twarzy jest wymagany.";
    }

    /// <summary>
    /// Komunikaty błędów autoryzacji i uwierzytelniania
    /// </summary>
    public static class Auth
    {
        public const string Unauthorized = "Nie masz uprawnień do wykonania tej operacji.";
        public const string Forbidden = "Dostęp do tego zasobu jest zabroniony.";
        public const string InvalidCredentials = "Podane dane logowania są nieprawidłowe.";
        public const string SessionExpired = "Sesja wygasła. Zaloguj się ponownie.";
        public const string AccountInactive = "Konto jest nieaktywne.";
        public const string AccountLocked = "Konto zostało zablokowane.";
    }

    /// <summary>
    /// Komunikaty związane z 2FA (weryfikacją dwuskładnikową)
    /// </summary>
    public static class TwoFactor
    {
        public const string TotpEnabled = "Weryfikacja TOTP została włączona.";
        public const string TotpDisabled = "Weryfikacja TOTP została wyłączona.";
        public const string TotpAlreadyEnabled = "Weryfikacja TOTP jest już włączona.";
        public const string TotpInvalidConfiguration = "Nieprawidłowa konfiguracja TOTP.";
        
        public const string FaceEnabled = "Weryfikacja twarzy została włączona.";
        public const string FaceDisabled = "Weryfikacja twarzy została wyłączona.";
        public const string Face2FaDisabled = "Weryfikacja twarzy 2FA jest wyłączona.";
    }

    /// <summary>
    /// Komunikaty sukcesu operacji
    /// </summary>
    public static class Success
    {
        public const string RegistrationCompleted = "Rejestracja zakończona pomyślnie.";
        public const string LoginSuccessful = "Logowanie zakończone pomyślnie.";
        public const string OperationCompleted = "Operacja zakończona pomyślnie.";
        public const string DataSaved = "Dane zostały zapisane.";
        public const string DataDeleted = "Dane zostały usunięte.";
    }

    /// <summary>
    /// Komunikaty błędów zasobów (CRUD)
    /// </summary>
    public static class Resource
    {
        public static string NotFound(string entity) => $"Nie znaleziono {entity}.";
        public static string NotFoundWithId(string entity, string id) => $"Nie znaleziono {entity} '{id}'.";
        public static string AlreadyExists(string entity) => $"{entity} już istnieje.";
        public static string CannotDelete(string entity) => $"Nie można usunąć {entity}.";
        public static string CannotUpdate(string entity) => $"Nie można zaktualizować {entity}.";
    }

    /// <summary>
    /// Komunikaty błędów systemowych
    /// </summary>
    public static class System
    {
        public const string InternalError = "Wystąpił nieoczekiwany błąd. Spróbuj ponownie później.";
        public const string ServiceUnavailable = "Usługa jest tymczasowo niedostępna.";
        public const string MissingHttpContext = "Brak kontekstu HTTP.";
        public const string ConfigurationError = "Błąd konfiguracji systemu.";
        public const string DatabaseError = "Błąd połączenia z bazą danych.";
    }

    /// <summary>
    /// Komunikaty związane z przetwarzaniem obrazów biometrycznych
    /// </summary>
    public static class Biometrics
    {
        public const string Base64Required = "Dane Base64 są wymagane.";
        public const string MissingDimensions = "Nagłówek danych twarzy nie zawiera wymiarów.";
        public const string InvalidDimensions = "Wymiary danych twarzy muszą być dodatnie.";
        public const string TruncatedData = "Dane twarzy są niekompletne.";
        public const string InvalidFaceData = "Nieprawidłowe dane obrazu twarzy.";
        public const string FaceNotDetected = "Nie wykryto twarzy na obrazie.";
        public const string MultipleFacesDetected = "Wykryto więcej niż jedną twarz na obrazie.";
        public const string LowQualityImage = "Jakość obrazu jest zbyt niska.";
    }
}

