using SkiaSharp;
using OpenTK.Mathematics;
using System;

namespace TextBouncer;

/// <summary>
/// Generates particles distributed across letter shapes.
/// Uses scanline even-odd algorithm for proper hole detection.
/// </summary>
public class ParticleGenerator
{
    private const byte AlphaThreshold = 128; // Middle gray for anti-aliasing

    /// <summary>
    /// Determines if a point is inside the letter (not in a hole).
    /// Uses even-odd rule: cast ray left, count boundary crossings.
    /// Odd count = inside letter, Even count = outside or in hole.
    /// </summary>
    public bool IsPointInLetter(int x, int y, SKBitmap bitmap)
    {
        if (bitmap == null || x < 0 || y < 0 || x >= bitmap.Width || y >= bitmap.Height)
            return false;

        // Get pixel span for direct access
        var pixelSpan = bitmap.GetPixelSpan();
        int stride = bitmap.RowBytes;
        int width = bitmap.Width;

        // Cast ray from (0, y) to (x-1, y), count crossings
        // Even-odd rule: count boundary crossings to the LEFT of the point
        // Odd count = inside solid, Even count = outside or in hole
        int crossingCount = 0;
        bool wasInside = false;

        // Scan from left edge to just before the point
        for (int scanX = 0; scanX < x; scanX++)
        {
            int pixelOffset = (y * stride) + (scanX * 4);
            byte alpha = pixelSpan[pixelOffset + 3]; // Alpha channel (BGRA)
            bool isInside = alpha > AlphaThreshold;

            // Count ALL boundary crossings
            if (isInside != wasInside)
            {
                crossingCount++;
            }

            wasInside = isInside;
        }

        // Even-odd rule: odd crossings = inside solid, even crossings = outside/hole
        return (crossingCount % 2) == 1;
    }

    /// <summary>
    /// Generates particles distributed across letter areas (excluding holes).
    /// Uses rejection sampling with even-odd point-in-letter test.
    /// </summary>
    public ParticleData[] GenerateParticles(
        SKBitmap textBitmap,
        int targetParticleCount,
        Vector4 particleColor)
    {
        if (textBitmap == null || targetParticleCount <= 0)
            return Array.Empty<ParticleData>();

        var particles = new System.Collections.Generic.List<ParticleData>();
        int width = textBitmap.Width;
        int height = textBitmap.Height;
        Random random = new Random();

        // Rejection sampling: generate points, keep if in letter
        int attempts = 0;
        int maxAttempts = targetParticleCount * 10; // Prevent infinite loop

        while (particles.Count < targetParticleCount && attempts < maxAttempts)
        {
            attempts++;

            // Generate random point in bitmap space
            int x = random.Next(width);
            int y = random.Next(height);

            // Test with even-odd rule
            if (IsPointInLetter(x, y, textBitmap))
            {
                // Convert bitmap coordinates to 3D space
                // Center at origin, scale to fit in box
                float normalizedX = (x / (float)width) * 2.0f - 1.0f; // -1 to 1
                float normalizedY = -((y / (float)height) * 2.0f - 1.0f); // Flip Y, -1 to 1
                float z = 0.0f;

                var position = new Vector3(normalizedX, normalizedY, z);
                particles.Add(new ParticleData(position, particleColor));
            }
        }

        return particles.ToArray();
    }
}
