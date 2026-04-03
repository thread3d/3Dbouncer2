using OpenTK.Mathematics;
using SkiaSharp;
using System;

namespace TextBouncer;

/// <summary>
/// Physics simulator implementing fixed timestep physics with elastic box collisions
/// and bitmap-based letter boundary constraints.
/// Uses accumulator pattern for frame-rate independent simulation.
/// </summary>
public class PhysicsSimulator
{
    // Constants for fixed timestep physics
    private const float FixedDt = 1f / 60f;              // 60Hz physics update rate
    private const float MaxAccumulation = 5f * FixedDt;    // Prevent spiral of death (max 5 frames)
    private const float BoxHalfSize = 1.0f;              // Box is 2x2x2 units centered at origin
    private const float ParticleRadius = 0.02f;          // Approximate particle visual size
    private const byte AlphaThreshold = 128;             // Alpha threshold for letter boundary

    // State
    private float _accumulator = 0f;
    private bool _isRunning = true;
    private ControlMode _currentMode = ControlMode.Automatic;

    // Manual override state (used in Manual mode)
    private Vector3 _manualPosition = Vector3.Zero;
    private Vector3 _manualRotation = Vector3.Zero; // Euler angles: pitch, roll, yaw in degrees

    // Performance tracking
    private int _physicsStepsThisFrame = 0;

    // Text bitmap for letter boundary constraints
    private SKBitmap? _textBitmap;
    private int _bitmapWidth;
    private int _bitmapHeight;
    private byte[]? _bitmapPixels;
    private int _bitmapStride;

    /// <summary>
    /// Bounciness factor for collisions. 1.0 = perfectly elastic, 0.0 = no bounce.
    /// </summary>
    public float Bounciness { get; set; } = 1.0f;

    /// <summary>
    /// Full box dimension (default 2.0 for 2x2x2 box).
    /// </summary>
    public float BoxSize { get; set; } = 2.0f;

    /// <summary>
    /// Gets or sets whether physics simulation is running (automatic mode).
    /// </summary>
    public bool IsRunning
    {
        get => _isRunning;
        set => _isRunning = value;
    }

    /// <summary>
    /// Gets or sets the current control mode.
    /// Setting to Manual pauses physics; Automatic or Mixture resumes physics.
    /// </summary>
    public ControlMode Mode
    {
        get => _currentMode;
        set
        {
            _currentMode = value;
            // Update internal running state based on mode
            _isRunning = _currentMode != ControlMode.Manual;
        }
    }

    /// <summary>
    /// Gets the number of physics steps performed in the last Update call.
    /// Useful for debugging and performance monitoring.
    /// </summary>
    public int PhysicsStepsThisFrame => _physicsStepsThisFrame;

    /// <summary>
    /// Gets or sets the manual position override (used in Manual mode).
    /// Applied to all particles uniformly when ApplyManualOverrides is called.
    /// </summary>
    public Vector3 ManualPosition
    {
        get => _manualPosition;
        set => _manualPosition = value;
    }

    /// <summary>
    /// Gets or sets the manual rotation override in Euler angles (pitch, roll, yaw) in degrees.
    /// Used in Manual mode for rotation control. Full rotation implementation deferred to Phase 6.
    /// </summary>
    public Vector3 ManualRotation
    {
        get => _manualRotation;
        set => _manualRotation = value;
    }

    /// <summary>
    /// Sets the text bitmap used for letter boundary constraints.
    /// Particles will be constrained to stay within letter shapes.
    /// </summary>
    public void SetTextBitmap(SKBitmap? bitmap)
    {
        _textBitmap = bitmap;
        if (bitmap != null)
        {
            _bitmapWidth = bitmap.Width;
            _bitmapHeight = bitmap.Height;
            _bitmapStride = bitmap.RowBytes;
            _bitmapPixels = bitmap.GetPixelSpan().ToArray();
        }
        else
        {
            _bitmapPixels = null;
        }
    }

