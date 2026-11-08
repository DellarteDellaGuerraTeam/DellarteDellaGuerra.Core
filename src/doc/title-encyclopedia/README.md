# Title encyclopedia

## First in-game version

The Core module now provides a native encyclopedia page under **Titles and
concepts**. In the political map's title-label mode, **Ctrl+click** opens the
title selected at the current zoom level. Ordinary clicks retain map selection.
Clan and kingdom modes open their respective native encyclopedia entries.

The page shows the territorial description first, followed by the recognised
holder, claimant portraits, possession, descendant towns and castles, and the
record of succession and transfers. Villages are excluded from the visible list.
The articles use native hero and settlement image providers. Ordinary concepts
remain available through the same index.

`tools/build_title_encyclopedia_runtime.py` generates the shipped title names,
territorial hierarchy, article XML and native Gauntlet prefab. Existing holders,
claims and personal allegiances are retained. The thirteen added superior groups
have no invented holders. Their pages report that no holder is recorded.

Validation: the Release solution compiles; 21 targeted territory/selection tests
and six generated-content/layout tests pass. Ctrl+click opened the matching
Coventry title in a live campaign. That check exposed horizontal section stacking;
the prefab now explicitly stacks both article columns vertically, with a regression
check. The corrected rendering still requires visual confirmation after restart.
No new Harmony patch or reflection registration is
used for the encyclopedia; Ctrl+click extends the existing map selection hook.

## Browser maquette

Open `index.html` in a normal web browser. It is self-contained, works offline,
and does not change the game. `template.html` is its editable source.

For the app's HTTP preview, run the following from `src`, then open
`http://127.0.0.1:18771/` in its Browser panel:

```powershell
rtk proxy C:/Users/Joe/.pyenv/pyenv-win/versions/3.12.1/python.exe tools/serve_title_maquette.py
```

This loopback-only server exposes the single generated HTML page, not the rest
of the workspace. Stop it with Ctrl+C when finished.

The design previews the researched territorial names for all 139 towns/castles,
with an index of 155 local, superior and realm titles. Search by title, settlement,
holder or house. The existing spring-1471 XML supplies starting holders,
possessors and seeded claims; this is not a debugger snapshot of a running save.
`title-renames.csv` provides the complete before/after naming and parent proposal.

## Shipping scope

- Keep towns, castles and villages as their existing Bannerlord components.
  “Estate” describes a territorial title, not a new settlement type or economy.
- Keep current household proxies and ordinary vassal behaviour for church land.
  A future church view may represent offices and spiritual jurisdictions
  independently of the secular territory view.
- Give every landed title a gameplay parent. Historical uncertainty must not
  leave a hole in the playable hierarchy or be relabelled as proven tenure.
- Keep territorial identity separate from the holder's other dignities.
  Hermitage/Liddesdale is not geographically Angus because its holder is Earl
  of Angus. Bristol's civic county status does not create an Earl of Bristol.

## Assignment proposal

`secular-title-plan.json` contains explicit parents and assignment reasons:

- 36 researched documented links used as proposed secular parents.
- 36 researched provisional links used as proposed secular parents.
- 54 unresolved links attached to their existing realm root as gameplay fallbacks.
- 12 church links attached to their existing realm root pending church integration.
- 1 cross-realm link held at its existing realm root pending review.

These counts concern the 139 landed nodes. Thirteen extra, seatless superior
groups represent named titles from the research, alongside three realm roots.
Their own realm attachments are explicitly scaffolding, not verified immediate
tenure. Group holders have not been invented. All 425 village bindings remain
gameplay bindings; the earlier research exceptions remain available separately.

The plan does **not** automatically alter noble-house allegiance. That system
already distinguishes territorial parent, dignity rank and personal allegiance.
The first in-game version activates these geographic assignments while retaining
existing personal allegiances. The superior groups still need suitable holders
where historically justified; campaign effects need live verification.
Simply renaming an existing county would falsely apply a local castle or burgh
name to all the unrelated estates currently nested under it.

## Try the maquette

The layout follows SandBox's `EncyclopediaSettlementPage.xml`: a narrow title
identity panel on the left and a scrolling article on the right. It reuses the
installed encyclopedia canvas and navigation textures, with the description at
the top and native-sized portrait rows for holders, claimants and settlements.
Search opens a separate title index; Back and Forward navigate visited titles.
Other native categories are disabled in this title-only maquette.

The opening description is player-facing, static territorial prose, separate
from the current holder, occupation and research notes. It contains no XML,
configuration or proposal commentary. Key examples have short historical
introductions drawn from the existing research; other entries currently have
concise geographic introductions and, where available, a jurisdiction glossary.
Full individual historical narratives remain an editorial follow-up. Research
uncertainty and implementation notes remain in the source research data. The
page's historical record uses player-facing prose and source links, rather than
displaying the internal research commentary.

Character portraits are silhouette placeholders, not historical likenesses.
Settlement portraits reuse native town/castle/village art by type. The live page
must use each character's portrait provider and the same `SettlementImageID` /
settlement image provider as the native encyclopedia, including its individual
settlement images. Claim data is currently clan-based: the maquette shows its
leader as the representative; live individual claims must show the claimant.
Settlement lists include all descendant titles, not the holder's unrelated lands.
Only towns and castles appear in these lists and their counts; village supply
bindings remain in the data but are not displayed in the encyclopedia.

1. **Hermitage:** its proposed local estate, Liddesdale parent, and the holder's
   separate Angus dignity.
2. **Bristol:** its civic county name and direct Crown jurisdiction, without a
   peerage earldom or the unrelated baronies formerly grouped beneath it.
