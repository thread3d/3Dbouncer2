---
phase: 01-foundation
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - src/TextBouncer.csproj
  - src/Program.cs
  - src/MainForm.cs
  - src/MainForm.Designer.cs
  - src/GLHost.cs
  - src/Shaders/box.vert
  - src/Shaders/box.frag
autonomous: true
requirements:
  - REND-01
  - REND-02
  - REND-04
  - REND-05
  - UI-03

must_haves:
  truths:
    - Application launches as standard Windows app with title bar and resizable window
    - OpenGL context initializes without crashes using proper GLControl.Load event pattern
    - Window resizes without breaking rendering context
    - Background color is configurable and renders correctly
    - Semi-transparent back panel on the box is visible (demonstrates working depth/alpha)
  artifacts:
    - path: "src/TextBouncer.csproj"
      provides: "Project configuration with OpenTK 4.9.4 and OpenTK.GLControl 4.0.2"
      contains: ["PackageReference Include=\"OpenTK\"", "PackageReference Include=\"OpenTK.GLControl\"", "TargetFramework>net8.0-windows"]
    - path: "src/MainForm.cs"
      provides: "Main application window with GLControl embedded"
      exports: ["MainForm class", "Application.Run"]
    - path: "src/GLHost.cs"
      provides: "OpenGL rendering host with proper lifecycle management"
      exports: ["OnGLControlLoad", "OnRenderFrame", "OnResize"]
      contains: ["_glLoaded flag guard", "MakeCurrent() calls", "GL.ClearColor", "SwapBuffers"]
    - path: "src/Shaders/box.vert"
      provides: "Vertex shader for semi-transparent box rendering"
    - path: "src/Shaders/box.frag"
      provides: "Fragment shader with alpha blending for box back panel"
  key_links:
    - from: "src/MainForm.cs"
      to: "src/GLHost.cs"
      via: "GLControl instance and event wiring"
      pattern: "glControl.Load += glHost.OnGLControlLoad"
    - from: "src/GLHost.cs"
      to: "OpenGL context"
      via: "MakeCurrent() before GL operations"
      pattern: "_glControl.MakeCurrent()"
    - from: "src/GLHost.cs"
      to: "Application.Idle"
      via: "Game loop registration"
      pattern: "Application.Idle += OnApplicationIdle"
---

<objective>
Create the foundational Windows desktop application with OpenTK GLControl integration. Establish proper OpenGL context lifecycle management following the Load event pattern to prevent race condition crashes. Implement a semi-transparent box rendering to validate the rendering pipeline works correctly with depth testing and alpha blending.

Purpose: Without a stable OpenGL foundation, all subsequent phases (text-to-particles, physics, UI) cannot function. The GL context initialization pattern established here prevents the common race condition crashes that plague OpenTK beginners.

Output:
- Complete .NET 8 WinForms project with OpenTK dependencies
- MainForm with embedded GLControl (proper event wiring)
- GLHost managing OpenGL lifecycle with _glLoaded guard
- Shader infrastructure for box rendering
- Configurable background color via GL.ClearColor
</objective>

<execution_context>
@C:/Users/threa/.claude/get-shit-done/workflows/execute-plan.md
@C:/Users/threa/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@D:/projects/3Dbouncer2/.planning/ROADMAP.md
@D:/projects/3Dbouncer2/.planning/REQUIREMENTS.md
@D:/projects/3Dbouncer2/.planning/research/STACK.md
@D:/projects/3Dbouncer2/.planning/research/PITFALLS.md
@D:/projects/3Dbouncer2/.planning/research/ARCHITECTURE.md

## Critical Patterns from Research

**OpenGL Context Initialization (REND-01):**
- GL context does NOT exist until GLControl.Load event fires
- Accessing GL before Load causes AccessViolationException
- Must use _glLoaded flag guard in all GL operations
- Always call MakeCurrent() before GL operations

**Application.Idle Game Loop (REND-04):**
- Application.Idle pauses during window resize (known limitation)
- Acceptable for Phase 1 - physics will resume after resize
- Alternative: background thread with synchronization (overkill for now)

