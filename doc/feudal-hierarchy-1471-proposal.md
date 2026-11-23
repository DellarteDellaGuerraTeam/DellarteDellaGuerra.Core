# Feudal hierarchy 1471 — proposal

Status: **applied** to `config/titles.config.xml` (§3, §4 and §5), with the starting claims of §8. Needs a new campaign (§7.2).

This proposal rebuilds the Kingdom > Duchy > County > Barony tree in `config/titles.config.xml`. It has three goals:

1. Every town and castle sits inside a county, so the political map has no holes.
2. Duchies, seats and holders match history **at the campaign start date**.
3. Each county is one block of land on the map, with no exclaves cut off from it.

Only existing settlements and clans are used, with one exception, which is flagged.

Inputs:
- a geography pass over `settlements.xml` and the baked `settlement_cells.bin.gz` (seat distances, contiguity);
- web research, with sources at the end.

## 0. The date: early 1471, the Readeption

`DadgCampaignTimeModel` starts the campaign at 1471 plus one season, which is about February–March 1471. That is the **Readeption**:
- Henry VI was restored in October 1470, and `kingdom_england` is held by `clan_lancaster`.
- Warwick the Kingmaker is alive and runs the government. Barnet, where he died, was on 14 April 1471.
- Edward IV, Gloucester, Hastings and Rivers are in exile in Burgundy until they land at Ravenspur on 14 March.
- Clarence is Warwick's ally. The Angers agreement promised him the Duchy of York.
- Somerset (Beaufort), Devon (Courtenay) and Jasper Tudor are back in their titles.

So the grants made **after** Barnet do not apply yet. That includes Gloucester getting Middleham, Penrith and Barnard (June 1471), and Clarence's share of the Warwick inheritance (1472).

Warwick's seat is the Kingmaker's. The settlements file already agrees: `clan_neville_of_middleham` owns Warwick, Middleham, Cardiff, Caerphilly and Rye.

## 1. What is wrong today

### 1.1 Six duchy seats sit outside every county

The six duchy-seat castles below, and their 15 villages, belong to no county. They show as holes on the county view.

| Castle | Duchy today | Land around it |
|---|---|---|
| Pontefract | York | Hallamshire 62%, York 37% |
| Dunstanburgh | Northumberland | Warkworth 57%, Durham 42% |
| Peveril | Lancaster | Cheshire 60%, Hallamshire 40% |
| Framlingham | Norfolk | Essex 77%, Norwich 22% |
| Wardour | Somerset | Bristol 82%, Hampshire 17% |
| Harlech | Wales | Gwynedd 100% |

### 1.2 Historical errors at the start date

| Config today | Early 1471 |
|---|---|
| Duchy of York seated at **Pontefract** | Pontefract was a Duchy of **Lancaster** honour. York's principal seat was **Fotheringhay**. |
| **Duchy of Northumberland** (Percy), seated at Dunstanburgh | Northumberland was an **earldom**, restored to Percy in March 1470. There was no dukedom, and Dunstanburgh was a Lancaster castle. |
| The Kingmaker has no great title; his castles are scattered across York, Wales, Warwick and Sussex as plain baronies | Warwick was the greatest magnate in England: Earl of Warwick and Salisbury, lord of Glamorgan, Middleham, Sheriff Hutton, Penrith and Barnard. |
| Duchy of Somerset seated at **Wardour** | Wardour was Lovell's. **Corfe** was the Beaufort seat; the config has it as a Hampshire barony held by Stourton. |
| `county_devon` is held by Holland | Courtenay was restored to the earldom of Devon on 9 October 1470. |
| `county_pembroke` is held by Herbert | Jasper Tudor was restored as Earl of Pembroke during the Readeption. |
| Sutherland is a Sinclair barony under Orkney | Sutherland belonged to the **Sutherland** earls. Sinclair held Caithness and Ravenscraig, and resigned Orkney to the crown in 1470. |

### 1.3 Geographic misfits that cause exclaves on the map

