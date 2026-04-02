using OpenTK.GLControl;
using OpenTK.Mathematics;
using SkiaSharp;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace TextBouncer;

/// <summary>
/// Main application window hosting the OpenTK GLControl for 3D rendering.
/// Implements proper OpenGL lifecycle management via GLHost.
/// </summary>
public partial class MainForm : Form
{
    private GLControl _glControl = null!;
    private GLHost _glHost = null!;

    // Text input and rasterization
    private TextBox _textInput = null!;
    private TextRasterizer _textRasterizer = null!;
    private SKBitmap? _currentTextBitmap = null;
    private ParticleGenerator _particleGenerator = null!;

    // UI Controls
    private Button _colorButton = null!;
    private ColorDialog _colorDialog = null!;
    private Label _colorLabel = null!;

    private TrackBar _particleCountSlider = null!;
    private Label _particleCountLabel = null!;

    private TrackBar _particleSizeSlider = null!;
    private Label _particleSizeLabel = null!;

    // Current state
    private Color _currentTextColor = Color.White;
    private int _currentParticleCount = 10000;
    private float _currentParticleSize = 4.0f;

    public MainForm()
    {
        InitializeComponent();
        InitializeGLControl();
        InitializeTextInput();
        InitializeParticleSystem();
        SetupUIControls();
        RegenerateParticles();
    }

    /// <summary>
    /// Initializes the text input field and rasterizer for text-to-particle conversion.
    /// </summary>
    private void InitializeTextInput()
    {
        // Initialize text rasterizer
        _textRasterizer = new TextRasterizer();

        // Create text input box
        _textInput = new TextBox
        {
            Text = "HELLO",
            Location = new Point(10, 10),
            Width = 200,
            Parent = this
        };
        _textInput.TextChanged += OnTextInputChanged;
    }

    /// <summary>
    /// Initializes the particle generator for text-to-particle conversion.
    /// </summary>
    private void InitializeParticleSystem()
    {
        _particleGenerator = new ParticleGenerator();
    }

    /// <summary>
    /// Sets up the UI controls for particle configuration.
    /// </summary>
    private void SetupUIControls()
    {
        int yPos = 50;

        // Color picker button
        _colorButton = new Button
        {
            Text = "Choose Color",
            Location = new Point(10, yPos),
            Width = 100,
            Parent = this
        };
        _colorButton.Click += OnColorButtonClick;

        _colorLabel = new Label
        {
            Text = "Color: White",
            Location = new Point(120, yPos + 5),
            Width = 150,
            Parent = this
        };

        yPos += 40;

        // Particle count slider (1K to 100K)
        _particleCountLabel = new Label
        {
            Text = "Particles: 10,000",
            Location = new Point(10, yPos),
            Width = 150,
            Parent = this
        };
        yPos += 25;

        _particleCountSlider = new TrackBar
        {
            Minimum = 1000,
            Maximum = 100000,
            Value = 10000,
            TickFrequency = 10000,
            Location = new Point(10, yPos),
            Width = 250,
            Parent = this
        };
        _particleCountSlider.ValueChanged += OnParticleCountChanged;

        yPos += 60;

        // Particle size slider (1px to 10px)
        _particleSizeLabel = new Label
        {
            Text = "Size: 4px",
            Location = new Point(10, yPos),
            Width = 150,
            Parent = this
        };
        yPos += 25;

        _particleSizeSlider = new TrackBar
        {
            Minimum = 1,
            Maximum = 10,
            Value = 4,
            TickFrequency = 1,
            Location = new Point(10, yPos),
            Width = 250,
            Parent = this
        };
        _particleSizeSlider.ValueChanged += OnParticleSizeChanged;

        // ColorDialog setup
        _colorDialog = new ColorDialog
        {
            Color = _currentTextColor,
            FullOpen = true
        };
    }

    /// <summary>
    /// Handles color picker button click.
    /// </summary>
    private void OnColorButtonClick(object? sender, EventArgs e)
    {
        if (_colorDialog.ShowDialog() == DialogResult.OK)
        {
            _currentTextColor = _colorDialog.Color;
            _colorLabel.Text = $"Color: {_currentTextColor.Name}";
            RegenerateParticles();
        }
    }

