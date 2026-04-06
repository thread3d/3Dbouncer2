# 3D Text Bouncer

A Windows desktop application that renders text as a physically-simulated 3D particle cloud bouncing inside a semi-transparent box. Built with OpenTK and .NET 8.

![.NET 8](https://img.shields.io/badge/.NET-8.0-blue)
![OpenTK 4.9.4](https://img.shields.io/badge/OpenTK-4.9.4-green)
![Status](https://img.shields.io/badge/status-Phase%205%20of%206-yellow)

## Features

- **Text-to-Particles** — Convert any text into a 3D particle cloud with proper hole detection (letters like O, P, Q, 0, 9 remain hollow)
- **Real-time Physics** — Elastic collision with box boundaries using a fixed timestep for stable, frame-rate independent simulation
- **Interactive Camera** — Orbit (left-drag), zoom (scroll), and pan (right-drag) around the particle cloud
- **Control Modes**
  - *Automatic* — Physics drives particle motion
  - *Manual* — Override position and rotation via sliders
  - *Mixture* — Apply user input as forces to the physics simulation
- **Live Parameter Adjustment** — Particle count (1K–100K), particle size, text color, physics parameters
- **100K Particles at 60 FPS** — GPU-efficient instanced rendering via OpenGL

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
├── FillRules/          # Polygon triangulation strategies (ear clipping, Hertel-Mehlhorn, etc.)
├── PhysicsSimulator.cs  # Fixed-timestep elastic collision physics
├── ParticleData.cs      # Particle struct (position, velocity, color)
├── ParticleGenerator.cs  # Bitmap sampling with scanline hole detection
├── TextRasterizer.cs    # SkiaSharp text-to-bitmap rasterization
├── MainWindow.xaml.cs   # OpenGL rendering loop and WinForms UI
└── ControlMode.cs       # Automatic / Manual / Mixture modes
```

## Tech Stack

| Component | Technology |
|-----------|------------|
| Runtime | .NET 8 LTS |
| 3D Rendering | OpenTK 4.9.4 (OpenGL 4.6) |
| UI Framework | WinForms |
| Text Rasterization | SkiaSharp 3.119.2 |
| Math Library | OpenTK.Mathematics |

## Roadmap

| Phase | Status | Description |
|-------|--------|-------------|
| 1. Foundation | ✅ | OpenTK GLControl, basic rendering pipeline |
| 2. Text-to-Particles | ✅ | SkiaSharp rasterization, hole detection, particle distribution |
| 3. Particle Rendering | ✅ | Instanced rendering, 100K particles at 60 FPS |
| 4. Physics Simulation | ✅ | Elastic collision, fixed timestep |
| 5. UI Integration | 🔄 | Camera controls, side panel, sliders |
| 6. Mixture Mode & Polish | ⏳ | Hybrid physics/manual interaction, polish |

## License

MIT