| Barony | Problem |
|---|---|
| Kilchurn (Glenorchy) | Under Gowrie/Perth, 99 m from Perth: a 2,594-cell exclave, the largest on the map. |
| Worcester | Under Hereford; cut off on land from the rest of the county. |
| Dunbar | Under Lothian; cut off behind Berwick. |
| Dornoch (Sutherland) | 143 m from Kirkwall. Its village Forres is a pocket inside Moray. |
| Cawdor | Under Ross (Isles), though 23 m from Inverness. |
| Rye | 96 m from Chichester, 55 m from Canterbury. |
| Kendal | 72 m from Carlisle, 32 m from Lancaster. |

## 2. Structural rule: ducal demesne

A seat must be unique, and the cadet split in `ClaimPressureCampaignBehavior` needs a duchy seat. So a duchy's seat castle cannot also be a barony.

- **In the titles:** a duchy keeps its seat castle as **ducal demesne**. The duke holds it directly and it sits under no county. This is how the config already works.
- **On the political map, county and feudal levels:** a demesne castle and its villages are painted as part of the county whose land surrounds them. This leaves no holes. `tools/political_map_bake.py` picks that county as the one sharing the longest border with the demesne and prints it; with the applied hierarchy: Pontefract → York, Warwick → Oxford, Fotheringhay → Warwick, Stafford → Cheshire, Framlingham → Norwich, Corfe → Bristol, Harlech → Gwynedd.
- **On the political map, duchy level:** the demesne takes **its own duchy's** colour. Lancaster's Pontefract then shows as a Lancaster pocket inside Yorkshire, which is true to how scattered the Duchy of Lancaster was.

The code doesn't check a seat's settlement type; `Titles/` has no `IsTown` or `IsCastle` check. So a county can be seated at a castle where geography calls for it (Kilchurn and Dornoch below).

## 3. Proposed England

Counties are listed with their baronies; ★ marks a change.

### Duchy of Lancaster: `clan_lancaster` (crown), seat **Pontefract** ★, moved from York
- **Lancashire** (Lancaster): Piel, Liverpool, **Kendal ★** (from Cumberland)
- **Cheshire** (Chester): Beeston, Chirk, **Peveril ★** (the former seat, now a barony held by `clan_lancaster`)

Kenilworth, Hertford and Dunstanburgh were also Lancaster honours. They stay baronies where they lie geographically, and their holder becomes `clan_lancaster` (see §5).

### Earldom of Warwick and Salisbury ★ new: `clan_neville_of_middleham`, seat **Warwick** ★
This is a duchy-rank title; the id could be `duchy_warwick`. `barony_warwick` is removed and its castle becomes the seat.
- **Warwick** (Coventry): Kenilworth, Northampton, **Worcester ★** (from Hereford; this fixes the Hereford exclave)
- **Durham** (Newcastle): Durham, Barnard, Skelton
- **Cumberland** (Carlisle): Penrith

Like Lancaster, the duchy is split between the Midlands and the north, and that is historical. Warwick also keeps Middleham (a barony in County York), Glamorgan and Caerphilly (Wales) and Rye (Kent) as holdings outside the duchy. He was Warden of the West March, which fits Cumberland.

If Warwick dies at Barnet in-game, the existing holder-death flow passes the title on. Neither Gloucester nor Clarence gets a duchy at the start: Gloucester is a landless exile, and Clarence's dukedom is York.

### Duchy of York: `clan_york`, seat **Fotheringhay** ★
`barony_fotheringhay` is removed and its castle becomes the seat.
- **York** (York): Skipton, Middleham, Richmond, Helmsley
- **Hull** (Hull): Scarborough
- **Hallamshire** (Sheffield): Conisbrough (a York castle), Nottingham
- **Lincoln** (Lincoln): Tattershall

The Readeption government held Edward's lands and promised the duchy to Clarence. `clan_york` covers Edward, Clarence and Gloucester, so it stays the holder.

### Earldom of Northumberland: `clan_percy`, a county directly under the crown ★
- **Northumberland** ★ (`county_warkworth` renamed): Bamburgh, **Dunstanburgh ★** (the former seat, now a barony)

This replaces `duchy_northumberland`. Percy goes from duke to earl, which is a gameplay downgrade worth weighing.

