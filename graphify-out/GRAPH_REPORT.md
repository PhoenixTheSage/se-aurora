# Graph Report - se-aurora  (2026-09-16)

## Corpus Check
- 49 files · ~982,303 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 553 nodes · 849 edges · 33 communities (29 shown, 4 thin omitted)
- Extraction: 97% EXTRACTED · 3% INFERRED · 0% AMBIGUOUS · INFERRED: 25 edges (avg confidence: 0.83)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `083d98a8`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Plugin
- Config
- Volumetric Aurora Borealis
- AnomalyBridge
- AuroraTexture
- TranspilerHelpers
- HLSL
- Layout
- PreloaderHelpers
- KeybindAttribute
- SettingsGenerator
- System.Runtime.CompilerServices
- setup.py
- AuroraRenderer
- Aurora Borealis with Planet Screenshot
- SliderAttribute
- ButtonAttribute
- Aurora Borealis
- Aurora Borealis
- ClientPlugin.Settings.Elements
- ClientPlugin.csproj
- Attribute
- DropdownAttribute
- Aurora Borealis
- CheckboxAttribute
- ColorAttribute
- IElement
- SeparatorAttribute
- TextboxAttribute
- Control
- clean.sh
- Deploy.sh
- verify_props.sh

## God Nodes (most connected - your core abstractions)
1. `Config` - 29 edges
2. `AuroraTexture` - 18 edges
3. `SettingsGenerator` - 17 edges
4. `TranspilerHelpers` - 17 edges
5. `PreloaderHelpers` - 16 edges
6. `ClientPlugin.Settings.Elements` - 15 edges
7. `Control` - 15 edges
8. `IElement` - 14 edges
9. `KeybindAttribute` - 14 edges
10. `AnomalyBridge` - 13 edges

## Surprising Connections (you probably didn't know these)
- `March LOD` --semantically_similar_to--> `Bounded Fixed-Step March`  [INFERRED] [semantically similar]
  AGENTS.md → Docs/Plan.md
- `AuroraBorealis.hlsl` --semantically_similar_to--> `HLSL Shader Programming`  [INFERRED] [semantically similar]
  Docs/Plan.md → .agents/skills/a5c-ai-babysitter-hlsl/SKILL.md
- `Roy Theunissen Aurora Breakdown` --semantically_similar_to--> `Roy Theunissen Unity Aurora`  [INFERRED] [semantically similar]
  README.md → Docs/Plan.md
- `Aurora Borealis Pack` --semantically_similar_to--> `Volumetric Aurora Borealis`  [INFERRED] [semantically similar]
  README.md → Docs/Plan.md
- `GitHub Copilot Instructions` --semantically_similar_to--> `VS Code Agents Instructions`  [INFERRED] [semantically similar]
  .github/copilot-instructions.md → .vscode/AGENTS.md

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Agent Instruction Bootstrap** — github_copilot_instructions, vscode_agents, agents [EXTRACTED 1.00]
- **Volumetric Aurora Technique** — docs_plan_raymarching, docs_plan_difference_clouds, docs_plan_spherical_shell_segment, docs_plan_additive_hdr_blending, docs_plan_auroraborealis_hlsl [EXTRACTED 1.00]
- **Ground-Looking-Up Night Landscape** — docs_abovemountain_aurora_borealis, docs_abovemountain_starfield, docs_abovemountain_volumetric_clouds, docs_abovemountain_snow_capped_mountains [EXTRACTED 1.00]
- **Orbital Limb Aurora Composition** — docs_fromspace_aurora_borealis, docs_fromspace_planetary_limb, docs_fromspace_starfield, docs_fromspace_atmospheric_terminator [EXTRACTED 1.00]
- **Ground-Looking-Up Night Landscape** — docs_screenshot_aurora_borealis, docs_screenshot_starfield, docs_screenshot_broken_night_clouds, docs_screenshot_snow_capped_mountains [EXTRACTED 1.00]
- **Planet-Surface Night Landscape** — docs_withplanet_aurora_borealis, docs_withplanet_planet_surface, docs_withplanet_snow_capped_mountains, docs_withplanet_milky_way_skybox, docs_withplanet_starfield, docs_withplanet_crescent_moon [EXTRACTED 1.00]
- **Duplicated HLSL Skill Copies** — agents_skills_a5c_ai_babysitter_hlsl_readme, agents_skills_a5c_ai_babysitter_hlsl_skill, cursor_skills_a5c_ai_babysitter_hlsl_readme, cursor_skills_a5c_ai_babysitter_hlsl_skill [INFERRED 0.95]

