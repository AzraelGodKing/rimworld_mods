# Strata V4 Roadmap

**Strata 4.0.0** is a RimWorld **1.7-only** major release. 1.6 is frozen at **3.8.x**, which gets bugfixes only, from a maintenance branch or tag.

Tracking: [AZR-467](https://linear.app/azraelgodking/issue/AZR-467) · Linear milestone **Strata 4 (RimWorld 1.7)** in the RimWorld Mods project.

Earlier work: [V3_ROADMAP.md](V3_ROADMAP.md) · Series roadmap: [../ROADMAP.md](../ROADMAP.md)

---

## Why 1.7 is a big deal for Strata

1.7 replaces Unity with Ludeon's own engine (Waylith, Vulkan) running on .NET 10. A diff of the 1.6 and 1.7 `Assembly-CSharp` shows **no new gameplay systems**: nothing native for levels, layers or portals, so Strata has nothing to merge with. The engine-side additions are what matter:

- **Mod shaders.** A mod can ship GLSL in a `Shaders/` folder (any load folder, so `1.7/Shaders` works), registered with `ShaderDef` / `MaterialDef` XML. Shaders hot-reload for mods in the local Mods folder. In 1.6, custom shaders needed Unity AssetBundles, which Strata never used.
- **A built-in frame profiler** (`Window_FrameProfiler`). It can instrument `Strata.dll` and attribute Harmony patches to their mod, with allocation and VRAM tables.
- **`Graphics.DrawMesh(..., renderQueue)`**, a per-draw queue override, so materials no longer need cloning per render queue.
- **.NET 10**: `UnsafeAccessor` / `UnsafeAccessorType`, `Span<T>`, `Vector256<T>`, `CollectionsMarshal`, `System.Threading.Lock`, and the BCL `PriorityQueue`.

Strata uses no draw paths the engine leaves unsupported (no `DrawMeshNow`, `RenderTexture`, `GL`, `RenderMeshInstanced`, or AssetBundles).

## Build and compat (blocking)

- [ ] **1.7 in-game test + `supportedVersions` 1.7** — [AZR-457](https://linear.app/azraelgodking/issue/AZR-457)
- [ ] **Transpilers still match** (`Patch_CombinedResources`, `RitualTravel`) — [AZR-458](https://linear.app/azraelgodking/issue/AZR-458)
- [ ] **See-below rendering on the new engine** — [AZR-459](https://linear.app/azraelgodking/issue/AZR-459)
- [ ] **CI checks against new game builds, then the 1.7 build** — [AZR-460](https://linear.app/azraelgodking/issue/AZR-460)
- [ ] **net10 build** (Strata: net10 only, no dual build) — [AZR-461](https://linear.app/azraelgodking/issue/AZR-461)

## Visuals (Strata's own shaders)

- [ ] **See-below shader**: darken, desaturate and depth-tint through a hole-mask texture. Replaces the mask mesh and the per-queue material clones; stretch goal is seeing two or more levels down — [AZR-468](https://linear.app/azraelgodking/issue/AZR-468)
- [ ] **Gas overlay shader**: one textured quad with smooth, drifting clouds instead of per-cell squares — [AZR-469](https://linear.app/azraelgodking/issue/AZR-469)
- [ ] **Depth effects**: screen-edge darkening with depth, heat shimmer (magma), water caustics (flooded level); each can be turned off — [AZR-470](https://linear.app/azraelgodking/issue/AZR-470)

## Performance

- [ ] **Profiler baseline** on a reference multi-level save; re-run after each item below — [AZR-471](https://linear.app/azraelgodking/issue/AZR-471)
- [ ] **Cross-mod compat accessors** (VEF / VTE / Dubs / Rimatomics / Rimefeller pipes, Combat Extended): cached delegates, then `UnsafeAccessorType` — [AZR-466](https://linear.app/azraelgodking/issue/AZR-466)
- [ ] **Gas overlay labels and readout**: stop allocating every OnGUI pass — [AZR-472](https://linear.app/azraelgodking/issue/AZR-472)
- [ ] **Atmosphere data layout**: flat density array, `Color32`, `Vector256`, `CollectionsMarshal`; fix the cave-plan worker that blocks the main thread — [AZR-473](https://linear.app/azraelgodking/issue/AZR-473)
- [ ] **Off-thread work modernisation** (`Lock`, `Parallel`, cancellation) — [AZR-464](https://linear.app/azraelgodking/issue/AZR-464)
- [ ] **Series-wide tick/draw pass** — [AZR-463](https://linear.app/azraelgodking/issue/AZR-463)

## Content (V3 leftovers, headline features)

These can be built on 1.6 now and ship in 4.0.

- [ ] **Magma layer**: bottom-of-the-world dig endpoint; geothermal taps, obsidian, fire and heat — [AZR-474](https://linear.app/azraelgodking/issue/AZR-474)
- [ ] **Flooded level**: underground lake floor; sump/flood promoted to a level type, Odyssey fishing, Homesteader wells, Stormproof surges — [AZR-475](https://linear.app/azraelgodking/issue/AZR-475)
- [ ] **Lost floor**: rare sealed story level that teaches the noise rule — [AZR-476](https://linear.app/azraelgodking/issue/AZR-476)

## Rules for V4 work

- Keep 1.7-only assets (shaders, `ShaderDef`/`MaterialDef`) in `1.7/` load folders.
- Multiplayer determinism: keep `Verse.Rand` for game logic; seeded `System.Random` cave plans must produce the same layout as 3.8.x; no game-state access off the main thread.
- 1.7 is still an unstable branch, so the shader API (`ShaderProgramDefinition`) may change before release. Re-check before shipping.
- Version: 3.8.3 → **4.0.0** at ship; `About.xml` is edited only after an explicit yes.

## Performance baseline

_Filled in by [AZR-471](https://linear.app/azraelgodking/issue/AZR-471)._
