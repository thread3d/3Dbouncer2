---
gsd_state_version: 1.0
milestone: v1.0
milestone_name: milestone
status: in_progress
last_updated: "2026-04-02T12:38:00.000Z"
progress:
  total_phases: 6
  completed_phases: 4
  total_plans: 6
  completed_plans: 7
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
| **Current Phase** | 05-ui-integration |
| **Current Plan** | 01 complete |
| **Status** | Phase 5 in progress - ControlMode foundation complete |
| **Last Action** | Completed 05-01 with ControlMode enum and PhysicsSimulator mode support |

### Progress Bar

```
Overall:  [██████████░░░░░░░░] 67% (4/6 phases complete, Phase 5 in progress)
Phase 1:  [██████████████████] 100% (1/1 plans complete)
Phase 2:  [██████████████████] 100% (3/3 plans complete)
Phase 3:  [██████████████████] 100% (1/1 plans complete)
Phase 4:  [██████████████████] 100% (1/1 plans complete)
Phase 5:  [███░░░░░░░░░░░░░░░] 25% (1/4 plans complete)
Phase 6:  [░░░░░░░░░░░░░░░░░░] 0% (not started)
```

---

## Performance Metrics

| Metric | Target | Current | Status |
|--------|--------|---------|--------|
| Particle Count | 100,000 | N/A | Not measured |
| Frame Rate | 60 FPS | N/A | Not measured |
| Physics Timestep | Fixed 1/60s | 1/60s | Implemented |

---

| Phase 03-particle-rendering P01 | 25 | 3 tasks | 6 files |
| Phase 04-physics-simulation P01 | 12 min | 3 tasks | 4 files |
| Phase 05-ui-integration P01 | 8 min | 3 tasks | 2 files |

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
| Instanced quads over point sprites | Better scalability and visual quality for 100K particles | 2026-04-02 |
| SSBO with std430 layout | Matches C# Sequential struct layout exactly | 2026-04-02 |
| Buffer orphaning pattern | Prevents GPU stalls during particle buffer updates | 2026-04-02 |
| Spherical camera coordinates | Natural orbit controls with pitch clamping | 2026-04-02 |
| Fixed timestep physics at 60Hz | Stable physics regardless of frame rate (PHYS-02) | 2026-04-02 |
| Accumulator clamped to 5 frames | Prevents spiral of death on frame drops | 2026-04-02 |
| Particle radius 0.02f for collisions | Accounts for visual size in boundary detection | 2026-04-02 |
| Perfectly elastic collisions default | Bounciness = 1.0 matches expected behavior | 2026-04-02 |
| _isRunning kept for backward compatibility | Mode property is primary control but IsRunning still works | 2026-04-02 |
| Manual mode pauses via early return | Cleanest implementation - skip physics updates entirely | 2026-04-02 |
| ApplyManualOffsets applies uniform translation | Phase 6 will add rotation-based transforms | 2026-04-02 |

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
- **Activity:** Executed Plan 01 of Phase 5 - UI Integration
- **Outcome:** ControlMode enum created, PhysicsSimulator updated with Mode property and manual overrides

### Next Actions
1. Plan Phase 5 Plan 2: Collapsible side panel with SplitContainer
2. Design UI controls for camera and physics parameters
3. Implement mode switcher UI (radio buttons)

### Deferred to v2
- GPU compute shaders for physics (if CPU insufficient)
- Particle glow/bloom effects
- Particle-to-particle collision
- Click-to-scatter with spring return
- Save/load presets
- Animation recording

---

*State initialized: 2026-04-02*
*Updated: 2026-04-02*