**Window Resize (REND-04):**
- Must guard against height=0 in viewport calculation
- Pattern: `if (h == 0) h = 1` before GL.Viewport
- Must call MakeCurrent() in resize handler

**Project Structure from ARCHITECTURE.md:**
```
src/
├── TextBouncer.csproj      # .NET 8 + OpenTK packages
├── Program.cs              # Entry point
├── MainForm.cs             # WinForms host
├── GLHost.cs               # OpenGL lifecycle manager
└── Shaders/
    ├── box.vert
    └── box.frag
```

**Anti-Patterns to Avoid (from PITFALLS.md):**
- DO NOT initialize GL in Form.Load - must use GLControl.Load
- DO NOT forget _glLoaded guard in Paint/Render methods
- DO NOT call GL operations without MakeCurrent()
- DO NOT use GL.SwapBuffers() - use glControl.SwapBuffers()
</context>

<tasks>

<task type="auto">
  <name>Task 1: Create project structure and .csproj with OpenTK dependencies</name>
  <files>src/TextBouncer.csproj, src/Program.cs</files>
  <action>
Create the .NET 8 WinForms project file with proper OpenTK package references.

Create src/TextBouncer.csproj:
- TargetFramework: net8.0-windows
- UseWindowsForms: true
- PackageReference: OpenTK 4.9.4
- PackageReference: OpenTK.GLControl 4.0.2
- OutputType: WinExe

Create src/Program.cs:
- Standard WinForms entry point with ApplicationConfiguration.Initialize()
- Application.Run(new MainForm())

Verification: dotnet build should succeed and restore packages.
  </action>
  <verify>
    <automated>cd src && dotnet restore && dotnet build</automated>
  </verify>
  <done>Project builds successfully, OpenTK packages restored</done>
</task>

<task type="auto">
  <name>Task 2: Implement MainForm with GLControl and GLHost lifecycle management</name>
  <files>src/MainForm.cs, src/MainForm.Designer.cs, src/GLHost.cs</files>
  <action>
Create the main application window with proper OpenTK GLControl integration and lifecycle management.

Create src/MainForm.cs:
- Inherit from Form
- Create GLControl in constructor with OpenGL 4.6 Core profile settings
- Set GLControl.Dock = Fill to fill the form
- Wire GLControl.Load event to GLHost.OnGLControlLoad
- Wire GLControl.Paint event to trigger rendering
- Wire GLControl.Resize event to GLHost.OnResize
- Start Application.Idle loop after GL is loaded

Create src/MainForm.Designer.cs:
- Partial class with InitializeComponent
- Declare GLControl field

Create src/GLHost.cs with CRITICAL race condition protection:
- Private bool _glLoaded = false field (REQUIRED - prevents AccessViolationException)
- OnGLControlLoad method:
  - Set _glLoaded = true
  - Call _glControl.MakeCurrent()
  - Set GL.ClearColor to configurable value (default: CornflowerBlue)
  - Setup viewport with GL.Viewport
  - Initialize shader program
  - Create box VAO/VBO
- OnRenderFrame method:
  - GUARD: if (!_glLoaded) return; (CRITICAL - prevents crash before context ready)
  - Call _glControl.MakeCurrent()
  - GL.Clear with ColorBufferBit and DepthBufferBit
  - Render box with semi-transparent back panel
  - _glControl.SwapBuffers() (NOT GL.SwapBuffers())
- OnResize method:
  - GUARD: if (!_glLoaded) return;
  - Call _glControl.MakeCurrent()
  - Guard: if (height == 0) height = 1 (prevents divide by zero)
  - Update GL.Viewport
  - Update projection matrix
- OnApplicationIdle method:
  - if (_glControl.IsIdle) { _glControl.Invalidate(); }

Critical implementation notes:
- The _glLoaded flag MUST be checked before ANY GL operation
- MakeCurrent() must be called before GL operations when multiple contexts might exist
- Window resize during drag will pause Application.Idle - this is expected behavior
  </action>
  <verify>
    <automated>cd src && dotnet build && timeout 5s dotnet run --no-build 2>&1 || echo "App started (timeout expected)"</automated>
  </verify>
  <done>
