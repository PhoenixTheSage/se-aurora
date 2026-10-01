# Aurora review fixes — October 1, 2026

All five findings from the October 1 review are implemented locally. Release
builds and regression tests pass for .NET Framework 4.8 and .NET 10. The installed
plugin has not been replaced; in-game acceptance remains pending.

## Changes

- **F1 — motion coverage:** empty volume emission and surface-only glow write
  alpha 0, retaining geometry motion. Volume RGB, ground RGB, ray budgets,
  quality settings, shell bounds and noise recipe remain unchanged.
- **F2 — texture lifetime:** deterministic CPU noise starts once on a worker.
  The render callback disables Aurora while it is pending and never waits for
  unfinished generation. Resolution changes retain the live textures; device
  recreation uploads the retained exact bytes. Texture publication and enabled
  state are cached, with lifecycle invalidation. Ramp replacement creates first
  and swaps only after success.
- **F3 — distance range:** Anomaly adds optional
  `FullscreenPassRegistry.SetVelocityDistanceScale(id, metresPerAlphaUnit)`.
  Aurora requests 1000, writes kilometres in FP16 alpha, and supplies the scale
  in uniform8.w. The host decodes to metres before camera reprojection. Legacy
  hosts or rejected negotiation use scale 1; the full range fix requires both
  updated Aurora and Anomaly. Scratch format, extras size and legacy defaults
  stay unchanged.
- **F4 — preset editing:** first Custom edit seeds both endpoints from the
  active preset before applying the selected edit. Saving still uses the
  existing deferred worker pipeline.
- **F5 — setup failure:** textures and uniforms must be accepted before enabling
  the pass. Ordinary failures disable it, clear uniforms and log once, with
  retry after session/lifecycle recovery. Genuine DXGI device-loss errors still
  propagate to the host. Partial texture construction releases its resources.

## Validation

- Aurora's linked production regression harness passes **105 assertions per
  runtime**. It tests every preset and both endpoints, nonblocking startup,
  exact noise hash, 20 viewport changes, publication/enable transitions, real
  D3D11 texture lifetime, injected ramp failure, recovery, device recreation,
  optional scale rejection and nested DXGI error propagation. Engine/UI/host
  boundaries are stubbed; this is not an in-game reset test.
- Anomaly's tests against its compiled Release assembly pass **15 assertions
  per runtime**, covering default/pre-registration/live scale, invalid input,
  reload/device persistence, opt-out and constant-buffer size/offset.
- Exact compiled Aurora shaders before/after produce bit-identical RGB across
  **8,294,400 pixels** in four synthetic 1080p scenes. Orbit and ground fixtures
  include nonzero emission; inside-shell and miss fixtures are black. Ground
  coverage is raised only in the fixture to exercise surface glow.
- Hardware velocity probes preserve geometry motion `(3,-2)` for visible ground
  glow and invalid/no-history hits. The 89,664.89 m reference decodes to 89,625 m
  (0.0445% FP16 error) instead of clamping to 65,504 m. Translation probes cover
  1 m through 1,000,000 m, within 0.2% relative tolerance. Legacy scale-1 motion
  matches the original contributor at 1000 and 65,000 m.
- Both projects build for net48 and net10 with deployment disabled. Aurora has
  no build warnings/errors beyond assembly-binding advisories. Anomaly retains
  ten existing unreachable-code warnings in ShaderCompileIntercept across the
  two targets. Updated shaders compile; Aurora retains two constant-call-site
  finite-check warnings, while the old uninitialized output warning is gone.

## Performance interpretation

The original repeated CPU bake measured **405.809 ms median** and 5,243,040
allocated bytes. That work is now a one-time background operation. Viewport
changes reuse existing textures. Cached GPU reupload measured approximately
**1.2 ms** in the hardware lifecycle harness on both runtimes; this is a setup
sample, not a whole-game frame-time guarantee. The retained CPU pixel array is
1 MiB. The active 36-float uniform payload remains 168 B per callback.

The updated shader was timed in 36 synthetic warm-resource cases on an RTX
5070 Ti. Timing variation and changed ground-fixture coverage prevent treating
separate runs as a GPU speedup or regression measurement. No shader quality or
render resolution was reduced, and live game FPS has not been measured.

## Reproduce and complete acceptance

Build Anomaly first, then Aurora, using Release with
`-p:RunPostBuildEvent=Never` and the existing `RepoRoot`/`Bin64` overrides.
Run both test projects in `Tests` for net10.0 and net48; see their READMEs.
External review evidence and hardware harness live at
`C:/Users/Zeridian/Documents/ChatGPT/Space Engineers Shaders/Reviews/Aurora-2026-10-01/`;
original source snapshots are retained under `fixes-baseline`.

Before deployment acceptance, validate both updated plugins in the game:
orbital/ground/terminator views with the user's upscaler, preset edits,
resolution changes and a supported device reset. Check startup appearance after
noise readiness and recovery logs. Visual/FPS acceptance and actual game reset
handling remain unverified.
