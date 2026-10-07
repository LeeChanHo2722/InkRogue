# Development tools verification — 2026-10-06

> Historical local validation record. Angle-bracket paths are portable placeholders, not personal machine paths.

Ponytail exists at `<CODEX_HOME>/skills/ponytail/SKILL.md`. It is a coding instruction skill, not an executable. Its full-mode rules were read and applied during weapon-wheel validation and review: trace actual input/time ownership, reuse the existing InputSystem and SlowMotionController, avoid a new time-service abstraction/dependency, and retain runnable lifecycle checks. It had NOT been read/applied explicitly at the beginning of the earlier work; this omission was acknowledged and corrected.

Graphify was used for code queries and post-change updates in the earlier work through `uvx --from graphifyy graphify ...`. `graphify-final.log` records a successful earlier update. The now-read global AGENTS.md specifies a preinstalled isolated interpreter, which is used from this point onward:

```powershell
& '<GRAPHIFY_PYTHON>' -X utf8 -m graphify query 'PlayerWeaponInputController SlowMotionController RestoreSmooth TryBeginWeaponWheel' --budget 1500
& '<GRAPHIFY_PYTHON>' -X utf8 -m graphify update .
```

Both commands succeeded from the InkRogue project root. Evidence: `wheel-graphify-query.log`, `wheel-graphify-update.log`. The update reported 3,608 nodes, 7,051 edges, 183 communities. Query output identified the existing wheel reset and cinematic time-control methods for source verification. AST extraction is local and needs no API key.

Installed Graphify package reports 0.9.55 while the skill is 0.9.77. This emits a version warning, but query and update work. No package upgrade, external API use, or new installation was performed for this request.

Final post-fix refresh also succeeded: `wheel-graphify-final.log` reports 3,608 nodes, 7,052 edges and 185 communities. Ponytail review kept the correction at the existing time-ownership boundary: save Unity's actual fixed-step read-back value, instead of adding a time manager or changing global timer behavior.

Delivery refresh after documentation and test-file whitespace cleanup also succeeded: `wheel-graphify-delivery.log`, 3,615 nodes, 7,057 edges, 186 communities.

Robot follow-up: Ponytail was applied from the start. Existing target/damage ownership helpers and the combat QA's bounded body-safe navigation pattern were reused. Unity's existing UI/Default shader supplies visible line geometry; no art, package or custom shader was added. Graphify was queried for robot/power/navigation relationships and refreshed after the final movement and test changes; see `robot-graphify-final.log`. Unity CLI and the Unity CLI skill handled execution/build validation.