3. **Pembroke → Claimants:** the configured Tudor holder and Herbert claim. “No
   seeded claims” on another page does not mean no possible blood claims.
4. **Scenario → Illustrative occupation:** shows another house possessing the
   seat while the recognised holder remains unchanged. This is labelled fiction
   for the interaction study, not asserted 1471 history.
5. **Historical record:** sources, historical superior, uncertainty, gameplay
   assignment and previous label.
6. **Map interaction:** at far zoom, select any of the three disconnected
   Lancastrian estates to darken all three; another selection restores the old
   colours. Ctrl+click opens the superior title. Near zoom selects the local
   estate instead. The map shapes are schematic, not historical polygons.

## Eventual in-game entry point

- Plain click retains political-map selection at the displayed zoom level.
- Ctrl+click resolves the **same displayed selection key** and opens its title
  encyclopedia page. Do not resolve it again from the nearest settlement or the
  holder's greatest dignity.
- In clan-label mode, Ctrl+click opens the clan encyclopedia; in kingdom mode,
  the kingdom page. This avoids opening an arbitrary title for a multi-title clan.
- Suppress opening through modal UI or during map dragging, and consume the
  handled click so it does not also issue a party movement order.
- A seatless superior title still gets a page. Its possessions are summarised
  from subordinate settlements, with multiple occupying clans shown separately.

These interactions are implemented by the first in-game version described above.

## Live data contract

The current code already supplies most dynamic information:

| Display | Existing source |
|---|---|
| Territorial name, seat and superior | `IFeudalStructure` / `FeudalTitleNode` |
| Recognised holder | `Title.HolderHeroId`, with clan derived via genealogy |
| Seat occupation | `Title.OccupantClanId` plus actual settlement ownership |
| Full territorial possession | All de jure settlements, grouped by actual owner |
| Claimant, strength and origin | Claim repository, including generated blood claims |
| Succession and transfers | `Title.Holders` ledger |
| Historical text and citations | Static research keyed by stable title ID |

Show the ledger holder, not the occupying clan's leader, as the titleholder.
Refresh dynamic fields when opening a page or after relevant state changes.
Succession dates must use the title ledger; the configured house tenure date
must not be mislabelled as a particular person's accession date.

## Rebuild and checks

### Geographic hierarchy and map prominence

The ownership title view uses a shared actual-domain resolver for borders, labels,
ordinary selection and Ctrl+click. A secondary title includes its controlled
estates and the whole branches of actual vassals attached to it. The primary
title also receives unattached dependants. For a king this is the Crown, so realm
vassals do not become dependants of his secondary duchy. Unrelated secondary titles stay
separate. Broad view groups controlled subordinate titles; detailed view exposes
the local title or immediate vassal branch. Clan selection includes all holdings
and all depths of actual vassals, across disconnected territories.

At broad zoom, minor personal estates without a controlled superior dignity join
the owner's main title, even outside its de jure boundaries. They retain
their individual identities at detailed zoom. Separate earldoms and higher titles
remain distinct. This includes Courtenay's Okehampton and St Michael's Mount under
Devon without changing their geographic parents or their owners.

Live settlement ownership and allegiance determine control; the geographic tree
determines constituent titles. Neither alone establishes an actual domain. The
encyclopedia lists actual-domain towns and castles separately from de jure extent.

Broad ownership grouping also follows controlled title allegiance: Lancashire
can belong to Lancaster even when their geographic nodes are siblings. Selection
keeps the same title/clan when zoom changes; changing the view or label mode clears
it. The selected wash uses quarter-intensity colours and at least 85% opacity.

The separate **De jure** map view uses only geographic parents, title names and
geographic prominence, with colours independent of settlement ownership or clan
allegiance. Its labels, frontiers, selection and Ctrl+click use the same title key.

`mapLabelRank` controls presentation independently of structural tier and personal
`dignityRank`. Geographic presentation uses the researched title kind:
duchies/principalities use the largest font, earldoms and superior territorial
groups the next sizes, and local estates, boroughs and honours the smaller sizes.
Ownership labels use the represented dignity's name and rank. This changes
neither personal vassalage nor settlement ownership.

Known coverage limit: `county_pembroke` currently contains only `Pembroke_town`.
Its actual domain can additionally include attached vassals; its de jure list
still reflects only configured descendants. This does not establish that the
entire historical earldom consisted of one town. Tenby, Haverfordwest and the
other lesser lordships need explicit representation for their de jure extent. Do not fold
Carmarthen into Pembroke merely because of proximity or temporary possession.
See the [Dyfed archaeological survey](https://heneb.org.uk/archive/dyfed/HLC/milford/themes.htm)
for the distinction between Pembroke, Rhos/Haverford and Narberth.

From `src`:

```powershell
rtk proxy C:/Users/Joe/.pyenv/pyenv-win/versions/3.12.1/python.exe tools/build_title_encyclopedia_maquette.py
rtk proxy C:/Users/Joe/.pyenv/pyenv-win/versions/3.12.1/python.exe -m unittest discover -s tools -p test_title_encyclopedia_maquette.py -v
```

The generator checks source XML fingerprints and rejects missing parents,
cycles, cross-realm parents and unassigned village bindings. The tests verify
historical uncertainty survives a gameplay fallback, church estates remain
ordinary castles, and an owner's unrelated dignity does not merge territories.

The browser interaction is a maquette, not an in-game runtime test. The HTTP
preview was visually inspected in the app. The generated JavaScript passed
Node's syntax check; descendant settlement coverage and claimant data were also
checked against the generated proposal.
