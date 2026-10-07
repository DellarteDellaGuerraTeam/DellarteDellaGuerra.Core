# Save/load availability revision — 2026-10-07

Approved direction: brighter navy campaign headings, neutral slate enabled rows,
dark charcoal-blue unavailable rows, muted unavailable text, and a six-unit banner
top inset. Selection retains the blue/gold outline. Native loadability rules,
corruption warnings, commands, dimensions and heraldry are preserved.

Build sources with `saveload-availability.json` using `build_shared_surfaces.py`,
then deploy that batch with `deploy_shared_surfaces.ps1`. Reimport
`ui_saveload_1_tex` through the official editor. Brush XML is deployed alongside
the atlas sources; the banner/text attribute patches require a rebuilt Core DLL
and a restarted session. No compiled TPAC or cache is written by these tools.

The existing HTML preview is a visual approximation, not runtime validation.

Revision 02: medium-navy header palette (#244761 -> #3b647f), unavailable-row
brightness 0.80 instead of 0.42 and saturation -30 instead of -65. Muted text
continues to identify unavailable saves without near-black backgrounds.
At this revision the installed TPAC still predates the revision-01 source PNG;
source deployment alone does not activate atlas colours. Reimport is required.

Revision 03 supersedes the text treatment above: user removed the text patches.
Keep that removal intact and omit DADGSaveLoadText.xml from batch deployment.
Header palette now #1e3c53 -> #32566e (roughly 15% darker than revision 02).
Unavailable backgrounds use saturation -60 and brightness 0.62, bringing back
the charcoal reference tone without the original 0.42 heavy dimming. Enabled
and selected source pixels, typography and C# are unchanged by this revision.

## Read-only recruitment audit

`RecruitmentFrameSkin` alone can be replaced by a native-name `Recruitment.Frame`
brush override. PNG-only is not an exact replacement: the native brush also adds
a separate stone-overlay frame layer.

The full recruitment skin contains eleven patches. Artwork can largely move to
native atlas rectangles, but these behaviors are not pixels:

- Direct prefab saturation on the left/right panels must be neutralized.
- Right-panel margin 75 -> 70 is the approved alignment correction.
- Title font colour belongs in brush XML.
- Recruitment-specific highlight redirects remove shared yellow/orange runtime
  tints while retaining hover/pressed states.

Shared-sprite hazards: `tabbar_popup` also serves encyclopedia/army/town panels;
`portrait_patron` is used by MapBar; `portrait_cart` by crafting history.
Repainting them globally is not a recruitment-only refactor.

Recommended future work: PNG artwork + a few native-name brush overrides, keeping
small targeted saturation/alignment/highlight patches. No recruitment code or
assets were changed for this audit.
