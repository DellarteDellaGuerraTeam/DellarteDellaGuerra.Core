# Title hierarchy audit and corrections, 1471

Implemented 1 November 2026. Scope: distinguish territorial nesting, the dignity a house holds, its configured title allegiance, and its current faction allegiance. No settlement, initial holder, title ID or territorial parent was changed (142 titles, 72 holding houses).

## Ownership follow-up, 3 November 2026

The totals and portfolio examples below record the 1 November snapshot. The
[starting ownership audit](starting-ownership-audit-1471.md) supersedes its initial
holder assumptions: 26 settlement owners corrected, Wardour assigned to the
restored Crown with a Lovell claim, and Douglas of Lochleven added as a separate
household. St Andrews remains a documented church placeholder. Lovell's current
primary estate is Northampton under Warwick, rather than Wardour under Somerset.

## What the hierarchy means

The XML element (Kingdom/Duchy/County/Barony) continues to describe the territorial tree used for de jure settlement rollups, claims and the territorial hierarchy screen. It no longer has to confer a peerage of that rank. Optional `dignityRank` and `dignityName` seed the actual held title; `allegiance` selects a different title parent for house allegiance. References must exist within the same configured realm and the resulting chains must be acyclic.

A house answers through its highest dignity, then its oldest tenure, then its ordinal title ID. A potential liege must hold a strictly higher dignity somewhere in that house's portfolio. This prevents a duke's lesser castle from making the entire house subordinate to an earl or another duke. It is a deliberately simplified, single-liege house model: historically, homage could differ from estate to estate, and a peer could owe obligations on particular lands to another peer.

The title policy follows lawful holders. The political map follows controllers (occupation first), respects current kingdom membership and falls back to that kingdom's ruler when the title liege belongs to another faction. Thus the English territorial Crown and York's current political leadership can yield different answers. The clan encyclopedia now labels Title Liege, Current Political Liege, Territorial Nesting and Direct Title Vassals separately. The hierarchy screen is explicitly titled Territorial Hierarchy.

