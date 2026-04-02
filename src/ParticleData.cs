using OpenTK.Mathematics;
using System.Runtime.InteropServices;

namespace TextBouncer;

/// <summary>
/// Particle data structure for GPU SSBO (Shader Storage Buffer Object).
/// Uses Sequential layout for direct memory mapping to GPU buffers.
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
    /// Particle color as RGBA (0-1 range per channel).
    /// </summary>
    public Vector4 Color;

    /// <summary>
    /// Creates a new ParticleData with the specified position and color.
    /// </summary>
    public ParticleData(Vector3 position, Vector4 color)
    {
        Position = position;
        Padding = 0;
        Color = color;
    }

    /// <summary>
    /// Returns the size of the structure in bytes (32 bytes total).
    /// Position (12) + Padding (4) + Color (16) = 32 bytes.
    /// </summary>
    public static int SizeInBytes => 32;
}
