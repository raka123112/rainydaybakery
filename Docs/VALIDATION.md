# Validation status — 20 October 2026

## Executed in cloud

- C# tree-sitter parse of all six source files: no syntax errors.
- Roslyn compilation and execution of `BakeryModel` + `BakeryModelChecks` on .NET 8: PASS. Covers missing stock, locked recipes, transactional ingredient use, one-batch constraint, three kneads, rain/fog timing, weather snapshot, oven prerequisites, inclusive quality bounds, accepted early/late bread, rewards, unlocks, all eight recipes, ingredient collection, Aruna-specific delivery, and restored in-progress state.
- Additional runtime-source compilation smoke test: PASS against third-party Unity 2021.3 core-module reference assemblies and a Unity UI reference assembly, with a small URP API-shape shim. This catches C# type/member errors against those references. It **does not validate Unity 2022.3, actual URP binaries, editor code, rendering, or a player build**. Temporary references/shims are not shipped.
- URP Create/Light2D initialization API inspected against Unity Graphics `2022.3/staging` source.
- Manifest JSON, unique asset GUIDs, scene/build references, required art/fonts, and whitespace checked.
- Generated village, interior, character and item art inspected visually. These are art inspections, not screenshots from a running Unity game.

## Required in Unity 2022.3.62f1 before release

1. Clean import: Package Manager resolves URP 14.0.11 and uGUI; Console has no compile errors. Verify the editor script creates Settings assets and applies the 2D pipeline.
2. Run **Rainy Day Bakery → Verify Gameplay Rules**.
3. Play the Bakery scene; confirm no missing/magenta materials, legible UI, correct character alpha slicing and correct light/sorting layers.
4. Complete a Cinnamon Roll from pantry to serving while raining. Confirm three kneads, 12s proof, displayed golden interval, and +17 shells for a matching golden loaf.
5. Repeat early/late removal and an alternative recipe; all bread must remain sellable.
6. Switch weather, visit every site, test click paths and keyboard movement. Confirm field/pier access restrictions and fog-only mushroom harvest. Check routes against the illustrated furniture/coastline.
7. Talk to Aruna in fog, bake coastal loaf, deliver to him in fog; verify quest and sunny pier honey reward. Generic counter delivery must not complete his quest.
8. Pause and open recipe/journal during baking; elapsed bake time must remain unchanged. Close panels and resume.
9. Quit while proofing/baking, restart, confirm stock, batch/weather snapshot, and timer restoration. Test a damaged primary JSON with a valid `.bak`; verify recovery and on-screen save warnings if writing fails.
10. Deliver five breads, unlock every recipe, collect all eight bread types and all ten ingredients.
11. Test 1600×1000, 1920×1080 and window resizing; inspect modal bounds and location markers.
12. Build Windows with Build Support installed and launch outside Unity. Check dynamic sprite shaders survive stripping, fonts render, controls work, saves persist and mute works.

Unity Editor is not installed in this cloud workspace. No Unity Play Mode pass, GPU screenshot, Windows executable, or successful Unity build is claimed.
