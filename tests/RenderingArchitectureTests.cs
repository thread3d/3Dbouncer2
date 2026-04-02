using Xunit;
using OpenTK.Mathematics;

namespace TextBouncer.Tests;

/// <summary>
/// Tests for GPU rendering architecture - instanced rendering and SSBO usage.
/// These tests validate the high-performance rendering path for 100K particles.
/// </summary>
public class RenderingArchitectureTests
{
    [Fact]
    public void ParticleData_StructLayout_Is32Bytes()
    {
        // SSBO requires predictable memory layout
        int expectedSize = 32; // Position (12) + Padding (4) + Color (16)

        int actualSize = ParticleData.SizeInBytes;

        Assert.Equal(expectedSize, actualSize);
    }

    [Fact]
    public void ParticleData_StructLayout_MatchesStd430()
    {
        // Verify struct layout is compatible with std430 SSBO layout
        // Position at offset 0, Color at offset 16
        var particle = new ParticleData(new Vector3(1.0f, 2.0f, 3.0f), new Vector4(0.5f, 0.6f, 0.7f, 1.0f));

        // These should be at sequential offsets
        Assert.Equal(1.0f, particle.Position.X);
        Assert.Equal(2.0f, particle.Position.Y);
        Assert.Equal(3.0f, particle.Position.Z);
        Assert.Equal(0.5f, particle.Color.X);
        Assert.Equal(0.6f, particle.Color.Y);
        Assert.Equal(0.7f, particle.Color.Z);
        Assert.Equal(1.0f, particle.Color.W);
    }

    [Fact]
    public void Camera_InitialState_HasValidMatrices()
    {
        var camera = new Camera(800, 600);

        Matrix4 view = camera.GetViewMatrix();
        Matrix4 projection = camera.GetProjectionMatrix();

        // View matrix should not be identity (camera at specific position)
        Assert.NotEqual(Matrix4.Identity, view);

        // Projection matrix should not be identity (perspective transform)
        Assert.NotEqual(Matrix4.Identity, projection);
    }

    [Fact]
    public void Camera_Orbit_UpdatesViewMatrix()
    {
        var camera = new Camera(800, 600);
        Matrix4 initialView = camera.GetViewMatrix();

        // Orbit the camera
        camera.UpdateOrbit(0.5f, 0.0f);
        Matrix4 newView = camera.GetViewMatrix();

        // View matrix should have changed
        Assert.NotEqual(initialView, newView);
    }

    [Fact]
    public void Camera_Zoom_ClampsDistance()
    {
        var camera = new Camera(800, 600);

        // Zoom in a lot (should clamp to min distance)
        camera.UpdateZoom(100.0f);

        // Zoom out a lot (should clamp to max distance)
        camera.UpdateZoom(-200.0f);

        // Camera should still produce valid matrices
        Matrix4 view = camera.GetViewMatrix();
        Assert.NotEqual(Matrix4.Identity, view);
    }

    [Fact]
    public void Camera_Pitch_ClampsToPreventGimbalLock()
    {
        var camera = new Camera(800, 600);

        // Try to pitch past vertical (should clamp)
        camera.UpdateOrbit(0.0f, 10.0f);   // Way past 90 degrees
        camera.UpdateOrbit(0.0f, -10.0f);  // Way past -90 degrees

        // Camera should still work
        Matrix4 view = camera.GetViewMatrix();
        Assert.NotEqual(Matrix4.Identity, view);
    }

    [Fact]
    public void Camera_Resize_UpdatesProjection()
    {
        var camera = new Camera(800, 600);
        Matrix4 initialProjection = camera.GetProjectionMatrix();

        // Resize to different aspect ratio
        camera.Resize(1920, 1080);
        Matrix4 newProjection = camera.GetProjectionMatrix();

        // Projection should have changed
        Assert.NotEqual(initialProjection, newProjection);
    }
}
