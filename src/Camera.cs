using OpenTK.Mathematics;
using System;

namespace TextBouncer;

/// <summary>
/// Orbit camera using spherical coordinates for 3D particle viewing.
/// Supports orbit (left-drag), zoom (scroll), and pan (right-drag) controls.
/// </summary>
public class Camera
{
    // Spherical coordinates
    private float _distance = 10.0f;
    private float _yaw = 0.0f;      // Azimuth angle in radians
    private float _pitch = 0.3f;     // Elevation angle in radians

    // Target point (center of orbit)
    private Vector3 _target = Vector3.Zero;

    // Projection parameters
    private float _width = 800.0f;
    private float _height = 600.0f;
    private float _fov = MathHelper.DegreesToRadians(45.0f);
    private float _nearPlane = 0.1f;
    private float _farPlane = 100.0f;

    // Cached matrices (recalculated on access)
    private bool _viewDirty = true;
    private bool _projectionDirty = true;
    private Matrix4 _cachedViewMatrix;
    private Matrix4 _cachedProjectionMatrix;

    // Constraints from PITFALLS.md
    private const float MinPitch = -MathF.PI / 2.0f + 0.01f;  // -89 degrees (avoid gimbal lock)
    private const float MaxPitch = MathF.PI / 2.0f - 0.01f;   // +89 degrees
    private const float MinDistance = 1.0f;
    private const float MaxDistance = 50.0f;

    /// <summary>
    /// Creates a new camera with the specified viewport dimensions.
    /// </summary>
    public Camera(float width, float height)
    {
        _width = width;
        _height = height;
    }

    /// <summary>
    /// Updates the camera orbit based on mouse delta (left-drag).
    /// Positive deltaYaw rotates camera right, positive deltaPitch rotates up.
    /// </summary>
    public void UpdateOrbit(float deltaYaw, float deltaPitch)
    {
        _yaw += deltaYaw;
        _pitch += deltaPitch;

        // Clamp pitch to prevent gimbal lock
        _pitch = MathHelper.Clamp(_pitch, MinPitch, MaxPitch);

        _viewDirty = true;
    }

    /// <summary>
    /// Updates the camera zoom based on scroll wheel delta.
    /// Positive delta zooms in (decreases distance), negative zooms out.
    /// </summary>
    public void UpdateZoom(float deltaZoom)
    {
        _distance -= deltaZoom;
        _distance = MathHelper.Clamp(_distance, MinDistance, MaxDistance);
        _viewDirty = true;
    }

    /// <summary>
    /// Updates the camera target point based on mouse delta (right-drag).
    /// Moves the center of orbit in camera space.
    /// </summary>
    public void UpdatePan(float deltaX, float deltaY)
    {
        // Calculate camera basis vectors for panning
        Vector3 position = CalculatePosition();
        Vector3 forward = Vector3.Normalize(_target - position);
        Vector3 right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
        Vector3 up = Vector3.Cross(right, forward);

        // Pan in world space
        _target += right * deltaX + up * deltaY;
        _viewDirty = true;
    }

    /// <summary>
    /// Returns the view matrix (world-to-camera transform).
    /// </summary>
    public Matrix4 GetViewMatrix()
    {
        if (_viewDirty)
        {
            Vector3 position = CalculatePosition();
            _cachedViewMatrix = Matrix4.LookAt(position, _target, Vector3.UnitY);
            _viewDirty = false;
        }
        return _cachedViewMatrix;
    }

    /// <summary>
    /// Returns the projection matrix (camera-to-clip transform).
    /// </summary>
    public Matrix4 GetProjectionMatrix()
    {
        if (_projectionDirty)
        {
            float aspectRatio = _width / _height;
            _cachedProjectionMatrix = Matrix4.CreatePerspectiveFieldOfView(
                _fov, aspectRatio, _nearPlane, _farPlane);
            _projectionDirty = false;
        }
        return _cachedProjectionMatrix;
    }

    /// <summary>
    /// Updates the viewport dimensions and marks projection for recalculation.
    /// </summary>
    public void Resize(float width, float height)
    {
        _width = width;
        _height = height;
        _projectionDirty = true;
    }

    /// <summary>
    /// Converts spherical coordinates to Cartesian position.
    /// x = distance * cos(pitch) * sin(yaw)
    /// y = distance * sin(pitch)
    /// z = distance * cos(pitch) * cos(yaw)
    /// </summary>
    private Vector3 CalculatePosition()
    {
        float cosPitch = MathF.Cos(_pitch);
        float sinPitch = MathF.Sin(_pitch);
        float cosYaw = MathF.Cos(_yaw);
        float sinYaw = MathF.Sin(_yaw);

        float x = _distance * cosPitch * sinYaw;
        float y = _distance * sinPitch;
        float z = _distance * cosPitch * cosYaw;

        return _target + new Vector3(x, y, z);
    }
}
