# ROADMAP: 3D Text Bouncer

**Project:** 3D Text Bouncer
**Core Value:** Text rendered as particles bounces realistically in a 3D box while users can interactively control physics and appearance in real-time.
**Granularity:** Fine
**Phases:** 6
**Requirements:** 24 v1 requirements mapped

---

## Phases

- [x] **Phase 1: Foundation** - Working WinForms app with OpenTK GLControl, basic rendering pipeline
- [ ] **Phase 2: Text-to-Particles** - Text rasterization and particle distribution with hole detection
- [ ] **Phase 3: Particle Rendering** - Efficient instanced rendering (100K particles at 60 FPS)
- [ ] **Phase 4: Physics Simulation** - Elastic collision physics with fixed timestep
- [ ] **Phase 5: UI Integration** - Camera controls, side panel with all sliders and inputs
- [ ] **Phase 6: Mixture Mode & Polish** - Physics/manual hybrid interaction and final polish

---

## Phase Details

### Phase 1: Foundation
**Goal:** Working Windows desktop application with OpenGL context initialized and basic rendering pipeline functional

**Depends on:** Nothing (first phase)

**Requirements:** REND-01, REND-02, REND-04, REND-05, UI-03

**Success Criteria** (what must be TRUE):
1. Application launches as standard Windows app with title bar and resizable window
2. OpenGL context initializes without crashes using proper Load event pattern
3. Window resizes without breaking rendering context
4. Background color is configurable and renders correctly
5. Semi-transparent back panel on the box is visible

**Plans:** 1 plan (1 complete)

Plans:
- [x] 01-PLAN.md — Project setup with OpenTK GLControl, OpenGL lifecycle management, and semi-transparent box rendering

---

### Phase 2: Text-to-Particles
**Goal:** Text input converts to particle distribution that respects letter shapes including holes

**Depends on:** Phase 1 (requires OpenGL context for buffer upload)

**Requirements:** TEXT-01, TEXT-02, TEXT-03, TEXT-04, TEXT-05, TEXT-06, TEXT-07

**Success Criteria** (what must be TRUE):
1. User can type text into an input field and see it accepted
2. Text renders to internal bitmap using SkiaSharp with anti-aliasing
3. Particles distribute across actual letter areas, not just bounding boxes
4. Letter holes (O, P, Q, 9, 0, etc.) remain empty - no particles inside
5. Text color is user-configurable via RGB color picker
6. Particle count is adjustable via slider (1K to 100K range)
7. Particle size is adjustable via slider (1px to 10px range)

**Plans:** 3 plans (0 complete)

**Research Flag:** MAYBE - Hole detection algorithm (scanline vs flood-fill) may need validation

Plans:
- [x] 02-01-PLAN.md — SkiaSharp integration: TextRasterizer, ParticleData struct, text input field
- [ ] 02-02-PLAN.md (TDD) — Hole detection: ParticleGenerator with scanline even-odd algorithm and unit tests
- [ ] 02-03-PLAN.md — UI integration: ColorDialog, count/size sliders, GPU buffer upload, particle shaders

---

### Phase 3: Particle Rendering
**Goal:** GPU-efficient particle rendering achieving 100K particles at 60 FPS

**Depends on:** Phase 2 (requires particle positions in GPU buffers)

**Requirements:** REND-03

**Success Criteria** (what must be TRUE):
1. 100,000 particles render at 60 FPS without drops
2. Rendering uses instanced draw call (single draw call for all particles)
3. Particles appear as visible points/sprites in the 3D view
4. Camera can orbit, zoom, and pan around the particle cloud

**Plans:** TBD

---

### Phase 4: Physics Simulation
**Goal:** Physics simulation with elastic bouncing within box boundaries

**Depends on:** Phase 3 (requires working rendering to visualize motion)

**Requirements:** PHYS-01, PHYS-02, PHYS-03, PHYS-04

**Success Criteria** (what must be TRUE):
1. Particles bounce within box boundaries with elastic collision behavior
2. Physics simulation is continuous and visible (particles never stop moving unexpectedly)
3. Physics uses fixed timestep for stable, frame-rate independent behavior
4. Automatic mode runs physics without requiring user input

**Plans:** TBD

**Research Flag:** MAYBE - Compute shader dispatch patterns in OpenTK 4.x may need API verification

---

### Phase 5: UI Integration
**Goal:** Complete UI with camera controls, collapsible side panel, and all parameter sliders

**Depends on:** Phase 4 (requires physics system to receive parameter updates)

**Requirements:** CTRL-01, CTRL-02, CTRL-03, CTRL-05, UI-01, UI-02, UI-04

**Success Criteria** (what must be TRUE):
1. User can switch between Automatic, Manual, and Mixture modes
2. Manual mode allows user to override physics with X, Y, Z position sliders
3. Manual mode allows rotation control via pitch, roll, yaw sliders
4. Side panel is collapsible to maximize 3D view space
5. Camera supports orbit (left-drag), zoom (scroll), and pan (right-drag)
6. UI remains responsive during particle simulation (no freezing)
7. Application maintains 60 FPS while UI interactions occur

**Plans:** TBD

---

### Phase 6: Mixture Mode & Polish
**Goal:** Physics/manual hybrid interaction (mixture mode) and final polish

**Depends on:** Phase 5 (requires both physics and manual controls working)

**Requirements:** CTRL-04

**Success Criteria** (what must be TRUE):
1. Mixture mode applies user input as forces/impulses to physics simulation (not direct override)
2. User can nudge particles and see physics respond naturally
3. Application handles all edge cases gracefully (empty text, extreme values, rapid mode switching)
4. Performance remains stable across all three modes

**Plans:** TBD

**Research Flag:** RESEARCH - Novel physics/manual hybrid interaction needs UX validation

---

## Progress Table

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Foundation | 1/1 | Complete | 2026-04-02 |
| 2. Text-to-Particles | 1/3 | In Progress | 2026-04-02 |
| 3. Particle Rendering | 0/2 | Not started | - |
| 4. Physics Simulation | 0/3 | Not started | - |
| 5. UI Integration | 0/4 | Not started | - |
| 6. Mixture Mode & Polish | 0/2 | Not started | - |

---

## Coverage Summary

**v1 Requirements:** 24 total
**Mapped to phases:** 24
**Unmapped:** 0

| Category | Count | Phases |
|----------|-------|--------|
| Rendering (REND) | 5 | 1, 3 |
| Text-to-Particles (TEXT) | 7 | 2 |
| Physics (PHYS) | 4 | 4 |
| Manual Control (CTRL) | 5 | 5, 6 |
| UI/UX (UI) | 4 | 1, 5 |

---

## Dependency Chain

```
Phase 1 (Foundation)
    ↓
Phase 2 (Text-to-Particles) - requires OpenGL context
    ↓
Phase 3 (Particle Rendering) - requires particle buffers
    ↓
Phase 4 (Physics Simulation) - requires rendering to visualize
    ↓
Phase 5 (UI Integration) - requires physics to control
    ↓
Phase 6 (Mixture Mode & Polish) - requires both physics and controls
```

---

*Roadmap created: 2026-04-02*
*Ready for phase planning*
