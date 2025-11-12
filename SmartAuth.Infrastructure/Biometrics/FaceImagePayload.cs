namespace SmartAuth.Infrastructure.Biometrics;

public readonly record struct FaceImagePayload(int Width, int Height, byte[] Rgb);
