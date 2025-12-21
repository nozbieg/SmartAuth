using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SmartAuth.Infrastructure.Commons;

namespace SmartAuth.Infrastructure.Biometrics;

/// <summary>
/// Face detector using UltraFace ONNX model (version-RFB-640).
/// Input: [1, 3, 480, 640] - NCHW, RGB, normalized to [-1, 1]
/// Outputs: "scores" [1, 17640, 2], "boxes" [1, 17640, 4]
/// </summary>
public sealed class OnnxFaceDetector : IFaceDetector, IDisposable
{
    private readonly Lazy<InferenceSession?> _session;

    private const int ModelWidth = 640;
    private const int ModelHeight = 480;
    private const float ConfidenceThreshold = 0.7f;
    private const float NmsThreshold = 0.3f;
    private const int MaxFaces = 10;
    private const int MinFaceSize = 20;

    public OnnxFaceDetector(BiometricsOptions opts)
    {
        _session = new Lazy<InferenceSession?>(() => OnnxSessionFactory.Create(opts.FaceDetectorModelPath));
    }

    public Task<FaceDetectionResult> DetectAsync(byte[] rgbImage, int width, int height, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(rgbImage);
        if (rgbImage.Length != width * height * 3)
            throw new ArgumentException(Messages.Biometrics.RgbBufferSizeMismatch, nameof(rgbImage));

        ct.ThrowIfCancellationRequested();

        var session = _session.Value 
            ?? throw new FaceRecognitionException("MODEL_NOT_AVAILABLE", Messages.Biometrics.ModelNotAvailable);

        return Task.FromResult(RunInference(session, rgbImage, width, height));
    }

    private FaceDetectionResult RunInference(InferenceSession session, byte[] rgbImage, int width, int height)
    {
        var inputMeta = session.InputMetadata.First();
        var scaleX = (float)width / ModelWidth;
        var scaleY = (float)height / ModelHeight;

        var inputTensor = PrepareInputTensor(rgbImage, width, height);
        var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(inputMeta.Key, inputTensor) };

        using var outputs = session.Run(inputs);
        var candidates = ParseOutputs(outputs.ToList(), scaleX, scaleY, width, height);

        if (candidates.Count == 0)
            return new FaceDetectionResult([], width, height);

        var faces = ApplyNms(candidates, NmsThreshold)
            .OrderByDescending(c => c.Box.Area)
            .Take(MaxFaces)
            .Select(c => new FaceCandidate(c.Box, c.Landmarks, c.Score))
            .ToList();

