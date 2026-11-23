# Warwick name and royal personal estates

Warwick's configured geographical name was already Earldom of Warwick, while its dignityName still combined Warwick and Salisbury. Corrected the dignityName and the research plan's configured_dignity field. The old_name field remains a historical record of the previous configuration.

At broad zoom, SuperiorOwnedTitle promoted minor personal estates to the holder's primary title, even when that primary title was a kingdom. Excluded royal primary titles from this fallback. Royal estates can still group under an actually held subordinate duchy through the existing allegiance/geographic rules; they no longer become one Crown region merely because the king owns them. Direct Crown vassals keep their own regions. Crown-domain membership for explicit selection remains unchanged.

Save loading refreshes configured title names while retaining the saved rank, seat, holder ledger and occupation. This allows the correction to appear in existing campaigns.

- 12 focused title-domain tests passed, including royal estates and Crown vassals at both levels.
- 8 title encyclopedia configuration tests passed.
- Release solution build: 0 errors, 74 warnings (`.tmp/crown-estate-label-build.log`).
- Four deployed DLL hashes verified; the build also copied the revised title configuration.
- Saved campaign before restart: `political-map-before-crown-estate-split-20261108`.
- Reloaded that save in Standalone.WarSails, Rider session `3118b524-569a-4834-8301-18ad063f921c`, Owners / Titles mode.
- Live production domain evaluation over every settlement in the loaded political grid: `Warwick=Earldom of Warwick; broadCrownSettlements=0`. This verifies both the saved-name refresh and removal of the Crown overview fallback in this campaign.
- Temporary evaluation breakpoint removed and debugger resumed; campaign loaded normally.
