using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using HelixToolkit.Wpf;
using SkiaSharp;
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
    private LinesVisual3D? _boxWireframe;
    private ModelVisual3D? _boxFace; // Changed to ModelVisual3D for mesh geometry

    // Original particle local positions
    private Vector3D[]? _particleLocalPositions;

    // Current state
    private Color _currentTextColor = Colors.White;
    private int _currentParticleCount = 10000;
    private ParticleData[]? _particleData;

    // Polyhedron selection
    private int _currentPolyhedronIndex = 1;
    private PolyhedronData? _currentPolyhedronData;

    // Box transform
    private Vector3D _boxOffset = new(0, 0, 0);
    private Vector3D _boxVelocity = new(0.015, 0.01, 0.008);
    private double _boxPitch = 0, _boxYaw = 0, _boxRoll = 0;
    private double _angularPitch = 0.3, _angularYaw = 0.2, _angularRoll = 0.15;
    private const double BoxBoundary = 0.7;
    private const double BounceDamping = 0.98;

    // Text relative to box
    private Vector3D _textOffset = new(0, 0, 0);
    private Vector3D _textVelocity = new(0.008, 0.006, 0.004);
    private double _textPitch = 0, _textYaw = 0, _textRoll = 0;
    private double _textAngularPitch = 0.2, _textAngularYaw = 0.15, _textAngularRoll = 0.1;
    private const double TextBoundary = 0.5;

    // Box face opacity
    private float _boxOpacity = 0.4f;

    // Transparency mode: 0 = face only, 1 = whole polyhedron, 2 = wireframe only
    private int _transparencyMode = 2;

    // Render loop
    private DispatcherTimer _renderTimer = null!;
    private long _lastFrameTime;
    private System.Diagnostics.Stopwatch _stopwatch = null!;

    public MainWindow()
    {
        InitializeComponent();
        SetupVisuals();
        WireEvents();
        SetupRenderLoop();
        RegenerateParticles();
    }

    private void SetupVisuals()
    {
        _particlesVisual = new PointsVisual3D();
        Viewport.Children.Add(_particlesVisual);

        _boxWireframe = new LinesVisual3D
        {
            Color = Colors.White,
            Thickness = 1
        };
        Viewport.Children.Add(_boxWireframe);

        _boxFace = new ModelVisual3D();
        Viewport.Children.Add(_boxFace);

        InitializePolyhedronGeometry();
    }

    private void InitializePolyhedronGeometry()
    {
        _currentPolyhedronData = PolyhedronLibrary.GetPolyhedron(_currentPolyhedronIndex);

        // Wireframe from current polyhedron edges
        if (_boxWireframe != null && _currentPolyhedronData != null)
        {
            var wireframePoints = new Point3DCollection();
            foreach (var edge in _currentPolyhedronData.Edges)
            {
                var v1 = _currentPolyhedronData.Vertices[edge[0]];
                var v2 = _currentPolyhedronData.Vertices[edge[1]];
                wireframePoints.Add(new Point3D(v1.X, v1.Y, v1.Z));
                wireframePoints.Add(new Point3D(v2.X, v2.Y, v2.Z));
            }
            _boxWireframe.Points = wireframePoints;
        }
    }

    private void WireEvents()
    {
        TextInputBox.TextChanged += OnTextInputChanged;
        ColorButton.Click += OnColorButtonClick;
        ParticleCountSlider.ValueChanged += OnParticleCountChanged;
        BoxOpacitySlider.ValueChanged += OnBoxOpacityChanged;
        PolyhedronSlider.ValueChanged += OnPolyhedronChanged;
        CameraControlRadio.Checked += OnControlTargetChanged;
        TextControlRadio.Checked += OnControlTargetChanged;
        FaceTransparencyRadio.Checked += OnTransparencyModeChanged;
        WholeTransparencyRadio.Checked += OnTransparencyModeChanged;
        WireframeOnlyRadio.Checked += OnTransparencyModeChanged;
        // Camera controls
        PosXSlider.ValueChanged += OnPositionChanged;
        PosYSlider.ValueChanged += OnPositionChanged;
        PosZSlider.ValueChanged += OnPositionChanged;
        PitchSlider.ValueChanged += OnRotationChanged;
        RollSlider.ValueChanged += OnRotationChanged;
        YawSlider.ValueChanged += OnRotationChanged;
        // Scene controls
        TextPosXSlider.ValueChanged += OnScenePositionChanged;
        TextPosYSlider.ValueChanged += OnScenePositionChanged;
        TextPosZSlider.ValueChanged += OnScenePositionChanged;
        TextPitchSlider.ValueChanged += OnSceneRotationChanged;
        TextRollSlider.ValueChanged += OnSceneRotationChanged;
        TextYawSlider.ValueChanged += OnSceneRotationChanged;
    }

    private void SetupRenderLoop()
    {
        _stopwatch = System.Diagnostics.Stopwatch.StartNew();
        _lastFrameTime = _stopwatch.ElapsedTicks;

        _renderTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _renderTimer.Tick += OnRenderTick;
        _renderTimer.Start();
    }

    private void OnRenderTick(object? sender, EventArgs e)
    {
        long currentTime = _stopwatch.ElapsedTicks;
        float deltaTime = (float)(currentTime - _lastFrameTime) / System.Diagnostics.Stopwatch.Frequency;
        _lastFrameTime = currentTime;
        deltaTime = Math.Min(deltaTime, 0.05f);

        UpdateBouncing(deltaTime);
        UpdateVisuals();
    }

    private void UpdateBouncing(float dt)
    {
        // Update box position
        _boxOffset.X += _boxVelocity.X * dt * 60;
        _boxOffset.Y += _boxVelocity.Y * dt * 60;
        _boxOffset.Z += _boxVelocity.Z * dt * 60;

        // Box bounces off scene boundary
        if (_boxOffset.X > BoxBoundary || _boxOffset.X < -BoxBoundary)
        {
            _boxVelocity.X *= -BounceDamping;
            _angularPitch = RandomAngular();
            _angularRoll = RandomAngular();
            _boxOffset.X = Math.Clamp(_boxOffset.X, -BoxBoundary, BoxBoundary);
        }
        if (_boxOffset.Y > BoxBoundary || _boxOffset.Y < -BoxBoundary)
        {
            _boxVelocity.Y *= -BounceDamping;
            _angularYaw = RandomAngular();
            _angularRoll = RandomAngular();
            _boxOffset.Y = Math.Clamp(_boxOffset.Y, -BoxBoundary, BoxBoundary);
        }
        if (_boxOffset.Z > BoxBoundary || _boxOffset.Z < -BoxBoundary)
        {
            _boxVelocity.Z *= -BounceDamping;
            _angularPitch = RandomAngular();
            _angularYaw = RandomAngular();
            _boxOffset.Z = Math.Clamp(_boxOffset.Z, -BoxBoundary, BoxBoundary);
        }

        // Update box rotation
        _boxPitch += _angularPitch;
        _boxYaw += _angularYaw;
        _boxRoll += _angularRoll;

        // Update text position relative to box
        _textOffset.X += _textVelocity.X * dt * 60;
        _textOffset.Y += _textVelocity.Y * dt * 60;
        _textOffset.Z += _textVelocity.Z * dt * 60;

        // Text bounces inside box
        if (_textOffset.X > TextBoundary || _textOffset.X < -TextBoundary)
        {
            _textVelocity.X *= -BounceDamping;
            _textAngularPitch = RandomAngular();
            _textAngularRoll = RandomAngular();
            _textOffset.X = Math.Clamp(_textOffset.X, -TextBoundary, TextBoundary);
        }
        if (_textOffset.Y > TextBoundary || _textOffset.Y < -TextBoundary)
        {
            _textVelocity.Y *= -BounceDamping;
            _textAngularYaw = RandomAngular();
            _textAngularRoll = RandomAngular();
            _textOffset.Y = Math.Clamp(_textOffset.Y, -TextBoundary, TextBoundary);
        }
        if (_textOffset.Z > TextBoundary || _textOffset.Z < -TextBoundary)
        {
            _textVelocity.Z *= -BounceDamping;
            _textAngularPitch = RandomAngular();
            _textAngularYaw = RandomAngular();
            _textOffset.Z = Math.Clamp(_textOffset.Z, -TextBoundary, TextBoundary);
        }

        // Update text rotation
        _textPitch += _textAngularPitch;
        _textYaw += _textAngularYaw;
        _textRoll += _textAngularRoll;
    }

    private static double RandomAngular()
    {
        return (Random.Shared.NextDouble() - 0.5) * 0.8;
    }

    private Point3D ApplyBoxTransform(Point3D p)
    {
        double pitchRad = _boxPitch * Math.PI / 180.0;
        double yawRad = _boxYaw * Math.PI / 180.0;
        double rollRad = _boxRoll * Math.PI / 180.0;

        double cosPitch = Math.Cos(pitchRad), sinPitch = Math.Sin(pitchRad);
        double cosYaw = Math.Cos(yawRad), sinYaw = Math.Sin(yawRad);
        double cosRoll = Math.Cos(rollRad), sinRoll = Math.Sin(rollRad);

        // Yaw (Y-axis)
        double x1 = cosYaw * p.X - sinYaw * p.Z;
        double y1 = p.Y;
        double z1 = sinYaw * p.X + cosYaw * p.Z;

        // Pitch (X-axis)
        double x2 = x1;
        double y2 = cosPitch * y1 - sinPitch * z1;
        double z2 = sinPitch * y1 + cosPitch * z1;

        // Roll (Z-axis)
        double x3 = cosRoll * x2 - sinRoll * y2;
        double y3 = sinRoll * x2 + cosRoll * y2;
        double z3 = z2;

        return new Point3D(x3 + _boxOffset.X, y3 + _boxOffset.Y, z3 + _boxOffset.Z);
    }

    private Point3D ApplyTextTransform(Vector3D local)
    {
        // Apply box transform first
        double pitchRad = _boxPitch * Math.PI / 180.0;
        double yawRad = _boxYaw * Math.PI / 180.0;
        double rollRad = _boxRoll * Math.PI / 180.0;

        double cosPitch = Math.Cos(pitchRad), sinPitch = Math.Sin(pitchRad);
        double cosYaw = Math.Cos(yawRad), sinYaw = Math.Sin(yawRad);
        double cosRoll = Math.Cos(rollRad), sinRoll = Math.Sin(rollRad);

        // Box rotation
        double x1 = cosYaw * local.X - sinYaw * local.Z;
        double y1 = local.Y;
        double z1 = sinYaw * local.X + cosYaw * local.Z;

        double x2 = x1;
        double y2 = cosPitch * y1 - sinPitch * z1;
        double z2 = sinPitch * y1 + cosPitch * z1;

        double x3 = cosRoll * x2 - sinRoll * y2;
        double y3 = sinRoll * x2 + cosRoll * y2;
        double z3 = z2;

        // Add text offset
        x3 += _textOffset.X;
        y3 += _textOffset.Y;
        z3 += _textOffset.Z;

        // Apply text rotation (relative to its own center)
        pitchRad = _textPitch * Math.PI / 180.0;
        yawRad = _textYaw * Math.PI / 180.0;
        rollRad = _textRoll * Math.PI / 180.0;

        cosPitch = Math.Cos(pitchRad); sinPitch = Math.Sin(pitchRad);
        cosYaw = Math.Cos(yawRad); sinYaw = Math.Sin(yawRad);
        cosRoll = Math.Cos(rollRad); sinRoll = Math.Sin(rollRad);

        // Text rotation around Y
        double x4 = cosYaw * x3 - sinYaw * z3;
        double y4 = y3;
        double z4 = sinYaw * x3 + cosYaw * z3;

        // Text rotation around X
        double x5 = x4;
        double y5 = cosPitch * y4 - sinPitch * z4;
        double z5 = sinPitch * y4 + cosPitch * z4;

        // Text rotation around Z
        double x6 = cosRoll * x5 - sinRoll * y5;
        double y6 = sinRoll * x5 + cosRoll * y5;
        double z6 = z5;

        // Add box offset (already added above)

        return new Point3D(x6, y6, z6);
    }

    private void UpdateVisuals()
    {
        if (_particlesVisual == null || _particleLocalPositions == null) return;

        // Update box wireframe using current polyhedron
        if (_boxWireframe != null && _currentPolyhedronData != null)
        {
            var wireframePoints = new Point3DCollection();
            foreach (var edge in _currentPolyhedronData.Edges)
            {
                if (edge.Length < 2) continue;
                int i0 = Math.Clamp(edge[0], 0, _currentPolyhedronData.Vertices.Length - 1);
                int i1 = Math.Clamp(edge[1], 0, _currentPolyhedronData.Vertices.Length - 1);
                var v1 = _currentPolyhedronData.Vertices[i0];
                var v2 = _currentPolyhedronData.Vertices[i1];
                wireframePoints.Add(ApplyBoxTransform(new Point3D(v1.X, v1.Y, v1.Z)));
                wireframePoints.Add(ApplyBoxTransform(new Point3D(v2.X, v2.Y, v2.Z)));
            }
            _boxWireframe.Points = wireframePoints;
        }

        // Update translucent face(s) mesh
        if (_boxFace != null && _currentPolyhedronData != null && _currentPolyhedronData.Vertices.Length >= 3)
        {
            var faces = _currentPolyhedronData.Faces;
            if (faces != null && faces.Length > 0)
            {
                byte alpha = (byte)(_boxOpacity * 255);
                var material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(alpha, 255, 255, 255)));

                // Determine which faces to render
                int[][] facesToRender;
                if (_transparencyMode == 0)
                    facesToRender = new int[][] { faces[0] };  // Single face only
                else if (_transparencyMode == 1)
                    facesToRender = faces;                     // All faces
                else
                    facesToRender = Array.Empty<int[]>();     // Wireframe only - no faces

                // Create a Model3DGroup to hold all face models
                var modelGroup = new Model3DGroup();

                foreach (var faceIndices in facesToRender)
                {
                    if (faceIndices.Length < 3) continue;

                    // Reorder vertices by angle around centroid so they form a proper polygon
                    var centroid = new Point3D(0, 0, 0);
                    foreach (int idx in faceIndices)
                    {
                        var v = _currentPolyhedronData.Vertices[idx];
                        centroid.X += v.X;
                        centroid.Y += v.Y;
                        centroid.Z += v.Z;
                    }
                    centroid.X /= faceIndices.Length;
                    centroid.Y /= faceIndices.Length;
                    centroid.Z /= faceIndices.Length;

                    // Sort by angle in XY plane (looking down Z axis)
                    var sorted = faceIndices
                        .Select(idx => new {
                            idx,
                            angle = Math.Atan2(
                                _currentPolyhedronData.Vertices[idx].Y - centroid.Y,
                                _currentPolyhedronData.Vertices[idx].X - centroid.X)
                        })
                        .OrderBy(x => x.angle)
                        .Select(x => x.idx)
                        .ToArray();

                    int n = sorted.Length;

                    // Create mesh with triangle fan from centroid
                    var mesh = new MeshGeometry3D();

                    for (int i = 0; i < n; i++)
                    {
                        int i1 = sorted[i];
                        int i2 = sorted[(i + 1) % n];
                        var v0 = ApplyBoxTransform(centroid);
                        var v1 = ApplyBoxTransform(_currentPolyhedronData.Vertices[i1]);
                        var v2 = ApplyBoxTransform(_currentPolyhedronData.Vertices[i2]);

                        int baseIndex = mesh.Positions.Count;
                        mesh.Positions.Add(v0);
                        mesh.Positions.Add(v1);
                        mesh.Positions.Add(v2);
                        mesh.TriangleIndices.Add(baseIndex);
                        mesh.TriangleIndices.Add(baseIndex + 1);
                        mesh.TriangleIndices.Add(baseIndex + 2);
                    }

                    var geometryModel = new GeometryModel3D(mesh, material);
                    geometryModel.BackMaterial = material; // Double-sided
                    modelGroup.Children.Add(geometryModel);
                }

                _boxFace.Content = modelGroup;
            }
        }

        // Update particles
        var points = new Point3DCollection();
        for (int i = 0; i < _particleLocalPositions.Length; i++)
        {
            var local = _particleLocalPositions[i];
            points.Add(ApplyTextTransform(local));
        }

        _particlesVisual.Points = points;
        _particlesVisual.Color = _currentTextColor;
    }

    private void RegenerateParticles()
    {
        if (string.IsNullOrEmpty(TextInputBox.Text) || _textRasterizer == null)
        {
            _particleData = null;
            _particleLocalPositions = null;
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
            textColor: skColor);

        if (_currentTextBitmap == null)
        {
            _particleData = null;
            _particleLocalPositions = null;
            return;
        }

        var particleColor = new OpenTK.Mathematics.Vector4(
            _currentTextColor.R / 255f,
            _currentTextColor.G / 255f,
            _currentTextColor.B / 255f,
            1.0f);

        ParticleData[] particles = _particleGenerator.GenerateParticles(
            _currentTextBitmap,
            _currentParticleCount,
            particleColor);

        _particleData = particles;

        _particleLocalPositions = new Vector3D[particles.Length];
        for (int i = 0; i < particles.Length; i++)
        {
            _particleLocalPositions[i] = new Vector3D(
                particles[i].Position.X,
                particles[i].Position.Y,
                particles[i].Position.Z);
        }

        // Reset to defaults
        _boxOffset = new Vector3D(0, 0, 0);
        _boxVelocity = new Vector3D(0.015, 0.01, 0.008);
        _boxPitch = _boxYaw = _boxRoll = 0;
        _angularPitch = 0.3; _angularYaw = 0.2; _angularRoll = 0.15;

        _textOffset = new Vector3D(0, 0, 0);
        _textVelocity = new Vector3D(0.008, 0.006, 0.004);
        _textPitch = _textYaw = _textRoll = 0;
        _textAngularPitch = 0.2; _textAngularYaw = 0.15; _textAngularRoll = 0.1;

        UpdateVisuals();
    }

    // --- UI Event Handlers ---

    private void OnColorButtonClick(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.ColorDialog { FullOpen = true };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _currentTextColor = Color.FromArgb(
                dialog.Color.A, dialog.Color.R, dialog.Color.G, dialog.Color.B);
            ColorLabel.Content = $"Color: {dialog.Color.Name}";
            ColorButton.Background = new SolidColorBrush(_currentTextColor);
            double brightness = (dialog.Color.R * 0.299 + dialog.Color.G * 0.587 + dialog.Color.B * 0.114) / 255;
            ColorButton.Foreground = new SolidColorBrush(brightness > 0.5 ? Colors.Black : Colors.White);
            UpdateVisuals();
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

    private void OnBoxOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (BoxOpacityLabel == null) return;
        _boxOpacity = (float)(BoxOpacitySlider.Value / 100.0);
        BoxOpacityLabel.Content = $"Box Face Opacity: {BoxOpacitySlider.Value}%";
    }

    private void OnTransparencyModeChanged(object sender, RoutedEventArgs e)
    {
        if (FaceTransparencyRadio == null) return;
        if (FaceTransparencyRadio.IsChecked == true)
            _transparencyMode = 0;
        else if (WholeTransparencyRadio.IsChecked == true)
            _transparencyMode = 1;
        else
            _transparencyMode = 2;
    }

    private void OnPolyhedronChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (PolyhedronLabel == null) return;
        _currentPolyhedronIndex = (int)PolyhedronSlider.Value;
        _currentPolyhedronData = PolyhedronLibrary.GetPolyhedron(_currentPolyhedronIndex);
        PolyhedronLabel.Content = $"Polyhedron: {_currentPolyhedronData.Name}";
        UpdateVisuals();
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

            // Yaw
            double x1 = Math.Cos(yawRad) * lookDir.X - Math.Sin(yawRad) * lookDir.Z;
            double y1 = lookDir.Y;
            double z1 = Math.Sin(yawRad) * lookDir.X + Math.Cos(yawRad) * lookDir.Z;

            // Pitch
            double x2 = x1;
            double y2 = Math.Cos(pitchRad) * y1 - Math.Sin(pitchRad) * z1;
            double z2 = Math.Sin(pitchRad) * y1 + Math.Cos(pitchRad) * z1;

            // Roll
            double x3 = Math.Cos(rollRad) * x2 - Math.Sin(rollRad) * y2;
            double y3 = Math.Sin(rollRad) * x2 + Math.Cos(rollRad) * y2;
            double z3 = z2;

            cam.LookDirection = new Vector3D(x3, y3, z3);
            cam.UpDirection = new Vector3D(
                Math.Cos(rollRad) - Math.Sin(rollRad),
                Math.Sin(rollRad) + Math.Cos(rollRad),
                0);
        }
    }

    private void OnScenePositionChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TextPosXLabel == null) return;
        double x = TextPosXSlider.Value / 100.0;
        double y = TextPosYSlider.Value / 100.0;
        double z = TextPosZSlider.Value / 100.0;

        TextPosXLabel.Content = $"Box Pos X: {x:F2}";
        TextPosYLabel.Content = $"Box Pos Y: {y:F2}";
        TextPosZLabel.Content = $"Box Pos Z: {z:F2}";

        _boxOffset = new Vector3D(x, y, z);
        _boxVelocity = new Vector3D(0, 0, 0);
    }

    private void OnSceneRotationChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TextPitchLabel == null) return;
        double pitch = TextPitchSlider.Value;
        double roll = TextRollSlider.Value;
        double yaw = TextYawSlider.Value;

        TextPitchLabel.Content = $"Box Pitch: {pitch:F0}°";
        TextRollLabel.Content = $"Box Roll: {roll:F0}°";
        TextYawLabel.Content = $"Box Yaw: {yaw:F0}°";

        _boxPitch = pitch;
        _boxRoll = roll;
        _boxYaw = yaw;
        _angularPitch = _angularYaw = _angularRoll = 0;
    }

    protected override void OnClosed(EventArgs e)
    {
        _renderTimer?.Stop();
        _currentTextBitmap?.Dispose();
        _textRasterizer?.Dispose();
        base.OnClosed(e);
    }
}
