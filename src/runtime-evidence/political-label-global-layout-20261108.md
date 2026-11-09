# Stable political labels while panning

Supersedes the viewport-anchor workaround in political-label-viewport-20261108.md. Font and wrapping hysteresis alone did not address this issue.

## Cause and change

Viewport culling occurred before collision selection. As a higher title crossed the screen edge, it stopped suppressing nearby lesser labels. Panning therefore changed the selected title names without any zoom change.

Collision selection now runs over the whole map in zoom-scaled coordinates that exclude camera translation. Offscreen labels still participate in collision and repeated-name suppression. The chosen labels are then projected for display. Panning can clip a label, but cannot promote a lesser label simply because a higher label crossed the viewport edge.

## Verification

- 80 focused domain tests passed, including edge-crossing and disconnected-patch regression cases (`.tmp/label-global-tests.log`).
- Release solution build completed with 0 errors and 82 warnings (`.tmp/label-global-build.log`).
- Four deployed mod DLL hashes verified against the build outputs.
- Real Bannerlord executable: `D:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/bin/Win64_Shipping_Client/Bannerlord.exe`, PID 32496.
- Rider profile: Standalone.WarSails; session `4e521c07-820e-45f0-8d7f-018698c0f342`.
- Loaded campaign: `political-map-before-global-label-layout-20261108`; Owners / Titles mode.
- Live debugger evaluation called the production Layout method at 50 camera translations: 25 horizontal and 25 vertical positions, offsets -480 to +480 world units, fixed camera distance 620. Result: `panPositions=50; eligible=52; labelSetChanges=0; failed=False; cameraDistance=620`.
- Camera frame restored after evaluation. Temporary evaluation breakpoint removed; debugger confirmed running.

This verifies the complete chosen label set under live camera translations, rather than replaying the user's recorded mouse inputs. The integration test suite was not run.
