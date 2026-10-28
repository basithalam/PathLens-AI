using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PathLens.Infrastructure.Cameras;

public sealed record FrameMetrics(
    bool Decoded,
    double Brightness,
    double Sharpness,
    string? PerceptualHash);

/// <summary>
/// Cheap local frame measurements for the camera quality gate: mean
/// brightness (0-255), a Laplacian-variance sharpness proxy, and a 64-bit
/// difference hash for view-change detection. No AI is involved; a frame
/// that cannot be decoded returns Decoded=false and is treated as
/// unknown quality, never as suitable.
/// </summary>
public static class CameraFrameAnalyzer
{
    public static FrameMetrics Analyze(byte[] imageBytes)
    {
        try
        {
            using var image = Image.Load<Rgba32>(imageBytes);

            // A frame this small cannot be judged for blur or view; call it
            // unknown rather than suitable or unsuitable.
            if (image.Width < 16 || image.Height < 16)
            {
                return new FrameMetrics(false, 0, 0, null);
            }

            // Small grayscale copy for brightness/sharpness statistics.
            using var small = image.Clone(context => context
                .Resize(new ResizeOptions { Size = new Size(96, 96), Mode = ResizeMode.Max })
                .Grayscale());

            var width = small.Width;
            var height = small.Height;
            var gray = new double[width * height];
            var sum = 0d;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var value = small[x, y].R;
                    gray[y * width + x] = value;
                    sum += value;
                }
            }

            var brightness = gray.Length == 0 ? 0 : sum / gray.Length;
            var sharpness = LaplacianVariance(gray, width, height);
            var hash = DifferenceHash(image);

            return new FrameMetrics(true, brightness, sharpness, hash);
        }
        catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException or NotSupportedException)
        {
            return new FrameMetrics(false, 0, 0, null);
        }
    }

    private static double LaplacianVariance(double[] gray, int width, int height)
    {
        if (width < 3 || height < 3)
        {
            return 0;
        }

        var values = new List<double>((width - 2) * (height - 2));
        for (var y = 1; y < height - 1; y++)
        {
            for (var x = 1; x < width - 1; x++)
            {
                var i = y * width + x;
                values.Add(4 * gray[i] - gray[i - 1] - gray[i + 1] - gray[i - width] - gray[i + width]);
            }
        }

        if (values.Count == 0)
        {
            return 0;
        }

        var mean = values.Average();
        return values.Average(value => (value - mean) * (value - mean));
    }

    /// <summary>64-bit difference hash: each bit says "brighter than the next pixel".</summary>
    private static string DifferenceHash(Image<Rgba32> image)
    {
        using var tiny = image.Clone(context => context
            .Resize(new ResizeOptions { Size = new Size(9, 8), Mode = ResizeMode.Stretch })
            .Grayscale());

        ulong hash = 0;
        for (var y = 0; y < 8; y++)
        {
            for (var x = 0; x < 8; x++)
            {
                hash <<= 1;
                if (tiny[x, y].R > tiny[x + 1, y].R)
                {
                    hash |= 1;
                }
            }
        }

        return hash.ToString("x16");
    }

    /// <summary>Hamming distance between two 64-bit hashes; null-safe (null = unknown).</summary>
    public static int? HammingDistance(string? left, string? right)
    {
        if (left is null || right is null ||
            !ulong.TryParse(left, System.Globalization.NumberStyles.HexNumber, null, out var a) ||
            !ulong.TryParse(right, System.Globalization.NumberStyles.HexNumber, null, out var b))
        {
            return null;
        }

        return System.Numerics.BitOperations.PopCount(a ^ b);
    }
}
