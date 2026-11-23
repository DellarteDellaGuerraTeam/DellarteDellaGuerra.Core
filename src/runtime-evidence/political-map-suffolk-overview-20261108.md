# Lincoln joins Suffolk at overview zoom

Removed the Earl-rank exemption from the broad ownership-domain fallback. A non-royal primary title now groups its lower-ranked personal titles, including earldoms, rather than only baronies. Equal-rank independent titles and royal primary titles retain the previous exceptions. The close level still uses the local title.

The focused regression models Suffolk, Lincoln and Lincoln's configured allegiance to an independently controlled York. Lincoln resolves to Suffolk in the overview, remains Lincoln locally, and does not become York's property.

Updated the existing secondary-earldom overview expectations to the requested aggregation. Specific secondary title selection still follows its own estates and attached vassals. Also corrected a zoom paint fixture that still used the old wash-band threshold instead of OwnerOverviewAbove.

- 309 focused political-map domain tests passed.
- Build log: `.tmp/suffolk-overview-build.log`.
- Saved campaign before restart: `political-map-before-suffolk-overview-20261108`.
- Release build completed with 0 errors and 80 warnings; four deployed DLL hashes verified.
- Reloaded the saved campaign under Standalone.WarSails in Rider session `bafba21e-a5b4-49d1-a0d0-99240fe7a7b2`, Owners / Titles mode.
- Live production evaluation: `Lincoln overview=county_suffolk (Duchy of Suffolk); local=county_lincoln (City of Lincoln); SuffolkContainsLincoln=True`.
- Set the real campaign camera to maximum zoom-out (620), removed the temporary evaluation breakpoint, and resumed execution.
