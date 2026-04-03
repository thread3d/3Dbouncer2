using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using HelixToolkit.Wpf;
using SkiaSharp;
using OpenTKMath = OpenTK.Mathematics;
using Color = System.Windows.Media.Color;

namespace TextBouncer;

public partial class MainWindow : Window
{
    // Text rasterization
    private TextRasterizer _textRasterizer = new();
    private SKBitmap? _currentTextBitmap;
    private ParticleGenerator _particleGenerator = new();

    // Helix viewport visuals
    private PointsVisual3D? _particlesVisual;

    // Current state
    private Color _currentTextColor = Colors.White;
    private int _currentParticleCount = 10000;
    private ParticleData[]? _particleData;

    // Text bouncing state - the entire text shape bounces as a unit
    private Vector3D _textOffset = new(0, 0, 0);
    private Vector3D _textVelocity = new(0.02, 0.015, 0.01);
    private const double BoxHalf = 0.8; // Text stays within 80% of box to keep particles visible
    private const double TextBounceDamping = 0.995;

    // Text rotation state (pitch, yaw, roll in degrees)
    private double _textPitch = 0;
    private double _textYaw = 0;
    private double _textRoll = 0;
    private double _angularPitch = 0.5;  // degrees per frame
    private double _angularYaw = 0.3;
    private double _angularRoll = 0.2;

    // Render loop
    private DispatcherTimer _renderTimer = null!;
    private System.Diagnostics.Stopwatch _stopwatch = null!;

    public MainWindow()
    {
        InitializeComponent();
        SetupParticlesVisual();
        InitializeBoxWireframe();
        WireEvents();
        SetupRenderLoop();
        RegenerateParticles();
    }

    private void WireEvents()
    {
        TextInputBox.TextChanged += OnTextInputChanged;
        ColorButton.Click += OnColorButtonClick;
        ParticleCountSlider.ValueChanged += OnParticleCountChanged;
        ParticleSizeSlider.ValueChanged += OnParticleSizeChanged;
        BoxOpacitySlider.ValueChanged += OnBoxOpacityChanged;
        AutoRadio.Checked += OnModeChanged;
        ManualRadio.Checked += OnModeChanged;
        MixRadio.Checked += OnModeChanged;
        CameraControlRadio.Checked += OnControlTargetChanged;
        TextControlRadio.Checked += OnControlTargetChanged;
        // Camera controls
        PosXSlider.ValueChanged += OnPositionChanged;
        PosYSlider.ValueChanged += OnPositionChanged;
        PosZSlider.ValueChanged += OnPositionChanged;
        PitchSlider.ValueChanged += OnRotationChanged;
        RollSlider.ValueChanged += OnRotationChanged;
        YawSlider.ValueChanged += OnRotationChanged;
        // Text controls
        TextPosXSlider.ValueChanged += OnTextPositionChanged;
        TextPosYSlider.ValueChanged += OnTextPositionChanged;
        TextPosZSlider.ValueChanged += OnTextPositionChanged;
        TextPitchSlider.ValueChanged += OnTextRotationChanged;
        TextRollSlider.ValueChanged += OnTextRotationChanged;
        TextYawSlider.ValueChanged += OnTextRotationChanged;
    }

    private void SetupParticlesVisual()
    {
        _particlesVisual = new PointsVisual3D();
        Viewport.Children.Add(_particlesVisual);
    }

    private void InitializeBoxWireframe()
    {
        var points = new Point3DCollection
        {
            // Front face
            new Point3D(-1, -1,  1), new Point3D( 1, -1,  1),
            new Point3D( 1, -1,  1), new Point3D( 1,  1,  1),
            new Point3D( 1,  1,  1), new Point3D(-1,  1,  1),
            new Point3D(-1,  1,  1), new Point3D(-1, -1,  1),
            // Back face
            new Point3D(-1, -1, -1), new Point3D( 1, -1, -1),
            new Point3D( 1, -1, -1), new Point3D( 1,  1, -1),
            new Point3D( 1,  1, -1), new Point3D(-1,  1, -1),
            new Point3D(-1,  1, -1), new Point3D(-1, -1, -1),
            // Connecting edges
            new Point3D(-1, -1,  1), new Point3D(-1, -1, -1),
            new Point3D( 1, -1,  1), new Point3D( 1, -1, -1),
            new Point3D( 1,  1,  1), new Point3D( 1,  1, -1),
            new Point3D(-1,  1,  1), new Point3D(-1,  1, -1),
        };
        BoxWireframe.Points = points;
    }

