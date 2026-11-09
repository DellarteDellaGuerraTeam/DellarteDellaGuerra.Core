# Stable label patches while zooming

## Runtime diagnosis

The real WarSails campaign was inspected at the safe evaluation breakpoint in Rider session `1a74a029-0874-428e-9de5-4902a69b0fcb`.

At camera distance 330.8543, the overview contained four Shrewsbury components (2080, 1790, 952 and 688 cells) and two Nottingham components (1074 and 577 cells). The eligible labels were on the 952-cell Shrewsbury patch at (643.5, 314.5) and the 577-cell Nottingham patch at (765.5, 353.5), rather than their largest patches at (661.5, 342.5) and (732.5, 359.5). Nottingham's component geometry was identical in both zoom levels. Thus this was not merely re-centering a title after splitting its vassals.

The layout suppressed repeated names only against labels successfully placed. A higher-priority name could crowd out the preferred component and allow a smaller nearby component to take over as zoom changed the collision boxes.

## Change

Reserve a group's preferred patch before testing collisions against other titles. Nearby duplicate patches remain suppressed even if the preferred label is crowded. Distant holdings can still acquire another copy once their projected separation is sufficient. A crowded preferred label may temporarily disappear; it does not migrate to a nearby holding.

The existing whole-map layout remains in place, preserving pan stability. Font and wrap hysteresis and the 250/260 ownership-detail transition remain unchanged.

## Checks

- 82 focused label tests passed, including crowded preferred patches, distant copies, and previous pan/edge regressions.
- Release solution build log: `.tmp/stable-patch-label-build.log`.
- Campaign saved before restart as `political-map-before-stable-patch-labels-20261108`.
- Release build completed with 0 errors and 83 warnings. Four deployed DLL hashes verified.
- Relaunched Standalone.WarSails in Rider session `f9546db4-f7cf-4b6b-bebf-c29b303621e7`; reloaded the saved campaign, Owners / Titles enabled.
- Live production Layout sampled at 27 camera distances (270 through 400 in steps of 5), with the overview region set active. Both previously selected small-patch candidates were present. Result: `level=Vassals; zoomSamples=27; checkedFallbackCandidates=2; previousSmallPatchFallbacks=0; failed=False`.
- The same sweep also passed with the detailed region set. This is a debugger-driven layout check, not replay of recorded mouse input.
- Temporary evaluation breakpoints removed and execution resumed. Campaign remains paused normally.
