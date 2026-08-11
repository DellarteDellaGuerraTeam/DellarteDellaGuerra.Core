# Diagnostic Patterns

Use these as patterns, not universal constants. Re-measure the current FBX and XML before editing.

## Centered handle reach

For Pollaxe A, a centered 217-unit handle at 80% scale originally used `piece_offset="22.9"`:

```text
before shaft reach = (217 / 2 + 22.9) * 0.80 = 105.12
after shaft reach  = (108.5 + 108.5) * 0.80 = 173.60
```

With its head, reach changed from about 132.12 to 200.60, matching the roughly 199.19-unit visible weapon. The seam did not need to own the reach correction; the handle pivot did.

## Transfer pair-specific corrections off shared connectors

The Rigsby axe handle carried `next_piece_offset="-8.22"`, pushing every alternate head away and creating gaps. Remove it from the shared handle and put `previous_piece_offset="-8.22"` on the Rigsby head. This preserves the Rigsby socket convention while making every head use the same handle connector.

The same pattern applies to swords: a blade-specific offset on a guard moves every blade. Glasgow and Gaelic compatibility required splitting guard/blade corrections so ordinary blades and guards no longer inherited the authored pair's private adjustment.

## Correct a protruding shaft

For `dadg_axe_craft_37_head` at 100%:

```text
handle max Y = 56.199997
head max Y   = 54.840550
protrusion   = 1.359447
```

Adding `previous_piece_offset="-1.36"` to the head moved it outward until the shaft tip was flush with the top surface while retaining the full axe-eye socket.

If the same protrusion changes with the handle but not the head, normalize the handle's `next_piece_offset` instead.

## Visible shoulder gap masked by tall guard tips

An earlier revision of this file blamed the Scottish guard/blade gap on a blade tang entering the guard,
and recommended `next_piece_offset="2.0"` on the guard. Both were wrong. Recorded here in full because
each wrong step was independently plausible.

The bounds reported a `-1.52` overlap sitting on the mod median while the blade visibly floated. The
cause was not a tang: rendering the blade alone from the `--all-pairs --out` gallery showed a
flat base with no tang at all. The guard's bulbous quillon *tips* (`max_y 11.321`) are taller than the
blade base (`9.802`), so whole-mesh bounds compared tip against blade and reported overlap while the
blade floated `1.23` above the guard's centre.

The real defect was one thing on one piece: the blade mesh was authored `5.353` off-origin, and its
`previous_piece_offset="-7.82"` was over-compensating. Recentring the mesh origin and setting
`previous_piece_offset="0.98"` fixed it. The guard's authored `next_piece_offset="4"` had been very
nearly right the whole time — the correct value was `3.84`.

Two traps, both of which passed their apparent checks:

- Cutting the guard to `2.0` seated the blade on the guard's raised central boss. That boss is a
  decorative detail on the guard's *front face*, rendering over the blade in depth, not a shoulder. The
  blade's real support plane is lower, where the quillon arms meet. Contact went from 0% of the blade's
  base width to only 15%, with a `1.75` median gap still visible under the rest.
- Measuring the joint at the blade's centre column alone reported flush at both `2.0` and `3.84`.

Only the per-column contact profile separated them: `0% / 3.28` before, `15% / 1.75` after the boss
mistake, `100% / 0.13` correct, against a `88-100% / 0.11-0.15` band from healthy DADG swords.

Fixing the origin also corrected reach, which the offset-only fix left wrong: declared `107.119`
against a visible blade tip at `101.779`, becoming `100.021` against `100.034`.

## Preserve intentional long overlaps

Halberd langets, the Jedburgh metal extension, pike sleeves, knuckle bows, and similar geometry can overlap a shaft by tens of units. Do not normalize these to zero. Verify the authored assembly and ensure the overlap is consistent across compatible handles.

For an off-centre socketed mesh, measure the physical Y bounds and authored connector position. Use `previous_piece_offset` or `next_piece_offset` only for the seam; keep the handle `piece_offset` responsible for grip-relative reach.

## Know when offsets cannot work

The training-stick blade, guard, and hilt meshes each span large portions of the complete stick and intentionally overlap by roughly 60-124 units. They cannot become normal sword components through Y offsets. Keep them hidden, move them to a dedicated template, remove them from the mixed pool, or split/re-author the meshes.

Likewise, X/Z drift and rotation require an FBX correction. BuildData connector offsets are axial and cannot express them.

A purely axial bad origin is the one case that offsets *can* hide: the visible joint can be made perfect
with `previous_piece_offset` alone. Prefer the FBX correction anyway, because the offset leaves
`weapon_length` computed from the wrong position and the piece stays an outlier that every future
cross-pair has to work around.
