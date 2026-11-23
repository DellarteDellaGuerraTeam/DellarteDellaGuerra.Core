# Ownership changes: live balance assessment

Read-only audit on 2026-11-03 of the running Spring 4, 1471 campaign. Two subagents examined ownership changes and native mechanics. Rider inspection matched the user's war-screen strength values. No balance, ownership, treasury, troop or campaign-time changes were made. The temporary breakpoint was removed and execution resumed.

## Verdict

The opening position is asymmetric, not proven balanced. The war screen's roughly 10% York advantage hides a roughly 54% advantage in mobile troop headcount; Lancaster has more garrison troops and towns. This snapshot is not a controlled before/after campaign experiment and cannot establish that the ownership changes caused the starting mobile-force gap.

| Metric | York | Lancaster |
| --- | ---: | ---: |
| Displayed total strength (combat-weighted, cached) | 15,344 | 13,914 |
| Active lord-party troops | 6,747 | 4,372 |
| Active lord parties | 51 | 32 |
| Garrison troops | 8,101 | 9,345 |
| Militia (excluded from displayed strength) | about 9,125 | about 10,142 |
| Towns | 11 | 17 |
| Castles | 38 | 30 |
| Unique bound villages | 140 | 150 |
| Total clan gold | 3,095,077 | 2,084,897 |
| Kingdom budget wallet | 2,000,000 | 2,000,000 |
| Modeled income/day at this snapshot | 129,645 | 183,360 |
| Modeled expenses/day | 42,020 | 39,845 |

York's party and troop counts include the player's one-person party. Excluding it gives 50 AI parties and 6,746 troops. Income values are model projections, not observed daily receipts; public finance methods are overridden by NavalDLCClanFinanceModel, whose implementation was not independently verified. Recomputed weighted mobile strength was about 7,485 versus 4,834, consistent with the headcount disparity. Recomputed components differ slightly from the cached total shown in the UI.

## Effect attributable to the ownership edits

See [static ownership comparison](ownership-balance-static.md) for the complete before/after tables. The English faction-wide net change is small: Lancaster gains one castle and three bound villages; town counts remain 17 versus 11. Most changes redistribute resources between clans within the same faction.

The larger effect is concentration. Henry's clan moves from 7 to 12 forts, and Henry plus Warwick from 12/46 to 20/47 Lancastrian forts (26% to 43%). Henry retains three towns. All ordinary English AI clans inspected are tier 4 with a two-party limit. More castles do not increase that limit; Henry currently fields only one party, Warwick two. This can concentrate defensive strength and money without creating additional independent field commands.

Six English clans become newly landless: Clinton, Willoughby, Dynham, Scrope of Masham, Stourton and Tuchet. Erskine and Hepburn also become landless in Scotland. All eight still had active parties during inspection.

The six English households have about 89,000–95,000 gold, current modeled income zero, and expenses of roughly 498–657/day. That is a risk to track, not a prediction of bankruptcy. Verified base mechanics supply poor kingdom clans below 30,000 gold and double support when the kingdom budget exceeds one million; both budgets currently meet that condition. The active DLC override prevents treating base thresholds as fully verified runtime behavior.

Native mechanics also reduce party-replacement scores and resistance to defection for landless clans. They do not automatically destroy a landless clan that remains in a kingdom. Starting cash can conceal replacement pressure until parties suffer losses.

## Separate data-cache finding

Clan.Villages and Clan.Settlements contain duplicate references to some village objects: Henry has 56 village entries but 41 unique villages; Edward has 36 entries but 25 unique. Settlement.All and each fief's Villages collection give the correct unique counts matching XML. The duplicates include unchanged Henry fiefs, so they cannot be attributed solely to these ownership transfers.

Verified base normal settlement income iterates Fiefs[].Villages, so these duplicates alone do not demonstrate doubled ordinary tax income. The ruling LandTax policy uses a different kingdom-village collection and warrants a separate check if active. No cache or economy fix was made in this analysis.

## Recommendation and useful verification

Keep historically reviewed ownership while assessing army behavior. Do not redistribute historical estates merely to make the war-screen totals equal, or multiply ruler party limits by castles.

If balance work follows, prioritize patronage or retainer funding and viable party replacement for displaced households. Existing kingdom support should be measured first; an additional funding system may be unnecessary.

A deterministic integration check should validate unique settlement/village membership, ownership/title consistency and intentional landless-clan exceptions. It cannot establish campaign balance. That needs repeatable fresh campaigns, with checkpoints around days 0, 30, 90 and 180: mobile combat strength, field-party count and size, post-defeat replacement, cash and subsidy flows, captures and defections. Compare baseline and changed data over multiple runs before setting numerical tuning targets. No such time-advancing simulation was run in the user's current campaign.

## Evidence

- balance-runtime-clans.txt: per-clan force, finance and holdings snapshot; village counts there are raw cache-entry counts and must not be interpreted as unique villages.
- balance-runtime-strength.txt: recomputed mobile and garrison combat strength.
- balance-runtime-bindings.txt: duplicated IDs, kingdom budgets and unique registry totals.
- balance-runtime-village-collections.txt: Clan.Villages versus unique and fief-bound counts.
- ownership-balance-static.md: XML before/after ownership comparison.
