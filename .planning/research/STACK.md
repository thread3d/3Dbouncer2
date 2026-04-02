# Technology Stack

**Project:** 3D Text Bouncer
**Domain:** Windows Desktop 3D Particle System Application
**Researched:** 2025-04-02
**Confidence:** HIGH

## Executive Summary

For a Windows desktop 3D particle system application using OpenTK in 2025, the recommended stack is:
- **.NET 8 (LTS)** — Long-term support until November 2026, stable and performant
- **OpenTK 4.9.4** — Latest stable release with full OpenGL 4.x support
- **Windows Forms + OpenTK.GLControl 4.0.2** — Simpler integration than WPF for this use case
- **SkiaSharp 3.119.2** — Modern cross-platform replacement for System.Drawing for text rasterization
- **Custom CPU-based physics** — Sufficient for 100K particles with AABB collision; GPU compute optional for scaling beyond

## Recommended Stack

### Core Framework

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| **.NET** | 8.0 (LTS) | Runtime & Base Framework | LTS support until Nov 2026; stable; better for desktop apps than STS (.NET 9) |
| **OpenTK** | 4.9.4 | OpenGL Bindings & Windowing | Latest stable (Mar 2025); full OpenGL 4.6 support; active maintenance |
| **OpenTK.GLControl** | 4.0.2 | WinForms OpenGL Integration | Official control; supports .NET 5-10; simpler than WPF for single-view apps |
| **OpenTK.Mathematics** | 4.9.4 | Vector/Matrix Math | Bundled with OpenTK; optimized structs for graphics math |

### Text Rendering

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| **SkiaSharp** | 3.119.2 | 2D Text Rasterization | Modern replacement for System.Drawing; cross-platform; hardware-accelerated |
| **SkiaSharp.Views** | 3.119.2 | Platform Views | If needed for hybrid UI approaches |

### Development/Build

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| **MSBuild** | 17.0+ | Build System | Standard .NET build toolchain |
| **dotnet CLI** | 8.0+ | Project Management | Standard .NET SDK tooling |

## Rationale

### Why .NET 8 (LTS) over .NET 9?

**.NET 9** offers 18-28% performance improvements but is **Standard-Term Support (STS)** only until May 2026. For a desktop application expected to have a reasonable lifespan, **.NET 8 LTS** (supported until November 2026) provides:
- Predictable upgrade path to .NET 10 LTS when available
- Stability for production use
- Full compatibility with all OpenTK packages

**Verdict:** Use .NET 8. Projects needing maximum performance can upgrade to .NET 9, but LTS is the safer default.

### Why OpenTK 4.9.4?

**OpenTK 4.9.4** (released March 17, 2025) is the current stable release:
- Targets .NET Core 3.1+ and .NET 5+
- Full OpenGL 4.6 support including compute shaders
- GLFW3-based windowing (cross-platform backend)
- Comprehensive math library (OpenTK.Mathematics)

**OpenTK 5.0** is in preview (5.0.0-pre.15) with Vulkan bindings and PAL2 windowing API, but it's not production-ready. Avoid for this project.

**OpenTK 3.x** is deprecated and targets legacy .NET Framework. Do not use for new projects.

### Why Windows Forms + GLControl over WPF?

For a 3D particle application with interactive controls:

| Factor | WinForms + GLControl | WPF + GLWpfControl |
|--------|----------------------|-------------------|
| **Integration Complexity** | Low — drag-drop control | Medium — requires D3D interop |
| **Performance** | Native OpenGL context | Buffer copies via D3DImage |
| **UI Controls** | Standard Windows sliders/buttons | Richer styling but more overhead |
| **Airspace Issues** | None | None with GLWpfControl 4.3+ |
| **Recommended For** | Single 3D view + controls | Multiple GL views, complex layouts |

**Verdict:** Windows Forms with GLControl is the pragmatic choice for this project. The application has a single 3D view with simple controls (sliders, text input, color picker) — exactly what WinForms excels at. WPF adds complexity without benefit here.

### Why SkiaSharp over System.Drawing?

**System.Drawing.Common** is now Windows-only as of .NET 6+ and generates compiler warnings. Microsoft recommends migrating to **SkiaSharp** or **ImageSharp** for new development.

**SkiaSharp advantages:**
- Cross-platform (if future porting needed)
- Hardware-accelerated rendering
- Modern API design
- Active development (last updated Feb 2025)
- Better text measurement with `SKPaint.MeasureText()`

**Critical note:** SkiaSharp defaults to 72 DPI vs System.Drawing's 96 DPI. Scale text sizes accordingly when porting code.

## Installation

```bash
# Create project
dotnet new winforms -n TextBouncer -f net8.0-windows
cd TextBouncer

# Core OpenTK
dotnet add package OpenTK --version 4.9.4

# WinForms integration
dotnet add package OpenTK.GLControl --version 4.0.2

# Text rendering
dotnet add package SkiaSharp --version 3.119.2

# Optional: SkiaSharp native assets (usually auto-restored)
dotnet add package SkiaSharp.NativeAssets.Win32 --version 3.119.2
```