        return new FaceDetectionResult(faces, width, height);
    }

    private List<(FaceBoundingBox Box, FaceLandmarks Landmarks, float Score)> ParseOutputs(
        List<DisposableNamedOnnxValue> outputs, float scaleX, float scaleY, int imageWidth, int imageHeight)
    {
        var candidates = new List<(FaceBoundingBox, FaceLandmarks, float)>();

        // Find outputs by name or shape
        var scoresOutput = outputs.FirstOrDefault(o => o.Name.Contains("score")) 
            ?? outputs.FirstOrDefault(o => (o.Value as DenseTensor<float>)?.Dimensions[^1] == 2);
        var boxesOutput = outputs.FirstOrDefault(o => o.Name.Contains("box")) 
            ?? outputs.FirstOrDefault(o => (o.Value as DenseTensor<float>)?.Dimensions[^1] == 4);

        if (scoresOutput?.Value is not DenseTensor<float> scoresTensor ||
            boxesOutput?.Value is not DenseTensor<float> boxesTensor)
            return candidates;

        var scores = scoresTensor.ToArray();
        var boxes = boxesTensor.ToArray();
        var numDetections = scores.Length / 2;

        for (var i = 0; i < numDetections; i++)
        {
            var faceScore = scores[i * 2 + 1]; // [background, face]
            if (faceScore < ConfidenceThreshold)
                continue;

            // Boxes are normalized [x1, y1, x2, y2]
            var boxIdx = i * 4;
            if (boxIdx + 3 >= boxes.Length) continue;

            var x1 = boxes[boxIdx] * ModelWidth * scaleX;
            var y1 = boxes[boxIdx + 1] * ModelHeight * scaleY;
            var x2 = boxes[boxIdx + 2] * ModelWidth * scaleX;
            var y2 = boxes[boxIdx + 3] * ModelHeight * scaleY;

            var x = Math.Clamp((int)x1, 0, imageWidth - 1);
            var y = Math.Clamp((int)y1, 0, imageHeight - 1);
            var w = Math.Clamp((int)(x2 - x1), 1, imageWidth - x);
            var h = Math.Clamp((int)(y2 - y1), 1, imageHeight - y);

            if (w < MinFaceSize || h < MinFaceSize)
                continue;

            var box = new FaceBoundingBox(x, y, w, h);
            candidates.Add((box, new FaceLandmarks(EstimateLandmarks(box)), faceScore));
        }

        return candidates;
    }

    private DenseTensor<float> PrepareInputTensor(byte[] rgbImage, int srcWidth, int srcHeight)
    {
        // NCHW format, normalized to [-1, 1]: (pixel - 127) / 128
        var tensor = new DenseTensor<float>([1, 3, ModelHeight, ModelWidth]);

        for (var y = 0; y < ModelHeight; y++)
        {
            for (var x = 0; x < ModelWidth; x++)
            {
                // Bilinear interpolation
                var srcX = (float)x / ModelWidth * srcWidth;
                var srcY = (float)y / ModelHeight * srcHeight;
                var x0 = (int)srcX;
                var y0 = (int)srcY;
                var x1 = Math.Min(x0 + 1, srcWidth - 1);
                var y1 = Math.Min(y0 + 1, srcHeight - 1);
                var xFrac = srcX - x0;
                var yFrac = srcY - y0;

                var idx00 = (y0 * srcWidth + x0) * 3;
                var idx01 = (y0 * srcWidth + x1) * 3;
                var idx10 = (y1 * srcWidth + x0) * 3;
                var idx11 = (y1 * srcWidth + x1) * 3;

                for (var c = 0; c < 3; c++)
                {
                    var v00 = idx00 + c < rgbImage.Length ? rgbImage[idx00 + c] : 0f;
                    var v01 = idx01 + c < rgbImage.Length ? rgbImage[idx01 + c] : 0f;
                    var v10 = idx10 + c < rgbImage.Length ? rgbImage[idx10 + c] : 0f;
                    var v11 = idx11 + c < rgbImage.Length ? rgbImage[idx11 + c] : 0f;

                    var value = v00 * (1 - xFrac) * (1 - yFrac) +
                                v01 * xFrac * (1 - yFrac) +
                                v10 * (1 - xFrac) * yFrac +
                                v11 * xFrac * yFrac;

                    tensor[0, c, y, x] = (value - 127f) / 128f;
                }
            }
        }

        return tensor;
    }

    private static List<(FaceBoundingBox Box, FaceLandmarks Landmarks, float Score)> ApplyNms(
        List<(FaceBoundingBox Box, FaceLandmarks Landmarks, float Score)> candidates, float threshold)
    {
        var sorted = candidates.OrderByDescending(c => c.Score).ToList();
        var keep = new List<(FaceBoundingBox, FaceLandmarks, float)>();

        while (sorted.Count > 0)
        {
            var best = sorted[0];
            keep.Add(best);
            sorted.RemoveAt(0);
            sorted.RemoveAll(c => ComputeIou(best.Box, c.Box) > threshold);
        }

        return keep;
    }

    private static float ComputeIou(FaceBoundingBox a, FaceBoundingBox b)
    {
        var x1 = Math.Max(a.X, b.X);
        var y1 = Math.Max(a.Y, b.Y);
        var x2 = Math.Min(a.Right, b.Right);
        var y2 = Math.Min(a.Bottom, b.Bottom);

        var intersection = Math.Max(0, x2 - x1) * Math.Max(0, y2 - y1);
        var union = a.Area + b.Area - intersection;
        return union > 0 ? (float)intersection / union : 0;
    }

    private static float[] EstimateLandmarks(FaceBoundingBox box) =>
    [
        box.X + box.Width * 0.3f, box.Y + box.Height * 0.35f,   // left eye
        box.X + box.Width * 0.7f, box.Y + box.Height * 0.35f,   // right eye
        box.X + box.Width * 0.5f, box.Y + box.Height * 0.55f,   // nose
        box.X + box.Width * 0.35f, box.Y + box.Height * 0.75f,  // left mouth
        box.X + box.Width * 0.65f, box.Y + box.Height * 0.75f   // right mouth
    ];

    public void Dispose()
    {
        if (_session.IsValueCreated) _session.Value?.Dispose();
    }
}

