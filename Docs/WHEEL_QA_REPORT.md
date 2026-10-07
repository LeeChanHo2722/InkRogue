# Q/E weapon-wheel slow motion — 2026-10-06

Default wheel speed is 0.1. Q opens the right-hand wheel; E opens the left-hand wheel. Cursor position, highlight and selection update every frame without scaled waits. Releasing the active hand equips the selection; a still-held other hand takes over. Releasing the final hand restores the previous game speed and physics step.

The wheel keeps one local time owner. Pause, death/cinematic control, input lock, restart, reward/result UI and floor transitions cancel it before changing time. External changes are not overwritten when the wheel closes. Existing 0.4 speed is restored to 0.4; an existing stronger 0.05 slow effect is preserved. The fixed step is proportional to game speed. Unity's read-back value is saved after assignment because its internal fixed-step quantization otherwise falsely looked like another time owner and immediately closed the wheel.

Run elapsed time and combat timers continue to use scaled game time. Existing real-time transition/cinematic timers retain their original behavior. No new package, asset, general-purpose time framework or art was added.

## Validation

- **PASS:** `TowerWheelValidation`, 39 assertions, `wheel-validation-12.log`.
- Actual InputSystem Q/E action callbacks and cursor state drive wheel opening, hand switching, selection and release. Existing input QA's Dynamic state feed is reused. The harness waits for an active encounter and explicitly enables its virtual devices in the headless Editor.
- Covered: 0.1 speed and proportional fixed step; cursor/pointer responsiveness; scaled run timer; release/equip; overlapping Q/E and quick switching; previous 0.4/0.05 speeds; Escape pause/resume and release while paused; external time writes; focus/application-pause callbacks; component disable/input lock; cinematic takeover and completed smooth restore; actual damage/death/respawn; floor reward/next floor; menu restart and result restart.
- **PASS:** existing input 30, run-flow 16 and power/robot 33 assertions, **79 regression assertions**. Logs: `wheel-TowerInputValidation.log`, `wheel-TowerPlayValidation.log`, `wheel-TowerPowerValidation.log`.
- **PASS:** Windows Standalone build, `wheel-windows-build.log`.
- **PASS:** standalone launch smoke, responsive process and Direct3D 12 initialization; no detected runtime exceptions during the 12-second observation. Evidence: `wheel-standalone-smoke.log` and `.json`. This is not a manual gameplay pass. The initial graphics-log matcher omitted the space in `Direct3D 12`; its result was corrected against the recorded log.
- Runtime assembly SHA256: `67E096C014225665783AEA550A2794CD878A1034632B4B78A9C7134EBA680556`.

Early harness attempts failed because of headless virtual-device enable state and premature input during startup. Later diagnostics separated those issues from the actual fixed-step rounding bug, which was corrected and tested. A further test waited for the cinematic's time value before its coroutine completed; the fixture now waits for coroutine completion. Failed diagnostic logs are retained in the workspace and are not counted as passes.

Focus loss/application pause are simulated by invoking the Unity lifecycle callbacks; physical Alt-Tab on a human desktop has not been manually verified. Tests use protection and forced floor-clear setup where appropriate. They do not establish full unprotected 20-floor survival balance; prior combat limitations in COMBAT_QA_REPORT.md remain applicable.

## Delivery

`Builds/TowerCleanupWheelSlow/TowerCleanup.exe`. Keep the adjacent data/DLL folders. The previous `TowerCleanupBalanced` build is preserved. User Packages changes remain outside the local commit. No remote push, publication or deployment.
