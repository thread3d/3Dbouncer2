using OpenTK.Mathematics;
using System.Runtime.InteropServices;

namespace TextBouncer;

/// <summary>
/// Particle data structure for GPU SSBO (Shader Storage Buffer Object).
/// Uses Sequential layout for direct memory mapping to GPU buffers.
/// Total size: 48 bytes for std430 layout compatibility.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ParticleData
{
    /// <summary>
    /// Particle position in 3D space (x, y, z).
    /// </summary>
    public Vector3 Position;

    /// <summary>
    /// Padding to align Position to 16 bytes for std140 layout compatibility.
    /// </summary>
    public float Padding;

    /// <summary>
    /// Particle velocity in 3D space (x, y, z) units per second.
    /// </summary>
    public Vector3 Velocity;

    /// <summary>
    /// Padding to align Velocity to 16 bytes for std140 layout compatibility.
    /// </summary>
    public float VelocityPadding;

    /// <summary>
    /// Particle color as RGBA (0-1 range per channel).
    /// </summary>
    public Vector4 Color;

    /// <summary>
    /// Creates a new ParticleData with the specified position and color.
    /// Velocity is set to zero by default.
    /// </summary>
    public ParticleData(Vector3 position, Vector4 color)
    {
        Position = position;
        Padding = 0;
        Velocity = Vector3.Zero;
        VelocityPadding = 0;
        Color = color;
    }

    /// <summary>
    /// Creates a new ParticleData with the specified position, velocity, and color.
    /// </summary>
    public ParticleData(Vector3 position, Vector3 velocity, Vector4 color)
    {
        Position = position;
        Padding = 0;
        Velocity = velocity;
        VelocityPadding = 0;
        Color = color;
    }

    /// <summary>
    /// Returns the size of the structure in bytes (48 bytes total).
    /// Position (16) + Velocity (16) + Color (16) = 48 bytes.
    /// </summary>
    public static int SizeInBytes => 48;
}
