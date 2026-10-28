using PathLens.Infrastructure.Cameras;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace PathLens.UnitTests.Cameras;

public sealed class CameraFrameAnalyzerTests
{
    private static byte[] Solid(byte value, int size = 64)
    {
        using var image = new Image<Rgba32>(size, size, new Rgba32(value, value, value));
        using var stream = new MemoryStream();
        image.Save(stream, new PngEncoder());
        return stream.ToArray();
    }

    private static byte[] Checkerboard(int size = 64, int block = 8)
    {
        using var image = new Image<Rgba32>(size, size);
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var white = (x / block + y / block) % 2 == 0;
                image[x, y] = white ? new Rgba32(240, 240, 240) : new Rgba32(15, 15, 15);
            }
        }

        using var stream = new MemoryStream();
        image.Save(stream, new PngEncoder());
        return stream.ToArray();
    }

    private static byte[] HorizontalGradient(bool decreasing, int size = 64)
    {
        using var image = new Image<Rgba32>(size, size);
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var value = (byte)(decreasing ? 255 - x * 4 : x * 4);
                image[x, y] = new Rgba32(value, value, value);
            }
        }

        using var stream = new MemoryStream();
        image.Save(stream, new PngEncoder());
        return stream.ToArray();
    }

    [Fact]
    public void Dark_frame_measures_dark_with_a_hash()
    {
        var metrics = CameraFrameAnalyzer.Analyze(Solid(5));

        Assert.True(metrics.Decoded);
        Assert.True(metrics.Brightness < 10, $"brightness was {metrics.Brightness}");
        Assert.False(string.IsNullOrEmpty(metrics.PerceptualHash));
    }

    [Fact]
    public void Flat_bright_frame_has_no_sharpness_but_checkerboard_does()
    {
        var flat = CameraFrameAnalyzer.Analyze(Solid(255));
        var textured = CameraFrameAnalyzer.Analyze(Checkerboard());

        Assert.Equal(0, flat.Sharpness);
        Assert.True(textured.Sharpness > 2, $"sharpness was {textured.Sharpness}");
        Assert.InRange(textured.Brightness, 100, 160);
    }

    [Fact]
    public void Opposite_gradients_are_far_apart_and_identical_frames_match()
    {
        var left = CameraFrameAnalyzer.Analyze(HorizontalGradient(decreasing: false));
        var right = CameraFrameAnalyzer.Analyze(HorizontalGradient(decreasing: true));

        var distance = CameraFrameAnalyzer.HammingDistance(left.PerceptualHash, right.PerceptualHash);
        Assert.NotNull(distance);
        Assert.True(distance!.Value > 30, $"distance was {distance}");

        Assert.Equal(0, CameraFrameAnalyzer.HammingDistance(left.PerceptualHash, left.PerceptualHash));
        Assert.Null(CameraFrameAnalyzer.HammingDistance(null, left.PerceptualHash));
    }

    [Fact]
    public void Undecodable_bytes_are_unknown_never_suitable()
    {
        var metrics = CameraFrameAnalyzer.Analyze(new byte[] { 0xFF, 0xD8, 0xFF, 0x00, 0x01 });

        Assert.False(metrics.Decoded);
        Assert.Null(metrics.PerceptualHash);
    }
}
