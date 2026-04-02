using SkiaSharp;
using Xunit;
using OpenTK.Mathematics;

namespace TextBouncer.Tests;

public class ParticleGeneratorTests
{
    [Fact]
    public void IsPointInLetter_SolidCenter_ReturnsTrue()
    {
        // Create 10x10 bitmap with filled center
        var bitmap = CreateSolidCenterBitmap();
        var generator = new ParticleGenerator();

        bool result = generator.IsPointInLetter(5, 5, bitmap);

        Assert.True(result); // Center should be inside
    }

    [Fact]
    public void IsPointInLetter_OutsideBitmap_ReturnsFalse()
    {
        var bitmap = new SKBitmap(10, 10);
        var generator = new ParticleGenerator();

        bool result = generator.IsPointInLetter(-1, -1, bitmap);

        Assert.False(result);
    }

    [Fact]
    public void IsPointInLetter_HollowRing_InsideHole_ReturnsFalse()
    {
        // Create bitmap with hollow ring (O shape)
        var bitmap = CreateHollowRingBitmap();
        var generator = new ParticleGenerator();

        // Point in center of ring (the hole)
        bool result = generator.IsPointInLetter(5, 5, bitmap);

        Assert.False(result); // Inside hole should NOT be in letter
    }

    [Fact]
    public void IsPointInLetter_HollowRing_OutsideHole_ReturnsTrue()
    {
        var bitmap = CreateHollowRingBitmap();
        var generator = new ParticleGenerator();

        // Point in the ring material
        bool result = generator.IsPointInLetter(2, 5, bitmap);

        Assert.True(result); // In ring material should be in letter
    }

    private SKBitmap CreateSolidCenterBitmap()
    {
        var bitmap = new SKBitmap(10, 10);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Transparent);
            using (var paint = new SKPaint { Color = SKColors.White })
            {
                canvas.DrawRect(2, 2, 6, 6, paint); // Filled 6x6 center
            }
        }
        return bitmap;
    }

    private SKBitmap CreateHollowRingBitmap()
    {
        // Create a bitmap with a donut shape:
        // Outer filled circle at center, inner hole created by transparent circle
        var bitmap = new SKBitmap(10, 10);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Transparent);

            // Draw filled outer circle (radius 4)
            using (var fillPaint = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill })
            {
                canvas.DrawCircle(5, 5, 4, fillPaint);
            }

            // Cut out inner hole by drawing transparent circle (radius 2)
            using (var clearPaint = new SKPaint { Color = SKColors.Transparent, Style = SKPaintStyle.Fill, BlendMode = SKBlendMode.Src })
            {
                canvas.DrawCircle(5, 5, 2, clearPaint);
            }
        }
        return bitmap;
    }
}
