# 3D Text Bouncer

## What This Is

A Windows desktop application that renders text as a 3D particle cloud bouncing inside a semi-transparent box. Users can control the text content, colors, particle density, and physics via an interactive UI with sliders. Built with C# .NET and OpenTK for 3D rendering.

## Core Value

Text rendered as particles bounces realistically in a 3D box while users can interactively control physics and appearance in real-time.

## Requirements

### Validated

(None yet — ship to validate)

### Active

- [ ] Real-time 3D particle cloud rendering from 2D text
- [ ] Particle distribution respects letter shapes including holes (9, 0, O, p, Q, etc.)
- [ ] Physics simulation: bouncing within box boundaries
- [ ] Semi-transparent back panel on the box
- [ ] Automatic bounce mode (physics-driven)
- [ ] Manual control mode (sliders for position/rotation)
- [ ] Mixture mode: physics sim + user nudges
- [ ] Text input to change rendered text
- [ ] Color picker for text color
- [ ] Slider for particle count
- [ ] Slider for particle size
- [ ] Sliders for X, Y, Z position
- [ ] Sliders for pitch, roll, yaw rotation
- [ ] Resizable window (standard Windows application)

### Out of Scope

- Save/load presets — focus on real-time interaction first
- Export to video/GIF — not core to the experience
- Multiple text objects — single text entity only
- Audio/visual sync — pure visual experience
- Mobile or web support — desktop Windows only

## Context

**Rendering approach:** OpenTK provides OpenGL bindings for .NET, enabling hardware-accelerated 3D rendering with good performance for particle systems.

**Text-to-particles challenge:** Converting 2D text to a particle cloud requires:
1. Rasterizing text to a bitmap
2. Detecting edge boundaries and interior regions
3. Excluding holes from particle placement (e.g., inside 'O', 'p')
4. Distributing points evenly across letter areas

**Physics considerations:** Simple elastic collision with box boundaries. Mixture mode applies user input as forces/impulses to the physics simulation.

## Constraints

- **Tech Stack**: C# .NET Framework or .NET 8+, OpenTK 4.x for 3D rendering
- **Platform**: Windows desktop application
- **UI Framework**: Windows Forms or WPF with WindowsFormsHost for OpenTK control
- **Performance**: Target 60 FPS with up to 100,000 particles
- **Deployment**: Single executable, no installer required

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| OpenTK vs WPF 3D | OpenTK gives better performance and flexibility for particle systems | — Pending |
| Windows Forms host for OpenTK | Easier integration than WPF native | — Pending |
| Bitmap-based text rasterization | Standard GDI+ approach for getting text shapes | — Pending |

---
*Last updated: 2025-04-02 after initialization*
