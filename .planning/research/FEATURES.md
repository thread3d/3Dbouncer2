# Feature Landscape: 3D Particle Text Bouncer

**Domain:** Interactive 3D Graphics Desktop Application
**Researched:** 2026-04-02
**Confidence:** MEDIUM-HIGH

## Research Context

This research covers interactive 3D particle text applications, examining:
- Professional motion graphics software (After Effects, Notch, TouchDesigner)
- Open-source particle frameworks (Babylon.js, Three.js)
- Desktop/mobile particle apps (Play with Particles)
- Physics simulation patterns and rendering techniques

Sources: Babylon.js 9.0 documentation, particle_text Flutter package, professional VFX software comparisons, physics simulation references, and 3D graphics optimization research papers.

---

## Table Stakes

Features users expect. Missing = product feels incomplete.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| **Real-time 3D rendering** | Core value proposition; users expect fluid interaction | Medium | Target 60 FPS. Use point sprites or instancing for performance |
| **Text-to-particle conversion** | Fundamental to the concept | High | Must respect letter shapes including holes (O, p, 9, etc.) |
| **Physics-based bouncing** | Expected behavior for "bouncer" in name | Medium | Elastic collision with box boundaries |
| **Basic camera controls** | Orbit, zoom, pan are standard 3D UI patterns | Low | OrbitControls pattern: left-drag=rotate, right-drag=pan, scroll=zoom |
| **Color customization** | Visual personalization is table stakes for graphics apps | Low | Color picker for text color |
| **Particle count control** | Users expect to balance quality vs performance | Low | Slider to adjust density |
| **Window resizing** | Standard Windows application behavior | Low | Maintain aspect ratio or adaptive layout |
| **Pause/Play physics** | Users need to freeze state for inspection | Low | Toggle simulation on/off |
| **Reset view/physics** | Escape hatch when things go wrong | Low | Return to default state |

**Table Stakes Analysis:**
These features establish the baseline for any credible 3D particle application. The Flutter particle_text package and Play with Particles iOS app both provide these as minimum viable features. Missing any of these would make the application feel incomplete or broken.

---

## Differentiators

Features that set the product apart. Not expected, but valued.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| **Mixture mode (physics + user control)** | Unique blend of automatic and manual control | Medium | Apply user input as forces/impulses to physics sim |
| **Text morphing** | Particles smoothly re-target when text changes | High | Requires particle-to-particle mapping between texts |
| **Spring-return interaction** | Click/drag to scatter, particles spring back | Medium | Adds tactile, playful quality |
| **Physics parameter controls** | Real-time gravity, elasticity, damping adjustment | Medium | Elasticity: 0-1, Damping: friction coefficient |
| **Blend mode options** | Additive, normal, screen blending for visual variety | Low | Changes how particles composite |
| **Auto-rotation idle mode** | Slow rotation when user inactive | Low | Showcases 3D nature automatically |
| **HSL color palettes** | Pre-configured harmonious color schemes | Low | Cosmic, Fire, Matrix, Pastel, Minimal presets |
| **Particle size variation** | Per-particle size or size-over-lifetime | Low | Adds visual richness |
| **Semi-transparent box panels** | Visual depth and boundary clarity | Low | Different opacity for each face |
| **Performance display** | FPS counter, particle count visible | Low | Helps users optimize settings |
| **Inertia/damping on camera** | Smooth, weighted camera movement | Low | More professional feel |

**Differentiator Analysis:**

The **mixture mode** is a genuinely unique feature not commonly found in particle text applications. Most apps are either fully automatic (physics only) or fully manual (keyframes/sliders). The hybrid approach where user input influences but doesn't override physics is rare and valuable.

**Spring physics** as seen in the Flutter particle_text package provides delightful tactile feedback. The repel-and-return pattern transforms static viewing into playful interaction.

**Real-time physics parameter controls** differentiate from traditional motion graphics software where physics are baked. The ability to tweak gravity, bounce, and friction while watching the result creates an exploratory, playful experience.

---

## Anti-Features

Features to explicitly NOT build.

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| **Save/load presets** | Adds complexity; project scope says defer | Document interesting settings in README examples |
| **Export to video/GIF** | Scope creep; not core experience | User can use screen capture tools if needed |
| **Multiple text objects** | Significant complexity; single text is focused | One powerful text object > multiple weak ones |
| **Audio/visual sync** | Requires audio pipeline, timing complexity | Pure visual experience is scope-appropriate |
| **Custom shaders/materials** | User complexity, debugging burden | Curated visual options with quality defaults |
| **Network/cloud features** | No online component needed | Local-only application |
| **Animation timeline/keyframes** | Traditional motion graphics approach | Focus on real-time physics + interaction |
| **Mobile/web support** | Desktop-focused scope | Perfect Windows desktop experience |
| **3D extruded text** | Changes fundamental particle concept | Stay true to "particle cloud" aesthetic |
| **Particle collision with each other** | N^2 complexity, minimal visual benefit | Simplified physics: particle-to-boundary only |
| **Complex lighting/shadows** | Performance hit, not core to bouncing concept | Simple ambient + diffuse lighting |

**Anti-Feature Rationale:**

These anti-features are deliberately excluded to maintain focus. The project is a **real-time interactive toy**, not a motion graphics production tool. Every anti-feature listed appears in professional software (After Effects, Notch, TouchDesigner) but would distract from the core bouncing particle experience.

Save/load presets were explicitly marked out-of-scope in PROJECT.md. This is correct - it adds file I/O complexity, serialization concerns, and UI clutter for an application meant for immediate, ephemeral play.

---

## Feature Dependencies

