using PathLens.Infrastructure.Ai;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PathLens.UnitTests;

public class ImageDownscalerTests
{
    [Fact]
    public void Large_image_is_downscaled_to_max_dimension_as_jpeg()
    {
        using var image = new Image<Rgba32>(2200, 1100);
        using var stream = new MemoryStream();
        image.SaveAsPng(stream);

        var (bytes, contentType) = ImageDownscaler.DownscaleForAnalysis(stream.ToArray(), "image/png");

        Assert.Equal("image/jpeg", contentType);
        using var result = Image.Load(bytes);
        Assert.True(result.Width <= ImageDownscaler.MaxDimension);
        Assert.True(result.Height <= ImageDownscaler.MaxDimension);
    }

    [Fact]
    public void Unreadable_bytes_are_returned_unchanged()
    {
        var original = new byte[] { 1, 2, 3, 4 };
        var (bytes, contentType) = ImageDownscaler.DownscaleForAnalysis(original, "image/png");
        Assert.Equal(original, bytes);
        Assert.Equal("image/png", contentType);
    }
}