MainForm displays with title bar, GLControl fills the window, no AccessViolationException on startup,
_glLoaded guard is implemented, MakeCurrent() called before all GL operations
  </done>
</task>

<task type="auto">
  <name>Task 3: Implement semi-transparent box rendering with depth/alpha validation</name>
  <files>src/Shaders/box.vert, src/Shaders/box.frag, src/GLHost.cs</files>
  <action>
Create shader infrastructure and box geometry to validate the rendering pipeline with depth testing and alpha blending. This validates REND-02 (semi-transparent box with visible back panel) and REND-05 (configurable background).

Create src/Shaders/box.vert:
- Input: vec3 aPosition
- Uniform: mat4 mvp (model-view-projection)
- Output: vec3 vPosition (world position for fragment shader)
- gl_Position = mvp * vec4(aPosition, 1.0)

Create src/Shaders/box.frag:
- Input: vec3 vPosition
- Output: vec4 FragColor
- Calculate face normal from vPosition to determine which face we're rendering
- Back faces (z < 0): render with semi-transparent color (alpha 0.3)
- Front faces (z >= 0): render with more opaque color (alpha 0.6)
- Enable proper alpha blending results

Update src/GLHost.cs:
- Add ShaderProgram compilation in OnGLControlLoad:
  - Load box.vert and box.frag from Shaders/ directory
  - Compile vertex and fragment shaders
  - Link program and validate
  - Store program ID
- Add Box mesh creation:
  - Create VAO, VBO for cube vertices (8 corners, 36 indices for triangles)
  - Store for rendering
- Add RenderBox method:
  - Use shader program
  - Set MVP uniform (simple perspective projection for now)
  - Enable GL_BLEND with GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA
  - Enable GL_DEPTH_TEST
  - Bind VAO and draw
- Update OnRenderFrame to call RenderBox
- Add background color configuration (GL.ClearColor in OnGLControlLoad)

Camera/Projection (minimal for Phase 1):
- Simple static camera looking at origin
- Perspective projection with reasonable FOV
- Box centered at origin, size approximately 2x2x2 units
  </action>
  <verify>
    <automated>cd src && dotnet build && ls Shaders/</automated>
  </verify>
  <done>
Shaders compile and link without errors, box renders with visible semi-transparent back panel,
depth testing works correctly (back panel visible through front), background color is configurable
  </done>
</task>

</tasks>

<verification>
[Phase 1 Foundation Verification]

1. Build Verification:
   - Command: cd src && dotnet build
   - Expected: Build succeeds with 0 errors, 0 warnings

2. Launch Verification:
   - Command: cd src && timeout 3s dotnet run 2>&1 || true
   - Expected: Window appears with title bar, no AccessViolationException

3. Runtime Verification:
   - Window resizes without crash
   - Background color is configurable (change GL.ClearColor, rebuild, verify)
   - Semi-transparent box with back panel visible

4. Code Review Verification:
   - GLHost.cs contains _glLoaded flag guard
   - All GL operations check _glLoaded first
   - MakeCurrent() called before GL operations
   - height == 0 guard present in resize handler
</verification>

<success_criteria>
[Observable Phase Completion Criteria]

1. Application launches as standard Windows app:
   - Has title bar, minimize/maximize/close buttons
   - Resizable window borders work
   - Taskbar icon visible

2. OpenGL context initializes without crashes:
   - No AccessViolationException on startup
   - No InvalidOperationException
   - Window displays background color immediately

3. Window resize works correctly:
   - Can drag window edges to resize
   - Rendering resumes after resize complete
   - No context loss or black screen

4. Background color configurable:
   - Changing GL.ClearColor value changes the displayed color
   - Color renders correctly (not black)

5. Semi-transparent box visible:
   - Box geometry renders in 3D view
   - Back panel is visible through semi-transparent front
   - Demonstrates depth buffer and alpha blending work
</success_criteria>

<output>
After completion, create `.planning/phases/01-foundation/01-SUMMARY.md` with:
- What was built (project structure, key files)
- Key implementation patterns established (_glLoaded guard, MakeCurrent)
- Files created with line counts
- Performance notes (Application.Idle behavior during resize)
- Known limitations (resize pauses - to be addressed in Phase 4 if needed)
</output>
