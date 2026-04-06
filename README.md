# 3D Text Bouncer

A Windows desktop application that renders text as a physically-simulated 3D particle cloud bouncing inside a translucent polyhedron. Built with WPF, HelixToolkit.Wpf, and .NET 8.

![.NET 8](https://img.shields.io/badge/.NET-8.0-blue)
![HelixToolkit](https://img.shields.io/badge/HelixToolkit.Wpf-2.24.0-green)
![Status](https://img.shields.io/badge/status-Phase%205%20of%206-yellow)

## Features

- **Text-to-Particles** — Convert any text into a 3D particle cloud using SkiaSharp rasterization with scanline even-odd hole detection (letters like O, P, Q, 0, 9 remain hollow)
- **Physics Simulation** — Fixed-timestep elastic collision with letter boundary constraints, keeping particles inside text shapes
- **Polyhedron Container** — Choose from multiple polyhedra (cube, tetrahedron, octahedron, dodecahedron, icosahedron, etc.) as the bouncing container
- **Transparency Modes** — Render face-only, whole polyhedron, or wireframe-only with adjustable opacity
- **18 Fill Rules** — Configurable polygon triangulation and 3D rendering strategies:
  - 2D Triangulation: Fan from Centroid, Ear Clipping, Convex Hull, Strip Partition, Kirkpatrick, Monotone Partition, and more
  - 3D Rendering: Painter Back-to-Front, Backface Culling, Normal Angle Shading, Distance Fog, Emissive Glow, Spectral Coloring
- **Control Modes**
  - *Automatic* — Physics drives particle motion
  - *Manual* — Override box position and rotation via sliders
  - *Mixture* — User input applies forces to physics
- **Live Parameter Adjustment** — Particle count (1K–100K), text color, box opacity, polyhedron selection
- **Additive Slider System** — Position/rotation sliders accumulate deltas rather than setting absolute values, allowing continuous adjustment

## Screenshots

> _Screenshots coming soon_

## Getting Started

### Prerequisites

- Windows 10 or later
- .NET 8 SDK

### Build & Run

```bash
dotnet build src
dotnet run --project src
```

### Run Tests

```bash
dotnet test
```

## Architecture

```
src/
├── FillRules/
│   ├── AlternatingFanStrategy.cs      # Alternating fan triangulation
│   ├── AlternatingOpacityRule.cs      # Alternating face opacity (3D)
│   ├── BackfaceCullingRule.cs         # Cull back-facing faces (3D)
│   ├── ConvexHullStrategy.cs          # Convex hull triangulation
│   ├── DialStrategy.cs                # Minimum diagonal angle
│   ├── DistanceFogRule.cs             # Depth-based fog (3D)
│   ├── EarClipTriangulationStrategy.cs # Ear clipping triangulation
│   ├── EmissiveGlowRule.cs            # Emissive glow effect (3D)
│   ├── FanFromVertexStrategy.cs       # Fan from first vertex
│   ├── FanTriangulationStrategy.cs    # Fan from face centroid
│   ├── FillRuleConfig.cs              # Strategy registry and presets
│   ├── HertelMehlhornStrategy.cs      # Hertel-Mehlhorn approximation
│   ├── IFillRuleStrategy.cs           # Strategy interface
│   ├── KirkpatrickStrategy.cs         # Kirkpatrick triangulation
│   ├── MinimumDiagonalStrategy.cs     # Minimum diagonal selection
│   ├── MonotonePartitionStrategy.cs   # Monotone partition
│   ├── NeighborFanStrategy.cs         # Fan from neighboring faces
│   ├── NormalAngleShadingRule.cs      # Normal-based shading (3D)
│   ├── PainterBackToFrontRule.cs      # Painter's algorithm (3D)
│   ├── SpectralColoringRule.cs        # Spectral coloring (3D)
│   └── WireframeOverlayRule.cs       # Wireframe overlay (3D)
├── PolyhedronData.cs           # Polyhedron vertices, edges, faces
├── ParticleData.cs             # Particle struct (position, velocity, color, 48 bytes)
├── ParticleGenerator.cs        # Bitmap sampling with scanline even-odd hole detection
├── PhysicsSimulator.cs        # Fixed-timestep elastic collision physics
├── TextRasterizer.cs          # SkiaSharp text-to-bitmap rasterization
├── MainWindow.xaml.cs         # WPF 3D viewport, rendering loop, UI event handlers
└── ControlMode.cs             # Automatic / Manual / Mixture modes
```

## Tech Stack

| Component | Technology |
|-----------|------------|
| Runtime | .NET 8 LTS |
| 3D Rendering | HelixToolkit.Wpf 2.24.0 (WPF 3D viewport) |
| Math / Particles | OpenTK.Mathematics 4.9.4 |
| UI Framework | WPF |
| Text Rasterization | SkiaSharp 3.119.2 |
| Physics | Custom fixed-timestep accumulator pattern |

## Roadmap

| Phase | Status | Description |
|-------|--------|-------------|
| 1. Foundation | ✅ | WPF HelixToolkit viewport, basic 3D rendering pipeline |
| 2. Text-to-Particles | ✅ | SkiaSharp rasterization, scanline hole detection, particle distribution |
| 3. Particle Rendering | ✅ | Point cloud rendering via HelixToolkit, 100K particles |
| 4. Physics Simulation | ✅ | Fixed-timestep elastic collision, letter boundary constraints |
| 5. UI Integration | ✅ | Camera controls, side panel, sliders, polyhedron selection, fill rules |
| 6. Mixture Mode & Polish | ⏳ | Hybrid physics/manual interaction, polish |

## License

MIT
