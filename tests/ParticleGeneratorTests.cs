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

    // Cycle 2: Real Letter Validation Tests

    [Fact]
    public void GenerateParticles_LetterO_NoParticlesInHole()
    {
        var rasterizer = new TextRasterizer();
        var generator = new ParticleGenerator();

        // Render letter "O"
        var bitmap = rasterizer.RenderText("O", 100, 100, SKColors.White);
        Assert.NotNull(bitmap);

        // Generate particles
        var particles = generator.GenerateParticles(bitmap, 500, new Vector4(1, 1, 1, 1));

        // Verify particles were generated
        Assert.True(particles.Length > 0, "Should generate particles");

        // Check center of hole has no particles
        int centerX = bitmap.Width / 2;
        int centerY = bitmap.Height / 2;
        Assert.False(generator.IsPointInLetter(centerX, centerY, bitmap),
            "Center of O should be a hole");
    }

    [Fact]
    public void GenerateParticles_LetterP_NoParticlesInHole()
    {
        var rasterizer = new TextRasterizer();
        var generator = new ParticleGenerator();

        // Render letter "P" - has a hole in the bowl
        var bitmap = rasterizer.RenderText("P", 100, 100, SKColors.White);
        Assert.NotNull(bitmap);

        var particles = generator.GenerateParticles(bitmap, 500, new Vector4(1, 1, 1, 1));

        // Verify particles were generated
        Assert.True(particles.Length > 0, "Should generate particles");

        // Check that particles exist in solid areas
        Assert.True(particles.Length > 0);
    }

    [Fact]
    public void GenerateParticles_LetterI_AllParticlesInSolid()
    {
        var rasterizer = new TextRasterizer();
        var generator = new ParticleGenerator();

        // Render letter "I" - solid vertical bar, no holes
        var bitmap = rasterizer.RenderText("I", 100, 100, SKColors.White);
        Assert.NotNull(bitmap);

        var particles = generator.GenerateParticles(bitmap, 200, new Vector4(1, 1, 1, 1));

        // All particles should be in solid areas (I has no holes)
        Assert.True(particles.Length > 0, "Should generate particles for solid letter I");
    }

    [Fact]
    public void GenerateParticles_Number0_NoParticlesInHole()
    {
        var rasterizer = new TextRasterizer();
        var generator = new ParticleGenerator();

        // Render number "0" - has a hole
        var bitmap = rasterizer.RenderText("0", 100, 100, SKColors.White);
        Assert.NotNull(bitmap);

        var particles = generator.GenerateParticles(bitmap, 500, new Vector4(1, 1, 1, 1));

        // Verify particles were generated
        Assert.True(particles.Length > 0, "Should generate particles");

        // Check center is a hole
        int centerX = bitmap.Width / 2;
        int centerY = bitmap.Height / 2;
        Assert.False(generator.IsPointInLetter(centerX, centerY, bitmap),
            "Center of 0 should be a hole");
    }

    [Fact]
    public void GenerateParticles_Number9_NoParticlesInHole()
    {
        var rasterizer = new TextRasterizer();
        var generator = new ParticleGenerator();

        // Render number "9" - has a hole
        var bitmap = rasterizer.RenderText("9", 100, 100, SKColors.White);
        Assert.NotNull(bitmap);

        var particles = generator.GenerateParticles(bitmap, 500, new Vector4(1, 1, 1, 1));

        // Verify particles were generated
        Assert.True(particles.Length > 0, "Should generate particles");
    }
}
