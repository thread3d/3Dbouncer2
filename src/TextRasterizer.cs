using SkiaSharp;
using System;

namespace TextBouncer;

/// <summary>
/// Renders text to bitmap using SkiaSharp with anti-aliasing.
/// Output format is Bgra8888 for direct GPU texture/buffer compatibility.
/// </summary>
public class TextRasterizer : IDisposable
{
    private const float DefaultFontSize = 72f;
    private const string DefaultFontFamily = "Arial";

    /// <summary>
    /// Renders text to a SkiaSharp bitmap.
    /// </summary>
    /// <param name="text">Text to render</param>
    /// <param name="width">Bitmap width in pixels</param>
    /// <param name="height">Bitmap height in pixels</param>
    /// <param name="textColor">Color to render text (alpha=255 for opaque)</param>
    /// <returns>SKBitmap with Bgra8888 format, or null if text is empty</returns>
    public SKBitmap? RenderText(string text, int width, int height, SKColor textColor)
    {
        if (string.IsNullOrEmpty(text) || width <= 0 || height <= 0)
            return null;

        // Create bitmap with Bgra8888 format (matches OpenGL GL_BGRA)
        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        var bitmap = new SKBitmap(info);

        using (var canvas = new SKCanvas(bitmap))
        {
            // Clear to transparent black
            canvas.Clear(SKColors.Transparent);

            // Setup paint with anti-aliasing enabled (CRITICAL for smooth edges)
            using (var paint = new SKPaint())
            {
                paint.IsAntialias = true;
                paint.Color = textColor;
                paint.TextSize = DefaultFontSize;
                paint.Typeface = SKTypeface.FromFamilyName(DefaultFontFamily);
                paint.TextAlign = SKTextAlign.Center;

                // Calculate text position (centered)
                float x = width / 2f;
                // Measure text for vertical centering
                SKRect textBounds = new SKRect();
                paint.MeasureText(text, ref textBounds);
                float y = (height + textBounds.Height) / 2f;

                canvas.DrawText(text, x, y, paint);
            }
        }

        return bitmap;
    }

    /// <summary>
    /// Gets the raw pixel data from an SKBitmap.
    /// Returns byte array in BGRA format (Blue, Green, Red, Alpha).
    /// </summary>
    public byte[] GetPixelData(SKBitmap bitmap)
    {
        if (bitmap == null)
            throw new ArgumentNullException(nameof(bitmap));

        // SKBitmap.GetPixelSpan provides direct access to pixel memory
        // For Bgra8888, each pixel is 4 bytes: B, G, R, A
        var pixelSpan = bitmap.GetPixelSpan();
        var pixelData = new byte[pixelSpan.Length];
        pixelSpan.CopyTo(pixelData);

        return pixelData;
    }

    /// <summary>
    /// Gets a pixel's alpha value at the specified coordinates.
    /// Uses 0-255 range where 255 = fully opaque.
    /// </summary>
    public byte GetPixelAlpha(SKBitmap bitmap, int x, int y)
    {
        if (bitmap == null || x < 0 || y < 0 || x >= bitmap.Width || y >= bitmap.Height)
            return 0;

        // For Bgra8888, alpha is at byte offset 3 (BGRA)
        int pixelOffset = (y * bitmap.RowBytes) + (x * 4);
        var pixelSpan = bitmap.GetPixelSpan();
        return pixelSpan[pixelOffset + 3]; // Alpha channel
    }

    public void Dispose()
    {
        // TextRasterizer doesn't hold persistent resources
        // Individual SKBitmaps are caller's responsibility
    }
}
