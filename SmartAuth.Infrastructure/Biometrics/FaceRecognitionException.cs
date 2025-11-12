namespace SmartAuth.Infrastructure.Biometrics;

public sealed class FaceRecognitionException : Exception
{
    public FaceRecognitionException(string code, string message)
        : base(message) => Code = code;

    public FaceRecognitionException(string code, string message, Exception inner)
        : base(message, inner) => Code = code;

    public string Code { get; }
}
