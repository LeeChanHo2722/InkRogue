# Floor route selection

This change keeps the 20-floor/endless runs, authored maps, existing objective directors, weapon families, boss cadence and power ownership. The selection screen is adapted at runtime through the existing `FloorSelectionUI` scene references; scene/prefab YAML and artwork are unchanged.

## Routes and reproducibility

- Three candidates retain Easy/Normal/Hard difficulty. Definitions are shuffled without replacement when enough are available. With one or two valid definitions, maps may repeat.
- Each candidate independently combines its map with Elimination, Rush or Defense. Two matching objectives are allowed. Three matching objectives are forbidden. Thus `Elimination / Elimination / Rush` is valid.
- A route matching either of the last two chosen map/objective pairs halves that offer combination's weight. Repeats remain possible; this is not a blacklist.
- The source catalog must contain a map included in build settings. Null definitions/maps, empty scene names and scenes outside the build are excluded. Zero eligible definitions opens a retry/menu screen; it does not advance a floor.
- `RunManager.InitializeRun(mode, weapons, seed)` accepts an optional explicit seed. The UI records the run seed. Reproduction requires the same seed, source catalog/order, mode and choice history. Default runs obtain a fresh seed. No save/replay UI is added.
- Route generation uses an independent `System.Random` stream per floor. Opening/cancelling/reopening the same offers does not consume RNG or reroll them. The chosen encounter and spawn seed travel with the candidate.

## Risk and rewards

- Objective previews use the same difficulty, seed and floor scaling as the encounter generator. Elimination shows actual generated quota; Rush uses its existing duration/cap; Defense uses its existing assault count/cap. Rush has no exit objective.
- Easy/Normal/Hard clears offer **up to 2/3/4 distinct eligible powers, with exactly one acquisition**. The first floor keeps three options. Player/robot ownership and both-owner eligibility coverage remain unchanged.
- Max-level powers are excluded. A smaller remaining pool yields fewer options. With no eligible power, the existing service action restores ink/shield.
- Final campaign floor 20 displays victory as its goal, not a power reward. Endless boss floors continue to normal rewards and the next floor.
- Reward choices also use a run/floor-derived seed. Damage formulas and power effects are unchanged by this change.

## Interaction

- Click a card or press 1/2/3 to preview. The run's selected candidate does not change until explicit confirmation. Enter confirms; Esc clears the preview.
- Confirm is disabled until a route is selected and while preparing weapons. Confirmation consumes its callback once and locks further input until transition/recovery.
- Weapon preparation stages changes. Choose a weapon (or empty), then a slot. Apply commits the draft; Cancel/Esc discards it. The same weapon is allowed in opposite hands, but not twice within one hand. Empty slots remain valid.
- The existing controls are **E: left-hand wheel; Q: right-hand wheel**. No input binding is changed.
- A root uGUI canvas scales a 1920×1080 reference with Expand, preserving the layout in 16:9, 4:3 and ultrawide displays. The original constant-pixel canvas is unchanged.

## Validation

Editor entry points:

- `FloorRouteValidation.Pure`: 1,000 seeds × 19 offers, reproducibility, filtered catalogs, recent-history uniform counterfactual, reward pools 0–4 and max-level filtering.
- `FloorRouteValidation.Start`: actual InputSystem mouse/keyboard → EventSystem → buttons, preview/cancel/apply, one reward after duplicate click, travel, boss, restart, and GPU captures.
- `FloorRouteMatrixValidation.Start`: all three authored maps × all three objective directors, defense target placement, live quota/preview agreement. Clears are controlled test fixtures; this is not a balance playthrough.
- `FloorBoundaryValidation.Start`: campaign 19→20 ending; endless 19→20→21 and 39→40→41.

Set `FLOOR_UI_CAPTURE_DIR` to a writable output directory before these tests. Use graphics-enabled batch mode for captures. Existing Tower core/play/input/power/wheel/robot regressions remain applicable.

## Next work, intentionally separate

2026-10-07 update: shared run lives and explicit upgrade growth are now implemented as a separate follow-up unit; see [RUN_LIVES_AND_GROWTH.md](RUN_LIVES_AND_GROWTH.md). The paragraph below records the original Floor UX scope. Detailed numerical balance QA is deferred by user instruction.

The newly agreed shared run lives (boss +1, cap 3), removal/rework of automatic total-power damage growth, roughly 100:60 player/robot equal-investment output target, and deeper boss identity require separate balancing/implementation work. None is silently included here. Ninety seconds is a recommended ordinary-floor tempo, not a forced timer; Rush/Defense retain mode-specific timings. Steam consideration is not publication authorization.
