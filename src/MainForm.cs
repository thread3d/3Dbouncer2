using OpenTK.GLControl;
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

    public MainForm()
    {
        InitializeComponent();
        InitializeGLControl();
        InitializeTextInput();
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
        _textInput.TextChanged += OnTextChanged;

        // Trigger initial text render
        OnTextChanged(this, EventArgs.Empty);
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

        // Dispose text rasterization resources
        _currentTextBitmap?.Dispose();
        _textRasterizer?.Dispose();

        base.OnFormClosing(e);
    }
}
