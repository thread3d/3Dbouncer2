---
gsd_state_version: 1.0
milestone: v1.0
milestone_name: milestone
status: unknown
last_updated: "2026-04-02T09:32:37.401Z"
progress:
  total_phases: 6
  completed_phases: 2
  total_plans: 4
  completed_plans: 4
---

# STATE: 3D Text Bouncer

**Project:** 3D Text Bouncer
**Core Value:** Text rendered as particles bounces realistically in a 3D box while users can interactively control physics and appearance in real-time.

---

## Project Reference

**Tech Stack:**
- .NET 8 LTS
- OpenTK 4.9.4
- OpenTK.GLControl 4.0.2
- SkiaSharp 3.119.2
- WinForms

**Target Performance:** 100,000 particles at 60 FPS

**Key Constraints:**
- Windows desktop only
- Single executable, no installer
- GPU-centric particle system architecture

---

## Current Position

| Attribute | Value |
|-----------|-------|
| **Current Phase** | 03-physics-simulation |
| **Current Plan** | 01 |
| **Status** | Phase 2 complete - Text-to-particle pipeline ready with GPU buffers |
| **Last Action** | Completed 02-03 with ColorDialog, TrackBars, and particle shaders |

### Progress Bar

```
Overall:  [████░░░░░░░░░░░░░░] 17% (2/6 phases complete, Phase 3 ready)
Phase 1:  [██████████████████] 100% (1/1 plans complete)
Phase 2:  [██████████████████] 100% (3/3 plans complete)
Phase 3:  [░░░░░░░░░░░░░░░░░░] 0% (not started)
Phase 4:  [░░░░░░░░░░░░░░░░░░] 0% (not started)
Phase 5:  [░░░░░░░░░░░░░░░░░░] 0% (not started)
Phase 6:  [░░░░░░░░░░░░░░░░░░] 0% (not started)
```

---

## Performance Metrics

| Metric | Target | Current | Status |
|--------|--------|---------|--------|
| Particle Count | 100,000 | N/A | Not measured |
| Frame Rate | 60 FPS | N/A | Not measured |
| Physics Timestep | Fixed 1/60s | N/A | Not implemented |

---

## Accumulated Context

### Key Decisions Made

| Decision | Rationale | Date |
|----------|-----------|------|
| OpenTK 4.9.4 over 5.0 preview | Stable release with full OpenGL 4.6 support | 2026-04-02 |
| WinForms + GLControl over WPF | Simpler integration for single-view 3D app | 2026-04-02 |
| SkiaSharp over System.Drawing | Microsoft deprecated System.Drawing for cross-platform | 2026-04-02 |
| 6-phase fine-grained roadmap | Requirements naturally group into 6 delivery boundaries | 2026-04-02 |
| Namespace is OpenTK.GLControl not WinForms | OpenTK 4.x uses GLControl namespace | 2026-04-02 |
| Application.Idle sufficient without IsIdle check | Modern GLControl doesn't expose IsIdle, Idle event throttles naturally | 2026-04-02 |
| Sequential struct layout for ParticleData | GPU SSBO requires predictable memory layout for std140 | 2026-04-02 |
| Bgra8888 pixel format for text | Matches OpenGL GL_BGRA for direct texture upload | 2026-04-02 |
| 512x256 bitmap size for text | Sufficient resolution for particle distribution, manageable size | 2026-04-02 |
| ParticleData 32-byte alignment | Position (16) + Color (16) for std140 layout compatibility | 2026-04-02 |
| Scanline even-odd for hole detection | Correctly identifies enclosed regions vs solid areas | 2026-04-02 |
| Alpha threshold 128 for edges | Middle gray handles anti-aliased text edges | 2026-04-02 |
| Scan 0 to x-1 for point-in-letter | Starting at point causes incorrect crossing count | 2026-04-02 |
| Point sprites over instanced quads | Simpler for 100K particles, single draw call | 2026-04-02 |
| GL.DeleteBuffer before GenBuffer | Explicit cleanup prevents GPU memory leaks | 2026-04-02 |

### Open Questions

1. **Compute shader dispatch:** Verify OpenTK 4.x API patterns during Phase 4
2. **Mixture mode physics:** Design force/impulse application approach during Phase 6

### Known Risks

| Risk | Mitigation | Phase to Watch |
|------|------------|----------------|
| OpenGL context race conditions | Initialize only in GLControl.Load event | Phase 1 |
| Physics timestep instability | Use fixed timestep with accumulator | Phase 4 |
| Text hole detection failure | Use scanline with proper anti-aliasing threshold | Phase 2 |
| GPU memory leaks | Implement IDisposable for GL resources | All phases |

---

## Session Continuity

### Last Session
- **Date:** 2026-04-02
- **Activity:** Executed Plan 03 of Phase 2 - Buffer Integration
- **Outcome:** Complete text-to-particle pipeline with ColorDialog, TrackBars, and point sprite rendering

### Next Actions
1. Plan Phase 3: Physics Simulation (Plan 03-01)
2. Design ParticlePhysics class with velocity/position integration
3. Implement collision detection with box boundaries

### Deferred to v2
- GPU compute shaders for physics (if CPU insufficient)
- Particle glow/bloom effects
- Particle-to-particle collision
- Click-to-scatter with spring return
- Save/load presets
- Animation recording

---

*State initialized: 2026-04-02*