## Alternatives Considered

| Category | Recommended | Alternative | Why Not |
|----------|-------------|-------------|---------|
| .NET Version | .NET 8 LTS | .NET 9 STS | Shorter support cycle (ends May 2026) not worth 18-28% perf gain for this app |
| UI Framework | WinForms | WPF | WPF adds D3D interop complexity; no benefit for single-view app |
| UI Framework | WinForms | WinUI 3 | WinUI 3 still maturing; more complex; WinForms is stable |
| OpenGL Library | OpenTK | Silk.NET | Silk.NET is newer but OpenTK has larger community, more tutorials |
| Text Rendering | SkiaSharp | ImageSharp | ImageSharp lacks advanced text layout; SkiaSharp is industry standard |
| Physics | Custom CPU | BepuPhysics2 | Bepu is excellent but overkill for simple AABB collision; custom is lighter |
| Particle Update | CPU | GPU Compute | Compute shaders add complexity; CPU can handle 100K particles at 60 FPS |

## Version-Specific Notes

### OpenTK 4.9.4 Breaking Changes
- None significant from 4.8.x — safe upgrade path
- Improved GLFW backend stability
- Better thread safety for `MakeCurrent()` calls

### GLControl 4.0.2 Important Configuration
```csharp
// In Designer or constructor — use 4-digit version for WinForms Designer compatibility
glControl.API = OpenTK.Graphics.API.OpenGL;
glControl.APIVersion = new Version(4, 6, 0, 0); // OpenGL 4.6
glControl.Profile = OpenTK.Graphics.OpenGL.Profile.Core; // Not Compatibility
```

### SkiaSharp 3.x Changes from 2.x
- Requires .NET 6+ (compatible with our .NET 8 target)
- Improved GPU backend support
- API largely unchanged from 2.x

## Confidence Assessment

| Recommendation | Confidence | Basis |
|----------------|------------|-------|
| OpenTK 4.9.4 | **HIGH** | Official NuGet data, recent release (Mar 2025), stable track record |
| .NET 8 LTS | **HIGH** | Microsoft LTS policy, well-documented support lifecycle |
| WinForms + GLControl | **HIGH** | Official OpenTK repos, community patterns, multiple verified tutorials |
| SkiaSharp 3.119.2 | **HIGH** | Official NuGet data, Microsoft's recommended System.Drawing replacement |
| Custom CPU physics | **MEDIUM** | Verified feasible for 100K particles; GPU compute viable alternative if profiling shows need |

## When to Reconsider

**Consider GPU compute shaders when:**
- Profiling shows CPU particle update is bottleneck (targeting >100K particles)
- Need complex particle-particle interactions (beyond AABB wall collision)
- Multiple particle systems running simultaneously

**Consider WPF when:**
- UI becomes significantly more complex (multiple views, docking, advanced styling)
- Need to host other WPF controls over the 3D view (airspace issues with WinForms)

**Consider Silk.NET when:**
- Want newer Vulkan bindings (OpenTK 5 will have these when stable)
- Prefer a more modular library design

## Sources

### Official Documentation
- [OpenTK Official Site](https://opentk.net/) — HIGH confidence
- [OpenTK LearnOpenTK Tutorial](https://opentk.net/learn/index.html) — HIGH confidence
- [OpenTK NuGet Package](https://www.nuget.org/packages/OpenTK/) — HIGH confidence (v4.9.4, Mar 2025)
- [OpenTK.GLControl NuGet](https://www.nuget.org/packages/OpenTK.GLControl) — HIGH confidence (v4.0.2, Jan 2025)
- [OpenTK.GLWpfControl NuGet](https://www.nuget.org/packages/OpenTK.GLWpfControl) — HIGH confidence (v4.3.6)
- [SkiaSharp NuGet](https://www.nuget.org/packages/SkiaSharp) — HIGH confidence (v3.119.2, Feb 2026)

### Community & Best Practices
- [OpenTK.GLControl Repository](https://github.com/opentk/GLControl) — MEDIUM confidence
- [OpenTK GLWpfControl MSAA PR #86](https://github.com/opentk/GLWpfControl/pull/86) — MEDIUM confidence
- [SkiaSharp Text Rendering Guide](https://swharden.com/csdv/skiasharp/text/) — MEDIUM confidence

### Compute Shader Research
- [GPU-Based Particle System (Jan 2025)](https://evanvoodoo.github.io/2025-01-24-gpu-particles/) — MEDIUM confidence
- [NVIDIA Compute Particles Sample](https://docs.nvidia.com/gameworks/content/gameworkslibrary/graphicssamples/opengl_samples/computeparticlessample.htm) — HIGH confidence (NVIDIA official)

### .NET Version Guidance
- [.NET 8 vs .NET 9 Comparison](https://medium.com/@adnankhan13/net-8-vs-net-9-evaluating-the-latest-enhancements-and-upgrade-considerations-7694fc133f2d) — MEDIUM confidence
- [.NET 9 Performance Analysis](https://www.c-sharpcorner.com/article/net-9-apps-are-faster-than-ever/) — MEDIUM confidence
