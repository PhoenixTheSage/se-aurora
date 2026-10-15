# se-aurora changelog

## 1.3.2 — 2026-10-15

- Report settings-disabled, missing atmospheric planet, daylight, distance fade, pending noise, and submitted states instead of a generic snapshot message.
- Update catalog fixtures to the CPU-image publication contract and exercise visibility gates, upload-failure recovery, device-end cleanup, and nested device-loss classification on both runtimes.

## 1.3.1 — 2026-10-14

- Add a renderer-aware status dialog and independently retried Status page shared by Pulsar configuration and Rich HUD.
- Expose renderer and terminal-registration health in aurora diagnostics.
- Normalize deployment paths and pass the configured Pulsar directory to deployment; replace empty Publicizer access grants with declared game assembly names.

## 1.3.0 — previous release

Release 1.3.0 guest scene and shared textures

Version: 1.2.2 -> 1.3.0 (minor).

Changelog:
- Read camera, sun, and frame time from Anomaly's scene view on the guest bus, and from the Keen render assembly on DX11.
- Drop the DX11-only texture owner from the core plugin so aurora art publishes through the shared catalog.
- Keep the AfterAtmosphere program on the shared bus for both renderers.

Co-authored-by: Cursor <cursoragent@cursor.com>