## Communities (33 total, 4 thin omitted)

### Community 0 - "Plugin"
Cohesion: 0.07
Nodes (16): AuroraSampler, Plugin, Instance, MethodImpl, AnomalyTerminalHook, Action, Color, MethodInfo (+8 more)

### Community 1 - "Config"
Cohesion: 0.05
Nodes (39): AuroraColorPreset, BlueTeal, Custom, Green, GreenBlue, GreenPurple, RedPurple, AuroraQuality (+31 more)

### Community 2 - "Volumetric Aurora Borealis"
Cohesion: 0.06
Nodes (42): Anomaly Owns Shared Rendering Gaps, IsolatedMix Energy, March LOD, Rich HUD Config Save Rule, se-dev Skill, Compute Shaders, Constant Buffer Management, HLSL Shader Programming (+34 more)

### Community 3 - "AnomalyBridge"
Cohesion: 0.07
Nodes (19): Assembly, AnomalyBridge, HasDisplayTenant, IEnumerable, MethodInfo, Type, RenderTraceBind, Exception (+11 more)

### Community 4 - "AuroraTexture"
Cohesion: 0.10
Nodes (18): AuroraTexture, Name, Resource, Size, Size3, Srv, AuroraTextures, Noise (+10 more)

### Community 5 - "TranspilerHelpers"
Cohesion: 0.17
Nodes (13): CodeInstructionNotFound, TranspilerHelpers, CodeInstruction, CodeInstructionPredicate, IEnumerable, List, MethodInfo, Exception (+5 more)

### Community 6 - "HLSL"
Cohesion: 0.10
Nodes (21): HLSL Skill README, Compute GPU Processing, DirectX Shaders, GLSL, HLSL, Shader Optimization, HLSL Skill, Microsoft HLSL Documentation (+13 more)

### Community 7 - "Layout"
Cohesion: 0.10
Nodes (18): Layout, SettingsPanelSize, Func, List, MyGuiControlBase, Vector2, None, SettingsPanelSize (+10 more)

### Community 8 - "PreloaderHelpers"
Cohesion: 0.21
Nodes (9): PreloaderHelpers, CodeInstructionPredicate, Instruction, List, Collection, FieldReference, MethodDefinition, MethodReference (+1 more)

### Community 9 - "KeybindAttribute"
Cohesion: 0.09
Nodes (18): ControlButtonData, KeybindAttribute, SupportedTypes, Action, Func, List, Type, Binding (+10 more)

### Community 10 - "SettingsGenerator"
Cohesion: 0.10
Nodes (17): AttributeInfo, SettingsGenerator, ActiveLayout, Dialog, Action, Func, List, MethodInfo (+9 more)

### Community 11 - "System.Runtime.CompilerServices"
Cohesion: 0.17
Nodes (9): Hashing, CodeInstruction, IEnumerable, Instruction, MethodImpl, MethodInfo, ConstructorInfo, System.Runtime.CompilerServices (+1 more)

### Community 12 - "setup.py"
Cohesion: 0.25
Nodes (14): _ensure_props(), _generate_guid(), _get_install_locations(), _get_linux_steam_path(), _get_steam_path(), _get_windows_steam_path(), _input_plugin_name(), _input_question() (+6 more)

### Community 13 - "AuroraRenderer"
Cohesion: 0.15
Nodes (13): Additive HDR Blending, AtmosphereRendererPatch, AuroraRenderer, AuroraSampler, AuroraSnapshot, Fail-Closed Renderer, Game-to-Render Snapshot, MyGBuffer Main LBuffer (+5 more)

### Community 14 - "Aurora Borealis with Planet Screenshot"
Cohesion: 0.31
Nodes (13): Additive Aurora Transparency, Aurora Borealis, Aurora Spill on Terrain, Aurora Borealis with Planet Screenshot, Crescent Moon, Green-to-Teal Altitude Gradient, Milky Way Skybox, Blue Night Atmosphere (+5 more)

### Community 15 - "SliderAttribute"
Cohesion: 0.18
Nodes (10): SliderAttribute, SupportedTypes, SliderType, Float, Integer, Action, Func, List (+2 more)

