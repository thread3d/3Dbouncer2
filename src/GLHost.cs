using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.GLControl;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace TextBouncer;

/// <summary>
/// Manages OpenGL context lifecycle and rendering for the 3D Text Bouncer application.
/// Implements critical race condition protection via _glLoaded flag.
/// </summary>
public class GLHost
{
    private GLControl _glControl = null!;
    private bool _glLoaded = false;

    // Shader program
    private int _shaderProgram = 0;
    private int _mvpUniformLocation = -1;

    // Box geometry
    private int _vao = 0;
    private int _vbo = 0;
    private int _ebo = 0;

    // Camera/Projection
    private Matrix4 _projectionMatrix;
    private Matrix4 _viewMatrix;
    private Matrix4 _modelMatrix;

    // Window dimensions (guarded against zero)
    private int _width = 800;
    private int _height = 600;

    // Particle system - Instanced rendering with SSBO
    private int _particleShaderProgram = 0;
    private int _particleSSBO = 0;       // Shader Storage Buffer Object for particle data
    private int _quadVBO = 0;            // Base quad mesh (4 vertices)
    private int _quadVAO = 0;
    private int _particleCount = 0;
    private float _particleSize = 4.0f;
    private int _particleSizeUniformLocation = -1;
    private int _particleMvpUniformLocation = -1;

    // Camera
    private Camera _camera = null!;

    // Physics simulation
    private PhysicsSimulator _physics = new();
    private ParticleData[]? _particleData;  // Cached CPU-side particle data
    private long _lastFrameTime;            // For delta time calculation

    /// <summary>
    /// Initializes the GLHost with the specified GLControl.
    /// </summary>
    public void Initialize(GLControl glControl)
    {
        _glControl = glControl ?? throw new ArgumentNullException(nameof(glControl));

        // Wire up GLControl events with proper lifecycle management
        _glControl.Load += OnGLControlLoad;
        _glControl.Paint += OnGLControlPaint;
        _glControl.Resize += OnResize;
    }

    /// <summary>
    /// Called when the GLControl is loaded and the OpenGL context is available.
    /// CRITICAL: This is the ONLY safe place to call GL operations.
    /// </summary>
    private void OnGLControlLoad(object? sender, EventArgs e)
    {
        // CRITICAL: Context is now ready - safe to use GL
        _glControl.MakeCurrent();
        _glLoaded = true;

        // Set configurable background color (CornflowerBlue default)
        GL.ClearColor(0.39f, 0.58f, 0.93f, 1.0f);

        // Enable depth testing
        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Less);

        // Setup viewport
        _width = _glControl.Width;
        _height = _glControl.Height;
        if (_height == 0) _height = 1; // Guard against divide by zero
        GL.Viewport(0, 0, _width, _height);

        // Initialize shader program
        InitializeShaders();

        // Initialize box geometry
        InitializeBoxGeometry();

        // Setup camera matrices
        UpdateProjectionMatrix();
        _viewMatrix = Matrix4.LookAt(
            new Vector3(3, 3, 5),  // Camera position
            new Vector3(0, 0, 0),   // Look at origin
            new Vector3(0, 1, 0)    // Up vector
        );
        _modelMatrix = Matrix4.Identity;

        // Initialize particle shaders and buffers
        InitializeParticleShaders();
        InitializeParticleBuffers();

        // Initialize camera
        _camera = new Camera(_width, _height);
        _viewMatrix = _camera.GetViewMatrix();

        // Initialize frame timing for physics
        _lastFrameTime = Stopwatch.GetTimestamp();

