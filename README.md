# Rainy Day Bakery

A small, playable Unity **2022.3.62f1** prototype based on the included [GDD](Docs/GDD_Rainy%20Day%20Bakery.pdf). Play as Mira in a cozy seaside bakery: change the weather, gather an ingredient, knead, bake, and serve Pak Aruna. Nami gives hints. Three orders complete a day; there is no countdown or game over.

## Open and play

1. Copy or clone this entire folder onto your computer.
2. In Unity Hub, choose **Open / Add project from disk**, select this folder, and open it with **2022.3.62f1**. Allow Unity to generate its local settings and import assets.
3. Open **Assets/Scenes/Bakery.unity** and press **Play**.
4. Select **Open the bakery** on the main menu.

The scene deliberately starts empty. `RainyDayGame.Bootstrap` creates the game and camera at runtime. All artwork is drawn in code with Unity's built-in IMGUI; no art downloads, paid assets, custom fonts, render pipeline, or Inspector wiring are required. Use a 1280 × 800 Game view or any larger window; the presentation scales and letterboxes automatically.

## First full day

- **Rain:** click **Gather**, then **Mix dough / Knead** three times. Click **Oven** to load the dough. When the timer reaches 6–10 seconds, click **Take out**, then **Serve**.
- **Fog:** select **FOG** in the sidebar and repeat to make a rosemary hearth loaf.
- **Sun:** select **SUN** and repeat to make a lemon morning roll.
- After three deliveries, view the day summary and choose another day or return to the main menu.

Clicking a station walks Mira there and interacts automatically. Alternatively use **WASD / arrow keys**, then **E** near a station. **Escape** or the pause button pauses both movement and baking. Manual movement cancels a pending click destination.

Weather can change freely. An ingredient already in your basket keeps its original recipe. Every loaf earns 8 shells, matching the request adds 5, and golden baking adds 4. Soft or toasty bread and alternative recipes are always accepted. A perfect day earns 51 shells. Lifetime shells and completed days save locally through `PlayerPrefs` after the third order. An unfinished day is not saved; the pause menu explicitly labels the option that discards it.

## Verify and build in Unity

- Choose **Rainy Day Bakery → Verify Gameplay Rules**. The Console should report eight passed checks for baking boundaries and rewards.
- For a Windows build, install **Windows Build Support (Mono)** for this editor version in Unity Hub, then choose **Rainy Day Bakery → Build Windows**. Output: `Builds/Windows/RainyDayBakery.exe` and supporting files. Distribute the whole output folder.
- For other platforms, use **File → Build Settings**. The Bakery scene is already included.
- Batch rule validation (replace the editor executable path as appropriate):

  ```sh
  Unity -batchmode -nographics -quit -projectPath /path/to/rainydaybakery -executeMethod BakeryProject.VerifyRules -logFile rules.log
  ```

## Scope and implementation

- Main menu, animated coastal courtyard, three weather effects, clickable stations, keyboard movement, recipe and order guidance, oven timing, pause, day summary, and local lifetime progress.
- `Assets/Scripts/RainyDayGame.cs`: runtime presentation and interaction state machine.
- `Assets/Scripts/BakeryRules.cs`: isolated baking quality and reward rules.
- `Assets/Editor/BakeryProject.cs`: Unity rule checks and Windows build command.

This is an original geometric-art prototype, not a reproduction of Lemon Cake assets. Its small loop draws on the management-game reference and the GDD's weather-to-bake idea. It does not yet include sound, full village exploration, seasons, story progression, multiple simultaneous customers, or an ingredient inventory beyond the current batch. Those are future scope, not required for this playable day.

## Validation in the cloud workspace

The cloud environment has no Unity editor or C# compiler. All three C# files were parsed with the C# tree-sitter grammar with zero syntax errors; project JSON, scene/build references, and source asset metadata were checked. This does **not** establish Unity compilation or Play Mode behavior. The eight Unity rule checks and a full interactive smoke test remain to be run in your local editor.

Suggested smoke test: finish the three matching orders for 51 shells; serve an early alternative loaf for 8 shells; pause during baking and confirm the timer stops; change weather after gathering and confirm the recipe stays fixed; complete a day and restart Play Mode to confirm lifetime totals persist.