Geography alone cannot establish personal homage or a fifteenth-century affinity. Retainers could be attached through contracts, annuities and patronage; the territorial fallback retained for unaudited entries is a gameplay assumption. See the [Cambridge introduction to bastard feudalism](https://www.cambridge.org/core/books/abs/bastard-feudalism-english-society-and-the-law/introduction/2F18148E4FF7EFF5313635FB58D856F5). Nor does a sale of land automatically create a new subordinate tenure: see the [text of Quia Emptores](https://avalon.law.yale.edu/medieval/land.asp).

## Corrected dignities and allegiance

| House or entry | Implemented correction | Evidence and limits |
| --- | --- | --- |
| Neville of Middleham | Warwick/Salisbury uses Earl, independently of its duchy-tier region. Warwick and Glamorgan territorial counties are lower estate dignities. Warwick answers to the Crown. | [English Heritage, Middleham](https://www.english-heritage.org.uk/visit/places/middleham-castle/history/). An earl, not a duke. These estate entries do not invent additional earldoms. |
| Neville of Raby | Durham's geographic county seeds the Earldom of Westmorland and answers to the Crown. | [Raby family history](https://www.raby.co.uk/blog/2022/11/21/ancient-families-of-raby-castle/). This does not transfer the bishop's palatine authority to the Nevilles; the geographic ownership remains a separate issue. |
| Clifford | Cumberland seeds Barony de Clifford, directly under the Crown. | The Clifford earldom of Cumberland belongs to 1525, not 1471: [Complete Peerage](https://en.wikisource.org/wiki/Page%3AThe_Complete_Peerage_Ed_1_Vol_2.djvu/293). No Warwick affinity is inferred from location. |
| Talbot | Shropshire seeds Earldom of Shrewsbury, directly under the Crown; Hallamshire is a lower estate dignity. | [1442 chronicle extracts](https://kinghenryvi.com/1442/). York's lesser Ludlow estate does not reverse the house relationship. |
| Neville of Bergavenny | Hereford seeds Barony of Bergavenny, directly under the Crown. | [DNB, Edward Neville](https://en.wikisource.org/wiki/Dictionary_of_National_Biography,_1885-1900/Neville,_Edward). This removes a fictitious Neville earldom of Hereford. |
| Howard | Norwich seeds Barony of Howard, not an earldom. | [National Portrait Gallery](https://www.npg.org.uk/collections/search/person/mp85693/john-howard-1st-duke-of-norfolk); the Norfolk dukedom is later, in 1483. |
| de la Pole | Suffolk seeds a Duke dignity, independently of Norfolk; tenure 1463. | [Wingfield College timeline](https://www.wingfieldcollege.com/wp-content/uploads/2016/09/Wingfield-College-Timeline-Final-version.pdf). |
| Holland | Sherborne seeds the Duchy of Exeter and answers to the Crown, not Somerset. | [Oxford DNB, Henry Holland](https://academic.oup.com/oxford-dictionary-of-national-biography/reference/63486/reference-article-abstract/580661953). Represents the Lancastrian duke in the campaign's Readeption setting, not a claim that Sherborne constituted a duchy. Existing estate tenure retained. |
| Bourchier, Courtenay, Tudor, Woodville | Essex, Devon, Pembroke and Cheshire carry the corresponding earldom names (Essex, Devon, Pembroke and Rivers), directly under the Crown. Ceredigion is a lower Tudor estate. | Keeps existing peerage ranks and holders while removing territorial subordination to another peer. It does not certify every assigned seat. |
| Devereux | Gwynedd seeds Barony of Ferrers of Chartley, directly under the Crown. | [Complete Peerage](https://en.wikisource.org/wiki/Page%3AThe_Complete_Peerage_Ed_2_Vol_4.djvu/752). No Devereux earldom of Gwynedd is inferred. |
| Hastings | The primary Nottingham title answers directly to the Crown, avoiding accidental subordination to York's duchy-tier region. | [English Heritage, Ashby](https://www.english-heritage.org.uk/visit/places/ashby-de-la-zouch-castle/history/) and [Kirby Muxloe](https://www.english-heritage.org.uk/visit/places/kirby-muxloe-castle/history-unfinished-castle): Edward IV's close ally. Current faction allegiance remains distinct from the lawful title Crown. |
| FitzHugh | Skelton's allegiance points to Warwick, without moving it out of geographic Durham. | [Richard III Society, uprisings](https://richardiii.net/richard-iii-his-world/the-war-of-the-roses/rebellions-and-uprisings/). This models the documented 1470 political affinity; it is not proof of feudal homage for Skelton. |
| Gordon | Strathbogie seeds Earldom of Huntly, tenure 1445. | [Historic Environment Scotland, Huntly](https://www.historicenvironment.scot/visit/all/huntly-castle/history-and-stories/). |
| Douglas of Angus | Hermitage seeds Earldom of Angus, ensuring the house's actual earldom outranks its castle estate. | Existing [Scottish family audit](scottish-families-1471.md), using the [Douglas Book](https://deriv.nls.uk/dcn23/9653/96531805.23.pdf). Existing estate tenure retained. |
| Sinclair | Caithness seeds its earldom. The Sutherland geographic county is a lower estate dignity, not an additional Sinclair earldom. | [World Monuments Fund, Sinclair Girnigoe](https://www.wmf.org/sites/default/files/article/pdfs/pg_34-37_girnigoe.pdf). Sinclair's configured possession of Sutherland remains unresolved. |
| Hamilton | Lanark seeds Lordship of Hamilton, not an earldom. | [1445 parliamentary charter](https://www.rps.ac.uk/search.php?action=print&filename=jamesii_trans&id=2392&type=trans). |
| Kennedy and Maxwell | Carrick and Nithsdale seed the houses' lordships, not earldoms. | [Glasgow Kennedy study](https://theses.gla.ac.uk/70978/1/2019brennanphd.pdf); [St Andrews, Maxwell](https://www.st-andrews.ac.uk/history/ssne/item.php?id=531). Earl of Cassillis and Earl of Nithsdale belong to later periods. |
| Stirling, Ruthven, Dunbar of Westfield, MacLeod of Lewis, Cameron of Lochiel | Geographic counties use lower estate/lordship dignities rather than inventing earldoms. | Existing [Scottish family audit](scottish-families-1471.md). Baron is the game's lowest generic landed rank here: it does not confer a parliamentary Lord Ruthven before 1488, or make every clan chief a parliamentary peer. |

These are corrections within the existing settlement/house roster. Geographic seats remain abstractions: the Earl of Rivers does not thereby become Earl of Cheshire, nor does the Earl of Westmorland become Durham's prince-bishop.

## Result on a new campaign

These are lawful title relationships, before applying the York/Lancaster/current-faction filter. The root houses are Lancaster, Stewart and Macdonald of the Isles. Calder lies in the Scottish territorial tree but belongs politically to the Isles, so political realm totals differ.

| Territorial realm of house's primary title | Holding houses, including root | Direct root vassals | Primary dignities (King / Duke / Earl / Baron) | Earl-ranked houses with vassals |
| --- | ---: | ---: | --- | ---: |
| England | 47 | 25 | 1 / 6 / 11 / 29 | 8 of 11 |
| Scotland | 13 | 12 | 1 / 0 / 6 / 6 | 0 of 6 |
| Isles | 12 | 11 | 1 / 0 / 0 / 11 | 0 of 0 |

English earl-ranked followings currently include Warwick (FitzHugh, Sutton), Shrewsbury (Burgh, Grey of Codnor), Westmorland (Greystoke), Devon (Basset, Blount, Harrington), Kent/Grey of Ruthin (Fiennes of Saye and Sele, West), Arundel (Fiennes of Dacre), Northumberland (Lumley), and Rivers (Vernon). Apart from the explicitly discussed FitzHugh affinity, these inherited territorial fallbacks remain provisional, not individually verified historical retainer lists.

There is no target quota for intermediate vassals. Rank corrections legitimately flatten some chains, particularly Scotland and the Isles. The implementation does not manufacture additional clients to make earls busier.

## Remaining historical work

- Verify the provisional followings against indentures, retainers' records and estate tenure, distinguishing affinity from homage. Lovell under Warwick (following the 3 November ownership correction), Paston under Norfolk and Greystoke under Westmorland are examples needing evidence.
- Audit geographic ownership separately, especially Durham's bishopric, Sinclair/Sutherland, and seats used as representations of a remote dignity. No new ecclesiastical or dispossessed family was invented in this change.
- The Isles remain King-ranked as the existing game's representation of their separate political faction, not a claim that the Lord of the Isles held a royal crown.
- A full legal model would permit obligations per estate, multiple affinities and changes during the 1470-71 restorations. This change retains one primary house relationship.
- Legacy `heldSince` estate dates are partly approximate. They were not changed to manufacture vassalage. Only Huntly's 1445 and Suffolk's 1463 dignity dates were corrected here.

Political-map Owners labels now read the live held title name rather than looking its ID up in the territorial tree. This displays Devon/Pembroke as earldoms, Shropshire as Shrewsbury, Cheshire as Rivers, Suffolk as a duchy and Holland as Exeter. Kent, Sussex/Arundel, Oxford and Crawford now also explicitly name their actual earldom through `dignityName`. Geographic county names and territorial rollups retain their administrative meaning.

## Saves, conquest and verification

Start a **new campaign**. The rank is now `Earl` throughout the domain, configuration and serialised title records, with no legacy rank alias or migration. Older saved rank strings are unsupported. Geographic `County` elements and title IDs retain their existing meaning.

Saved conquest reattachments take precedence over configured allegiance. An initial allegiance is ignored once the title and target lie in different territorial realms. Conquest attachment compares territorial tiers, rather than confusing a peerage dignity with the territory's level.

Verified on 1 November 2026:

- Full Release solution build: passed with deployment disabled using `dotnet build DellarteDellaGuerra.sln --configuration Release --no-restore -p:ModuleId= -v:minimal` (existing warnings remain).
- Focused Domain.Tests filter `FullyQualifiedName~Titles|FullyQualifiedName~PoliticalMap`: 441 passed.
- Focused Infrastructure.Tests filter `FullyQualifiedName~Titles`: 78 passed, using the default Module content provider, which reads genealogy from the installed DellarteDellaGuerraMap module and the current title configuration embedded at build time. This includes parser validation, independent dignity/territorial tiers, conquest reattachments, all 72 house chains, unchanged territorial seat rollups and private-war claim/support scenarios.
- After the complete `Earl` rank rename: the full Release solution build passed again (81 existing warnings, no errors); 441 domain, 78 infrastructure and 5 serialization/political-map refresh tests passed (524 total). The UI reads `Earl` directly from the enum, with no display-only rank translation.
- Political-map dignity-name follow-up: full Release solution build passed (78 existing warnings, no errors); 89 title infrastructure tests and 273 political-map domain tests passed. Eleven new content cases cover earldom/duchy names used by the Owners labels. After the final reset-notification edit was rebuilt, 12 native-free smoke cases passed, including three actual Owners label adapter cases. Once the DADG game exited, all four DADG DLLs and PDBs were deployed to Core/bin/Win64_Shipping_Client and SHA-256 verified. Live appearance and physical keyboard input remain unverified.
- Compared configuration with HEAD: all 142 title IDs, territorial parents, seats and initial holder clans preserved. `git diff --check` passed.

The private-war content expectations were updated for the new followings rather than altering the scoring rules. With the test fixture's equal strength per house and minority holders, 19 of 117 inter-house opportunities score above zero; none reaches the declaration threshold under neutral, undistracted conditions. Foreign friendship and support deduplication checks still pass. Actual campaign strengths and relations vary.

Verification also repaired a stale political-map zoom test to use the configured threshold rather than the old numeric distance. Existing political-map work was preserved.

The earlier normal build deployment failed with access denied deleting `Bannerlord.Cannons.API.dll`; subsequent verification builds disabled that deployment step. After the DADG game exited, the final follow-up deployed the four DADG assemblies and symbols individually to `Core/bin/Win64_Shipping_Client`, verifying SHA-256 hashes and leaving dependency DLLs intact. Relaunch the game to load these binaries.

No live campaign or encyclopedia layout has been playtested as part of this change. Integration.Tests was not run, in accordance with the workspace instruction about its hanging behavior.
