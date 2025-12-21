using SmartAuth.Infrastructure.Biometrics;
using SmartAuth.Infrastructure.Commons;

namespace SmartAuth.Api.Utilities;

internal static class VoicePayloadDecoder
{
    public static VoiceSamplePayload DecodeBase64(string base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
            throw new ArgumentException(Messages.Biometrics.AudioRequired, nameof(base64));

        var cleaned = StripDataPrefix(base64);
        var buffer = Convert.FromBase64String(cleaned);
        return ParseWav(buffer);
    }

    private static string StripDataPrefix(string raw)
    {
        var span = raw.AsSpan();
        var commaIndex = span.IndexOf(',');
        if (commaIndex >= 0 && span[..commaIndex].Contains("base64", StringComparison.OrdinalIgnoreCase))
            span = span[(commaIndex + 1)..];
        return span.ToString().Trim();
    }

    private static VoiceSamplePayload ParseWav(byte[] buffer)
    {
        try
        {
            using var ms = new MemoryStream(buffer);
            using var br = new BinaryReader(ms);
            var riff = br.ReadBytes(4);
            if (System.Text.Encoding.ASCII.GetString(riff) != "RIFF")
                throw new InvalidDataException();

            br.ReadInt32(); // file length
            var waveId = br.ReadBytes(4);
            if (System.Text.Encoding.ASCII.GetString(waveId) != "WAVE")
                throw new InvalidDataException();

            // fmt chunk
            var fmtId = br.ReadBytes(4);
            if (System.Text.Encoding.ASCII.GetString(fmtId) != "fmt ")
                throw new InvalidDataException();
            var fmtSize = br.ReadInt32();
            var audioFormat = br.ReadInt16();
            var channels = br.ReadInt16();
            var sampleRate = br.ReadInt32();
            br.ReadInt32(); // byte rate
            br.ReadInt16(); // block align
            var bitsPerSample = br.ReadInt16();
            if (fmtSize > 16) br.ReadBytes(fmtSize - 16); // skip extra

            if (audioFormat != 1 || bitsPerSample is not 16)
                throw new InvalidDataException();

            // seek data chunk
            while (ms.Position < ms.Length)
            {
                var chunkId = br.ReadBytes(4);
                var chunkSize = br.ReadInt32();
                var idStr = System.Text.Encoding.ASCII.GetString(chunkId);
                if (idStr == "data")
                {
                    var samples = ReadPcm16(br, chunkSize, channels);
                    return new VoiceSamplePayload(sampleRate, channels, samples);
                }
                ms.Position += chunkSize;
            }

            throw new InvalidDataException();
        }
        catch (Exception ex)
        {
            throw new ArgumentException(Messages.Biometrics.InvalidAudio, nameof(buffer), ex);
        }
    }

    private static float[] ReadPcm16(BinaryReader br, int bytes, int channels)
    {
        var sampleCount = bytes / 2; // 16-bit
        var data = new float[sampleCount];
        for (var i = 0; i < sampleCount; i++)
        {
            var sample = br.ReadInt16();
            data[i] = sample / 32768f;
        }

        if (channels <= 1) return data;

        var frames = sampleCount / channels;
        var mono = new float[frames];
        for (var frame = 0; frame < frames; frame++)
        {
            float sum = 0;
            for (var ch = 0; ch < channels; ch++)
                sum += data[frame * channels + ch];
            mono[frame] = sum / channels;
        }

        return mono;
    }
}
