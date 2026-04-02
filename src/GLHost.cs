using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.GLControl;
using System;
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

    // Particle system
    private int _particleShaderProgram = 0;
    private int _particleVBO = 0;
    private int _particleVAO = 0;
    private int _particleCount = 0;
    private float _particleSize = 4.0f;
    private int _pointSizeUniformLocation = -1;

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

        _pointSizeUniformLocation = GL.GetUniformLocation(_particleShaderProgram, "uPointSize");
    }

    /// <summary>
    /// Creates the particle VAO/VBO for instanced rendering.
    /// </summary>
    private void InitializeParticleBuffers()
    {
        _particleVAO = GL.GenVertexArray();
        _particleVBO = GL.GenBuffer();

        GL.BindVertexArray(_particleVAO);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _particleVBO);

        // Position (location 0) - 3 floats
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 32, 0);
        GL.EnableVertexAttribArray(0);

        // Color (location 1) - 4 floats, offset by 16 bytes (3 floats + 1 padding)
        GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 32, 16);
        GL.EnableVertexAttribArray(1);

        GL.BindVertexArray(0);
    }

    /// <summary>
    /// Updates the GPU buffer with new particle data.
    /// Handles proper disposal of old buffer to prevent memory leaks.
    /// </summary>
    public void UpdateParticleBuffer(ParticleData[] particles)
    {
        if (!_glLoaded || particles == null || particles.Length == 0)
            return;

        _glControl.MakeCurrent();

        // CRITICAL: Delete old buffer to prevent GPU memory leak
        if (_particleVBO != 0)
        {
            GL.DeleteBuffer(_particleVBO);
        }

        // Create new VBO with particle data
        _particleVBO = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _particleVBO);

        // Upload data
        GL.BufferData(BufferTarget.ArrayBuffer,
            particles.Length * sizeof(float) * 8, // 8 floats per particle (3 pos + 1 pad + 4 color)
            particles,
            BufferUsageHint.StaticDraw);

        // Reconfigure VAO
        GL.BindVertexArray(_particleVAO);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _particleVBO);

        // Position (location 0)
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 32, 0);
        GL.EnableVertexAttribArray(0);

        // Color (location 1)
        GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 32, 16);
        GL.EnableVertexAttribArray(1);

        GL.BindVertexArray(0);
        _particleCount = particles.Length;
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
    /// Renders the particles using point sprites.
    /// </summary>
    private void RenderParticles()
    {
        if (_particleCount == 0) return;

        GL.UseProgram(_particleShaderProgram);

        // Calculate MVP (same as box)
        Matrix4 mvp = _modelMatrix * _viewMatrix * _projectionMatrix;
        int mvpLoc = GL.GetUniformLocation(_particleShaderProgram, "mvp");
        GL.UniformMatrix4(mvpLoc, false, ref mvp);

        // Set point size
        GL.Uniform1(_pointSizeUniformLocation, _particleSize);

        // Enable point sprites
        GL.Enable(EnableCap.PointSprite);
        GL.Enable(EnableCap.ProgramPointSize);

        // Draw particles
        GL.BindVertexArray(_particleVAO);
        GL.DrawArrays(PrimitiveType.Points, 0, _particleCount);
        GL.BindVertexArray(0);

        GL.Disable(EnableCap.PointSprite);
        GL.Disable(EnableCap.ProgramPointSize);
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
    }

    /// <summary>
    /// Application.Idle handler for the game loop.
    /// NOTE: Application.Idle pauses during window resize - this is expected behavior.
    /// </summary>
    private void OnApplicationIdle(object? sender, EventArgs e)
    {
        // Only render when GL is loaded
        // Application.Idle only fires when no messages are pending,
        // so this creates our game loop
        if (_glLoaded)
        {
            _glControl.Invalidate();
        }
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
        if (_particleVBO != 0)
        {
            GL.DeleteBuffer(_particleVBO);
        }
        if (_particleVAO != 0)
        {
            GL.DeleteVertexArray(_particleVAO);
        }
        if (_particleShaderProgram != 0)
        {
            GL.DeleteProgram(_particleShaderProgram);
        }

        _glLoaded = false;
    }
}