    /// <summary>
    /// Checks if a 3D position is within the letter shapes by mapping to bitmap coordinates.
    /// </summary>
    private bool IsPointInLetter(Vector3 pos)
    {
        if (_textBitmap == null || _bitmapPixels == null)
            return true; // No bitmap = no constraint

        // Map 3D position (-1 to 1) to bitmap coordinates
        int bx = (int)((pos.X + 1.0f) * 0.5f * _bitmapWidth);
        int by = (int)((1.0f - (pos.Y + 1.0f) * 0.5f) * _bitmapHeight);

        // Clamp to bitmap bounds
        if (bx < 0) bx = 0;
        if (bx >= _bitmapWidth) bx = _bitmapWidth - 1;
        if (by < 0) by = 0;
        if (by >= _bitmapHeight) by = _bitmapHeight - 1;

        // Get pixel alpha (BGRA format, alpha at offset 3)
        int pixelOffset = by * _bitmapStride + bx * 4;
        byte alpha = _bitmapPixels[pixelOffset + 3];

        return alpha > AlphaThreshold;
    }

    /// <summary>
    /// Finds the nearest boundary point and returns a push direction.
    /// Uses a simple search outward from the current position.
    /// </summary>
    private Vector3 GetPushDirection(Vector3 pos)
    {
        if (_textBitmap == null || _bitmapPixels == null)
            return Vector3.Zero;

        // Map 3D position to bitmap
        int bx = (int)((pos.X + 1.0f) * 0.5f * _bitmapWidth);
        int by = (int)((1.0f - (pos.Y + 1.0f) * 0.5f) * _bitmapHeight);

        // Search in a spiral pattern to find nearest letter pixel
        for (int radius = 1; radius < 20; radius++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (Math.Abs(dx) != radius && Math.Abs(dy) != radius)
                        continue;

                    int sx = bx + dx;
                    int sy = by + dy;

                    if (sx < 0 || sx >= _bitmapWidth || sy < 0 || sy >= _bitmapHeight)
                        continue;

                    int pixelOffset = sy * _bitmapStride + sx * 4;
                    byte alpha = _bitmapPixels[pixelOffset + 3];

                    if (alpha > AlphaThreshold)
                    {
                        // Found a letter pixel - push away from it
                        float pushX = -dx;
                        float pushY = -dy;
                        float len = (float)Math.Sqrt(pushX * pushX + pushY * pushY);
                        if (len > 0.001f)
                        {
                            pushX /= len;
                            pushY /= len;
                        }
                        return new Vector3(pushX, pushY, 0);
                    }
                }
            }
        }

        // Fallback: push toward center
        return new Vector3(-pos.X, -pos.Y, 0);
    }

    /// <summary>
    /// Updates physics simulation using fixed timestep accumulator pattern.
    /// Call this once per frame with the frame's delta time.
    /// </summary>
    /// <param name="deltaTime">Elapsed time since last frame in seconds</param>
    /// <param name="particles">Particle array to simulate</param>
    public void Update(float deltaTime, ParticleData[] particles)
    {
        if (particles == null || particles.Length == 0)
            return;

        _physicsStepsThisFrame = 0;

        // In Manual mode, physics updates are paused - user controls positions directly
        if (_currentMode == ControlMode.Manual || !_isRunning)
            return;

        // Accumulate time, clamped to prevent spiral of death
        // If game freezes, don't simulate too many steps at once
        _accumulator = Math.Min(_accumulator + deltaTime, MaxAccumulation);

        // Run fixed timestep physics steps
        while (_accumulator >= FixedDt)
        {
            StepSimulation(FixedDt, particles);
            _accumulator -= FixedDt;
            _physicsStepsThisFrame++;
        }
    }

    /// <summary>
    /// Performs a single physics simulation step with collision detection.
    /// Updates all particle positions and handles box boundary collisions
    /// and letter boundary constraints.
    /// </summary>
    /// <param name="dt">Fixed timestep duration in seconds</param>
    /// <param name="particles">Particle array to simulate</param>
    public void StepSimulation(float dt, ParticleData[] particles)
    {
        if (particles == null)
            return;

        float boxHalf = BoxSize / 2f;
        float radius = ParticleRadius;

        for (int i = 0; i < particles.Length; i++)
        {
            ref ParticleData particle = ref particles[i];

            // Update position: p = p + v * dt
            particle.Position += particle.Velocity * dt;

            // First: Box boundary collisions
            // X axis
            if (particle.Position.X > boxHalf - radius)
            {
                particle.Position.X = boxHalf - radius;
                particle.Velocity.X *= -Bounciness;
            }
            else if (particle.Position.X < -boxHalf + radius)
            {
                particle.Position.X = -boxHalf + radius;
                particle.Velocity.X *= -Bounciness;
            }

            // Y axis
            if (particle.Position.Y > boxHalf - radius)
            {
                particle.Position.Y = boxHalf - radius;
                particle.Velocity.Y *= -Bounciness;
            }
            else if (particle.Position.Y < -boxHalf + radius)
            {
                particle.Position.Y = -boxHalf + radius;
                particle.Velocity.Y *= -Bounciness;
            }

            // Z axis
            if (particle.Position.Z > boxHalf - radius)
            {
                particle.Position.Z = boxHalf - radius;
                particle.Velocity.Z *= -Bounciness;
            }
            else if (particle.Position.Z < -boxHalf + radius)
            {
                particle.Position.Z = -boxHalf + radius;
                particle.Velocity.Z *= -Bounciness;
            }

            // Second: Letter boundary constraints
            if (_textBitmap != null)
            {
                // Iteratively push particle back into letter shape
                for (int iter = 0; iter < 5; iter++)
                {
                    if (IsPointInLetter(particle.Position))
                        break;

                    // Get push direction toward nearest letter boundary
                    Vector3 pushDir = GetPushDirection(particle.Position);

                    if (pushDir.LengthSquared > 0.001f)
                    {
                        // Push particle toward boundary
                        float pushStrength = 0.05f;
                        particle.Position.X += pushDir.X * pushStrength;
                        particle.Position.Y += pushDir.Y * pushStrength;

                        // Reflect velocity component along push direction
                        float dot = particle.Velocity.X * pushDir.X + particle.Velocity.Y * pushDir.Y;
                        if (dot < 0)
                        {
                            particle.Velocity.X -= 2 * dot * pushDir.X * Bounciness;
                            particle.Velocity.Y -= 2 * dot * pushDir.Y * Bounciness;
                        }
                    }
                    else
                    {
                        // No boundary found - move particle toward center
                        particle.Position.X *= 0.95f;
                        particle.Position.Y *= 0.95f;
                        particle.Velocity.X *= 0.9f;
                        particle.Velocity.Y *= 0.9f;
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Enables automatic physics simulation (PHYS-04: automatic mode).
    /// </summary>
    public void Start()
    {
        _isRunning = true;
    }

    /// <summary>
    /// Disables automatic physics simulation.
    /// </summary>
    public void Stop()
    {
        _isRunning = false;
    }

    /// <summary>
    /// Resets the accumulator. Call when pausing/unpausing to prevent time spikes.
    /// </summary>
    public void ResetAccumulator()
    {
        _accumulator = 0f;
    }

    /// <summary>
    /// Applies manual position and rotation overrides to all particles.
    /// Should only be called when Mode == ControlMode.Manual.
    /// Currently applies position as uniform translation; rotation deferred to Phase 6.
    /// </summary>
    /// <param name="particles">Particle array to modify</param>
    /// <param name="position">Position offset to apply</param>
    /// <param name="rotation">Rotation in Euler angles (pitch, roll, yaw) in degrees</param>
    public void ApplyManualOverrides(ParticleData[] particles, Vector3 position, Vector3 rotation)
    {
        if (particles == null || particles.Length == 0)
            return;

        for (int i = 0; i < particles.Length; i++)
        {
            // Apply position offset uniformly to all particles
            particles[i].Position += position;
        }

        // Store the rotation values for future Phase 6 implementation
        _manualRotation = rotation;
    }
}
