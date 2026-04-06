using SkiaSharp;
using System;
using System.Numerics;

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
    /// Measures the dimensions required to render text with the default font.
    /// </summary>
    /// <param name="text">Text to measure</param>
    /// <returns>Vector2 where X=width, Y=height in pixels</returns>
    public Vector2 MeasureText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return Vector2.Zero;

        using var paint = new SKPaint
        {
            IsAntialias = true,
            TextSize = DefaultFontSize,
            Typeface = SKTypeface.FromFamilyName(DefaultFontFamily)
        };

        // Get the actual bounding box - this includes full glyph extents
        var bounds = new SKRect();
        paint.MeasureText(text, ref bounds);

        // Width must include the full glyph bounds
        float width = bounds.Width;
        // Height includes ascent and descent
        float height = Math.Max(Math.Abs(bounds.Top) + Math.Abs(bounds.Bottom), DefaultFontSize);

        // Ensure minimum dimensions
        width = Math.Max(width, 10f);

        return new Vector2(width, height);
    }

    /// <summary>
    /// Renders text to a SkiaSharp bitmap with auto-sized dimensions.
    /// </summary>
    /// <param name="text">Text to render</param>
    /// <param name="textColor">Color to render text (alpha=255 for opaque)</param>
    /// <param name="padding">Extra padding around text in pixels</param>
    /// <returns>SKBitmap with Bgra8888 format, or null if text is empty</returns>
    public SKBitmap? RenderText(string text, SKColor textColor, float padding = 20f)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        // Measure text to determine required size
        var measured = MeasureText(text);
        int width = (int)Math.Ceiling(measured.X + padding * 2);
        int height = (int)Math.Ceiling(measured.Y + padding * 2);

        return RenderText(text, width, height, textColor);
    }

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
            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color = textColor,
                TextSize = DefaultFontSize,
                Typeface = SKTypeface.FromFamilyName(DefaultFontFamily)
            };

            // Measure text for proper centering
            var bounds = new SKRect();
            paint.MeasureText(text, ref bounds);

            // Calculate text position (centered in bitmap)
            // bounds.Width gives full glyph width, bounds.Left is typically 0 or negative
            float textWidth = bounds.Width;
            float x = (width - textWidth) / 2f;
            // Vertical: center in bitmap, adjusting for baseline
            // bounds.Top is negative (above baseline), so subtract it to get proper baseline
            float y = (height + Math.Abs(bounds.Top) + Math.Abs(bounds.Bottom)) / 2f;

            canvas.DrawText(text, x, y, paint);
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
