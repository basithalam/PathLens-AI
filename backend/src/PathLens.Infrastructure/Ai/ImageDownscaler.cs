using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace PathLens.Infrastructure.Ai;

/// <summary>
/// Prepares a photo for AI analysis: downscales the longest edge and
/// re-encodes as JPEG. Re-encoding also strips EXIF metadata (including any
/// embedded GPS) before the image leaves the server. If the image cannot be
/// processed, the original bytes are used — preprocessing must never be the
/// reason an analysis fails.
/// </summary>
public static class ImageDownscaler
{
    public const int MaxDimension = 1568;

    public static (byte[] Bytes, string ContentType) DownscaleForAnalysis(byte[] imageBytes, string contentType)
    {
        try
        {
            using var image = Image.Load(imageBytes);
            if (image.Width <= MaxDimension && image.Height <= MaxDimension &&
                string.Equals(contentType, "image/jpeg", StringComparison.OrdinalIgnoreCase))
            {
                return (imageBytes, contentType);
            }

            image.Mutate(context => context.Resize(new ResizeOptions
            {
                Size = new Size(MaxDimension, MaxDimension),
                Mode = ResizeMode.Max,
            }));

            using var output = new MemoryStream();
            image.Save(output, new JpegEncoder { Quality = 85 });
            return (output.ToArray(), "image/jpeg");
        }
        catch (UnknownImageFormatException)
        {
            return (imageBytes, contentType);
        }
        catch (InvalidImageContentException)
        {
            return (imageBytes, contentType);
        }
    }
}