```
Core Rendering Pipeline
├── Text Rasterization (GDI+)
├── Particle Distribution (respects letter shapes)
├── Physics Simulation (velocity, collision)
└── Rendering (OpenTK/OpenGL)
    ├── Point Sprites or Instancing
    └── Shader (vertex + fragment)

UI Controls Layer
├── Text Input → triggers Particle Regeneration
├── Color Picker → updates Shader Uniform
├── Particle Count → triggers Regeneration
├── Particle Size → updates Shader Uniform
├── Physics Params → updates Simulation State
│   ├── Gravity (Y acceleration)
│   ├── Elasticity (bounce coefficient)
│   └── Damping (friction)
└── Transform Sliders (Mixture Mode)
    ├── Position (X, Y, Z) → applies impulse
    └── Rotation (Pitch, Roll, Yaw) → applies torque

Camera Controls
├── Orbit (left-drag)
├── Pan (right-drag)
├── Zoom (scroll)
└── Reset (button)
```

**Critical Path Dependencies:**

1. **Text-to-particles must work** before physics or rendering matter
2. **Physics simulation** depends on particle positions being valid
3. **UI controls** depend on physics system accepting parameter updates
4. **Mixture mode** requires both physics AND manual control systems working together

---

## MVP Recommendation

**Prioritize for v1.0:**

1. **Real-time 3D particle text rendering** - Core experience, validate tech stack
2. **Basic physics (bounce in box)** - Validates "bouncer" concept
3. **Text input + color picker** - Basic customization
4. **Camera orbit/zoom/pan** - Essential 3D navigation
5. **Particle count/size sliders** - Performance + appearance control

**Defer to v1.x:**

- Mixture mode (requires careful physics integration)
- Physics parameter sliders (gravity, elasticity, damping)
- Text morphing (complex particle remapping)
- Color presets (nice-to-have)

**Never build:**

- Export features
- Multiple text objects
- Audio sync
- Save/load presets (per PROJECT.md)

**MVP Rationale:**

The MVP establishes the core "text as bouncing particles" experience. Users must be able to: see their text as particles, watch it bounce, change the text and color, and navigate the 3D view. Everything else enhances but these five features validate the concept.

---

## Implementation Notes

### Physics Parameters (from research)

| Parameter | Typical Range | Implementation |
|-----------|---------------|----------------|
| Gravity | 0 - 50 m/s² | Y-axis acceleration |
| Elasticity (Restitution) | 0.0 - 1.0 | 0=inelastic, 1=perfect bounce |
| Linear Damping | 0.0 - 0.1 | Velocity *= (1 - damping) per frame |
| Particle Mass | Fixed or varied | Affects momentum in collisions |

### Rendering Approach

**Recommended: Point Sprites**
- Single draw call for all particles
- Automatic billboarding
- Sufficient for 100K particles at 60fps
- Simpler than instancing for this use case

**Alternative: GPU Instancing**
- Better for complex per-particle variation
- More setup overhead
- Consider if point sprite size limits become issue

### UI Patterns

**Sliders with immediate feedback:**
- Value changes apply immediately to running simulation
- No "Apply" button needed for real-time apps
- Consider value labels showing current setting

**Mode switching:**
- Automatic mode: physics drives everything
- Manual mode: sliders control directly
- Mixture mode: sliders apply forces (rare/unique)

---

## Competitive Analysis Summary

| Feature | Play with Particles | particle_text | Notch/TouchDesigner | This App |
|---------|---------------------|---------------|---------------------|----------|
| Real-time physics | Yes | Yes | Yes | Yes |
| Text-to-particles | Yes | Yes | Yes | Yes |
| Spring interaction | Yes | Yes | No | Should have |
| Mixture physics/manual | No | No | Partial | **Differentiator** |
| Export video | No | No | Yes | Out of scope |
| Multiple objects | No | No | Yes | Out of scope |
| Live param adjust | Yes | Yes | Yes | Yes |

**Gap identified:** No existing app combines real-time physics with user nudge controls (mixture mode). This is the opportunity.

---

## Sources

- [Babylon.js 9.0 Particle Features](https://blogs.windows.com/windowsdeveloper/2026/03/26/announcing-babylon-js-9-0/) - Physics attractors, flow maps
- [particle_text Flutter Package](https://pub.dev/packages/particle_text) - Spring physics, interaction patterns
- [Play with Particles App](https://apps.apple.com/us/app/play-with-particles/id6758525457) - Mobile particle text UX
- [Notch vs TouchDesigner Comparison](https://www.saashub.com/compare-notch-vs-touchdesigner) - Professional VFX features
- [Three.js OrbitControls](https://threejs.org/docs/pages/OrbitControls.html) - Camera control patterns
- [Particle Rendering Techniques](https://geeks3d.com/20140929/test-particle-rendering-point-sprites-vs-geometry-instancing-based-billboards) - Performance optimization
- [X-Particles Presets Documentation](https://docs.x-particles.net/html/presets.php) - Save/load UX patterns
- [OpenTK/GLControl](https://github.com/opentk/GLControl) - Windows OpenGL integration
- [myPhysicsLab Collisions](https://www.myphysicslab.com/engine2D/collision-en.html) - Physics simulation parameters
- [Unity Particle GPU Instancing](https://docs.unity3d.com/2021.2/Documentation/Manual/PartSysInstancing.html) - Rendering optimization

**Confidence Notes:**
- **HIGH** for table stakes features (well-established patterns)
- **MEDIUM** for mixture mode (novel combination, feasible but needs validation)
- **HIGH** for anti-features (explicit scope decisions from PROJECT.md)
