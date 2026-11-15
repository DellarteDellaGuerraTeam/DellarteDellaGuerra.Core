# Rudder imports and untracked caches — 15 November 2026

Six native geometry packages and their six matching generated RDC files are committed in both Map repositories:

- v1.4 Map: `776ac282abe401c5bc2952b1cda5c8943bd8305b`
- Primary Map: `7fbd43f4a7d3ec2f8cf913a5a86119a1a33f3b1e`

The six imported metameshes are three `body_no_rudder_v1` bodies and three `rudder_v1` blades. Their TPAC package GUIDs identify the corresponding `RuntimeDataCache/<PACKAGE-GUID>.rdc` filenames; neither the metamesh GUID nor the submesh count determines this pairing. Each TPAC/cache pair was mirrored byte-for-byte, committed through the existing Git LFS rules, and checked against its committed SHA-256 pointer. Unrelated staged files were preserved. Live prefab activation and in-game steering were not performed.

## Why v1.4 Map had 28 untracked caches

| Files | Explanation | Action |
|---|---|---|
| 6 | Newly imported rudder/body package caches | Committed with their six TPACs |
| 22, totaling 103,463,542 bytes (98.67 MiB) | Old ship-texture package caches left after the earlier v1.4 mirror restored the primary texture packages | Diagnosed; retained without modification |

Every remaining Map cache GUID matches one of the **backed-up pre-mirror texture packages**. None matches a current Map package GUID. Each replacement texture TPAC uses a different package GUID and has its own existing, tracked RDC. The mirror replaced the TPACs and copied the required replacement caches, but did not remove the old GUID-named files. Those old caches consequently remained untracked. They are obsolete cache cleanup candidates, not missing rudder dependencies.

There are also two untracked caches in the main `DellarteDellaGuerra` content repository in **each** install. Their bytes match across installs and they belong to current animation packages:

- `76A09545-19DE-4297-B16F-1E728D927744.rdc`: `customanimations/visor/new_animation_clip_anm.tpac`, asset `visor_open_conversation`.
- `816D33B2-EBB6-4A2C-8561-74BD1B6AAA06.rdc`: `customanimations/firearms/reload_firearm_long_new_anm.tpac`, asset `reload_firearm_long_new`.

These animation caches should be kept and reviewed with their animation work; they were excluded from the rudder commits. Scenes and Core have no untracked RuntimeDataCache files in either install. One unrelated main-module TPAC could not be decoded; the two animation owners were decoded successfully, so their attribution does not depend on that file.

## Evidence and reproduction

`untracked-cache-before-commit.json` records the initial 28 files. `untracked-cache-audit.json` records the remaining 22 with exact hashes, timestamps, previous owners and replacement owners, plus the two unrelated animation caches. `rudder-cache-index.json` records the imported resource and package identities. `native-import-commits.json` records both asset commits.

The read-only `tpac-compare` utility's `--package-cache-index <Assets-root> <output.json>` mode records package GUIDs, asset names and any decode failures. Use it on current Map Assets, the backed-up pre-mirror Map Assets and main-module Assets before running `audit_untracked_cache.py`. `--mesh-cache-index` records the six imported rudder/body metameshes. Neither utility writes TPACs or RDCs. `commit_native_imports.py` performs the focused mirror and commit and checks unrelated index entries and Git LFS payload hashes.

No obsolete caches were deleted, and no generated binary was edited by hand.
