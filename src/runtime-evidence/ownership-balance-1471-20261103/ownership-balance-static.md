# Static ownership balance audit

Source: before-fix settlement backup versus current installed Map XML, joined to current clan `super_faction` and kingdom XML. These are starting-data counts; loaded campaign ownership may differ. Prosperity and hearth totals are XML proxies, not actual taxes, wage budgets, recruit supply, party strength or projected victory odds.

## Faction distribution

| Faction | Towns before → after | Castles | Bound villages | Landed clans | XML prosperity | XML hearths |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| House of York | 11 → 11 | 39 → 38 | 143 → 140 | 32 → 28 | 84114 → 86730 | 53027 → 51787 |
| House of Lancaster | 17 → 17 | 29 → 30 | 147 → 150 | 21 → 19 | 115819 → 113203 | 59306 → 60546 |
| Crown of Scotland | 13 → 13 | 14 → 13 | 81 → 79 | 14 → 13 | 50075 → 49625 | 23070 → 22550 |
| Lordship of the Isles | 4 → 4 | 12 → 13 | 54 → 56 | 13 → 13 | 14075 → 14525 | 12020 → 12540 |

## Newly landless households

| Clan | Initial faction | Holdings lost |
| --- | --- | --- |
| Clinton | House of Lancaster | dadg_Kenilworth_castle |
| Dynham | House of York | dadg_Dunster_castle |
| Erskine | Crown of Scotland | dadg_sco_kildrummy_castle |
| Hepburn | Crown of Scotland | dadg_sco_dunbar_castle |
| Scrope of Masham | House of York | dadg_Helmsley_castle |
| Stourton | House of York | dadg_Corfe_castle |
| Tuchet | House of York | dadg_Stafford_castle |
| Willoughby | House of Lancaster | dadg_Tattershall_castle, dadg_Donnington_castle |

## Ruler concentration

| Ruler clan | Towns | Castles | Bound villages | XML prosperity | XML hearths |
| --- | ---: | ---: | ---: | ---: | ---: |
| Lancaster | 3 → 3 | 4 → 9 | 28 → 41 | 25896 → 25463 | 11261 → 15551 |
| York | 2 → 2 | 4 → 6 | 19 → 25 | 14457 → 15896 | 7694 → 9846 |
| Stewart | 2 → 4 | 2 → 4 | 13 → 22 | 9275 → 14800 | 3710 → 6010 |
| MacDonald of the Isles | 1 → 2 | 2 → 3 | 8 → 14 | 2775 → 5325 | 1700 → 3240 |

## Cross-faction settlement transfers

| Settlement | Previous faction | New faction |
| --- | --- | --- |
| Launceston_town | House of York | House of Lancaster |
| Exeter_town | House of Lancaster | House of York |
| dadg_Corfe_castle | House of York | House of Lancaster |
| dadg_Farleigh_castle | House of Lancaster | House of York |
| dadg_Skipton_castle | House of Lancaster | House of York |
| dadg_Pontefract_castle | House of York | House of Lancaster |
| dadg_Barnard_castle | House of York | House of Lancaster |
| dadg_Richmond_castle | House of Lancaster | House of York |
| dadg_Dunstanburgh_castle | House of York | House of Lancaster |
| dadg_sco_urquhart_castle | Crown of Scotland | Lordship of the Isles |

## Concentration by English clan