### Duchy of Buckingham ★ new: `clan_stafford`, seat **Stafford** ★
`barony_stafford` is removed and its castle becomes the seat.
- **Hereford** (Hereford): Brecon, Painscastle
- **Shropshire** (Shrewsbury): Ludlow, Montgomery

Ludlow and Montgomery were March lands, so Edward's. They stay here for map coherence, with their existing holders. The duke was a minor in 1471, and his side during the Readeption is unverified.

### Duchy of Norfolk: `clan_mowbray`, seat Framlingham (unchanged)
- Norwich, Lynn, Suffolk, Essex (unchanged)

### Duchy of Somerset: `clan_beaufort`, seat **Corfe** ★
`barony_corfe` is removed from Hampshire and its castle becomes the seat.
- **Bristol** (Bristol): Chepstow, Gloucester, Farleigh, Sherborne, **Wardour ★** (the former seat, now a barony held by `clan_lovell`)
- **Devon** (Exeter): Dunster, Plymouth, Totnes, Taunton. The county holder changes from Holland to **`clan_courtenay` ★**, restored as Earl of Devon in October 1470.
- **Cornwall** (Launceston): Okehampton, St Michael's Mount. The county holder changes from Courtenay to **`clan_lancaster` ★**, since the Duchy of Cornwall belonged to Prince Edward of Westminster. Okehampton stays with Courtenay.

### Principality of Wales: `clan_lancaster`, seat Harlech (unchanged)
- Glamorgan (Neville, correct), **Pembroke** (holder changes from Herbert to **`clan_tudor` ★**), Ceredigion, Gwynedd

### Crown counties
- London: Windsor, Berkhamsted, Hertford
- Kent: Dover, Rochester, Tonbridge, **Rye ★** (from Sussex)
- Sussex: Lewes, Arundel
- Hampshire: Carisbrooke, Farnham, Winchester
- **Oxford** ★ (back to the crown): Donnington
- **Northumberland** ★ (Percy, see above)

**Result:** 6 duchy-rank titles plus the Principality, replacing 5 plus the Principality. Every English town and castle sits inside a county.

## 4. Proposed Scotland and the Isles

**Moves and new counties**
- **Earldom of March:** `barony_dunbar` ★ moves from Lothian to **Berwick**; Dunbar and Berwickshire made up that earldom. This fixes the Lothian exclave. The holder changes from `clan_hepburn` to `clan_stewart` (Albany).
- **County of Argyll** ★ new: `barony_glenorchy` becomes a county seated at **Kilchurn**, directly under Scotland, held by `clan_campbell`. This removes the largest exclave on the map, and brings in the Campbells, a 1471 power the tree is missing.
- **County of Sutherland** ★ new: `barony_sutherland` becomes a county seated at **Dornoch**, directly under Scotland.
  - No Sutherland clan exists. Either keep `clan_sinclair` as a placeholder, or add `clan_sutherland`; that would be the only new clan in this proposal.
  - Orkney keeps Caithness and Dirlot. Orkney has been crown land since 1470, so `clan_stewart` is correct.
- **Cawdor** ★ moves from Ross (Isles) to **Inverness** (Scotland). `clan_calder` is right: the Thanes of Cawdor were Calders.

**Holder change**
- **Fife** was a crown earldom, so its holder goes from `clan_lindsay` to `clan_stewart`. Lindsay keeps Crawford.

**Unchanged**
- Lewis: the Barra and Skye pieces are natural islands.
- Ross: Sleat stays here, since John of Islay was Earl of Ross until 1475.
- Lochaber and Islay.

## 5. Holder corrections at the start date

These are optional. Each one uses an existing clan and stands apart from the structural changes above.

