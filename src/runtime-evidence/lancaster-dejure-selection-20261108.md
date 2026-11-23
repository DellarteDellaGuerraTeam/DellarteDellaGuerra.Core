# Lancaster, de jure view and persistent selection

## Setup

Saved the user's current Spring 4, 1471 campaign as
`political-map-before-dejure-20261108`, confirmed the save was written, closed the
old debugger session, deployed the four DADG assemblies and title UI assets, and
restarted `Standalone.WarSails`. Reloaded the same save successfully.

Rider session: `9911a820-4a2d-4412-b4a3-7938f5a6786c`.
GABS connected on the configured bridge. Campaign remains paused in game, with
the debugger running and all temporary line breakpoints removed.

## Lancaster regression

Before the fix, live evaluation returned:

- Lancashire: geographic parent `group_7abb388a4f64`, allegiance
  `duchy_lancaster`, broad key `county_lancashire`, excluded from Lancaster.
- Somerset: broad key `duchy_somerset`, incorrectly included in Lancaster.
- Lancaster's owner held `kingdom_england` as his primary title, but the resolver
  used his highest landed dignity `duchy_lancaster` as its fallback instead.

After deployment, live evaluation on the reloaded campaign returned:

```text
county_lancashire broad=duchy_lancaster inLancaster=True
duchy_lancaster broad=duchy_lancaster inLancaster=True
duchy_somerset broad=duchy_somerset inLancaster=False
```

## De jure view and zoom selection

`polmap.view DeJure` succeeded. Rendered geographic labels and gold borders were
visually checked. Selected Coventry through the same drawing method used by map
clicks, then changed both the camera distance and target distance in the debugger.

- At distance 250: displayed `Vassals`; selection retained `county_warwick`.
- At distance 90: displayed `SubVassals`; selection retained `county_warwick`.
- Selected Coventry rendered noticeably darker than adjacent territories.

Screenshots:

- `C:/Users/Joe/Documents/Mount and Blade II Bannerlord/Screenshots/GABS/screenshot_20261108_173300.jpg` — initial de jure view.
- `C:/Users/Joe/Documents/Mount and Blade II Bannerlord/Screenshots/GABS/screenshot_20261108_173612.jpg` — broad zoom.
- `C:/Users/Joe/Documents/Mount and Blade II Bannerlord/Screenshots/GABS/screenshot_20261108_173700.jpg` — detailed zoom, retained darker Coventry selection.

## Automated verification

93 focused ownership, domain, selection, hierarchy and layout tests passed.
43 focused de jure, hover, view-cycle, label-mode and cache tests passed.
Full Release solution build succeeded with zero errors (80 warnings).

De jure tests use throwing live-ownership accessors to verify that geographic
rendering and selection do not depend on faction control or allegiance.
Ctrl+click uses the existing title-opening route; that gesture was not repeated
in this live check.
