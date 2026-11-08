# Devon ownership-domain debugger check

Rider `Standalone.WarSails`, session `83374609-fcd0-4c3a-9722-4125ca1a3a88`.
Paused at `CompilingShaderNotifier.cs:37`; inspected live title services and
settlement ownership. Removed the breakpoint and resumed before editing.

Observed in the running campaign:

| Settlement | Actual owner | Broad title | In Devon selection |
|---|---|---|---|
| Exeter | Courtenay | county_devon | yes |
| Okehampton | Courtenay | barony_okehampton | no |
| St Michael's Mount | Courtenay | barony_saint_michaels_mount | no |
| Plymouth | Blount, under Courtenay | county_devon | yes |
| Taunton | Harrington, under Courtenay | county_devon | yes |
| Dunster | Herbert, directly under York | barony_dunster | no |
| Totnes | Basset, under Lancaster | barony_totnes | no |

Courtenay's highest landed title was `county_devon`. The resolver incorrectly
kept his disconnected minor personal estates separate at broad zoom. The fix
folds these into the main landed title while retaining local identities at
detailed zoom and preserving separate earldoms or higher dignities.

Two Devon label candidates existed: a 3,611-cell patch and a 925-cell patch.
The larger was selected by layout, with the other suppressed as a nearby repeat.
This confirms that label location also depended on the incorrectly split domains;
it does not prove that font/layout changes alone would fix the map.

The blue colour indicates York's kingdom, not common ownership or vassalage.
Dunster cannot be assigned to Devon merely from its colour; Herbert's live
allegiance needs a separate decision if that relationship is meant to change.

Regression covers disconnected personal baronies, separate secondary earldoms,
both zoom levels and membership. Updated runtime visuals remain unverified until
the corrected assembly is deployed and the game restarted.