    /// <summary>
    /// Handles particle count slider changes.
    /// </summary>
    private void OnParticleCountChanged(object? sender, EventArgs e)
    {
        _currentParticleCount = _particleCountSlider.Value;
        _particleCountLabel.Text = $"Particles: {_currentParticleCount:N0}";
        RegenerateParticles();
    }

    /// <summary>
    /// Handles particle size slider changes.
    /// </summary>
    private void OnParticleSizeChanged(object? sender, EventArgs e)
    {
        _currentParticleSize = _particleSizeSlider.Value;
        _particleSizeLabel.Text = $"Size: {_currentParticleSize}px";
        _glHost?.SetParticleSize(_currentParticleSize);
        _glControl?.Invalidate();
    }

    /// <summary>
    /// Handles text input changes.
    /// </summary>
    private void OnTextInputChanged(object? sender, EventArgs e)
    {
        RegenerateParticles();
    }

    /// <summary>
    /// Regenerates particles from current text, color, and count settings.
    /// Uploads new particle data to GPU.
    /// </summary>
    private void RegenerateParticles()
    {
        if (string.IsNullOrEmpty(_textInput.Text) || _textRasterizer == null)
        {
            _glHost?.UpdateParticleBuffer(Array.Empty<ParticleData>());
            return;
        }

        // Convert System.Drawing.Color to SkiaSharp.SKColor
        var skColor = new SKColor(
            _currentTextColor.R,
            _currentTextColor.G,
            _currentTextColor.B,
            _currentTextColor.A);

        // Render text to bitmap
        _currentTextBitmap?.Dispose();
        _currentTextBitmap = _textRasterizer.RenderText(
            _textInput.Text,
            width: 512,
            height: 256,
            textColor: skColor);

        if (_currentTextBitmap == null)
        {
            _glHost?.UpdateParticleBuffer(Array.Empty<ParticleData>());
            return;
        }

        // Generate particles with hole detection
        var particleColor = new Vector4(
            _currentTextColor.R / 255f,
            _currentTextColor.G / 255f,
            _currentTextColor.B / 255f,
            1.0f);

        ParticleData[] particles = _particleGenerator.GenerateParticles(
            _currentTextBitmap,
            _currentParticleCount,
            particleColor);

        // Upload to GPU
        _glHost?.UpdateParticleBuffer(particles);
    }

    /// <summary>
    /// Handles text input changes by re-rasterizing the text.
    /// </summary>
    private void OnTextChanged(object? sender, EventArgs e)
    {
        // Dispose old bitmap
        _currentTextBitmap?.Dispose();
        _currentTextBitmap = null;

        if (!string.IsNullOrEmpty(_textInput.Text))
        {
            // Render text to 512x256 bitmap (sufficient for particle distribution)
            var textColor = new SKColor(255, 255, 255); // White
            _currentTextBitmap = _textRasterizer.RenderText(
                _textInput.Text,
                width: 512,
                height: 256,
                textColor: textColor
            );
        }

        // Invalidate GL control to trigger repaint
        _glControl?.Invalidate();
    }

    /// <summary>
    /// Initializes the OpenTK GLControl with proper OpenGL 4.6 Core profile settings.
    /// The GLControl.Load event is the ONLY safe place to perform GL initialization.
    /// </summary>
    private void InitializeGLControl()
    {
        // Create GLControl with OpenGL 4.6 Core profile
        var settings = new GLControlSettings
        {
            Profile = OpenTK.Windowing.Common.ContextProfile.Core,
            APIVersion = new System.Version(4, 6),
            Flags = OpenTK.Windowing.Common.ContextFlags.Default
        };

        _glControl = new GLControl(settings);
        _glControl.Dock = DockStyle.Fill;

        // Add GLControl to the form
        Controls.Add(_glControl);

        // Initialize GLHost with proper lifecycle management
        _glHost = new GLHost();
        _glHost.Initialize(_glControl);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _glHost?.Dispose();

        // Dispose all resources
        _colorDialog?.Dispose();
        _currentTextBitmap?.Dispose();
        _textRasterizer?.Dispose();

        base.OnFormClosing(e);
    }
}
