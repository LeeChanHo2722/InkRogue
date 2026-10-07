# Robot companion movement and attack feedback — 2026-10-06

The robot now searches around the player, approaches a nearby enemy, attacks, returns when separated, and resumes its search. It no longer follows one fixed offset. Existing temporary art, damage curves, owner-specific growth and power-recursion guards are retained. No graphics were generated and no package was added.

## Behaviour

- Acquire visible targets within 5.5 world units of the player. Discard destroyed, invalid, occluded or out-of-leash targets.
- Return starts beyond 6 units and stays latched until within 1.6 units. Returning takes priority over attacks, preventing boundary oscillation.
- Search / pursuit / return speeds are 2.5 / 3.8 / 6.5 units per game second. Acceleration is capped at 12 units/s² and turning at 360 degrees/s. Attacks begin inside 2.2 units; movement eases toward a 1.7-unit standoff.
- Body-radius casts and a bounded local route search reuse the existing combat QA's navigation approach. Normal obstacle avoidance does not teleport. A floor/scene relocation, player teleport over 14 units, or a persistently unreachable return route can relocate the robot safely to the player. A fully enclosed route uses this recovery rather than remaining stuck.
- Player input locks, death/transitions, results and restart stop the companion and remove stale effects. Game time drives movement, attacks and effect lifetime, preserving pause and Q/E slow motion.

## Visible combat feedback

Basic attacks show a muzzle flash, a gold beam from the robot and an impact ring on the same frame as the damage. Spray shows an orange radius and target lines; arc shows cyan radius/zigzag links; brush shows its range and impacts; vacuum shows inward cyan cues; salvage shows green recovery feedback toward the player. Effects last 0.32 game seconds and use the gameplay sorting layer.

Actual render inspection caught a shader mismatch that state-only tests missed: the sprite-specific shader did not draw LineRenderer geometry. The final version reuses Unity's existing UI/Default shader, with a shared material, and shows the lines in the actual game render. No custom shader or new art asset is required.

## Validation and evidence

- **PASS:** dedicated robot validation, **42 assertions**, `robot-validation-4.log`.
- Covered autonomous live-map damage, search/follow, approach/attack, bounded acceleration, separate damage ownership, target destruction, return hysteresis, no attacks during return, player-centered leash, body-safe wall detour, no shooting through walls, enclosed-route recovery, transition locks, floor relocation, all power feedback, pause and restart cleanup.
- **PASS:** Q/E lifecycle **39**, existing input **30**, progression **16**, power/ownership **33**; **118 regression assertions**. Final total including robot checks: **160**.
- Logs: `robot-TowerWheelValidation.log`, `robot-TowerInputValidation.log`, `robot-TowerPlayValidation.log`, `robot-TowerPowerValidation.log`.
- Actual effect-on / effect-off camera renders changed **6,251 pixels** for basic fire, **44,907** for brush/vacuum, **53,134** for spray/arc, and **20,742** for salvage. HUD and camera setup remain identical in each pair. Each is independently above the 100-pixel minimum.
- Eight actual Unity render captures are in `robot-captures/`; preview archive: `InkRogue-Robot-Preview.zip`. Basic fire and movement captures use the live map; power captures add stationary high-HP targets to expose the effects clearly. They are screenshots, not a continuous recording.
- **PASS:** Windows build, `robot-windows-build.log`.
- **PASS:** standalone launch smoke: responsive process, Direct3D 12 initialized, no detected runtime exceptions during a 12-second observation. Evidence: `robot-standalone-smoke.log` and `.json`. This does not claim manual gameplay or Alt-Tab validation.
- Runtime assembly SHA256: `996AE46DED3456AE1D9D5018848AC07D7D398EB6D017ED46BF07D7A9A9DCF0F3`.

Early diagnostic results are retained. A first state-only run passed despite invisible effects; it was not accepted as visual completion. Shader correction and independent render comparison are included in the final pass. A later fixture expected a null target after returning to a populated map; it now permits a valid newly acquired nearby target while rejecting stale distant targets.

## Scope and remaining checks

Movement checks use controlled obstacles and targets for reproducibility, plus live-map combat and rendered captures. They do not establish full unprotected 20-floor survival balance or long-duration pathfinding performance across every map seed. Prior combat limitations remain in COMBAT_QA_REPORT.md. Extended combat measurements predate this movement change; unprotected survival balance needs a new manual run. Physical desktop Alt-Tab/manual control feel remains a manual check; the Q/E focus/pause lifecycle regression passes.

Latest executable: `Builds/TowerCleanupRobotCompanion/TowerCleanup.exe`. Keep its adjacent data/DLL folders. Previous builds are preserved. User Packages changes are excluded from the commit. No remote push, publication, deployment, asset purchase or generated art.

Preview archive saved to ChatGPT Library as `InkRogue-Robot-Preview.zip` (final version).
