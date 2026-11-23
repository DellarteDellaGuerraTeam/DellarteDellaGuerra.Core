# Measured label collisions and same-patch placement

At maximum zoom-out in the live campaign, Devon and Arundel were valid Earl-ranked labels rejected by collisions, while Pembroke and Essex were selected. Essex's preferred component was a disconnected holding.

Replaced longest-line character-count rectangles with native mapped-font glyph advances per wrapped line. Measurements match native Text's 0.5 glyph padding and (Font.Base + 5) baseline spacing; the collision check retains a margin for outline/glow. This alone improved the label count but did not clear Devon and Arundel's real collisions. Trying the next smaller Earl font in the debugger also did not resolve them.

Added bounded alternative label centres reached through the same region. Candidate points lie 0.6, 1.2 or 1.8 inscribed radii away in eight directions, admitted only when the sampled path remains in the same region key. The preferred drawn position is retained between frames, avoiding a return to the original centre solely because space reappears. No title IDs are special-cased.

Collision layout still excludes camera translation and viewport clipping. Nearby duplicate names still reserve their preferred holding even if crowded.

- 313 focused political-map domain tests passed, including measured widths, wrapped-line gaps, same-patch placement and water-separated holdings.
- Corrected collision-space Y to increase downward, matching the rendered order of asymmetric wrapped lines. Whole-map coordinates still exclude camera translation.
- Overview labels use one smaller font step, preserving their rank hierarchy. Measured bounds and alternative centres alone did not clear the two reported earldoms; a live candidate trial with the compact overview sizes admitted both.
- Serial Release solution build: 0 errors, 80 warnings (`.tmp/measured-label-build.log`). A prior parallel build encountered concurrent writes to ExpandedTemplate's shared manifest; the serial build passed.
- Four deployed DLL hashes verified.
- Saved campaign: `political-map-before-measured-label-bounds-20261109`.
- Final real-game verification: Standalone.WarSails session `fabb3d73-5450-43e6-924a-91d26331af01`, PID 49324, Owners / Titles at distance 620.
- Production result: `level=Vassals; distance=620; eligible=78; failed=False; county_devon=True; county_arundel=True; county_essex=True; county_pembroke=True; panSamples=50; labelSetChanges=0`.
- Pan check translated the native camera through 25 horizontal and 25 vertical positions and called production Layout. Camera frame restored afterward. This is a debugger-driven layout check, not replayed mouse input.
- Screenshot: `C:/Users/Joe/Documents/Mount and Blade II Bannerlord/Screenshots/GABS/screenshot_20261109_013021.jpg`.
- Evaluation breakpoint removed; debugger resumed.