| Title | Today | Early 1471 | Proposed |
|---|---|---|---|
| barony_barnard | harrington | Warwick (Beauchamp inheritance) | clan_neville_of_middleham |
| barony_penrith | neville_of_raby | Warwick (inherited 1460) | clan_neville_of_middleham |
| barony_richmond | tudor | Clarence (granted 1462) | clan_york |
| barony_helmsley | scrope_of_masham | Clarence (Ros reversion, 1465) | clan_york |
| barony_skipton | clifford | Sir William Stanley (Clifford attainted) | clan_stanley |
| barony_farleigh | holland | Gloucester's 1462 grant (Hungerford attainted) | clan_york |
| barony_dunster | dynham | Herbert | clan_herbert |
| barony_tattershall | willoughby | Bourchier, Lord Cromwell | clan_bourchier |
| barony_donnington | willoughby | de la Pole (Alice Chaucer) | clan_de_la_pole |
| barony_kenilworth | clinton | Duchy of Lancaster | clan_lancaster |
| barony_hertford | bourchier | Duchy of Lancaster | clan_lancaster |
| barony_dunstanburgh (new) | — | Duchy of Lancaster | clan_lancaster |
| barony_urquhart | dunbar_moray | John of Islay (1452–76) | clan_macdonald_isles |
| barony_hermitage | maxwell | Douglas, Earl of Angus | clan_douglas_angus |
| barony_threave | maxwell | crown keepership | clan_stewart |
| barony_mar | erskine | crown (Prince John) | clan_stewart |
| barony_lochleven | ruthven | Douglas of Lochleven | clan_douglas_angus (closest existing clan) |

Left alone because no fitting clan exists:
- Piel (Furness Abbey)
- Baconsthorpe (Heydon)
- Farnham and Taunton (Bishop of Winchester)
- Sherborne (Bishop of Salisbury)
- Dirlot (Gunn)

Settlement **ownership** in `settlements.xml` is a separate question from these de jure holders. Check that the two don't diverge in ways that start claims on day one.

## 6. Village-binding fixes

These are `settlements.xml` changes, not title changes. Each one left a small pocket of one county inside another.

| Village | Bound to | Problem | Fix |
|---|---|---|---|
| `dadg_sco_forres_village` | Dornoch | Sits inside Moray | **Done:** rebound to Elgin |
| `dadg_sco_castle_stalker_village` | Urquhart | Sits in Appin, inside Argyll | **Done:** rebound to Kilchurn |
| `dadg_sco_ardnaclach_village` | Duart | On the Appin mainland, not Mull | **Done:** rebound to Kilchurn |
| `dadg_sco_tain_village` | Dornoch | A burgh of Ross | **Done:** rebound to Balnagowan. Portmahomack's leftover sliver went once the bake measured distance over land |
| `dadg_sco_bona_village` | Cawdor | Sits inside Inverness | Fixed once Cawdor moves |
| `village_Huddersfield` | Sheffield | Sits inside York | — |
| `dadg_sco_lochinver_village`, `dadg_sco_achnahaird_village` | Stornoway | On the mainland, inside Ross | — |
| `dadg_sco_dirleton_castle` (a village) | St Andrews | Sits inside Lothian | — |

## 7. Applying it

1. Edit `titles.config.xml` following §3 and §4, and §5 if wanted. Then rerun `tools/political_map_bake.py` and check the preview: no towns or castles outside a county, and no land exclaves.
2. **Saves:** old saves are not supported, so start a new campaign. Removed ids: the baronies fotheringhay, warwick, stafford, corfe, glenorchy and sutherland, plus `duchy_northumberland`. New ids: duchy_warwick, duchy_buckingham, county_argyll, county_sutherland, barony_peveril, barony_dunstanburgh and barony_wardour.
3. **Tests:** the title-registry and de jure tests under `DellarteDellaGuerra.Domain.Tests/Titles` and `DellarteDellaGuerra.Infrastructure.Tests/Titles`, which use the real 1471 content, should keep passing. A config-level test that every town and castle has a County ancestor would lock in goal 1.

## 8. Starting claims

Blood claims come from the genealogy and only cover heirs of the current holders. A house that was attainted, dispossessed or exiled by spring 1471 has no blood claim on what it lost. So `titles.config.xml` now lists these as `<Claim clanId strength/>` children of the title. They are seeded on a new game, or on a save with no feudal state, as `ClaimOrigin.Conquest` claims, and they survive the blood-claim rebuild.