### Community 16 - "ButtonAttribute"
Cohesion: 0.29
Nodes (6): ButtonAttribute, SupportedTypes, Action, Func, List, Type

### Community 17 - "Aurora Borealis"
Cohesion: 0.35
Nodes (11): Additive Aurora Transparency, Atmospheric Terminator Glow, Aurora Borealis, Aurora From Orbit Screenshot, Green-to-Teal-to-Blue Gradient, Limb-Following Aurora Band, Planetary Limb, Snowy Planet Surface (+3 more)

### Community 18 - "Aurora Borealis"
Cohesion: 0.35
Nodes (11): Additive Aurora Transparency, Aurora Borealis, Aurora Night Landscape Screenshot, Broken Night Clouds, Clouds Occlude Aurora, Emerald Green Emission, Night Sky, Sky-Only Aurora Composite (+3 more)

### Community 20 - "ClientPlugin.csproj"
Cohesion: 0.22
Nodes (6): net10.0, net48, Krafs.Publicizer (2.3.0), Lib.Harmony (2.4.2), Mono.Cecil (0.11.6), Microsoft.NET.Sdk

### Community 21 - "Attribute"
Cohesion: 0.50
Nodes (3): Attribute, IgnoresAccessChecksToAttribute, AssemblyName

### Community 22 - "DropdownAttribute"
Cohesion: 0.28
Nodes (6): DropdownAttribute, SupportedTypes, Action, Func, List, Type

### Community 24 - "Aurora Borealis"
Cohesion: 0.42
Nodes (9): Additive Aurora Transparency, Aurora Borealis, Aurora Over Mountains Screenshot, Green-to-Teal Altitude Gradient, Night Sky, Snow-Capped Mountain Foreground, Night Starfield, Vertical Curtain Rays (+1 more)

### Community 25 - "CheckboxAttribute"
Cohesion: 0.29
Nodes (6): CheckboxAttribute, SupportedTypes, Action, Func, List, Type

### Community 26 - "ColorAttribute"
Cohesion: 0.25
Nodes (7): ColorAttribute, SupportedTypes, Action, Color, Func, List, Type

### Community 27 - "IElement"
Cohesion: 0.29
Nodes (6): IElement, SupportedTypes, Action, Func, List, Type

### Community 28 - "SeparatorAttribute"
Cohesion: 0.29
Nodes (6): SeparatorAttribute, SupportedTypes, Action, Func, List, Type

### Community 29 - "TextboxAttribute"
Cohesion: 0.29
Nodes (6): TextboxAttribute, SupportedTypes, Action, Func, List, Type

### Community 30 - "Control"
Cohesion: 0.40
Nodes (4): Control, MyGuiControlBase, Vector2, MyGuiDrawAlignEnum

## Knowledge Gaps
- **96 isolated node(s):** `HasDisplayTenant`, `Name`, `Resource`, `Srv`, `Size` (+91 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 210 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **4 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `System.Runtime.CompilerServices` connect `System.Runtime.CompilerServices` to `Config`, `Attribute`?**
  _High betweenness centrality (0.159) - this node is a cross-community bridge._
- **Why does `ClientPlugin.Settings.Elements` connect `ClientPlugin.Settings.Elements` to `Config`, `KeybindAttribute`, `SliderAttribute`, `ButtonAttribute`, `DropdownAttribute`, `CheckboxAttribute`, `ColorAttribute`, `IElement`, `SeparatorAttribute`, `TextboxAttribute`, `Control`?**
  _High betweenness centrality (0.137) - this node is a cross-community bridge._
- **Why does `SettingsGenerator` connect `SettingsGenerator` to `Plugin`, `ClientPlugin.Settings.Elements`, `Control`, `Layout`?**
  _High betweenness centrality (0.120) - this node is a cross-community bridge._
- **What connects `HasDisplayTenant`, `Name`, `Resource` to the rest of the system?**
  _96 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Plugin` be split into smaller, more focused modules?**
  _Cohesion score 0.07439024390243902 - nodes in this community are weakly interconnected._
- **Should `Config` be split into smaller, more focused modules?**
  _Cohesion score 0.04717853839037928 - nodes in this community are weakly interconnected._
- **Should `Volumetric Aurora Borealis` be split into smaller, more focused modules?**
  _Cohesion score 0.05758582502768549 - nodes in this community are weakly interconnected._