| Clan | Faction | Towns | Castles | Villages | XML prosperity | XML hearths |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Lancaster | House of Lancaster | 3 → 3 | 4 → 9 | 28 → 41 | 25896 → 25463 | 11261 → 15551 |
| Neville of Middleham | House of Lancaster | 1 → 2 | 4 → 6 | 14 → 24 | 9941 → 17838 | 6225 → 10232 |
| York | House of York | 2 → 2 | 4 → 6 | 19 → 25 | 14457 → 15896 | 7694 → 9846 |
| Bourchier | House of Lancaster | 1 → 1 | 2 → 2 | 9 → 9 | 5990 → 6118 | 4241 → 4355 |
| Courtenay | House of York | 1 → 1 | 2 → 2 | 11 → 9 | 4063 → 7740 | 4094 → 3328 |
| de la Pole | House of Lancaster | 2 → 2 | 0 → 1 | 8 → 11 | 10250 → 11124 | 3346 → 4719 |
| Herbert | House of York | 0 → 0 | 2 → 3 | 6 → 9 | 1416 → 1867 | 2037 → 3233 |
| Stafford | House of York | 1 → 1 | 1 → 2 | 6 → 8 | 6607 → 7596 | 2133 → 2958 |
| Tudor | House of Lancaster | 2 → 2 | 2 → 1 | 14 → 11 | 7971 → 7538 | 5026 → 3970 |
| Beaufort | House of Lancaster | 1 → 1 | 1 → 1 | 8 → 8 | 7552 → 7551 | 3119 → 3205 |
| Stanley | House of York | 0 → 0 | 1 → 2 | 3 → 6 | 562 → 962 | 965 → 1999 |
| Clifford | House of Lancaster | 1 → 1 | 1 → 0 | 6 → 3 | 4528 → 4128 | 2601 → 1567 |
| Harrington | House of York | 0 → 0 | 2 → 1 | 6 → 3 | 1252 → 702 | 2076 → 1121 |
| Holland | House of Lancaster | 1 → 0 | 2 → 1 | 9 → 3 | 7733 → 715 | 3867 → 1486 |
| Neville of Raby | House of Lancaster | 1 → 1 | 1 → 0 | 6 → 4 | 7455 → 6862 | 2415 → 1660 |
| Percy | House of York | 1 → 1 | 1 → 0 | 5 → 3 | 3568 → 2795 | 1405 → 795 |
| Clinton | House of Lancaster | 0 → 0 | 1 → 0 | 3 → 0 | 1017 → 0 | 913 → 0 |
| Dynham | House of York | 0 → 0 | 1 → 0 | 3 → 0 | 451 → 0 | 1196 → 0 |
| Scrope of Masham | House of York | 0 → 0 | 1 → 0 | 2 → 0 | 1056 → 0 | 938 → 0 |
| Stourton | House of York | 0 → 0 | 1 → 0 | 3 → 0 | 521 → 0 | 1157 → 0 |
| Tuchet | House of York | 0 → 0 | 1 → 0 | 2 → 0 | 989 → 0 | 825 → 0 |
| Willoughby | House of Lancaster | 0 → 0 | 2 → 0 | 6 → 0 | 1620 → 0 | 2491 → 0 |

## Interpretation limits

- The supplied screenshot is York 15,344 versus Lancaster 13,914: York is 10.28% stronger by that displayed metric, with a 52.44% share of the two-side total. This is reasonably close initially, not proof of sustained balance.
- English faction assets moved only slightly: Lancaster gains one net castle and three bound villages; town totals are unchanged. Lancaster has more towns (17 versus 11), while York has more castles (38 versus 30). These are different resource mixes, not equivalent fief counts.
- Concentration changes much more: Henry plus Warwick rise from 12/46 of Lancastrian forts (26.1%) to 20/47 (42.6%). Henry alone moves from 7/46 (15.2%) to 12/47 (25.5%). Individual clan solvency and party rebuilding therefore deserve more attention than the small faction-wide land shift.
- Henry gains no net town: Coventry is replaced by Launceston. His XML prosperity sum falls 1.7%, while bound-village hearth sum rises 38.1%; a five-fief gain is not a simple five-fief-sized tax gain.
- Initial XML has not changed troop types, clan tiers or clan faction memberships in these ownership transfers; runtime money, parties and garrisons need separate inspection.
- Six English clans and two Scottish clans lose every town/castle. The actual party upkeep and regeneration consequences require runtime/finance-model evidence.
- Do not interpret all royal estates as a personal historical demesne: current Bannerlord clan ownership makes the estates mechanically concentrated even when the historical institution differed.
- No production files were modified, and no tests or runtime simulation were run by this static audit.

## Read-only runtime reconciliation

A separate live debugger snapshot at Spring 4, 1471 confirmed the screenshot strengths (York 15,344.29; Lancaster 13,914.11), all English town/castle counts, and the unique village totals above: York 140 and Lancaster 150. The runtime `Settlement.All` registry agrees with the XML-derived totals.

`Clan.Settlements` contains duplicate village references in that snapshot. Counting its entries produces 199 York and 196 Lancaster villages; Henry has 56 entries for 41 unique villages, Warwick 33 for 24, and Holland 5 for 3. These are collection duplicates, not additional holdings or incomplete XML binding traversal. Henry's duplicates include unchanged fiefs such as Peveril, Berkhamsted, Carmarthen and Harlech, so the discrepancy is a separate confound and cannot be attributed solely to the recent ownership transfers. Use unique settlement IDs from `Settlement.All` when auditing asset counts; any effect of these duplicate caches on native economic calculations requires separate investigation. No production data was changed for this reconciliation.
