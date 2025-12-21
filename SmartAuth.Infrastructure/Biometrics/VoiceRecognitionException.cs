namespace SmartAuth.Infrastructure.Biometrics;

public sealed class VoiceRecognitionException : Exception
{
    public VoiceRecognitionException(string code, string message)
        : base(message) => Code = code;

    public VoiceRecognitionException(string code, string message, Exception inner)
        : base(message, inner) => Code = code;

    public string Code { get; }
}