        // Start the game loop
        Application.Idle += OnApplicationIdle;
    }

    /// <summary>
    /// Compiles and links the shader program.
    /// </summary>
    private void InitializeShaders()
    {
        string vertexShaderSource = File.ReadAllText(Path.Combine("Shaders", "box.vert"));
        string fragmentShaderSource = File.ReadAllText(Path.Combine("Shaders", "box.frag"));

        // Compile vertex shader
        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexShaderSource);
        GL.CompileShader(vertexShader);

        GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int vertexStatus);
        if (vertexStatus != 1)
        {
            string log = GL.GetShaderInfoLog(vertexShader);
            throw new InvalidOperationException($"Vertex shader compilation failed: {log}");
        }

        // Compile fragment shader
        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentShaderSource);
        GL.CompileShader(fragmentShader);

        GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out int fragmentStatus);
        if (fragmentStatus != 1)
        {
            string log = GL.GetShaderInfoLog(fragmentShader);
            throw new InvalidOperationException($"Fragment shader compilation failed: {log}");
        }

        // Link program
        _shaderProgram = GL.CreateProgram();
        GL.AttachShader(_shaderProgram, vertexShader);
        GL.AttachShader(_shaderProgram, fragmentShader);
        GL.LinkProgram(_shaderProgram);

        GL.GetProgram(_shaderProgram, GetProgramParameterName.LinkStatus, out int linkStatus);
        if (linkStatus != 1)
        {
            string log = GL.GetProgramInfoLog(_shaderProgram);
            throw new InvalidOperationException($"Shader program linking failed: {log}");
        }

        // Cleanup shaders (they're now linked into the program)
        GL.DetachShader(_shaderProgram, vertexShader);
        GL.DetachShader(_shaderProgram, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        // Get uniform locations
        _mvpUniformLocation = GL.GetUniformLocation(_shaderProgram, "mvp");
    }

    /// <summary>
    /// Creates the box VAO/VBO with cube geometry.
    /// </summary>
    private void InitializeBoxGeometry()
    {
        // Cube vertices (8 corners)
        float[] vertices = new float[]
        {
            // Front face (z = 1)
            -1.0f, -1.0f,  1.0f,  // 0: bottom-left
             1.0f, -1.0f,  1.0f,  // 1: bottom-right
             1.0f,  1.0f,  1.0f,  // 2: top-right
            -1.0f,  1.0f,  1.0f,  // 3: top-left
            // Back face (z = -1)
            -1.0f, -1.0f, -1.0f,  // 4: bottom-left
             1.0f, -1.0f, -1.0f,  // 5: bottom-right
             1.0f,  1.0f, -1.0f,  // 6: top-right
            -1.0f,  1.0f, -1.0f,  // 7: top-left
        };

        // Indices for 12 triangles (6 faces * 2 triangles each)
        uint[] indices = new uint[]
        {
            // Front face
            0, 1, 2,  0, 2, 3,
            // Back face
            4, 6, 5,  4, 7, 6,
            // Left face
            4, 0, 3,  4, 3, 7,
            // Right face
            1, 5, 6,  1, 6, 2,
            // Top face
            3, 2, 6,  3, 6, 7,
            // Bottom face
            4, 5, 1,  4, 1, 0,
        };

        // Create VAO
        _vao = GL.GenVertexArray();
        GL.BindVertexArray(_vao);

        // Create VBO
        _vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        // Create EBO
        _ebo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

        // Configure vertex attribute
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        // Unbind
        GL.BindVertexArray(0);
    }

    /// <summary>
    /// Compiles and links the particle shader program.
    /// </summary>
    private void InitializeParticleShaders()
    {
        string vertexSource = File.ReadAllText(Path.Combine("Shaders", "particle.vert"));
        string fragmentSource = File.ReadAllText(Path.Combine("Shaders", "particle.frag"));

        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexSource);
        GL.CompileShader(vertexShader);
        GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int vStatus);
        if (vStatus != 1) throw new InvalidOperationException($"Particle vertex shader failed: {GL.GetShaderInfoLog(vertexShader)}");

        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentSource);
        GL.CompileShader(fragmentShader);
        GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out int fStatus);
        if (fStatus != 1) throw new InvalidOperationException($"Particle fragment shader failed: {GL.GetShaderInfoLog(fragmentShader)}");

        _particleShaderProgram = GL.CreateProgram();
        GL.AttachShader(_particleShaderProgram, vertexShader);
        GL.AttachShader(_particleShaderProgram, fragmentShader);
        GL.LinkProgram(_particleShaderProgram);

        GL.DetachShader(_particleShaderProgram, vertexShader);
        GL.DetachShader(_particleShaderProgram, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        _particleMvpUniformLocation = GL.GetUniformLocation(_particleShaderProgram, "mvp");
        _particleSizeUniformLocation = GL.GetUniformLocation(_particleShaderProgram, "uParticleSize");
    }

    /// <summary>
    /// Creates the base quad mesh and SSBO for instanced rendering.
    /// Uses a single triangle strip with 4 vertices for each particle instance.
    /// </summary>
    private void InitializeParticleBuffers()
    {
        // Create base quad mesh (4 vertices for triangle strip: BL, BR, TL, TR)
        // This quad is instanced for each particle
        float[] quadVertices = {
            -0.5f, -0.5f,  // bottom-left (0)
             0.5f, -0.5f,  // bottom-right (1)
            -0.5f,  0.5f,  // top-left (2)
             0.5f,  0.5f   // top-right (3)
        };

        _quadVAO = GL.GenVertexArray();
        _quadVBO = GL.GenBuffer();
        _particleSSBO = GL.GenBuffer();

        // Setup quad VAO/VBO
        GL.BindVertexArray(_quadVAO);

        // Upload quad vertices (location 0, divisor 0 - same for all instances)
        GL.BindBuffer(BufferTarget.ArrayBuffer, _quadVBO);
        GL.BufferData(BufferTarget.ArrayBuffer, quadVertices.Length * sizeof(float), quadVertices, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 0, 0);
        GL.EnableVertexAttribArray(0);

        // Unbind
        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
    }

    /// <summary>
    /// Updates the SSBO with new particle data using buffer orphaning pattern.
    /// This prevents GPU stalls by discarding the old buffer before uploading new data.
    /// Also caches the particle data for physics simulation.
    /// </summary>
    public void UpdateParticleBuffer(ParticleData[] particles)
    {
        if (!_glLoaded || particles == null)
            return;

        // Cache particles for physics simulation
        _particleData = particles;

        UploadParticlesToGPU(particles);
    }

    /// <summary>
    /// Uploads particle data to GPU using buffer orphaning pattern.
    /// Separated from UpdateParticleBuffer for re-upload during physics updates.
    /// </summary>
    private void UploadParticlesToGPU(ParticleData[] particles)
    {
        if (!_glLoaded || particles == null || particles.Length == 0)
            return;

        _glControl.MakeCurrent();
        _particleCount = particles.Length;

        // Bind SSBO
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _particleSSBO);

        // Buffer orphaning pattern from PITFALLS.md:
        // 1. Allocate new storage (orphans old buffer, prevents GPU stall)
        // 2. Upload data with BufferSubData
        int bufferSize = particles.Length * ParticleData.SizeInBytes;
        GL.BufferData(BufferTarget.ShaderStorageBuffer,
            bufferSize,
            IntPtr.Zero,  // Orphan existing buffer
            BufferUsageHint.DynamicDraw);

        GL.BufferSubData(BufferTarget.ShaderStorageBuffer,
            IntPtr.Zero,
            bufferSize,
            particles);

        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
    }

    /// <summary>
    /// Sets the particle size for rendering.
    /// </summary>
    public void SetParticleSize(float size)
    {
        _particleSize = MathHelper.Clamp(size, 1.0f, 50.0f);
    }

    /// <summary>
    /// Updates the projection matrix based on current viewport dimensions.
    /// </summary>
    private void UpdateProjectionMatrix()
    {
        float aspectRatio = (float)_width / _height;
        _projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(45.0f),
            aspectRatio,
            0.1f,
            100.0f
        );
    }

    /// <summary>
    /// Called when the GLControl needs to be painted.
    /// CRITICAL: Must check _glLoaded before any GL operations.
    /// </summary>
    private void OnGLControlPaint(object? sender, PaintEventArgs e)
    {
        RenderFrame();
    }

    /// <summary>
    /// Renders a single frame.
    /// CRITICAL: Guarded by _glLoaded flag to prevent AccessViolationException.
    /// </summary>
    public void RenderFrame()
    {
        // CRITICAL GUARD: Don't render if GL context isn't ready
        if (!_glLoaded) return;

        _glControl.MakeCurrent();

        // Clear buffers
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Render the box
        RenderBox();

        // Render particles (will blend with box)
        RenderParticles();

        // Swap buffers (use glControl, NOT GL.SwapBuffers)
        _glControl.SwapBuffers();
    }

    /// <summary>
    /// Renders particles using instanced rendering with SSBO.
    /// Single draw call for all particles: glDrawArraysInstanced.
    /// </summary>
    private void RenderParticles()
    {
        if (_particleCount == 0 || _particleSSBO == 0) return;

        GL.UseProgram(_particleShaderProgram);

        // Bind SSBO to binding point 0 for shader access
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, _particleSSBO);

        // Calculate MVP matrix from camera
        Matrix4 view = _camera.GetViewMatrix();
        Matrix4 projection = _camera.GetProjectionMatrix();
        Matrix4 mvp = view * projection;
        GL.UniformMatrix4(_particleMvpUniformLocation, false, ref mvp);

        // Set particle size uniform
        GL.Uniform1(_particleSizeUniformLocation, _particleSize);

        // Instanced draw: 4 vertices (quad), _particleCount instances
        // This is the key performance optimization - single draw call for 100K particles
        GL.BindVertexArray(_quadVAO);
        GL.DrawArraysInstanced(PrimitiveType.TriangleStrip, 0, 4, _particleCount);
        GL.BindVertexArray(0);

        // Unbind SSBO
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, 0);
        GL.UseProgram(0);
    }

    /// <summary>
    /// Renders the semi-transparent box with proper alpha blending.
    /// </summary>
    private void RenderBox()
    {
        // Enable alpha blending
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        // Use shader program
        GL.UseProgram(_shaderProgram);

        // Calculate MVP matrix
        Matrix4 mvp = _modelMatrix * _viewMatrix * _projectionMatrix;
        GL.UniformMatrix4(_mvpUniformLocation, false, ref mvp);

        // Bind VAO and draw
        GL.BindVertexArray(_vao);
        GL.DrawElements(PrimitiveType.Triangles, 36, DrawElementsType.UnsignedInt, 0);

        // Cleanup
        GL.BindVertexArray(0);
        GL.UseProgram(0);
        GL.Disable(EnableCap.Blend);
    }

    /// <summary>
    /// Called when the window is resized.
    /// CRITICAL: Guarded by _glLoaded and height == 0 check.
    /// </summary>
    public void OnResize(object? sender, EventArgs e)
    {
        // CRITICAL GUARD: Don't resize if GL context isn't ready
        if (!_glLoaded) return;

        _glControl.MakeCurrent();

        _width = _glControl.Width;
        _height = _glControl.Height;

        // CRITICAL GUARD: Prevent divide by zero in viewport/projection calculations
        if (_height == 0) _height = 1;

        GL.Viewport(0, 0, _width, _height);
        UpdateProjectionMatrix();

        // Update camera projection
        _camera?.Resize(_width, _height);
    }

    /// <summary>
    /// Orbits the camera based on mouse delta (left-drag).
    /// </summary>
    public void OrbitCamera(float deltaYaw, float deltaPitch)
    {
        if (!_glLoaded) return;
        _camera?.UpdateOrbit(deltaYaw, deltaPitch);
    }

    /// <summary>
    /// Zooms the camera based on scroll wheel delta.
    /// </summary>
    public void ZoomCamera(float deltaZoom)
    {
        if (!_glLoaded) return;
        _camera?.UpdateZoom(deltaZoom);
    }

    /// <summary>
    /// Pans the camera target point (right-drag).
    /// </summary>
    public void PanCamera(float deltaX, float deltaY)
    {
        if (!_glLoaded) return;
        _camera?.UpdatePan(deltaX, deltaY);
    }

    /// <summary>
    /// Application.Idle handler for the game loop.
    /// Runs physics simulation before rendering for frame-rate independent behavior.
    /// NOTE: Application.Idle pauses during window resize - this is expected behavior.
    /// </summary>
    private void OnApplicationIdle(object? sender, EventArgs e)
    {
        if (!_glLoaded)
            return;

        // Calculate delta time for physics using high-precision timer
        var currentTime = Stopwatch.GetTimestamp();
        float deltaTime = (float)(currentTime - _lastFrameTime) / Stopwatch.Frequency;
        _lastFrameTime = currentTime;

        // Clamp delta time to prevent physics explosion on frame drops
        // (e.g., when window is minimized or system is under heavy load)
        deltaTime = Math.Min(deltaTime, 0.1f);

        // Run physics if we have particles and automatic mode is enabled
        if (_particleData != null && _particleData.Length > 0)
        {
            _physics.Update(deltaTime, _particleData);

            // Re-upload particle data to GPU after physics update
            // This ensures GPU renders the latest positions
            UploadParticlesToGPU(_particleData);
        }

        // Trigger render
        _glControl.Invalidate();
    }

    /// <summary>
    /// Cleans up GL resources.
    /// </summary>
    public void Dispose()
    {
        if (!_glLoaded) return;

        Application.Idle -= OnApplicationIdle;

        // Delete box resources
        GL.DeleteBuffer(_vbo);
        GL.DeleteBuffer(_ebo);
        GL.DeleteVertexArray(_vao);
        GL.DeleteProgram(_shaderProgram);

        // CRITICAL: Delete particle resources to prevent memory leak
        // SSBO for instanced rendering
        if (_particleSSBO != 0)
        {
            GL.DeleteBuffer(_particleSSBO);
        }
        // Quad mesh for instanced rendering
        if (_quadVBO != 0)
        {
            GL.DeleteBuffer(_quadVBO);
        }
        if (_quadVAO != 0)
        {
            GL.DeleteVertexArray(_quadVAO);
        }
        if (_particleShaderProgram != 0)
        {
            GL.DeleteProgram(_particleShaderProgram);
        }

        _glLoaded = false;
    }
}