    private void SetupRenderLoop()
    {
        _stopwatch = System.Diagnostics.Stopwatch.StartNew();

        _renderTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _renderTimer.Tick += OnRenderTick;
        _renderTimer.Start();
    }

    private void OnRenderTick(object? sender, EventArgs e)
    {
        UpdateTextBounce();
        UpdateParticleVisuals();
    }

    private void UpdateTextBounce()
    {
        // Update text position based on velocity
        _textOffset.X += _textVelocity.X;
        _textOffset.Y += _textVelocity.Y;
        _textOffset.Z += _textVelocity.Z;

        // Bounce off box boundaries with damping
        if (_textOffset.X > BoxHalf || _textOffset.X < -BoxHalf)
        {
            _textVelocity.X *= -TextBounceDamping;
            _angularPitch *= -1; // Flip pitch direction on X bounce
            _angularRoll *= -1;  // Flip roll direction on X bounce
            _textOffset.X = Math.Clamp(_textOffset.X, -BoxHalf, BoxHalf);
        }
        if (_textOffset.Y > BoxHalf || _textOffset.Y < -BoxHalf)
        {
            _textVelocity.Y *= -TextBounceDamping;
            _angularYaw *= -1;   // Flip yaw direction on Y bounce
            _angularRoll *= -1;    // Flip roll direction on Y bounce
            _textOffset.Y = Math.Clamp(_textOffset.Y, -BoxHalf, BoxHalf);
        }
        if (_textOffset.Z > BoxHalf || _textOffset.Z < -BoxHalf)
        {
            _textVelocity.Z *= -TextBounceDamping;
            _angularPitch *= -1;  // Flip pitch direction on Z bounce
            _angularYaw *= -1;    // Flip yaw direction on Z bounce
            _textOffset.Z = Math.Clamp(_textOffset.Z, -BoxHalf, BoxHalf);
        }

        // Update rotation angles
        _textPitch += _angularPitch;
        _textYaw += _angularYaw;
        _textRoll += _angularRoll;

        // Keep angles in reasonable range
        if (_textPitch > 360) _textPitch -= 360;
        if (_textPitch < 0) _textPitch += 360;
        if (_textYaw > 360) _textYaw -= 360;
        if (_textYaw < 0) _textYaw += 360;
        if (_textRoll > 360) _textRoll -= 360;
        if (_textRoll < 0) _textRoll += 360;
    }

    private void RegenerateParticles()
    {
        if (string.IsNullOrEmpty(TextInputBox.Text) || _textRasterizer == null)
        {
            _particleData = null;
            UpdateParticleVisuals();
            return;
        }

        var skColor = new SKColor(
            _currentTextColor.R,
            _currentTextColor.G,
            _currentTextColor.B,
            _currentTextColor.A);

        _currentTextBitmap?.Dispose();
        _currentTextBitmap = _textRasterizer.RenderText(
            TextInputBox.Text,
            width: 512,
            height: 256,
            textColor: skColor);

        if (_currentTextBitmap == null)
        {
            _particleData = null;
            UpdateParticleVisuals();
            return;
        }

        var particleColor = new OpenTKMath.Vector4(
            _currentTextColor.R / 255f,
            _currentTextColor.G / 255f,
            _currentTextColor.B / 255f,
            1.0f);

        ParticleData[] particles = _particleGenerator.GenerateParticles(
            _currentTextBitmap,
            _currentParticleCount,
            particleColor);

        _particleData = particles;

        // Reset text position and give it a fresh random velocity
        _textOffset = new Vector3D(0, 0, 0);
        _textVelocity = new Vector3D(
            (Random.Shared.NextDouble() - 0.5) * 0.04,
            (Random.Shared.NextDouble() - 0.5) * 0.03,
            (Random.Shared.NextDouble() - 0.5) * 0.02);

        // Reset rotation
        _textPitch = 0;
        _textYaw = 0;
        _textRoll = 0;
        _angularPitch = (Random.Shared.NextDouble() - 0.5) * 1.0;
        _angularYaw = (Random.Shared.NextDouble() - 0.5) * 0.6;
        _angularRoll = (Random.Shared.NextDouble() - 0.5) * 0.4;

        UpdateParticleVisuals();
    }

