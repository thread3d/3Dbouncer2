using OpenTK.GLControl;
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

    public MainForm()
    {
        InitializeComponent();
        InitializeGLControl();
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
        base.OnFormClosing(e);
    }
}
