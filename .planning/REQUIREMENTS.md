# Requirements: 3D Text Bouncer

**Defined:** 2025-04-02
**Core Value:** Text rendered as particles bounces realistically in a 3D box while users can interactively control physics and appearance in real-time.

## v1 Requirements

Requirements for initial release. Each maps to roadmap phases.

### Rendering

- [x] **REND-01**: OpenGL context initializes without crashes (using Load event pattern)
- [x] **REND-02**: Semi-transparent box renders with back panel visible
- [ ] **REND-03**: Point particles render efficiently (100K at 60 FPS via instancing)
- [x] **REND-04**: Window resizes without breaking rendering context
- [x] **REND-05**: Background color is configurable

### Text-to-Particles

- [ ] **TEXT-01**: Text input field accepts any string input
- [ ] **TEXT-02**: Text is rasterized to bitmap using SkiaSharp
- [ ] **TEXT-03**: Particles are distributed across letter areas (not just bounding box)
- [ ] **TEXT-04**: Letter holes (O, P, Q, 9, etc.) are not filled with particles
- [ ] **TEXT-05**: Text color is user-configurable (RGB color picker)
- [ ] **TEXT-06**: Particle count is controllable via slider (1K to 100K)
- [ ] **TEXT-07**: Particle size is controllable via slider (1px to 10px)

### Physics

- [ ] **PHYS-01**: Particles bounce within box boundaries (elastic collision)
- [ ] **PHYS-02**: Physics uses fixed timestep for stability
- [ ] **PHYS-03**: Bouncing motion is visible and continuous
- [ ] **PHYS-04**: Automatic mode: physics runs without user input

### Manual Control

- [ ] **CTRL-01**: Sliders for X, Y, Z position (range: -box to +box)
- [ ] **CTRL-02**: Sliders for pitch, roll, yaw rotation (range: -180 to +180 degrees)
- [ ] **CTRL-03**: Manual mode: user controls override physics
- [ ] **CTRL-04**: Mixture mode: user input applies as forces to physics simulation
- [ ] **CTRL-05**: Mode switch: Automatic / Manual / Mixture

### UI/UX

- [ ] **UI-01**: All controls are in a collapsible side panel
- [ ] **UI-02**: 3D view is main focus with orbit/zoom/pan camera controls
- [x] **UI-03**: Application launches as standard Windows app with title bar
- [ ] **UI-04**: UI remains responsive during particle simulation

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### Rendering

- **REND-V2-01**: GPU compute shaders for physics (if CPU physics insufficient)
- **REND-V2-02**: Particle glow/bloom effects
- **REND-V2-03**: Screenshot capture

### Physics

- **PHYS-V2-01**: Particle-to-particle collision
- **PHYS-V2-02**: Gravity direction control
- **PHYS-V2-03**: Bounciness/elasticity parameter

### Interaction

- **INT-V2-01**: Click-to-scatter particles with spring return
- **INT-V2-02**: Save/load presets
- **INT-V2-03**: Animation recording (video export)

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| Audio/visual sync | Not core to particle visualization experience |
| Mobile or web support | Desktop Windows focus per PROJECT.md |
| Multiple text objects | Single text entity keeps scope manageable |
| Network/multiplayer | No networking component in vision |
| Font selection | System default is sufficient for v1 |
| Particle textures | Point sprites are simpler and performant |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| REND-01 | Phase 1 | Complete |
| REND-02 | Phase 1 | Complete |
| REND-03 | Phase 3 | Pending |
| REND-04 | Phase 1 | Complete |
| REND-05 | Phase 1 | Complete |
| TEXT-01 | Phase 2 | Pending |
| TEXT-02 | Phase 2 | Pending |
| TEXT-03 | Phase 2 | Pending |
| TEXT-04 | Phase 2 | Pending |
| TEXT-05 | Phase 2 | Pending |
| TEXT-06 | Phase 2 | Pending |
| TEXT-07 | Phase 2 | Pending |
| PHYS-01 | Phase 4 | Pending |
| PHYS-02 | Phase 4 | Pending |
| PHYS-03 | Phase 4 | Pending |
| PHYS-04 | Phase 4 | Pending |
| CTRL-01 | Phase 5 | Pending |
| CTRL-02 | Phase 5 | Pending |
| CTRL-03 | Phase 5 | Pending |
| CTRL-04 | Phase 6 | Pending |
| CTRL-05 | Phase 5 | Pending |
| UI-01 | Phase 5 | Pending |
| UI-02 | Phase 5 | Pending |
| UI-03 | Phase 1 | Complete |
| UI-04 | Phase 5 | Pending |

**Coverage:**
- v1 requirements: 24 total
- Mapped to phases: 24
- Unmapped: 0 ✓

---
*Requirements defined: 2025-04-02*
*Last updated: 2026-04-02 after completing Phase 1 Plan 01*