    private void UpdateParticleVisuals()
    {
        if (_particlesVisual == null || _particleData == null)
            return;

        // Precompute rotation matrix for efficiency
        double pitchRad = _textPitch * Math.PI / 180.0;
        double yawRad = _textYaw * Math.PI / 180.0;
        double rollRad = _textRoll * Math.PI / 180.0;

        double cosPitch = Math.Cos(pitchRad), sinPitch = Math.Sin(pitchRad);
        double cosYaw = Math.Cos(yawRad), sinYaw = Math.Sin(yawRad);
        double cosRoll = Math.Cos(rollRad), sinRoll = Math.Sin(rollRad);

        var points = new Point3DCollection();

        for (int i = 0; i < _particleData.Length; i++)
        {
            var p = _particleData[i];
            // Translate to origin (relative to text center)
            double px = p.Position.X;
            double py = p.Position.Y;
            double pz = p.Position.Z;

            // Apply Yaw (Y-axis rotation) - spinning left/right
            double x1 = cosYaw * px - sinYaw * pz;
            double y1 = py;
            double z1 = sinYaw * px + cosYaw * pz;

            // Apply Pitch (X-axis rotation) - nodding up/down
            double x2 = x1;
            double y2 = cosPitch * y1 - sinPitch * z1;
            double z2 = sinPitch * y1 + cosPitch * z1;

            // Apply Roll (Z-axis rotation) - tilting side to side
            double x3 = cosRoll * x2 - sinRoll * y2;
            double y3 = sinRoll * x2 + cosRoll * y2;
            double z3 = z2;

            // Translate back to world position (add text offset)
            points.Add(new Point3D(
                x3 + _textOffset.X,
                y3 + _textOffset.Y,
                z3 + _textOffset.Z));
        }

        _particlesVisual.Points = points;
        _particlesVisual.Color = _currentTextColor;
    }

    private void SetParticleColor()
    {
        UpdateParticleVisuals();
    }

    // --- UI Event Handlers ---

    private void OnColorButtonClick(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.ColorDialog
        {
            FullOpen = true
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _currentTextColor = Color.FromArgb(
                dialog.Color.A,
                dialog.Color.R,
                dialog.Color.G,
                dialog.Color.B);
            ColorLabel.Content = $"Color: {dialog.Color.Name}";
            ColorButton.Background = new SolidColorBrush(_currentTextColor);
            double brightness = (dialog.Color.R * 0.299 + dialog.Color.G * 0.587 + dialog.Color.B * 0.114) / 255;
            ColorButton.Foreground = new SolidColorBrush(brightness > 0.5 ? Colors.Black : Colors.White);
            SetParticleColor();
        }
    }

    private void OnTextInputChanged(object sender, TextChangedEventArgs e)
    {
        RegenerateParticles();
    }

