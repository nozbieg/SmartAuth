using System.Buffers.Binary;
using SmartAuth.Infrastructure.Biometrics;

namespace SmartAuth.Api.Utilities;

internal static class ImagePayloadDecoder
{
    public static FaceImagePayload DecodeBase64(string base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
            throw new ArgumentException("Base64 payload is required.", nameof(base64));

        var span = base64.AsSpan();
        var commaIndex = span.IndexOf(',');
        if (commaIndex >= 0)
        {
            var prefix = span[..commaIndex];
            if (prefix.Contains("base64", StringComparison.OrdinalIgnoreCase))
            {
                span = span[(commaIndex + 1)..];
            }
        }

        var cleaned = span.Trim();
        var temp = cleaned.Length % 4 == 0
            ? Convert.FromBase64String(cleaned.ToString())
            : Convert.FromBase64String(cleaned.ToString().PadRight((cleaned.Length + 3) / 4 * 4, '='));

        if (temp.Length < 8)
            throw new ArgumentException("Face payload header is missing dimensions.", nameof(base64));

        var width = BinaryPrimitives.ReadInt32LittleEndian(temp.AsSpan(0, 4));
        var height = BinaryPrimitives.ReadInt32LittleEndian(temp.AsSpan(4, 4));
        if (width <= 0 || height <= 0)
            throw new ArgumentException("Face payload dimensions must be positive.", nameof(base64));

        var expected = checked(width * height * 3);
        if (temp.Length - 8 < expected)
            throw new ArgumentException("Face payload is truncated.", nameof(base64));

        var rgb = new byte[expected];
        Buffer.BlockCopy(temp, 8, rgb, 0, expected);
        return new FaceImagePayload(width, height, rgb);
    }
}
