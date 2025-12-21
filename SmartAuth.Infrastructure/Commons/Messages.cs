namespace SmartAuth.Infrastructure.Commons;

/// <summary>
/// Centralized application messages - easy to localize.
/// All texts used in Result/Error messages should come from this class.
/// </summary>
public static class Messages
{
    /// <summary>
    /// Form field validation messages.
    /// </summary>
    public static class Validation
    {
        public static string Required(string fieldName) => $"{fieldName} jest wymagany/a.";
        public static string Invalid(string fieldName) => $"{fieldName} jest nieprawidłowy/a.";
        public static string TooShort(string fieldName, int minLength) => $"{fieldName} musi mieć co najmniej {minLength} znaków.";
        public static string TooLong(string fieldName, int maxLength) => $"{fieldName} może mieć maksymalnie {maxLength} znaków.";
        public static string InvalidFormat(string fieldName) => $"Format {fieldName} jest nieprawidłowy.";
        
        public const string EmailRequired = "Email jest wymagany.";
        public const string PasswordRequired = "Hasło jest wymagane.";
        public const string CodeRequired = "Kod jest wymagany.";
        public const string SetupIdRequired = "Identyfikator konfiguracji jest wymagany.";
        public const string FaceImageRequired = "Obraz twarzy jest wymagany.";
    }

    /// <summary>
    /// Authorization and authentication error messages.
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
    /// Two-factor authentication (2FA) messages.
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
    /// Success operation messages.
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
    /// Resource error messages (CRUD operations).
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
    /// System error messages.
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
    /// Biometric image processing messages.
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
        public const string NoReferenceForVerification = "Brak zapisanej biometrii twarzy do weryfikacji.";
        public const string NoActiveReferenceMatched = "Żadna aktywna biometria twarzy nie pasuje do próbki.";
        public const string MatchFailed = "Podobieństwo twarzy poniżej wymaganego progu.";
        public const string ImageDecodeFailed = "Dane obrazu twarzy są niespójne z zadeklarowanymi wymiarami.";
        public const string QualityInsufficient = "Jakość obrazu twarzy jest niewystarczająca.";
        public const string LivenessFailed = "Weryfikacja żywotności nie powiodła się.";
        public const string ModelNotAvailable = "Model rozpoznawania twarzy nie jest dostępny.";
        public const string RgbBufferSizeMismatch = "Rozmiar bufora RGB nie odpowiada wymiarom obrazu.";
        public const string EmbeddingDimensionMismatch = "Niezgodność wymiarów wektorów osadzenia.";
        public const string PgvectorBufferMissing = "Brak bufora wektora Pgvector.Vector.";
    }

    /// <summary>
    /// Security and cryptography messages.
    /// </summary>
    public static class Security
    {
        public const string InvalidBase32Character = "Nieprawidłowy znak Base32.";
    }

    /// <summary>
    /// Validation result messages.
    /// </summary>
    public static class ValidationResult
    {
        public static string SingleFieldInvalid(string fieldName, object message) => $"Pole '{fieldName}' jest nieprawidłowe: {message}";
        public static string MultipleFieldsInvalid(string fieldNames, string messages) => $"Pola '{fieldNames}' są nieprawidłowe.\n{messages}";
    }
}