    private void OnParticleCountChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ParticleCountLabel == null) return;
        _currentParticleCount = (int)ParticleCountSlider.Value;
        ParticleCountLabel.Content = $"Particles: {_currentParticleCount:N0}";
        RegenerateParticles();
    }

    private void OnParticleSizeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ParticleSizeLabel == null) return;
        ParticleSizeLabel.Content = $"Size: {ParticleSizeSlider.Value:F0}px";
    }

    private void OnBoxOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (BoxOpacityLabel == null || BoxWireframe == null) return;
        float opacity = (float)(BoxOpacitySlider.Value / 100.0);
        BoxOpacityLabel.Content = $"Box Opacity: {BoxOpacitySlider.Value}%";
        BoxWireframe.Color = Color.FromArgb(
            (byte)(opacity * 255),
            255, 255, 255);
    }

    private void OnModeChanged(object sender, RoutedEventArgs e)
    {
        // Mode changes don't affect text bouncing in this implementation
    }

    private void OnControlTargetChanged(object sender, RoutedEventArgs e)
    {
        if (CameraControlRadio == null || TextControlRadio == null) return;
        if (CameraControlsPanel == null || TextControlsPanel == null) return;

        if (CameraControlRadio.IsChecked == true)
        {
            CameraControlsPanel.Visibility = Visibility.Visible;
            TextControlsPanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            CameraControlsPanel.Visibility = Visibility.Collapsed;
            TextControlsPanel.Visibility = Visibility.Visible;
        }
    }

    private void OnTextPositionChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TextPosXLabel == null) return;
        double x = TextPosXSlider.Value / 100.0;
        double y = TextPosYSlider.Value / 100.0;
        double z = TextPosZSlider.Value / 100.0;

        TextPosXLabel.Content = $"Text Pos X: {x:F2}";
        TextPosYLabel.Content = $"Text Pos Y: {y:F2}";
        TextPosZLabel.Content = $"Text Pos Z: {z:F2}";

        _textOffset = new Vector3D(x, y, z);
    }

    private void OnTextRotationChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TextPitchLabel == null) return;
        double pitch = TextPitchSlider.Value;
        double roll = TextRollSlider.Value;
        double yaw = TextYawSlider.Value;

        TextPitchLabel.Content = $"Text Pitch: {pitch:F0}°";
        TextRollLabel.Content = $"Text Roll: {roll:F0}°";
        TextYawLabel.Content = $"Text Yaw: {yaw:F0}°";

        _textPitch = pitch;
        _textRoll = roll;
        _textYaw = yaw;
    }

    private void OnPositionChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (PosXLabel == null) return;
        float x = (float)(PosXSlider.Value / 100.0);
        float y = (float)(PosYSlider.Value / 100.0);
        float z = (float)(PosZSlider.Value / 100.0);

        PosXLabel.Content = $"Cam Pos X: {x:F2}";
        PosYLabel.Content = $"Cam Pos Y: {y:F2}";
        PosZLabel.Content = $"Cam Pos Z: {z:F2}";

        if (Viewport.Camera is PerspectiveCamera cam)
        {
            cam.Position = new Point3D(x, y, z);
        }
    }

    private void OnRotationChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (PitchLabel == null) return;
        float pitch = (float)PitchSlider.Value;
        float roll = (float)RollSlider.Value;
        float yaw = (float)YawSlider.Value;

        PitchLabel.Content = $"Cam Pitch: {pitch}°";
        RollLabel.Content = $"Cam Roll: {roll}°";
        YawLabel.Content = $"Cam Yaw: {yaw}°";

        if (Viewport.Camera is PerspectiveCamera cam)
        {
            double pitchRad = pitch * Math.PI / 180.0;
            double yawRad = yaw * Math.PI / 180.0;
            double rollRad = roll * Math.PI / 180.0;

            var lookDir = new Vector3D(0, 0, -1);
            var upDir = new Vector3D(0, 1, 0);

            var rotated = RotateVector(lookDir, upDir, yawRad);
            rotated = RotateVector(rotated, new Vector3D(1, 0, 0), pitchRad);

            cam.LookDirection = rotated;
            cam.UpDirection = RotateVector(upDir, new Vector3D(1, 0, 0), rollRad);
        }
    }

    private static Vector3D RotateVector(Vector3D v, Vector3D axis, double angle)
    {
        double cos = Math.Cos(angle);
        double sin = Math.Sin(angle);
        double t = 1.0 - cos;

        double x = axis.X, y = axis.Y, z = axis.Z;

        double resultX = (t * x * x + cos) * v.X
            + (t * x * y - sin * z) * v.Y
            + (t * x * z + sin * y) * v.Z;
        double resultY = (t * x * y + sin * z) * v.X
            + (t * y * y + cos) * v.Y
            + (t * y * z - sin * x) * v.Z;
        double resultZ = (t * x * z - sin * y) * v.X
            + (t * y * z + sin * x) * v.Y
            + (t * z * z + cos) * v.Z;

        return new Vector3D(resultX, resultY, resultZ);
    }

    protected override void OnClosed(EventArgs e)
    {
        _renderTimer?.Stop();
        _currentTextBitmap?.Dispose();
        _textRasterizer?.Dispose();
        base.OnClosed(e);
    }
}
