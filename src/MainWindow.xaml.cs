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

    // Original box wireframe points (12 edges = 24 points)
    private Point3D[] _originalBoxPoints = null!;
    private Point3DCollection _currentBoxPoints = null!;

    // Current state
    private Color _currentTextColor = Colors.White;
    private int _currentParticleCount = 10000;
    private ParticleData[]? _particleData;

    // Scene transform - applies to entire scene (box + text)
    private Vector3D _sceneOffset = new(0, 0, 0);
    private Vector3D _sceneVelocity = new(0.02, 0.015, 0.01);
    private const double SceneBoundary = 0.8;
    private const double SceneBounceDamping = 0.995;

    // Scene rotation state (pitch, yaw, roll in degrees)
    private double _scenePitch = 0;
    private double _sceneYaw = 0;
    private double _sceneRoll = 0;
    private double _angularPitch = 0.5;
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
        // Store original box points for scene transform
        _originalBoxPoints = new Point3D[]
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
        _currentBoxPoints = new Point3DCollection(_originalBoxPoints);
        BoxWireframe.Points = _currentBoxPoints;
    }

    private Point3D ApplySceneRotation(Point3D p)
    {
        double pitchRad = _scenePitch * Math.PI / 180.0;
        double yawRad = _sceneYaw * Math.PI / 180.0;
        double rollRad = _sceneRoll * Math.PI / 180.0;

        double cosPitch = Math.Cos(pitchRad), sinPitch = Math.Sin(pitchRad);
        double cosYaw = Math.Cos(yawRad), sinYaw = Math.Sin(yawRad);
        double cosRoll = Math.Cos(rollRad), sinRoll = Math.Sin(rollRad);

        // Yaw (Y-axis rotation)
        double x1 = cosYaw * p.X - sinYaw * p.Z;
        double y1 = p.Y;
        double z1 = sinYaw * p.X + cosYaw * p.Z;

        // Pitch (X-axis rotation)
        double x2 = x1;
        double y2 = cosPitch * y1 - sinPitch * z1;
        double z2 = sinPitch * y1 + cosPitch * z1;

        // Roll (Z-axis rotation)
        double x3 = cosRoll * x2 - sinRoll * y2;
        double y3 = sinRoll * x2 + cosRoll * y2;
        double z3 = z2;

        return new Point3D(x3, y3, z3);
    }

    private void UpdateSceneVisuals()
    {
        // Update box wireframe with scene transform
        for (int i = 0; i < _originalBoxPoints.Length; i++)
        {
            var rotated = ApplySceneRotation(_originalBoxPoints[i]);
            _currentBoxPoints[i] = new Point3D(
                rotated.X + _sceneOffset.X,
                rotated.Y + _sceneOffset.Y,
                rotated.Z + _sceneOffset.Z);
        }
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
        UpdateSceneBounce();
        UpdateSceneVisuals();
        UpdateParticleVisuals();
    }

    private void UpdateSceneBounce()
    {
        // Update scene position based on velocity
        _sceneOffset.X += _sceneVelocity.X;
        _sceneOffset.Y += _sceneVelocity.Y;
        _sceneOffset.Z += _sceneVelocity.Z;

        // Bounce off scene boundaries with damping
        if (_sceneOffset.X > SceneBoundary || _sceneOffset.X < -SceneBoundary)
        {
            _sceneVelocity.X *= -SceneBounceDamping;
            _angularPitch *= -1;
            _angularRoll *= -1;
            _sceneOffset.X = Math.Clamp(_sceneOffset.X, -SceneBoundary, SceneBoundary);
        }
        if (_sceneOffset.Y > SceneBoundary || _sceneOffset.Y < -SceneBoundary)
        {
            _sceneVelocity.Y *= -SceneBounceDamping;
            _angularYaw *= -1;
            _angularRoll *= -1;
            _sceneOffset.Y = Math.Clamp(_sceneOffset.Y, -SceneBoundary, SceneBoundary);
        }
        if (_sceneOffset.Z > SceneBoundary || _sceneOffset.Z < -SceneBoundary)
        {
            _sceneVelocity.Z *= -SceneBounceDamping;
            _angularPitch *= -1;
            _angularYaw *= -1;
            _sceneOffset.Z = Math.Clamp(_sceneOffset.Z, -SceneBoundary, SceneBoundary);
        }

        // Update rotation angles
        _scenePitch += _angularPitch;
        _sceneYaw += _angularYaw;
        _sceneRoll += _angularRoll;

        // Keep angles in reasonable range
        if (_scenePitch > 360) _scenePitch -= 360;
        if (_scenePitch < 0) _scenePitch += 360;
        if (_sceneYaw > 360) _sceneYaw -= 360;
        if (_sceneYaw < 0) _sceneYaw += 360;
        if (_sceneRoll > 360) _sceneRoll -= 360;
        if (_sceneRoll < 0) _sceneRoll += 360;
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

        // Reset scene to defaults
        _sceneOffset = new Vector3D(0, 0, 0);
        _sceneVelocity = new Vector3D(0.02, 0.015, 0.01);
        _scenePitch = 0;
        _sceneYaw = 0;
        _sceneRoll = 0;
        _angularPitch = 0.5;
        _angularYaw = 0.3;
        _angularRoll = 0.2;

        UpdateSceneVisuals();
        UpdateParticleVisuals();
    }

    private void UpdateParticleVisuals()
    {
        if (_particlesVisual == null || _particleData == null)
            return;

        // Precompute rotation matrix for scene transform
        double pitchRad = _scenePitch * Math.PI / 180.0;
        double yawRad = _sceneYaw * Math.PI / 180.0;
        double rollRad = _sceneRoll * Math.PI / 180.0;

        double cosPitch = Math.Cos(pitchRad), sinPitch = Math.Sin(pitchRad);
        double cosYaw = Math.Cos(yawRad), sinYaw = Math.Sin(yawRad);
        double cosRoll = Math.Cos(rollRad), sinRoll = Math.Sin(rollRad);

        var points = new Point3DCollection();

        for (int i = 0; i < _particleData.Length; i++)
        {
            var p = _particleData[i];
            // Apply scene rotation (same as box)
            // Yaw (Y-axis rotation)
            double x1 = cosYaw * p.Position.X - sinYaw * p.Position.Z;
            double y1 = p.Position.Y;
            double z1 = sinYaw * p.Position.X + cosYaw * p.Position.Z;

            // Pitch (X-axis rotation)
            double x2 = x1;
            double y2 = cosPitch * y1 - sinPitch * z1;
            double z2 = sinPitch * y1 + cosPitch * z1;

            // Roll (Z-axis rotation)
            double x3 = cosRoll * x2 - sinRoll * y2;
            double y3 = sinRoll * x2 + cosRoll * y2;
            double z3 = z2;

            // Apply scene offset
            points.Add(new Point3D(
                x3 + _sceneOffset.X,
                y3 + _sceneOffset.Y,
                z3 + _sceneOffset.Z));
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

        _sceneOffset = new Vector3D(x, y, z);
        UpdateSceneVisuals();
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

        _scenePitch = pitch;
        _sceneRoll = roll;
        _sceneYaw = yaw;
        UpdateSceneVisuals();
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