| Title | Holder in 1471 | Claimant | Strength | Basis |
|---|---|---|---|---|
| kingdom_england | Lancaster | York | Strong | Edward IV, king 1461–70, deposed by the Readeption |
| county_cheshire | Woodville | Lancaster | Strong | Edward of Westminster, Earl of Chester since 1454 |
| barony_denbigh | Herbert | York | Strong | Mortimer lordship, York's since 1425; Herbert was only steward |
| barony_brecon | Neville of Bergavenny | Stafford | Strong | Bohun lordship of Brecknock, heir the 2nd Duke of Buckingham |
| county_pembroke | Tudor | Herbert | Strong | Earl of Pembroke 1468–70, retaken by Jasper Tudor |
| barony_richmond | York | Tudor | Strong | Earldom of Richmond, forfeited to Clarence in 1462 |
| barony_skipton | Stanley | Clifford | Strong | Attainted 1461, granted to Sir William Stanley |
| county_warkworth | Percy | Neville of Middleham | Strong | Montagu was Earl of Northumberland 1464–70 |
| barony_nottingham | Hastings | Beaumont | Strong | Beaumont lands granted to Hastings; attainder reversed 1470 |
| barony_sherborne | Holland | Grey of Groby | Weak | Reversion of the Exeter estates through Anne Holland |
| barony_tattershall | Bourchier | Willoughby | Weak | Cromwell co-heiress (marginal) |
| barony_mar | Stewart | Erskine | Strong | Served heir 1438; the crown won the 1457 assize |
| county_moray | Stewart | Crichton | Strong | Earl in right of the elder Dunbar co-heiress from 1452 |
| county_moray | Stewart | Dunbar of Moray | Weak | Natural line of the 4th Dunbar earl |
| barony_dunbar | Stewart | Hepburn | Weak | Kept Dunbar Castle after 1435, occupied it 1445–46 |
| county_ross | MacDonald of the Isles | Ross | Weak | Heirs male of the old earls of Ross |
| county_lochaber | Cameron | MacDonald of Keppoch | Weak | Lordship of Lochaber, forfeited in the 1430s |
| barony_knapdale | MacDonald of the Isles | MacNeil | Weak | Hereditary keepers of Castle Sween |

No defensible claim was found for Clinton, Scrope of Masham, Dynham, Stourton, Tuchet, Halyburton, Home, MacAlister, MacKinnon or MacPhee, so they start with none. Holland on the crown and Neville of Raby on Middleham were left out as too thin.

How these play out under claim pressure:
- A Weak claim can only be pressed against a woman or a child.
- Claims across realms (Scotland against the Isles) and claims involving the royal house (York on the crown, Lancaster on Cheshire) are screened out of private wars. They are recorded, but they will not start a feud by themselves.

## Sources

**The Readeption**
- [Readeption of Henry VI](https://en.wikipedia.org/wiki/Readeption_of_Henry_VI)
- [The records of the Readeption Parliament (National Archives)](https://blog.nationalarchives.gov.uk/the-records-of-the-readeption-parliament-of-1470-71)
- [Angers agreement](https://www.historyofwar.org/articles/treaty_angers_agreement.html)
- [Richard III chronology](https://www.richardiii.ca/biography-of-richardiii-with-corollary-events/)

**Warwick the Kingmaker**
- [Richard Neville, 16th Earl of Warwick](https://en.wikipedia.org/wiki/Richard_Neville,_16th_Earl_of_Warwick)
- [Cardiff Castle: the Nevilles](https://www.cardiffcastle.com/history/nevilles/)
- [Penrith Castle history](https://www.english-heritage.org.uk/visit/places/penrith-castle/history/)
- [Barnard Castle history](https://english-heritage.org.uk/visit/places/barnard-castle/history)
- [Richard III patent grants, June 1471](https://richardiii-ipup.org.uk/riii/44)

**Lancaster, York and the north**
- [Honour of Pontefract](https://en.wikipedia.org/wiki/Honour_of_Pontefract)
- [Pontefract Castle](https://en.wikipedia.org/wiki/Pontefract_Castle)
- [Dunstanburgh Castle](https://en.wikipedia.org/wiki/Dunstanburgh_Castle)
- [Duchy of Lancaster: castles](https://www.duchyoflancaster.co.uk/our-estates/castles-historic-monuments/)
- [Fotheringhay Castle](https://en.wikipedia.org/wiki/Fotheringhay_Castle)
- [Henry Percy, 4th Earl of Northumberland](https://en.wikipedia.org/wiki/Henry_Percy,_4th_Earl_of_Northumberland)
- [John Neville, Marquess Montagu](https://www.britannica.com/biography/John-Neville-earl-of-Northumberland-Lord-Montagu)

**Clarence's holdings**
- [Honour of Richmond](https://en.wikipedia.org/wiki/Honour_of_Richmond)
- [Helmsley ownership](https://bilsdale-pc.gov.uk/local-history-articles/detailed-ownership)

**Lancastrian restorations**
- [John Courtenay, 15th Earl of Devon](https://en.wikipedia.org/wiki/John_Courtenay,_15th_Earl_of_Devon)
- [Edmund Beaufort (d. 1471)](https://en.wikipedia.org/wiki/Edmund_Beaufort_(died_1471))
- [Jasper Tudor and the Readeption](https://tudortimes.co.uk/people/jasper-tudor-life-story/readeption-of-henry-vi)

**Somerset and the west**
- [Corfe Castle](https://en.wikipedia.org/wiki/Corfe_Castle)
- [Wardour Castle](https://en.wikipedia.org/wiki/Wardour_Castle)

**Other English holders**
- [Henry Clifford, 10th Baron Clifford](https://en.wikipedia.org/wiki/Henry_Clifford,_10th_Baron_Clifford)
- [Sir William Stanley](https://luminarium.org/encyclopedia/williamstanley.htm)
- [Humphrey Bourchier, Lord Cromwell](https://en.wikipedia.org/wiki/Humphrey_Bourchier,_1st_Baron_Cromwell)
- [Robert Hungerford, 3rd Baron Hungerford](https://en.wikipedia.org/wiki/Robert_Hungerford,_3rd_Baron_Hungerford)
- [William Herbert, 1st Earl of Pembroke](https://en.wikipedia.org/wiki/William_Herbert,_1st_Earl_of_Pembroke_(died_1469))
- [Donnington Castle](https://en.wikipedia.org/wiki/Donnington_Castle)
- [Stafford Castle](https://en.wikipedia.org/wiki/Stafford_Castle)
- [Lordship of Brecknock](https://en.wikipedia.org/wiki/Lordship_of_Brecknock)

**Wales**
- [Justiciar of South Wales](https://en.wikipedia.org/wiki/Justiciar_of_South_Wales)
- [Chamberlain of North Wales](https://en.wikipedia.org/wiki/Chamberlain_of_North_Wales)

**Scotland: Caithness, Sutherland and Orkney**
- [William Sinclair, 1st Earl of Caithness](https://en.wikipedia.org/wiki/William_Sinclair,_1st_Earl_of_Caithness)
- [Ravenscraig Castle](https://www.historicenvironment.scot/visit/all/ravenscraig-castle/history-and-stories/)
- [John Sutherland, 8th Earl of Sutherland](https://en.wikipedia.org/wiki/John_Sutherland,_8th_Earl_of_Sutherland)

**Scotland: Ross and the Isles**
- [John of Islay, Earl of Ross](https://en.wikipedia.org/wiki/John_of_Islay,_Earl_of_Ross)
- [Castle Urquhart](https://jacobitetrail.co.uk/castle-urquhart)

**Scotland: crown earldoms**
- [Earl of Mar](https://en.wikipedia.org/wiki/Earl_of_Mar)
- [Kildrummy Castle](https://en.wikipedia.org/wiki/Kildrummy_Castle)
- [Earl of Moray](https://en.wikipedia.org/wiki/Earl_of_Moray)
- [Darnaway Castle](https://en.wikipedia.org/wiki/Darnaway_Castle)

**Scotland: other castles and holders**
- [Kilchurn Castle](https://en.wikipedia.org/wiki/Kilchurn_Castle)
- [Hermitage Castle](https://en.wikipedia.org/wiki/Hermitage_Castle)
- [Threave Castle (Canmore)](https://canmore.org.uk/site/64698)
- [Lochleven Castle](https://en.wikipedia.org/wiki/Lochleven_Castle)
- [David Lindsay, 5th Earl of Crawford](https://en.wikipedia.org/wiki/David_Lindsay,_1st_Duke_of_Montrose)
